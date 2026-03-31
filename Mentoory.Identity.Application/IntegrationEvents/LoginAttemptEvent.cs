using Mentoory.Shared.Application.IntegrationEvents;

namespace Mentoory.Identity.Application.IntegrationEvents;

/// <summary>
/// Integration event raised when a login attempt occurs (successful or failed).
/// </summary>
/// <param name="UserId">The internal ID of the user (null if user not found).</param>
/// <param name="Email">The email address used in the login attempt.</param>
/// <param name="Success">Whether the login attempt was successful.</param>
/// <param name="IpAddress">The IP address of the login attempt.</param>
/// <param name="OccurredOnUtc">The UTC timestamp when the event occurred.</param>
public sealed record LoginAttemptEvent(
    long? UserId,
    string Email,
    bool Success,
    string IpAddress,
    DateTime OccurredOnUtc) : IntegrationEvent(OccurredOnUtc);
