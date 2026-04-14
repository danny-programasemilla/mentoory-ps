using Mentoory.Notification.Application.Services;
using Mentoory.Notification.Domain.Aggregates.Notification;
using Mentoory.Notification.Domain.Enums;
using Mentoory.Notification.Domain.Repositories;
using Mentoory.Notification.Application.Configuration;
using Mentoory.Shared.Application.TimeProvider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationAggregate = Mentoory.Notification.Domain.Aggregates.Notification.Notification;

namespace Mentoory.Notification.Infrastructure.Services;

public partial class NotificationQueueService : INotificationQueueService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IEmailService _emailService;
    private readonly ITemplateRenderer _templateRenderer;
    private readonly ITimeProvider _timeProvider;
    private readonly NotificationSettings _settings;
    private readonly ILogger<NotificationQueueService> _logger;

    public NotificationQueueService(
        INotificationRepository notificationRepository,
        IEmailService emailService,
        ITemplateRenderer templateRenderer,
        ITimeProvider timeProvider,
        IOptions<NotificationSettings> settings,
        ILogger<NotificationQueueService> logger)
    {
        _notificationRepository = notificationRepository;
        _emailService = emailService;
        _templateRenderer = templateRenderer;
        _timeProvider = timeProvider;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task QueueRegistrationEmailAsync(
        long userId,
        string email,
        string firstName,
        string lastName,
        string verificationUrl,
        int expirationHours,
        Guid sourceEventId,
        CancellationToken cancellationToken)
    {
        if (await IsDuplicateAsync(sourceEventId, NotificationType.UserRegistration, cancellationToken))
        {
            LogDuplicateSkipped(sourceEventId, NotificationType.UserRegistration);
            return;
        }

        var htmlBody = await _templateRenderer.RenderAsync("UserRegistration.cshtml", new
        {
            FirstName = firstName,
            LastName = lastName,
            VerificationUrl = verificationUrl,
            ExpirationHours = expirationHours,
        });

        var utcNow = _timeProvider.UtcNow;
        var notification = NotificationAggregate.Create(
            NotificationType.UserRegistration,
            "Bienvenido/a a Mentoory - Verifica tu correo electrónico",
            htmlBody,
            sourceEventId,
            utcNow,
            utcNow);

        notification.AddRecipient(userId, email, DeliveryChannel.Email);
        _notificationRepository.Add(notification);
        await _notificationRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        LogNotificationQueued(NotificationType.UserRegistration, email);
    }

    public async Task QueueInvitationEmailAsync(
        long userId,
        string email,
        string firstName,
        string lastName,
        string projectName,
        string incubatorName,
        string actionUrl,
        int expirationHours,
        Guid sourceEventId,
        CancellationToken cancellationToken)
    {
        if (await IsDuplicateAsync(sourceEventId, NotificationType.ProjectInvitation, cancellationToken))
        {
            LogDuplicateSkipped(sourceEventId, NotificationType.ProjectInvitation);
            return;
        }

        var htmlBody = await _templateRenderer.RenderAsync("ProjectInvitation.cshtml", new
        {
            FirstName = firstName,
            LastName = lastName,
            ProjectName = projectName,
            IncubatorName = incubatorName,
            ActionUrl = actionUrl,
            ExpirationHours = expirationHours,
        });

        var utcNow = _timeProvider.UtcNow;
        var notification = NotificationAggregate.Create(
            NotificationType.ProjectInvitation,
            $"Invitación al proyecto {projectName} - Mentoory",
            htmlBody,
            sourceEventId,
            utcNow,
            utcNow);

        notification.AddRecipient(userId, email, DeliveryChannel.Email);
        _notificationRepository.Add(notification);
        await _notificationRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        LogNotificationQueued(NotificationType.ProjectInvitation, email);
    }

    public async Task QueueLoginAlertAsync(
        long userId,
        string email,
        LoginContext loginContext,
        Guid sourceEventId,
        CancellationToken cancellationToken)
    {
        if (await IsDuplicateAsync(sourceEventId, NotificationType.LoginAlert, cancellationToken))
        {
            LogDuplicateSkipped(sourceEventId, NotificationType.LoginAlert);
            return;
        }

        var templateName = loginContext.IsSuspicious
            ? "SuspiciousLoginAlert.cshtml"
            : "LoginAlert.cshtml";

        var subject = loginContext.IsSuspicious
            ? "Actividad sospechosa detectada - Mentoory"
            : "Alerta de inicio de sesión - Mentoory";

        var utcNow = _timeProvider.UtcNow;

        var htmlBody = await _templateRenderer.RenderAsync(templateName, new
        {
            loginContext.IpAddress,
            loginContext.BrowserName,
            loginContext.OperatingSystem,
            Timestamp = utcNow.ToString("dd/MM/yyyy HH:mm:ss 'UTC'"),
        });
        var notification = NotificationAggregate.Create(
            NotificationType.LoginAlert,
            subject,
            htmlBody,
            sourceEventId,
            utcNow,
            utcNow,
            loginContext);

        notification.AddRecipient(userId, email, DeliveryChannel.Email);
        _notificationRepository.Add(notification);
        await _notificationRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        LogNotificationQueued(NotificationType.LoginAlert, email);
    }

    public async Task ProcessPendingAsync(CancellationToken cancellationToken)
    {
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
                    recipient.RecordFailedAttempt(utcNow, ex.Message, _settings.MaxRetryAttempts);
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
        var existing = await _notificationRepository.GetBySourceEventIdAndTypeAsync(sourceEventId, type, cancellationToken);
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
