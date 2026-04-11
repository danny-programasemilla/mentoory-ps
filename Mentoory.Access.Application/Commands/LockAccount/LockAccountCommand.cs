using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Commands.LockAccount;

/// <summary>
/// Represents a command to lock a user account for a specified duration.
/// </summary>
/// <param name="UserId">The internal ID of the user to lock.</param>
/// <param name="Duration">The duration to lock the account.</param>
public sealed record LockAccountCommand(long UserId, TimeSpan Duration) : IBaseRequest;
