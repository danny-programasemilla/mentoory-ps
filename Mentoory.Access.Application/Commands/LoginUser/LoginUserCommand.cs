using Mentoory.Access.Domain.Aggregates.AuthSession;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Commands.LoginUser;

/// <summary>
/// Represents a command to authenticate a user and create a session.
/// </summary>
/// <param name="Email">The user's email address.</param>
/// <param name="Password">The plaintext password to verify.</param>
/// <param name="IpAddress">The IP address of the login request.</param>
/// <param name="UserAgent">The user agent string of the client browser.</param>
public sealed record LoginUserCommand(
    string Email,
    string Password,
    string IpAddress,
    string? UserAgent) : IBaseRequest<AuthSession>;
