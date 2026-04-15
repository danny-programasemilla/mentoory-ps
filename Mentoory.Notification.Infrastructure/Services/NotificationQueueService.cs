using Mentoory.Notification.Application.Services;
using Mentoory.Notification.Contracts.Configuration;
using Mentoory.Notification.Domain.Enums;
using Mentoory.Notification.Domain.Repositories;
using Mentoory.Shared.Application.TimeProvider;
using Microsoft.Extensions.Logging;
using NotificationAggregate = Mentoory.Notification.Domain.Aggregates.Notification.Notification;

namespace Mentoory.Notification.Infrastructure.Services;

public partial class NotificationQueueService : INotificationQueueService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IEmailService _emailService;
    private readonly ITimeProvider _timeProvider;
    private readonly INotificationConfigurationReader _configReader;
    private readonly ILogger<NotificationQueueService> _logger;

    public NotificationQueueService(
        INotificationRepository notificationRepository,
        IEmailService emailService,
        ITimeProvider timeProvider,
        INotificationConfigurationReader configReader,
        ILogger<NotificationQueueService> logger)
    {
        _notificationRepository = notificationRepository;
        _emailService = emailService;
        _timeProvider = timeProvider;
        _configReader = configReader;
        _logger = logger;
    }

    public async Task QueueAsync(
        NotificationType notificationType,
        string subject,
        string htmlBody,
        long recipientUserId,
        string recipientEmail,
        Guid? sourceEventId,
        DateTime scheduledForUtc,
        CancellationToken cancellationToken)
    {
        if (sourceEventId.HasValue &&
            await IsDuplicateAsync(sourceEventId.Value, notificationType, cancellationToken))
        {
            LogDuplicateSkipped(sourceEventId.Value, notificationType);
            return;
        }

        var utcNow = _timeProvider.UtcNow;
        var notification = NotificationAggregate.Create(
            notificationType,
            subject,
            htmlBody,
            sourceEventId,
            scheduledForUtc,
            utcNow);

        notification.AddRecipient(recipientUserId, recipientEmail, DeliveryChannel.Email);
        _notificationRepository.Add(notification);
        await _notificationRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        LogNotificationQueued(notificationType, recipientEmail);
    }

    public async Task ProcessPendingAsync(CancellationToken cancellationToken)
    {
        var maxRetryAttempts = await _configReader.GetIntAsync(
            nameof(NotificationConfigurationKey.MaxRetryAttempts), cancellationToken);
        var utcNow = _timeProvider.UtcNow;
        var pending = await _notificationRepository.GetPendingAsync(utcNow, cancellationToken);

        foreach (var notification in pending)
        {
            foreach (var recipient in notification.Recipients)
            {
                if (recipient.DeliveryStatus != DeliveryStatus.Pending)
                {
                    continue;
                }

                var nextRetry = recipient.GetNextRetryAtUtc();
                if (nextRetry.HasValue && utcNow < nextRetry.Value)
                {
                    continue;
                }

                try
                {
                    await _emailService.SendAsync(
                        recipient.Email,
                        notification.Subject,
                        notification.HtmlBody,
                        cancellationToken);

                    recipient.MarkSent(utcNow);
                    LogDeliverySuccess(notification.ExternalId, recipient.Email);
                }
                catch (Exception ex)
                {
                    recipient.RecordFailedAttempt(utcNow, ex.Message, maxRetryAttempts);
                    LogDeliveryFailed(notification.ExternalId, recipient.Email, ex);
                }
            }

            await _notificationRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        }
    }

    private async Task<bool> IsDuplicateAsync(
        Guid sourceEventId,
        NotificationType type,
        CancellationToken cancellationToken)
    {
        var existing = await _notificationRepository.GetBySourceEventIdAndTypeAsync(
            sourceEventId, type, cancellationToken);
        return existing is not null;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Notification queued: {Type} for {Email}")]
    partial void LogNotificationQueued(NotificationType type, string email);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Duplicate notification skipped: EventId={EventId}, Type={Type}")]
    partial void LogDuplicateSkipped(Guid eventId, NotificationType type);

    [LoggerMessage(Level = LogLevel.Information, Message = "Email delivered: NotificationId={NotificationId}, To={Email}")]
    partial void LogDeliverySuccess(Guid notificationId, string email);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Email delivery failed: NotificationId={NotificationId}, To={Email}")]
    partial void LogDeliveryFailed(Guid notificationId, string email, Exception ex);
}
