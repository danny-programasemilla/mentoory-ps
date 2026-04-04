using Mentoory.Access.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Microsoft.Extensions.Logging;

namespace Mentoory.Access.Application.Commands.RevokeRole;

/// <summary>
/// Handler for the RevokeRoleCommand that revokes an existing role assignment.
/// </summary>
/// <param name="logger">Logger instance for tracking operations.</param>
/// <param name="repository">The repository for accessing role assignments.</param>
/// <param name="timeProvider">The time provider for obtaining the current UTC time.</param>
public partial class RevokeRoleHandler(
    ILogger<RevokeRoleHandler> logger,
    IRoleAssignmentRepository repository,
    ITimeProvider timeProvider)
    : BaseCommandHandler<RevokeRoleCommand>
{
    /// <summary>
    /// Handles the command to revoke a role assignment.
    /// </summary>
    /// <param name="request">The command containing the role assignment external identifier.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the command result.</returns>
    public override async Task<Result> Handle(RevokeRoleCommand request, CancellationToken cancellationToken)
    {
        var roleAssignment = await repository.GetByExternalIdAsync(
            request.RoleAssignmentExternalId,
            cancellationToken);

        if (roleAssignment is null)
        {
            return Failure(
                ResultErrorCodes.GenericError,
                (nameof(request.RoleAssignmentExternalId), "Role assignment not found."));
        }

        var utcNow = timeProvider.UtcNow;

        roleAssignment.Revoke(utcNow);

        repository.Update(roleAssignment);

        await repository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        LogRoleRevoked(request.RoleAssignmentExternalId, roleAssignment.UserId);

        return Success();
    }

    /// <summary>
    /// Logs when a role is successfully revoked.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Role revoked. ExternalId: {ExternalId}, UserId: {UserId}")]
    partial void LogRoleRevoked(Guid externalId, long userId);
}
