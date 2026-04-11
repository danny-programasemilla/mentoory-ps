using FluentAssertions;
using Mentoory.Diagnostic.Domain.Aggregates.FormTemplate;
using Mentoory.Diagnostic.Domain.Enums;
using Xunit;

namespace Mentoory.Diagnostic.Tests.Domain;

public class FormTemplateTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_ShouldInitializeTemplate()
    {
        var template = FormTemplate.Create("Diagnóstico Inicial", "Descripción", "Basic", UtcNow);

        template.Name.Should().Be("Diagnóstico Inicial");
        template.Description.Should().Be("Descripción");
        template.SubscriptionTier.Should().Be("Basic");
        template.Version.Should().Be(1);
        template.IsActive.Should().BeTrue();
        template.ExternalId.Should().NotBeEmpty();
        template.CreatedAtUtc.Should().Be(UtcNow);
        template.Questions.Should().BeEmpty();
    }

    [Fact]
    public void Update_ShouldIncrementVersion()
    {
        var template = FormTemplate.Create("Template", null, null, UtcNow);
        template.Update("Updated", "Desc", "Premium");

        template.Name.Should().Be("Updated");
        template.Description.Should().Be("Desc");
        template.SubscriptionTier.Should().Be("Premium");
        template.Version.Should().Be(2);
    }

    [Fact]
    public void AddQuestion_ShouldAddToCollection()
    {
        var template = FormTemplate.Create("Template", null, null, UtcNow);

        var question = template.AddQuestion(
            topicId: 1,
            questionText: "¿Cuál es su experiencia?",
            questionType: QuestionType.Text,
            stageApplicability: StageApplicability.Both,
            sortOrder: 1,
            blockGroup: null,
            isOptional: false);

        template.Questions.Should().HaveCount(1);
        question.QuestionText.Should().Be("¿Cuál es su experiencia?");
        question.QuestionType.Should().Be(QuestionType.Text);
        question.TopicId.Should().Be(1);
    }

    [Fact]
    public void Deactivate_ShouldSetInactive()
    {
        var template = FormTemplate.Create("Template", null, null, UtcNow);

        template.Deactivate();

        template.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_ShouldSetActive()
    {
        var template = FormTemplate.Create("Template", null, null, UtcNow);
        template.Deactivate();

        template.Activate();

        template.IsActive.Should().BeTrue();
    }

    [Fact]
    public void AddAnswerOption_ShouldAddToQuestion()
    {
        var template = FormTemplate.Create("Template", null, null, UtcNow);
        var question = template.AddQuestion(1, "Test?", QuestionType.SingleSelect, StageApplicability.Initial, 1, null, false);

        var option = question.AddAnswerOption("Opción A", 5.0m, SwotClassification.Strength, OdsrOrientation.Offensive, 1);

        question.AnswerOptions.Should().HaveCount(1);
        option.OptionText.Should().Be("Opción A");
        option.Score.Should().Be(5.0m);
        option.SwotClassification.Should().Be(SwotClassification.Strength);
        option.OdsrOrientation.Should().Be(OdsrOrientation.Offensive);
    }
}
