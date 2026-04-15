using Mentoory.Shared.Application.IntegrationEvents;

namespace Mentoory.Tenant.Contracts.IntegrationEvents;

/// <summary>
/// Integration event raised when a new project is created.
/// </summary>
/// <param name="ProjectExternalId">The external identifier of the created project.</param>
/// <param name="IncubatorExternalId">The external identifier of the incubator the project belongs to.</param>
/// <param name="Name">The name of the created project.</param>
/// <param name="OccurredOn">The time when this event occurred.</param>
public sealed record ProjectCreatedEvent(
    Guid ProjectExternalId,
    Guid IncubatorExternalId,
    string Name,
    DateTime OccurredOn)
    : IntegrationEvent(OccurredOn);
