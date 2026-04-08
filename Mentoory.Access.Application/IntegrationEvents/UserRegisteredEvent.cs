using Mentoory.Shared.Application.IntegrationEvents;

namespace Mentoory.Access.Application.IntegrationEvents;

public sealed record UserRegisteredEvent(
    long UserId,
    Guid UserExternalId,
    string Email,
    string FirstName,
    string LastName,
    string AccountStatus,
    Guid? ProjectExternalId,
    bool RequiresVerification,
    string EnrollmentVariant,
    int InvitationExpiryHours,
    DateTime CreatedAtUtc,
    DateTime OccurredOnUtc) : IntegrationEvent(OccurredOnUtc);
