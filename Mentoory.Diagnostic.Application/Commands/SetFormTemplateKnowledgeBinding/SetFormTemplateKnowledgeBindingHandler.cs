using Mentoory.Diagnostic.Domain.Repositories;
using Mentoory.Knowledge.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Microsoft.Extensions.Logging;

namespace Mentoory.Diagnostic.Application.Commands.SetFormTemplateKnowledgeBinding;

/// <summary>
/// Handles binding a knowledge structure template to a form template so the diagnostic
/// clone cascade can rewrite template-topic ids to project-topic ids (FR-K20, FR-K21).
/// </summary>
public partial class SetFormTemplateKnowledgeBindingHandler
    : BaseCommandHandler<SetFormTemplateKnowledgeBindingCommand>
{
    private readonly IFormTemplateRepository _formTemplateRepository;
    private readonly IKnowledgeStructureTemplateRepository _knowledgeTemplateRepository;
    private readonly ILogger<SetFormTemplateKnowledgeBindingHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SetFormTemplateKnowledgeBindingHandler"/> class.
    /// </summary>
    /// <param name="formTemplateRepository">The form template repository.</param>
    /// <param name="knowledgeTemplateRepository">The knowledge structure template repository.</param>
    /// <param name="logger">The logger instance.</param>
    public SetFormTemplateKnowledgeBindingHandler(
        IFormTemplateRepository formTemplateRepository,
        IKnowledgeStructureTemplateRepository knowledgeTemplateRepository,
        ILogger<SetFormTemplateKnowledgeBindingHandler> logger)
    {
        _formTemplateRepository = formTemplateRepository;
        _knowledgeTemplateRepository = knowledgeTemplateRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task<Result> Handle(
        SetFormTemplateKnowledgeBindingCommand request,
        CancellationToken cancellationToken)
    {
        if (request.DefaultKnowledgeStructureTemplateExternalId.HasValue)
        {
            var exists = await _knowledgeTemplateRepository.ExistsByExternalIdAsync(
                request.DefaultKnowledgeStructureTemplateExternalId.Value,
                cancellationToken);

            if (!exists)
            {
                return Failure(ResultErrorCodes.GenericError,
                    ("Plantilla de conocimiento", "La plantilla de conocimiento no existe o está archivada."));
            }
        }

        var formTemplate = await _formTemplateRepository.GetByExternalIdAsync(
            request.FormTemplateExternalId,
            cancellationToken);

        if (formTemplate is null)
        {
            return Failure(ResultErrorCodes.GenericError,
                ("Formulario", "El formulario no fue encontrado."));
        }

        formTemplate.SetDefaultKnowledgeStructureTemplate(request.DefaultKnowledgeStructureTemplateExternalId);

        await _formTemplateRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        LogBindingUpdated(formTemplate.ExternalId, request.DefaultKnowledgeStructureTemplateExternalId);

        return Success();
    }

    /// <summary>
    /// Logs that a form template's knowledge binding was updated.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Form template {FormTemplateExternalId} knowledge binding set to {KnowledgeStructureTemplateExternalId}")]
    partial void LogBindingUpdated(Guid formTemplateExternalId, Guid? knowledgeStructureTemplateExternalId);
}
