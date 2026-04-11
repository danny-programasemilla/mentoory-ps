using Mentoory.Shared.Application.IntegrationEvents;

namespace Mentoory.Access.Application.IntegrationEvents;

/// <summary>
/// Integration event published when a user's email is successfully verified.
/// </summary>
/// <param name="UserId">The internal identifier of the user.</param>
/// <param name="UserExternalId">The external identifier of the user.</param>
/// <param name="Email">The verified email address.</param>
/// <param name="OccurredOnUtc">The UTC timestamp when the verification occurred.</param>
public sealed record UserEmailVerifiedEvent(
    long UserId,
    Guid UserExternalId,
    string Email,
    DateTime OccurredOnUtc) : IntegrationEvent(OccurredOnUtc);
