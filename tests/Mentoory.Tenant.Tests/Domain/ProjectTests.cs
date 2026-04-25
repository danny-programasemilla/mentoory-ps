using FluentAssertions;
using Mentoory.Tenant.Domain.Aggregates.Project;
using Mentoory.Tenant.Domain.Enums;
using Xunit;

namespace Mentoory.Tenant.Tests.Domain;

public class ProjectTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_ShouldInitializeAllSevenStages()
    {
        var project = Project.Create(1, "Test Project", null, UtcNow);

        project.Stages.Should().HaveCount(7);
        project.CurrentStageType.Should().Be(StageType.Registration);
        project.CurrentStageState.Should().Be(StageState.InProgress);
    }

    [Fact]
    public void Create_ShouldSetRegistrationStageAsInProgress()
    {
        var project = Project.Create(1, "Test", null, UtcNow);
        var registrationStage = project.Stages.Single(s => s.StageType == StageType.Registration);
        registrationStage.State.Should().Be(StageState.InProgress);
    }

    [Fact]
    public void AdvanceStage_ShouldCompleteCurrentAndStartNext()
    {
        var project = Project.Create(1, "Test", null, UtcNow);
        project.AdvanceStage(1, UtcNow.AddDays(1));

        project.CurrentStageType.Should().Be(StageType.Forms);
        project.CurrentStageState.Should().Be(StageState.InProgress);
    }

    [Fact]
    public void AdvanceStage_RecordsAuditOnCurrentAndNextStage()
    {
        var project = Project.Create(1, "Test", null, UtcNow);
        var advancedAt = UtcNow.AddHours(2);

        project.AdvanceStage(42, advancedAt);

        var completedRegistration = project.Stages.Single(s => s.StageType == StageType.Registration);
        completedRegistration.State.Should().Be(StageState.Completed);
        completedRegistration.CompletedAtUtc.Should().Be(advancedAt);

        var forms = project.Stages.Single(s => s.StageType == StageType.Forms);
        forms.State.Should().Be(StageState.InProgress);
        forms.StartedAtUtc.Should().Be(advancedAt);
        forms.AdvancedByUserId.Should().Be(42);
    }

    [Fact]
    public void AdvanceStage_AtFinalStage_StopsInClosureWithCompletedState()
    {
        var project = Project.Create(1, "Test", null, UtcNow);
        for (var i = 0; i < 6; i++)
        {
            project.AdvanceStage(1, UtcNow.AddHours(i + 1));
        }

        project.CurrentStageType.Should().Be(StageType.Closure);
        project.CurrentStageState.Should().Be(StageState.InProgress);

        project.AdvanceStage(1, UtcNow.AddHours(10));

        project.CurrentStageType.Should().Be(StageType.Closure);
        project.CurrentStageState.Should().Be(StageState.Completed);
    }

    [Fact]
    public void AdvanceStage_WhenStageNotInProgress_Throws()
    {
        var project = Project.Create(1, "Test", null, UtcNow);
        for (var i = 0; i < 7; i++)
        {
            project.AdvanceStage(1, UtcNow.AddHours(i + 1));
        }

        project.CurrentStageState.Should().Be(StageState.Completed);

        var act = () => project.AdvanceStage(1, UtcNow.AddHours(20));
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void EnrollParticipant_ShouldAddToCollection()
    {
        var project = Project.Create(1, "Test", null, UtcNow);
        var participant = project.EnrollParticipant(5, "Entrepreneur", UtcNow);

        project.Participants.Should().HaveCount(1);
        participant.UserId.Should().Be(5);
        participant.Role.Should().Be("Entrepreneur");
    }

    [Fact]
    public void AssignMentor_WithLeadFlag_ShouldRemovePreviousLead()
    {
        var project = Project.Create(1, "Test", null, UtcNow);
        project.AssignMentor(10, 5, true, UtcNow);
        project.AssignMentor(11, 5, true, UtcNow.AddHours(1));

        project.MentorAssignments.Should().HaveCount(2);
        project.MentorAssignments.Count(ma => ma.IsLeadMentor).Should().Be(1);
        project.MentorAssignments.Single(ma => ma.IsLeadMentor).MentorUserId.Should().Be(11);
    }
}
