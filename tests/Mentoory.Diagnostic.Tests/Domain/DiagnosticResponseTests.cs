using FluentAssertions;
using Mentoory.Diagnostic.Domain.Aggregates.DiagnosticResponse;
using Xunit;

namespace Mentoory.Diagnostic.Tests.Domain;

public class DiagnosticResponseTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_ShouldInitializeResponse()
    {
        var response = DiagnosticResponse.Create(
            projectFormId: 1,
            projectId: 10,
            incubatorId: 1,
            entrepreneurUserId: 100,
            stageFormAssignmentId: 1L,
            utcNow: UtcNow);

        response.ProjectFormId.Should().Be(1);
        response.ProjectId.Should().Be(10);
        response.IncubatorId.Should().Be(1);
        response.EntrepreneurUserId.Should().Be(100);
        response.StageFormAssignmentId.Should().Be(1L);
        response.IsCompleted.Should().BeFalse();
        response.CompletedAtUtc.Should().BeNull();
        response.ExternalId.Should().NotBeEmpty();
        response.QuestionResponses.Should().BeEmpty();
    }

    [Fact]
    public void AddResponse_ShouldAddQuestionResponse()
    {
        var response = DiagnosticResponse.Create(1, 10, 1, 100, 1L, UtcNow);

        var qr = response.AddResponse(questionId: 1, textValue: "My answer", numericValue: null, selectedOptionIds: null, UtcNow);

        response.QuestionResponses.Should().HaveCount(1);
        qr.QuestionId.Should().Be(1);
        qr.TextValue.Should().Be("My answer");
    }

    [Fact]
    public void AddResponse_DuplicateQuestion_ShouldThrow()
    {
        var response = DiagnosticResponse.Create(1, 10, 1, 100, 1L, UtcNow);
        response.AddResponse(1, "Answer", null, null, UtcNow);

        var act = () => response.AddResponse(1, "Another", null, null, UtcNow);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public void AddResponse_WhenCompleted_ShouldThrow()
    {
        var response = DiagnosticResponse.Create(1, 10, 1, 100, 1L, UtcNow);
        response.MarkAsCompleted(UtcNow);

        var act = () => response.AddResponse(1, "Answer", null, null, UtcNow);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*completed*");
    }

    [Fact]
    public void MarkAsCompleted_ShouldSetCompletedState()
    {
        var response = DiagnosticResponse.Create(1, 10, 1, 100, 1L, UtcNow);
        var completedAt = UtcNow.AddHours(1);

        response.MarkAsCompleted(completedAt);

        response.IsCompleted.Should().BeTrue();
        response.CompletedAtUtc.Should().Be(completedAt);
    }

    [Fact]
    public void MarkAsCompleted_WhenAlreadyCompleted_ShouldThrow()
    {
        var response = DiagnosticResponse.Create(1, 10, 1, 100, 1L, UtcNow);
        response.MarkAsCompleted(UtcNow);

        var act = () => response.MarkAsCompleted(UtcNow.AddHours(1));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already completed*");
    }

    [Fact]
    public void AddResponse_WithSelectedOptions_ShouldStoreOptionIds()
    {
        var response = DiagnosticResponse.Create(1, 10, 1, 100, 1L, UtcNow);

        var qr = response.AddResponse(1, null, null, new List<long> { 10, 20, 30 }, UtcNow);

        qr.SelectedOptionIds.Should().BeEquivalentTo(new long[] { 10, 20, 30 });
    }
}
