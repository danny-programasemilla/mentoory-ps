using FluentAssertions;
using Mentoory.Access.Application.StageActions;
using Mentoory.Tenant.Domain.Enums;
using Xunit;

namespace Mentoory.Access.Tests.StageActions;

public class StageActionRegistryTests
{
    public static IEnumerable<object[]> AllStagesAndActions()
    {
        foreach (StageType stage in Enum.GetValues<StageType>())
        {
            foreach (StageGatedAction action in Enum.GetValues<StageGatedAction>())
            {
                yield return new object[] { stage, action };
            }
        }
    }

    [Theory]
    [MemberData(nameof(AllStagesAndActions))]
    public void GetState_ShouldMatchNumericOrderOfStages(StageType currentStage, StageGatedAction action)
    {
        var gatingStage = StageActionRegistry.GetGatingStage(action);

        var expected = (int)currentStage == (int)gatingStage
            ? StageGatedActionState.Available
            : (int)currentStage < (int)gatingStage
                ? StageGatedActionState.Locked
                : StageGatedActionState.Past;

        StageActionRegistry.GetState(currentStage, action).Should().Be(expected);
    }

    [Theory]
    [InlineData(StageGatedAction.DiagnosticForms, StageType.Forms)]
    [InlineData(StageGatedAction.AnswerCorrection, StageType.Analysis)]
    [InlineData(StageGatedAction.LearningAssignment, StageType.LearningAssignment)]
    [InlineData(StageGatedAction.MentoringCoordination, StageType.Mentoring)]
    [InlineData(StageGatedAction.FinalEvaluation, StageType.FinalEvaluation)]
    [InlineData(StageGatedAction.Closure, StageType.Closure)]
    public void GetGatingStage_ReturnsExpectedStage(StageGatedAction action, StageType expected)
    {
        StageActionRegistry.GetGatingStage(action).Should().Be(expected);
    }

    [Fact]
    public void ActionsByStage_ContainsAnEntryForEveryStage()
    {
        foreach (StageType stage in Enum.GetValues<StageType>())
        {
            StageActionRegistry.ActionsByStage.Should().ContainKey(stage);
        }
    }

    [Fact]
    public void ActionsByStage_RegistrationStageHasNoActions()
    {
        StageActionRegistry.ActionsByStage[StageType.Registration].Should().BeEmpty();
    }

    [Fact]
    public void ActionsByStage_DistributionMatchesGatingStage()
    {
        foreach (StageGatedAction action in Enum.GetValues<StageGatedAction>())
        {
            var gatingStage = StageActionRegistry.GetGatingStage(action);
            StageActionRegistry.ActionsByStage[gatingStage].Should().Contain(action);
        }
    }
}
