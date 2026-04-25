using System.Collections.Frozen;
using Mentoory.Tenant.Domain.Enums;

namespace Mentoory.Access.Application.StageActions;

/// <summary>
/// Authoritative mapping between project lifecycle stages and the coordination-area actions
/// that are available within each stage. Consumed by the lifecycle query (for the UI) and by
/// <c>RequiresStageAttribute</c> (for server-side gating) to avoid drift between the two.
/// </summary>
public static class StageActionRegistry
{
    /// <summary>
    /// For each lifecycle stage, the set of actions available in that stage.
    /// </summary>
    public static readonly FrozenDictionary<StageType, IReadOnlySet<StageGatedAction>> ActionsByStage;

    private static readonly FrozenDictionary<StageGatedAction, StageType> GatingStageByAction;

    static StageActionRegistry()
    {
        GatingStageByAction = new Dictionary<StageGatedAction, StageType>
        {
            [StageGatedAction.DiagnosticForms] = StageType.Forms,
            [StageGatedAction.AnswerCorrection] = StageType.Analysis,
            [StageGatedAction.LearningAssignment] = StageType.LearningAssignment,
            [StageGatedAction.MentoringCoordination] = StageType.Mentoring,
            [StageGatedAction.FinalEvaluation] = StageType.FinalEvaluation,
            [StageGatedAction.Closure] = StageType.Closure,
        }.ToFrozenDictionary();

        ActionsByStage = BuildActionsByStage(GatingStageByAction);
    }

    /// <summary>
    /// Returns the stage during which the given action becomes available.
    /// </summary>
    public static StageType GetGatingStage(StageGatedAction action) => GatingStageByAction[action];

    /// <summary>
    /// Returns the state of <paramref name="action"/> for a project whose current stage is
    /// <paramref name="currentStage"/>. Uses the numeric order of <see cref="StageType"/>.
    /// </summary>
    public static StageGatedActionState GetState(StageType currentStage, StageGatedAction action)
    {
        var gatingStage = GatingStageByAction[action];
        if (currentStage == gatingStage)
        {
            return StageGatedActionState.Available;
        }

        return (int)currentStage < (int)gatingStage
            ? StageGatedActionState.Locked
            : StageGatedActionState.Past;
    }

    private static FrozenDictionary<StageType, IReadOnlySet<StageGatedAction>> BuildActionsByStage(
        FrozenDictionary<StageGatedAction, StageType> gatingByAction)
    {
        var mutable = new Dictionary<StageType, HashSet<StageGatedAction>>();
        foreach (StageType stage in Enum.GetValues<StageType>())
        {
            mutable[stage] = new HashSet<StageGatedAction>();
        }

        foreach (var (action, stage) in gatingByAction)
        {
            mutable[stage].Add(action);
        }

        return mutable.ToFrozenDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlySet<StageGatedAction>)kvp.Value);
    }
}
