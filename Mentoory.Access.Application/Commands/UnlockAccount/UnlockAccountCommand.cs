using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Commands.UnlockAccount;

/// <summary>
/// Represents a command to unlock a user account.
/// </summary>
/// <param name="UserExternalId">The external GUID identifier of the user to unlock.</param>
public sealed record UnlockAccountCommand(Guid UserExternalId) : IBaseRequest;
