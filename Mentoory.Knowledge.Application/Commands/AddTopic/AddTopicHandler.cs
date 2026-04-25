using Mentoory.Knowledge.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using KS = Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructure.KnowledgeStructure;

namespace Mentoory.Knowledge.Application.Commands.AddTopic;

/// <summary>
/// Handles the addition of a topic to a module within a knowledge structure (project clone).
/// </summary>
public class AddTopicHandler : BaseCommandHandler<AddTopicCommand, Guid>
{
    private readonly IKnowledgeStructureRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddTopicHandler"/> class.
    /// </summary>
    /// <param name="repository">The knowledge structure repository.</param>
    public AddTopicHandler(IKnowledgeStructureRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc />
    public override async Task<Result<Guid>> Handle(
        AddTopicCommand request,
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
            var topic = structure.AddTopic(
                request.ModuleExternalId,
                request.Name,
                request.Description,
                request.SortOrder);

            await _repository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
            return Success(topic.ExternalId);
        }
        catch (InvalidOperationException ex)
        {
            return Failure(ResultErrorCodes.GenericError, ("Operación", ex.Message));
        }
    }
}
