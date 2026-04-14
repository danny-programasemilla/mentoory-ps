using Mentoory.Notification.Domain.Enums;
using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Notification.Domain.Aggregates.Notification;

public class Notification : Entity, IAggregateRoot
{
    private readonly List<NotificationRecipient> _recipients = [];

    private Notification()
    {
    }

    public Guid ExternalId { get; private set; }

    public NotificationType NotificationType { get; private set; }

    public string Subject { get; private set; } = null!;

    public string HtmlBody { get; private set; } = null!;

    public Guid? SourceEventId { get; private set; }

    public DateTime ScheduledForUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public LoginContext? LoginContext { get; private set; }

    public IReadOnlyCollection<NotificationRecipient> Recipients => _recipients.AsReadOnly();

    public static Notification Create(
        NotificationType type,
        string subject,
        string htmlBody,
        Guid? sourceEventId,
        DateTime scheduledForUtc,
        DateTime createdAtUtc,
        LoginContext? loginContext = null)
    {
        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new ArgumentException("Subject cannot be empty.", nameof(subject));
        }

        if (string.IsNullOrWhiteSpace(htmlBody))
        {
            throw new ArgumentException("HTML body cannot be empty.", nameof(htmlBody));
        }

        return new Notification
        {
            ExternalId = Guid.NewGuid(),
            NotificationType = type,
            Subject = subject,
            HtmlBody = htmlBody,
            SourceEventId = sourceEventId,
            ScheduledForUtc = scheduledForUtc,
            CreatedAtUtc = createdAtUtc,
            LoginContext = loginContext,
        };
    }

    public void AddRecipient(long userId, string email, DeliveryChannel channel)
    {
        _recipients.Add(NotificationRecipient.Create(userId, email, channel));
    }

    public void AddSuppressedRecipient(long userId, string email, DeliveryChannel channel)
    {
        _recipients.Add(NotificationRecipient.CreateSuppressed(userId, email, channel));
    }
}
