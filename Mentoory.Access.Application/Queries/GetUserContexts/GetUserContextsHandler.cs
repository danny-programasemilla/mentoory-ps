using Mentoory.Access.Domain.ReadModels;
using Mentoory.Access.Domain.Repositories;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.MediatR;

namespace Mentoory.Access.Application.Queries.GetUserContexts;

/// <summary>
/// Handler for the GetUserContextsQuery that retrieves all active role assignment
/// contexts for a specified user.
/// </summary>
/// <param name="repository">The repository for accessing role assignments.</param>
public class GetUserContextsHandler(IRoleAssignmentRepository repository)
    : BaseCommandHandler<GetUserContextsQuery, List<UserContext>>
{
    /// <summary>
    /// Handles the query to retrieve all active user contexts.
    /// </summary>
    /// <param name="request">The query containing the user identifier.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the list of user contexts.</returns>
    public override async Task<Result<List<UserContext>>> Handle(
        GetUserContextsQuery request,
        CancellationToken cancellationToken)
    {
        var roleAssignments = await repository.GetActiveByUserIdAsync(
            request.UserId,
            cancellationToken);

        var contexts = roleAssignments
            .Select(ra => new UserContext(
                RoleAssignmentExternalId: ra.ExternalId,
                UserId: ra.UserId,
                IncubatorId: ra.IncubatorId,
                IncubatorName: null,
                ProjectId: ra.ProjectId,
                ProjectName: null,
                Role: ra.Role))
            .ToList();

        return Success(contexts);
    }
}
