using MediatR;
using Mentoory.Access.Contracts.IntegrationEvents;
using Mentoory.Notification.Contracts.Configuration;
using Mentoory.Notification.Contracts.IntegrationEvents;
using Mentoory.Notification.Domain.Enums;
using Mentoory.Shared.Application.Notifications;
using Mentoory.Shared.Application.TimeProvider;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Mentoory.Access.Application.IntegrationEvents;

public partial class UserRegisteredNotificationHandler : INotificationHandler<UserRegisteredEvent>
{
    private readonly ITemplateRenderer _templateRenderer;
    private readonly IEmailLayoutWrapper _layoutWrapper;
    private readonly INotificationConfigurationReader _configReader;
    private readonly IMediator _mediator;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<UserRegisteredNotificationHandler> _logger;

    public UserRegisteredNotificationHandler(
        [FromKeyedServices("Access")] ITemplateRenderer templateRenderer,
        IEmailLayoutWrapper layoutWrapper,
        INotificationConfigurationReader configReader,
        IMediator mediator,
        ITimeProvider timeProvider,
        ILogger<UserRegisteredNotificationHandler> logger)
    {
        _templateRenderer = templateRenderer;
        _layoutWrapper = layoutWrapper;
        _configReader = configReader;
        _mediator = mediator;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task Handle(UserRegisteredEvent notification, CancellationToken cancellationToken)
    {
        if (!notification.RequiresVerification)
        {
            return;
        }

        var baseUrl = await _configReader.GetStringAsync(
            nameof(NotificationConfigurationKey.BaseUrl), cancellationToken);
        var verificationUrl = $"{baseUrl}/Access/VerifyEmail?userId={notification.UserExternalId}";

        var innerHtml = await _templateRenderer.RenderAsync("UserRegistration.cshtml", new
        {
            FirstName = notification.FirstName,
            LastName = notification.LastName,
            VerificationUrl = verificationUrl,
            ExpirationHours = notification.InvitationExpiryHours,
        });

        var htmlBody = _layoutWrapper.WrapInBrandLayout(innerHtml);

        await _mediator.Publish(new NotificationRequestedEvent(
            NotificationType.UserRegistration,
            "Bienvenido/a a Mentoory - Verifica tu correo electrónico",
            htmlBody,
            notification.UserId,
            notification.Email,
            notification.EventId,
            _timeProvider.UtcNow), cancellationToken);

        LogRegistrationEmailPublished(notification.UserId, notification.Email);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Registration notification published for user {UserId} ({Email})")]
    partial void LogRegistrationEmailPublished(long userId, string email);
}
