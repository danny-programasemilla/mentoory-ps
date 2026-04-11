using Mentoory.Tenant.Domain.Enums;
using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Tenant.Domain.Aggregates.Project;

public class ProjectStage : Entity
{
    private ProjectStage()
    {
    }

    public StageType StageType { get; private set; }
    public StageState State { get; private set; }
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public long? AdvancedByUserId { get; private set; }

    public static ProjectStage Create(StageType stageType, StageState initialState, DateTime? startedAtUtc)
    {
        return new ProjectStage
        {
            StageType = stageType,
            State = initialState,
            StartedAtUtc = startedAtUtc,
        };
    }

    public void Start(long advancedByUserId, DateTime utcNow)
    {
        if (State != StageState.NotStarted)
        {
            throw new InvalidOperationException($"Stage {StageType} cannot be started from state {State}.");
        }

        State = StageState.InProgress;
        StartedAtUtc = utcNow;
        AdvancedByUserId = advancedByUserId;
    }

    public void Complete(DateTime utcNow)
    {
        if (State != StageState.InProgress)
        {
            throw new InvalidOperationException($"Stage {StageType} cannot be completed from state {State}.");
        }

        State = StageState.Completed;
        CompletedAtUtc = utcNow;
    }
}
