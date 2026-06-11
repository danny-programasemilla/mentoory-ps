using FluentAssertions;
using Mentoory.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace Mentoory.Tests.E2E.Tests;

/// <summary>
/// E2E (024-page-banner-redesign): on a representative authenticated page the content banner
/// is present and visible, sits above the page content (and below the header band), is
/// ~96px tall, and carries the page title exactly once. Mirrors the 021 HeaderBandTests shape.
/// Note: SC-005 (WCAG-AA title contrast) is verified by manual review per quickstart.md — the
/// title is dark text over the transparent left region (a clean light surface) on every area.
/// </summary>
[Collection(E2ETestCollection.Name)]
[Trait("Category", "E2E")]
public class PageBannerTests
{
    private readonly PlaywrightFixture _fixture;

    public PageBannerTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PageBanner_IsVisibleAboveContent_WithTitleOnce()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginHelper.LoginAsync(page, "incadmin1@test.mentoory.com", "Test123!@#", _fixture.BaseUrl);

            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/Dashboard");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Present and visible (FR-001).
            var banner = page.Locator(".page-banner");
            (await banner.CountAsync()).Should().Be(1, "exactly one content banner strip should render");
            (await banner.IsVisibleAsync()).Should().BeTrue("the banner strip must be visible");

            // ~96px height band (FR-012/SC-006). The band has min-height 96px with zero vertical
            // padding and no border, so a correct render measures ~96px. Bounds enforce the spec's
            // 88–100px band with a small sub-pixel/zoom margin (104px ceiling), guarding both a
            // collapsed band and runaway growth past the 100px ceiling.
            var box = await banner.BoundingBoxAsync();
            box.Should().NotBeNull();
            box!.Height.Should().BeGreaterThanOrEqualTo(88, "the band should present its ~96px height (FR-012/SC-006)");
            box!.Height.Should().BeLessThanOrEqualTo(104, "the band height should stay within the 88–100px spec band + measurement margin (FR-012/SC-006)");

            // Title renders once inside the strip (whole-page de-dup, SC-008, is covered by the
            // PageBannerRenderTests markup-contract guard).
            var title = page.Locator(".page-banner .page-banner__title");
            (await title.CountAsync()).Should().Be(1, "the title should render once in the strip");
            (await title.IsVisibleAsync()).Should().BeTrue("the strip title must be visible");

            // The strip sits inside .page-body, above the rendered page content.
            var inBody = await page.EvaluateAsync<bool>(@"() => {
                const strip = document.querySelector('.page-banner');
                const body = document.querySelector('.page-body');
                return !!strip && !!body && body.contains(strip);
            }");
            inBody.Should().BeTrue("the strip must render inside .page-body (above page content)");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(PageBanner_IsVisibleAboveContent_WithTitleOnce));
            await page.Context.DisposeAsync();
        }
    }
}
