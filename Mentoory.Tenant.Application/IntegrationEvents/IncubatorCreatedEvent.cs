using Mentoory.Shared.Application.IntegrationEvents;

namespace Mentoory.Tenant.Application.IntegrationEvents;

/// <summary>
/// Integration event raised when a new incubator is created.
/// </summary>
/// <param name="IncubatorExternalId">The external identifier of the created incubator.</param>
/// <param name="Name">The name of the created incubator.</param>
/// <param name="OccurredOn">The time when this event occurred.</param>
public sealed record IncubatorCreatedEvent(
    Guid IncubatorExternalId,
    string Name,
    DateTime OccurredOn)
    : IntegrationEvent(OccurredOn);
