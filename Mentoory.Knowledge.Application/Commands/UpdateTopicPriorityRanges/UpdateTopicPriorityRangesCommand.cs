using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Knowledge.Application.Commands.UpdateTopicPriorityRanges;

/// <summary>
/// Represents a command to update the priority range bands (high, medium, low) of a topic within a knowledge structure (project clone).
/// </summary>
/// <param name="StructureExternalId">The external identifier of the knowledge structure.</param>
/// <param name="TopicExternalId">The external identifier of the topic whose priority ranges are being updated.</param>
/// <param name="High">The inclusive [Min, Max] range for the high priority band, or null to clear it.</param>
/// <param name="Medium">The inclusive [Min, Max] range for the medium priority band, or null to clear it.</param>
/// <param name="Low">The inclusive [Min, Max] range for the low priority band, or null to clear it.</param>
public sealed record UpdateTopicPriorityRangesCommand(
    Guid StructureExternalId,
    Guid TopicExternalId,
    PriorityRangeInput? High,
    PriorityRangeInput? Medium,
    PriorityRangeInput? Low) : IBaseRequest;

/// <summary>
/// Inclusive [Min, Max] decimal range input used when configuring a topic priority band.
/// </summary>
/// <param name="Min">The inclusive lower bound of the range.</param>
/// <param name="Max">The inclusive upper bound of the range.</param>
public sealed record PriorityRangeInput(decimal Min, decimal Max);
