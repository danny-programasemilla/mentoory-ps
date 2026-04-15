using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Notification.Domain.Repositories;

public interface INotificationConfigurationRepository : IRepository<Aggregates.NotificationConfiguration.NotificationConfiguration>
{
    Task<Aggregates.NotificationConfiguration.NotificationConfiguration?> GetByKeyAsync(string key, CancellationToken cancellationToken);
    Task<List<Aggregates.NotificationConfiguration.NotificationConfiguration>> GetAllAsync(CancellationToken cancellationToken);
    void Update(Aggregates.NotificationConfiguration.NotificationConfiguration configuration);
}
