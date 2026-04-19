using FluentAssertions;
using Mentoory.Diagnostic.Application.Commands.CloneFormTemplate;
using Mentoory.Diagnostic.Domain.Aggregates.FormTemplate;
using Mentoory.Diagnostic.Domain.Enums;
using Mentoory.Diagnostic.Infrastructure.Persistence;
using Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructureTemplate;
using Mentoory.Knowledge.Infrastructure.Persistence;
using Mentoory.Tenant.Application.Commands.CreateIncubator;
using Mentoory.Tenant.Application.Commands.CreateProject;
using Mentoory.Tenant.Infrastructure.Persistence;
using Mentoory.Tests.Integration.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using KS = Mentoory.Knowledge.Domain.Aggregates.KnowledgeStructure.KnowledgeStructure;

namespace Mentoory.Tests.Integration.Knowledge;

/// <summary>
/// End-to-end round-trip under the Phase 9 amendment: the project's
/// <see cref="KS"/> is materialized when the project is created (via
/// <see cref="CreateProjectCommand"/> and the <c>IKnowledgeStructureProvisioner</c>).
/// Cloning a form template bound to the same KS template only rewrites
/// <c>Question.TopicId</c> values — it must NOT create any new KS rows.
///
/// The database is reset between tests via <see cref="IntegrationTestBase"/> (Respawn).
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class DiagnosticCascadeRoundTripTests : IntegrationTestBase
{
    public DiagnosticCascadeRoundTripTests(MentooryWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task CloneFormTemplate_CompatibleForm_RewritesTopicIdsToProjectsKs()
    {
        var (projectId, incubatorId, ksTemplateExternalId, topicFinanzasId, topicMercadeoId) =
            await SetupProjectWithKsTemplateAsync();

        var formTemplateExternalId = await CreateFormTemplateAsync(ksTemplateExternalId, (topicFinanzasId, "¿Flujo de caja?"), (topicMercadeoId, "¿Segmento objetivo?"));

        var cloneResult = await SendWithTenantAsync(
            new CloneFormTemplateCommand(formTemplateExternalId, projectId, incubatorId),
            incubatorId);

        cloneResult.IsSuccess.Should().BeTrue($"compatibility clone should succeed, got: {FormatFailure(cloneResult)}");

        using var scope = CreateScope();
        var knowledgeDb = scope.ServiceProvider.GetRequiredService<KnowledgeDbContext>();
        var diagnosticDb = scope.ServiceProvider.GetRequiredService<DiagnosticDbContext>();

        var projectStructure = await knowledgeDb.Set<KS>()
            .Include(s => s.Modules).ThenInclude(m => m.Topics)
            .SingleAsync(s => s.ProjectId == projectId);

        var projectTopicIds = projectStructure.Modules.SelectMany(m => m.Topics).Select(t => t.Id).ToHashSet();
        projectTopicIds.Should().HaveCount(2);

        var projectForm = await diagnosticDb.ProjectForms
            .Include(f => f.Questions)
            .FirstAsync(f => f.ProjectId == projectId);

        projectForm.Questions.Should().HaveCount(2);
        foreach (var question in projectForm.Questions)
        {
            projectTopicIds.Should().Contain(question.TopicId,
                "every question's TopicId must resolve to a real project-topic id");
        }
    }

    [Fact]
    public async Task CloneFormTemplate_TwiceForSameProject_DoesNotDuplicateProjectKs()
    {
        var (projectId, incubatorId, ksTemplateExternalId, topicId, _) =
            await SetupProjectWithKsTemplateAsync();

        var formTemplateExternalId = await CreateFormTemplateAsync(ksTemplateExternalId, (topicId, "Q"));

        var firstClone = await SendWithTenantAsync(
            new CloneFormTemplateCommand(formTemplateExternalId, projectId, incubatorId),
            incubatorId);
        var secondClone = await SendWithTenantAsync(
            new CloneFormTemplateCommand(formTemplateExternalId, projectId, incubatorId),
            incubatorId);

        firstClone.IsSuccess.Should().BeTrue($"first clone: {FormatFailure(firstClone)}");
        secondClone.IsSuccess.Should().BeTrue($"second clone: {FormatFailure(secondClone)}");

        using var scope = CreateScope();
        var knowledgeDb = scope.ServiceProvider.GetRequiredService<KnowledgeDbContext>();
        var diagnosticDb = scope.ServiceProvider.GetRequiredService<DiagnosticDbContext>();

        var projectStructures = await knowledgeDb.Set<KS>()
            .Where(s => s.ProjectId == projectId)
            .ToListAsync();
        projectStructures.Should().HaveCount(1,
            "UNIQUE(ProjectId) on KnowledgeStructures guarantees exactly one KS per project");

        var projectForms = await diagnosticDb.ProjectForms
            .Where(f => f.ProjectId == projectId)
            .ToListAsync();
        projectForms.Should().HaveCount(2, "each clone creates a distinct ProjectForm");
    }

    private static void SetGuid(object entity, Guid externalId)
    {
        var prop = entity.GetType().GetProperty(
            "ExternalId",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        prop!.SetValue(entity, externalId);
    }

    private static string FormatFailure(Mentoory.Shared.Application.Result result)
    {
        if (result.ErrorMessages is null || result.ErrorMessages.Length == 0)
        {
            return result.ErrorCode?.ToString() ?? "<unknown>";
        }

        return string.Join("; ", result.ErrorMessages.Select(m => $"[{m.Context}] {m.Message}"));
    }

    private async Task<(long ProjectId, long IncubatorId, Guid KsTemplateExternalId, long TopicAId, long TopicBId)>
        SetupProjectWithKsTemplateAsync()
    {
        var ksTemplateExternalId = Guid.NewGuid();
        long topicAId, topicBId;

        using (var scope = CreateScope())
        {
            var knowledgeDb = scope.ServiceProvider.GetRequiredService<KnowledgeDbContext>();

            var ksTemplate = KnowledgeStructureTemplate.Create("Gestión", "Knowledge template", DateTime.UtcNow);
            SetGuid(ksTemplate, ksTemplateExternalId);
            var kmodule = ksTemplate.AddModule("M1", null, 1);
            ksTemplate.AddTopic(kmodule.ExternalId, "Finanzas", null, 1);
            ksTemplate.AddTopic(kmodule.ExternalId, "Mercadeo", null, 2);
            knowledgeDb.Set<KnowledgeStructureTemplate>().Add(ksTemplate);
            await knowledgeDb.SaveChangesAsync();

            var reloaded = await knowledgeDb.Set<KnowledgeStructureTemplate>()
                .Include(k => k.Modules).ThenInclude(m => m.Topics)
                .FirstAsync(k => k.ExternalId == ksTemplateExternalId);
            topicAId = reloaded.Modules.Single().Topics.First(t => t.Name == "Finanzas").Id;
            topicBId = reloaded.Modules.Single().Topics.First(t => t.Name == "Mercadeo").Id;
        }

        var incubatorResult = await SendAsync(new CreateIncubatorCommand("Cascade Inc", null));
        incubatorResult.IsSuccess.Should().BeTrue();
        var incubatorExternalId = incubatorResult.Value!;

        var projectResult = await SendAsync(new CreateProjectCommand(
            incubatorExternalId, "Cascade Project", null, ksTemplateExternalId));
        projectResult.IsSuccess.Should().BeTrue($"project creation must succeed with the bound KS template: {FormatFailure(projectResult)}");

        using (var scope = CreateScope())
        {
            var tenantDb = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
            var project = await tenantDb.Projects.IgnoreQueryFilters()
                .FirstAsync(p => p.ExternalId == projectResult.Value!);
            var incubator = await tenantDb.Incubators.FirstAsync(i => i.ExternalId == incubatorExternalId);
            return (project.Id, incubator.Id, ksTemplateExternalId, topicAId, topicBId);
        }
    }

    private async Task<Guid> CreateFormTemplateAsync(
        Guid ksTemplateExternalId,
        params (long TopicId, string Text)[] questions)
    {
        using var scope = CreateScope();
        var diagnosticDb = scope.ServiceProvider.GetRequiredService<DiagnosticDbContext>();

        var formTemplate = FormTemplate.Create("Diagnóstico", null, null, DateTime.UtcNow);
        formTemplate.SetDefaultKnowledgeStructureTemplate(ksTemplateExternalId);
        for (var i = 0; i < questions.Length; i++)
        {
            formTemplate.AddQuestion(questions[i].TopicId, questions[i].Text, QuestionType.Text, StageApplicability.Both, i + 1, null, false);
        }

        diagnosticDb.FormTemplates.Add(formTemplate);
        await diagnosticDb.SaveChangesAsync();
        return formTemplate.ExternalId;
    }
}
