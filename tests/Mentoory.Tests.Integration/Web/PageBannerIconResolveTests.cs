using FluentAssertions;
using Mentoory.Web.Infrastructure;
using Xunit;

namespace Mentoory.Tests.Integration.Web;

/// <summary>
/// Unit tests for the pure MVC-action → banner-icon resolver (023-page-content-banner).
/// No database or web host required — the resolver is side-effect free, so this class
/// deliberately does NOT join the Integration collection (it must not spin a container).
/// Asserts every mapping row from data-model.md, case-insensitivity, and the default fallback.
/// </summary>
[Trait("Category", "Unit")]
public class PageBannerIconResolveTests
{
    [Theory]
    // Each mapping row from data-model.md (FR-004).
    [InlineData("Index", "list")]
    [InlineData("Create", "plus")]
    [InlineData("Edit", "edit")]
    [InlineData("Details", "eye")]
    [InlineData("Delete", "trash")]
    public void Resolve_MapsKnownAction_ToExpectedSlug(string action, string expectedSlug)
    {
        PageBannerIcon.Resolve(action).Should().Be(expectedSlug);
    }

    [Theory]
    [InlineData("index", "list")]
    [InlineData("CREATE", "plus")]
    [InlineData("eDiT", "edit")]
    [InlineData("DETAILS", "eye")]
    public void Resolve_IsCaseInsensitiveOnAction(string action, string expectedSlug)
    {
        PageBannerIcon.Resolve(action).Should().Be(expectedSlug);
    }

    [Theory]
    // Unmapped, non-CRUD actions fall through to the neutral default (FR-005).
    [InlineData("Export")]
    [InlineData("Download")]
    [InlineData("SomethingCustom")]
    public void Resolve_UnmappedAction_ReturnsDefault(string action)
    {
        PageBannerIcon.Resolve(action).Should().Be("layout-2");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_NullOrBlankAction_ReturnsDefault(string? action)
    {
        PageBannerIcon.Resolve(action).Should().Be("layout-2");
    }

    [Fact]
    public void Resolve_NeverReturnsNullOrEmpty()
    {
        PageBannerIcon.Resolve(null).Should().NotBeNullOrWhiteSpace();
        PageBannerIcon.Resolve("Index").Should().NotBeNullOrWhiteSpace();
        PageBannerIcon.Resolve("Unmapped").Should().NotBeNullOrWhiteSpace();
    }
}
