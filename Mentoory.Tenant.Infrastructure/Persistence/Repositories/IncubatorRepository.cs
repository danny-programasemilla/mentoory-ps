using Mentoory.Shared.Infrastructure.Persistence.Repositories;
using Mentoory.Tenant.Domain.Aggregates.Incubator;
using Mentoory.Tenant.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Mentoory.Tenant.Infrastructure.Persistence.Repositories;

public class IncubatorRepository : AbstractRepository<Incubator>, IIncubatorRepository
{
    private readonly TenantDbContext _dbContext;

    public IncubatorRepository(TenantDbContext dbContext)
        : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public new Incubator Add(Incubator incubator)
    {
        return _dbContext.Incubators.Add(incubator).Entity;
    }

    public new void Update(Incubator incubator)
    {
        _dbContext.Entry(incubator).State = EntityState.Modified;
    }

    public Task<Incubator?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        return _dbContext.Incubators
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public Task<Incubator?> GetByExternalIdAsync(Guid externalId, CancellationToken cancellationToken)
    {
        return _dbContext.Incubators
            .FirstOrDefaultAsync(i => i.ExternalId == externalId, cancellationToken);
    }

    public IQueryable<Incubator> Query()
    {
        return _dbContext.Incubators.AsQueryable();
    }

    public Task<int> CountAsync(CancellationToken cancellationToken)
    {
        return _dbContext.Incubators.CountAsync(cancellationToken);
    }

    public Task<int> CountAsync(IQueryable<Incubator> query, CancellationToken cancellationToken)
    {
        return query.CountAsync(cancellationToken);
    }

    public Task<List<TResult>> ToListAsync<TResult>(IQueryable<TResult> query, CancellationToken cancellationToken)
    {
        return query.ToListAsync(cancellationToken);
    }
}
