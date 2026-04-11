using Mentoory.Access.Domain.ReadModels;
using Mentoory.Access.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Microsoft.Extensions.Logging;

namespace Mentoory.Access.Application.Commands.SetActiveContext;

/// <summary>
/// Handler for the SetActiveContextCommand that validates a role assignment belongs
/// to the user and is active, then returns the UserContext read model.
/// </summary>
/// <param name="logger">Logger instance for tracking operations.</param>
/// <param name="repository">The repository for accessing role assignments.</param>
public partial class SetActiveContextHandler(
    ILogger<SetActiveContextHandler> logger,
    IRoleAssignmentRepository repository)
    : BaseCommandHandler<SetActiveContextCommand, UserContext>
{
    /// <summary>
    /// Handles the command to set the active context for a user.
    /// </summary>
    /// <param name="request">The command containing user and role assignment identifiers.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the UserContext or a failure result.</returns>
    public override async Task<Result<UserContext>> Handle(
        SetActiveContextCommand request,
        CancellationToken cancellationToken)
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

        if (roleAssignment.UserId != request.UserId)
        {
            return Failure(
                ResultErrorCodes.GenericError,
                (nameof(request.RoleAssignmentExternalId), "Role assignment does not belong to the specified user."));
        }

        if (!roleAssignment.IsActive)
        {
            return Failure(
                ResultErrorCodes.GenericError,
                (nameof(request.RoleAssignmentExternalId), "Role assignment is not active."));
        }

        var userContext = new UserContext(
            RoleAssignmentExternalId: roleAssignment.ExternalId,
            UserId: roleAssignment.UserId,
            IncubatorId: roleAssignment.IncubatorId,
            IncubatorName: null,
            ProjectId: roleAssignment.ProjectId,
            ProjectName: null,
            Role: roleAssignment.Role);

        LogActiveContextSet(request.UserId, roleAssignment.Role, roleAssignment.IncubatorId);

        return Success(userContext);
    }

    /// <summary>
    /// Logs when an active context is successfully set.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Active context set. UserId: {UserId}, Role: {Role}, IncubatorId: {IncubatorId}")]
    partial void LogActiveContextSet(long userId, string role, long incubatorId);
}
