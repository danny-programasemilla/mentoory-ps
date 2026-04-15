using Mentoory.Shared.Domain.SeedWork;
using Mentoory.Tenant.Domain.Enums;

namespace Mentoory.Tenant.Domain.Aggregates.Project;

public class Project : Entity, IAggregateRoot
{
    private static readonly Dictionary<StageType, string> StageBaseNames = new()
    {
        { StageType.Registration, "Registro" },
        { StageType.Diagnosis, "Diagnóstico" },
        { StageType.Mentorship, "Mentoría" },
        { StageType.Closure, "Cierre" },
    };

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
    public StageType CurrentStageType { get; private set; }
    public StageState CurrentStageState { get; private set; }
    public bool IsPublic { get; private set; }
    public EnrollmentVariant EnrollmentVariant { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public IReadOnlyCollection<ProjectStage> Stages => _stages.AsReadOnly();
    public IReadOnlyCollection<ProjectParticipant> Participants => _participants.AsReadOnly();
    public IReadOnlyCollection<MentorAssignment> MentorAssignments => _mentorAssignments.AsReadOnly();

    public static Project Create(
        long incubatorId,
        string name,
        string? description,
        DateTime utcNow,
        bool isPublic = false,
        EnrollmentVariant enrollmentVariant = EnrollmentVariant.FullFlow)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Project name is required.", nameof(name));
        }

        var project = new Project
        {
            ExternalId = Guid.NewGuid(),
            IncubatorId = incubatorId,
            Name = name.Trim(),
            Description = description?.Trim(),
            CurrentStageType = StageType.Registration,
            CurrentStageState = StageState.InProgress,
            IsPublic = isPublic,
            EnrollmentVariant = enrollmentVariant,
            IsActive = true,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow,
        };

        // Default 5-stage pipeline: Registration → Diagnosis → Mentorship → Diagnosis → Closure
        project._stages.Add(ProjectStage.Create(StageType.Registration, StageState.InProgress, 0, "Registro", utcNow));
        project._stages.Add(ProjectStage.Create(StageType.Diagnosis, StageState.NotStarted, 1, "Diagnóstico 1", null));
        project._stages.Add(ProjectStage.Create(StageType.Mentorship, StageState.NotStarted, 2, "Mentoría", null));
        project._stages.Add(ProjectStage.Create(StageType.Diagnosis, StageState.NotStarted, 3, "Diagnóstico 2", null));
        project._stages.Add(ProjectStage.Create(StageType.Closure, StageState.NotStarted, 4, "Cierre", null));

        return project;
    }

    public ProjectStage AddStage(StageType stageType, int position, DateTime utcNow)
    {
        if (stageType == StageType.Registration || stageType == StageType.Closure)
        {
            throw new InvalidOperationException("Cannot add Registration or Closure stages manually.");
        }

        if (position < 1 || position >= _stages.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(position),
                "Stage must be inserted between first and last position.");
        }

        // Shift positions of subsequent stages
        foreach (var stage in _stages.Where(s => s.Position >= position))
        {
            stage.SetPosition(stage.Position + 1);
        }

        var displayName = GenerateDisplayName(stageType);
        var newStage = ProjectStage.Create(stageType, StageState.NotStarted, position, displayName, null);
        _stages.Add(newStage);
        UpdatedAtUtc = utcNow;

        RegenerateDisplayNames();

        return newStage;
    }

    public void RemoveStage(long stageId, DateTime utcNow)
    {
        var stage = _stages.SingleOrDefault(s => s.Id == stageId)
            ?? throw new InvalidOperationException("Stage not found.");

        if (stage.StageType == StageType.Registration || stage.StageType == StageType.Closure)
        {
            throw new InvalidOperationException("Cannot remove Registration or Closure stages.");
        }

        if (_stages.Count <= 2)
        {
            throw new InvalidOperationException("Pipeline must have at least Registration and Closure stages.");
        }

        var removedPosition = stage.Position;
        _stages.Remove(stage);

        // Shift positions of subsequent stages
        foreach (var s in _stages.Where(s => s.Position > removedPosition))
        {
            s.SetPosition(s.Position - 1);
        }

        UpdatedAtUtc = utcNow;
        RegenerateDisplayNames();
    }

    public void ReorderStages(IReadOnlyList<long> orderedStageIds, DateTime utcNow)
    {
        if (orderedStageIds.Count != _stages.Count)
        {
            throw new ArgumentException("Must provide exactly one ID per stage.", nameof(orderedStageIds));
        }

        var stageMap = _stages.ToDictionary(s => s.Id);

        foreach (var id in orderedStageIds)
        {
            if (!stageMap.ContainsKey(id))
            {
                throw new ArgumentException($"Stage ID {id} not found in pipeline.", nameof(orderedStageIds));
            }
        }

        // Validate boundary constraints
        var firstId = orderedStageIds[0];
        var lastId = orderedStageIds[^1];

        if (stageMap[firstId].StageType != StageType.Registration)
        {
            throw new InvalidOperationException("First stage must be Registration.");
        }

        if (stageMap[lastId].StageType != StageType.Closure)
        {
            throw new InvalidOperationException("Last stage must be Closure.");
        }

        // Apply new positions
        for (var i = 0; i < orderedStageIds.Count; i++)
        {
            stageMap[orderedStageIds[i]].SetPosition(i);
        }

        UpdatedAtUtc = utcNow;
        RegenerateDisplayNames();
    }

    public void RenameStage(long stageId, string displayName, DateTime utcNow)
    {
        var stage = _stages.SingleOrDefault(s => s.Id == stageId)
            ?? throw new InvalidOperationException("Stage not found.");

        stage.Rename(displayName);
        UpdatedAtUtc = utcNow;
    }

    public void AdvanceStage(long advancedByUserId, DateTime utcNow)
    {
        if (CurrentStageState != StageState.InProgress)
        {
            throw new InvalidOperationException("Current stage must be in progress to advance.");
        }

        var orderedStages = _stages.OrderBy(s => s.Position).ToList();
        var currentStage = orderedStages.FirstOrDefault(s => s.State == StageState.InProgress)
            ?? throw new InvalidOperationException("No stage is currently in progress.");

        currentStage.Complete(utcNow);

        var currentIndex = orderedStages.IndexOf(currentStage);
        if (currentIndex >= orderedStages.Count - 1)
        {
            // Last stage (Closure) completed
            CurrentStageState = StageState.Completed;
            UpdatedAtUtc = utcNow;
            return;
        }

        // Start next stage
        var nextStage = orderedStages[currentIndex + 1];
        nextStage.Start(advancedByUserId, utcNow);
        CurrentStageType = nextStage.StageType;
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

    private string GenerateDisplayName(StageType stageType)
    {
        return StageBaseNames.TryGetValue(stageType, out var baseName) ? baseName : stageType.ToString();
    }

    private void RegenerateDisplayNames()
    {
        var orderedStages = _stages.OrderBy(s => s.Position).ToList();

        var typeGroups = orderedStages
            .GroupBy(s => s.StageType)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var (stageType, stages) in typeGroups)
        {
            if (!StageBaseNames.TryGetValue(stageType, out var baseName))
            {
                continue;
            }

            if (stages.Count == 1)
            {
                stages[0].Rename(baseName);
            }
            else
            {
                for (var i = 0; i < stages.Count; i++)
                {
                    stages[i].Rename($"{baseName} {i + 1}");
                }
            }
        }
    }
}
