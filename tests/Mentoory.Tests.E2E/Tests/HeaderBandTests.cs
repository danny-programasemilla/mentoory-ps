using FluentAssertions;
using Mentoory.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace Mentoory.Tests.E2E.Tests;

/// <summary>
/// T012 - Themed header band (021): on a representative authenticated page the decorative
/// band is present, the page title is visible, and the band is decorative-only — hidden
/// from assistive tech and never reachable via the keyboard tab order (FR-008, SC-005).
/// </summary>
[Collection(E2ETestCollection.Name)]
[Trait("Category", "E2E")]
public class HeaderBandTests
{
    private readonly PlaywrightFixture _fixture;

    public HeaderBandTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task HeaderBand_IsPresentDecorativeOnly_AndNotInTabOrder()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginHelper.LoginAsync(page, "incadmin1@test.mentoory.com", "Test123!@#", _fixture.BaseUrl);

            await page.GotoAsync($"{_fixture.BaseUrl}/Administration/Dashboard");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Band present (FR-001).
            var band = page.Locator(".page-header-band");
            (await band.CountAsync()).Should().Be(1, "exactly one decorative band should render in the header");

            // Title still visible over the band (SC-002 legibility precondition).
            var title = page.Locator("h2.page-title");
            (await title.IsVisibleAsync()).Should().BeTrue("the page title must remain visible above the band");

            // Decorative-only: aria-hidden for assistive tech (FR-008).
            (await band.GetAttributeAsync("aria-hidden")).Should().Be("true",
                "the band must be hidden from assistive technology");

            // Not in the keyboard tab order: a plain div with no tabindex cannot take focus.
            var focusState = await page.EvaluateAsync<string>(@"() => {
                const el = document.querySelector('.page-header-band');
                if (!el) return 'missing';
                el.focus();
                return document.activeElement === el ? 'focusable' : 'not-focusable';
            }");
            focusState.Should().Be("not-focusable", "the decorative band must never receive keyboard focus");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(HeaderBand_IsPresentDecorativeOnly_AndNotInTabOrder));
            await page.Context.DisposeAsync();
        }
    }
}
