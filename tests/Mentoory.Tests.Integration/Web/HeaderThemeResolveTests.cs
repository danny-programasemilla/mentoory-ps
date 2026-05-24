using FluentAssertions;
using Mentoory.Web.Infrastructure;
using Xunit;

namespace Mentoory.Tests.Integration.Web;

/// <summary>
/// Unit tests for the pure route → header-band theme resolver (021-themed-header-band).
/// No database or web host required — the resolver is side-effect free, so this class
/// deliberately does NOT join the Integration collection (it must not spin a container).
/// Asserts every mapping row from data-model.md, case-insensitivity, and the default fallback.
/// </summary>
[Trait("Category", "Unit")]
public class HeaderThemeResolveTests
{
    [Theory]
    // Each mapping row from data-model.md / research.md R5.
    [InlineData("Dashboard", "dashboard")]
    [InlineData("Projects", "proyectos")]
    [InlineData("Knowledge", "conocimiento")]
    [InlineData("Templates", "conocimiento")]
    [InlineData("Diagnostics", "diagnostico")]
    [InlineData("Diagnostic", "diagnostico")]
    [InlineData("AnswerCorrection", "diagnostico")]
    [InlineData("Users", "personas")]
    [InlineData("Sponsor", "personas")]
    [InlineData("BatchUpload", "personas")]
    [InlineData("Incubators", "incubadoras")]
    [InlineData("AuditLog", "auditoria")]
    public void Resolve_MapsKnownController_ToExpectedSlug(string controller, string expectedSlug)
    {
        HeaderTheme.Resolve(area: null, controller: controller).Should().Be(expectedSlug);
    }

    [Theory]
    [InlineData("dashboard", "dashboard")]
    [InlineData("PROJECTS", "proyectos")]
    [InlineData("aNsWeRcOrReCtIoN", "diagnostico")]
    [InlineData("batchupload", "personas")]
    public void Resolve_IsCaseInsensitiveOnController(string controller, string expectedSlug)
    {
        HeaderTheme.Resolve(area: null, controller: controller).Should().Be(expectedSlug);
    }

    [Theory]
    [InlineData("Configuration")]
    [InlineData("Home")]
    [InlineData("AvailableProjects")]
    [InlineData("SomethingUnmapped")]
    public void Resolve_UnmappedController_ReturnsDefault(string controller)
    {
        HeaderTheme.Resolve(area: null, controller: controller).Should().Be("default");
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("   ", "   ")]
    [InlineData("Administration", null)]
    public void Resolve_NullOrBlankController_ReturnsDefault(string? area, string? controller)
    {
        HeaderTheme.Resolve(area, controller).Should().Be("default");
    }

    [Fact]
    public void Resolve_NeverReturnsNullOrEmpty()
    {
        HeaderTheme.Resolve(null, null).Should().NotBeNullOrWhiteSpace();
        HeaderTheme.Resolve("X", "Y").Should().NotBeNullOrWhiteSpace();
    }
}
