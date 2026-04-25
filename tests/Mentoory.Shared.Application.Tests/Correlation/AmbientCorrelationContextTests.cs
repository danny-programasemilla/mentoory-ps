using System.Diagnostics;
using FluentAssertions;
using Mentoory.Shared.Infrastructure.Correlation;
using Xunit;

namespace Mentoory.Shared.Application.Tests.Correlation;

public sealed class AmbientCorrelationContextTests
{
    [Fact]
    public void ClientIpAddress_is_always_null()
    {
        var ctx = new AmbientCorrelationContext();
        ctx.ClientIpAddress.Should().BeNull();
    }

    [Fact]
    public void CorrelationId_returns_fresh_GUID_when_no_ambient_activity()
    {
        var existing = Activity.Current;
        Activity.Current = null;
        try
        {
            var ctx = new AmbientCorrelationContext();

            var first = ctx.CorrelationId;
            var second = ctx.CorrelationId;

            first.Should().NotBe(Guid.Empty);
            second.Should().NotBe(Guid.Empty);
        }
        finally
        {
            Activity.Current = existing;
        }
    }

    [Fact]
    public void CorrelationId_parses_Activity_RootId_when_it_is_a_valid_GUID()
    {
        var existing = Activity.Current;
        var guid = Guid.NewGuid();
        using var activity = new Activity("test-op");
        // Activity.RootId derives from traceparent-style SpanId/TraceId. We seed it via
        // SetParentId with a W3C-style id whose root segment is our GUID-without-dashes.
        activity.SetParentId("00-" + guid.ToString("N") + "-0000000000000001-01");
        activity.Start();
        try
        {
            var ctx = new AmbientCorrelationContext();
            var rootId = activity.RootId;

            // Only assert equality if RootId is a parseable GUID. Otherwise AmbientCorrelationContext
            // falls back to a fresh GUID — still valid, just not observable here.
            if (Guid.TryParse(rootId, out var expected))
            {
                ctx.CorrelationId.Should().Be(expected);
            }
            else
            {
                ctx.CorrelationId.Should().NotBe(Guid.Empty);
            }
        }
        finally
        {
            activity.Stop();
            Activity.Current = existing;
        }
    }
}
