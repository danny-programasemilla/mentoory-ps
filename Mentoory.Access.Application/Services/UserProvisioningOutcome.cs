namespace Mentoory.Access.Application.Services;

public enum UserProvisioningOutcome
{
    Success = 0,
    DuplicateEmail = 1,
    DuplicateNationalId = 2,
}
