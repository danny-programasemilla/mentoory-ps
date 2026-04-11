using FluentAssertions;
using Mentoory.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace Mentoory.Tests.E2E.Tests;

/// <summary>
/// E2E-T011 - Validates that logout terminates the session and subsequent
/// requests require re-authentication.
/// </summary>
[Collection(E2ETestCollection.Name)]
public class LogoutTests
{
    private readonly PlaywrightFixture _fixture;

    public LogoutTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Logout_AuthenticatedUser_RedirectsToLogin()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsync(page, "entrepreneur1@test.mentoory.com", "Test123!@#");

            // Verify user is authenticated (not on login page)
            page.Url.Should().NotContain("/Access/Login",
                "user should be authenticated and past the login page");

            // Find and submit the logout mechanism in the navigation
            var logoutButton = page.Locator("form[action*='Logout'] button[type='submit'], a[href*='Logout']").First;
            await logoutButton.WaitForAsync(new LocatorWaitForOptions { Timeout = 5000 });
            await logoutButton.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            page.Url.Should().Contain("/Access/Login",
                "after logout, user should be redirected to the login page");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Logout_AuthenticatedUser_RedirectsToLogin));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task Logout_ThenAccessProtectedPage_RedirectsToLogin()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsync(page, "entrepreneur1@test.mentoory.com", "Test123!@#");

            // Logout
            var logoutButton = page.Locator("form[action*='Logout'] button[type='submit'], a[href*='Logout']").First;
            await logoutButton.WaitForAsync(new LocatorWaitForOptions { Timeout = 5000 });
            await logoutButton.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Attempt to access a protected page after logout
            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/Dashboard");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            page.Url.Should().Contain("/Access/Login",
                "accessing a protected page after logout should redirect to login");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Logout_ThenAccessProtectedPage_RedirectsToLogin));
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
