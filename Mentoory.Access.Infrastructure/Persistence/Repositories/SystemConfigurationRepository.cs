using Mentoory.Access.Domain.Aggregates.SystemConfiguration;
using Mentoory.Access.Domain.Repositories;
using Mentoory.Shared.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Mentoory.Access.Infrastructure.Persistence.Repositories;

public class SystemConfigurationRepository : AbstractRepository<SystemConfiguration>, ISystemConfigurationRepository
{
    private readonly AccessDbContext _dbContext;

    public SystemConfigurationRepository(AccessDbContext dbContext)
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
