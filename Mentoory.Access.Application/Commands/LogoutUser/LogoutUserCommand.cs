using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Commands.LogoutUser;

/// <summary>
/// Represents a command to log out a user by deactivating their session.
/// </summary>
/// <param name="SessionToken">The session token to invalidate.</param>
public sealed record LogoutUserCommand(string SessionToken) : IBaseRequest;
