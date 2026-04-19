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
/// Handles the cloning of a form template into a project-specific form.
/// When the source template declares a default knowledge structure template (FR-K21),
/// this handler also auto-provisions (or reuses) the matching project knowledge structure
/// and rewrites every cloned question's <c>TopicId</c> from template-topic ids to
/// project-topic ids — closing the dangling <c>Questions.TopicId</c> FK.
/// </summary>
public partial class CloneFormTemplateHandler : BaseCommandHandler<CloneFormTemplateCommand>
{
    private readonly IFormTemplateRepository _formTemplateRepository;
    private readonly IProjectFormRepository _projectFormRepository;
    private readonly IKnowledgeStructureTemplateRepository _ksTemplateRepository;
    private readonly IKnowledgeStructureRepository _ksStructureRepository;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<CloneFormTemplateHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CloneFormTemplateHandler"/> class.
    /// </summary>
    /// <param name="formTemplateRepository">The form template repository for loading templates.</param>
    /// <param name="projectFormRepository">The project form repository for persisting cloned forms.</param>
    /// <param name="ksTemplateRepository">The knowledge structure template repository for resolving bound templates.</param>
    /// <param name="ksStructureRepository">The knowledge structure repository for reusing or provisioning project clones.</param>
    /// <param name="timeProvider">The time provider for obtaining current UTC time.</param>
    /// <param name="logger">The logger instance.</param>
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

        var utcNow = _timeProvider.UtcNow;

        IReadOnlyDictionary<long, long>? topicIdRewriteMap = null;
        KS? cascadedStructure = null;

        if (template.DefaultKnowledgeStructureTemplateExternalId is Guid ksTemplateExternalId)
        {
            var cascadeResult = await ResolveCascadeAsync(
                ksTemplateExternalId,
                request.ProjectId,
                request.IncubatorId,
                utcNow,
                cancellationToken);

            if (cascadeResult.Failure is not null)
            {
                return cascadeResult.Failure;
            }

            cascadedStructure = cascadeResult.Structure;

            // TODO: future hardening — share a DbContextTransaction across both DbContexts for
            // true cross-module atomicity (per research R3). Acceptable risk for v1 MVP: save
            // Knowledge first so EF populates IDENTITY columns on any newly-cloned project
            // topics, then build the rewrite map below with the now-persisted ids. If the
            // Diagnostic save fails afterwards the knowledge writes will need manual cleanup.
            if (cascadeResult.CreatedStructure)
            {
                await _ksStructureRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
            }

            // Build the rewrite map AFTER the structure is persisted so project-topic ids are
            // the real DB-assigned values (not the transient 0 of a fresh entity).
            topicIdRewriteMap = BuildTopicIdRewriteMap(cascadeResult.FullTreeTemplate!, cascadedStructure!);
        }

        Domain.Aggregates.ProjectForm.ProjectForm projectForm;
        try
        {
            projectForm = Domain.Aggregates.ProjectForm.ProjectForm.CloneFromTemplate(
                template, request.ProjectId, request.IncubatorId, utcNow, topicIdRewriteMap);
        }
        catch (InvalidOperationException ex)
        {
            // FR-K23: unresolved template-topic → project-topic mapping aborts the whole operation.
            return Failure(ResultErrorCodes.GenericError,
                ("Cascada", $"No se puede clonar el formulario: {ex.Message}"));
        }

        _projectFormRepository.Add(projectForm);
        await _projectFormRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        LogFormCloned(projectForm.ExternalId, template.ExternalId);

        if (cascadedStructure is not null)
        {
            LogCascadedKnowledgeStructure(cascadedStructure.ExternalId, projectForm.ExternalId);
        }

        return Success();
    }

    private async Task<CascadeResolution> ResolveCascadeAsync(
        Guid ksTemplateExternalId,
        long projectId,
        long incubatorId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        // Step 1: resolve the bound knowledge template (Guid → Id) so we can look up existing clones.
        var knowledgeTemplate = await _ksTemplateRepository.GetByExternalIdAsync(
            ksTemplateExternalId, cancellationToken);

        if (knowledgeTemplate is null)
        {
            return CascadeResolution.Fail(Failure(ResultErrorCodes.GenericError,
                ("Cascada", "La plantilla de conocimiento enlazada no fue encontrada.")));
        }

        // Step 2: try to reuse an existing project clone for this template.
        var existingStructure = await _ksStructureRepository.GetByProjectAndSourceTemplateIdAsync(
            projectId, knowledgeTemplate.Id, cancellationToken);

        KS structure;
        bool createdStructure;
        KnowledgeStructureTemplate fullTreeTemplate;

        if (existingStructure is null)
        {
            // Step 3a: no clone exists yet; load full tree and create a fresh one.
            var loaded = await _ksTemplateRepository.GetByExternalIdWithFullTreeAsync(
                ksTemplateExternalId, cancellationToken);

            if (loaded is null)
            {
                // Shouldn't happen (we just resolved the template by ExternalId above) — guard defensively.
                return CascadeResolution.Fail(Failure(ResultErrorCodes.GenericError,
                    ("Cascada", "La plantilla de conocimiento enlazada no fue encontrada.")));
            }

            fullTreeTemplate = loaded;
            structure = KS.CloneFromTemplate(fullTreeTemplate, projectId, incubatorId, utcNow);
            _ksStructureRepository.Add(structure);
            createdStructure = true;
        }
        else
        {
            // Step 3b: reuse the existing clone. We still need the template's tree to build
            // the template-topic-id side of the rewrite map.
            structure = existingStructure;
            createdStructure = false;

            var loaded = await _ksTemplateRepository.GetByExternalIdWithFullTreeAsync(
                ksTemplateExternalId, cancellationToken);

            if (loaded is null)
            {
                return CascadeResolution.Fail(Failure(ResultErrorCodes.GenericError,
                    ("Cascada", "La plantilla de conocimiento enlazada no fue encontrada.")));
            }

            fullTreeTemplate = loaded;
        }

        return CascadeResolution.Ok(structure, fullTreeTemplate, createdStructure);
    }

    private IReadOnlyDictionary<long, long> BuildTopicIdRewriteMap(
        KnowledgeStructureTemplate ksTemplate,
        KS projectStructure)
    {
        // Pair TEMPLATE-topic.ExternalId (the stable identity that survives cloning) against
        // the PROJECT-topic whose SourceTemplateTopicExternalId equals it. The resulting map
        // keys on the template-topic's INTERNAL id (what diagnostic QuestionTemplates reference)
        // and values the project-topic's internal id (what cloned Questions must point to).
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

    /// <summary>
    /// Logs when a form is successfully cloned from a template.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Project form {ProjectFormExternalId} cloned from template {TemplateExternalId}")]
    partial void LogFormCloned(Guid projectFormExternalId, Guid templateExternalId);

    /// <summary>
    /// Logs when the diagnostic cascade auto-provisions or reuses a knowledge structure for the project.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Cascaded knowledge structure {StructureExternalId} for form {FormExternalId}")]
    partial void LogCascadedKnowledgeStructure(Guid structureExternalId, Guid formExternalId);

    /// <summary>
    /// Carries the cascade resolution outcome: either a ready-to-use structure + full-tree template
    /// (rewrite map is built by the caller AFTER saving so project-topic ids are DB-populated), or a Failure.
    /// </summary>
    private readonly record struct CascadeResolution(
        KS? Structure,
        KnowledgeStructureTemplate? FullTreeTemplate,
        bool CreatedStructure,
        Result? Failure)
    {
        public static CascadeResolution Ok(
            KS structure,
            KnowledgeStructureTemplate fullTreeTemplate,
            bool createdStructure) => new(structure, fullTreeTemplate, createdStructure, null);

        public static CascadeResolution Fail(Result failure) => new(null, null, false, failure);
    }
}
