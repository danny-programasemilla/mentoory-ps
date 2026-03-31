using Mentoory.Authorization.Domain.Aggregates.RoleAssignment;
using Mentoory.Authorization.Domain.Repositories;
using Mentoory.Shared.Domain.Constants;
using Mentoory.Shared.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Mentoory.Authorization.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for managing RoleAssignment aggregate roots.
/// </summary>
/// <param name="dbContext">The database context for accessing authorization data.</param>
public class RoleAssignmentRepository(AuthorizationDbContext dbContext)
    : AbstractRepository<RoleAssignment>(dbContext), IRoleAssignmentRepository
{
    /// <summary>
    /// Retrieves a role assignment by its external identifier.
    /// </summary>
    /// <param name="externalId">The external identifier of the role assignment.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The role assignment if found; otherwise, null.</returns>
    public Task<RoleAssignment?> GetByExternalIdAsync(Guid externalId, CancellationToken cancellationToken)
    {
        return dbContext.RoleAssignments
            .FirstOrDefaultAsync(r => r.ExternalId == externalId, cancellationToken);
    }

    /// <summary>
    /// Retrieves all active role assignments for a given user.
    /// </summary>
    /// <param name="userId">The identifier of the user.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A list of active role assignments for the user.</returns>
    public Task<List<RoleAssignment>> GetActiveByUserIdAsync(long userId, CancellationToken cancellationToken)
    {
        return dbContext.RoleAssignments
            .Where(r => r.UserId == userId && r.IsActive)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Retrieves an active role assignment matching the specified criteria.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="incubatorId">The incubator identifier.</param>
    /// <param name="projectId">The optional project identifier.</param>
    /// <param name="role">The role name.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The matching active role assignment if found; otherwise, null.</returns>
    public Task<RoleAssignment?> GetActiveAssignmentAsync(
        long userId,
        long incubatorId,
        long? projectId,
        string role,
        CancellationToken cancellationToken)
    {
        return dbContext.RoleAssignments
            .FirstOrDefaultAsync(
                r => r.UserId == userId
                     && r.IncubatorId == incubatorId
                     && r.ProjectId == projectId
                     && r.Role == role
                     && r.IsActive,
                cancellationToken);
    }

    /// <summary>
    /// Counts the number of active entrepreneur role assignments for a user in an incubator.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="incubatorId">The incubator identifier.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The count of active entrepreneur assignments.</returns>
    public Task<int> CountActiveEntrepreneurAssignmentsAsync(
        long userId,
        long incubatorId,
        CancellationToken cancellationToken)
    {
        return dbContext.RoleAssignments
            .CountAsync(
                r => r.UserId == userId
                     && r.IncubatorId == incubatorId
                     && r.Role == Roles.Entrepreneur
                     && r.IsActive,
                cancellationToken);
    }
}
