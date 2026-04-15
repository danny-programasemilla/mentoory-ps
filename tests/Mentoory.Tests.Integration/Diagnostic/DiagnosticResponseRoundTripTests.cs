using FluentAssertions;
using Mentoory.Diagnostic.Domain.Aggregates.DiagnosticResponse;
using Mentoory.Diagnostic.Domain.Aggregates.FormTemplate;
using Mentoory.Diagnostic.Domain.Aggregates.ProjectForm;
using Mentoory.Diagnostic.Domain.Aggregates.StageFormAssignment;
using Mentoory.Diagnostic.Domain.Enums;
using Mentoory.Diagnostic.Infrastructure.Persistence;
using Mentoory.Tests.Integration.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Mentoory.Tests.Integration.Diagnostic;

[Collection(IntegrationTestCollection.Name)]
public class DiagnosticResponseRoundTripTests : IntegrationTestBase
{
    public DiagnosticResponseRoundTripTests(MentooryWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task SubmitResponse_Save_Reload_ShouldPersistAllData()
    {
        Guid responseExternalId;
        long projectFormId;
        long stageFormAssignmentId;

        // Create form + stage assignment
        using (var scope = CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<DiagnosticDbContext>();

            var template = FormTemplate.Create("Template", null, null, DateTime.UtcNow);
            var q = template.AddQuestion(1, "Q1", QuestionType.SingleSelect, 1, null, false);
            q.AddAnswerOption("A", 5.0m, SwotClassification.Strength, OdsrOrientation.Offensive, 1);
            dbContext.FormTemplates.Add(template);
            await dbContext.SaveChangesAsync();

            var form = ProjectForm.CloneFromTemplate(template, 10, 1, DateTime.UtcNow);
            dbContext.ProjectForms.Add(form);
            await dbContext.SaveChangesAsync();
            projectFormId = form.Id;

            var questionIds = form.Questions.Select(fq => fq.Id).ToList();
            var assignment = StageFormAssignment.Create(10, 1, 1, form.Id, questionIds, DateTime.UtcNow);
            dbContext.StageFormAssignments.Add(assignment);
            await dbContext.SaveChangesAsync();
            stageFormAssignmentId = assignment.Id;
        }

        // Submit response
        using (var scope = CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<DiagnosticDbContext>();
            var form = await dbContext.ProjectForms
                .Include(f => f.Questions)
                .FirstAsync(f => f.Id == projectFormId);

            var response = DiagnosticResponse.Create(
                form.Id, 10, 1, 100, stageFormAssignmentId, DateTime.UtcNow);

            var questionId = form.Questions.First().Id;
            response.AddResponse(questionId, "My answer", null, null, DateTime.UtcNow);
            response.MarkAsCompleted(DateTime.UtcNow);

            dbContext.DiagnosticResponses.Add(response);
            await dbContext.SaveChangesAsync();
            responseExternalId = response.ExternalId;
        }

        // Reload and verify
        using (var scope = CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<DiagnosticDbContext>();
            var loaded = await dbContext.DiagnosticResponses
                .Include(r => r.QuestionResponses)
                    .ThenInclude(qr => qr.Corrections)
                .FirstAsync(r => r.ExternalId == responseExternalId);

            loaded.IsCompleted.Should().BeTrue();
            loaded.CompletedAtUtc.Should().NotBeNull();
            loaded.QuestionResponses.Should().HaveCount(1);
            loaded.QuestionResponses.First().TextValue.Should().Be("My answer");
        }
    }

    [Fact]
    public async Task CorrectAnswer_Save_Reload_ShouldTrackCorrectionHistory()
    {
        Guid responseExternalId;

        // Setup — create form + assignment + response
        using (var scope = CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<DiagnosticDbContext>();

            var template = FormTemplate.Create("Template", null, null, DateTime.UtcNow);
            template.AddQuestion(1, "Q1", QuestionType.Text, 1, null, false);
            dbContext.FormTemplates.Add(template);
            await dbContext.SaveChangesAsync();

            var form = ProjectForm.CloneFromTemplate(template, 10, 1, DateTime.UtcNow);
            dbContext.ProjectForms.Add(form);
            await dbContext.SaveChangesAsync();

            var questionId = (await dbContext.ProjectForms.Include(f => f.Questions).FirstAsync(f => f.Id == form.Id))
                .Questions.First().Id;

            var assignment = StageFormAssignment.Create(10, 1, 1, form.Id, new[] { questionId }, DateTime.UtcNow);
            dbContext.StageFormAssignments.Add(assignment);
            await dbContext.SaveChangesAsync();

            var response = DiagnosticResponse.Create(form.Id, 10, 1, 100, assignment.Id, DateTime.UtcNow);
            response.AddResponse(questionId, "Original", null, null, DateTime.UtcNow);
            dbContext.DiagnosticResponses.Add(response);
            await dbContext.SaveChangesAsync();
            responseExternalId = response.ExternalId;
        }

        // Correct the answer
        using (var scope = CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<DiagnosticDbContext>();
            var response = await dbContext.DiagnosticResponses
                .Include(r => r.QuestionResponses)
                    .ThenInclude(qr => qr.Corrections)
                .FirstAsync(r => r.ExternalId == responseExternalId);

            var qr = response.QuestionResponses.First();
            response.CorrectAnswer(qr.Id, "Corrected", null, null, 200, "Typo", DateTime.UtcNow);
            await dbContext.SaveChangesAsync();
        }

        // Verify
        using (var scope = CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<DiagnosticDbContext>();
            var loaded = await dbContext.DiagnosticResponses
                .Include(r => r.QuestionResponses)
                    .ThenInclude(qr => qr.Corrections)
                .FirstAsync(r => r.ExternalId == responseExternalId);

            var qr = loaded.QuestionResponses.First();
            qr.TextValue.Should().Be("Corrected");
            qr.Corrections.Should().HaveCount(1);
            qr.Corrections.First().PreviousTextValue.Should().Be("Original");
            qr.Corrections.First().CorrectedByUserId.Should().Be(200);
            qr.Corrections.First().Reason.Should().Be("Typo");
        }
    }

    [Fact]
    public async Task SelectedOptionIds_ShouldRoundTripCorrectly()
    {
        Guid responseExternalId;

        using (var scope = CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<DiagnosticDbContext>();

            var template = FormTemplate.Create("Template", null, null, DateTime.UtcNow);
            var q = template.AddQuestion(1, "Q1", QuestionType.MultiSelect, 1, null, false);
            q.AddAnswerOption("A", 5.0m, SwotClassification.Strength, OdsrOrientation.Offensive, 1);
            q.AddAnswerOption("B", 3.0m, SwotClassification.Weakness, OdsrOrientation.Defensive, 2);
            dbContext.FormTemplates.Add(template);
            await dbContext.SaveChangesAsync();

            var form = ProjectForm.CloneFromTemplate(template, 10, 1, DateTime.UtcNow);
            dbContext.ProjectForms.Add(form);
            await dbContext.SaveChangesAsync();

            var questionId = (await dbContext.ProjectForms.Include(f => f.Questions).FirstAsync(f => f.Id == form.Id))
                .Questions.First().Id;

            var assignment = StageFormAssignment.Create(10, 1, 1, form.Id, new[] { questionId }, DateTime.UtcNow);
            dbContext.StageFormAssignments.Add(assignment);
            await dbContext.SaveChangesAsync();

            var optionIds = (await dbContext.ProjectForms
                .Include(f => f.Questions).ThenInclude(q2 => q2.AnswerOptions)
                .FirstAsync(f => f.Id == form.Id))
                .Questions.First().AnswerOptions.Select(ao => ao.Id).ToList();

            var response = DiagnosticResponse.Create(form.Id, 10, 1, 100, assignment.Id, DateTime.UtcNow);
            response.AddResponse(questionId, null, null, optionIds, DateTime.UtcNow);
            response.MarkAsCompleted(DateTime.UtcNow);
            dbContext.DiagnosticResponses.Add(response);
            await dbContext.SaveChangesAsync();
            responseExternalId = response.ExternalId;
        }

        // Reload and verify the serialized option IDs
        using (var scope = CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<DiagnosticDbContext>();
            var loaded = await dbContext.DiagnosticResponses
                .Include(r => r.QuestionResponses)
                .FirstAsync(r => r.ExternalId == responseExternalId);

            var qr = loaded.QuestionResponses.First();
            qr.SelectedOptionIdsRaw.Should().NotBeNullOrWhiteSpace();
            qr.SelectedOptionIdsRaw!.Split(',').Should().HaveCount(2);
        }
    }
}
