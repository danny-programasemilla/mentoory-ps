using MediatR;
using Mentoory.Access.Contracts.IntegrationEvents;
using Mentoory.Notification.Application.Configuration;
using Mentoory.Notification.Application.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mentoory.Notification.Application.IntegrationEvents;

public partial class UserRegisteredNotificationHandler : INotificationHandler<UserRegisteredEvent>
{
    private readonly INotificationQueueService _queueService;
    private readonly NotificationSettings _settings;
    private readonly ILogger<UserRegisteredNotificationHandler> _logger;

    public UserRegisteredNotificationHandler(
        INotificationQueueService queueService,
        IOptions<NotificationSettings> settings,
        ILogger<UserRegisteredNotificationHandler> logger)
    {
        _queueService = queueService;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task Handle(UserRegisteredEvent notification, CancellationToken cancellationToken)
    {
        if (!notification.RequiresVerification)
        {
            return;
        }

        var verificationUrl = $"{_settings.BaseUrl}/Access/VerifyEmail?userId={notification.UserExternalId}";

        await _queueService.QueueRegistrationEmailAsync(
            notification.UserId,
            notification.Email,
            notification.FirstName,
            notification.LastName,
            verificationUrl,
            notification.InvitationExpiryHours,
            notification.EventId,
            cancellationToken);

        LogRegistrationEmailQueued(notification.UserId, notification.Email);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Registration email queued for user {UserId} ({Email})")]
    partial void LogRegistrationEmailQueued(long userId, string email);
}
