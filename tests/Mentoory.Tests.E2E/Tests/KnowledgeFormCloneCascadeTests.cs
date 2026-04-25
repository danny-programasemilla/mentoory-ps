using FluentAssertions;
using Mentoory.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace Mentoory.Tests.E2E.Tests;

/// <summary>
/// E2E coverage for spec 017 US3 — cross-module form-clone cascade. The DB-invariant portions
/// (TopicId rewrite correctness, null-binding no-cascade, double-clone idempotency) live in
/// the integration suite (<c>DiagnosticCascadeRoundTripTests</c>, File 7 in the contract);
/// this file covers the UI-observable surface for the happy path and the Phase 9 mismatch
/// error.
///
/// | Spec 017 scenario | Method |
/// |---|---|
/// | US3-1 (UI)        | CloneCompatibleForm_HappyPath_CreatesProjectForm |
/// | US3-2             | CloneMismatchedForm_ShowsPhase9MismatchError |
/// </summary>
[Collection(E2ETestCollection.Name)]
[Trait("Category", "E2E")]
public class KnowledgeFormCloneCascadeTests
{
    private readonly PlaywrightFixture _fixture;

    public KnowledgeFormCloneCascadeTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CloneCompatibleForm_HappyPath_CreatesProjectForm()
    {
        var seededKsTemplateId = await KnowledgeIntegrationHelpers.GetSeededKsTemplateExternalIdAsync(_fixture);
        var seededFormTemplateId = await KnowledgeIntegrationHelpers.GetSeededBoundFormTemplateExternalIdAsync(_fixture);
        var projectName = $"E2E-US3-1-{Guid.NewGuid():N}"[..24];
        var project = await KnowledgeIntegrationHelpers.CreateProjectWithCoordinatorAsync(
            _fixture,
            incubatorName: "Incubadora Alpha",
            ksTemplateExternalId: seededKsTemplateId,
            projectName: projectName);

        var baselineFormCount = await KnowledgeIntegrationHelpers.CountProjectFormsAsync(_fixture, project.ProjectId);

        var page = await _fixture.CreatePageAsync();
        try
        {
            await KnowledgeTestHelpers.LoginAndSelectAsync(
                page, _fixture.BaseUrl, project.CoordinatorEmail, project.CoordinatorPassword,
                ContextSelection.CoordinatorForProject(project.Name));

            await page.GotoAsync($"{_fixture.BaseUrl}/Coordination/Diagnostics/Clone");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var radio = page.Locator($"input[type='radio'][name='SourceTemplateExternalId'][value='{seededFormTemplateId}']");
            (await radio.CountAsync()).Should().Be(1,
                "the seeded 'Diagnóstico Básico de Emprendimiento' FormTemplate must appear on the clone page");
            await radio.CheckAsync();

            await page.Locator("button[type='submit']").Filter(new LocatorFilterOptions
            {
                HasText = "Clonar Plantilla",
            }).First.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            page.Url.Should().EndWith("/Coordination/Diagnostics",
                "successful clone must redirect back to the diagnostics list");

            var body = await page.ContentAsync();
            body.Should().Contain("Formulario diagnóstico clonado exitosamente",
                "the Spanish success toast must render on the list after a successful clone");

            var postCount = await KnowledgeIntegrationHelpers.CountProjectFormsAsync(_fixture, project.ProjectId);
            postCount.Should().Be(baselineFormCount + 1,
                "exactly one new ProjectForm row must exist for the target project after the clone");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(CloneCompatibleForm_HappyPath_CreatesProjectForm));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task CloneMismatchedForm_ShowsPhase9MismatchError()
    {
        // Setup: build a FormTemplate bound to a DIFFERENT KS template than the project's bound
        // template. The mismatch check in CloneFormTemplateHandler short-circuits before any
        // TopicId rewrite, so we can reuse the seeded 'Proyecto Innovación' for coord1 here
        // (FirstEnabled context lands on it) without needing the Phase-9 provisioned state that
        // the happy path requires.
        var otherKsTemplateId = await KnowledgeIntegrationHelpers.CreateKsTemplateAsync(
            _fixture, $"E2E-US3-2-Other-{Guid.NewGuid():N}"[..24]);
        var mismatchedFormTemplateId = await KnowledgeIntegrationHelpers.CreateFormTemplateAsync(
            _fixture, boundKsTemplateExternalId: otherKsTemplateId);

        var seededProject = await KnowledgeIntegrationHelpers.GetProjectByNameAsync(_fixture, "Proyecto Innovación");
        var baselineFormCount = await KnowledgeIntegrationHelpers.CountProjectFormsAsync(_fixture, seededProject.ProjectId);

        var page = await _fixture.CreatePageAsync();
        try
        {
            await KnowledgeTestHelpers.LoginAndSelectAsync(
                page, _fixture.BaseUrl, "coord1@test.mentoory.com", "Test123!@#",
                ContextSelection.FirstEnabled);

            await page.GotoAsync($"{_fixture.BaseUrl}/Coordination/Diagnostics/Clone");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var radio = page.Locator($"input[type='radio'][name='SourceTemplateExternalId'][value='{mismatchedFormTemplateId}']");
            (await radio.CountAsync()).Should().Be(1,
                "the test-local mismatched FormTemplate must appear on the clone page");
            await radio.CheckAsync();

            await page.Locator("button[type='submit']").Filter(new LocatorFilterOptions
            {
                HasText = "Clonar Plantilla",
            }).First.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var body = await page.ContentAsync();
            // Verbatim Spanish error from CloneFormTemplateHandler § mismatch branch. Substring
            // match (not full string) tolerates trailing whitespace / DOM decorations; the unique
            // first clause is specific enough to avoid false positives per research R6.
            body.Should().Contain("Este formulario está diseñado para una estructura de conocimiento diferente",
                "the Phase 9 mismatch error must surface verbatim in the clone view on rejection");

            var postCount = await KnowledgeIntegrationHelpers.CountProjectFormsAsync(_fixture, seededProject.ProjectId);
            postCount.Should().Be(baselineFormCount,
                "no ProjectForm row must be created when the KS mismatch check rejects the clone");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(CloneMismatchedForm_ShowsPhase9MismatchError));
            await page.Context.DisposeAsync();
        }
    }
}
