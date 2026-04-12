using FluentAssertions;
using Mentoory.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace Mentoory.Tests.E2E.Tests;

/// <summary>
/// Validates cascading context selector behavior: dropdown UI, auto-skip,
/// tenant isolation, and returnUrl preservation.
/// </summary>
[Collection(E2ETestCollection.Name)]
public class ContextSelectionTests
{
    private readonly PlaywrightFixture _fixture;

    public ContextSelectionTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    // -----------------------------------------------------------------------
    // UI Behavior
    // -----------------------------------------------------------------------
    [Fact]
    public async Task MultiRoleUser_ShouldSee_CascadingDropdowns()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsync(page, "multirole@test.mentoory.com", "Test123!@#");

            page.Url.Should().Contain("/Context/Select",
                "multi-role users must land on the context selection page");

            var heading = page.Locator("h2").Filter(new LocatorFilterOptions
            {
                HasText = "Seleccionar Contexto de Trabajo"
            });
            (await heading.CountAsync()).Should().Be(1);

            // Should have cascading dropdowns (not card grid)
            var roleDropdown = page.Locator("[data-cs='role']");
            var incubatorDropdown = page.Locator("[data-cs='incubator']");
            var projectDropdown = page.Locator("[data-cs='project']");

            (await roleDropdown.CountAsync()).Should().Be(1, "role dropdown must be present");
            (await incubatorDropdown.CountAsync()).Should().Be(1, "incubator dropdown must be present");
            (await projectDropdown.CountAsync()).Should().Be(1, "project dropdown must be present");

            // Card grid should NOT be present
            var cardGrid = page.Locator(".context-card");
            (await cardGrid.CountAsync()).Should().Be(0, "old card grid must not be present");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(MultiRoleUser_ShouldSee_CascadingDropdowns));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task MultiRoleUser_RoleDropdown_PopulatesWithDistinctRoles()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsync(page, "multirole@test.mentoory.com", "Test123!@#");

            // Wait for roles to load via AJAX
            var roleDropdown = page.Locator("[data-cs='role']");
            await roleDropdown.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // multirole user has IncubatorAdmin + ProjectCoordinator → 2 role options + placeholder
            var options = roleDropdown.Locator("option");
            var count = await options.CountAsync();
            count.Should().BeGreaterThanOrEqualTo(3,
                "role dropdown should have placeholder + at least 2 roles for multi-role user");

            var optionTexts = await options.AllTextContentsAsync();
            optionTexts.Should().Contain("Administrador de Incubadora");
            optionTexts.Should().Contain("Coordinador de Proyecto");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(MultiRoleUser_RoleDropdown_PopulatesWithDistinctRoles));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task MultiRoleUser_SelectRole_PopulatesIncubatorDropdown()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsync(page, "multirole@test.mentoory.com", "Test123!@#");

            var roleDropdown = page.Locator("[data-cs='role']");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Select IncubatorAdmin role
            await roleDropdown.SelectOptionAsync(new SelectOptionValue { Label = "Administrador de Incubadora" });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Incubator dropdown should populate
            var incubatorDropdown = page.Locator("[data-cs='incubator']");
            await incubatorDropdown.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached });

            var options = incubatorDropdown.Locator("option");
            var optionTexts = await options.AllTextContentsAsync();
            optionTexts.Should().Contain("Incubadora Alpha",
                "IncubatorAdmin for Alpha should see Alpha in incubator dropdown");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(MultiRoleUser_SelectRole_PopulatesIncubatorDropdown));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task SingleRoleUser_ShouldAutoSelect_Context()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsync(page, "entrepreneur1@test.mentoory.com", "Test123!@#");

            // Single-role user should skip selection and land directly on their dashboard
            page.Url.Should().NotContain("/Context/Select",
                "single-role users should bypass the context selection screen");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(SingleRoleUser_ShouldAutoSelect_Context));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task MultiRoleUser_CanComplete_FullCascadeAndSubmit()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsync(page, "multirole@test.mentoory.com", "Test123!@#");

            // Wait for roles to load
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Select IncubatorAdmin role (incubator-scoped, no project needed)
            var roleDropdown = page.Locator("[data-cs='role']");
            await roleDropdown.SelectOptionAsync(new SelectOptionValue { Label = "Administrador de Incubadora" });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // IncubatorAdmin has only 1 incubator (Alpha) → should auto-select
            // Wait for projects to load after auto-cascade
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Confirmar button should be enabled
            var confirmBtn = page.Locator("[data-cs='confirm']");
            await confirmBtn.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached });
            (await confirmBtn.IsDisabledAsync()).Should().BeFalse("Confirmar should be enabled after role+incubator selection");

            // Submit
            await confirmBtn.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Should redirect away from context selection
            page.Url.Should().NotContain("/Context/Select",
                "after confirming context, user should leave the selection page");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(MultiRoleUser_CanComplete_FullCascadeAndSubmit));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task ReturnUrl_ShouldBePreserved_AfterContextSelection()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            // Navigate to a protected page while unauthenticated to generate returnUrl
            var targetPath = "/Coordination/Diagnostics";
            await page.GotoAsync($"{_fixture.BaseUrl}{targetPath}");

            // Should be redirected to login with returnUrl
            page.Url.Should().Contain("/Access/Login");

            // Login as multi-role user (coord1 has single role → auto-skip)
            await page.FillAsync("input[name='Email']", "coord1@test.mentoory.com");
            await page.FillAsync("input[name='Password']", "Test123!@#");
            await page.ClickAsync("button[type='submit']");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // After login and context selection, the user should be redirected to the original target
            page.Url.Should().Contain(targetPath,
                "returnUrl must be preserved through the login and context selection flow");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(ReturnUrl_ShouldBePreserved_AfterContextSelection));
            await page.Context.DisposeAsync();
        }
    }

    // -----------------------------------------------------------------------
    // Tenant Isolation — Non-GlobalAdmin sees only their assigned data
    // -----------------------------------------------------------------------
    [Fact]
    public async Task NonGlobalAdmin_CascadeApi_ReturnsOnlyAssignedIncubators()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsync(page, "multirole@test.mentoory.com", "Test123!@#");

            // Call cascade API directly from authenticated browser context
            var response = await page.APIRequest.GetAsync(
                $"{_fixture.BaseUrl}/api/context/incubators?role=IncubatorAdmin");

            response.Status.Should().Be(200);
            var body = await response.TextAsync();

            // multirole is IncubatorAdmin for Alpha ONLY — must NOT see Beta
            body.Should().Contain("Incubadora Alpha");
            body.Should().NotContain("Incubadora Beta",
                "tenant isolation: user must only see incubators they are assigned to");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(NonGlobalAdmin_CascadeApi_ReturnsOnlyAssignedIncubators));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task NonGlobalAdmin_CascadeApi_ReturnsOnlyAssignedProjects()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsync(page, "multirole@test.mentoory.com", "Test123!@#");

            // Get incubators first to find Alpha's ID
            var incResponse = await page.APIRequest.GetAsync(
                $"{_fixture.BaseUrl}/api/context/incubators?role=ProjectCoordinator");

            incResponse.Status.Should().Be(200);
            var incBody = await incResponse.TextAsync();
            incBody.Should().Contain("Incubadora Alpha");

            // Extract incubator ID from JSON response
            // multirole as ProjectCoordinator is only for Proyecto Sostenibilidad
            // Get Alpha's ID by parsing response
            var incJson = System.Text.Json.JsonDocument.Parse(incBody);
            var alphaId = incJson.RootElement.EnumerateArray()
                .First(e => e.GetProperty("name").GetString() == "Incubadora Alpha")
                .GetProperty("id").GetInt64();

            var projResponse = await page.APIRequest.GetAsync(
                $"{_fixture.BaseUrl}/api/context/projects?role=ProjectCoordinator&incubatorId={alphaId}");

            projResponse.Status.Should().Be(200);
            var projBody = await projResponse.TextAsync();

            // multirole as ProjectCoordinator is assigned to Sostenibilidad ONLY
            projBody.Should().Contain("Sostenibilidad");
            projBody.Should().NotContain("Innovación",
                "tenant isolation: user must only see projects they are assigned to for that role");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(NonGlobalAdmin_CascadeApi_ReturnsOnlyAssignedProjects));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task GlobalAdmin_CascadeApi_ReturnsAllIncubators()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAndSelectContextAsync(page, "admin@mentoory.com", "123abc987");

            var response = await page.APIRequest.GetAsync(
                $"{_fixture.BaseUrl}/api/context/incubators?role=GlobalAdmin");

            response.Status.Should().Be(200);
            var body = await response.TextAsync();

            // GlobalAdmin sees ALL incubators in the system
            body.Should().Contain("Incubadora Alpha");
            body.Should().Contain("Incubadora Beta",
                "GlobalAdmin must see all incubators regardless of assignment");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(GlobalAdmin_CascadeApi_ReturnsAllIncubators));
            await page.Context.DisposeAsync();
        }
    }

    // -----------------------------------------------------------------------
    // Security — Unauthenticated and invalid requests
    // -----------------------------------------------------------------------
    [Fact]
    public async Task UnauthenticatedRequest_CascadeApi_Returns401()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            // Do NOT login — call cascade API directly
            var rolesResponse = await page.APIRequest.GetAsync(
                $"{_fixture.BaseUrl}/api/context/roles");

            rolesResponse.Status.Should().NotBe(200,
                "unauthenticated requests to cascade API must not succeed");
        }
        finally
        {
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task CascadeApi_InvalidRole_ReturnsBadRequest()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsync(page, "multirole@test.mentoory.com", "Test123!@#");

            var response = await page.APIRequest.GetAsync(
                $"{_fixture.BaseUrl}/api/context/incubators?role=FakeAdminRole");

            response.Status.Should().Be(400,
                "invalid role parameter must return 400 Bad Request");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(CascadeApi_InvalidRole_ReturnsBadRequest));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task CascadeApi_RoleNotAssignedToUser_ReturnsEmptyList()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            // incadmin1 is IncubatorAdmin only — has no Mentor role
            await LoginAsync(page, "incadmin1@test.mentoory.com", "Test123!@#");

            var response = await page.APIRequest.GetAsync(
                $"{_fixture.BaseUrl}/api/context/incubators?role=Mentor");

            response.Status.Should().Be(200);
            var body = await response.TextAsync();
            body.Should().Be("[]",
                "requesting incubators for a role the user doesn't have must return empty array");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(CascadeApi_RoleNotAssignedToUser_ReturnsEmptyList));
            await page.Context.DisposeAsync();
        }
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------
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
        await LoginAsync(page, email, password);

        if (page.Url.Contains("/Context/Select"))
        {
            // Wait for cascade to load, then select first available options
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var roleDropdown = page.Locator("[data-cs='role']");
            // Select first non-placeholder option
            await roleDropdown.SelectOptionAsync(new SelectOptionValue { Index = 1 });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Wait for incubator to populate (may auto-select)
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var confirmBtn = page.Locator("[data-cs='confirm']");
            await confirmBtn.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
    }
}
