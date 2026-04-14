using Mentoory.Notification.Domain.Enums;
using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Notification.Domain.Aggregates.NotificationPreference;

public class NotificationPreference : Entity, IAggregateRoot
{
    private static readonly HashSet<NotificationType> SystemMandatoryTypes =
    [
        NotificationType.UserRegistration,
        NotificationType.ProjectInvitation,
    ];

    private NotificationPreference()
    {
    }

    public Guid ExternalId { get; private set; }

    public long UserId { get; private set; }

    public NotificationType NotificationType { get; private set; }

    public bool IsEnabled { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static bool IsSystemMandatory(NotificationType type) => SystemMandatoryTypes.Contains(type);

    public static NotificationPreference Create(long userId, NotificationType notificationType, bool isEnabled, DateTime utcNow)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId), "User ID must be positive.");
        }

        if (!isEnabled && SystemMandatoryTypes.Contains(notificationType))
        {
            throw new InvalidOperationException(
                $"Cannot disable system-mandatory notification type: {notificationType}.");
        }

        return new NotificationPreference
        {
            ExternalId = Guid.NewGuid(),
            UserId = userId,
            NotificationType = notificationType,
            IsEnabled = isEnabled,
            UpdatedAtUtc = utcNow,
        };
    }

    public void UpdateEnabled(bool isEnabled, DateTime utcNow)
    {
        if (!isEnabled && SystemMandatoryTypes.Contains(NotificationType))
        {
            throw new InvalidOperationException(
                $"Cannot disable system-mandatory notification type: {NotificationType}.");
        }

        IsEnabled = isEnabled;
        UpdatedAtUtc = utcNow;
    }
}
