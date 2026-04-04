using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Commands.UnlockAccount;

/// <summary>
/// Represents a command to unlock a user account.
/// </summary>
/// <param name="UserId">The internal ID of the user to unlock.</param>
public sealed record UnlockAccountCommand(long UserId) : IBaseRequest;
