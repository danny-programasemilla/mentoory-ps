using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Tenant.Domain.Aggregates.Incubator;

public class Incubator : Entity, IAggregateRoot
{
    private Incubator()
    {
    }

    public Guid ExternalId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public long? SubscriptionPlanId { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static Incubator Create(string name, string? description, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Incubator name is required.", nameof(name));
        }

        return new Incubator
        {
            ExternalId = Guid.NewGuid(),
            Name = name.Trim(),
            Description = description?.Trim(),
            IsActive = true,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow,
        };
    }

    public void Update(string name, string? description, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Incubator name is required.", nameof(name));
        }

        Name = name.Trim();
        Description = description?.Trim();
        UpdatedAtUtc = utcNow;
    }

    public void Activate(DateTime utcNow)
    {
        IsActive = true;
        UpdatedAtUtc = utcNow;
    }

    public void Deactivate(DateTime utcNow)
    {
        IsActive = false;
        UpdatedAtUtc = utcNow;
    }

    public void AssignSubscriptionPlan(long subscriptionPlanId, DateTime utcNow)
    {
        SubscriptionPlanId = subscriptionPlanId;
        UpdatedAtUtc = utcNow;
    }
}
