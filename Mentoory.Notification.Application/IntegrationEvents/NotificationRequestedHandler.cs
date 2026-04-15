using MediatR;
using Mentoory.Notification.Application.Services;
using Mentoory.Notification.Contracts.IntegrationEvents;
using Mentoory.Notification.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Mentoory.Notification.Application.IntegrationEvents;

public partial class NotificationRequestedHandler : INotificationHandler<NotificationRequestedEvent>
{
    private readonly INotificationQueueService _queueService;
    private readonly INotificationPreferenceRepository _preferenceRepository;
    private readonly ILogger<NotificationRequestedHandler> _logger;

    public NotificationRequestedHandler(
        INotificationQueueService queueService,
        INotificationPreferenceRepository preferenceRepository,
        ILogger<NotificationRequestedHandler> logger)
    {
        _queueService = queueService;
        _preferenceRepository = preferenceRepository;
        _logger = logger;
    }

    public async Task Handle(NotificationRequestedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var preference = await _preferenceRepository.GetByUserAndTypeAsync(
                notification.RecipientUserId,
                notification.NotificationType,
                cancellationToken);

            if (preference is not null && !preference.IsEnabled)
            {
                LogNotificationSuppressed(notification.RecipientUserId, notification.NotificationType);
                return;
            }
        }
        catch (Exception ex)
        {
            LogPreferenceLookupFailed(notification.RecipientUserId, ex);
        }

        await _queueService.QueueAsync(
            notification.NotificationType,
            notification.Subject,
            notification.HtmlBody,
            notification.RecipientUserId,
            notification.RecipientEmail,
            notification.SourceEventId,
            notification.OccurredOn,
            cancellationToken);

        LogNotificationQueued(notification.NotificationType, notification.RecipientEmail);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Notification suppressed for user {UserId} (type={NotificationType}, disabled by preference)")]
    partial void LogNotificationSuppressed(long userId, Domain.Enums.NotificationType notificationType);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to check notification preferences for user {UserId}, defaulting to send")]
    partial void LogPreferenceLookupFailed(long userId, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Notification queued: {Type} for {Email}")]
    partial void LogNotificationQueued(Domain.Enums.NotificationType type, string email);
}
