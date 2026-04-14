using Mentoory.Notification.Domain.Enums;
using Mentoory.Notification.Domain.Repositories;
using Mentoory.Shared.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using NotificationPreferenceAggregate = Mentoory.Notification.Domain.Aggregates.NotificationPreference.NotificationPreference;

namespace Mentoory.Notification.Infrastructure.Persistence;

public class NotificationPreferenceRepository : AbstractRepository<NotificationPreferenceAggregate>, INotificationPreferenceRepository
{
    private readonly NotificationDbContext _dbContext;

    public NotificationPreferenceRepository(NotificationDbContext dbContext)
        : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public new NotificationPreferenceAggregate Add(NotificationPreferenceAggregate preference)
    {
        return _dbContext.NotificationPreferences.Add(preference).Entity;
    }

    public new void Update(NotificationPreferenceAggregate preference)
    {
        _dbContext.Entry(preference).State = EntityState.Modified;
    }

    public Task<NotificationPreferenceAggregate?> GetByUserAndTypeAsync(
        long userId,
        NotificationType type,
        CancellationToken cancellationToken)
    {
        return _dbContext.NotificationPreferences
            .FirstOrDefaultAsync(
                p => p.UserId == userId && p.NotificationType == type,
                cancellationToken);
    }

    public Task<List<NotificationPreferenceAggregate>> GetByUserAsync(
        long userId,
        CancellationToken cancellationToken)
    {
        return _dbContext.NotificationPreferences
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .ToListAsync(cancellationToken);
    }
}
