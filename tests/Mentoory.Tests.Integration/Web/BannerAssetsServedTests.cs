using System.Net;
using System.Xml.Linq;
using FluentAssertions;
using Mentoory.Tests.Integration.Fixtures;
using Xunit;

namespace Mentoory.Tests.Integration.Web;

/// <summary>
/// Static-asset assertions for the curated page-banner SVG set (024-page-banner-redesign).
/// Every slug a <c>HeaderTheme</c> can return binds to a <c>/img/banners/{slug}.svg</c> via the
/// <c>--banner-art</c> custom property, so a typo'd or missing file would ship a broken band.
/// This guards FR-021 / SC-011: each of the 8 curated assets is served (200) and is a well-formed
/// SVG. Pure static-file HTTP through the existing <see cref="MentooryWebApplicationFactory"/>
/// (collection fixture, no DB reset needed — static files are served before auth).
/// </summary>
[Collection(IntegrationTestCollection.Name)]
[Trait("Category", "Integration")]
public class BannerAssetsServedTests
{
    private readonly MentooryWebApplicationFactory _factory;

    public BannerAssetsServedTests(MentooryWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("dashboard")]
    [InlineData("proyectos")]
    [InlineData("conocimiento")]
    [InlineData("diagnostico")]
    [InlineData("personas")]
    [InlineData("incubadoras")]
    [InlineData("auditoria")]
    [InlineData("default")]
    public async Task BannerAsset_IsServed_AsWellFormedSvg(string slug)
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/img/banners/{slug}.svg");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            $"the curated banner asset for '{slug}' must be served (FR-021/SC-011)");

        var body = await response.Content.ReadAsStringAsync();
        var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;

        var looksLikeSvg = contentType.Contains("svg", StringComparison.OrdinalIgnoreCase)
            || body.TrimStart().StartsWith("<svg", StringComparison.OrdinalIgnoreCase);
        looksLikeSvg.Should().BeTrue(
            $"'{slug}.svg' should be served as SVG (content-type '{contentType}' or an <svg> root)");

        body.Should().Contain("<svg", $"'{slug}.svg' must contain an <svg> root");

        // Parse the document to back the well-formedness claim: a substring check alone would pass
        // a file with unbalanced tags or malformed geometry. The root element must be <svg>.
        var parse = () => XDocument.Parse(body);
        parse.Should().NotThrow($"'{slug}.svg' must be well-formed XML");
        XDocument.Parse(body).Root!.Name.LocalName.Should().Be("svg",
            $"'{slug}.svg' must have an <svg> root element");
    }
}
