using Mentoory.Notification.Domain.Aggregates.NotificationConfiguration;
using Mentoory.Notification.Domain.Repositories;
using Mentoory.Shared.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Mentoory.Notification.Infrastructure.Persistence;

public class NotificationConfigurationRepository : AbstractRepository<NotificationConfiguration>, INotificationConfigurationRepository
{
    private readonly NotificationDbContext _dbContext;

    public NotificationConfigurationRepository(NotificationDbContext dbContext)
        : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<NotificationConfiguration?> GetByKeyAsync(string key, CancellationToken cancellationToken)
    {
        return _dbContext.NotificationConfigurations
            .FirstOrDefaultAsync(c => c.Key == key, cancellationToken);
    }

    public Task<List<NotificationConfiguration>> GetAllAsync(CancellationToken cancellationToken)
    {
        return _dbContext.NotificationConfigurations
            .AsNoTracking()
            .OrderBy(c => c.Key)
            .ToListAsync(cancellationToken);
    }

    public new void Update(NotificationConfiguration configuration)
    {
        _dbContext.Entry(configuration).State = EntityState.Modified;
    }
}
