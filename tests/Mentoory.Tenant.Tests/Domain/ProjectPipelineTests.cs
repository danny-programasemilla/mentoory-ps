using FluentAssertions;
using Mentoory.Tenant.Domain.Aggregates.Project;
using Mentoory.Tenant.Domain.Enums;
using Xunit;

namespace Mentoory.Tenant.Tests.Domain;

public class ProjectPipelineTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_ShouldHaveDefaultFiveStagePipeline()
    {
        var project = CreateDefaultProject();

        project.Stages.Should().HaveCount(5);
        var ordered = project.Stages.OrderBy(s => s.Position).ToList();

        ordered[0].StageType.Should().Be(StageType.Registration);
        ordered[1].StageType.Should().Be(StageType.Diagnosis);
        ordered[2].StageType.Should().Be(StageType.Mentorship);
        ordered[3].StageType.Should().Be(StageType.Diagnosis);
        ordered[4].StageType.Should().Be(StageType.Closure);
    }

    [Fact]
    public void Create_RegistrationShouldBeInProgress()
    {
        var project = CreateDefaultProject();
        var reg = project.Stages.Single(s => s.Position == 0);

        reg.State.Should().Be(StageState.InProgress);
        reg.StartedAtUtc.Should().Be(UtcNow);
    }

    [Fact]
    public void Create_AllNonRegistrationStagesShouldBeNotStarted()
    {
        var project = CreateDefaultProject();

        project.Stages
            .Where(s => s.Position > 0)
            .Should().AllSatisfy(s => s.State.Should().Be(StageState.NotStarted));
    }

    [Fact]
    public void Create_ShouldGenerateDisplayNamesWithOrdinals()
    {
        var project = CreateDefaultProject();
        var ordered = project.Stages.OrderBy(s => s.Position).ToList();

        ordered[0].DisplayName.Should().Be("Registro");
        ordered[1].DisplayName.Should().Be("Diagnóstico 1");
        ordered[2].DisplayName.Should().Be("Mentoría");
        ordered[3].DisplayName.Should().Be("Diagnóstico 2");
        ordered[4].DisplayName.Should().Be("Cierre");
    }

    [Fact]
    public void Create_ShouldAssignExternalIdToAllStages()
    {
        var project = CreateDefaultProject();

        foreach (var stage in project.Stages)
        {
            stage.ExternalId.Should().NotBeEmpty();
        }

        project.Stages.Select(s => s.ExternalId).Distinct().Should().HaveCount(5);
    }

    [Fact]
    public void AddStage_ShouldInsertAtPosition()
    {
        var project = CreateDefaultProject();

        var newStage = project.AddStage(StageType.Diagnosis, 2, UtcNow);

        project.Stages.Should().HaveCount(6);
        newStage.Position.Should().Be(2);
        newStage.StageType.Should().Be(StageType.Diagnosis);
        newStage.State.Should().Be(StageState.NotStarted);
    }

    [Fact]
    public void AddStage_ShouldShiftSubsequentPositions()
    {
        var project = CreateDefaultProject();
        project.AddStage(StageType.Mentorship, 2, UtcNow);

        var ordered = project.Stages.OrderBy(s => s.Position).ToList();
        ordered.Should().HaveCount(6);

        for (var i = 0; i < ordered.Count; i++)
        {
            ordered[i].Position.Should().Be(i);
        }
    }

    [Fact]
    public void AddStage_ShouldRegenerateDisplayNames()
    {
        var project = CreateDefaultProject();
        project.AddStage(StageType.Mentorship, 3, UtcNow);

        var mentorships = project.Stages.Where(s => s.StageType == StageType.Mentorship).OrderBy(s => s.Position).ToList();
        mentorships.Should().HaveCount(2);
        mentorships[0].DisplayName.Should().Be("Mentoría 1");
        mentorships[1].DisplayName.Should().Be("Mentoría 2");
    }

    [Fact]
    public void AddStage_RegistrationType_ShouldThrow()
    {
        var project = CreateDefaultProject();
        var act = () => project.AddStage(StageType.Registration, 1, UtcNow);
        act.Should().Throw<InvalidOperationException>().WithMessage("*Registration*");
    }

    [Fact]
    public void AddStage_ClosureType_ShouldThrow()
    {
        var project = CreateDefaultProject();
        var act = () => project.AddStage(StageType.Closure, 1, UtcNow);
        act.Should().Throw<InvalidOperationException>().WithMessage("*Closure*");
    }

    [Fact]
    public void AddStage_AtPositionZero_ShouldThrow()
    {
        var project = CreateDefaultProject();
        var act = () => project.AddStage(StageType.Diagnosis, 0, UtcNow);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void AddStage_AtLastPosition_ShouldThrow()
    {
        var project = CreateDefaultProject();
        var act = () => project.AddStage(StageType.Diagnosis, 5, UtcNow);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void RemoveStage_ShouldRemoveAndShiftPositions()
    {
        var project = CreateDefaultProject();
        var diagStage = project.Stages.Single(s => s.Position == 1);
        SetEntityId(diagStage, 10);

        project.RemoveStage(10, UtcNow);

        project.Stages.Should().HaveCount(4);
        var ordered = project.Stages.OrderBy(s => s.Position).ToList();
        for (var i = 0; i < ordered.Count; i++)
        {
            ordered[i].Position.Should().Be(i);
        }
    }

    [Fact]
    public void RemoveStage_ShouldRegenerateDisplayNames()
    {
        var project = CreateDefaultProject();
        var diagStage1 = project.Stages.Single(s => s.Position == 1);
        SetEntityId(diagStage1, 10);

        project.RemoveStage(10, UtcNow);

        var diagStages = project.Stages.Where(s => s.StageType == StageType.Diagnosis).ToList();
        diagStages.Should().HaveCount(1);
        diagStages[0].DisplayName.Should().Be("Diagnóstico");
    }

    [Fact]
    public void RemoveStage_RegistrationStage_ShouldThrow()
    {
        var project = CreateDefaultProject();
        var reg = project.Stages.Single(s => s.StageType == StageType.Registration);
        SetEntityId(reg, 1);

        var act = () => project.RemoveStage(1, UtcNow);
        act.Should().Throw<InvalidOperationException>().WithMessage("*Registration*");
    }

    [Fact]
    public void RemoveStage_ClosureStage_ShouldThrow()
    {
        var project = CreateDefaultProject();
        var closure = project.Stages.Single(s => s.StageType == StageType.Closure);
        SetEntityId(closure, 5);

        var act = () => project.RemoveStage(5, UtcNow);
        act.Should().Throw<InvalidOperationException>().WithMessage("*Closure*");
    }

    [Fact]
    public void RemoveStage_NonExistentId_ShouldThrow()
    {
        var project = CreateDefaultProject();
        var act = () => project.RemoveStage(999, UtcNow);
        act.Should().Throw<InvalidOperationException>().WithMessage("*not found*");
    }

    [Fact]
    public void ReorderStages_ShouldApplyNewPositions()
    {
        var project = CreateDefaultProject();
        var stages = project.Stages.OrderBy(s => s.Position).ToList();

        for (var i = 0; i < stages.Count; i++)
        {
            SetEntityId(stages[i], i + 1);
        }

        // Swap the two middle stages (Diagnosis1 at 1 and Mentorship at 2)
        var newOrder = new List<long> { 1, 3, 2, 4, 5 };
        project.ReorderStages(newOrder, UtcNow);

        var ordered = project.Stages.OrderBy(s => s.Position).ToList();
        ordered[0].Id.Should().Be(1);
        ordered[1].Id.Should().Be(3);
        ordered[2].Id.Should().Be(2);
        ordered[3].Id.Should().Be(4);
        ordered[4].Id.Should().Be(5);
    }

    [Fact]
    public void ReorderStages_FirstNotRegistration_ShouldThrow()
    {
        var project = CreateDefaultProject();
        var stages = project.Stages.OrderBy(s => s.Position).ToList();

        for (var i = 0; i < stages.Count; i++)
        {
            SetEntityId(stages[i], i + 1);
        }

        var newOrder = new List<long> { 2, 1, 3, 4, 5 };
        var act = () => project.ReorderStages(newOrder, UtcNow);
        act.Should().Throw<InvalidOperationException>().WithMessage("*Registration*");
    }

    [Fact]
    public void ReorderStages_LastNotClosure_ShouldThrow()
    {
        var project = CreateDefaultProject();
        var stages = project.Stages.OrderBy(s => s.Position).ToList();

        for (var i = 0; i < stages.Count; i++)
        {
            SetEntityId(stages[i], i + 1);
        }

        var newOrder = new List<long> { 1, 5, 2, 3, 4 };
        var act = () => project.ReorderStages(newOrder, UtcNow);
        act.Should().Throw<InvalidOperationException>().WithMessage("*Closure*");
    }

    [Fact]
    public void ReorderStages_WrongCount_ShouldThrow()
    {
        var project = CreateDefaultProject();
        var stages = project.Stages.OrderBy(s => s.Position).ToList();

        for (var i = 0; i < stages.Count; i++)
        {
            SetEntityId(stages[i], i + 1);
        }

        var act = () => project.ReorderStages(new List<long> { 1, 2 }, UtcNow);
        act.Should().Throw<ArgumentException>().WithMessage("*exactly one*");
    }

    [Fact]
    public void ReorderStages_InvalidId_ShouldThrow()
    {
        var project = CreateDefaultProject();
        var stages = project.Stages.OrderBy(s => s.Position).ToList();

        for (var i = 0; i < stages.Count; i++)
        {
            SetEntityId(stages[i], i + 1);
        }

        var act = () => project.ReorderStages(new List<long> { 1, 2, 999, 4, 5 }, UtcNow);
        act.Should().Throw<ArgumentException>().WithMessage("*not found*");
    }

    [Fact]
    public void RenameStage_ShouldUpdateDisplayName()
    {
        var project = CreateDefaultProject();
        var stage = project.Stages.First(s => s.Position == 1);
        SetEntityId(stage, 2);

        project.RenameStage(2, "Diagnóstico Inicial", UtcNow);

        stage.DisplayName.Should().Be("Diagnóstico Inicial");
    }

    [Fact]
    public void RenameStage_NonExistentId_ShouldThrow()
    {
        var project = CreateDefaultProject();
        var act = () => project.RenameStage(999, "New Name", UtcNow);
        act.Should().Throw<InvalidOperationException>().WithMessage("*not found*");
    }

    [Fact]
    public void RenameStage_EmptyName_ShouldThrow()
    {
        var project = CreateDefaultProject();
        var stage = project.Stages.First(s => s.Position == 1);
        SetEntityId(stage, 2);

        var act = () => project.RenameStage(2, string.Empty, UtcNow);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AdvanceStage_ShouldMoveToNextByPosition()
    {
        var project = CreateDefaultProject();

        project.AdvanceStage(1, UtcNow.AddDays(1));

        project.CurrentStageType.Should().Be(StageType.Diagnosis);
        project.CurrentStageState.Should().Be(StageState.InProgress);
    }

    [Fact]
    public void AdvanceStage_ThroughEntirePipeline_ShouldCompleteClosure()
    {
        var project = CreateDefaultProject();

        for (var i = 0; i < 4; i++)
        {
            project.AdvanceStage(1, UtcNow.AddDays(i + 1));
        }

        project.CurrentStageType.Should().Be(StageType.Closure);
        project.CurrentStageState.Should().Be(StageState.InProgress);

        project.AdvanceStage(1, UtcNow.AddDays(5));
        project.CurrentStageState.Should().Be(StageState.Completed);
    }

    [Fact]
    public void AdvanceStage_WhenNotInProgress_ShouldThrow()
    {
        var project = CreateDefaultProject();

        for (var i = 0; i < 5; i++)
        {
            project.AdvanceStage(1, UtcNow.AddDays(i + 1));
        }

        var act = () => project.AdvanceStage(1, UtcNow.AddDays(6));
        act.Should().Throw<InvalidOperationException>().WithMessage("*in progress*");
    }

    [Fact]
    public void AdvanceStage_ShouldRecordAdvancedByAndTimestamps()
    {
        var project = CreateDefaultProject();
        var advanceTime = UtcNow.AddDays(1);

        project.AdvanceStage(42, advanceTime);

        var completedReg = project.Stages.Single(s => s.StageType == StageType.Registration);
        completedReg.State.Should().Be(StageState.Completed);
        completedReg.CompletedAtUtc.Should().Be(advanceTime);

        var inProgressDiag = project.Stages.OrderBy(s => s.Position).First(s => s.State == StageState.InProgress);
        inProgressDiag.StartedAtUtc.Should().Be(advanceTime);
        inProgressDiag.AdvancedByUserId.Should().Be(42);
    }

    [Fact]
    public void Pipeline_ShouldAlwaysStartWithRegistrationAndEndWithClosure()
    {
        var project = CreateDefaultProject();
        var ordered = project.Stages.OrderBy(s => s.Position).ToList();

        ordered.First().StageType.Should().Be(StageType.Registration);
        ordered.Last().StageType.Should().Be(StageType.Closure);
    }

    private static Project CreateDefaultProject() => Project.Create(1, "Test", null, UtcNow);

    private static void SetEntityId(ProjectStage stage, long id)
    {
        var prop = typeof(ProjectStage).BaseType!.GetProperty("Id")!;
        prop.SetValue(stage, id);
    }
}
