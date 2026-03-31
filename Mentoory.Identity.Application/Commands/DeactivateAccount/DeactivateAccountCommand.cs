using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Identity.Application.Commands.DeactivateAccount;

/// <summary>
/// Represents a command to deactivate a user account.
/// </summary>
/// <param name="UserId">The internal ID of the user to deactivate.</param>
public sealed record DeactivateAccountCommand(long UserId) : IBaseRequest;
