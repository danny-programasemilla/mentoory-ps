using System.Net;
using System.Text.RegularExpressions;
using FluentAssertions;
using Mentoory.Tests.Integration.Fixtures;
using Xunit;

namespace Mentoory.Tests.Integration.Web;

/// <summary>
/// Rendered-HTML assertions for the themed header band (021-themed-header-band).
/// Drives real authenticated routes through the existing <see cref="MentooryWebApplicationFactory"/>
/// (logged in as IncubatorAdmin) and asserts the band's DOM contract:
/// exactly one .page-header, a first-child .page-header-band carrying the resolver's
/// header-band--{slug} class, aria-hidden, and no interactive descendants (FR-008/FR-010).
/// </summary>
[Collection(IntegrationTestCollection.Name)]
[Trait("Category", "Integration")]
public class HeaderBandRenderTests : IntegrationTestBase
{
    public HeaderBandRenderTests(MentooryWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Theory]
    // Routes reachable by the IncubatorAdmin fixture user, one per distinct theme + one unmapped.
    [InlineData("/Administration/Dashboard", "dashboard")]
    [InlineData("/Administration/Users", "personas")]
    [InlineData("/Administration/Projects", "proyectos")]
    [InlineData("/AvailableProjects", "default")] // unmapped controller → default
    public async Task AuthenticatedRoute_RendersSingleHeader_WithExpectedThemeBand(string route, string expectedSlug)
    {
        var client = await CreateAuthenticatedAdminClientAsync();

        var response = await client.GetAsync(route);
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            $"'{route}' should render for an IncubatorAdmin with an active context");

        var html = await response.Content.ReadAsStringAsync();

        // FR-010: exactly one page header. Match the header opening (page-header + space)
        // so .page-header-band / .page-title / .page-body / .page-pretitle never count.
        Regex.Matches(html, "class=\"page-header[ \"]").Count.Should().Be(1,
            "the page must render exactly one .page-header (no duplicate-header regression)");

        // The band is the FIRST child of the single header and carries the resolved theme class.
        // [^>]* tolerates the Razor scoped-CSS attribute (b-xxxx) injected before class=.
        Regex.IsMatch(html, $"<div[^>]*class=\"page-header[^\"]*\">\\s*<div[^>]*class=\"page-header-band header-band--{expectedSlug}\" aria-hidden=\"true\">")
            .Should().BeTrue($"'{route}' should inject header-band--{expectedSlug} as the first child of .page-header");
    }

    [Fact]
    public async Task HeaderBand_IsDecorativeOnly_NoInteractiveDescendants()
    {
        var client = await CreateAuthenticatedAdminClientAsync();

        var response = await client.GetAsync("/Administration/Dashboard");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();

        // Isolate the rendered band element ([^>]* tolerates the scoped-CSS b-xxxx attribute).
        var band = Regex.Match(html, "<div[^>]*class=\"page-header-band [^>]*>.*?</div>", RegexOptions.Singleline);
        band.Success.Should().BeTrue("the band element should be present");

        var bandHtml = band.Value;
        bandHtml.Should().Contain("aria-hidden=\"true\"", "the band must be hidden from assistive tech (FR-008)");

        // FR-008: not focusable / not in the tab order — no interactive descendants, no tabindex.
        bandHtml.Should().NotContain("<a", "the band must contain no links");
        bandHtml.Should().NotContain("<button", "the band must contain no buttons");
        bandHtml.Should().NotContain("tabindex", "the band must not be tabbable");
    }
}
