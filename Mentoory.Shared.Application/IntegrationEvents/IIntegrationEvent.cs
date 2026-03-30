using MediatR;

namespace Mentoory.Shared.Application.IntegrationEvents;

/// <summary>
/// Marker interface for integration events that can be published across modules or services.
/// Integration events represent something that has already happened in the domain.
/// </summary>
public interface IIntegrationEvent : INotification
{
    /// <summary>
    /// Gets unique identifier for this integration event.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Gets the time when this event occurred.
    /// </summary>
    DateTime OccurredOn { get; }
}
