using Mentoory.Tenant.Domain.Enums;
using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Tenant.Domain.Aggregates.Project;

public class ProjectStage : Entity
{
    private ProjectStage()
    {
    }

    public Guid ExternalId { get; private set; }
    public StageType StageType { get; private set; }
    public StageState State { get; private set; }
    public int Position { get; private set; }
    public string DisplayName { get; private set; } = null!;
    public DateTime? PlannedStartDate { get; private set; }
    public DateTime? PlannedEndDate { get; private set; }
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public long? AdvancedByUserId { get; private set; }

    internal static ProjectStage Create(
        StageType stageType,
        StageState initialState,
        int position,
        string displayName,
        DateTime? startedAtUtc)
    {
        return new ProjectStage
        {
            ExternalId = Guid.NewGuid(),
            StageType = stageType,
            State = initialState,
            Position = position,
            DisplayName = displayName,
            StartedAtUtc = startedAtUtc,
        };
    }

    internal void Start(long advancedByUserId, DateTime utcNow)
    {
        if (State != StageState.NotStarted)
        {
            throw new InvalidOperationException($"Stage '{DisplayName}' cannot be started from state {State}.");
        }

        State = StageState.InProgress;
        StartedAtUtc = utcNow;
        AdvancedByUserId = advancedByUserId;
    }

    internal void Complete(DateTime utcNow)
    {
        if (State != StageState.InProgress)
        {
            throw new InvalidOperationException($"Stage '{DisplayName}' cannot be completed from state {State}.");
        }

        State = StageState.Completed;
        CompletedAtUtc = utcNow;
    }

    internal void SetPosition(int position)
    {
        Position = position;
    }

    internal void Rename(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Display name is required.", nameof(displayName));
        }

        DisplayName = displayName.Trim();
    }

    internal void SetPlannedDates(DateTime? plannedStartDate, DateTime? plannedEndDate)
    {
        PlannedStartDate = plannedStartDate;
        PlannedEndDate = plannedEndDate;
    }
}
