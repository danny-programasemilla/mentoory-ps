using Mentoory.Shared.Application.IntegrationEvents;

namespace Mentoory.Identity.Application.IntegrationEvents;

/// <summary>
/// Integration event raised when a new user registers in the system.
/// </summary>
/// <param name="UserId">The internal ID of the registered user.</param>
/// <param name="UserExternalId">The external GUID identifier of the registered user.</param>
/// <param name="Email">The email address of the registered user.</param>
/// <param name="OccurredOnUtc">The UTC timestamp when the event occurred.</param>
public sealed record UserRegisteredEvent(
    long UserId,
    Guid UserExternalId,
    string Email,
    DateTime OccurredOnUtc) : IntegrationEvent(OccurredOnUtc);
