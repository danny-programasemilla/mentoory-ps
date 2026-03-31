using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Identity.Application.Commands.RegisterUser;

/// <summary>
/// Represents a command to register a new user in the identity system.
/// </summary>
/// <param name="Email">The email address for the new user account.</param>
/// <param name="Country">The country code for national identity verification.</param>
/// <param name="NationalId">The national identification number.</param>
/// <param name="FirstName">The user's first name.</param>
/// <param name="LastName">The user's last name.</param>
/// <param name="Password">The plaintext password to hash and store.</param>
public sealed record RegisterUserCommand(
    string Email,
    string Country,
    string NationalId,
    string FirstName,
    string LastName,
    string Password) : IBaseRequest;
