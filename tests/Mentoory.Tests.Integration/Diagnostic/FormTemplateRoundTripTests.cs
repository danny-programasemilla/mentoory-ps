using FluentAssertions;
using Mentoory.Diagnostic.Application.Commands.CloneFormTemplate;
using Mentoory.Diagnostic.Application.Queries.ListFormTemplates;
using Mentoory.Diagnostic.Domain.Aggregates.FormTemplate;
using Mentoory.Diagnostic.Domain.Enums;
using Mentoory.Diagnostic.Infrastructure.Persistence;
using Mentoory.Shared.Application.DataTables;
using Mentoory.Tests.Integration.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Mentoory.Tests.Integration.Diagnostic;

[Collection(IntegrationTestCollection.Name)]
public class FormTemplateRoundTripTests : IntegrationTestBase
{
    public FormTemplateRoundTripTests(MentooryWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task CreateTemplate_Save_Reload_ShouldPersistAllData()
    {
        // Arrange & Act — create template directly via DbContext
        Guid externalId;
        using (var scope = CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<DiagnosticDbContext>();
            var template = FormTemplate.Create("Diagnóstico Empresarial", "Evaluación completa", "Premium", DateTime.UtcNow);
            var q1 = template.AddQuestion(1, "¿Cuál es su experiencia?", QuestionType.Text, 1, null, false);
            var q2 = template.AddQuestion(2, "Nivel de madurez", QuestionType.SingleSelect, 2, "Madurez", false);
            q2.AddAnswerOption("Alto", 5.0m, SwotClassification.Strength, OdsrOrientation.Offensive, 1);
            q2.AddAnswerOption("Medio", 3.0m, SwotClassification.Opportunity, OdsrOrientation.Reorientation, 2);

            dbContext.FormTemplates.Add(template);
            await dbContext.SaveChangesAsync();
            externalId = template.ExternalId;
        }

        // Assert — reload and verify
        using (var scope = CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<DiagnosticDbContext>();
            var loaded = await dbContext.FormTemplates
                .Include(f => f.Questions)
                    .ThenInclude(q => q.AnswerOptions)
                .FirstAsync(f => f.ExternalId == externalId);

            loaded.Name.Should().Be("Diagnóstico Empresarial");
            loaded.Description.Should().Be("Evaluación completa");
            loaded.SubscriptionTier.Should().Be("Premium");
            loaded.Version.Should().Be(1);
            loaded.IsActive.Should().BeTrue();
            loaded.Questions.Should().HaveCount(2);

            var selectQuestion = loaded.Questions.First(q => q.QuestionType == QuestionType.SingleSelect);
            selectQuestion.AnswerOptions.Should().HaveCount(2);
            selectQuestion.AnswerOptions.Should().Contain(ao => ao.OptionText == "Alto" && ao.Score == 5.0m);
            selectQuestion.BlockGroup.Should().Be("Madurez");
        }
    }

    [Fact]
    public async Task UpdateTemplate_ShouldIncrementVersionAndPersist()
    {
        Guid externalId;
        using (var scope = CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<DiagnosticDbContext>();
            var template = FormTemplate.Create("V1 Template", null, null, DateTime.UtcNow);
            dbContext.FormTemplates.Add(template);
            await dbContext.SaveChangesAsync();
            externalId = template.ExternalId;
        }

        // Update
        using (var scope = CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<DiagnosticDbContext>();
            var template = await dbContext.FormTemplates.FirstAsync(f => f.ExternalId == externalId);
            template.Update("V2 Template", "Updated desc", "Basic");
            await dbContext.SaveChangesAsync();
        }

        // Verify
        using (var scope = CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<DiagnosticDbContext>();
            var loaded = await dbContext.FormTemplates.FirstAsync(f => f.ExternalId == externalId);
            loaded.Name.Should().Be("V2 Template");
            loaded.Version.Should().Be(2);
            loaded.SubscriptionTier.Should().Be("Basic");
        }
    }
}
