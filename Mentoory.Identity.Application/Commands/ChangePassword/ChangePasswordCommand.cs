using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Identity.Application.Commands.ChangePassword;

/// <summary>
/// Represents a command to change an authenticated user's password.
/// </summary>
/// <param name="UserId">The internal ID of the user changing their password.</param>
/// <param name="CurrentPassword">The user's current plaintext password for verification.</param>
/// <param name="NewPassword">The new plaintext password to set.</param>
public sealed record ChangePasswordCommand(long UserId, string CurrentPassword, string NewPassword) : IBaseRequest;
