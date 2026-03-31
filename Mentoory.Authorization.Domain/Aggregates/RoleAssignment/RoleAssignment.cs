using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Authorization.Domain.Aggregates.RoleAssignment;

public class RoleAssignment : Entity, IAggregateRoot
{
    private RoleAssignment()
    {
    }

    public Guid ExternalId { get; private set; }
    public long UserId { get; private set; }
    public long IncubatorId { get; private set; }
    public long? ProjectId { get; private set; }
    public string Role { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static RoleAssignment Create(
        long userId,
        long incubatorId,
        long? projectId,
        string role,
        DateTime utcNow)
    {
        ValidateRole(role);

        return new RoleAssignment
        {
            ExternalId = Guid.NewGuid(),
            UserId = userId,
            IncubatorId = incubatorId,
            ProjectId = projectId,
            Role = role,
            IsActive = true,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow,
        };
    }

    public void Revoke(DateTime utcNow)
    {
        if (!IsActive)
        {
            throw new InvalidOperationException("Role assignment is already revoked.");
        }

        IsActive = false;
        UpdatedAtUtc = utcNow;
    }

    public void Reactivate(DateTime utcNow)
    {
        IsActive = true;
        UpdatedAtUtc = utcNow;
    }

    private static void ValidateRole(string role)
    {
        if (!Shared.Domain.Constants.Roles.All.Contains(role))
        {
            throw new ArgumentException($"Invalid role: {role}", nameof(role));
        }
    }
}
