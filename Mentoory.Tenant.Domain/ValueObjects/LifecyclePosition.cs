using Mentoory.Shared.Domain.SeedWork;
using Mentoory.Tenant.Domain.Enums;

namespace Mentoory.Tenant.Domain.ValueObjects;

public class LifecyclePosition : ValueObject
{
    public LifecyclePosition(StageType currentStage, StageState currentState)
    {
        CurrentStage = currentStage;
        CurrentState = currentState;
    }

    public StageType CurrentStage { get; }
    public StageState CurrentState { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return CurrentStage;
        yield return CurrentState;
    }
}
