using FluentAssertions;
using Mentoory.Diagnostic.Domain.Aggregates.FormTemplate;
using Mentoory.Diagnostic.Domain.Aggregates.ProjectForm;
using Mentoory.Diagnostic.Domain.Enums;
using Xunit;

namespace Mentoory.Diagnostic.Tests.Domain;

public class ProjectFormSyncTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void SyncNewQuestionsFromTemplate_ShouldAddOnlyNewQuestions()
    {
        var template = CreateTemplateWithQuestions();
        var form = ProjectForm.CloneFromTemplate(template, 10, 1, UtcNow);
        form.EnablePartialSync();

        // Add a new question to the template
        template.AddQuestion(3, "Pregunta 3", QuestionType.Numeric, StageApplicability.Final, 3, null, false);

        form.SyncNewQuestionsFromTemplate(template);

        form.Questions.Should().HaveCount(3);
        form.Questions.Should().Contain(q => q.QuestionText == "Pregunta 3");
    }

    [Fact]
    public void SyncNewQuestionsFromTemplate_ShouldNotDuplicateExistingQuestions()
    {
        var template = CreateTemplateWithQuestions();
        var form = ProjectForm.CloneFromTemplate(template, 10, 1, UtcNow);
        form.EnablePartialSync();

        // Sync without adding new questions — should remain the same
        form.SyncNewQuestionsFromTemplate(template);

        form.Questions.Should().HaveCount(2);
    }

    [Fact]
    public void SyncNewQuestionsFromTemplate_ShouldUpdateSourceTemplateVersion()
    {
        var template = CreateTemplateWithQuestions();
        var form = ProjectForm.CloneFromTemplate(template, 10, 1, UtcNow);
        form.EnablePartialSync();

        template.Update("Template v2", null, null);
        template.AddQuestion(3, "Pregunta 3", QuestionType.Text, StageApplicability.Both, 3, null, false);

        form.SyncNewQuestionsFromTemplate(template);

        form.SourceTemplateVersion.Should().Be(template.Version);
    }

    [Fact]
    public void SyncNewQuestionsFromTemplate_NotInPartialSyncMode_ShouldThrow()
    {
        var template = CreateTemplateWithQuestions();
        var form = ProjectForm.CloneFromTemplate(template, 10, 1, UtcNow);

        // Form is in Disconnected mode by default
        var act = () => form.SyncNewQuestionsFromTemplate(template);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not in partial sync mode*");
    }

    [Fact]
    public void SyncNewQuestionsFromTemplate_WithFormCreatedWithoutTemplate_ShouldThrowOnEnablePartialSync()
    {
        // A form created without a template cannot enable sync
        var form = ProjectForm.Create("Custom Form", 10, 1, UtcNow);

        var act = () => form.EnablePartialSync();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*without a source template*");
    }

    [Fact]
    public void SyncNewQuestionsFromTemplate_ShouldAssignCorrectSortOrders()
    {
        var template = CreateTemplateWithQuestions();
        var form = ProjectForm.CloneFromTemplate(template, 10, 1, UtcNow);
        form.EnablePartialSync();

        template.AddQuestion(3, "Pregunta 3", QuestionType.Numeric, StageApplicability.Final, 3, null, false);
        template.AddQuestion(4, "Pregunta 4", QuestionType.Text, StageApplicability.Both, 4, null, false);

        form.SyncNewQuestionsFromTemplate(template);

        var newQuestions = form.Questions
            .Where(q => q.QuestionText is "Pregunta 3" or "Pregunta 4")
            .OrderBy(q => q.SortOrder)
            .ToList();

        newQuestions.Should().HaveCount(2);
        newQuestions[0].SortOrder.Should().BeGreaterThan(0);
        newQuestions[1].SortOrder.Should().BeGreaterThan(newQuestions[0].SortOrder);
    }

    [Fact]
    public void DisableSync_ShouldSetDisconnected()
    {
        var template = CreateTemplateWithQuestions();
        var form = ProjectForm.CloneFromTemplate(template, 10, 1, UtcNow);
        form.EnablePartialSync();

        form.DisableSync();

        form.SyncMode.Should().Be(SyncMode.Disconnected);
    }

    [Fact]
    public void Create_WithNullName_ShouldThrow()
    {
        var act = () => ProjectForm.Create(null!, 10, 1, UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrow()
    {
        var act = () => ProjectForm.Create("  ", 10, 1, UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    private static FormTemplate CreateTemplateWithQuestions()
    {
        var template = FormTemplate.Create("Template", "Desc", null, UtcNow);
        var q1 = template.AddQuestion(1, "Pregunta 1", QuestionType.SingleSelect, StageApplicability.Both, 1, null, false);
        q1.AddAnswerOption("Opción A", 5.0m, SwotClassification.Strength, OdsrOrientation.Offensive, 1);
        q1.AddAnswerOption("Opción B", 3.0m, SwotClassification.Weakness, OdsrOrientation.Defensive, 2);
        template.AddQuestion(2, "Pregunta 2", QuestionType.Text, StageApplicability.Initial, 2, null, true);
        return template;
    }
}
