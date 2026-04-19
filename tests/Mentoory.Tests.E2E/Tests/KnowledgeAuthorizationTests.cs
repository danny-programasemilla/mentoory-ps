using System.Globalization;
using FluentAssertions;
using Mentoory.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace Mentoory.Tests.E2E.Tests;

/// <summary>
/// E2E coverage for spec 017 US6 — authorization, tenant isolation, and menu visibility on
/// the Knowledge module surface. This is the negative-path contract: each test drives an
/// unauthorized actor against a protected route or a cross-tenant target and asserts the
/// boundary holds. Positive-path coverage lives in the sibling KnowledgeTemplatesTests and
/// KnowledgeProjectStructureTests.
///
/// | Spec 017 scenario | Method |
/// |---|---|
/// | US6-1 | ProtectedRoutes_CoordinatorDenied_ForTemplateRoutes (theory) |
/// | US6-2 | ProtectedRoutes_Unauthenticated_RedirectToLogin (theory) |
/// | US6-3 | TenantIsolation_CoordinatorB_CannotAccessCoordinatorAsKs |
/// | US6-4 | CreateProject_CrossIncubatorForm_Denied |
/// | US6-5 | Menu_GlobalAdmin_ShowsBothEntries_Coordinator_ShowsOnlyProjects |
/// </summary>
[Collection(E2ETestCollection.Name)]
[Trait("Category", "E2E")]
public class KnowledgeAuthorizationTests
{
    // coord1's seeded project KS (Proyecto Innovación @ Incubadora Alpha). Used by the
    // unauthenticated theory and the tenant-isolation fact; referenced as a literal string in
    // [InlineData] rows since attribute args must be compile-time constants.
    private const string Coord1ProjectKsExternalIdLiteral = "99999999-9999-9999-9999-999999999901";
    private static readonly Guid Coord1ProjectKsExternalId = new(Coord1ProjectKsExternalIdLiteral);

    // Seeded module/topic names under coord1's project KS (004.SeedTestData.sql § "Proyecto
    // Innovación"). The tenant-isolation assertion scans for these on coordnorte's rendered
    // page body — any leak means the HasQueryFilter on KnowledgeStructure failed.
    private static readonly string[] Coord1KsContentMarkers = new[]
    {
        "Modelo de Negocio",
        "Equipo",
        "Finanzas",
    };

    private readonly PlaywrightFixture _fixture;

    public KnowledgeAuthorizationTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [InlineData("/Coordination/Knowledge/Templates")]
    [InlineData("/Coordination/Knowledge/Templates/Create")]
    [InlineData("/Coordination/Knowledge/Templates/{0}")]
    public async Task ProtectedRoutes_CoordinatorDenied_ForTemplateRoutes(string routeTemplate)
    {
        var seededKsExtId = await KnowledgeIntegrationHelpers.GetSeededKsTemplateExternalIdAsync(_fixture);
        var route = string.Format(CultureInfo.InvariantCulture, routeTemplate, seededKsExtId);

        var page = await _fixture.CreatePageAsync();
        try
        {
            await KnowledgeTestHelpers.LoginAndSelectAsync(
                page, _fixture.BaseUrl,
                "coord1@test.mentoory.com", "Test123!@#",
                ContextSelection.FirstEnabled);

            var response = await page.GotoAsync($"{_fixture.BaseUrl}{route}");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            AssertDenied(page, response?.Status, route, "ProjectCoordinator");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(
                page,
                $"{nameof(ProtectedRoutes_CoordinatorDenied_ForTemplateRoutes)}_{SanitizeForFilename(route)}");
            await page.Context.DisposeAsync();
        }
    }

    [Theory]
    [InlineData("/Coordination/Knowledge/Templates")]
    [InlineData("/Coordination/Knowledge/Templates/Create")]
    [InlineData("/Coordination/Knowledge/Templates/11111111-1111-1111-1111-111111111111")]
    [InlineData("/Coordination/Knowledge/Projects")]
    [InlineData("/Coordination/Knowledge/Projects/" + Coord1ProjectKsExternalIdLiteral)]
    public async Task ProtectedRoutes_Unauthenticated_RedirectToLogin(string route)
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await page.GotoAsync($"{_fixture.BaseUrl}{route}");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            page.Url.Should().Contain("/Access/Login",
                $"anonymous GET {route} must land on /Access/Login per US6-2 (got {page.Url})");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(
                page,
                $"{nameof(ProtectedRoutes_Unauthenticated_RedirectToLogin)}_{SanitizeForFilename(route)}");
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task TenantIsolation_CoordinatorB_CannotAccessCoordinatorAsKs()
    {
        var route = $"/Coordination/Knowledge/Projects/{Coord1ProjectKsExternalId}";

        var page = await _fixture.CreatePageAsync();
        try
        {
            // coordnorte lives only in Incubadora Norte — the Phase 6 incubator-scoped
            // HasQueryFilter on KnowledgeStructure must mask coord1's KS row from the DbContext
            // scan this request runs. 404-class exit is the happy path; a 200 with no leaked
            // Spanish names is the acceptable alternative per the spec's OR-worded assertion.
            await KnowledgeTestHelpers.LoginAndSelectAsync(
                page, _fixture.BaseUrl,
                "coordnorte@test.mentoory.com", "Test123!@#",
                ContextSelection.FirstEnabled);

            var response = await page.GotoAsync($"{_fixture.BaseUrl}{route}");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var status = response?.Status ?? 200;
            var body = await page.ContentAsync();
            var leakedMarker = Coord1KsContentMarkers.FirstOrDefault(
                marker => body.Contains(marker, StringComparison.Ordinal));

            (status >= 400 || leakedMarker is null).Should().BeTrue(
                $"coordnorte must not see coord1's KS at {route}: got HTTP {status}, leaked='{leakedMarker}'");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(
                page, nameof(TenantIsolation_CoordinatorB_CannotAccessCoordinatorAsKs));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task CreateProject_CrossIncubatorForm_Denied()
    {
        // ProjectsController.Create (POST) derives IncubatorId from User.GetActiveIncubatorId()
        // and CreateProjectViewModel has no IncubatorId field — so a body-level "form hack" is
        // ignored by the model binder. This test encodes that invariant: if someone ever adds
        // IncubatorId to the view model or binds it from the body, the injected Norte id would
        // land in the handler and the post-check below would fail.
        var uniqueProjectName = $"E2E-AuthHack-{Guid.NewGuid():N}"[..28];

        var alphaTask = KnowledgeIntegrationHelpers.GetIncubatorByNameAsync(_fixture, "Incubadora Alpha");
        var norteTask = KnowledgeIntegrationHelpers.GetIncubatorByNameAsync(_fixture, "Incubadora Norte");
        var seededKsTask = KnowledgeIntegrationHelpers.GetSeededKsTemplateExternalIdAsync(_fixture);
        await Task.WhenAll(alphaTask, norteTask, seededKsTask);

        var (alphaIncubatorId, _) = await alphaTask;
        var (norteIncubatorId, norteIncubatorExternalId) = await norteTask;
        var seededKsTemplateExternalId = await seededKsTask;

        var page = await _fixture.CreatePageAsync();
        try
        {
            await KnowledgeTestHelpers.LoginAndSelectAsync(
                page, _fixture.BaseUrl,
                "incadmin1@test.mentoory.com", "Test123!@#",
                ContextSelection.FirstEnabled);

            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/Projects/Create");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await page.FillAsync("input[name='Name']", uniqueProjectName);
            await page.Locator("select[name='KnowledgeStructureTemplateExternalId']")
                .SelectOptionAsync(new SelectOptionValue { Value = seededKsTemplateExternalId.ToString() });

            // Inject a hidden IncubatorId input targeting Norte. If the controller trusted
            // request-body IncubatorId, this would redirect creation into an incubator the
            // actor does not administer.
            await page.EvaluateAsync(
                "value => { const i = document.createElement('input'); i.type='hidden'; i.name='IncubatorId'; i.value=value; document.querySelector('form').appendChild(i); }",
                norteIncubatorExternalId.ToString());

            await page.Locator("form button[type='submit'].btn-primary").First.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var project = await KnowledgeIntegrationHelpers.GetProjectByNameAsync(_fixture, uniqueProjectName);

            project.IncubatorId.Should().Be(alphaIncubatorId,
                "IncubatorId must be derived from session (incadmin1's Alpha context) — the form-hack IncubatorId field must be ignored");
            project.IncubatorId.Should().NotBe(norteIncubatorId,
                "cross-incubator form-hack must never land a project under an incubator the actor does not administer");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(CreateProject_CrossIncubatorForm_Denied));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task Menu_GlobalAdmin_ShowsBothEntries_Coordinator_ShowsOnlyProjects()
    {
        var globalAdminPage = await _fixture.CreatePageAsync();
        try
        {
            // multirole has both GlobalAdmin and ProjectCoordinator assignments — explicit
            // RoleLabel = "GlobalAdmin" pins the context-select dropdown so MenuService.
            // GetActiveRole() resolves to GlobalAdmin and surfaces both knowledge entries.
            await KnowledgeTestHelpers.LoginAndSelectAsync(
                globalAdminPage, _fixture.BaseUrl,
                "multirole@test.mentoory.com", "Test123!@#",
                ContextSelection.GlobalAdmin);

            var gaSidebar = globalAdminPage.Locator("aside#sidebar");
            (await FindSidebarEntryAsync(gaSidebar, "Plantillas de conocimiento"))
                .Should().BeGreaterThan(0,
                    "GlobalAdmin must see the 'Plantillas de conocimiento' sidebar entry");
            (await FindSidebarEntryAsync(gaSidebar, "Estructuras del proyecto"))
                .Should().BeGreaterThan(0,
                    "GlobalAdmin must see the 'Estructuras del proyecto' sidebar entry");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(
                globalAdminPage,
                $"{nameof(Menu_GlobalAdmin_ShowsBothEntries_Coordinator_ShowsOnlyProjects)}_GlobalAdmin");
            await globalAdminPage.Context.DisposeAsync();
        }

        var coordPage = await _fixture.CreatePageAsync();
        try
        {
            await KnowledgeTestHelpers.LoginAndSelectAsync(
                coordPage, _fixture.BaseUrl,
                "coord1@test.mentoory.com", "Test123!@#",
                ContextSelection.FirstEnabled);

            var coordSidebar = coordPage.Locator("aside#sidebar");
            (await FindSidebarEntryAsync(coordSidebar, "Plantillas de conocimiento"))
                .Should().Be(0,
                    "ProjectCoordinator must NOT see the GlobalAdmin-only 'Plantillas de conocimiento' entry");
            (await FindSidebarEntryAsync(coordSidebar, "Estructuras del proyecto"))
                .Should().BeGreaterThan(0,
                    "ProjectCoordinator must see the 'Estructuras del proyecto' entry");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(
                coordPage,
                $"{nameof(Menu_GlobalAdmin_ShowsBothEntries_Coordinator_ShowsOnlyProjects)}_Coord1");
            await coordPage.Context.DisposeAsync();
        }
    }

    private static Task<int> FindSidebarEntryAsync(ILocator sidebar, string spanishLabel) =>
        sidebar.Locator("a.dropdown-item")
            .Filter(new LocatorFilterOptions { HasText = spanishLabel })
            .CountAsync();

    private static void AssertDenied(IPage page, int? status, string route, string role)
    {
        var effectiveStatus = status ?? 200;
        var stillOnProtectedRoute =
            page.Url.Contains(route, StringComparison.OrdinalIgnoreCase)
            && !page.Url.Contains("/Access/Login", StringComparison.OrdinalIgnoreCase);

        if (stillOnProtectedRoute)
        {
            (effectiveStatus >= 400).Should().BeTrue(
                $"{role} must be denied on GET {route} (got HTTP {effectiveStatus})");
        }
        else
        {
            page.Url.Should().NotContain(route,
                $"{role} should be redirected away from GET {route}");
        }
    }

    private static string SanitizeForFilename(string route)
    {
        var buffer = new char[route.Length];
        for (var i = 0; i < route.Length; i++)
        {
            var c = route[i];
            buffer[i] = char.IsLetterOrDigit(c) || c == '-' ? c : '_';
        }

        return new string(buffer);
    }
}
