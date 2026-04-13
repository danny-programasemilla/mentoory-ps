using Mentoory.Tenant.Domain.Enums;
using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Tenant.Domain.Aggregates.ProjectInvitation;

public class ProjectInvitation : Entity, IAggregateRoot
{
    private ProjectInvitation()
    {
    }

    public Guid ExternalId { get; private set; }
    public long ProjectId { get; private set; }
    public long UserId { get; private set; }
    public InvitationStatus Status { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? AcceptedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public long CreatedByUserId { get; private set; }
    public bool IsActive { get; private set; }
    public bool RequiresAcceptance { get; private set; }

    public static ProjectInvitation Create(
        long projectId,
        long userId,
        DateTime expiresAtUtc,
        long createdByUserId,
        DateTime utcNow,
        bool requiresAcceptance = true)
    {
        return new ProjectInvitation
        {
            ExternalId = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = userId,
            Status = InvitationStatus.Pending,
            ExpiresAtUtc = expiresAtUtc,
            CreatedAtUtc = utcNow,
            CreatedByUserId = createdByUserId,
            IsActive = true,
            RequiresAcceptance = requiresAcceptance,
        };
    }

    public void Accept(DateTime utcNow)
    {
        CheckExpiration(utcNow);

        if (Status != InvitationStatus.Pending)
        {
            throw new InvalidOperationException("Only pending invitations can be accepted.");
        }

        Status = InvitationStatus.Accepted;
        AcceptedAtUtc = utcNow;
    }

    public void CheckExpiration(DateTime utcNow)
    {
        if (Status == InvitationStatus.Pending && utcNow >= ExpiresAtUtc)
        {
            Status = InvitationStatus.Expired;
        }
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
