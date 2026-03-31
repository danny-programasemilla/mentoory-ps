using MediatR;
using Mentoory.Identity.Application.IntegrationEvents;
using Microsoft.Extensions.Logging;

namespace Mentoory.Authorization.Application.IntegrationEvents.Handlers;

/// <summary>
/// Handles the UserRegisteredEvent integration event.
/// Currently a no-op — roles are assigned explicitly by administrators,
/// not automatically upon user registration.
/// </summary>
/// <param name="logger">Logger instance for tracking event handling.</param>
public partial class UserRegisteredEventHandler(ILogger<UserRegisteredEventHandler> logger)
    : INotificationHandler<UserRegisteredEvent>
{
    /// <summary>
    /// Handles the user registered event. No default role assignment on open registration.
    /// </summary>
    /// <param name="notification">The user registered event notification.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A completed task.</returns>
    public Task Handle(UserRegisteredEvent notification, CancellationToken cancellationToken)
    {
        // No default role assignment on open registration — roles are assigned by admins
        LogUserRegisteredEventReceived(notification.UserId, notification.Email);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Logs when a user registered event is received.
    /// </summary>
    [LoggerMessage(Level = LogLevel.Information, Message = "UserRegisteredEvent received. UserId: {UserId}, Email: {Email}")]
    partial void LogUserRegisteredEventReceived(long userId, string email);
}
