using FluentAssertions;
using Mentoory.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace Mentoory.Tests.E2E.Tests;

/// <summary>
/// Phase 3 (US3) — correlation middleware is wired through the production pipeline and
/// surfaced at the HTTP boundary: every GET response carries a valid-GUID
/// <c>x-correlation-id</c>; a client-supplied valid-GUID header is echoed verbatim; two
/// requests without the header receive distinct ids.
/// </summary>
[Collection(E2ETestCollection.Name)]
public class AuditLogCorrelationTests : E2ETestBase
{
    // CorrelationMiddleware.HeaderName is "X-Correlation-Id" (title case) when set on the
    // request, but Playwright lowercases all response header keys. Both spellings appear
    // intentionally below: title-case for outbound (ExtraHTTPHeaders) and lowercase for
    // inbound (IResponse.Headers lookup).
    private const string OutboundHeaderName = "X-Correlation-Id";
    private const string InboundHeaderName = "x-correlation-id";

    public AuditLogCorrelationTests(PlaywrightFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task AnyGetResponse_CarriesValidGuidCorrelationId()
    {
        var page = await Fixture.CreatePageAsync();
        try
        {
            var response = await page.GotoAsync($"{Fixture.BaseUrl}/");

            response.Should().NotBeNull("a navigation to the root must produce an HTTP response");
            var headerValue = ReadCorrelationHeader(response!);

            headerValue.Should().NotBeNullOrWhiteSpace(
                "the CorrelationMiddleware must stamp every response with the x-correlation-id header");
            Guid.TryParse(headerValue, out var parsed).Should().BeTrue(
                $"the x-correlation-id header value must be a valid GUID (got: '{headerValue}')");
            parsed.Should().NotBe(Guid.Empty);
        }
        finally
        {
            await Fixture.TakeScreenshotOnFailureAsync(page, nameof(AnyGetResponse_CarriesValidGuidCorrelationId));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task ClientProvidedCorrelationId_IsEchoedVerbatim()
    {
        var knownId = Guid.Parse("11111111-2222-3333-4444-555555555555").ToString();

        var context = await Fixture.Browser!.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true,
            ScreenSize = new ScreenSize { Width = 1280, Height = 720 },
            ExtraHTTPHeaders = new Dictionary<string, string>
            {
                [OutboundHeaderName] = knownId,
            },
        });
        try
        {
            var page = await context.NewPageAsync();
            var response = await page.GotoAsync($"{Fixture.BaseUrl}/");

            response.Should().NotBeNull();
            var echoed = ReadCorrelationHeader(response!);

            echoed.Should().Be(knownId,
                "a valid-GUID X-Correlation-Id supplied by the client must be echoed back unchanged");
        }
        finally
        {
            await context.DisposeAsync();
        }
    }

    [Fact]
    public async Task TwoRequestsWithoutHeader_GetDistinctCorrelationIds()
    {
        var firstId = await CaptureCorrelationIdFromFreshPageAsync();
        var secondId = await CaptureCorrelationIdFromFreshPageAsync();

        Guid.TryParse(firstId, out var firstGuid).Should().BeTrue(
            $"first request's correlation id must parse as a GUID (got: '{firstId}')");
        Guid.TryParse(secondId, out var secondGuid).Should().BeTrue(
            $"second request's correlation id must parse as a GUID (got: '{secondId}')");

        firstGuid.Should().NotBe(Guid.Empty);
        secondGuid.Should().NotBe(Guid.Empty);
        firstGuid.Should().NotBe(secondGuid,
            "two unheadered requests must each receive a freshly generated, distinct correlation id");
    }

    private static string? ReadCorrelationHeader(IResponse response)
    {
        return response.Headers.TryGetValue(InboundHeaderName, out var value) ? value : null;
    }

    private async Task<string?> CaptureCorrelationIdFromFreshPageAsync()
    {
        var page = await Fixture.CreatePageAsync();
        try
        {
            var response = await page.GotoAsync($"{Fixture.BaseUrl}/");
            response.Should().NotBeNull();
            return ReadCorrelationHeader(response!);
        }
        finally
        {
            await page.Context.DisposeAsync();
        }
    }
}
