using System.Net;
using System.Text.RegularExpressions;
using FluentAssertions;
using Mentoory.Tests.Integration.Fixtures;
using Xunit;

namespace Mentoory.Tests.Integration.Web;

/// <summary>
/// Rendered-HTML assertions for the page content banner strip (023-page-content-banner).
/// Drives real authenticated routes through the existing <see cref="MentooryWebApplicationFactory"/>
/// (logged in as IncubatorAdmin) and asserts the strip's DOM contract (contracts/page-banner.md):
/// exactly one .page-banner inside .page-body carrying page-banner--{slug}, one decorative
/// action icon (aria-hidden, non-interactive), the title shown once and no longer in the
/// header band, the header band's breadcrumb/topbar preserved, and the strip excluded from the
/// unauthenticated login page.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
[Trait("Category", "Integration")]
public class PageBannerRenderTests : IntegrationTestBase
{
    public PageBannerRenderTests(MentooryWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Theory]
    // One route per distinct section + one unmapped → default. Each is an Index action → list.
    [InlineData("/Administration/Dashboard", "dashboard", "list")]
    [InlineData("/Administration/Users", "personas", "list")]
    [InlineData("/Administration/Projects", "proyectos", "list")]
    [InlineData("/AvailableProjects", "default", "list")] // unmapped controller → default slug
    public async Task AuthenticatedRoute_RendersSingleBanner_WithSectionAndActionIcon(
        string route, string expectedSlug, string expectedIcon)
    {
        var client = await CreateAuthenticatedAdminClientAsync();

        var response = await client.GetAsync(route);
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            $"'{route}' should render for an IncubatorAdmin with an active context");

        var html = await response.Content.ReadAsStringAsync();

        // C-01 (presence): exactly one .page-banner. Match the opening (page-banner + space)
        // so .page-banner--{slug} / .page-banner__title / .page-banner__icon never count.
        Regex.Matches(html, "class=\"page-banner[ \"]").Count.Should().Be(1,
            "exactly one .page-banner strip should render on an authenticated page");

        // C-01 (placement): the strip is inside .page-body, not .page-header.
        var body = Regex.Match(html, "<div[^>]*class=\"page-body\".*?</body>", RegexOptions.Singleline);
        body.Success.Should().BeTrue("the .page-body container should be present");
        body.Value.Should().Contain("class=\"page-banner",
            "the strip must render inside .page-body");

        // C-02 (section class): the strip carries the resolved section slug.
        Regex.IsMatch(html, $"<div[^>]*class=\"page-banner page-banner--{expectedSlug}\"")
            .Should().BeTrue($"'{route}' should render page-banner--{expectedSlug}");

        // C-03 (action icon): exactly one decorative icon with the action's slug, aria-hidden.
        Regex.Matches(html, "class=\"ti ti-[a-z0-9-]+ page-banner__icon\"").Count.Should().Be(1,
            "the strip should render exactly one action icon");
        Regex.IsMatch(html, $"<i class=\"ti ti-{expectedIcon} page-banner__icon\" aria-hidden=\"true\">")
            .Should().BeTrue($"'{route}' (Index) should render the ti-{expectedIcon} icon, aria-hidden");
    }

    [Fact]
    public async Task Banner_Icon_IsDecorativeOnly_NoInteractiveContent()
    {
        var client = await CreateAuthenticatedAdminClientAsync();

        var response = await client.GetAsync("/Administration/Dashboard");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();

        // C-07: isolate the strip and confirm the icon carries no link/button/tabindex/text.
        var banner = Regex.Match(html, "<div[^>]*class=\"page-banner [^>]*>.*?</div>\\s*</div>", RegexOptions.Singleline);
        banner.Success.Should().BeTrue("the strip element should be present");

        var iconTag = Regex.Match(html, "<i class=\"ti ti-[a-z0-9-]+ page-banner__icon\"[^>]*></i>");
        iconTag.Success.Should().BeTrue("the icon should be a self-closed <i> with no inner content");
        iconTag.Value.Should().Contain("aria-hidden=\"true\"", "the icon must be hidden from assistive tech (FR-011)");
        iconTag.Value.Should().NotContain("tabindex", "the icon must not be tabbable");
        iconTag.Value.Should().NotContain("href", "the icon must not be a link");
    }

    [Fact]
    public async Task Banner_HoldsTitleOnce_AndHeaderBandNoLongerShowsIt()
    {
        var client = await CreateAuthenticatedAdminClientAsync();

        var response = await client.GetAsync("/Administration/Dashboard");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();

        // C-04 (single title): the page title lives in the strip as h2.page-title.page-banner__title.
        Regex.Matches(html, "class=\"page-title page-banner__title\"").Count.Should().Be(1,
            "the title should appear once, inside the strip");

        // C-04: the header band no longer contains a bare h2.page-title (the title moved out).
        // The only h2.page-title in the document is the strip's combined-class one.
        Regex.IsMatch(html, "<h2 class=\"page-title\">")
            .Should().BeFalse("the header band must no longer render a standalone h2.page-title (title relocated)");

        // C-05 (header band preserved): breadcrumb + topbar still render in the header band.
        html.Should().Contain("class=\"page-header", "the header band must still render");
        html.Should().Contain("class=\"breadcrumb\"", "the breadcrumb must remain in the header band (FR-009)");
        html.Should().Contain("class=\"page-pretitle\"", "the page-pretitle (breadcrumb wrapper) must remain");
        html.Should().Contain("aria-label=\"Menu de usuario\"",
            "the _TopBar user menu must remain in the header band after the title moves out (FR-009/C-05)");
    }

    [Theory]
    // Non-Index actions reachable by the IncubatorAdmin fixture, proving the icon VARIES by action
    // (the section Theory above is all Index→list). Crucially, /Users/Enroll has an UNMAPPED action
    // ("Enroll"), so it exercises the default-icon DOM path (FR-005 → ti-layout-2), non-vacuously.
    [InlineData("/Administration/Users/Enroll", "personas", "layout-2")] // unmapped action → default icon
    [InlineData("/Administration/Projects/Create", "proyectos", "plus")] // Create → plus
    public async Task NonIndexAction_RendersExpectedActionIcon_OverSectionColour(
        string route, string expectedSlug, string expectedIcon)
    {
        var client = await CreateAuthenticatedAdminClientAsync();

        var response = await client.GetAsync(route);
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            $"'{route}' should render for an IncubatorAdmin with an active context");

        var html = await response.Content.ReadAsStringAsync();

        // Exactly one strip, carrying the section slug.
        Regex.Matches(html, "class=\"page-banner[ \"]").Count.Should().Be(1,
            "exactly one .page-banner strip should render");
        Regex.IsMatch(html, $"<div[^>]*class=\"page-banner page-banner--{expectedSlug}\"")
            .Should().BeTrue($"'{route}' should render page-banner--{expectedSlug}");

        // FR-004/FR-005: the action's icon (mapped → plus; unmapped → the layout-2 default).
        Regex.IsMatch(html, $"<i class=\"ti ti-{expectedIcon} page-banner__icon\" aria-hidden=\"true\">")
            .Should().BeTrue($"'{route}' should render the ti-{expectedIcon} action icon, aria-hidden");
    }

    [Fact]
    public async Task LoginPage_RendersNoBanner()
    {
        // C-06 (exclusion): the unauthenticated login page uses the else-branch / no main layout
        // chrome, so it must render no .page-banner element (SC-004).
        var client = Factory.CreateClient();

        var response = await client.GetAsync("/Access/Login");
        response.StatusCode.Should().Be(HttpStatusCode.OK, "the login page should render");

        var html = await response.Content.ReadAsStringAsync();
        html.Should().NotContain("class=\"page-banner",
            "the login page must not render the banner strip (FR-013)");
    }
}
