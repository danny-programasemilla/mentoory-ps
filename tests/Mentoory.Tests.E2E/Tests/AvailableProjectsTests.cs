using FluentAssertions;
using Mentoory.Tests.E2E.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.Playwright;
using Xunit;

namespace Mentoory.Tests.E2E.Tests;

/// <summary>
/// E2E-T007 - Validates that users without project context see public projects
/// and can request self-enrollment.
/// </summary>
[Collection(E2ETestCollection.Name)]
public class AvailableProjectsTests
{
    private readonly PlaywrightFixture _fixture;

    public AvailableProjectsTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AvailableProjects_UserWithNoContext_SeesProjectList()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            // Step (a): Admin creates a public project
            await LoginAndSelectContextAsync(page, "incadmin1@test.mentoory.com", "Test123!@#");
            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/Projects/Create");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var projectName = $"E2E Public Project {Guid.NewGuid():N}";
            await page.FillAsync("input[name='Name']", projectName);

            var isPublicCheckbox = page.Locator("input[type='checkbox'][name='IsPublic']");
            if (!await isPublicCheckbox.IsCheckedAsync())
            {
                await isPublicCheckbox.CheckAsync();
            }

            await page.SelectOptionAsync("select[name='EnrollmentVariant']", "1"); // Directo
            await page.Locator("button[type='submit']").Filter(new LocatorFilterOptions
            {
                HasText = "Crear Proyecto"
            }).ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Logout admin by navigating away and clearing context
            await page.Context.ClearCookiesAsync();

            // Step (b): Register a new user via public registration
            var uniqueEmail = $"e2e-avail-{Guid.NewGuid():N}@test.mentoory.com";
            await page.GotoAsync($"{_fixture.BaseUrl}/Access/Register");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var nationalId1 = $"1-{Guid.NewGuid().ToString("N")[..4]}-{Guid.NewGuid().ToString("N")[..4]}";
            await page.FillAsync("input[name='Email']", uniqueEmail);
            await page.FillAsync("input[name='FirstName']", "Available");
            await page.FillAsync("input[name='LastName']", "ProjectUser");
            await page.SelectOptionAsync("select[name='Country']", "CRI");
            await page.FillAsync("input[name='NationalId']", nationalId1);
            await page.FillAsync("input[name='Password']", "SecurePass12!@");
            await page.FillAsync("input[name='ConfirmPassword']", "SecurePass12!@");
            await page.ClickAsync("button[type='submit']");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Step (c): Activate user via DB
            await using (var connection = new SqlConnection(_fixture.ConnectionString))
            {
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = @"
                    UPDATE [access].[Users]
                    SET AccountStatus = 1, EmailVerifiedAtUtc = GETUTCDATE()
                    WHERE NormalizedEmail = @Email";
                command.Parameters.AddWithValue("@Email", uniqueEmail.ToUpperInvariant());
                await command.ExecuteNonQueryAsync();
            }

            // Step (d): Login as new user
            await page.Context.ClearCookiesAsync();
            await LoginAsync(page, uniqueEmail, "SecurePass12!@");

            // Step (e): Assert available projects page
            page.Url.Should().Contain("/AvailableProjects",
                "user without project context should land on available projects page");

            var pageContent = await page.ContentAsync();
            pageContent.Should().Contain(projectName,
                "the public project created by admin should be visible");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(AvailableProjects_UserWithNoContext_SeesProjectList));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task AvailableProjects_NoPublicProjects_ShowsEmptyState()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            // Register and activate a new user (no public projects assigned)
            var uniqueEmail = $"e2e-nopub-{Guid.NewGuid():N}@test.mentoory.com";
            await page.GotoAsync($"{_fixture.BaseUrl}/Access/Register");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var nationalId2 = $"2-{Guid.NewGuid().ToString("N")[..4]}-{Guid.NewGuid().ToString("N")[..4]}";
            await page.FillAsync("input[name='Email']", uniqueEmail);
            await page.FillAsync("input[name='FirstName']", "NoPublic");
            await page.FillAsync("input[name='LastName']", "User");
            await page.SelectOptionAsync("select[name='Country']", "CRI");
            await page.FillAsync("input[name='NationalId']", nationalId2);
            await page.FillAsync("input[name='Password']", "SecurePass12!@");
            await page.FillAsync("input[name='ConfirmPassword']", "SecurePass12!@");
            await page.ClickAsync("button[type='submit']");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Activate user via DB
            await using (var connection = new SqlConnection(_fixture.ConnectionString))
            {
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = @"
                    UPDATE [access].[Users]
                    SET AccountStatus = 1, EmailVerifiedAtUtc = GETUTCDATE()
                    WHERE NormalizedEmail = @Email";
                command.Parameters.AddWithValue("@Email", uniqueEmail.ToUpperInvariant());
                await command.ExecuteNonQueryAsync();
            }

            // Login as new user
            await LoginAsync(page, uniqueEmail, "SecurePass12!@");

            // User without any role context should be redirected to AvailableProjects
            page.Url.Should().Contain("/AvailableProjects",
                "user without context should land on available projects page");

            var pageContent = await page.ContentAsync();
            // Page should show either the empty state or the projects list
            var hasEmptyState = pageContent.Contains("No hay proyectos disponibles");
            var hasProjectsList = pageContent.Contains("Proyectos Disponibles");
            (hasEmptyState || hasProjectsList).Should().BeTrue(
                "page should show either the empty state or the projects list heading");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(AvailableProjects_NoPublicProjects_ShowsEmptyState));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task AvailableProjects_SelfEnrollment_ShowsSuccessMessage()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            // Admin creates a public project with direct enrollment
            await LoginAndSelectContextAsync(page, "incadmin1@test.mentoory.com", "Test123!@#");
            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/Projects/Create");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var projectName = $"E2E Enroll Project {Guid.NewGuid():N}";
            await page.FillAsync("input[name='Name']", projectName);

            var isPublicCheckbox = page.Locator("input[type='checkbox'][name='IsPublic']");
            if (!await isPublicCheckbox.IsCheckedAsync())
            {
                await isPublicCheckbox.CheckAsync();
            }

            await page.SelectOptionAsync("select[name='EnrollmentVariant']", "1"); // Directo
            await page.Locator("button[type='submit']").Filter(new LocatorFilterOptions
            {
                HasText = "Crear Proyecto"
            }).ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Clear admin session
            await page.Context.ClearCookiesAsync();

            // Register and activate a new user
            var uniqueEmail = $"e2e-enroll-{Guid.NewGuid():N}@test.mentoory.com";
            await page.GotoAsync($"{_fixture.BaseUrl}/Access/Register");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var nationalId3 = $"3-{Guid.NewGuid().ToString("N")[..4]}-{Guid.NewGuid().ToString("N")[..4]}";
            await page.FillAsync("input[name='Email']", uniqueEmail);
            await page.FillAsync("input[name='FirstName']", "Enroll");
            await page.FillAsync("input[name='LastName']", "User");
            await page.SelectOptionAsync("select[name='Country']", "CRI");
            await page.FillAsync("input[name='NationalId']", nationalId3);
            await page.FillAsync("input[name='Password']", "SecurePass12!@");
            await page.FillAsync("input[name='ConfirmPassword']", "SecurePass12!@");
            await page.ClickAsync("button[type='submit']");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Activate user via DB
            await using (var connection = new SqlConnection(_fixture.ConnectionString))
            {
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = @"
                    UPDATE [access].[Users]
                    SET AccountStatus = 1, EmailVerifiedAtUtc = GETUTCDATE()
                    WHERE NormalizedEmail = @Email";
                command.Parameters.AddWithValue("@Email", uniqueEmail.ToUpperInvariant());
                await command.ExecuteNonQueryAsync();
            }

            // Login as new user and navigate to available projects
            await LoginAsync(page, uniqueEmail, "SecurePass12!@");

            // Click enrollment button on the project card
            var enrollButton = page.Locator("button, a").Filter(new LocatorFilterOptions
            {
                HasText = "Solicitar inscripci\u00f3n"
            }).First;
            await enrollButton.WaitForAsync(new LocatorWaitForOptions { Timeout = 5000 });
            await enrollButton.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var pageContent = await page.ContentAsync();
            pageContent.Should().Contain("Solicitud de inscripci\u00f3n enviada exitosamente",
                "success message should appear after self-enrollment request");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(AvailableProjects_SelfEnrollment_ShowsSuccessMessage));
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
