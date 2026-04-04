using Mentoory.Shared.Application.IntegrationEvents;

namespace Mentoory.Access.Application.IntegrationEvents;

/// <summary>
/// Integration event published when a user account is locked out.
/// </summary>
/// <param name="UserId">The internal identifier of the user.</param>
/// <param name="UserExternalId">The external identifier of the user.</param>
/// <param name="Email">The email address of the locked-out user.</param>
/// <param name="OccurredOnUtc">The UTC timestamp when the lockout occurred.</param>
public sealed record UserLockedOutEvent(
    long UserId,
    Guid UserExternalId,
    string Email,
    DateTime OccurredOnUtc) : IntegrationEvent(OccurredOnUtc);
