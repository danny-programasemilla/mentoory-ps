using System.Text;
using FluentAssertions;
using Mentoory.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace Mentoory.Tests.E2E.Tests;

/// <summary>
/// E2E-T012 - End-to-end composite test validating the complete batch user onboarding:
/// CSV upload -> temp password -> forced password change -> application access.
/// </summary>
[Collection(E2ETestCollection.Name)]
public class BatchUserJourneyTests
{
    private readonly PlaywrightFixture _fixture;

    public BatchUserJourneyTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task BatchCreatedUser_Login_ForcedPasswordChange_ThenAccess()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            // (a) Admin login and upload CSV with 1 user
            await LoginAndSelectContextAsync(page, "incadmin1@test.mentoory.com", "Test123!@#");
            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/BatchUpload");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var batchUserEmail = $"e2e-journey-{Guid.NewGuid():N}@test.mentoory.com";
            var csvContent = $"Country,Identification,Email,FirstName,LastName\nCRI,6-7890-1234,{batchUserEmail},Journey,BatchUser";
            var csvBytes = Encoding.UTF8.GetBytes(csvContent);

            var fileInput = page.Locator("input[type='file']");
            await fileInput.SetInputFilesAsync(new FilePayload
            {
                Name = "test.csv",
                MimeType = "text/csv",
                Buffer = csvBytes
            });

            // Select the first available project from the dropdown
            await SelectFirstProjectOptionAsync(page);

            await page.Locator("button[type='submit']").Filter(new LocatorFilterOptions
            {
                HasText = "Procesar Archivo"
            }).ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // (b) Extract temporary password from results table
            var tempPasswordCell = page.Locator("table tbody tr.table-success td:nth-child(6)");
            var tempPassword = await tempPasswordCell.TextContentAsync();
            tempPassword.Should().NotBeNullOrWhiteSpace(
                "temporary password should be present in the results table");
            tempPassword = tempPassword!.Trim();

            // (c) Logout admin
            var logoutButton = page.Locator("form[action*='Logout'] button[type='submit'], a[href*='Logout']").First;
            await logoutButton.WaitForAsync(new LocatorWaitForOptions { Timeout = 5000 });
            await logoutButton.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // (d) Login as batch-created user with temp password
            await page.GotoAsync($"{_fixture.BaseUrl}/Access/Login");
            await page.FillAsync("input[name='Email']", batchUserEmail);
            await page.FillAsync("input[name='Password']", tempPassword);
            await page.ClickAsync("button[type='submit']");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // (e) Assert redirect to ChangePassword
            page.Url.Should().Contain("/Access/ChangePassword",
                "batch-created user should be redirected to forced password change");

            // (f) Change password
            await page.FillAsync("input[name='CurrentPassword']", tempPassword);
            await page.FillAsync("input[name='NewPassword']", "NewSecurePass12!");
            await page.FillAsync("input[name='ConfirmNewPassword']", "NewSecurePass12!");
            await page.Locator("button[type='submit']").Filter(new LocatorFilterOptions
            {
                HasText = "Cambiar Contraseña"
            }).ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // (g) Assert redirect away from ChangePassword
            page.Url.Should().NotContain("/Access/ChangePassword",
                "after changing password, user should be redirected away from the change password page");

            var redirectedToContextOrProjects = page.Url.Contains("/Context/Select")
                                                || page.Url.Contains("/AvailableProjects");
            redirectedToContextOrProjects.Should().BeTrue(
                "after password change, user should be redirected to context selection or available projects");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(BatchCreatedUser_Login_ForcedPasswordChange_ThenAccess));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task BatchCreatedUser_CannotSkipPasswordChange()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            // Admin uploads CSV with 1 user
            await LoginAndSelectContextAsync(page, "incadmin1@test.mentoory.com", "Test123!@#");
            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/BatchUpload");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var batchUserEmail = $"e2e-skip-{Guid.NewGuid():N}@test.mentoory.com";
            var csvContent = $"Country,Identification,Email,FirstName,LastName\nCRI,7-8901-2345,{batchUserEmail},Skip,BatchUser";
            var csvBytes = Encoding.UTF8.GetBytes(csvContent);

            var fileInput = page.Locator("input[type='file']");
            await fileInput.SetInputFilesAsync(new FilePayload
            {
                Name = "test.csv",
                MimeType = "text/csv",
                Buffer = csvBytes
            });

            // Select the first available project from the dropdown
            await SelectFirstProjectOptionAsync(page);

            await page.Locator("button[type='submit']").Filter(new LocatorFilterOptions
            {
                HasText = "Procesar Archivo"
            }).ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Extract temporary password
            var tempPasswordCell = page.Locator("table tbody tr.table-success td:nth-child(6)");
            var tempPassword = await tempPasswordCell.TextContentAsync();
            tempPassword = tempPassword!.Trim();

            // Logout admin
            var logoutButton = page.Locator("form[action*='Logout'] button[type='submit'], a[href*='Logout']").First;
            await logoutButton.WaitForAsync(new LocatorWaitForOptions { Timeout = 5000 });
            await logoutButton.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Login as batch user
            await page.GotoAsync($"{_fixture.BaseUrl}/Access/Login");
            await page.FillAsync("input[name='Email']", batchUserEmail);
            await page.FillAsync("input[name='Password']", tempPassword);
            await page.ClickAsync("button[type='submit']");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Attempt to navigate to a protected page, bypassing password change
            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/Dashboard");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            page.Url.Should().Contain("/Access/ChangePassword",
                "batch user with PasswordResetRequired should be redirected back to change password page");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(BatchCreatedUser_CannotSkipPasswordChange));
            await page.Context.DisposeAsync();
        }
    }

    private static async Task SelectFirstProjectOptionAsync(IPage page)
    {
        var projectSelect = page.Locator("select[name='ProjectExternalId']");
        // Select the first non-empty option (skip the "Seleccione un proyecto" placeholder)
        var firstOption = projectSelect.Locator("option:not([value=''])").First;
        var value = await firstOption.GetAttributeAsync("value");
        value.Should().NotBeNullOrWhiteSpace("there should be at least one project in Registration stage");
        await projectSelect.SelectOptionAsync(value!);
    }

    private async Task LoginAndSelectContextAsync(IPage page, string email, string password)
    {
        await page.GotoAsync($"{_fixture.BaseUrl}/Access/Login");
        await page.FillAsync("input[name='Email']", email);
        await page.FillAsync("input[name='Password']", password);
        await page.ClickAsync("button[type='submit']");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Wait for login redirect chain to complete (login -> context -> home)
        await page.WaitForURLAsync(url => !url.Contains("/Access/Login"), new PageWaitForURLOptions { Timeout = 10000 });
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // If redirected to context selection, pick the first available context
        if (page.Url.Contains("/Context/Select"))
        {
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            var roleDropdown = page.Locator("[data-cs='role']");
            await roleDropdown.SelectOptionAsync(new SelectOptionValue { Index = 1 });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            var confirmBtn = page.Locator("[data-cs='confirm']");
            await confirmBtn.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
    }
}
