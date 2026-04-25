using Mentoory.Shared.Application.Audit;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Commands.RegisterUser;

/// <summary>
/// Represents a command to register a new user via the public self-registration endpoint.
/// </summary>
/// <param name="Email">The email address for the new user account.</param>
/// <param name="Country">The country code for national identity verification.</param>
/// <param name="NationalId">The national identification number.</param>
/// <param name="FirstName">The user's first name.</param>
/// <param name="LastName">The user's last name.</param>
/// <param name="Password">The plaintext password to hash and store.</param>
/// <param name="CorrelationId">HTTP trace identifier; logged alongside the public-registration outcome. Never surfaced in any response.</param>
/// <param name="ClientIpAddress">Remote IP string; logged alongside the public-registration outcome. Never surfaced in any response.</param>
[Audited(AuditEventTypes.UserRegistered, EntityType = "User")]
public sealed record RegisterUserCommand(
    string Email,
    string Country,
    string NationalId,
    string FirstName,
    string LastName,
    string Password,
    string? CorrelationId = null,
    string? ClientIpAddress = null) : IBaseRequest;
