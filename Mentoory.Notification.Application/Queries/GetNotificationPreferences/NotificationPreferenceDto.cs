using Mentoory.Notification.Domain.Enums;

namespace Mentoory.Notification.Application.Queries.GetNotificationPreferences;

public sealed record NotificationPreferenceDto(
    Guid ExternalId,
    NotificationType NotificationType,
    bool IsEnabled,
    DateTime UpdatedAtUtc);
