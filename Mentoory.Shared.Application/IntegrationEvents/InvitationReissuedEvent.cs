using Mentoory.Shared.Application.IntegrationEvents;

namespace Mentoory.Shared.Application.IntegrationEvents;

/// <summary>
/// Integration event published when an invitation is reissued.
/// The Access domain consumes this to generate a fresh EmailVerificationToken.
/// </summary>
/// <param name="UserId">The internal identifier of the invited user.</param>
/// <param name="InvitationExpiryHours">The expiry duration for the new verification token.</param>
/// <param name="OccurredOnUtc">The UTC timestamp when the reissue occurred.</param>
public sealed record InvitationReissuedEvent(
    long UserId,
    int InvitationExpiryHours,
    DateTime OccurredOnUtc) : IntegrationEvent(OccurredOnUtc);
