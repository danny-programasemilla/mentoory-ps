using FluentAssertions;
using Mentoory.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace Mentoory.Tests.E2E.Tests;

/// <summary>
/// T049 - Validates authorization boundaries: roles cannot access areas they lack permissions for.
/// </summary>
[Collection(E2ETestCollection.Name)]
public class AuthorizationTests
{
    private readonly PlaywrightFixture _fixture;

    public AuthorizationTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Entrepreneur_ShouldNotAccess_AdministrationArea()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsync(page, "entrepreneur1@test.mentoory.com", "Test123!@#");

            // Attempt to navigate to the Administration area
            var response = await page.GotoAsync($"{_fixture.BaseUrl}/Administration/Dashboard");

            // Should be denied access — either redirected to Access Denied, login, or receive 403
            var denied = response?.Status == 403
                         || page.Url.Contains("/Access/Login")
                         || page.Url.Contains("/AccessDenied")
                         || page.Url.Contains("/Access/AccessDenied");

            denied.Should().BeTrue(
                "Entrepreneur role must not have access to the Administration area");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Entrepreneur_ShouldNotAccess_AdministrationArea));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task IncubatorAdmin_ShouldNotAccess_PlatformIncubators()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsync(page, "incadmin1@test.mentoory.com", "Test123!@#");

            // Attempt to navigate to the Platform-level Incubators page (GlobalAdmin only)
            var response = await page.GotoAsync($"{_fixture.BaseUrl}/Platform/Incubators");

            var denied = response?.Status == 403
                         || page.Url.Contains("/Access/Login")
                         || page.Url.Contains("/AccessDenied")
                         || page.Url.Contains("/Access/AccessDenied");

            denied.Should().BeTrue(
                "IncubatorAdmin role must not have access to the Platform/Incubators area");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(IncubatorAdmin_ShouldNotAccess_PlatformIncubators));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task GlobalAdmin_ShouldAccess_AllAreas()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsync(page, "admin@mentoory.com", "123abc987");

            // Platform area
            var platformResponse = await page.GotoAsync($"{_fixture.BaseUrl}/Platform/Incubators");
            platformResponse!.Status.Should().Be(200,
                "GlobalAdmin must have access to Platform/Incubators");

            // Administration area
            var adminResponse = await page.GotoAsync($"{_fixture.BaseUrl}/Administration/Dashboard");
            adminResponse!.Status.Should().Be(200,
                "GlobalAdmin must have access to Administration/Dashboard");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(GlobalAdmin_ShouldAccess_AllAreas));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task Sponsor_ShouldNotAccess_CoordinationArea()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsync(page, "sponsor1@test.mentoory.com", "Test123!@#");

            var response = await page.GotoAsync($"{_fixture.BaseUrl}/Coordination/Diagnostics");

            var denied = response?.Status == 403
                         || page.Url.Contains("/Access/Login")
                         || page.Url.Contains("/AccessDenied")
                         || page.Url.Contains("/Access/AccessDenied");

            denied.Should().BeTrue(
                "Sponsor role must not have access to the Coordination area");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Sponsor_ShouldNotAccess_CoordinationArea));
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
