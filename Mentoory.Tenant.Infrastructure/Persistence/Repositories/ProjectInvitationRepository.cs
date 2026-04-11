using Mentoory.Tenant.Domain.Aggregates.ProjectInvitation;
using Mentoory.Tenant.Domain.Enums;
using Mentoory.Tenant.Domain.Repositories;
using Mentoory.Shared.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Mentoory.Tenant.Infrastructure.Persistence.Repositories;

public class ProjectInvitationRepository : AbstractRepository<ProjectInvitation>, IProjectInvitationRepository
{
    private readonly TenantDbContext _dbContext;

    public ProjectInvitationRepository(TenantDbContext dbContext)
        : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public new ProjectInvitation Add(ProjectInvitation invitation)
    {
        return _dbContext.ProjectInvitations.Add(invitation).Entity;
    }

    public new void Update(ProjectInvitation invitation)
    {
        _dbContext.Entry(invitation).State = EntityState.Modified;
    }

    public Task<ProjectInvitation?> GetByExternalIdAsync(Guid externalId, CancellationToken cancellationToken)
    {
        return _dbContext.ProjectInvitations
            .FirstOrDefaultAsync(i => i.ExternalId == externalId && i.IsActive, cancellationToken);
    }

    public Task<ProjectInvitation?> GetActiveByUserAndProjectAsync(long userId, long projectId, CancellationToken cancellationToken)
    {
        return _dbContext.ProjectInvitations
            .FirstOrDefaultAsync(i => i.UserId == userId
                                      && i.ProjectId == projectId
                                      && i.IsActive
                                      && i.Status == InvitationStatus.Pending, cancellationToken);
    }

    public Task<List<ProjectInvitation>> GetByProjectAsync(long projectId, CancellationToken cancellationToken)
    {
        return _dbContext.ProjectInvitations
            .Where(i => i.ProjectId == projectId && i.IsActive)
            .OrderByDescending(i => i.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task<List<ProjectInvitation>> GetByUserAsync(long userId, CancellationToken cancellationToken)
    {
        return _dbContext.ProjectInvitations
            .Where(i => i.UserId == userId && i.IsActive)
            .OrderByDescending(i => i.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }
}
