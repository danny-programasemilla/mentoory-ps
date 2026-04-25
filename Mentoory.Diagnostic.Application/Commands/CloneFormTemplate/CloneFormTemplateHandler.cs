using Mentoory.Diagnostic.Domain.Repositories;
using Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructureTemplate;
using Mentoory.Knowledge.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Microsoft.Extensions.Logging;
using KS = Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructure.KnowledgeStructure;

namespace Mentoory.Diagnostic.Application.Commands.CloneFormTemplate;

/// <summary>
/// Handles the cloning of a form template into a project-specific form. Under the
/// project-owned KS binding (spec 016 Phase 9), the project's <c>KnowledgeStructure</c>
/// is materialized at project creation, not here. This handler only checks that the form
/// template is compatible with the project's KS and rewrites <c>Question.TopicId</c>
/// values from template-topic ids to the project's existing topic ids.
/// </summary>
public partial class CloneFormTemplateHandler : BaseCommandHandler<CloneFormTemplateCommand>
{
    private readonly IFormTemplateRepository _formTemplateRepository;
    private readonly IProjectFormRepository _projectFormRepository;
    private readonly IKnowledgeStructureTemplateRepository _ksTemplateRepository;
    private readonly IKnowledgeStructureRepository _ksStructureRepository;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<CloneFormTemplateHandler> _logger;

    public CloneFormTemplateHandler(
        IFormTemplateRepository formTemplateRepository,
        IProjectFormRepository projectFormRepository,
        IKnowledgeStructureTemplateRepository ksTemplateRepository,
        IKnowledgeStructureRepository ksStructureRepository,
        ITimeProvider timeProvider,
        ILogger<CloneFormTemplateHandler> logger)
    {
        _formTemplateRepository = formTemplateRepository;
        _projectFormRepository = projectFormRepository;
        _ksTemplateRepository = ksTemplateRepository;
        _ksStructureRepository = ksStructureRepository;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task<Result> Handle(CloneFormTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await _formTemplateRepository.GetByExternalIdWithQuestionsAsync(
            request.SourceTemplateExternalId, cancellationToken);

        if (template is null)
        {
            return Failure(ResultErrorCodes.GenericError,
                ("Template", "La plantilla de formulario no fue encontrada."));
        }

        IReadOnlyDictionary<long, long>? topicIdRewriteMap = null;

        if (template.DefaultKnowledgeStructureTemplateExternalId is Guid formKsTemplateExternalId)
        {
            var projectStructure = await _ksStructureRepository.GetByProjectIdAsync(
                request.ProjectId, cancellationToken);
            if (projectStructure is null)
            {
                return Failure(ResultErrorCodes.GenericError,
                    ("KnowledgeStructure", "El proyecto no tiene estructura de conocimiento."));
            }

            if (projectStructure.SourceTemplateId is not long projectSourceTemplateId)
            {
                return Failure(ResultErrorCodes.GenericError,
                    ("KnowledgeStructure", "No se pudo resolver la plantilla de conocimiento del proyecto."));
            }

            var projectKsTemplate = await _ksTemplateRepository.GetByIdAsync(projectSourceTemplateId, cancellationToken);
            if (projectKsTemplate is null)
            {
                return Failure(ResultErrorCodes.GenericError,
                    ("KnowledgeStructure", "No se pudo resolver la plantilla de conocimiento del proyecto."));
            }

            if (formKsTemplateExternalId != projectKsTemplate.ExternalId)
            {
                return Failure(ResultErrorCodes.GenericError,
                    ("Cascada", "Este formulario está diseñado para una estructura de conocimiento diferente a la del proyecto."));
            }

            var fullTreeTemplate = await _ksTemplateRepository.GetByExternalIdWithFullTreeAsync(
                formKsTemplateExternalId, cancellationToken);
            if (fullTreeTemplate is null)
            {
                return Failure(ResultErrorCodes.GenericError,
                    ("Cascada", "La plantilla de conocimiento enlazada no fue encontrada."));
            }

            topicIdRewriteMap = BuildTopicIdRewriteMap(fullTreeTemplate, projectStructure);
        }

        Domain.Aggregates.ProjectForm.ProjectForm projectForm;
        try
        {
            projectForm = Domain.Aggregates.ProjectForm.ProjectForm.CloneFromTemplate(
                template, request.ProjectId, request.IncubatorId, _timeProvider.UtcNow, topicIdRewriteMap);
        }
        catch (InvalidOperationException ex)
        {
            return Failure(ResultErrorCodes.GenericError,
                ("Cascada", $"No se puede clonar el formulario: {ex.Message}"));
        }

        _projectFormRepository.Add(projectForm);
        await _projectFormRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        LogFormCloned(projectForm.ExternalId, template.ExternalId);

        return Success();
    }

    private static IReadOnlyDictionary<long, long> BuildTopicIdRewriteMap(
        KnowledgeStructureTemplate ksTemplate,
        KS projectStructure)
    {
        var templateTopicsByExternalId = new Dictionary<Guid, long>();
        foreach (var moduleTemplate in ksTemplate.Modules)
        {
            foreach (var topicTemplate in moduleTemplate.Topics)
            {
                templateTopicsByExternalId[topicTemplate.ExternalId] = topicTemplate.Id;
            }
        }

        var rewriteMap = new Dictionary<long, long>();
        foreach (var module in projectStructure.Modules)
        {
            foreach (var topic in module.Topics)
            {
                if (topic.SourceTemplateTopicExternalId is Guid sourceExternalId
                    && templateTopicsByExternalId.TryGetValue(sourceExternalId, out var templateTopicId))
                {
                    rewriteMap[templateTopicId] = topic.Id;
                }
            }
        }

        return rewriteMap;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Project form {ProjectFormExternalId} cloned from template {TemplateExternalId}")]
    partial void LogFormCloned(Guid projectFormExternalId, Guid templateExternalId);
}
