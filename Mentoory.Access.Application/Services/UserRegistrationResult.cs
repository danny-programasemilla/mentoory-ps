namespace Mentoory.Access.Application.Services;

public sealed record UserRegistrationResult(
    long UserId,
    Guid UserExternalId,
    string Email,
    string AccountStatus,
    DateTime CreatedAtUtc);
