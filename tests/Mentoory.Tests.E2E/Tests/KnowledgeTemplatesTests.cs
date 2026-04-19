using FluentAssertions;
using Mentoory.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace Mentoory.Tests.E2E.Tests;

/// <summary>
/// E2E smoke for spec 016 US1 — GlobalAdmin curates the knowledge catalog.
/// Covers: templates list renders with seeded data, TemplateDetail loads the tree,
/// new-template happy path, and ProjectCoordinator is denied access to the templates URL.
/// </summary>
[Collection(E2ETestCollection.Name)]
public class KnowledgeTemplatesTests
{
    private readonly PlaywrightFixture _fixture;

    public KnowledgeTemplatesTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Templates_PageLoads_ShowsSeededTemplate()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsGlobalAdminAsync(page);
            await page.GotoAsync($"{_fixture.BaseUrl}/Coordination/Knowledge/Templates");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            page.Url.Should().Contain("/Coordination/Knowledge/Templates",
                "GlobalAdmin should reach the templates list without redirects");

            var content = await page.ContentAsync();
            content.Should().Contain("Emprendimiento Básico",
                "the seeded 'Emprendimiento Básico' template must be listed");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Templates_PageLoads_ShowsSeededTemplate));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task TemplateDetail_PageLoads_ShowsTree()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsGlobalAdminAsync(page);
            await page.GotoAsync($"{_fixture.BaseUrl}/Coordination/Knowledge/Templates");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var row = page.Locator("tr").Filter(new LocatorFilterOptions { HasText = "Emprendimiento Básico" }).First;
            await row.WaitForAsync(new LocatorWaitForOptions { Timeout = 5000 });

            var detailLink = row.Locator("a[href*='/Templates/']").First;
            await detailLink.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            page.Url.Should().MatchRegex(@"/Coordination/Knowledge/Templates/[0-9a-fA-F-]{36}",
                "template detail URL must include the template's ExternalId");

            var content = await page.ContentAsync();
            content.Should().Contain("Ideación",
                "seeded Module 'Ideación' should appear in the detail tree");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(TemplateDetail_PageLoads_ShowsTree));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task CreateTemplate_HappyPath_AppearsInList()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsGlobalAdminAsync(page);
            await page.GotoAsync($"{_fixture.BaseUrl}/Coordination/Knowledge/Templates");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var newButton = page.Locator("a, button").Filter(new LocatorFilterOptions
            {
                HasText = "Nueva plantilla",
            }).First;

            if (await newButton.CountAsync() == 0)
            {
                await page.GotoAsync($"{_fixture.BaseUrl}/Coordination/Knowledge/Templates/Create");
            }
            else
            {
                await newButton.ClickAsync();
            }

            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var templateName = $"E2E KS Template {Guid.NewGuid():N}";
            await page.FillAsync("input[name='Name']", templateName);

            var descriptionField = page.Locator("textarea[name='Description'], input[name='Description']").First;
            if (await descriptionField.CountAsync() > 0)
            {
                await descriptionField.FillAsync("E2E smoke description");
            }

            await page.Locator("button[type='submit']").Filter(new LocatorFilterOptions
            {
                HasText = "Crear plantilla",
            }).First.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await page.GotoAsync($"{_fixture.BaseUrl}/Coordination/Knowledge/Templates");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var content = await page.ContentAsync();
            content.Should().Contain(templateName,
                "the newly created template must appear in the list after a successful save");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(CreateTemplate_HappyPath_AppearsInList));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task Templates_CoordinatorCannotAccess()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAndSelectContextAsync(page, "coord1@test.mentoory.com", "Test123!@#");
            var response = await page.GotoAsync($"{_fixture.BaseUrl}/Coordination/Knowledge/Templates");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var reachedTemplates = page.Url.Contains("/Coordination/Knowledge/Templates", StringComparison.OrdinalIgnoreCase)
                && !page.Url.Contains("/Access/Login", StringComparison.OrdinalIgnoreCase);
            if (reachedTemplates)
            {
                var status = response?.Status ?? 200;
                (status >= 400).Should().BeTrue(
                    $"ProjectCoordinator must not be allowed to view the GlobalAdmin-only templates list (got HTTP {status})");
            }
            else
            {
                page.Url.Should().NotContain("/Coordination/Knowledge/Templates",
                    "ProjectCoordinator should be redirected away from the GlobalAdmin-only templates list");
            }
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Templates_CoordinatorCannotAccess));
            await page.Context.DisposeAsync();
        }
    }

    private Task LoginAsGlobalAdminAsync(IPage page) =>
        // 'multirole@test.mentoory.com' carries GlobalAdmin in the test seed (004 § 4).
        KnowledgeTestHelpers.LoginAndSelectAsync(
            page, _fixture.BaseUrl, "multirole@test.mentoory.com", "Test123!@#", ContextSelection.GlobalAdmin);

    private Task LoginAndSelectContextAsync(IPage page, string email, string password) =>
        KnowledgeTestHelpers.LoginAndSelectAsync(
            page, _fixture.BaseUrl, email, password, ContextSelection.FirstEnabled);
}
