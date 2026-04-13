using System.Security.Cryptography;
using Mentoory.Access.Domain.Repositories;
using Mentoory.Access.Domain.Services;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Shared.Application.IntegrationEvents;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Mentoory.Access.Application.IntegrationEvents;

/// <summary>
/// Handles <see cref="InvitationReissuedEvent"/> by generating a fresh
/// EmailVerificationToken for the invited user.
/// </summary>
public partial class InvitationReissuedEventHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ITimeProvider timeProvider,
    ILogger<InvitationReissuedEventHandler> logger)
    : INotificationHandler<InvitationReissuedEvent>
{
    public async Task Handle(InvitationReissuedEvent notification, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(notification.UserId, cancellationToken);
        if (user is null)
        {
            LogUserNotFound(notification.UserId);
            return;
        }

        var utcNow = timeProvider.UtcNow;
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var tokenHash = passwordHasher.HashPassword(Convert.ToBase64String(tokenBytes));
        user.GenerateEmailVerificationToken(utcNow, tokenHash, notification.InvitationExpiryHours);

        userRepository.Update(user);
        await userRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        LogTokenGenerated(notification.UserId);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "User {UserId} not found when handling InvitationReissuedEvent")]
    partial void LogUserNotFound(long userId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Fresh verification token generated for user {UserId} after invitation reissue")]
    partial void LogTokenGenerated(long userId);
}
