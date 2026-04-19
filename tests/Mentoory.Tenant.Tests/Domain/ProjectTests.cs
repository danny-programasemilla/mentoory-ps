using FluentAssertions;
using Mentoory.Tenant.Domain.Aggregates.Project;
using Mentoory.Tenant.Domain.Enums;
using Xunit;

namespace Mentoory.Tenant.Tests.Domain;

public class ProjectTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid KsTemplateId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void Create_ShouldInitializeAllSevenStages()
    {
        var project = Project.Create(1, "Test Project", null, KsTemplateId, UtcNow);

        project.Stages.Should().HaveCount(7);
        project.CurrentStageType.Should().Be(StageType.Registration);
        project.CurrentStageState.Should().Be(StageState.InProgress);
    }

    [Fact]
    public void Create_ShouldSetRegistrationStageAsInProgress()
    {
        var project = Project.Create(1, "Test", null, KsTemplateId, UtcNow);
        var registrationStage = project.Stages.Single(s => s.StageType == StageType.Registration);
        registrationStage.State.Should().Be(StageState.InProgress);
    }

    [Fact]
    public void Create_WithEmptyKsTemplate_ShouldThrow()
    {
        var act = () => Project.Create(1, "Test", null, Guid.Empty, UtcNow);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ShouldStampKsTemplateExternalId()
    {
        var project = Project.Create(1, "Test", null, KsTemplateId, UtcNow);
        project.KnowledgeStructureTemplateExternalId.Should().Be(KsTemplateId);
    }

    [Fact]
    public void AdvanceStage_ShouldCompleteCurrentAndStartNext()
    {
        var project = Project.Create(1, "Test", null, KsTemplateId, UtcNow);
        project.AdvanceStage(1, UtcNow.AddDays(1));

        project.CurrentStageType.Should().Be(StageType.Forms);
        project.CurrentStageState.Should().Be(StageState.InProgress);
    }

    [Fact]
    public void EnrollParticipant_ShouldAddToCollection()
    {
        var project = Project.Create(1, "Test", null, KsTemplateId, UtcNow);
        var participant = project.EnrollParticipant(5, "Entrepreneur", UtcNow);

        project.Participants.Should().HaveCount(1);
        participant.UserId.Should().Be(5);
        participant.Role.Should().Be("Entrepreneur");
    }

    [Fact]
    public void AssignMentor_WithLeadFlag_ShouldRemovePreviousLead()
    {
        var project = Project.Create(1, "Test", null, KsTemplateId, UtcNow);
        project.AssignMentor(10, 5, true, UtcNow);
        project.AssignMentor(11, 5, true, UtcNow.AddHours(1));

        project.MentorAssignments.Should().HaveCount(2);
        project.MentorAssignments.Count(ma => ma.IsLeadMentor).Should().Be(1);
        project.MentorAssignments.Single(ma => ma.IsLeadMentor).MentorUserId.Should().Be(11);
    }
}
