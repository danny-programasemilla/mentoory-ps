using Mentoory.Shared.Domain.SeedWork;
using Mentoory.Tenant.Domain.Enums;

namespace Mentoory.Tenant.Domain.Aggregates.Project;

public class Project : Entity, IAggregateRoot
{
    private readonly List<ProjectStage> _stages = new();
    private readonly List<ProjectParticipant> _participants = new();
    private readonly List<MentorAssignment> _mentorAssignments = new();

    private Project()
    {
    }

    public Guid ExternalId { get; private set; }
    public long IncubatorId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public Guid KnowledgeStructureTemplateExternalId { get; private set; }
    public StageType CurrentStageType { get; private set; }
    public StageState CurrentStageState { get; private set; }
    public bool IsPublic { get; private set; }
    public EnrollmentVariant EnrollmentVariant { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;
    public IReadOnlyCollection<ProjectStage> Stages => _stages.AsReadOnly();
    public IReadOnlyCollection<ProjectParticipant> Participants => _participants.AsReadOnly();
    public IReadOnlyCollection<MentorAssignment> MentorAssignments => _mentorAssignments.AsReadOnly();

    public static Project Create(
        long incubatorId,
        string name,
        string? description,
        Guid knowledgeStructureTemplateExternalId,
        DateTime utcNow,
        bool isPublic = false,
        EnrollmentVariant enrollmentVariant = EnrollmentVariant.FullFlow)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Project name is required.", nameof(name));
        }

        if (knowledgeStructureTemplateExternalId == Guid.Empty)
        {
            throw new ArgumentException("Knowledge structure template is required.", nameof(knowledgeStructureTemplateExternalId));
        }

        var project = new Project
        {
            ExternalId = Guid.NewGuid(),
            IncubatorId = incubatorId,
            Name = name.Trim(),
            Description = description?.Trim(),
            KnowledgeStructureTemplateExternalId = knowledgeStructureTemplateExternalId,
            CurrentStageType = StageType.Registration,
            CurrentStageState = StageState.InProgress,
            IsPublic = isPublic,
            EnrollmentVariant = enrollmentVariant,
            IsActive = true,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow,
        };

        // Initialize all 7 stages
        foreach (StageType stageType in Enum.GetValues<StageType>())
        {
            var state = stageType == StageType.Registration ? StageState.InProgress : StageState.NotStarted;
            project._stages.Add(ProjectStage.Create(stageType, state, stageType == StageType.Registration ? utcNow : null));
        }

        return project;
    }

    public void AdvanceStage(long advancedByUserId, DateTime utcNow)
    {
        if (CurrentStageState != StageState.InProgress)
        {
            throw new InvalidOperationException("Current stage must be in progress to advance.");
        }

        // Complete current stage
        var currentStage = _stages.Single(s => s.StageType == CurrentStageType);
        currentStage.Complete(utcNow);
        CurrentStageState = StageState.Completed;

        // Find next stage
        var nextStageType = (StageType)((int)CurrentStageType + 1);
        if (!Enum.IsDefined(nextStageType))
        {
            // Already at final stage (Closure)
            UpdatedAtUtc = utcNow;
            return;
        }

        // Start next stage
        var nextStage = _stages.Single(s => s.StageType == nextStageType);
        nextStage.Start(advancedByUserId, utcNow);
        CurrentStageType = nextStageType;
        CurrentStageState = StageState.InProgress;
        UpdatedAtUtc = utcNow;
    }

    public ProjectParticipant EnrollParticipant(long userId, string role, DateTime utcNow)
    {
        var participant = ProjectParticipant.Create(userId, role, utcNow);
        _participants.Add(participant);
        UpdatedAtUtc = utcNow;
        return participant;
    }

    public MentorAssignment AssignMentor(long mentorUserId, long entrepreneurUserId, bool isLeadMentor, DateTime utcNow)
    {
        if (isLeadMentor)
        {
            // Ensure only one lead mentor per entrepreneur per project
            var existingLead = _mentorAssignments
                .SingleOrDefault(ma => ma.EntrepreneurUserId == entrepreneurUserId && ma.IsLeadMentor && ma.IsActive);
            existingLead?.RemoveLeadFlag();
        }

        var assignment = MentorAssignment.Create(mentorUserId, entrepreneurUserId, isLeadMentor, utcNow);
        _mentorAssignments.Add(assignment);
        UpdatedAtUtc = utcNow;
        return assignment;
    }
}
