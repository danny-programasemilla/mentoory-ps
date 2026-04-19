using FluentAssertions;
using Mentoory.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace Mentoory.Tests.E2E.Tests;

/// <summary>
/// E2E coverage for spec 016 US2 + Phase 9 amendment, extended by spec 017 Phase 4
/// (T019–T023). Under the Phase 9 binding, every project is bound 1:1 to a KS template at
/// creation (via <c>IKnowledgeStructureProvisioner</c>), so the coordinator-side "Clone from
/// template" UI is retired and the seeded 'Proyecto Innovación' already has a materialized KS.
///
/// | Spec 017 scenario | Method |
/// |---|---|
/// | US2-1            | Projects_PageLoads_ShowsMaterializedKs |
/// | US2-4 (+ US4-2 baseline) | ProjectStructureDetail_PageLoads_ShowsTree |
/// | US2-4 (clone-name regression) | ProjectStructureDetail_TreeContainsClonedNames |
/// | US2-5            | Projects_NoCloneFromTemplateButton_Phase9Regression |
/// | US2-6            | CloneFromTemplate_LegacyRoute_NoLongerAccessible |
/// </summary>
[Collection(E2ETestCollection.Name)]
[Trait("Category", "E2E")]
public class KnowledgeProjectStructureTests
{
    private readonly PlaywrightFixture _fixture;

    public KnowledgeProjectStructureTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Projects_PageLoads_ShowsMaterializedKs()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsCoordinatorAsync(page);
            await page.GotoAsync($"{_fixture.BaseUrl}/Coordination/Knowledge/Projects");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            page.Url.Should().Contain("/Coordination/Knowledge/Projects",
                "ProjectCoordinator should reach the project-structures list");

            var content = await page.ContentAsync();
            content.Should().Contain("Estructura de Conocimiento",
                "seeded project KS row for 'Proyecto Innovación' must be listed (auto-materialized at seed time)");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Projects_PageLoads_ShowsMaterializedKs));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task Projects_NoCloneFromTemplateButton_Phase9Regression()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsCoordinatorAsync(page);
            await page.GotoAsync($"{_fixture.BaseUrl}/Coordination/Knowledge/Projects");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var cloneButton = page.Locator("a, button").Filter(new LocatorFilterOptions
            {
                HasText = "Clonar desde plantilla",
            });
            (await cloneButton.CountAsync()).Should().Be(0,
                "Phase 9 retired the coordinator 'Clonar desde plantilla' UI; KS materializes at project creation");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Projects_NoCloneFromTemplateButton_Phase9Regression));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task CloneFromTemplate_LegacyRoute_NoLongerAccessible()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsCoordinatorAsync(page);
            var response = await page.GotoAsync($"{_fixture.BaseUrl}/Coordination/Knowledge/Projects/Clone");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var status = response?.Status ?? 200;
            var body = await page.ContentAsync();
            var looksLikeCloneForm = body.Contains("SourceTemplateExternalId", StringComparison.OrdinalIgnoreCase)
                || body.Contains("Clonar desde plantilla", StringComparison.OrdinalIgnoreCase);

            (status is >= 400 || !looksLikeCloneForm).Should().BeTrue(
                "Phase 9 removed the /Knowledge/Projects/Clone GET+POST actions; route must not render the retired form");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(CloneFromTemplate_LegacyRoute_NoLongerAccessible));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task ProjectStructureDetail_PageLoads_ShowsTree()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsCoordinatorAsync(page);
            await page.GotoAsync($"{_fixture.BaseUrl}/Coordination/Knowledge/Projects");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var detailLink = page.Locator("a[href*='/Projects/']").Filter(new LocatorFilterOptions
            {
                HasText = "Ver detalles",
            }).First;

            (await detailLink.CountAsync()).Should().BeGreaterThan(0,
                "the seeded 'Proyecto Innovación' must surface a 'Ver detalles' link in the list");

            await detailLink.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            page.Url.Should().MatchRegex(@"/Coordination/Knowledge/Projects/[0-9a-fA-F-]{36}",
                "project-structure detail URL must include the KS ExternalId");

            var body = await page.ContentAsync();

            // (a) All four hierarchy levels are enumerated in the tree-card header.
            body.Should().Contain("Módulos / Temas / Asignaturas / Recursos",
                "the project KS tree header must enumerate all four hierarchy levels");

            // (b) Seeded KS defaults to SyncMode.Disconnected → "Desconectada" badge renders.
            body.Should().Contain("Desconectada",
                "seeded project KS defaults to SyncMode.Disconnected — the 'Desconectada' badge must render");

            // (c) Specific seeded Spanish names are rendered in the tree.
            body.Should().Contain("Diagnóstico",
                "the seeded module 'Diagnóstico' must appear in the project KS tree");
            body.Should().Contain("Modelo de Negocio",
                "the seeded topic 'Modelo de Negocio' must appear in the project KS tree");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(ProjectStructureDetail_PageLoads_ShowsTree));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task ProjectStructureDetail_TreeContainsClonedNames()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsCoordinatorAsync(page);
            await page.GotoAsync($"{_fixture.BaseUrl}/Coordination/Knowledge/Projects");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await page.Locator("a[href*='/Projects/']").Filter(new LocatorFilterOptions
            {
                HasText = "Ver detalles",
            }).First.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Acts as a regression guard for US4-2 ("rename doesn't leak" baseline): if a
            // future test renames one of these topics, the template-side unchanged assertion
            // is only meaningful when we can identify the known pre-rename names here.
            var body = await page.ContentAsync();
            body.Should().Contain("Finanzas",
                "the seeded topic 'Finanzas' must appear in the project KS tree");
            body.Should().Contain("Mercado",
                "the seeded topic 'Mercado' must appear in the project KS tree");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(ProjectStructureDetail_TreeContainsClonedNames));
            await page.Context.DisposeAsync();
        }
    }

    private Task LoginAsCoordinatorAsync(IPage page) =>
        KnowledgeTestHelpers.LoginAndSelectAsync(
            page, _fixture.BaseUrl, "coord1@test.mentoory.com", "Test123!@#", ContextSelection.FirstEnabled);
}
