using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Identity.Application.Commands.ResetPassword;

/// <summary>
/// Represents a command to reset a user's password using a reset token.
/// </summary>
/// <param name="TokenHash">The hashed password reset token.</param>
/// <param name="NewPassword">The new plaintext password to set.</param>
public sealed record ResetPasswordCommand(string TokenHash, string NewPassword) : IBaseRequest;
