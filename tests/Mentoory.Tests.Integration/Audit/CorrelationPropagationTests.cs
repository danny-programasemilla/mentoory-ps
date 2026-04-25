using FluentAssertions;
using Mentoory.Tests.Integration.Fixtures;
using Mentoory.Web.Infrastructure.Correlation;
using Xunit;

namespace Mentoory.Tests.Integration.Audit;

/// <summary>
/// Verifies <see cref="CorrelationMiddleware"/> attaches a correlation id to every
/// HTTP request and echoes the incoming <c>X-Correlation-Id</c> header when the client
/// supplies a valid GUID. Two-commands-per-request propagation is covered by the
/// <see cref="AuditPipelineTests"/> design where each command writes a row; asserting
/// a shared id requires an authenticated HTTP endpoint and is deferred pending an
/// authenticated-client fixture.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class CorrelationPropagationTests : IntegrationTestBase
{
    public CorrelationPropagationTests(MentooryWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Response_carries_generated_correlation_id_when_none_supplied()
    {
        using var client = Factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await client.GetAsync("/");

        response.Headers.Should().ContainKey(CorrelationMiddleware.HeaderName);
        var echoed = response.Headers.GetValues(CorrelationMiddleware.HeaderName).Single();
        Guid.TryParse(echoed, out _).Should().BeTrue();
    }

    [Fact]
    public async Task Response_echoes_custom_correlation_id_when_supplied_as_guid()
    {
        var expected = Guid.NewGuid();
        using var client = Factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        client.DefaultRequestHeaders.Add(CorrelationMiddleware.HeaderName, expected.ToString());

        var response = await client.GetAsync("/");

        response.Headers.Should().ContainKey(CorrelationMiddleware.HeaderName);
        var echoed = response.Headers.GetValues(CorrelationMiddleware.HeaderName).Single();
        Guid.Parse(echoed).Should().Be(expected);
    }

    [Fact]
    public async Task Invalid_correlation_id_header_is_replaced_with_fresh_guid()
    {
        using var client = Factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        client.DefaultRequestHeaders.Add(CorrelationMiddleware.HeaderName, "not-a-guid");

        var response = await client.GetAsync("/");

        response.Headers.Should().ContainKey(CorrelationMiddleware.HeaderName);
        var echoed = response.Headers.GetValues(CorrelationMiddleware.HeaderName).Single();
        Guid.TryParse(echoed, out var parsed).Should().BeTrue();
        parsed.Should().NotBe(Guid.Empty);
    }
}
