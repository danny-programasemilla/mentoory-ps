using FluentAssertions;
using Mentoory.Diagnostic.Application.Commands.CloneFormTemplate;
using Mentoory.Diagnostic.Domain.Aggregates.FormTemplate;
using Mentoory.Diagnostic.Domain.Enums;
using Mentoory.Diagnostic.Infrastructure.Persistence;
using Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructure;
using Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructureTemplate;
using Mentoory.Knowledge.Infrastructure.Persistence;
using Mentoory.Tests.Integration.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using KS = Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructure.KnowledgeStructure;

namespace Mentoory.Tests.Integration.Knowledge;

/// <summary>
/// End-to-end round-trip: a form template bound to a knowledge structure template is cloned into
/// a project; the cascade auto-provisions the project's knowledge structure and rewrites every
/// question's <c>TopicId</c> to a real project-topic id (FR-K20 through FR-K22).
///
/// The database is reset between tests via <see cref="IntegrationTestBase"/> (Respawn).
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class DiagnosticCascadeRoundTripTests : IntegrationTestBase
{
    private const long ProjectId = 10L;
    private const long IncubatorId = 1L;

    public DiagnosticCascadeRoundTripTests(MentooryWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task CloneFormTemplate_WithKnowledgeBinding_CascadesAndRewritesTopicIds()
    {
        // Arrange: seed knowledge structure template + form template bound to it.
        Guid formTemplateExternalId;
        Guid ksTemplateExternalId;

        using (var scope = CreateScope())
        {
            var knowledgeDb = scope.ServiceProvider.GetRequiredService<KnowledgeDbContext>();
            var diagnosticDb = scope.ServiceProvider.GetRequiredService<DiagnosticDbContext>();

            var ksTemplate = KnowledgeStructureTemplate.Create("Gestión", "Knowledge template", DateTime.UtcNow);
            var kmodule = ksTemplate.AddModule("M1", null, 1);
            ksTemplate.AddTopic(kmodule.ExternalId, "Finanzas", null, 1);
            ksTemplate.AddTopic(kmodule.ExternalId, "Mercadeo", null, 2);
            knowledgeDb.Set<KnowledgeStructureTemplate>().Add(ksTemplate);
            await knowledgeDb.SaveChangesAsync();
            ksTemplateExternalId = ksTemplate.ExternalId;

            // Reload to get persisted-topic ids so we can wire the diagnostic questions to them.
            var reloadedKs = await knowledgeDb.Set<KnowledgeStructureTemplate>()
                .Include(k => k.Modules).ThenInclude(m => m.Topics)
                .FirstAsync(k => k.ExternalId == ksTemplateExternalId);
            var topicFinanzasId = reloadedKs.Modules.Single().Topics.First(t => t.Name == "Finanzas").Id;
            var topicMercadeoId = reloadedKs.Modules.Single().Topics.First(t => t.Name == "Mercadeo").Id;

            var formTemplate = FormTemplate.Create("Diagnóstico", null, null, DateTime.UtcNow);
            formTemplate.SetDefaultKnowledgeStructureTemplate(ksTemplateExternalId);
            formTemplate.AddQuestion(topicFinanzasId, "¿Flujo de caja?", QuestionType.Text, StageApplicability.Both, 1, null, false);
            formTemplate.AddQuestion(topicMercadeoId, "¿Segmento objetivo?", QuestionType.Text, StageApplicability.Both, 2, null, false);
            diagnosticDb.FormTemplates.Add(formTemplate);
            await diagnosticDb.SaveChangesAsync();
            formTemplateExternalId = formTemplate.ExternalId;
        }

        // Act: clone the form template — this must auto-provision the knowledge structure.
        var cloneResult = await SendWithTenantAsync(
            new CloneFormTemplateCommand(formTemplateExternalId, ProjectId, IncubatorId),
            IncubatorId);

        // Assert
        cloneResult.IsSuccess.Should().BeTrue($"cascade should succeed, got: {FormatFailure(cloneResult)}");

        using (var scope = CreateScope())
        {
            var knowledgeDb = scope.ServiceProvider.GetRequiredService<KnowledgeDbContext>();
            var diagnosticDb = scope.ServiceProvider.GetRequiredService<DiagnosticDbContext>();

            // A KnowledgeStructure for the project must now exist, sourced from our template.
            var ksTemplate = await knowledgeDb.Set<KnowledgeStructureTemplate>()
                .FirstAsync(k => k.ExternalId == ksTemplateExternalId);

            var projectStructure = await knowledgeDb.Set<KS>()
                .Include(s => s.Modules).ThenInclude(m => m.Topics)
                .FirstAsync(s => s.ProjectId == ProjectId && s.SourceTemplateId == ksTemplate.Id);

            projectStructure.IncubatorId.Should().Be(IncubatorId);
            var projectTopicIds = projectStructure.Modules.SelectMany(m => m.Topics).Select(t => t.Id).ToHashSet();
            projectTopicIds.Should().HaveCount(2, "both template topics must have been cloned into project topics");

            // Every cloned Question.TopicId must resolve to a real project-topic id within THIS structure.
            var projectForm = await diagnosticDb.ProjectForms
                .Include(f => f.Questions)
                .FirstAsync(f => f.SourceTemplateId != null && f.ProjectId == ProjectId);

            projectForm.Questions.Should().HaveCount(2);
            foreach (var question in projectForm.Questions)
            {
                projectTopicIds.Should().Contain(question.TopicId,
                    "the cascade must rewrite every question's TopicId to a project-topic id from the auto-provisioned structure");
            }
        }
    }

    [Fact]
    public async Task CloneFormTemplate_TwiceForSameProject_ReusesExistingKnowledgeStructure()
    {
        // Arrange: same binding as above.
        Guid formTemplateExternalId;
        Guid ksTemplateExternalId;

        using (var scope = CreateScope())
        {
            var knowledgeDb = scope.ServiceProvider.GetRequiredService<KnowledgeDbContext>();
            var diagnosticDb = scope.ServiceProvider.GetRequiredService<DiagnosticDbContext>();

            var ksTemplate = KnowledgeStructureTemplate.Create("KT", null, DateTime.UtcNow);
            var kmodule = ksTemplate.AddModule("M1", null, 1);
            ksTemplate.AddTopic(kmodule.ExternalId, "T1", null, 1);
            knowledgeDb.Set<KnowledgeStructureTemplate>().Add(ksTemplate);
            await knowledgeDb.SaveChangesAsync();
            ksTemplateExternalId = ksTemplate.ExternalId;

            var reloadedKs = await knowledgeDb.Set<KnowledgeStructureTemplate>()
                .Include(k => k.Modules).ThenInclude(m => m.Topics)
                .FirstAsync(k => k.ExternalId == ksTemplateExternalId);
            var templateTopicId = reloadedKs.Modules.Single().Topics.Single().Id;

            var formTemplate = FormTemplate.Create("FT", null, null, DateTime.UtcNow);
            formTemplate.SetDefaultKnowledgeStructureTemplate(ksTemplateExternalId);
            formTemplate.AddQuestion(templateTopicId, "Q", QuestionType.Text, StageApplicability.Both, 1, null, false);
            diagnosticDb.FormTemplates.Add(formTemplate);
            await diagnosticDb.SaveChangesAsync();
            formTemplateExternalId = formTemplate.ExternalId;
        }

        // Act: clone twice for the same project.
        var firstClone = await SendWithTenantAsync(
            new CloneFormTemplateCommand(formTemplateExternalId, ProjectId, IncubatorId),
            IncubatorId);
        var secondClone = await SendWithTenantAsync(
            new CloneFormTemplateCommand(formTemplateExternalId, ProjectId, IncubatorId),
            IncubatorId);

        // Assert
        firstClone.IsSuccess.Should().BeTrue($"first clone should succeed, got: {FormatFailure(firstClone)}");
        secondClone.IsSuccess.Should().BeTrue($"second clone should succeed, got: {FormatFailure(secondClone)}");

        using (var scope = CreateScope())
        {
            var knowledgeDb = scope.ServiceProvider.GetRequiredService<KnowledgeDbContext>();
            var diagnosticDb = scope.ServiceProvider.GetRequiredService<DiagnosticDbContext>();

            var projectStructures = await knowledgeDb.Set<KS>()
                .Where(s => s.ProjectId == ProjectId)
                .ToListAsync();
            projectStructures.Should().HaveCount(1,
                "the cascade must reuse the existing project KnowledgeStructure on subsequent clones");

            var projectForms = await diagnosticDb.ProjectForms
                .Where(f => f.ProjectId == ProjectId)
                .ToListAsync();
            projectForms.Should().HaveCount(2, "each clone creates a distinct ProjectForm");
        }
    }

    private static string FormatFailure(Mentoory.Shared.Application.Result result)
    {
        if (result.ErrorMessages is null || result.ErrorMessages.Length == 0)
        {
            return result.ErrorCode?.ToString() ?? "<unknown>";
        }

        return string.Join("; ", result.ErrorMessages.Select(m => $"[{m.Context}] {m.Message}"));
    }
}
