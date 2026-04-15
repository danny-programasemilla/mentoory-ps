using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Notification.Domain.Aggregates.NotificationConfiguration;

public class NotificationConfiguration : Entity, IAggregateRoot
{
    private NotificationConfiguration()
    {
    }

    public Guid ExternalId { get; private set; }
    public string Key { get; private set; } = null!;
    public string Value { get; private set; } = null!;
    public string? Description { get; private set; }
    public string DataType { get; private set; } = null!;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static NotificationConfiguration Create(
        string key,
        string value,
        string dataType,
        string? description,
        DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Configuration key is required.", nameof(key));
        }

        return new NotificationConfiguration
        {
            ExternalId = Guid.NewGuid(),
            Key = key.Trim(),
            Value = value,
            Description = description,
            DataType = dataType,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow,
        };
    }

    public void Update(string value, DateTime utcNow)
    {
        Value = value;
        UpdatedAtUtc = utcNow;
    }
}
