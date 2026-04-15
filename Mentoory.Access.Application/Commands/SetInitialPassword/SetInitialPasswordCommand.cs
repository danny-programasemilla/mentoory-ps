using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Commands.SetInitialPassword;

/// <summary>
/// Command to set the initial password for a user during email verification.
/// </summary>
/// <param name="UserExternalId">The external identifier of the user.</param>
/// <param name="Token">The raw verification token.</param>
/// <param name="NewPassword">The new password to set.</param>
/// <param name="ConfirmPassword">Confirmation of the new password.</param>
public sealed record SetInitialPasswordCommand(
    Guid UserExternalId,
    string Token,
    string NewPassword,
    string ConfirmPassword) : IBaseRequest;
