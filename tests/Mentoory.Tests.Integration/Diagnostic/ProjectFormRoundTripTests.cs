using FluentAssertions;
using Mentoory.Diagnostic.Domain.Aggregates.FormTemplate;
using Mentoory.Diagnostic.Domain.Aggregates.ProjectForm;
using Mentoory.Diagnostic.Domain.Enums;
using Mentoory.Diagnostic.Infrastructure.Persistence;
using Mentoory.Tests.Integration.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Mentoory.Tests.Integration.Diagnostic;

[Collection(IntegrationTestCollection.Name)]
public class ProjectFormRoundTripTests : IntegrationTestBase
{
    public ProjectFormRoundTripTests(MentooryWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task CloneFromTemplate_Save_Reload_ShouldHaveAllQuestions()
    {
        Guid formExternalId;

        // Create template and clone
        using (var scope = CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<DiagnosticDbContext>();

            var template = FormTemplate.Create("Template", "Desc", null, DateTime.UtcNow);
            var q1 = template.AddQuestion(1, "Pregunta 1", QuestionType.SingleSelect, StageApplicability.Both, 1, null, false);
            q1.AddAnswerOption("Opción A", 5.0m, SwotClassification.Strength, OdsrOrientation.Offensive, 1);
            q1.AddAnswerOption("Opción B", 3.0m, SwotClassification.Weakness, OdsrOrientation.Defensive, 2);
            template.AddQuestion(2, "Pregunta 2", QuestionType.Text, StageApplicability.Initial, 2, null, true);

            dbContext.FormTemplates.Add(template);
            await dbContext.SaveChangesAsync();

            var form = ProjectForm.CloneFromTemplate(template, 10, 1, DateTime.UtcNow);
            dbContext.ProjectForms.Add(form);
            await dbContext.SaveChangesAsync();
            formExternalId = form.ExternalId;
        }

        // Reload and verify
        using (var scope = CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<DiagnosticDbContext>();
            var loaded = await dbContext.ProjectForms
                .Include(f => f.Questions)
                    .ThenInclude(q => q.AnswerOptions)
                .Include(f => f.Questions)
                    .ThenInclude(q => q.FollowUpQuestions)
                .FirstAsync(f => f.ExternalId == formExternalId);

            loaded.ProjectId.Should().Be(10);
            loaded.IncubatorId.Should().Be(1);
            loaded.SyncMode.Should().Be(SyncMode.Disconnected);
            loaded.Questions.Should().HaveCount(2);

            var selectQuestion = loaded.Questions.First(q => q.QuestionType == QuestionType.SingleSelect);
            selectQuestion.AnswerOptions.Should().HaveCount(2);
            selectQuestion.AnswerOptions.Should().Contain(ao => ao.Score == 5.0m);
        }
    }

    [Fact]
    public async Task AddQuestion_Save_Reload_ShouldPersistNewQuestion()
    {
        Guid formExternalId;

        using (var scope = CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<DiagnosticDbContext>();
            var form = ProjectForm.Create("Custom Form", 10, 1, DateTime.UtcNow);
            form.AddQuestion(1, "Custom Q1", QuestionType.Numeric, StageApplicability.Final, 1, "GroupA", false);
            dbContext.ProjectForms.Add(form);
            await dbContext.SaveChangesAsync();
            formExternalId = form.ExternalId;
        }

        using (var scope = CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<DiagnosticDbContext>();
            var loaded = await dbContext.ProjectForms
                .Include(f => f.Questions)
                .FirstAsync(f => f.ExternalId == formExternalId);

            loaded.Questions.Should().HaveCount(1);
            loaded.Questions.First().QuestionText.Should().Be("Custom Q1");
            loaded.Questions.First().BlockGroup.Should().Be("GroupA");
        }
    }
}
