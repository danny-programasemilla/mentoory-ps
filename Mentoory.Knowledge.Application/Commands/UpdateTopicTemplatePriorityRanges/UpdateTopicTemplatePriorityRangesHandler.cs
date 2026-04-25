using Mentoory.Knowledge.Domain.Repositories;
using Mentoory.Knowledge.Domain.ValueObjects;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Commands.UpdateTopicTemplatePriorityRanges;

/// <summary>
/// Handles the update of the high/medium/low priority range bands of a topic within a knowledge structure template.
/// </summary>
public class UpdateTopicTemplatePriorityRangesHandler : BaseCommandHandler<UpdateTopicTemplatePriorityRangesCommand>
{
    private readonly IKnowledgeStructureTemplateRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateTopicTemplatePriorityRangesHandler"/> class.
    /// </summary>
    /// <param name="repository">The knowledge structure template repository.</param>
    public UpdateTopicTemplatePriorityRangesHandler(IKnowledgeStructureTemplateRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc />
    public override async Task<Result> Handle(
        UpdateTopicTemplatePriorityRangesCommand request,
        CancellationToken cancellationToken)
    {
        var template = await _repository.GetByExternalIdWithFullTreeAsync(request.TemplateExternalId, cancellationToken);
        if (template is null)
        {
            return Failure(ResultErrorCodes.GenericError, ("Plantilla", "La plantilla no fue encontrada."));
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
            template.UpdateTopicPriorityRanges(request.TopicExternalId, high, medium, low);
        }
        catch (InvalidOperationException ex)
        {
            return Failure(ResultErrorCodes.GenericError, ("Operación", ex.Message));
        }

        await _repository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        return Success();
    }
}
