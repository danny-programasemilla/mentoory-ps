using Mentoory.Notification.Domain.Enums;
using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Notification.Domain.Aggregates.Notification;

public class NotificationRecipient : Entity
{
    private readonly List<DeliveryAttempt> _deliveryAttempts = [];

    private NotificationRecipient()
    {
    }

    public long UserId { get; private set; }

    public string Email { get; private set; } = null!;

    public DeliveryChannel DeliveryChannel { get; private set; }

    public DeliveryStatus DeliveryStatus { get; private set; }

    public DateTime? SentAtUtc { get; private set; }

    public string? FailureReason { get; private set; }

    public IReadOnlyCollection<DeliveryAttempt> DeliveryAttempts => _deliveryAttempts.AsReadOnly();

    public void MarkSent(DateTime sentAtUtc)
    {
        if (DeliveryStatus != DeliveryStatus.Pending)
        {
            throw new InvalidOperationException("Only pending recipients can be marked as sent.");
        }

        DeliveryStatus = DeliveryStatus.Sent;
        SentAtUtc = sentAtUtc;
        _deliveryAttempts.Add(DeliveryAttempt.Record(_deliveryAttempts.Count + 1, sentAtUtc, true, null));
    }

    public void RecordFailedAttempt(DateTime attemptedAtUtc, string failureReason, int maxRetryAttempts)
    {
        if (DeliveryStatus != DeliveryStatus.Pending)
        {
            throw new InvalidOperationException("Only pending recipients can record delivery attempts.");
        }

        _deliveryAttempts.Add(DeliveryAttempt.Record(_deliveryAttempts.Count + 1, attemptedAtUtc, false, failureReason));
        FailureReason = failureReason;

        if (_deliveryAttempts.Count >= maxRetryAttempts)
        {
            DeliveryStatus = DeliveryStatus.Failed;
        }
    }

    public DateTime? GetNextRetryAtUtc()
    {
        if (DeliveryStatus != DeliveryStatus.Pending || _deliveryAttempts.Count == 0)
        {
            return null;
        }

        var lastAttempt = _deliveryAttempts[^1];
        var delayMinutes = _deliveryAttempts.Count switch
        {
            1 => 1,
            2 => 5,
            3 => 15,
            4 => 60,
            _ => 240,
        };

        return lastAttempt.AttemptedAtUtc.AddMinutes(delayMinutes);
    }

    internal static NotificationRecipient Create(long userId, string email, DeliveryChannel channel)
        => CreateWithStatus(userId, email, channel, DeliveryStatus.Pending);

    internal static NotificationRecipient CreateSuppressed(long userId, string email, DeliveryChannel channel)
        => CreateWithStatus(userId, email, channel, DeliveryStatus.Suppressed);

    private static NotificationRecipient CreateWithStatus(
        long userId,
        string email,
        DeliveryChannel channel,
        DeliveryStatus status)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId), "User ID must be positive.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email cannot be empty.", nameof(email));
        }

        return new NotificationRecipient
        {
            UserId = userId,
            Email = email,
            DeliveryChannel = channel,
            DeliveryStatus = status,
        };
    }
}
