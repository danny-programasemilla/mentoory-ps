using MediatR;

namespace Mentoory.Knowledge.Application.IntegrationEvents;

/// <summary>
/// Integration event raised when a project-clone topic's priority ranges are updated.
/// Template-side priority-range edits do NOT emit this event.
/// Consumers (e.g. future Mentoring Plan module) subscribe via MediatR's <see cref="INotification"/>.
/// </summary>
public sealed record TopicPriorityRangesChanged(
    Guid TopicExternalId,
    long ProjectId,
    PriorityRangeDto? HighRange,
    PriorityRangeDto? MediumRange,
    PriorityRangeDto? LowRange) : INotification;

public sealed record PriorityRangeDto(decimal Min, decimal Max);
