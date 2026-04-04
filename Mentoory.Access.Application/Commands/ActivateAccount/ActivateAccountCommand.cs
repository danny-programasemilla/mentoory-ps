using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Commands.ActivateAccount;

/// <summary>
/// Represents a command to activate a user account.
/// </summary>
/// <param name="UserId">The internal ID of the user to activate.</param>
public sealed record ActivateAccountCommand(long UserId) : IBaseRequest;
