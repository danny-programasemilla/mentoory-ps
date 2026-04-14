using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Notification.Domain.Aggregates.Notification;

public class DeliveryAttempt : Entity
{
    private DeliveryAttempt()
    {
    }

    public int AttemptNumber { get; private set; }

    public DateTime AttemptedAtUtc { get; private set; }

    public bool Success { get; private set; }

    public string? FailureReason { get; private set; }

    internal static DeliveryAttempt Record(int attemptNumber, DateTime attemptedAtUtc, bool success, string? failureReason)
    {
        if (attemptNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(attemptNumber), "Attempt number must be at least 1.");
        }

        return new DeliveryAttempt
        {
            AttemptNumber = attemptNumber,
            AttemptedAtUtc = attemptedAtUtc,
            Success = success,
            FailureReason = failureReason,
        };
    }
}
