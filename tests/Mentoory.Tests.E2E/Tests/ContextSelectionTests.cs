using FluentAssertions;
using Mentoory.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace Mentoory.Tests.E2E.Tests;

/// <summary>
/// T047 - Validates context selection behavior for multi-role and single-role users.
/// </summary>
[Collection(E2ETestCollection.Name)]
public class ContextSelectionTests
{
    private readonly PlaywrightFixture _fixture;

    public ContextSelectionTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task MultiRoleUser_ShouldSee_ContextSelectionScreen()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsync(page, "multirole@test.mentoory.com", "Test123!@#");

            // Multi-role user should be presented with a role/context selection screen
            var selectionHeading = page.Locator("h1, h2, h3").Filter(new LocatorFilterOptions
            {
                HasText = "Seleccionar"
            });
            (await selectionHeading.CountAsync()).Should().BeGreaterThan(0,
                "multi-role users must see a context selection screen");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(MultiRoleUser_ShouldSee_ContextSelectionScreen));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task SingleRoleUser_ShouldAutoSelect_Context()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsync(page, "entrepreneur1@test.mentoory.com", "Test123!@#");

            // Single-role user should skip selection and land directly on their dashboard
            page.Url.Should().NotContain("/Context/Select",
                "single-role users should bypass the context selection screen");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(SingleRoleUser_ShouldAutoSelect_Context));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task ReturnUrl_ShouldBePreserved_AfterContextSelection()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            // Navigate to a protected page while unauthenticated to generate returnUrl
            var targetPath = "/Coordination/Diagnostics";
            await page.GotoAsync($"{_fixture.BaseUrl}{targetPath}");

            // Should be redirected to login with returnUrl
            page.Url.Should().Contain("/Access/Login");

            // Fill the login form on the CURRENT page (which already has returnUrl in the form action)
            // Do NOT navigate to /Access/Login fresh, as that would lose the returnUrl
            await page.FillAsync("input[name='Email']", "coord1@test.mentoory.com");
            await page.FillAsync("input[name='Password']", "Test123!@#");
            await page.ClickAsync("button[type='submit']");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // After login and context selection, the user should be redirected to the original target
            page.Url.Should().Contain(targetPath,
                "returnUrl must be preserved through the login and context selection flow");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(ReturnUrl_ShouldBePreserved_AfterContextSelection));
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
    }
}
