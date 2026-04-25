using Mentoory.Knowledge.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using KS = Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructure.KnowledgeStructure;

namespace Mentoory.Knowledge.Application.Commands.AddResource;

/// <summary>
/// Handles the addition of a resource under a subject within a knowledge structure (project clone).
/// </summary>
public class AddResourceHandler : BaseCommandHandler<AddResourceCommand, Guid>
{
    private readonly IKnowledgeStructureRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddResourceHandler"/> class.
    /// </summary>
    /// <param name="repository">The knowledge structure repository.</param>
    public AddResourceHandler(IKnowledgeStructureRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc />
    public override async Task<Result<Guid>> Handle(
        AddResourceCommand request,
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
            var resource = structure.AddResource(
                request.SubjectExternalId,
                request.Title,
                request.Description,
                request.Url,
                request.ResourceType,
                request.SortOrder);

            await _repository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
            return Success(resource.ExternalId);
        }
        catch (InvalidOperationException ex)
        {
            return Failure(ResultErrorCodes.GenericError, ("Operación", ex.Message));
        }
    }
}
