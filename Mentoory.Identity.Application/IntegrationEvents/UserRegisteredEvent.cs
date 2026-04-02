using Mentoory.Shared.Application.IntegrationEvents;

namespace Mentoory.Identity.Application.IntegrationEvents;

/// <summary>
/// Integration event raised when a new user registers in the system.
/// </summary>
/// <param name="UserId">The internal ID of the registered user.</param>
/// <param name="UserExternalId">The external GUID identifier of the registered user.</param>
/// <param name="Email">The email address of the registered user.</param>
/// <param name="FirstName">The first name of the registered user.</param>
/// <param name="LastName">The last name of the registered user.</param>
/// <param name="AccountStatus">The account status of the registered user (e.g., "PendingVerification").</param>
/// <param name="CreatedAtUtc">The UTC timestamp when the user was created.</param>
/// <param name="OccurredOnUtc">The UTC timestamp when the event occurred.</param>
public sealed record UserRegisteredEvent(
    long UserId,
    Guid UserExternalId,
    string Email,
    string FirstName,
    string LastName,
    string AccountStatus,
    DateTime CreatedAtUtc,
    DateTime OccurredOnUtc) : IntegrationEvent(OccurredOnUtc);
