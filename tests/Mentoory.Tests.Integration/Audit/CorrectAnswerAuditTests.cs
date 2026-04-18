using System.Text.Json;
using FluentAssertions;
using Mentoory.Diagnostic.Application.Commands.CorrectAnswer;
using Mentoory.Diagnostic.Domain.Aggregates.DiagnosticResponse;
using Mentoory.Diagnostic.Domain.Aggregates.FormTemplate;
using Mentoory.Diagnostic.Domain.Aggregates.ProjectForm;
using Mentoory.Diagnostic.Domain.Enums;
using Mentoory.Diagnostic.Infrastructure.Persistence;
using Mentoory.Shared.Application.Audit;
using Mentoory.Shared.Application.Queries.Audit;
using Mentoory.Shared.Infrastructure.Persistence.Audit;
using Mentoory.Tests.Integration.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Mentoory.Tests.Integration.Audit;

/// <summary>
/// End-to-end Manual-mode coverage for <c>CorrectAnswerCommand</c>: verifies the handler
/// writes exactly one audit row through the real MediatR pipeline (proving the
/// <c>AuditingBehavior</c> Manual-mode passthrough does NOT also write a row) and that
/// <c>Details</c> captures both the previous and the new answer text.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class CorrectAnswerAuditTests : IntegrationTestBase
{
    public CorrectAnswerAuditTests(MentooryWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task CorrectAnswer_WritesExactlyOneManualModeAuditRow_WithBeforeAndAfter()
    {
        Guid responseExternalId;
        long questionResponseId;

        using (var scope = CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<DiagnosticDbContext>();

            var template = FormTemplate.Create("AuditTemplate", null, null, DateTime.UtcNow);
            template.AddQuestion(1, "Q1", QuestionType.Text, StageApplicability.Both, 1, null, false);
            dbContext.FormTemplates.Add(template);
            await dbContext.SaveChangesAsync();

            var form = ProjectForm.CloneFromTemplate(template, 10, 1, DateTime.UtcNow);
            dbContext.ProjectForms.Add(form);
            await dbContext.SaveChangesAsync();

            var questionId = (await dbContext.ProjectForms.Include(f => f.Questions).FirstAsync(f => f.Id == form.Id))
                .Questions.First().Id;

            var response = DiagnosticResponse.Create(form.Id, 10, 1, 100, EvaluationStage.Initial, DateTime.UtcNow);
            response.AddResponse(questionId, "OriginalAnswer", null, null, DateTime.UtcNow);
            dbContext.DiagnosticResponses.Add(response);
            await dbContext.SaveChangesAsync();

            responseExternalId = response.ExternalId;
            questionResponseId = response.QuestionResponses.First().Id;
        }

        var result = await SendAsync(new CorrectAnswerCommand(
            responseExternalId,
            questionResponseId,
            "FixedAnswer",
            null,
            null,
            200,
            "Typo fix"));

        result.IsSuccess.Should().BeTrue();

        using (var scope = CreateScope())
        {
            var auditCtx = scope.ServiceProvider.GetRequiredService<AuditReadDbContext>();
            var rows = await auditCtx.AuditLogs
                .AsNoTracking()
                .Where(r => r.Action == nameof(CorrectAnswerCommand))
                .ToListAsync();

            rows.Should().HaveCount(1,
                "Manual-mode commands must write exactly one audit row — the handler itself, not the behavior");

            var row = rows.Single();
            row.EventType.Should().Be(AuditEventTypes.AnswerCorrected);
            row.EntityType.Should().Be("AnswerCorrection");
            row.Outcome.Should().Be("Success");
            row.Details.Should().NotBeNull();

            using var doc = JsonDocument.Parse(row.Details!);
            doc.RootElement.GetProperty("Before").GetProperty("TextValue").GetString()
                .Should().Be("OriginalAnswer");
            doc.RootElement.GetProperty("After").GetProperty("NewTextValue").GetString()
                .Should().Be("FixedAnswer");
        }
    }
}
