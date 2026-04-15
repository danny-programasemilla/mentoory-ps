using FluentAssertions;
using Mentoory.Tenant.Domain.Aggregates.Project;
using Mentoory.Tenant.Domain.Enums;
using Xunit;

namespace Mentoory.Tenant.Tests.Domain;

public class ProjectTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_ShouldInitializeDefaultFiveStagePipeline()
    {
        var project = Project.Create(1, "Test Project", null, UtcNow);

        project.Stages.Should().HaveCount(5);
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
    public void Create_ShouldAssignCorrectPositionsAndDisplayNames()
    {
        var project = Project.Create(1, "Test", null, UtcNow);
        var ordered = project.Stages.OrderBy(s => s.Position).ToList();

        ordered[0].StageType.Should().Be(StageType.Registration);
        ordered[0].Position.Should().Be(0);
        ordered[0].DisplayName.Should().Be("Registro");

        ordered[1].StageType.Should().Be(StageType.Diagnosis);
        ordered[1].Position.Should().Be(1);
        ordered[1].DisplayName.Should().Be("Diagnóstico 1");

        ordered[2].StageType.Should().Be(StageType.Mentorship);
        ordered[2].Position.Should().Be(2);
        ordered[2].DisplayName.Should().Be("Mentoría");

        ordered[3].StageType.Should().Be(StageType.Diagnosis);
        ordered[3].Position.Should().Be(3);
        ordered[3].DisplayName.Should().Be("Diagnóstico 2");

        ordered[4].StageType.Should().Be(StageType.Closure);
        ordered[4].Position.Should().Be(4);
        ordered[4].DisplayName.Should().Be("Cierre");
    }

    [Fact]
    public void Create_ShouldAssignExternalIdToAllStages()
    {
        var project = Project.Create(1, "Test", null, UtcNow);

        foreach (var stage in project.Stages)
        {
            stage.ExternalId.Should().NotBeEmpty();
        }

        project.Stages.Select(s => s.ExternalId).Distinct().Should().HaveCount(5);
    }

    [Fact]
    public void AdvanceStage_ShouldCompleteCurrentAndStartNext()
    {
        var project = Project.Create(1, "Test", null, UtcNow);
        project.AdvanceStage(1, UtcNow.AddDays(1));

        project.CurrentStageType.Should().Be(StageType.Diagnosis);
        project.CurrentStageState.Should().Be(StageState.InProgress);
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
