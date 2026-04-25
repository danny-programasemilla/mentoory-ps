using Mentoory.Knowledge.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using KS = Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructure.KnowledgeStructure;

namespace Mentoory.Knowledge.Application.Commands.AddModule;

/// <summary>
/// Handles the addition of a module to a knowledge structure (project clone).
/// </summary>
public class AddModuleHandler : BaseCommandHandler<AddModuleCommand, Guid>
{
    private readonly IKnowledgeStructureRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddModuleHandler"/> class.
    /// </summary>
    /// <param name="repository">The knowledge structure repository.</param>
    public AddModuleHandler(IKnowledgeStructureRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc />
    public override async Task<Result<Guid>> Handle(
        AddModuleCommand request,
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
            var module = structure.AddModule(request.Name, request.Description, request.SortOrder);
            await _repository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
            return Success(module.ExternalId);
        }
        catch (InvalidOperationException ex)
        {
            return Failure(ResultErrorCodes.GenericError, ("Operación", ex.Message));
        }
    }
}
