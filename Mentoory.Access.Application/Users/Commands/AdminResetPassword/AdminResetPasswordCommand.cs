using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Users.Commands.AdminResetPassword;

/// <summary>
/// Command to administratively reset a user's password.
/// Generates a temporary password that the user must change on next login.
/// </summary>
/// <param name="UserExternalId">The external GUID identifier of the user.</param>
public sealed record AdminResetPasswordCommand(Guid UserExternalId) : IBaseRequest<AdminResetPasswordResult>;

/// <summary>
/// Result of an administrative password reset operation.
/// </summary>
/// <param name="TemporaryPassword">The generated temporary password in plaintext.</param>
public sealed record AdminResetPasswordResult(string TemporaryPassword);
