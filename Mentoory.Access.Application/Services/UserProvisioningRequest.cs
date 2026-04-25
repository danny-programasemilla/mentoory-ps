namespace Mentoory.Access.Application.Services;

public sealed record UserProvisioningRequest(
    string Email,
    string Country,
    string NationalId,
    string FirstName,
    string LastName,
    string Password);
