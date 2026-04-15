namespace Mentoory.Access.Application.Services;

public sealed record UserRegistrationRequest(
    string Email,
    string Country,
    string NationalId,
    string FirstName,
    string LastName,
    string Password,
    Guid? ProjectExternalId,
    EmailVerificationMode EmailVerificationMode,
    string EnrollmentVariant,
    bool RequirePasswordReset);
