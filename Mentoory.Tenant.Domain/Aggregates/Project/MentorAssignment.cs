using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Tenant.Domain.Aggregates.Project;

public class MentorAssignment : Entity
{
    private MentorAssignment()
    {
    }

    public Guid ExternalId { get; private set; }
    public long MentorUserId { get; private set; }
    public long EntrepreneurUserId { get; private set; }
    public bool IsLeadMentor { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime AssignedAtUtc { get; private set; }

    public static MentorAssignment Create(long mentorUserId, long entrepreneurUserId, bool isLeadMentor, DateTime utcNow)
    {
        return new MentorAssignment
        {
            ExternalId = Guid.NewGuid(),
            MentorUserId = mentorUserId,
            EntrepreneurUserId = entrepreneurUserId,
            IsLeadMentor = isLeadMentor,
            IsActive = true,
            AssignedAtUtc = utcNow,
        };
    }

    public void Deactivate() => IsActive = false;

    public void RemoveLeadFlag() => IsLeadMentor = false;

    public void SetAsLead() => IsLeadMentor = true;
}
