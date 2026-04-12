using FluentAssertions;
using Mentoory.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace Mentoory.Tests.E2E.Tests;

/// <summary>
/// E2E-T008 - Validates GlobalAdmin can view and update system configuration values,
/// and non-GlobalAdmin users cannot access the configuration page.
/// </summary>
[Collection(E2ETestCollection.Name)]
public class ConfigurationManagementTests
{
    private readonly PlaywrightFixture _fixture;

    public ConfigurationManagementTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Configuration_PageLoads_WithAllSeededKeys()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsGlobalAdminAsync(page);
            await page.GotoAsync($"{_fixture.BaseUrl}/Platform/Configuration");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert table is visible
            var table = page.Locator("table");
            (await table.CountAsync()).Should().BeGreaterThan(0,
                "configuration page should contain a table");

            // Assert all 7 seeded configuration keys are present
            var expectedKeys = new[]
            {
                "EmailVerificationTokenExpiryHours",
                "PasswordResetTokenExpiryHours",
                "InvitationTokenExpiryHours",
                "MaxFailedLoginAttempts",
                "LockoutDurationMinutes",
                "SessionTimeoutHours",
                "PasswordHistoryDepth"
            };

            var pageContent = await page.ContentAsync();
            foreach (var key in expectedKeys)
            {
                pageContent.Should().Contain(key,
                    $"configuration key '{key}' should be present on the page");
            }
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Configuration_PageLoads_WithAllSeededKeys));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task Configuration_UpdateValue_ShowsSuccessMessage()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsGlobalAdminAsync(page);
            await page.GotoAsync($"{_fixture.BaseUrl}/Platform/Configuration");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Find the row for MaxFailedLoginAttempts and update its value
            var targetRow = page.Locator("tr").Filter(new LocatorFilterOptions
            {
                HasText = "MaxFailedLoginAttempts"
            });

            var valueInput = targetRow.Locator("input[type='text'], input[type='number']").First;
            await valueInput.ClearAsync();
            await valueInput.FillAsync("10");

            // Click the save button in that row
            var saveButton = targetRow.Locator("button[type='submit']").First;
            await saveButton.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var pageContent = await page.ContentAsync();
            pageContent.Should().Contain("Configuraci\u00f3n actualizada exitosamente",
                "success message should appear after updating configuration value");

            // Cleanup: reset value back to 5
            await page.GotoAsync($"{_fixture.BaseUrl}/Platform/Configuration");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var resetRow = page.Locator("tr").Filter(new LocatorFilterOptions
            {
                HasText = "MaxFailedLoginAttempts"
            });

            var resetInput = resetRow.Locator("input[type='text'], input[type='number']").First;
            await resetInput.ClearAsync();
            await resetInput.FillAsync("5");

            var resetButton = resetRow.Locator("button[type='submit']").First;
            await resetButton.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Configuration_UpdateValue_ShowsSuccessMessage));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task Configuration_NonGlobalAdmin_CannotAccess()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAndSelectContextAsync(page, "incadmin1@test.mentoory.com", "Test123!@#");

            var response = await page.GotoAsync($"{_fixture.BaseUrl}/Platform/Configuration");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var accessDenied = response?.Status == 403
                               || page.Url.Contains("/Access/Login")
                               || page.Url.Contains("/AccessDenied")
                               || page.Url.Contains("/Access/AccessDenied");

            accessDenied.Should().BeTrue(
                "non-GlobalAdmin users should not be able to access the configuration page");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Configuration_NonGlobalAdmin_CannotAccess));
            await page.Context.DisposeAsync();
        }
    }

    private async Task LoginAsGlobalAdminAsync(IPage page)
    {
        await page.GotoAsync($"{_fixture.BaseUrl}/Access/Login");
        await page.FillAsync("input[name='Email']", "admin@mentoory.com");
        await page.FillAsync("input[name='Password']", "123abc987");
        await page.ClickAsync("button[type='submit']");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // GlobalAdmin sees context selection — pick the first context
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

    private async Task LoginAndSelectContextAsync(IPage page, string email, string password)
    {
        await page.GotoAsync($"{_fixture.BaseUrl}/Access/Login");
        await page.FillAsync("input[name='Email']", email);
        await page.FillAsync("input[name='Password']", password);
        await page.ClickAsync("button[type='submit']");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

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
