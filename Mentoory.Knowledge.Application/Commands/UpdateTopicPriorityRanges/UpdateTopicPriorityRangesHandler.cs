using MediatR;
using Mentoory.Knowledge.Application.IntegrationEvents;
using Mentoory.Knowledge.Domain.Repositories;
using Mentoory.Knowledge.Domain.ValueObjects;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using KS = Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructure.KnowledgeStructure;

namespace Mentoory.Knowledge.Application.Commands.UpdateTopicPriorityRanges;

/// <summary>
/// Handles the update of the high/medium/low priority range bands of a topic within a knowledge
/// structure (project clone). Publishes <see cref="TopicPriorityRangesChanged"/> on success so
/// downstream modules (e.g. Mentoring Plan) can react to priority-band changes.
/// </summary>
public class UpdateTopicPriorityRangesHandler : BaseCommandHandler<UpdateTopicPriorityRangesCommand>
{
    private readonly IKnowledgeStructureRepository _repository;
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateTopicPriorityRangesHandler"/> class.
    /// </summary>
    /// <param name="repository">The knowledge structure repository.</param>
    /// <param name="mediator">MediatR instance used to publish the integration event.</param>
    public UpdateTopicPriorityRangesHandler(IKnowledgeStructureRepository repository, IMediator mediator)
    {
        _repository = repository;
        _mediator = mediator;
    }

    /// <inheritdoc />
    public override async Task<Result> Handle(
        UpdateTopicPriorityRangesCommand request,
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

        PriorityRange? high;
        PriorityRange? medium;
        PriorityRange? low;

        try
        {
            high = request.High is null ? null : PriorityRange.Create(request.High.Min, request.High.Max);
            medium = request.Medium is null ? null : PriorityRange.Create(request.Medium.Min, request.Medium.Max);
            low = request.Low is null ? null : PriorityRange.Create(request.Low.Min, request.Low.Max);
        }
        catch (ArgumentException ex)
        {
            return Failure(ResultErrorCodes.GenericError, ("Rango", ex.Message));
        }

        try
        {
            structure.UpdateTopicPriorityRanges(request.TopicExternalId, high, medium, low);
        }
        catch (InvalidOperationException ex)
        {
            return Failure(ResultErrorCodes.GenericError, ("Operación", ex.Message));
        }

        await _repository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        var updatedTopic = structure.Modules
            .SelectMany(m => m.Topics)
            .First(t => t.ExternalId == request.TopicExternalId);

        var evt = new TopicPriorityRangesChanged(
            updatedTopic.ExternalId,
            structure.ProjectId,
            updatedTopic.HighRange is null ? null : new PriorityRangeDto(updatedTopic.HighRange.Min, updatedTopic.HighRange.Max),
            updatedTopic.MediumRange is null ? null : new PriorityRangeDto(updatedTopic.MediumRange.Min, updatedTopic.MediumRange.Max),
            updatedTopic.LowRange is null ? null : new PriorityRangeDto(updatedTopic.LowRange.Min, updatedTopic.LowRange.Max));

        await _mediator.Publish(evt, cancellationToken);

        return Success();
    }
}
