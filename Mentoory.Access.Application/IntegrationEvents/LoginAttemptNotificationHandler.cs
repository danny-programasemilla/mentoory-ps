using MediatR;
using Mentoory.Access.Application.Services;
using Mentoory.Access.Contracts.IntegrationEvents;
using Mentoory.Notification.Contracts.IntegrationEvents;
using Mentoory.Notification.Domain.Enums;
using Mentoory.Shared.Application.Notifications;
using Mentoory.Shared.Application.TimeProvider;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Mentoory.Access.Application.IntegrationEvents;

public partial class LoginAttemptNotificationHandler : INotificationHandler<LoginAttemptEvent>
{
    private const int SuspiciousThreshold = 3;

    private readonly ITemplateRenderer _templateRenderer;
    private readonly IEmailLayoutWrapper _layoutWrapper;
    private readonly IUserAgentParser _userAgentParser;
    private readonly IMediator _mediator;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<LoginAttemptNotificationHandler> _logger;

    public LoginAttemptNotificationHandler(
        [FromKeyedServices("Access")] ITemplateRenderer templateRenderer,
        IEmailLayoutWrapper layoutWrapper,
        IUserAgentParser userAgentParser,
        IMediator mediator,
        ITimeProvider timeProvider,
        ILogger<LoginAttemptNotificationHandler> logger)
    {
        _templateRenderer = templateRenderer;
        _layoutWrapper = layoutWrapper;
        _userAgentParser = userAgentParser;
        _mediator = mediator;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task Handle(LoginAttemptEvent notification, CancellationToken cancellationToken)
    {
        if (!notification.Success || notification.UserId is null)
        {
            return;
        }

        var userId = notification.UserId.Value;
        var isSuspicious = notification.FailedAttemptCount >= SuspiciousThreshold;
        var (browserName, operatingSystem) = _userAgentParser.Parse(notification.UserAgentString);
        var utcNow = _timeProvider.UtcNow;

        var templateName = isSuspicious
            ? "SuspiciousLoginAlert.cshtml"
            : "LoginAlert.cshtml";

        var subject = isSuspicious
            ? "Actividad sospechosa detectada - Mentoory"
            : "Alerta de inicio de sesión - Mentoory";

        var innerHtml = await _templateRenderer.RenderAsync(templateName, new
        {
            IpAddress = notification.IpAddress,
            BrowserName = browserName,
            OperatingSystem = operatingSystem,
            Timestamp = utcNow.ToString("dd/MM/yyyy HH:mm:ss 'UTC'"),
        });

        var htmlBody = _layoutWrapper.WrapInBrandLayout(innerHtml);

        await _mediator.Publish(new NotificationRequestedEvent(
            NotificationType.LoginAlert,
            subject,
            htmlBody,
            userId,
            notification.Email,
            notification.EventId,
            utcNow), cancellationToken);

        LogLoginAlertPublished(userId, notification.Email, isSuspicious);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Login alert published for user {UserId} ({Email}), suspicious={IsSuspicious}")]
    partial void LogLoginAlertPublished(long userId, string email, bool isSuspicious);
}
