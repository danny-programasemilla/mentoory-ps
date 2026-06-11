using FluentAssertions;
using Mentoory.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace Mentoory.Tests.E2E.Tests;

/// <summary>
/// E2E (023-page-content-banner): on a representative authenticated page the content banner
/// strip is present and visible, sits above the page content (and below the header band), is
/// ~60px tall, and carries the page title exactly once. Mirrors the 021 HeaderBandTests shape.
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

            // ~60px height band (FR-014). Allow padding/border slack.
            var box = await banner.BoundingBoxAsync();
            box.Should().NotBeNull();
            box!.Height.Should().BeGreaterThanOrEqualTo(56, "the strip should present its ~60px height band");

            // Title appears once, inside the strip (SC-002, FR-002).
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
