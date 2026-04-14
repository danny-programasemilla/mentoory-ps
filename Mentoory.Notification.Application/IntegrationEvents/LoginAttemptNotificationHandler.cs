using MediatR;
using Mentoory.Access.Contracts.IntegrationEvents;
using Mentoory.Notification.Application.Services;
using Mentoory.Notification.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Mentoory.Notification.Application.IntegrationEvents;

public partial class LoginAttemptNotificationHandler : INotificationHandler<LoginAttemptEvent>
{
    private const int SuspiciousThreshold = 3;

    private readonly INotificationQueueService _queueService;
    private readonly ILoginContextParser _loginContextParser;
    private readonly INotificationPreferenceRepository _preferenceRepository;
    private readonly ILogger<LoginAttemptNotificationHandler> _logger;

    public LoginAttemptNotificationHandler(
        INotificationQueueService queueService,
        ILoginContextParser loginContextParser,
        INotificationPreferenceRepository preferenceRepository,
        ILogger<LoginAttemptNotificationHandler> logger)
    {
        _queueService = queueService;
        _loginContextParser = loginContextParser;
        _preferenceRepository = preferenceRepository;
        _logger = logger;
    }

    public async Task Handle(LoginAttemptEvent notification, CancellationToken cancellationToken)
    {
        if (!notification.Success || notification.UserId is null)
        {
            return;
        }

        var userId = notification.UserId.Value;

        // Check user preference for login alerts
        try
        {
            var preference = await _preferenceRepository.GetByUserAndTypeAsync(
                userId,
                Domain.Enums.NotificationType.LoginAlert,
                cancellationToken);

            if (preference is not null && !preference.IsEnabled)
            {
                LogLoginAlertSuppressed(userId);
                return;
            }
        }
        catch (Exception ex)
        {
            LogPreferenceLookupFailed(userId, ex);
        }

        var isSuspicious = notification.FailedAttemptCount >= SuspiciousThreshold;
        var loginContext = _loginContextParser.Parse(notification.UserAgentString, notification.IpAddress, isSuspicious);

        await _queueService.QueueLoginAlertAsync(
            userId,
            notification.Email,
            loginContext,
            notification.EventId,
            cancellationToken);

        LogLoginAlertQueued(userId, notification.Email, isSuspicious);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Login alert suppressed for user {UserId} (disabled by preference)")]
    partial void LogLoginAlertSuppressed(long userId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to check notification preferences for user {UserId}, defaulting to send")]
    partial void LogPreferenceLookupFailed(long userId, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Login alert queued for user {UserId} ({Email}), suspicious={IsSuspicious}")]
    partial void LogLoginAlertQueued(long userId, string email, bool isSuspicious);
}
