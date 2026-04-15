using FluentAssertions;
using Mentoory.Diagnostic.Domain.Aggregates.StageFormAssignment;
using Xunit;

namespace Mentoory.Diagnostic.Tests.Domain;

public class StageFormAssignmentTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_ShouldInitializeAssignment()
    {
        var assignment = StageFormAssignment.Create(
            projectId: 10,
            incubatorId: 1,
            projectStageId: 5,
            projectFormId: 3,
            selectedQuestionIds: [100L, 200L, 300L],
            utcNow: UtcNow);

        assignment.ProjectId.Should().Be(10);
        assignment.IncubatorId.Should().Be(1);
        assignment.ProjectStageId.Should().Be(5);
        assignment.ProjectFormId.Should().Be(3);
        assignment.IsActive.Should().BeTrue();
        assignment.CreatedAtUtc.Should().Be(UtcNow);
        assignment.ExternalId.Should().NotBeEmpty();
        assignment.AssignedQuestions.Should().HaveCount(3);
    }

    [Fact]
    public void Create_ShouldAssignSortOrderSequentially()
    {
        var assignment = StageFormAssignment.Create(10, 1, 5, 3, [100L, 200L, 300L], UtcNow);

        var questions = assignment.AssignedQuestions.OrderBy(q => q.SortOrder).ToList();
        questions[0].QuestionId.Should().Be(100L);
        questions[0].SortOrder.Should().Be(0);
        questions[1].QuestionId.Should().Be(200L);
        questions[1].SortOrder.Should().Be(1);
        questions[2].QuestionId.Should().Be(300L);
        questions[2].SortOrder.Should().Be(2);
    }

    [Fact]
    public void Create_WithEmptyQuestionIds_ShouldThrow()
    {
        var act = () => StageFormAssignment.Create(10, 1, 5, 3, [], UtcNow);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*At least one question*");
    }

    [Fact]
    public void Create_WithNullQuestionIds_ShouldThrow()
    {
        var act = () => StageFormAssignment.Create(10, 1, 5, 3, null!, UtcNow);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*At least one question*");
    }

    [Fact]
    public void UpdateQuestionSelection_ShouldReplaceAllQuestions()
    {
        var assignment = StageFormAssignment.Create(10, 1, 5, 3, [100L, 200L], UtcNow);

        assignment.UpdateQuestionSelection([300L, 400L, 500L]);

        assignment.AssignedQuestions.Should().HaveCount(3);
        assignment.AssignedQuestions.Select(q => q.QuestionId).Should().BeEquivalentTo([300L, 400L, 500L]);
    }

    [Fact]
    public void UpdateQuestionSelection_ShouldResetSortOrder()
    {
        var assignment = StageFormAssignment.Create(10, 1, 5, 3, [100L], UtcNow);

        assignment.UpdateQuestionSelection([300L, 400L]);

        var questions = assignment.AssignedQuestions.OrderBy(q => q.SortOrder).ToList();
        questions[0].SortOrder.Should().Be(0);
        questions[1].SortOrder.Should().Be(1);
    }

    [Fact]
    public void UpdateQuestionSelection_WithEmptyList_ShouldThrow()
    {
        var assignment = StageFormAssignment.Create(10, 1, 5, 3, [100L], UtcNow);

        var act = () => assignment.UpdateQuestionSelection([]);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*At least one question*");
    }

    [Fact]
    public void UpdateQuestionSelection_WithDuplicates_ShouldThrow()
    {
        var assignment = StageFormAssignment.Create(10, 1, 5, 3, [100L], UtcNow);

        var act = () => assignment.UpdateQuestionSelection([200L, 200L]);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Duplicate*");
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveFalse()
    {
        var assignment = StageFormAssignment.Create(10, 1, 5, 3, [100L], UtcNow);

        assignment.Deactivate();

        assignment.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_ShouldBeIdempotent()
    {
        var assignment = StageFormAssignment.Create(10, 1, 5, 3, [100L], UtcNow);

        assignment.Deactivate();
        assignment.Deactivate();

        assignment.IsActive.Should().BeFalse();
    }
}
