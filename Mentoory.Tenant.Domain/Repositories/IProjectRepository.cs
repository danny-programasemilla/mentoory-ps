using Mentoory.Tenant.Domain.Aggregates.Project;
using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Tenant.Domain.Repositories;

public interface IProjectRepository : IRepository<Project>
{
    Project Add(Project project);
    void Update(Project project);
    void Detach(Project project);
    Task<Project?> GetByIdAsync(long id, CancellationToken cancellationToken);
    Task<Project?> GetByExternalIdAsync(Guid externalId, CancellationToken cancellationToken);
    Task<Project?> GetByExternalIdWithParticipantsAsync(Guid externalId, CancellationToken cancellationToken);
    Task<Project?> GetByExternalIdWithStagesAsync(Guid externalId, CancellationToken cancellationToken);
    IQueryable<Project> Query();
    IQueryable<Project> QueryUnfiltered();
    Task<int> CountAsync(CancellationToken cancellationToken);
    Task<int> CountAsync(IQueryable<Project> query, CancellationToken cancellationToken);
    Task<List<TResult>> ToListAsync<TResult>(IQueryable<TResult> query, CancellationToken cancellationToken);
}
