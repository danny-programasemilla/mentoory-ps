using Mentoory.Access.Domain.Enums;
using Mentoory.Access.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;
using Mentoory.Shared.Domain.Constants;

namespace Mentoory.Access.Application.Queries.CheckPermission;

/// <summary>
/// Handler for the CheckPermissionQuery that verifies if a user has the specified
/// permission based on their active role assignment.
/// </summary>
/// <param name="repository">The repository for accessing role assignments.</param>
public class CheckPermissionHandler(IRoleAssignmentRepository repository)
    : BaseCommandHandler<CheckPermissionQuery, bool>
{
    /// <summary>
    /// Role-to-permission mapping defining which permissions each role has.
    /// </summary>
    private static readonly Dictionary<string, HashSet<Permission>> RolePermissions = new()
    {
        [Roles.GlobalAdmin] = new HashSet<Permission>(Enum.GetValues<Permission>()),
        [Roles.IncubatorAdmin] = new(
        [
            Permission.ManageProjects,
            Permission.ManageIncubatorUsers,
            Permission.EnrollParticipants,
            Permission.ManageDiagnostics,
            Permission.ManageKnowledge,
            Permission.ManageLifecycle,
            Permission.ManageProjectParticipants,
            Permission.ViewProjectProgress,
        ]),
        [Roles.ProjectCoordinator] = new(
        [
            Permission.ManageDiagnostics,
            Permission.ManageKnowledge,
            Permission.ManageLifecycle,
            Permission.ManageProjectParticipants,
            Permission.ViewProjectProgress,
        ]),
        [Roles.Mentor] = new(
        [
            Permission.ManageMentoringPlans,
            Permission.ManageSessions,
            Permission.ManageAssignments,
            Permission.CorrectAnswers,
            Permission.ViewProjectProgress,
        ]),
        [Roles.Entrepreneur] = new(
        [
            Permission.CompleteDiagnostic,
            Permission.ViewMentoringPlan,
            Permission.SubmitAssignments,
        ]),
        [Roles.Sponsor] = new(
        [
            Permission.ViewProjectProgress,
        ]),
    };

    /// <summary>
    /// Handles the query to check if a user has a specific permission.
    /// </summary>
    /// <param name="request">The query containing the permission check parameters.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result indicates whether the user has the permission.</returns>
    public override async Task<Result<bool>> Handle(
        CheckPermissionQuery request,
        CancellationToken cancellationToken)
    {
        // Verify the user has an active role assignment matching the context
        var roleAssignment = await repository.GetActiveAssignmentAsync(
            request.UserId,
            request.IncubatorId,
            request.ProjectId,
            request.Role,
            cancellationToken);

        if (roleAssignment is null)
        {
            return Success(false);
        }

        // Check if the role has the requested permission
        if (!Enum.TryParse<Permission>(request.Permission, out var permission))
        {
            return Success(false);
        }

        var hasPermission = RolePermissions.TryGetValue(request.Role, out var permissions)
                            && permissions.Contains(permission);

        return Success(hasPermission);
    }
}
