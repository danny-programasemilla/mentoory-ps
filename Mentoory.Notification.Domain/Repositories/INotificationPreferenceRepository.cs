using Mentoory.Notification.Domain.Enums;
using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Notification.Domain.Repositories;

public interface INotificationPreferenceRepository : IRepository<Aggregates.NotificationPreference.NotificationPreference>
{
    Aggregates.NotificationPreference.NotificationPreference Add(Aggregates.NotificationPreference.NotificationPreference preference);

    void Update(Aggregates.NotificationPreference.NotificationPreference preference);

    Task<Aggregates.NotificationPreference.NotificationPreference?> GetByUserAndTypeAsync(
        long userId,
        NotificationType type,
        CancellationToken cancellationToken);

    Task<List<Aggregates.NotificationPreference.NotificationPreference>> GetByUserAsync(
        long userId,
        CancellationToken cancellationToken);
}
