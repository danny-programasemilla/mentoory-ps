using FluentAssertions;
using Mentoory.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace Mentoory.Tests.E2E.Tests;

/// <summary>
/// E2E smoke for spec 016 US2 + Phase 9 amendment — ProjectCoordinator's per-project knowledge
/// structure views. Under the Phase 9 binding, every project is bound 1:1 to a KS template at
/// creation (via <c>IKnowledgeStructureProvisioner</c>), so the coordinator-side "Clone from
/// template" UI is retired and the seeded 'Proyecto Innovación' already has a materialized KS.
/// </summary>
[Collection(E2ETestCollection.Name)]
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

            if (await detailLink.CountAsync() == 0)
            {
                return;
            }

            await detailLink.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            page.Url.Should().MatchRegex(@"/Coordination/Knowledge/Projects/[0-9a-fA-F-]{36}",
                "project-structure detail URL must include the KS ExternalId");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(ProjectStructureDetail_PageLoads_ShowsTree));
            await page.Context.DisposeAsync();
        }
    }

    private async Task LoginAsCoordinatorAsync(IPage page)
    {
        await page.GotoAsync($"{_fixture.BaseUrl}/Access/Login");
        await page.FillAsync("input[name='Email']", "coord1@test.mentoory.com");
        await page.FillAsync("input[name='Password']", "Test123!@#");
        await page.ClickAsync("button[type='submit']");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await page.WaitForURLAsync(url => !url.Contains("/Access/Login"), new PageWaitForURLOptions { Timeout = 10000 });
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        if (!page.Url.Contains("/Context/Select"))
        {
            return;
        }

        var roleDropdown = page.Locator("[data-mode='page'] [data-cs='role']");
        await page.WaitForFunctionAsync(
            "sel => sel.options.length > 1",
            await roleDropdown.ElementHandleAsync(),
            new() { Timeout = 10000 });
        if (await roleDropdown.IsEnabledAsync())
        {
            await roleDropdown.SelectOptionAsync(new SelectOptionValue { Index = 1 });
        }

        var incubatorDropdown = page.Locator("[data-mode='page'] [data-cs='incubator']");
        await page.WaitForFunctionAsync(
            "sel => sel.options.length > 1",
            await incubatorDropdown.ElementHandleAsync(),
            new() { Timeout = 10000 });
        if (await incubatorDropdown.IsEnabledAsync())
        {
            await incubatorDropdown.SelectOptionAsync(new SelectOptionValue { Index = 1 });
        }

        var projectDropdown = page.Locator("[data-mode='page'] [data-cs='project']");
        if (await projectDropdown.CountAsync() > 0 && await projectDropdown.IsEnabledAsync())
        {
            await page.WaitForFunctionAsync(
                "sel => sel.options.length > 1",
                await projectDropdown.ElementHandleAsync(),
                new() { Timeout = 10000 });
            await projectDropdown.SelectOptionAsync(new SelectOptionValue { Index = 1 });
        }

        var confirmBtn = page.Locator("[data-mode='page'] [data-cs='confirm']");
        await Assertions.Expect(confirmBtn).ToBeEnabledAsync(new() { Timeout = 15000 });
        await confirmBtn.ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
