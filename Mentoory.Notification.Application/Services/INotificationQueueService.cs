using Mentoory.Notification.Domain.Enums;

namespace Mentoory.Notification.Application.Services;

public interface INotificationQueueService
{
    Task QueueAsync(
        NotificationType notificationType,
        string subject,
        string htmlBody,
        long recipientUserId,
        string recipientEmail,
        Guid? sourceEventId,
        DateTime scheduledForUtc,
        CancellationToken cancellationToken);

    Task ProcessPendingAsync(CancellationToken cancellationToken);
}
