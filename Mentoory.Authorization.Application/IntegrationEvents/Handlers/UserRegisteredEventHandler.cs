using MediatR;
using Mentoory.Authorization.Domain.ReadModels;
using Mentoory.Authorization.Domain.Repositories;
using Mentoory.Identity.Application.IntegrationEvents;
using Microsoft.Extensions.Logging;

namespace Mentoory.Authorization.Application.IntegrationEvents.Handlers;

/// <summary>
/// Handles the UserRegisteredEvent integration event by creating a UserProfile
/// read model entry in the Authorization domain.
/// </summary>
/// <param name="userProfileRepository">Repository for persisting user profile read models.</param>
/// <param name="logger">Logger instance for tracking event handling.</param>
public partial class UserRegisteredEventHandler(
    IUserProfileRepository userProfileRepository,
    ILogger<UserRegisteredEventHandler> logger)
    : INotificationHandler<UserRegisteredEvent>
{
    /// <summary>
    /// Handles the user registered event by creating a UserProfile read model.
    /// Idempotent — skips if a profile for the user already exists.
    /// </summary>
    /// <param name="notification">The user registered event notification.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    public async Task Handle(UserRegisteredEvent notification, CancellationToken cancellationToken)
    {
        var existing = await userProfileRepository.GetByUserIdAsync(notification.UserId, cancellationToken);
        if (existing is not null)
        {
            LogUserProfileAlreadyExists(notification.UserId, notification.Email);
            return;
        }

        var profile = UserProfile.Create(
            notification.UserId,
            notification.UserExternalId,
            notification.Email,
            notification.FirstName,
            notification.LastName,
            notification.AccountStatus,
            notification.CreatedAtUtc,
            notification.OccurredOn);

        userProfileRepository.Add(profile);
        await userProfileRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

        LogUserProfileCreated(notification.UserId, notification.Email);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "UserProfile created for UserId: {UserId}, Email: {Email}")]
    partial void LogUserProfileCreated(long userId, string email);

    [LoggerMessage(Level = LogLevel.Debug, Message = "UserProfile already exists for UserId: {UserId}, Email: {Email}")]
    partial void LogUserProfileAlreadyExists(long userId, string email);
}
