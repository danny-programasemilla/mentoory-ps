using FluentAssertions;
using Mentoory.Diagnostic.Domain.Aggregates.FormTemplate;
using Mentoory.Diagnostic.Domain.Aggregates.ProjectForm;
using Mentoory.Diagnostic.Domain.Enums;
using Xunit;

namespace Mentoory.Diagnostic.Tests.Domain;

public class ProjectFormTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void CloneFromTemplate_ShouldDeepCopyQuestions()
    {
        var template = CreateTemplateWithQuestions();

        var form = ProjectForm.CloneFromTemplate(template, projectId: 10, incubatorId: 1, UtcNow);

        form.Name.Should().Be("Template");
        form.ProjectId.Should().Be(10);
        form.IncubatorId.Should().Be(1);
        form.SourceTemplateId.Should().Be(template.Id);
        form.SourceTemplateVersion.Should().Be(template.Version);
        form.SyncMode.Should().Be(SyncMode.Disconnected);
        form.Questions.Should().HaveCount(2);
        form.ExternalId.Should().NotBeEmpty();
    }

    [Fact]
    public void CloneFromTemplate_ShouldCopyAnswerOptions()
    {
        var template = CreateTemplateWithQuestions();

        var form = ProjectForm.CloneFromTemplate(template, 10, 1, UtcNow);

        var firstQuestion = form.Questions.First();
        firstQuestion.AnswerOptions.Should().HaveCount(2);
        firstQuestion.AnswerOptions.First().OptionText.Should().Be("Opción A");
        firstQuestion.AnswerOptions.First().Score.Should().Be(5.0m);
    }

    [Fact]
    public void AddQuestion_ShouldAddToForm()
    {
        var form = ProjectForm.Create("Custom Form", 10, 1, UtcNow);

        var question = form.AddQuestion(1, "New Question", QuestionType.Numeric, 1, "Group1", false);

        form.Questions.Should().HaveCount(1);
        question.QuestionText.Should().Be("New Question");
        question.BlockGroup.Should().Be("Group1");
    }

    [Fact]
    public void RemoveQuestion_WhenNotFound_ShouldThrow()
    {
        var form = ProjectForm.Create("Form", 10, 1, UtcNow);

        var act = () => form.RemoveQuestion(999);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public void EnablePartialSync_WithoutTemplate_ShouldThrow()
    {
        var form = ProjectForm.Create("Form", 10, 1, UtcNow);

        var act = () => form.EnablePartialSync();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*without a source template*");
    }

    [Fact]
    public void AddFollowUpQuestion_ShouldAddToQuestion()
    {
        var form = ProjectForm.Create("Form", 10, 1, UtcNow);
        var question = form.AddQuestion(1, "Main?", QuestionType.Text, 1, null, false);

        var followUp = question.AddFollowUpQuestion("Follow-up?", 1);

        question.FollowUpQuestions.Should().HaveCount(1);
        followUp.QuestionText.Should().Be("Follow-up?");
    }

    private static FormTemplate CreateTemplateWithQuestions()
    {
        var template = FormTemplate.Create("Template", "Desc", null, UtcNow);
        var q1 = template.AddQuestion(1, "Pregunta 1", QuestionType.SingleSelect, 1, null, false);
        q1.AddAnswerOption("Opción A", 5.0m, SwotClassification.Strength, OdsrOrientation.Offensive, 1);
        q1.AddAnswerOption("Opción B", 3.0m, SwotClassification.Weakness, OdsrOrientation.Defensive, 2);
        template.AddQuestion(2, "Pregunta 2", QuestionType.Text, 2, null, true);
        return template;
    }
}
