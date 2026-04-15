using MediatR;
using Mentoory.Notification.Contracts.Configuration;
using Mentoory.Notification.Contracts.IntegrationEvents;
using Mentoory.Notification.Domain.Enums;
using Mentoory.Shared.Application.Notifications;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Tenant.Contracts.IntegrationEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Mentoory.Tenant.Application.IntegrationEvents;

public partial class InvitationReissuedNotificationHandler : INotificationHandler<InvitationReissuedEvent>
{
    private readonly ITemplateRenderer _templateRenderer;
    private readonly IEmailLayoutWrapper _layoutWrapper;
    private readonly INotificationConfigurationReader _configReader;
    private readonly IMediator _mediator;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<InvitationReissuedNotificationHandler> _logger;

    public InvitationReissuedNotificationHandler(
        [FromKeyedServices("Tenant")] ITemplateRenderer templateRenderer,
        IEmailLayoutWrapper layoutWrapper,
        INotificationConfigurationReader configReader,
        IMediator mediator,
        ITimeProvider timeProvider,
        ILogger<InvitationReissuedNotificationHandler> logger)
    {
        _templateRenderer = templateRenderer;
        _layoutWrapper = layoutWrapper;
        _configReader = configReader;
        _mediator = mediator;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task Handle(InvitationReissuedEvent notification, CancellationToken cancellationToken)
    {
        var baseUrl = await _configReader.GetStringAsync(
            nameof(NotificationConfigurationKey.BaseUrl), cancellationToken);
        var actionUrl = $"{baseUrl}/Access/AcceptInvitation?userId={notification.UserExternalId}";

        var innerHtml = await _templateRenderer.RenderAsync("ProjectInvitation.cshtml", new
        {
            FirstName = notification.FirstName,
            LastName = notification.LastName,
            ProjectName = notification.ProjectName,
            IncubatorName = notification.IncubatorName,
            ActionUrl = actionUrl,
            ExpirationHours = notification.InvitationExpiryHours,
        });

        var htmlBody = _layoutWrapper.WrapInBrandLayout(innerHtml);

        await _mediator.Publish(new NotificationRequestedEvent(
            NotificationType.ProjectInvitation,
            $"Invitación al proyecto {notification.ProjectName} - Mentoory",
            htmlBody,
            notification.UserId,
            notification.Email,
            notification.EventId,
            _timeProvider.UtcNow), cancellationToken);

        LogInvitationPublished(notification.UserId, notification.Email, notification.ProjectName);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Invitation notification published for user {UserId} ({Email}) - project {ProjectName}")]
    partial void LogInvitationPublished(long userId, string email, string projectName);
}
