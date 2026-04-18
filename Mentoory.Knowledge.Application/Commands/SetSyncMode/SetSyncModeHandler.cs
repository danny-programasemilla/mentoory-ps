using Mentoory.Knowledge.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using KS = Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructure.KnowledgeStructure;

namespace Mentoory.Knowledge.Application.Commands.SetSyncMode;

/// <summary>
/// Handles changes to the sync mode of a knowledge structure (project clone).
/// </summary>
public class SetSyncModeHandler : BaseCommandHandler<SetSyncModeCommand>
{
    private readonly IKnowledgeStructureRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="SetSyncModeHandler"/> class.
    /// </summary>
    /// <param name="repository">The knowledge structure repository.</param>
    public SetSyncModeHandler(IKnowledgeStructureRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc />
    public override async Task<Result> Handle(
        SetSyncModeCommand request,
        CancellationToken cancellationToken)
    {
        KS? structure = await _repository.GetByExternalIdAsync(request.StructureExternalId, cancellationToken);
        if (structure is null)
        {
            return Failure(ResultErrorCodes.GenericError,
                ("Estructura", "La estructura de conocimiento no fue encontrada."));
        }

        try
        {
            structure.SetSyncMode(request.SyncMode);
        }
        catch (InvalidOperationException ex)
        {
            return Failure(ResultErrorCodes.GenericError, ("SyncMode", ex.Message));
        }

        await _repository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        return Success();
    }
}
