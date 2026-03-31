using Mentoory.Tenant.Domain.Aggregates.Incubator;
using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Tenant.Domain.Repositories;

public interface IIncubatorRepository : IRepository<Incubator>
{
    Incubator Add(Incubator incubator);
    void Update(Incubator incubator);
    Task<Incubator?> GetByIdAsync(long id, CancellationToken cancellationToken);
    Task<Incubator?> GetByExternalIdAsync(Guid externalId, CancellationToken cancellationToken);
    IQueryable<Incubator> Query();
    Task<int> CountAsync(CancellationToken cancellationToken);
    Task<int> CountAsync(IQueryable<Incubator> query, CancellationToken cancellationToken);
    Task<List<TResult>> ToListAsync<TResult>(IQueryable<TResult> query, CancellationToken cancellationToken);
}
