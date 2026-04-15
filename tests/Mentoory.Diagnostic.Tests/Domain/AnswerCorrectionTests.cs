using FluentAssertions;
using Mentoory.Diagnostic.Domain.Aggregates.DiagnosticResponse;
using Xunit;

namespace Mentoory.Diagnostic.Tests.Domain;

public class AnswerCorrectionTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void CorrectAnswer_ShouldRecordPreviousValue()
    {
        var response = CreateResponseWithAnswer();
        var qr = response.QuestionResponses.First();

        var correction = response.CorrectAnswer(
            questionResponseId: qr.Id,
            newTextValue: "Corrected answer",
            newNumericValue: null,
            newSelectedOptionIds: null,
            correctedByUserId: 200,
            reason: "Error de digitación",
            utcNow: UtcNow.AddHours(1));

        correction.PreviousTextValue.Should().Be("Original answer");
        correction.CorrectedByUserId.Should().Be(200);
        correction.Reason.Should().Be("Error de digitación");
        correction.CorrectedAtUtc.Should().Be(UtcNow.AddHours(1));
    }

    [Fact]
    public void CorrectAnswer_ShouldUpdateCurrentValue()
    {
        var response = CreateResponseWithAnswer();
        var qr = response.QuestionResponses.First();

        response.CorrectAnswer(qr.Id, "New value", null, null, 200, "Fix", UtcNow.AddHours(1));

        qr.TextValue.Should().Be("New value");
    }

    [Fact]
    public void CorrectAnswer_ShouldMaintainCorrectionHistory()
    {
        var response = CreateResponseWithAnswer();
        var qr = response.QuestionResponses.First();

        response.CorrectAnswer(qr.Id, "Value 2", null, null, 200, "First fix", UtcNow.AddHours(1));
        response.CorrectAnswer(qr.Id, "Value 3", null, null, 300, "Second fix", UtcNow.AddHours(2));

        qr.Corrections.Should().HaveCount(2);
        qr.TextValue.Should().Be("Value 3");
    }

    [Fact]
    public void CorrectAnswer_WithInvalidResponseId_ShouldThrow()
    {
        var response = CreateResponseWithAnswer();

        var act = () => response.CorrectAnswer(999, "New", null, null, 200, "Fix", UtcNow);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public void CorrectAnswer_WithSelectedOptions_ShouldRecordPreviousSelections()
    {
        var response = DiagnosticResponse.Create(1, 10, 1, 100, 1L, UtcNow);
        response.AddResponse(1, null, null, new List<long> { 10, 20 }, UtcNow);
        var qr = response.QuestionResponses.First();

        var correction = response.CorrectAnswer(
            qr.Id, null, null, new List<long> { 30, 40 }, 200, "Changed selection", UtcNow.AddHours(1));

        correction.PreviousSelectedOptionIds.Should().Be("10,20");
        qr.SelectedOptionIds.Should().BeEquivalentTo(new long[] { 30, 40 });
    }

    private static DiagnosticResponse CreateResponseWithAnswer()
    {
        var response = DiagnosticResponse.Create(1, 10, 1, 100, 1L, UtcNow);
        response.AddResponse(1, "Original answer", null, null, UtcNow);
        return response;
    }
}
