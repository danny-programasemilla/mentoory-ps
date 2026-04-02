using Mentoory.Authorization.Domain.Aggregates.RoleAssignment;
using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Authorization.Domain.Repositories;

public interface IRoleAssignmentRepository : IRepository<RoleAssignment>
{
    RoleAssignment Add(RoleAssignment roleAssignment);
    void Update(RoleAssignment roleAssignment);
    Task<RoleAssignment?> GetByExternalIdAsync(Guid externalId, CancellationToken cancellationToken);
    Task<List<RoleAssignment>> GetActiveByUserIdAsync(long userId, CancellationToken cancellationToken);
    Task<RoleAssignment?> GetActiveAssignmentAsync(long userId, long incubatorId, long? projectId, string role, CancellationToken cancellationToken);
    Task<int> CountActiveEntrepreneurAssignmentsAsync(long userId, long incubatorId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a read-only queryable for role assignments.
    /// </summary>
    IQueryable<RoleAssignment> Query();
}
