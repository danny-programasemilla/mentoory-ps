using Mentoory.Shared.Application.IntegrationEvents;

namespace Mentoory.Tenant.Contracts.IntegrationEvents;

public sealed record InvitationReissuedEvent(
    long UserId,
    Guid UserExternalId,
    string Email,
    string FirstName,
    string LastName,
    Guid ProjectExternalId,
    string ProjectName,
    string IncubatorName,
    int InvitationExpiryHours,
    DateTime OccurredOnUtc) : IntegrationEvent(OccurredOnUtc);
