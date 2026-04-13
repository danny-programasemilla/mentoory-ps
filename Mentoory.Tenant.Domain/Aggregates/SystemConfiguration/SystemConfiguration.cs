using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Tenant.Domain.Aggregates.SystemConfiguration;

public class SystemConfiguration : Entity, IAggregateRoot
{
    private SystemConfiguration()
    {
    }

    public Guid ExternalId { get; private set; }
    public string Key { get; private set; } = null!;
    public string Value { get; private set; } = null!;
    public string? Description { get; private set; }
    public string DataType { get; private set; } = null!;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static SystemConfiguration Create(
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

        return new SystemConfiguration
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
