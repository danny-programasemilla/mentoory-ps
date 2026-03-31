using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Identity.Application.Commands.RequestPasswordReset;

/// <summary>
/// Represents a command to request a password reset for a user account.
/// </summary>
/// <param name="Email">The email address of the user requesting the password reset.</param>
public sealed record RequestPasswordResetCommand(string Email) : IBaseRequest;
