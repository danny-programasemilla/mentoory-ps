namespace Mentoory.Shared.Application.IntegrationEvents;

/// <summary>
/// Base class for integration events.
/// </summary>
public abstract record IntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IntegrationEvent"/> class.
    /// </summary>
    /// <param name="occurredOn">The time when this event occurred.</param>
    protected IntegrationEvent(DateTime occurredOn)
    {
        EventId = Guid.NewGuid();
        OccurredOn = occurredOn;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IntegrationEvent"/> class with a specific event ID and occurrence time.
    /// </summary>
    /// <param name="eventId">The unique identifier for this event.</param>
    /// <param name="occurredOn">The time when this event occurred.</param>
    protected IntegrationEvent(Guid eventId, DateTime occurredOn)
    {
        EventId = eventId;
        OccurredOn = occurredOn;
    }

    /// <inheritdoc />
    public Guid EventId { get; init; }

    /// <inheritdoc />
    public DateTime OccurredOn { get; init; }
}
