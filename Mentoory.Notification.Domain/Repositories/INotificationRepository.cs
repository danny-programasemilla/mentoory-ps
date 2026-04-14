using Mentoory.Notification.Domain.Enums;
using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Notification.Domain.Repositories;

public interface INotificationRepository : IRepository<Aggregates.Notification.Notification>
{
    Aggregates.Notification.Notification Add(Aggregates.Notification.Notification notification);

    Task<List<Aggregates.Notification.Notification>> GetPendingAsync(DateTime utcNow, CancellationToken cancellationToken);

    Task<Aggregates.Notification.Notification?> GetBySourceEventIdAndTypeAsync(
        Guid sourceEventId,
        NotificationType type,
        CancellationToken cancellationToken);
}
