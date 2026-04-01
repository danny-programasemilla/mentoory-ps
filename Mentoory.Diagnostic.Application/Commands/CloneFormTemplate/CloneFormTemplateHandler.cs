using Mentoory.Diagnostic.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Microsoft.Extensions.Logging;

namespace Mentoory.Diagnostic.Application.Commands.CloneFormTemplate;

/// <summary>
/// Handles the cloning of a form template into a project-specific form.
/// </summary>
public partial class CloneFormTemplateHandler : BaseCommandHandler<CloneFormTemplateCommand>
{
    private readonly IFormTemplateRepository _formTemplateRepository;
    private readonly IProjectFormRepository _projectFormRepository;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<CloneFormTemplateHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CloneFormTemplateHandler"/> class.
    /// </summary>
    /// <param name="formTemplateRepository">The form template repository for loading templates.</param>
    /// <param name="projectFormRepository">The project form repository for persisting cloned forms.</param>
    /// <param name="timeProvider">The time provider for obtaining current UTC time.</param>
    /// <param name="logger">The logger instance.</param>
    public CloneFormTemplateHandler(
        IFormTemplateRepository formTemplateRepository,
        IProjectFormRepository projectFormRepository,
        ITimeProvider timeProvider,
        ILogger<CloneFormTemplateHandler> logger)
    {
        _formTemplateRepository = formTemplateRepository;
        _projectFormRepository = projectFormRepository;
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

        var projectForm = Domain.Aggregates.ProjectForm.ProjectForm.CloneFromTemplate(
            template, request.ProjectId, request.IncubatorId, utcNow);

        _projectFormRepository.Add(projectForm);
        await _projectFormRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        LogFormCloned(projectForm.ExternalId, template.ExternalId);

        return Success();
    }

    /// <summary>
    /// Logs when a form is successfully cloned from a template.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Project form {ProjectFormExternalId} cloned from template {TemplateExternalId}")]
    partial void LogFormCloned(Guid projectFormExternalId, Guid templateExternalId);
}
