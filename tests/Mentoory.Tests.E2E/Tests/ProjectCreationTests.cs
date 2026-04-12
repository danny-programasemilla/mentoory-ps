using FluentAssertions;
using Mentoory.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace Mentoory.Tests.E2E.Tests;

/// <summary>
/// E2E-T009 - Validates that the project creation form includes IsPublic checkbox,
/// EnrollmentVariant dropdown, and standard project fields.
/// </summary>
[Collection(E2ETestCollection.Name)]
public class ProjectCreationTests
{
    private readonly PlaywrightFixture _fixture;

    public ProjectCreationTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateProject_PageLoads_WithNewFields()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAndSelectContextAsync(page, "incadmin1@test.mentoory.com", "Test123!@#");
            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/Projects/Create");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert IsPublic checkbox is present
            var isPublicCheckbox = page.Locator("input[type='checkbox'][name='IsPublic']");
            (await isPublicCheckbox.CountAsync()).Should().Be(1,
                "project creation form should have an IsPublic checkbox");

            // Assert EnrollmentVariant dropdown is present
            var enrollmentDropdown = page.Locator("select[name='EnrollmentVariant']");
            (await enrollmentDropdown.CountAsync()).Should().Be(1,
                "project creation form should have an EnrollmentVariant dropdown");

            // Assert dropdown options
            var flujoCompletoOption = enrollmentDropdown.Locator("option[value='0']");
            (await flujoCompletoOption.CountAsync()).Should().Be(1,
                "EnrollmentVariant should have 'Flujo completo' option (value 0)");

            var directoOption = enrollmentDropdown.Locator("option[value='1']");
            (await directoOption.CountAsync()).Should().Be(1,
                "EnrollmentVariant should have 'Directo' option (value 1)");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(CreateProject_PageLoads_WithNewFields));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task CreateProject_ValidSubmission_RedirectsToProjectsList()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAndSelectContextAsync(page, "incadmin1@test.mentoory.com", "Test123!@#");
            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/Projects/Create");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var projectName = $"E2E Project {Guid.NewGuid():N}";
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

            page.Url.Should().Contain("/Administration/Projects",
                "after valid project creation, should redirect to projects list");

            var pageContent = await page.ContentAsync();
            pageContent.Should().Contain("Proyecto creado exitosamente",
                "success message should be displayed after project creation");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(CreateProject_ValidSubmission_RedirectsToProjectsList));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task CreateProject_EmptyName_ShowsValidationError()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAndSelectContextAsync(page, "incadmin1@test.mentoory.com", "Test123!@#");
            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/Projects/Create");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Leave Name field empty and submit
            await page.ClickAsync("button[type='submit']");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert validation error appears
            var validationErrors = page.Locator(".text-danger, .input-validation-error, .validation-summary-errors, .field-validation-error");
            (await validationErrors.CountAsync()).Should().BeGreaterThan(0,
                "submitting with an empty name should show a validation error");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(CreateProject_EmptyName_ShowsValidationError));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task CreateProject_IsPublicCheckbox_DefaultsToUnchecked()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAndSelectContextAsync(page, "incadmin1@test.mentoory.com", "Test123!@#");
            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/Projects/Create");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var isPublicCheckbox = page.Locator("input[type='checkbox'][name='IsPublic']");
            var isChecked = await isPublicCheckbox.IsCheckedAsync();

            isChecked.Should().BeFalse(
                "IsPublic checkbox should default to unchecked on the create project page");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(CreateProject_IsPublicCheckbox_DefaultsToUnchecked));
            await page.Context.DisposeAsync();
        }
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
