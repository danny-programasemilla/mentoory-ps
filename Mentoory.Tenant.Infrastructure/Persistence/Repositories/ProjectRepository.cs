using Mentoory.Shared.Infrastructure.Persistence.Repositories;
using Mentoory.Tenant.Domain.Aggregates.Project;
using Mentoory.Tenant.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Mentoory.Tenant.Infrastructure.Persistence.Repositories;

public class ProjectRepository : AbstractRepository<Project>, IProjectRepository
{
    private readonly TenantDbContext _dbContext;

    public ProjectRepository(TenantDbContext dbContext)
        : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public new Project Add(Project project)
    {
        return _dbContext.Projects.Add(project).Entity;
    }

    public new void Update(Project project)
    {
        _dbContext.Entry(project).State = EntityState.Modified;
    }

    public Task<Project?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        return _dbContext.Projects
            .Include(p => p.Stages)
            .Include(p => p.Participants)
            .Include(p => p.MentorAssignments)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public Task<Project?> GetByExternalIdAsync(Guid externalId, CancellationToken cancellationToken)
    {
        return _dbContext.Projects
            .Include(p => p.Stages)
            .Include(p => p.Participants)
            .Include(p => p.MentorAssignments)
            .FirstOrDefaultAsync(p => p.ExternalId == externalId, cancellationToken);
    }

    public Task<Project?> GetByExternalIdWithParticipantsAsync(Guid externalId, CancellationToken cancellationToken)
    {
        return _dbContext.Projects
            .Include(p => p.Participants)
            .Include(p => p.MentorAssignments)
            .FirstOrDefaultAsync(p => p.ExternalId == externalId, cancellationToken);
    }

    public IQueryable<Project> Query()
    {
        return _dbContext.Projects.AsQueryable();
    }

    public Task<int> CountAsync(CancellationToken cancellationToken)
    {
        return _dbContext.Projects.CountAsync(cancellationToken);
    }

    public Task<int> CountAsync(IQueryable<Project> query, CancellationToken cancellationToken)
    {
        return query.CountAsync(cancellationToken);
    }

    public Task<List<TResult>> ToListAsync<TResult>(IQueryable<TResult> query, CancellationToken cancellationToken)
    {
        return query.ToListAsync(cancellationToken);
    }
}
