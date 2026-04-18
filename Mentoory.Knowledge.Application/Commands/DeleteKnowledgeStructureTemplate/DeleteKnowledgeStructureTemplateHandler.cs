using Mentoory.Knowledge.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Microsoft.Extensions.Logging;

namespace Mentoory.Knowledge.Application.Commands.DeleteKnowledgeStructureTemplate;

/// <summary>
/// Handles deletion of a knowledge structure template.
/// </summary>
public partial class DeleteKnowledgeStructureTemplateHandler
    : BaseCommandHandler<DeleteKnowledgeStructureTemplateCommand>
{
    private readonly IKnowledgeStructureTemplateRepository _repository;
    private readonly IKnowledgeStructureRepository _structureRepository;
    private readonly ILogger<DeleteKnowledgeStructureTemplateHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteKnowledgeStructureTemplateHandler"/> class.
    /// </summary>
    /// <param name="repository">The knowledge structure template repository.</param>
    /// <param name="structureRepository">The knowledge structure (clone) repository used for the EC-01 clone-count guard.</param>
    /// <param name="logger">The logger instance.</param>
    public DeleteKnowledgeStructureTemplateHandler(
        IKnowledgeStructureTemplateRepository repository,
        IKnowledgeStructureRepository structureRepository,
        ILogger<DeleteKnowledgeStructureTemplateHandler> logger)
    {
        _repository = repository;
        _structureRepository = structureRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task<Result> Handle(
        DeleteKnowledgeStructureTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var template = await _repository.GetByExternalIdAsync(request.ExternalId, cancellationToken);

        if (template is null)
        {
            return Failure(ResultErrorCodes.GenericError,
                ("Plantilla", "La plantilla no fue encontrada."));
        }

        var cloneCount = await _structureRepository.CountClonesBySourceTemplateIdAsync(template.Id, cancellationToken);
        if (cloneCount > 0)
        {
            return Failure(ResultErrorCodes.GenericError,
                ("Plantilla", $"No se puede eliminar: la plantilla tiene clones en uso por {cloneCount} proyecto(s). Archívela en su lugar."));
        }

        _repository.Remove(template);

        await _repository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        LogTemplateDeleted(request.ExternalId);

        return Success();
    }

    /// <summary>
    /// Logs when a knowledge structure template is successfully deleted.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Knowledge structure template {ExternalId} deleted.")]
    partial void LogTemplateDeleted(Guid externalId);
}
