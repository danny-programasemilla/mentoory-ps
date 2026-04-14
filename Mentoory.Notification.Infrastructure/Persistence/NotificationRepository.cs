using Mentoory.Notification.Domain.Enums;
using Mentoory.Notification.Domain.Repositories;
using Mentoory.Shared.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using NotificationAggregate = Mentoory.Notification.Domain.Aggregates.Notification.Notification;

namespace Mentoory.Notification.Infrastructure.Persistence;

public class NotificationRepository : AbstractRepository<NotificationAggregate>, INotificationRepository
{
    private readonly NotificationDbContext _dbContext;

    public NotificationRepository(NotificationDbContext dbContext)
        : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public new NotificationAggregate Add(NotificationAggregate notification)
    {
        return _dbContext.Notifications.Add(notification).Entity;
    }

    public Task<List<NotificationAggregate>> GetPendingAsync(DateTime utcNow, CancellationToken cancellationToken)
    {
        return _dbContext.Notifications
            .Include(n => n.Recipients)
                .ThenInclude(r => r.DeliveryAttempts)
            .Where(n => n.ScheduledForUtc <= utcNow)
            .Where(n => n.Recipients.Any(r => r.DeliveryStatus == DeliveryStatus.Pending))
            .OrderBy(n => n.ScheduledForUtc)
            .Take(50)
            .ToListAsync(cancellationToken);
    }

    public Task<NotificationAggregate?> GetBySourceEventIdAndTypeAsync(
        Guid sourceEventId,
        NotificationType type,
        CancellationToken cancellationToken)
    {
        return _dbContext.Notifications
            .AsNoTracking()
            .FirstOrDefaultAsync(
                n => n.SourceEventId == sourceEventId && n.NotificationType == type,
                cancellationToken);
    }
}
