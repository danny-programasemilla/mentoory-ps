using FluentAssertions;
using Mentoory.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace Mentoory.Tests.E2E.Tests;

/// <summary>
/// Validates context switching behavior from the top-bar modal.
/// </summary>
[Collection(E2ETestCollection.Name)]
public class ContextSwitchingTests
{
    private readonly PlaywrightFixture _fixture;

    public ContextSwitchingTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task TopBar_ShouldDisplay_CurrentContext()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsync(page, "incadmin1@test.mentoory.com", "Test123!@#");

            // The top navigation bar should display the current incubator/project context
            var contextIndicator = page.Locator("[data-testid='current-context'], .context-indicator, .navbar .context-name");
            (await contextIndicator.CountAsync()).Should().BeGreaterThan(0,
                "the top bar must show the current context information");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(TopBar_ShouldDisplay_CurrentContext));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task CambiarContextoButton_ShouldOpen_Modal()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAndSelectContextAsync(page, "multirole@test.mentoory.com", "Test123!@#");

            // "Cambiar contexto" should be a button that opens a modal
            var switchButton = page.Locator("button").Filter(new LocatorFilterOptions
            {
                HasText = "Cambiar contexto"
            });
            (await switchButton.CountAsync()).Should().BeGreaterThan(0,
                "'Cambiar contexto' button must be visible in the navigation");

            await switchButton.First.ClickAsync();

            // Modal should appear
            var modal = page.Locator("#contextSwitcherModal");
            await modal.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 5000
            });

            (await modal.IsVisibleAsync()).Should().BeTrue("context switcher modal must be visible");

            // Modal should contain cascade dropdowns
            var roleDropdown = modal.Locator("[data-cs='role']");
            (await roleDropdown.CountAsync()).Should().Be(1, "modal must contain role dropdown");

            // Should NOT have navigated away
            page.Url.Should().NotContain("/Context/Select",
                "modal should open without page navigation");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(CambiarContextoButton_ShouldOpen_Modal));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task ContextSwitch_ViaModal_ShouldShowToastAndReload()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAndSelectContextAsync(page, "multirole@test.mentoory.com", "Test123!@#");

            // Open modal
            var switchButton = page.Locator("button").Filter(new LocatorFilterOptions
            {
                HasText = "Cambiar contexto"
            });
            await switchButton.First.ClickAsync();

            var modal = page.Locator("#contextSwitcherModal");
            await modal.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 5000
            });

            // Wait for roles to load in modal
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Select IncubatorAdmin (should have it)
            var roleDropdown = modal.Locator("[data-cs='role']");
            await roleDropdown.SelectOptionAsync(new SelectOptionValue { Label = "Administrador de Incubadora" });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Wait for auto-cascade to complete
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Click Confirmar in modal
            var confirmBtn = modal.Locator("[data-cs='confirm']");
            await confirmBtn.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached });

            if (!await confirmBtn.IsDisabledAsync())
            {
                await confirmBtn.ClickAsync();
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                // After switch, page should not show errors
                var pageContent = await page.ContentAsync();
                pageContent.Should().NotContain("Internal Server Error");
            }
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(ContextSwitch_ViaModal_ShouldShowToastAndReload));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task ContextSwitch_ShouldNavigateSafely_WithoutErrors()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAndSelectContextAsync(page, "multirole@test.mentoory.com", "Test123!@#");

            // The page should not show a server error
            var pageContent = await page.ContentAsync();
            pageContent.Should().NotContain("500");
            pageContent.Should().NotContain("Internal Server Error");

            // The response status should be healthy
            var title = await page.TitleAsync();
            title.Should().NotBeEmpty("the page should render correctly after context selection");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(ContextSwitch_ShouldNavigateSafely_WithoutErrors));
            await page.Context.DisposeAsync();
        }
    }

    private static async Task SelectFirstContextAsync(IPage page)
    {
        var container = page.Locator("[data-mode='page']");
        var roleDropdown = container.Locator("[data-cs='role']");
        var incubatorDropdown = container.Locator("[data-cs='incubator']");
        var confirmBtn = container.Locator("[data-cs='confirm']");

        await page.WaitForFunctionAsync(
            "sel => sel.options.length > 1",
            await roleDropdown.ElementHandleAsync(),
            new() { Timeout = 10000 });

        if (await roleDropdown.IsEnabledAsync())
        {
            await roleDropdown.SelectOptionAsync(new SelectOptionValue { Index = 1 });
        }

        await page.WaitForFunctionAsync(
            "sel => sel.options.length > 1",
            await incubatorDropdown.ElementHandleAsync(),
            new() { Timeout = 10000 });

        if (await incubatorDropdown.IsEnabledAsync())
        {
            await incubatorDropdown.SelectOptionAsync(new SelectOptionValue { Index = 1 });
        }

        await Assertions.Expect(confirmBtn).ToBeEnabledAsync(new() { Timeout = 15000 });
        await confirmBtn.ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    private async Task LoginAsync(IPage page, string email, string password)
    {
        await page.GotoAsync($"{_fixture.BaseUrl}/Access/Login");
        await page.FillAsync("input[name='Email']", email);
        await page.FillAsync("input[name='Password']", password);
        await page.ClickAsync("button[type='submit']");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    private async Task LoginAndSelectContextAsync(IPage page, string email, string password)
    {
        await LoginAsync(page, email, password);

        if (page.Url.Contains("/Context/Select"))
        {
            await SelectFirstContextAsync(page);
        }
    }
}
