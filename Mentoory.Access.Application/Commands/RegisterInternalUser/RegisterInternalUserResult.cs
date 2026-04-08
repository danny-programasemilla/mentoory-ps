namespace Mentoory.Access.Application.Commands.RegisterInternalUser;

public sealed record RegisterInternalUserResult(
    Guid UserExternalId,
    string EnrollmentStatus);
