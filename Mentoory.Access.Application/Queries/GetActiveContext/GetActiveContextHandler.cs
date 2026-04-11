using Mentoory.Access.Domain.ReadModels;
using Mentoory.Access.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Queries.GetActiveContext;

/// <summary>
/// Handler for the GetActiveContextQuery that retrieves a single active role
/// assignment context for a user.
/// </summary>
/// <param name="repository">The repository for accessing role assignments.</param>
public class GetActiveContextHandler(IRoleAssignmentRepository repository)
    : BaseCommandHandler<GetActiveContextQuery, UserContext>
{
    /// <summary>
    /// Handles the query to retrieve a single active user context.
    /// </summary>
    /// <param name="request">The query containing the user and role assignment identifiers.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the user context or a failure result.</returns>
    public override async Task<Result<UserContext>> Handle(
        GetActiveContextQuery request,
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

        var context = new UserContext(
            RoleAssignmentExternalId: roleAssignment.ExternalId,
            UserId: roleAssignment.UserId,
            IncubatorId: roleAssignment.IncubatorId,
            IncubatorName: null,
            ProjectId: roleAssignment.ProjectId,
            ProjectName: null,
            Role: roleAssignment.Role);

        return Success(context);
    }
}
