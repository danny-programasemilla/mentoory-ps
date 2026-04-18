using FluentAssertions;
using Mentoory.Shared.Application.Audit;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Mentoory.Shared.Application.Tests.Audit;

public class AuditPayloadRedactorTests
{
    private static readonly AuditOptions DefaultOptions = new();

    [Fact]
    public void Redacts_password_field_at_top_level()
    {
        var payload = new { Email = "a@b.c", Password = "hunter2" };

        var json = AuditPayloadRedactor.SerializeRedacted(payload, DefaultOptions, NullLogger.Instance);

        json.Should().Contain(DefaultOptions.RedactionSentinel);
        json.Should().NotContain("hunter2");
        json.Should().Contain("a@b.c");
    }

    [Fact]
    public void Redacts_national_id_field()
    {
        var payload = new { NationalId = "12345678" };

        var json = AuditPayloadRedactor.SerializeRedacted(payload, DefaultOptions, NullLogger.Instance);

        json.Should().NotContain("12345678");
        json.Should().Contain(DefaultOptions.RedactionSentinel);
    }

    [Fact]
    public void Leaves_non_matching_properties_untouched()
    {
        var payload = new { FirstName = "Ada", LastName = "Lovelace" };

        var json = AuditPayloadRedactor.SerializeRedacted(payload, DefaultOptions, NullLogger.Instance);

        json.Should().Contain("Ada");
        json.Should().Contain("Lovelace");
        json.Should().NotContain(DefaultOptions.RedactionSentinel);
    }

    [Fact]
    public void Truncates_payload_exceeding_max_characters()
    {
        var options = new AuditOptions { DetailsMaxCharacters = 100 };
        var payload = new { Blob = new string('x', 500) };

        var json = AuditPayloadRedactor.SerializeRedacted(payload, options, NullLogger.Instance);

        json.Length.Should().BeLessThanOrEqualTo(options.DetailsMaxCharacters);
        json.Should().EndWith(options.TruncationSuffix);
    }

    [Fact]
    public void Is_case_insensitive_when_matching_redacted_field_names()
    {
        var lower = new Dictionary<string, object?> { ["password"] = "a" };
        var upper = new Dictionary<string, object?> { ["PASSWORD"] = "b" };
        var mixed = new Dictionary<string, object?> { ["Password"] = "c" };

        foreach (var payload in new object[] { lower, upper, mixed })
        {
            var json = AuditPayloadRedactor.SerializeRedacted(payload, DefaultOptions, NullLogger.Instance);
            json.Should().Contain(DefaultOptions.RedactionSentinel);
        }
    }

    [Fact]
    public void Custom_redacted_fields_list_is_honored()
    {
        var options = new AuditOptions { RedactedFields = ["Pin"] };
        var payload = new { Pin = "9999", Password = "visible-because-not-in-list" };

        var json = AuditPayloadRedactor.SerializeRedacted(payload, options, NullLogger.Instance);

        json.Should().NotContain("9999");
        json.Should().Contain("visible-because-not-in-list");
    }

    [Fact]
    public void Serialization_failure_falls_through_to_sentinel()
    {
        var payload = new CircularPayload();
        payload.Self = payload;

        var json = AuditPayloadRedactor.SerializeRedacted(payload, DefaultOptions, NullLogger.Instance);

        json.Should().StartWith("<serialization-failed:");
    }

    private sealed class CircularPayload
    {
        public CircularPayload? Self { get; set; }
    }
}
