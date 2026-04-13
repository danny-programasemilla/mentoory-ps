using Mentoory.Tenant.Domain.Aggregates.SystemConfiguration;
using Mentoory.Tenant.Domain.Repositories;
using Mentoory.Shared.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Mentoory.Tenant.Infrastructure.Persistence.Repositories;

public class SystemConfigurationRepository : AbstractRepository<SystemConfiguration>, ISystemConfigurationRepository
{
    private readonly TenantDbContext _dbContext;

    public SystemConfigurationRepository(TenantDbContext dbContext)
        : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<SystemConfiguration?> GetByKeyAsync(string key, CancellationToken cancellationToken)
    {
        return _dbContext.SystemConfigurations
            .FirstOrDefaultAsync(c => c.Key == key, cancellationToken);
    }

    public Task<List<SystemConfiguration>> GetAllAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SystemConfigurations
            .AsNoTracking()
            .OrderBy(c => c.Key)
            .ToListAsync(cancellationToken);
    }

    public new void Update(SystemConfiguration configuration)
    {
        _dbContext.Entry(configuration).State = EntityState.Modified;
    }
}
