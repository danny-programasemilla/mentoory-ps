using FluentAssertions;
using Mentoory.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace Mentoory.Tests.E2E.Tests;

/// <summary>
/// E2E-T005 - Validates admin user creation form with country dropdown,
/// toggle switches for verification/invitation, and project context requirement.
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
    public async Task Create_PageLoads_WithCountryDropdownAndFields()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAndSelectContextAsync(page, "multirole@test.mentoory.com", "Test123!@#");
            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/Users/Create");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Assert all required form fields are present
            (await page.Locator("input[name='Email']").CountAsync()).Should().Be(1);
            (await page.Locator("select[name='Country']").CountAsync()).Should().Be(1);
            (await page.Locator("input[name='Identification']").CountAsync()).Should().Be(1);
            (await page.Locator("input[name='FirstName']").CountAsync()).Should().Be(1);
            (await page.Locator("input[name='LastName']").CountAsync()).Should().Be(1);

            // Toggle switches for verification and invitation
            (await page.Locator("input[type='checkbox'][name='SkipEmailVerification']").CountAsync()).Should().Be(1);
            (await page.Locator("input[type='checkbox'][name='SkipInvitationAcceptance']").CountAsync()).Should().Be(1);

            // Assert Country dropdown has Costa Rica option
            var costaRicaOption = page.Locator("select[name='Country'] option[value='CRI']");
            (await costaRicaOption.CountAsync()).Should().Be(1,
                "Country dropdown should contain Costa Rica (CRI) option");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Create_PageLoads_WithCountryDropdownAndFields));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task Create_SuccessfulCreation_RedirectsToUsersList()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAndSelectContextAsync(page, "multirole@test.mentoory.com", "Test123!@#");
            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/Users/Create");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            var uniqueEmail = $"e2e-create-{uniqueId}@test.mentoory.com";
            var uniqueNationalId = $"1-{uniqueId[..4]}-{uniqueId[4..8]}";

            await page.FillAsync("input[name='Email']", uniqueEmail);
            await page.SelectOptionAsync("select[name='Country']", "CRI");
            await page.FillAsync("input[name='Identification']", uniqueNationalId);
            await page.FillAsync("input[name='FirstName']", "E2E");
            await page.FillAsync("input[name='LastName']", "TestUser");

            // Enable both toggles for simplest flow (skip verification + skip invitation = temp password)
            var skipEmail = page.Locator("input[type='checkbox'][name='SkipEmailVerification']");
            if (!await skipEmail.IsCheckedAsync())
            {
                await skipEmail.CheckAsync();
            }

            var skipInvitation = page.Locator("input[type='checkbox'][name='SkipInvitationAcceptance']");
            if (!await skipInvitation.IsCheckedAsync())
            {
                await skipInvitation.CheckAsync();
            }

            await page.Locator("button[type='submit']").Filter(new LocatorFilterOptions
            {
                HasText = "Crear Usuario"
            }).ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            page.Url.Should().Contain("/Administration/Users",
                "after successful user creation, should redirect to users list");

            var pageContent = await page.ContentAsync();
            pageContent.Should().Contain("exitosamente",
                "success message should be displayed");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Create_SuccessfulCreation_RedirectsToUsersList));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task Create_EmptyForm_ShowsValidationErrors()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAndSelectContextAsync(page, "multirole@test.mentoory.com", "Test123!@#");
            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/Users/Create");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Submit empty form (filter to the Create form's submit button)
            await page.Locator("button[type='submit']").Filter(new LocatorFilterOptions
            {
                HasText = "Crear Usuario"
            }).ClickAsync();
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

    private async Task EnsureProjectContextAsync(IPage page)
    {
        // Navigate to Users/Index to get an antiforgery token (the page has a token form)
        await page.GotoAsync($"{_fixture.BaseUrl}/Administration/Users");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Use the context API to switch context with the first available project
        var hasProject = await page.EvaluateAsync<bool>(@"
            (async () => {
                try {
                    var rolesResp = await fetch('/api/context/roles');
                    var roles = await rolesResp.json();
                    if (!roles || roles.length === 0) return false;

                    var role = roles[0].role;

                    var incResp = await fetch('/api/context/incubators?role=' + role);
                    var incubators = await incResp.json();
                    if (!incubators || incubators.length === 0) return false;

                    var incubatorId = incubators[0].id;
                    var incubatorName = incubators[0].name;
                    var raExternalId = incubators[0].roleAssignmentExternalId;

                    var projResp = await fetch('/api/context/projects?role=' + role + '&incubatorId=' + incubatorId);
                    var projects = await projResp.json();
                    if (!projects || projects.length === 0) return false;

                    var tokenEl = document.querySelector('[name=""__RequestVerificationToken""]');
                    if (!tokenEl) return false;

                    var switchResp = await fetch('/api/context/switch', {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json',
                            'RequestVerificationToken': tokenEl.value
                        },
                        body: JSON.stringify({
                            roleAssignmentExternalId: raExternalId,
                            incubatorId: incubatorId,
                            incubatorName: incubatorName,
                            projectId: projects[0].id,
                            projectName: projects[0].name
                        })
                    });
                    return switchResp.ok;
                } catch (e) {
                    return false;
                }
            })()
        ");

        if (hasProject)
        {
            // Reload to pick up the updated auth cookie
            await page.ReloadAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        // If no project available, the Create form will show disabled state — this is expected
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

            // Wait for project dropdown to be enabled (populated after incubator selection)
            var projectDropdown = page.Locator("[data-mode='page'] [data-cs='project']");
            if (await projectDropdown.CountAsync() > 0)
            {
                // Wait for dropdown to become enabled (projects load async after incubator selection)
                await Assertions.Expect(projectDropdown).ToBeEnabledAsync(new() { Timeout = 15000 });
                await page.WaitForFunctionAsync(
                    "sel => sel.options.length > 1",
                    await projectDropdown.ElementHandleAsync(),
                    new() { Timeout = 10000 });
                await projectDropdown.SelectOptionAsync(new SelectOptionValue { Index = 1 });
            }

            var confirmBtn = page.Locator("[data-mode='page'] [data-cs='confirm']");
            await Assertions.Expect(confirmBtn).ToBeEnabledAsync(new() { Timeout = 15000 });
            await confirmBtn.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        // Ensure project context is set (auto-skip may not include a project).
        // Use the context API to switch context with the first available project.
        await EnsureProjectContextAsync(page);
    }
}
