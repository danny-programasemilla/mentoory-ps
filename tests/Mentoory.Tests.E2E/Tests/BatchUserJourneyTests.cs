using System.Text;
using FluentAssertions;
using Mentoory.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace Mentoory.Tests.E2E.Tests;

/// <summary>
/// E2E-T012 - End-to-end composite test validating the complete batch user onboarding:
/// GlobalAdmin with project context -> CSV upload with both toggles ON ->
/// temp password -> forced password change -> application access.
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
            // (a) Login as GlobalAdmin with project via cascade context selector
            await LoginAsGlobalAdminWithProjectAsync(page);

            // (b) Navigate to batch upload, upload CSV with both toggles ON
            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/BatchUpload");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            var batchUserEmail = $"e2e-journey-{uniqueId}@test.mentoory.com";
            var csvContent = $"Country,Identification,Email,FirstName,LastName\nCRI,6-7890-1234,{batchUserEmail},Journey,BatchUser";
            var csvBytes = Encoding.UTF8.GetBytes(csvContent);

            var fileInput = page.Locator("input[type='file']");
            await fileInput.SetInputFilesAsync(new FilePayload
            {
                Name = "test.csv",
                MimeType = "text/csv",
                Buffer = csvBytes
            });

            // Turn BOTH toggles ON to get temporary password
            await page.Locator("input[type='checkbox'][name='SkipEmailVerification']").CheckAsync();
            await page.Locator("input[type='checkbox'][name='SkipInvitationAcceptance']").CheckAsync();

            // (c) Submit
            await page.Locator("button[type='submit']").Filter(new LocatorFilterOptions
            {
                HasText = "Procesar Archivo"
            }).ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var pageContent = await page.ContentAsync();
            pageContent.Should().Contain("Resultados de Carga Masiva",
                "results page should display after batch upload");

            // (d) Extract temporary password from table (6th column of table-success row)
            var tempPasswordCell = page.Locator("table tbody tr.table-success td:nth-child(6)");
            var tempPassword = await tempPasswordCell.TextContentAsync();
            tempPassword.Should().NotBeNullOrWhiteSpace(
                "temporary password should be present in the results table");
            tempPassword = tempPassword!.Trim();
            tempPassword.Should().NotBe("\u2014",
                "temporary password should not be a dash placeholder");

            // (e) Logout admin
            var logoutButton = page.Locator("form[action*='Logout'] button[type='submit'], a[href*='Logout']").First;
            await logoutButton.WaitForAsync(new LocatorWaitForOptions { Timeout = 5000 });
            await logoutButton.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // (f) Login as batch-created user with temp password
            await page.GotoAsync($"{_fixture.BaseUrl}/Access/Login");
            await page.FillAsync("input[name='Email']", batchUserEmail);
            await page.FillAsync("input[name='Password']", tempPassword);
            await page.ClickAsync("button[type='submit']");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // (g) Assert redirect to ChangePassword
            page.Url.Should().Contain("/Access/ChangePassword",
                "batch-created user should be redirected to forced password change");

            // (h) Fill change password form
            await page.FillAsync("input[name='CurrentPassword']", tempPassword);
            await page.FillAsync("input[name='NewPassword']", "NewSecurePass12!");
            await page.FillAsync("input[name='ConfirmNewPassword']", "NewSecurePass12!");
            await page.Locator("button[type='submit']").Filter(new LocatorFilterOptions
            {
                HasText = "Cambiar Contrase"
            }).ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // (i) Assert redirect away from ChangePassword
            page.Url.Should().NotContain("/Access/ChangePassword",
                "after changing password, user should be redirected away from the change password page");

            // (j) Assert redirect to ContextSelect or AvailableProjects
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
            // Login as GlobalAdmin with project context
            await LoginAsGlobalAdminWithProjectAsync(page);

            // Upload CSV with both toggles ON
            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/BatchUpload");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            var batchUserEmail = $"e2e-skip-{uniqueId}@test.mentoory.com";
            var csvContent = $"Country,Identification,Email,FirstName,LastName\nCRI,7-8901-2345,{batchUserEmail},Skip,BatchUser";
            var csvBytes = Encoding.UTF8.GetBytes(csvContent);

            var fileInput = page.Locator("input[type='file']");
            await fileInput.SetInputFilesAsync(new FilePayload
            {
                Name = "test.csv",
                MimeType = "text/csv",
                Buffer = csvBytes
            });

            // Turn BOTH toggles ON to get temporary password
            await page.Locator("input[type='checkbox'][name='SkipEmailVerification']").CheckAsync();
            await page.Locator("input[type='checkbox'][name='SkipInvitationAcceptance']").CheckAsync();

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

    private async Task LoginAsGlobalAdminWithProjectAsync(IPage page)
    {
        await page.GotoAsync($"{_fixture.BaseUrl}/Access/Login");
        await page.FillAsync("input[name='Email']", "admin@mentoory.com");
        await page.FillAsync("input[name='Password']", "123abc987");
        await page.ClickAsync("button[type='submit']");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await page.WaitForURLAsync(url => !url.Contains("/Access/Login"),
            new PageWaitForURLOptions { Timeout = 10000 });
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        if (page.Url.Contains("/Context/Select"))
        {
            // Wait for cascade to auto-complete or manually select
            var confirmBtn = page.Locator("[data-mode='page'] [data-cs='confirm']");
            await Assertions.Expect(confirmBtn).ToBeEnabledAsync(new() { Timeout = 20000 });

            // Ensure project is selected (needed for batch upload)
            var projectDropdown = page.Locator("[data-mode='page'] [data-cs='project']");
            var currentProjectValue = await projectDropdown.InputValueAsync();
            if (string.IsNullOrEmpty(currentProjectValue))
            {
                var projectOptions = projectDropdown.Locator("option:not([value=''])");
                if (await projectOptions.CountAsync() > 0)
                {
                    await projectDropdown.SelectOptionAsync(new SelectOptionValue { Index = 1 });
                    await page.WaitForTimeoutAsync(300);
                }
            }

            await confirmBtn.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
    }
}
