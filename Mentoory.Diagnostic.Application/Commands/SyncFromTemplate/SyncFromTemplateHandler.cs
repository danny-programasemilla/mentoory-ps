using Mentoory.Diagnostic.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Microsoft.Extensions.Logging;

namespace Mentoory.Diagnostic.Application.Commands.SyncFromTemplate;

/// <summary>
/// Handles the synchronization of new questions from a source template into a project form.
/// </summary>
public partial class SyncFromTemplateHandler : BaseCommandHandler<SyncFromTemplateCommand>
{
    private readonly IProjectFormRepository _projectFormRepository;
    private readonly IFormTemplateRepository _formTemplateRepository;
    private readonly ILogger<SyncFromTemplateHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncFromTemplateHandler"/> class.
    /// </summary>
    /// <param name="projectFormRepository">The project form repository for persistence operations.</param>
    /// <param name="formTemplateRepository">The form template repository for loading the source template.</param>
    /// <param name="logger">The logger instance.</param>
    public SyncFromTemplateHandler(
        IProjectFormRepository projectFormRepository,
        IFormTemplateRepository formTemplateRepository,
        ILogger<SyncFromTemplateHandler> logger)
    {
        _projectFormRepository = projectFormRepository;
        _formTemplateRepository = formTemplateRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task<Result> Handle(SyncFromTemplateCommand request, CancellationToken cancellationToken)
    {
        var form = await _projectFormRepository.GetByExternalIdWithQuestionsAsync(
            request.ProjectFormExternalId, cancellationToken);

        if (form is null)
        {
            return Failure(ResultErrorCodes.GenericError,
                ("Form", "El formulario del proyecto no fue encontrado."));
        }

        if (form.SourceTemplateId is null)
        {
            return Failure(ResultErrorCodes.GenericError,
                ("Form", "El formulario no tiene una plantilla de origen asociada."));
        }

        var template = await _formTemplateRepository.GetByIdWithQuestionsAsync(
            form.SourceTemplateId.Value, cancellationToken);

        if (template is null)
        {
            return Failure(ResultErrorCodes.GenericError,
                ("Template", "La plantilla de origen no fue encontrada."));
        }

        form.SyncNewQuestionsFromTemplate(template);

        _projectFormRepository.Update(form);
        await _projectFormRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        LogFormSynced(form.ExternalId, template.ExternalId);

        return Success();
    }

    /// <summary>
    /// Logs when a form is successfully synced from its source template.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Project form {ProjectFormExternalId} synced from template {TemplateExternalId}")]
    partial void LogFormSynced(Guid projectFormExternalId, Guid templateExternalId);
}
