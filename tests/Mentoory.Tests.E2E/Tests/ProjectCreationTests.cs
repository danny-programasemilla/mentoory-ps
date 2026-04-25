using FluentAssertions;
using Mentoory.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace Mentoory.Tests.E2E.Tests;

/// <summary>
/// E2E coverage for spec 016 Phase 9 project-creation fields (IsPublic, EnrollmentVariant,
/// required KS-template dropdown), kept under spec 017 Phase 4 US2 regression umbrella.
///
/// | Spec 017 scenario | Method |
/// |---|---|
/// | US2-2 (required KS dropdown + Emprendimiento Básico option) | CreateProject_PageLoads_WithNewFields |
/// | US2-2 (happy-path redirect + success toast)                 | CreateProject_ValidSubmission_RedirectsToProjectsList |
/// | US2-3 (missing KS template blocks submit)                   | CreateProject_MissingKsTemplate_ShowsValidationError |
/// | US2-2 (IsPublic default)                                     | CreateProject_IsPublicCheckbox_DefaultsToUnchecked |
/// | Name validation (pre-Phase 9 regression)                     | CreateProject_EmptyName_ShowsValidationError |
/// </summary>
[Collection(E2ETestCollection.Name)]
[Trait("Category", "E2E")]
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

            var isPublicCheckbox = page.Locator("input[type='checkbox'][name='IsPublic']");
            (await isPublicCheckbox.CountAsync()).Should().Be(1,
                "project creation form should have an IsPublic checkbox");

            var enrollmentDropdown = page.Locator("select[name='EnrollmentVariant']");
            (await enrollmentDropdown.CountAsync()).Should().Be(1,
                "project creation form should have an EnrollmentVariant dropdown");

            var flujoCompletoOption = enrollmentDropdown.Locator("option[value='0']");
            (await flujoCompletoOption.CountAsync()).Should().Be(1,
                "EnrollmentVariant should have 'Flujo completo' option (value 0)");

            var directoOption = enrollmentDropdown.Locator("option[value='1']");
            (await directoOption.CountAsync()).Should().Be(1,
                "EnrollmentVariant should have 'Directo' option (value 1)");

            var ksTemplateDropdown = page.Locator("select[name='KnowledgeStructureTemplateExternalId']");
            (await ksTemplateDropdown.CountAsync()).Should().Be(1,
                "Phase 9: project creation form must expose a required KS-template dropdown");

            var seededTemplateOption = ksTemplateDropdown.Locator("option").Filter(new LocatorFilterOptions
            {
                HasText = "Emprendimiento Básico",
            });
            (await seededTemplateOption.CountAsync()).Should().BeGreaterThan(0,
                "the seeded 'Emprendimiento Básico' template should appear in the dropdown");
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

            await page.SelectOptionAsync("select[name='EnrollmentVariant']", "1");
            await SelectFirstKnowledgeTemplateAsync(page);

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

            await page.Locator("button[type='submit']").Filter(new LocatorFilterOptions
            {
                HasText = "Crear Proyecto"
            }).ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

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
    public async Task CreateProject_MissingKsTemplate_ShowsValidationError()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAndSelectContextAsync(page, "incadmin1@test.mentoory.com", "Test123!@#");
            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/Projects/Create");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var projectName = $"E2E KS-missing {Guid.NewGuid():N}";
            await page.FillAsync("input[name='Name']", projectName);

            await page.Locator("button[type='submit']").Filter(new LocatorFilterOptions
            {
                HasText = "Crear Proyecto"
            }).ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            page.Url.Should().Contain("/Administration/Projects/Create",
                "Phase 9: without a KS template the form must not submit");

            var pageContent = await page.ContentAsync();
            pageContent.Should().Contain("plantilla de conocimiento",
                "validation error for the missing KS-template selection should be displayed");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(CreateProject_MissingKsTemplate_ShowsValidationError));
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

    private static async Task SelectFirstKnowledgeTemplateAsync(IPage page)
    {
        var ksDropdown = page.Locator("select[name='KnowledgeStructureTemplateExternalId']");
        await ksDropdown.WaitForAsync(new LocatorWaitForOptions { Timeout = 5000 });

        var values = await ksDropdown.Locator("option").EvaluateAllAsync<string[]>(
            "nodes => nodes.map(n => n.value).filter(v => v && v.length > 0)");

        values.Should().NotBeEmpty("KS-template dropdown must have at least one real option (seeded 'Emprendimiento Básico')");
        await ksDropdown.SelectOptionAsync(values[0]);
    }

    private Task LoginAndSelectContextAsync(IPage page, string email, string password) =>
        KnowledgeTestHelpers.LoginAndSelectAsync(
            page, _fixture.BaseUrl, email, password, ContextSelection.FirstEnabled);
}
