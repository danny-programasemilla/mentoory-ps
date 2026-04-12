using FluentAssertions;
using Mentoory.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace Mentoory.Tests.E2E.Tests;

/// <summary>
/// Validates the unified user creation workflow: toggle behavior, onboarding,
/// temporary password display, form state with/without project context,
/// duplicate handling, and role-based access.
/// </summary>
[Collection(E2ETestCollection.Name)]
public class UnifiedUserCreationTests
{
    private readonly PlaywrightFixture _fixture;

    public UnifiedUserCreationTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Create_BothTogglesOff_CreatesUserWithOnboarding()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAndSelectContextWithProjectAsync(page, "admin@mentoory.com", "123abc987");
            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/Users/Create");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            var uniqueEmail = $"e2e-onboard-{uniqueId}@test.mentoory.com";

            await page.FillAsync("input[name='Email']", uniqueEmail);
            await page.SelectOptionAsync("select[name='Country']", "CRI");
            await page.FillAsync("input[name='Identification']", $"1-{uniqueId[..4]}-{uniqueId[4..]}");
            await page.FillAsync("input[name='FirstName']", "OnboardTest");
            await page.FillAsync("input[name='LastName']", "User");

            // Ensure both toggles are OFF (default state)
            var skipEmail = page.Locator("input[type='checkbox'][name='SkipEmailVerification']");
            if (await skipEmail.IsCheckedAsync())
            {
                await skipEmail.UncheckAsync();
            }

            var skipInvitation = page.Locator("input[type='checkbox'][name='SkipInvitationAcceptance']");
            if (await skipInvitation.IsCheckedAsync())
            {
                await skipInvitation.UncheckAsync();
            }

            await page.Locator("button[type='submit']").ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // On success, controller redirects to Index with TempData messages
            page.Url.Should().Contain("/Administration/Users",
                "after successful creation, should redirect to users list");

            var pageContent = await page.ContentAsync();
            pageContent.Should().Contain("creado exitosamente",
                "success message should indicate user was created");
            pageContent.Should().Contain("incorporaci",
                "success message should mention the onboarding process");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Create_BothTogglesOff_CreatesUserWithOnboarding));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task Create_BothTogglesOn_ShowsTemporaryPassword()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAndSelectContextWithProjectAsync(page, "admin@mentoory.com", "123abc987");
            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/Users/Create");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            var uniqueEmail = $"e2e-direct-{uniqueId}@test.mentoory.com";

            await page.FillAsync("input[name='Email']", uniqueEmail);
            await page.SelectOptionAsync("select[name='Country']", "CRI");
            await page.FillAsync("input[name='Identification']", $"2-{uniqueId[..4]}-{uniqueId[4..]}");
            await page.FillAsync("input[name='FirstName']", "DirectTest");
            await page.FillAsync("input[name='LastName']", "User");

            // Turn both toggles ON
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

            await page.Locator("button[type='submit']").ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // On success, redirects to Index with temp password in TempData
            page.Url.Should().Contain("/Administration/Users",
                "after successful creation, should redirect to users list");

            var pageContent = await page.ContentAsync();
            pageContent.Should().Contain("creado exitosamente",
                "success message should indicate user was created");
            pageContent.Should().Contain("Contrasena temporal",
                "temporary password section should be displayed");

            // Verify the <code> element with the password exists
            var passwordCode = page.Locator("code.user-select-all");
            (await passwordCode.CountAsync()).Should().BeGreaterThan(0,
                "a <code> element with the temporary password should be displayed");

            var passwordText = await passwordCode.First.TextContentAsync();
            passwordText.Should().NotBeNullOrWhiteSpace(
                "the temporary password should not be empty");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Create_BothTogglesOn_ShowsTemporaryPassword));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task Create_NoProjectContext_FormDisabled()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            // incadmin1 auto-skips context selection without project
            await LoginAsync(page, "incadmin1@test.mentoory.com", "Test123!@#");
            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/Users/Create");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Verify warning alert is shown
            var warningAlert = page.Locator(".alert-warning");
            await Assertions.Expect(warningAlert).ToBeVisibleAsync();
            var alertText = await warningAlert.TextContentAsync();
            alertText.Should().Contain("Seleccione un proyecto desde el selector de contexto para continuar",
                "warning should tell user to select a project");

            // Verify fieldset is disabled
            var fieldset = page.Locator("fieldset[disabled], fieldset:disabled");
            (await fieldset.CountAsync()).Should().BeGreaterThan(0,
                "form fieldset should be disabled when no project is in session context");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Create_NoProjectContext_FormDisabled));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task Create_WithProjectContext_FormEnabled()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAndSelectContextWithProjectAsync(page, "admin@mentoory.com", "123abc987");
            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/Users/Create");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Verify form is NOT disabled
            var disabledFieldset = page.Locator("fieldset[disabled]");
            (await disabledFieldset.CountAsync()).Should().Be(0,
                "form fieldset should NOT be disabled when project context is active");

            // Verify project name is displayed
            var pageContent = await page.ContentAsync();
            pageContent.Should().Contain("Proyecto:",
                "active project name should be displayed on the form");

            // Verify the email field is enabled and interactive
            var emailInput = page.Locator("input[name='Email']");
            await Assertions.Expect(emailInput).ToBeEnabledAsync();
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Create_WithProjectContext_FormEnabled));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task Create_DuplicateUser_ShowsEnrolledMessage()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAndSelectContextWithProjectAsync(page, "admin@mentoory.com", "123abc987");

            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            var uniqueEmail = $"e2e-dup-{uniqueId}@test.mentoory.com";
            var identification = $"3-{uniqueId[..4]}-{uniqueId[4..]}";

            // First creation
            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/Users/Create");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await page.FillAsync("input[name='Email']", uniqueEmail);
            await page.SelectOptionAsync("select[name='Country']", "CRI");
            await page.FillAsync("input[name='Identification']", identification);
            await page.FillAsync("input[name='FirstName']", "Duplicate");
            await page.FillAsync("input[name='LastName']", "Test");

            // Both toggles ON for fast creation
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

            await page.Locator("button[type='submit']").ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Verify first creation succeeded
            page.Url.Should().Contain("/Administration/Users",
                "first creation should redirect to users list");

            // Second creation with same email
            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/Users/Create");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await page.FillAsync("input[name='Email']", uniqueEmail);
            await page.SelectOptionAsync("select[name='Country']", "CRI");
            await page.FillAsync("input[name='Identification']", identification);
            await page.FillAsync("input[name='FirstName']", "Duplicate");
            await page.FillAsync("input[name='LastName']", "Test");

            var skipEmail2 = page.Locator("input[type='checkbox'][name='SkipEmailVerification']");
            if (!await skipEmail2.IsCheckedAsync())
            {
                await skipEmail2.CheckAsync();
            }

            var skipInvitation2 = page.Locator("input[type='checkbox'][name='SkipInvitationAcceptance']");
            if (!await skipInvitation2.IsCheckedAsync())
            {
                await skipInvitation2.CheckAsync();
            }

            await page.Locator("button[type='submit']").ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Should show "existing user enrolled" or "already enrolled" message
            var pageContent = await page.ContentAsync();
            var hasEnrolledMessage = pageContent.Contains("existente inscrito")
                                     || pageContent.Contains("ya est");
            hasEnrolledMessage.Should().BeTrue(
                "second creation of same user should show enrolled or already-enrolled message");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Create_DuplicateUser_ShowsEnrolledMessage));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task OnboardingExpired_PageLoads()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await page.GotoAsync($"{_fixture.BaseUrl}/Access/Onboarding/Expired");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Verify heading
            var heading = page.Locator("h1, h2, h3, h4, h5").Filter(new LocatorFilterOptions
            {
                HasText = "Enlace Expirado"
            });
            (await heading.CountAsync()).Should().BeGreaterThan(0,
                "page should show 'Enlace Expirado' heading");

            // Verify expiration text
            var pageContent = await page.ContentAsync();
            pageContent.Should().Contain("expirado",
                "page should contain text about the expired link");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(OnboardingExpired_PageLoads));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task UsersIndex_ShowsOnboardingStatusColumn()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAndSelectContextWithProjectAsync(page, "admin@mentoory.com", "123abc987");
            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/Users");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Verify DataTable has "Incorporacion" header
            var onboardingHeader = page.Locator("#usersTable th").Filter(new LocatorFilterOptions
            {
                HasText = "Incorporacion"
            });
            (await onboardingHeader.CountAsync()).Should().BeGreaterThan(0,
                "users DataTable should have an 'Incorporacion' column header");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(UsersIndex_ShowsOnboardingStatusColumn));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task GlobalAdmin_CanAccessCreateForm()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAndSelectContextWithProjectAsync(page, "admin@mentoory.com", "123abc987");

            var response = await page.GotoAsync($"{_fixture.BaseUrl}/Administration/Users/Create");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            response!.Status.Should().Be(200,
                "GlobalAdmin should receive 200 for the Create page");

            var heading = page.Locator("h1, h2, h3, h4, h5").Filter(new LocatorFilterOptions
            {
                HasText = "Crear Usuario"
            });
            (await heading.CountAsync()).Should().BeGreaterThan(0,
                "Create form heading should be visible for GlobalAdmin");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(GlobalAdmin_CanAccessCreateForm));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task Entrepreneur_CannotAccessCreateForm()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsync(page, "entrepreneur1@test.mentoory.com", "Test123!@#");

            var response = await page.GotoAsync($"{_fixture.BaseUrl}/Administration/Users/Create");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var denied = response?.Status == 403
                         || page.Url.Contains("/Access/Login")
                         || page.Url.Contains("/AccessDenied")
                         || page.Url.Contains("/Access/AccessDenied");

            denied.Should().BeTrue(
                "Entrepreneur role must not have access to the user creation form");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Entrepreneur_CannotAccessCreateForm));
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

        // If redirected to context selection, ensure project is selected
        if (page.Url.Contains("/Context/Select"))
        {
            var container = page.Locator("[data-mode='page']");
            var confirmBtn = container.Locator("[data-cs='confirm']");

            // Wait for cascade to auto-complete (confirm becomes enabled)
            await Assertions.Expect(confirmBtn).ToBeEnabledAsync(new() { Timeout = 20000 });

            // Ensure project is selected (may have auto-selected already)
            var projectDropdown = container.Locator("[data-cs='project']");
            var projectOptions = projectDropdown.Locator("option:not([value=''])");
            if (await projectOptions.CountAsync() > 0)
            {
                var selectedValue = await projectDropdown.InputValueAsync();
                if (string.IsNullOrEmpty(selectedValue))
                {
                    await projectDropdown.SelectOptionAsync(new SelectOptionValue { Index = 1 });
                    await page.WaitForTimeoutAsync(500);
                }
            }

            await confirmBtn.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
    }
}
