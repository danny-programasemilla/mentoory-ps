using FluentAssertions;
using Mentoory.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace Mentoory.Tests.E2E.Tests;

/// <summary>
/// T053 - Validates that each role's dashboard loads with correct content.
/// </summary>
[Collection(E2ETestCollection.Name)]
public class DashboardRenderingTests
{
    private readonly PlaywrightFixture _fixture;

    public DashboardRenderingTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GlobalAdminDashboard_ShouldRender_IncubatorList()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsync(page, "admin@mentoory.com", "123abc987");

            await page.GotoAsync($"{_fixture.BaseUrl}/Platform/Incubators");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Should display a list or table of incubators
            var content = page.Locator("table, .card, [data-testid='incubator-list']");
            (await content.CountAsync()).Should().BeGreaterThan(0,
                "GlobalAdmin dashboard should render an incubator listing");

            // Page title or heading should be meaningful
            var title = await page.TitleAsync();
            title.Should().NotBeNullOrWhiteSpace(
                "the page should have a descriptive title");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(GlobalAdminDashboard_ShouldRender_IncubatorList));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task IncubatorAdminDashboard_ShouldRender_WithContent()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsync(page, "incadmin1@test.mentoory.com", "Test123!@#");

            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/Dashboard");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // The dashboard should not be empty and should not show a server error
            var pageContent = await page.ContentAsync();
            pageContent.Should().NotContain("Internal Server Error");

            var heading = page.Locator("h1, h2").First;
            (await heading.IsVisibleAsync()).Should().BeTrue(
                "IncubatorAdmin dashboard should display a heading");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(IncubatorAdminDashboard_ShouldRender_WithContent));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task CoordinatorDashboard_ShouldRender_DiagnosticsContent()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsync(page, "coord1@test.mentoory.com", "Test123!@#");

            await page.GotoAsync($"{_fixture.BaseUrl}/Coordination/Diagnostics");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var pageContent = await page.ContentAsync();
            pageContent.Should().NotContain("Internal Server Error");

            // Coordinator diagnostics page should have diagnostic-related content
            var diagnosticContent = page.Locator("table, .card, [data-testid='diagnostic-list']");
            (await diagnosticContent.CountAsync()).Should().BeGreaterThan(0,
                "Coordinator dashboard should render diagnostic content");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(CoordinatorDashboard_ShouldRender_DiagnosticsContent));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task EntrepreneurDashboard_ShouldRender_ParticipantContent()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsync(page, "entrepreneur1@test.mentoory.com", "Test123!@#");

            await page.GotoAsync($"{_fixture.BaseUrl}/Participant/Diagnostic");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var pageContent = await page.ContentAsync();
            pageContent.Should().NotContain("Internal Server Error");

            // Entrepreneur should see their diagnostic forms or an empty-state message
            var title = await page.TitleAsync();
            title.Should().NotBeNullOrWhiteSpace(
                "Entrepreneur dashboard page should have a title");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(EntrepreneurDashboard_ShouldRender_ParticipantContent));
            await page.Context.DisposeAsync();
        }
    }

    private async Task LoginAsync(IPage page, string email, string password)
    {
        await page.GotoAsync($"{_fixture.BaseUrl}/Access/Login");
        await page.FillAsync("input[name='Email']", email);
        await page.FillAsync("input[name='Password']", password);
        await page.ClickAsync("button[type='submit']");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // GlobalAdmin sees context selection with incubator choices; pick the first one
        if (page.Url.Contains("/Context/Select"))
        {
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
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

            var confirmBtn = page.Locator("[data-mode='page'] [data-cs='confirm']");
            await Assertions.Expect(confirmBtn).ToBeEnabledAsync(new() { Timeout = 15000 });
            await confirmBtn.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
    }
}
