using Mentoory.Notification.Domain.Aggregates.Notification;

namespace Mentoory.Notification.Application.Services;

public interface INotificationQueueService
{
    Task QueueRegistrationEmailAsync(
        long userId,
        string email,
        string firstName,
        string lastName,
        string verificationUrl,
        int expirationHours,
        Guid sourceEventId,
        CancellationToken cancellationToken);

    Task QueueInvitationEmailAsync(
        long userId,
        string email,
        string firstName,
        string lastName,
        string projectName,
        string incubatorName,
        string actionUrl,
        int expirationHours,
        Guid sourceEventId,
        CancellationToken cancellationToken);

    Task QueueLoginAlertAsync(
        long userId,
        string email,
        LoginContext loginContext,
        Guid sourceEventId,
        CancellationToken cancellationToken);

    Task ProcessPendingAsync(CancellationToken cancellationToken);
}
