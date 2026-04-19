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
[Trait("Category", "E2E")]
public class ContextSelectionTests
{
    private readonly PlaywrightFixture _fixture;

    public ContextSelectionTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task MultiRoleUser_ShouldSee_CascadingDropdowns()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsync(page, "multirole@test.mentoory.com", "Test123!@#");

            page.Url.Should().Contain("/Context/Select",
                "multi-role users must land on the context selection page");

            var heading = page.Locator("h3.card-title").Filter(new LocatorFilterOptions
            {
                HasText = "Seleccionar Contexto de Trabajo"
            });
            (await heading.CountAsync()).Should().Be(1);

            // Scope to the page-mode container (not the modal)
            var container = page.Locator("[data-mode='page']");
            (await container.CountAsync()).Should().BeGreaterThan(0, "page-mode container must exist");

            var roleDropdown = container.Locator("[data-cs='role']");
            var incubatorDropdown = container.Locator("[data-cs='incubator']");
            var projectDropdown = container.Locator("[data-cs='project']");

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

            var container = page.Locator("[data-mode='page']");
            var roleDropdown = container.Locator("[data-cs='role']");
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

            var container = page.Locator("[data-mode='page']");
            var roleDropdown = container.Locator("[data-cs='role']");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await roleDropdown.SelectOptionAsync(new SelectOptionValue { Label = "Administrador de Incubadora" });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var incubatorDropdown = container.Locator("[data-cs='incubator']");
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
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var container = page.Locator("[data-mode='page']");
            var roleDropdown = container.Locator("[data-cs='role']");
            await roleDropdown.SelectOptionAsync(new SelectOptionValue { Label = "Administrador de Incubadora" });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Multirole user has IncubatorAdmin at 2 incubators → must manually select one
            var incubatorDropdown = container.Locator("[data-cs='incubator']");
            await incubatorDropdown.SelectOptionAsync(new SelectOptionValue { Label = "Incubadora Alpha" });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var confirmBtn = container.Locator("[data-cs='confirm']");
            await Assertions.Expect(confirmBtn).ToBeEnabledAsync(new() { Timeout = 10000 });

            await confirmBtn.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

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
            var targetPath = "/Coordination/Diagnostics";
            await page.GotoAsync($"{_fixture.BaseUrl}{targetPath}");

            page.Url.Should().Contain("/Access/Login");

            // coord1 has single role → auto-skip
            await page.FillAsync("input[name='Email']", "coord1@test.mentoory.com");
            await page.FillAsync("input[name='Password']", "Test123!@#");
            await page.ClickAsync("button[type='submit']");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

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
    // Tenant Isolation — uses page.EvaluateAsync(fetch) to share session cookies
    // -----------------------------------------------------------------------
    [Fact]
    public async Task NonGlobalAdmin_CascadeApi_ReturnsOnlyAssignedIncubators()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            // incadmin1 is IncubatorAdmin for Alpha ONLY (single-role, auto-skips selection)
            await LoginAsync(page, "incadmin1@test.mentoory.com", "Test123!@#");

            var body = await FetchJsonAsync(page, "/api/context/incubators?role=IncubatorAdmin");

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
            // coord1 is ProjectCoordinator for Alpha/Innovación ONLY (single-role, auto-skips)
            await LoginAsync(page, "coord1@test.mentoory.com", "Test123!@#");

            var incBody = await FetchJsonAsync(page, "/api/context/incubators?role=ProjectCoordinator");
            incBody.Should().Contain("Incubadora Alpha");

            var incJson = System.Text.Json.JsonDocument.Parse(incBody);
            var alphaId = incJson.RootElement.EnumerateArray()
                .First(e => e.GetProperty("name").GetString() == "Incubadora Alpha")
                .GetProperty("id").GetInt64();

            var projBody = await FetchJsonAsync(page, $"/api/context/projects?role=ProjectCoordinator&incubatorId={alphaId}");

            // coord1 is assigned to Innovación ONLY under Alpha
            projBody.Should().Contain("Innovación");
            projBody.Should().NotContain("Sostenibilidad",
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

            var body = await FetchJsonAsync(page, "/api/context/incubators?role=GlobalAdmin");

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
    public async Task UnauthenticatedRequest_CascadeApi_RedirectsToLogin()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            // Navigate directly to the API endpoint — cookie auth will redirect to login
            await page.GotoAsync($"{_fixture.BaseUrl}/api/context/roles");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            page.Url.Should().Contain("/Access/Login",
                "unauthenticated requests to cascade API must redirect to login");
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

            var status = await page.EvaluateAsync<int>(
                "fetch('/api/context/incubators?role=FakeAdminRole').then(r => r.status)");

            status.Should().Be(400,
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

            var body = await FetchJsonAsync(page, "/api/context/incubators?role=Mentor");

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
    private static async Task<string> FetchJsonAsync(IPage page, string path)
    {
        return await page.EvaluateAsync<string>(
            "async (path) => { const r = await fetch(path); return await r.text(); }",
            path);
    }

    private static async Task SelectFirstContextAsync(IPage page)
    {
        var container = page.Locator("[data-mode='page']");
        var roleDropdown = container.Locator("[data-cs='role']");
        var incubatorDropdown = container.Locator("[data-cs='incubator']");
        var confirmBtn = container.Locator("[data-cs='confirm']");

        // Wait for roles to load
        await page.WaitForFunctionAsync(
            "sel => sel.options.length > 1",
            await roleDropdown.ElementHandleAsync(),
            new() { Timeout = 10000 });

        if (await roleDropdown.IsEnabledAsync())
        {
            await roleDropdown.SelectOptionAsync(new SelectOptionValue { Index = 1 });
        }

        // Wait for incubators to load
        await page.WaitForFunctionAsync(
            "sel => sel.options.length > 1",
            await incubatorDropdown.ElementHandleAsync(),
            new() { Timeout = 10000 });

        if (await incubatorDropdown.IsEnabledAsync())
        {
            await incubatorDropdown.SelectOptionAsync(new SelectOptionValue { Index = 1 });
        }

        await Assertions.Expect(confirmBtn).ToBeEnabledAsync(new() { Timeout = 15000 });
        await confirmBtn.ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
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
        await LoginAsync(page, email, password);

        if (page.Url.Contains("/Context/Select"))
        {
            await SelectFirstContextAsync(page);
        }
    }
}
