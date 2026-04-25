using Mentoory.Knowledge.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using KS = Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructure.KnowledgeStructure;

namespace Mentoory.Knowledge.Application.Commands.UpdateKnowledgeStructure;

/// <summary>
/// Handles updates to the details of an existing knowledge structure (project clone).
/// </summary>
public class UpdateKnowledgeStructureHandler : BaseCommandHandler<UpdateKnowledgeStructureCommand>
{
    private readonly IKnowledgeStructureRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateKnowledgeStructureHandler"/> class.
    /// </summary>
    /// <param name="repository">The knowledge structure repository.</param>
    public UpdateKnowledgeStructureHandler(IKnowledgeStructureRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc />
    public override async Task<Result> Handle(
        UpdateKnowledgeStructureCommand request,
        CancellationToken cancellationToken)
    {
        KS? structure = await _repository.GetByExternalIdAsync(request.ExternalId, cancellationToken);
        if (structure is null)
        {
            return Failure(ResultErrorCodes.GenericError,
                ("Estructura", "La estructura de conocimiento no fue encontrada."));
        }

        try
        {
            structure.UpdateDetails(request.Name, request.Description);
        }
        catch (InvalidOperationException ex)
        {
            return Failure(ResultErrorCodes.GenericError, ("Operación", ex.Message));
        }

        await _repository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        return Success();
    }
}
