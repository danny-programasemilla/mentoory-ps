using FluentAssertions;
using Mentoory.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace Mentoory.Tests.E2E.Tests;

/// <summary>
/// T048 - Validates context switching behavior from the top bar navigation.
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
    public async Task CambiarContextoLink_ShouldNavigateTo_ContextSelection()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsync(page, "multirole@test.mentoory.com", "Test123!@#");

            // Multi-role user lands on context selection — pick the first context
            var selectButton = page.Locator("button[type='submit']").Filter(new LocatorFilterOptions
            {
                HasText = "Seleccionar"
            });
            await selectButton.First.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Now the top bar should show the "Cambiar contexto" link
            var switchLink = page.Locator("a").Filter(new LocatorFilterOptions
            {
                HasText = "Cambiar contexto"
            });
            (await switchLink.CountAsync()).Should().BeGreaterThan(0,
                "a 'Cambiar contexto' link must be visible in the navigation");

            await switchLink.First.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Should navigate to the context selection page
            page.Url.Should().Contain("/Context",
                "clicking 'Cambiar contexto' should navigate to the context selection page");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(CambiarContextoLink_ShouldNavigateTo_ContextSelection));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task ContextSwitch_ShouldNavigateSafely_WithoutErrors()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsync(page, "multirole@test.mentoory.com", "Test123!@#");

            // Navigate to a known page first
            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/Dashboard");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Switch context via the link
            var switchLink = page.Locator("a").Filter(new LocatorFilterOptions
            {
                HasText = "Cambiar contexto"
            });

            if (await switchLink.CountAsync() > 0)
            {
                await switchLink.First.ClickAsync();
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            }

            // After context switch, the page should not show a server error
            var pageContent = await page.ContentAsync();
            pageContent.Should().NotContain("500");
            pageContent.Should().NotContain("Internal Server Error");

            // The response status should be healthy (no crash)
            var title = await page.TitleAsync();
            title.Should().NotBeEmpty("the page should render correctly after context switch");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(ContextSwitch_ShouldNavigateSafely_WithoutErrors));
            await page.Context.DisposeAsync();
        }
    }

    private async Task LoginAsync(IPage page, string email, string password)
    {
        await page.GotoAsync($"{_fixture.BaseUrl}/Identity/Login");
        await page.FillAsync("input[name='Email']", email);
        await page.FillAsync("input[name='Password']", password);
        await page.ClickAsync("button[type='submit']");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
