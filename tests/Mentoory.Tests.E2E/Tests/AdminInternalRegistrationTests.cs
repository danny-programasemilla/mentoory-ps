using FluentAssertions;
using Mentoory.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace Mentoory.Tests.E2E.Tests;

/// <summary>
/// Validates the unified user creation form (Create), redirect behavior
/// from legacy routes (RegisterInternal, Enroll), and form field structure.
/// </summary>
[Collection(E2ETestCollection.Name)]
public class AdminInternalRegistrationTests
{
    private readonly PlaywrightFixture _fixture;

    public AdminInternalRegistrationTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task RegisterInternal_RedirectsToCreate()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsync(page, "incadmin1@test.mentoory.com", "Test123!@#");
            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/Users/RegisterInternal");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            page.Url.Should().Contain("/Administration/Users/Create",
                "RegisterInternal should redirect to the unified Create page");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(RegisterInternal_RedirectsToCreate));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task Enroll_RedirectsToCreate()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsync(page, "incadmin1@test.mentoory.com", "Test123!@#");
            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/Users/Enroll");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            page.Url.Should().Contain("/Administration/Users/Create",
                "Enroll should redirect to the unified Create page");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Enroll_RedirectsToCreate));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task Create_PageLoads_WithCorrectFormFields()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAndSelectContextWithProjectAsync(page, "admin@mentoory.com", "123abc987");
            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/Users/Create");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert heading
            var heading = page.Locator("h1, h2, h3, h4, h5").Filter(new LocatorFilterOptions
            {
                HasText = "Crear Usuario"
            });
            (await heading.CountAsync()).Should().BeGreaterThan(0,
                "page should show 'Crear Usuario' heading");

            // Assert all required form fields are present
            (await page.Locator("input[name='Email']").CountAsync()).Should().Be(1,
                "Email field should be present");
            (await page.Locator("select[name='Country']").CountAsync()).Should().Be(1,
                "Country dropdown should be present");
            (await page.Locator("input[name='Identification']").CountAsync()).Should().Be(1,
                "Identification field should be present");
            (await page.Locator("input[name='FirstName']").CountAsync()).Should().Be(1,
                "FirstName field should be present");
            (await page.Locator("input[name='LastName']").CountAsync()).Should().Be(1,
                "LastName field should be present");

            // Assert toggle switches
            (await page.Locator("input[type='checkbox'][name='SkipEmailVerification']").CountAsync()).Should().Be(1,
                "SkipEmailVerification toggle should be present");
            (await page.Locator("input[type='checkbox'][name='SkipInvitationAcceptance']").CountAsync()).Should().Be(1,
                "SkipInvitationAcceptance toggle should be present");

            // Assert Country dropdown has Costa Rica option
            var costaRicaOption = page.Locator("select[name='Country'] option[value='CRI']");
            (await costaRicaOption.CountAsync()).Should().Be(1,
                "Country dropdown should contain Costa Rica (CRI) option");

            // Assert NO password field (unified form removed it)
            (await page.Locator("input[name='Password']").CountAsync()).Should().Be(0,
                "Password field should NOT be present on the unified Create form");

            // Assert NO project dropdown (project comes from session context)
            (await page.Locator("select[name='ProjectExternalId']").CountAsync()).Should().Be(0,
                "ProjectExternalId dropdown should NOT be present on the unified Create form");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Create_PageLoads_WithCorrectFormFields));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task Create_EmptyForm_ShowsValidationErrors()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAndSelectContextWithProjectAsync(page, "admin@mentoory.com", "123abc987");
            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/Users/Create");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Submit empty form
            await page.ClickAsync("button[type='submit']");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert validation errors appear
            var validationErrors = page.Locator(".text-danger, .input-validation-error, .validation-summary-errors, .field-validation-error");
            (await validationErrors.CountAsync()).Should().BeGreaterThan(0,
                "submitting an empty form should show validation errors");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Create_EmptyForm_ShowsValidationErrors));
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

        // Wait for login redirect chain to complete
        await page.WaitForURLAsync(url => !url.Contains("/Access/Login"), new PageWaitForURLOptions { Timeout = 10000 });
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // If redirected to context selection, pick first available context (no project needed)
        if (page.Url.Contains("/Context/Select"))
        {
            var container = page.Locator("[data-mode='page']");
            var confirmBtn = container.Locator("[data-cs='confirm']");
            await Assertions.Expect(confirmBtn).ToBeEnabledAsync(new() { Timeout = 20000 });
            await confirmBtn.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
    }

    private async Task LoginAndSelectContextWithProjectAsync(IPage page, string email, string password)
    {
        await page.GotoAsync($"{_fixture.BaseUrl}/Access/Login");
        await page.FillAsync("input[name='Email']", email);
        await page.FillAsync("input[name='Password']", password);
        await page.ClickAsync("button[type='submit']");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Wait for login redirect chain to complete
        await page.WaitForURLAsync(url => !url.Contains("/Access/Login"), new PageWaitForURLOptions { Timeout = 10000 });
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // If redirected to context selection, explicitly select role → incubator → project
        if (page.Url.Contains("/Context/Select"))
        {
            var container = page.Locator("[data-mode='page']");

            // Select role (first non-empty option)
            var roleDropdown = container.Locator("[data-cs='role']");
            await page.WaitForFunctionAsync(
                "sel => sel.options.length > 1",
                await roleDropdown.ElementHandleAsync(),
                new() { Timeout = 10000 });
            if (await roleDropdown.IsEnabledAsync())
            {
                await roleDropdown.SelectOptionAsync(new SelectOptionValue { Index = 1 });
            }

            // Select incubator (first non-empty option)
            var incubatorDropdown = container.Locator("[data-cs='incubator']");
            await page.WaitForFunctionAsync(
                "sel => sel.options.length > 1",
                await incubatorDropdown.ElementHandleAsync(),
                new() { Timeout = 10000 });
            if (await incubatorDropdown.IsEnabledAsync())
            {
                await incubatorDropdown.SelectOptionAsync(new SelectOptionValue { Index = 1 });
            }

            // Select project if available (first non-empty option)
            var projectDropdown = container.Locator("[data-cs='project']");
            await page.WaitForFunctionAsync(
                "sel => sel.options.length > 1",
                await projectDropdown.ElementHandleAsync(),
                new() { Timeout = 10000 });
            if (await projectDropdown.IsEnabledAsync())
            {
                await projectDropdown.SelectOptionAsync(new SelectOptionValue { Index = 1 });
            }

            var confirmBtn = container.Locator("[data-cs='confirm']");
            await Assertions.Expect(confirmBtn).ToBeEnabledAsync(new() { Timeout = 15000 });
            await confirmBtn.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
    }
}
