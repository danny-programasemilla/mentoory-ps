using FluentAssertions;
using Mentoory.Diagnostic.Application.Commands.CloneFormTemplate;
using Mentoory.Diagnostic.Domain.Aggregates.FormTemplate;
using Mentoory.Diagnostic.Domain.Enums;
using Mentoory.Diagnostic.Infrastructure.Persistence;
using Mentoory.Knowledge.Application.Commands.AddModuleTemplate;
using Mentoory.Knowledge.Application.Commands.AddTopicTemplate;
using Mentoory.Knowledge.Application.Commands.CreateKnowledgeStructureTemplate;
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

    /// <summary>
    /// Spec 017 US3-3 — when the source FormTemplate has no
    /// <c>DefaultKnowledgeStructureTemplateExternalId</c>, the clone path skips the whole KS
    /// cascade: no mismatch check, no TopicId rewrite, no new KS row. Covers the null-binding
    /// branch in <see cref="CloneFormTemplateHandler"/>.
    /// </summary>
    [Fact]
    public async Task CloneFormTemplate_NullBinding_NoKnowledgeCascade()
    {
        var (projectId, incubatorId, _, topicAId, _) = await SetupProjectWithKsTemplateAsync();

        var baselineStructureCount = await CountProjectKnowledgeStructuresAsync(projectId);
        var baselineFormCount = await CountProjectFormsAsync(projectId);

        var formTemplateExternalId = await CreateFormTemplateAsync(
            ksTemplateExternalId: null, (topicAId, "¿Descripción del equipo?"));

        var cloneResult = await SendWithTenantAsync(
            new CloneFormTemplateCommand(formTemplateExternalId, projectId, incubatorId),
            incubatorId);

        cloneResult.IsSuccess.Should().BeTrue(
            $"null-binding clone must succeed without invoking the KS cascade, got: {FormatFailure(cloneResult)}");

        (await CountProjectKnowledgeStructuresAsync(projectId)).Should().Be(baselineStructureCount,
            "a null-bound FormTemplate must NOT create or duplicate the project's KS row");

        (await CountProjectFormsAsync(projectId)).Should().Be(baselineFormCount + 1,
            "exactly one ProjectForm row must be created by the successful clone");

        using var scope = CreateScope();
        var diagnosticDb = scope.ServiceProvider.GetRequiredService<DiagnosticDbContext>();
        var projectForm = await diagnosticDb.ProjectForms
            .Include(f => f.Questions)
            .AsNoTracking()
            .FirstAsync(f => f.ProjectId == projectId);

        projectForm.Questions.Should().HaveCount(1);
        projectForm.Questions.Single().TopicId.Should().Be(topicAId,
            "null-binding clone must preserve the source Question.TopicId verbatim (no rewrite)");
    }

    /// <summary>
    /// Spec 017 T029 — regression guard confirming the command-layer equivalents of
    /// <c>KnowledgeIntegrationHelpers.AddTopicToTemplateAsync</c> and
    /// <c>CreateFormTemplateAsync</c> (the E2E helpers in <c>Mentoory.Tests.E2E.Infrastructure</c>)
    /// commit to the same database the integration fixture's <see cref="KnowledgeDbContext"/>
    /// reads. A fail in this test signals drift in DB-scope semantics (e.g., a new service
    /// registration that silently splits the connection string) before the slower E2E suite
    /// picks it up.
    /// </summary>
    [Fact]
    public async Task KnowledgeIntegrationHelpers_UtilityContract_RegressionGuard()
    {
        var createTemplateResult = await SendAsync(new CreateKnowledgeStructureTemplateCommand(
            $"Guard-{Guid.NewGuid():N}"[..16], null));
        createTemplateResult.IsSuccess.Should().BeTrue(
            $"CreateKnowledgeStructureTemplateCommand must succeed: {FormatFailure(createTemplateResult)}");
        var templateExternalId = createTemplateResult.Value!;

        var addModuleResult = await SendAsync(new AddModuleTemplateCommand(
            templateExternalId, "Guard Module", Description: null, SortOrder: 1));
        addModuleResult.IsSuccess.Should().BeTrue(
            $"AddModuleTemplateCommand must succeed: {FormatFailure(addModuleResult)}");
        var moduleExternalId = addModuleResult.Value!;

        var addTopicResult = await SendAsync(new AddTopicTemplateCommand(
            templateExternalId, moduleExternalId, "Guard Topic", Description: null, SortOrder: 1));
        addTopicResult.IsSuccess.Should().BeTrue(
            $"AddTopicTemplateCommand must succeed: {FormatFailure(addTopicResult)}");
        var topicExternalId = addTopicResult.Value!;

        using var scope = CreateScope();
        var knowledgeDb = scope.ServiceProvider.GetRequiredService<KnowledgeDbContext>();
        var reloaded = await knowledgeDb.Set<KnowledgeStructureTemplate>()
            .Include(t => t.Modules).ThenInclude(m => m.Topics)
            .AsNoTracking()
            .FirstAsync(t => t.ExternalId == templateExternalId);

        reloaded.Modules.Should().ContainSingle(m => m.ExternalId == moduleExternalId,
            "the module added via command must be readable from a fresh KnowledgeDbContext scope");
        reloaded.Modules.Single().Topics.Should().ContainSingle(t => t.ExternalId == topicExternalId,
            "the topic added via command must be readable from the same scope");
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
        Guid? ksTemplateExternalId,
        params (long TopicId, string Text)[] questions)
    {
        using var scope = CreateScope();
        var diagnosticDb = scope.ServiceProvider.GetRequiredService<DiagnosticDbContext>();

        var formTemplate = FormTemplate.Create("Diagnóstico", null, null, DateTime.UtcNow);
        if (ksTemplateExternalId is Guid boundExternalId)
        {
            formTemplate.SetDefaultKnowledgeStructureTemplate(boundExternalId);
        }

        for (var i = 0; i < questions.Length; i++)
        {
            formTemplate.AddQuestion(questions[i].TopicId, questions[i].Text, QuestionType.Text, StageApplicability.Both, i + 1, null, false);
        }

        diagnosticDb.FormTemplates.Add(formTemplate);
        await diagnosticDb.SaveChangesAsync();
        return formTemplate.ExternalId;
    }

    private async Task<int> CountProjectKnowledgeStructuresAsync(long projectId)
    {
        using var scope = CreateScope();
        var knowledgeDb = scope.ServiceProvider.GetRequiredService<KnowledgeDbContext>();
        return await knowledgeDb.Set<KS>().AsNoTracking().CountAsync(s => s.ProjectId == projectId);
    }

    private async Task<int> CountProjectFormsAsync(long projectId)
    {
        using var scope = CreateScope();
        var diagnosticDb = scope.ServiceProvider.GetRequiredService<DiagnosticDbContext>();
        return await diagnosticDb.ProjectForms.AsNoTracking().CountAsync(f => f.ProjectId == projectId);
    }
}
