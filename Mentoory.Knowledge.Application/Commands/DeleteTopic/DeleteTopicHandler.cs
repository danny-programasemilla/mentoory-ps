using Mentoory.Knowledge.Application.Abstractions;
using Mentoory.Knowledge.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using KS = Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructure.KnowledgeStructure;

namespace Mentoory.Knowledge.Application.Commands.DeleteTopic;

/// <summary>
/// Handles the removal of a topic from a knowledge structure (project clone). Enforces EC-30:
/// rejects deletion when the topic is referenced by any diagnostic question.
/// </summary>
public class DeleteTopicHandler : BaseCommandHandler<DeleteTopicCommand>
{
    private readonly IKnowledgeStructureRepository _repository;
    private readonly ITopicUsageQuery _topicUsageQuery;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteTopicHandler"/> class.
    /// </summary>
    /// <param name="repository">The knowledge structure repository.</param>
    /// <param name="topicUsageQuery">The cross-module query reporting diagnostic question usage of a topic.</param>
    public DeleteTopicHandler(
        IKnowledgeStructureRepository repository,
        ITopicUsageQuery topicUsageQuery)
    {
        _repository = repository;
        _topicUsageQuery = topicUsageQuery;
    }

    /// <inheritdoc />
    public override async Task<Result> Handle(
        DeleteTopicCommand request,
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

        var topic = structure.Modules
            .SelectMany(m => m.Topics)
            .FirstOrDefault(t => t.ExternalId == request.TopicExternalId);

        if (topic is null)
        {
            return Failure(ResultErrorCodes.GenericError,
                ("Tema", "El tema no fue encontrado."));
        }

        var questionCount = await _topicUsageQuery.CountQuestionsReferencingTopicAsync(topic.Id, cancellationToken);
        if (questionCount > 0)
        {
            return Failure(ResultErrorCodes.GenericError,
                ("Tema", $"No se puede eliminar: el tema está referenciado por {questionCount} pregunta(s) de diagnóstico."));
        }

        try
        {
            structure.RemoveTopic(request.TopicExternalId);
        }
        catch (InvalidOperationException ex)
        {
            return Failure(ResultErrorCodes.GenericError, ("Operación", ex.Message));
        }

        await _repository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        return Success();
    }
}
