using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Tenant.Domain.Aggregates.Project;

public class ProjectParticipant : Entity
{
    private ProjectParticipant()
    {
    }

    public Guid ExternalId { get; private set; }
    public long UserId { get; private set; }
    public string Role { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public DateTime EnrolledAtUtc { get; private set; }

    public static ProjectParticipant Create(long userId, string role, DateTime utcNow)
    {
        return new ProjectParticipant
        {
            ExternalId = Guid.NewGuid(),
            UserId = userId,
            Role = role,
            IsActive = true,
            EnrolledAtUtc = utcNow,
        };
    }

    public void Deactivate() => IsActive = false;
}
