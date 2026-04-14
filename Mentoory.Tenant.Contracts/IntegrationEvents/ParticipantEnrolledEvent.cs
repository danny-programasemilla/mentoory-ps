using Mentoory.Shared.Application.IntegrationEvents;

namespace Mentoory.Tenant.Contracts.IntegrationEvents;

/// <summary>
/// Integration event raised when a participant is enrolled in a project.
/// </summary>
/// <param name="ParticipantExternalId">The external identifier of the enrolled participant.</param>
/// <param name="ProjectExternalId">The external identifier of the project.</param>
/// <param name="UserId">The user identifier of the enrolled participant.</param>
/// <param name="Role">The role assigned to the participant.</param>
/// <param name="OccurredOn">The time when this event occurred.</param>
public sealed record ParticipantEnrolledEvent(
    Guid ParticipantExternalId,
    Guid ProjectExternalId,
    long UserId,
    string Role,
    DateTime OccurredOn)
    : IntegrationEvent(OccurredOn);
