using Mentoory.Knowledge.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using KS = Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructure.KnowledgeStructure;

namespace Mentoory.Knowledge.Application.Commands.DeleteResource;

/// <summary>
/// Handles the removal of a resource from a knowledge structure (project clone).
/// </summary>
public class DeleteResourceHandler : BaseCommandHandler<DeleteResourceCommand>
{
    private readonly IKnowledgeStructureRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteResourceHandler"/> class.
    /// </summary>
    /// <param name="repository">The knowledge structure repository.</param>
    public DeleteResourceHandler(IKnowledgeStructureRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc />
    public override async Task<Result> Handle(
        DeleteResourceCommand request,
        CancellationToken cancellationToken)
    {
        KS? structure = await _repository.GetByExternalIdWithFullTreeAsync(
            request.StructureExternalId,
            cancellationToken);

        if (structure is null)
        {
            return Failure(ResultErrorCodes.GenericError,
                ("Estructura", "La estructura de conocimiento no fue encontrada."));
        }

        try
        {
            structure.RemoveResource(request.ResourceExternalId);
        }
        catch (InvalidOperationException ex)
        {
            return Failure(ResultErrorCodes.GenericError, ("Operación", ex.Message));
        }

        await _repository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        return Success();
    }
}
