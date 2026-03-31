using Mentoory.Authorization.Domain.Aggregates.RoleAssignment;
using Mentoory.Authorization.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Shared.Domain.Constants;
using Microsoft.Extensions.Logging;

namespace Mentoory.Authorization.Application.Commands.AssignRole;

/// <summary>
/// Handler for the AssignRoleCommand that creates a new role assignment.
/// Validates the entrepreneur single-active-assignment limit per incubator.
/// </summary>
/// <param name="logger">Logger instance for tracking operations.</param>
/// <param name="repository">The repository for persisting role assignments.</param>
/// <param name="timeProvider">The time provider for obtaining the current UTC time.</param>
public partial class AssignRoleHandler(
    ILogger<AssignRoleHandler> logger,
    IRoleAssignmentRepository repository,
    ITimeProvider timeProvider)
    : BaseCommandHandler<AssignRoleCommand>
{
    /// <summary>
    /// Handles the command to assign a role to a user.
    /// </summary>
    /// <param name="request">The command containing the role assignment data.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the command result.</returns>
    public override async Task<Result> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        // Check for duplicate active assignment
        var existing = await repository.GetActiveAssignmentAsync(
            request.UserId,
            request.IncubatorId,
            request.ProjectId,
            request.Role,
            cancellationToken);

        if (existing is not null)
        {
            return Failure(
                ResultErrorCodes.GenericError,
                (nameof(request.Role), "User already has this active role assignment."));
        }

        // Validate entrepreneur limit: max 1 active entrepreneur role per incubator
        if (request.Role == Roles.Entrepreneur)
        {
            var activeCount = await repository.CountActiveEntrepreneurAssignmentsAsync(
                request.UserId,
                request.IncubatorId,
                cancellationToken);

            if (activeCount >= 1)
            {
                return Failure(
                    ResultErrorCodes.GenericError,
                    (nameof(request.Role), "User already has an active entrepreneur role in this incubator."));
            }
        }

        var utcNow = timeProvider.UtcNow;

        var roleAssignment = RoleAssignment.Create(
            request.UserId,
            request.IncubatorId,
            request.ProjectId,
            request.Role,
            utcNow);

        repository.Add(roleAssignment);

        await repository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        LogRoleAssigned(request.UserId, request.Role, request.IncubatorId);

        return Success();
    }

    /// <summary>
    /// Logs when a role is successfully assigned.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "Role assigned. UserId: {UserId}, Role: {Role}, IncubatorId: {IncubatorId}")]
    partial void LogRoleAssigned(long userId, string role, long incubatorId);
}
