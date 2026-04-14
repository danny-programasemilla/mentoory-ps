using MediatR;
using Mentoory.Notification.Application.Configuration;
using Mentoory.Notification.Application.Services;
using Mentoory.Tenant.Contracts.IntegrationEvents;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mentoory.Notification.Application.IntegrationEvents;

public partial class InvitationReissuedNotificationHandler : INotificationHandler<InvitationReissuedEvent>
{
    private readonly INotificationQueueService _queueService;
    private readonly NotificationSettings _settings;
    private readonly ILogger<InvitationReissuedNotificationHandler> _logger;

    public InvitationReissuedNotificationHandler(
        INotificationQueueService queueService,
        IOptions<NotificationSettings> settings,
        ILogger<InvitationReissuedNotificationHandler> logger)
    {
        _queueService = queueService;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task Handle(InvitationReissuedEvent notification, CancellationToken cancellationToken)
    {
        var actionUrl = $"{_settings.BaseUrl}/Access/AcceptInvitation?userId={notification.UserExternalId}";

        await _queueService.QueueInvitationEmailAsync(
            notification.UserId,
            notification.Email,
            notification.FirstName,
            notification.LastName,
            notification.ProjectName,
            notification.IncubatorName,
            actionUrl,
            notification.InvitationExpiryHours,
            notification.EventId,
            cancellationToken);

        LogInvitationReissuedEmailQueued(notification.UserId, notification.Email, notification.ProjectName);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Invitation reissued email queued for user {UserId} ({Email}) - project {ProjectName}")]
    partial void LogInvitationReissuedEmailQueued(long userId, string email, string projectName);
}
