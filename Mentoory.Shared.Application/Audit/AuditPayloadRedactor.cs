using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace Mentoory.Shared.Application.Audit;

/// <summary>
/// Serializes a command payload to JSON, redacting top-level properties whose names
/// match <see cref="AuditOptions.RedactedFields"/> and truncating overly long payloads.
/// </summary>
public static class AuditPayloadRedactor
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Serializes <paramref name="payload"/> to a JSON string with redaction and truncation applied.
    /// </summary>
    public static string SerializeRedacted(object payload, AuditOptions options, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(options);

        string json;
        try
        {
            var node = JsonSerializer.SerializeToNode(payload, payload.GetType(), SerializerOptions);
            if (node is JsonObject obj)
            {
                RedactTopLevel(obj, options);
            }

            json = node?.ToJsonString(SerializerOptions) ?? string.Empty;
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or InvalidOperationException)
        {
            logger.LogWarning(ex, "Failed to serialize audit payload of type {PayloadType}", payload.GetType().FullName);
            return $"<serialization-failed: {ex.GetType().Name}>";
        }

        if (json.Length > options.DetailsMaxCharacters)
        {
            var keep = Math.Max(0, options.DetailsMaxCharacters - options.TruncationSuffix.Length);
            return json[..keep] + options.TruncationSuffix;
        }

        return json;
    }

    private static void RedactTopLevel(JsonObject node, AuditOptions options)
    {
        var redactedSet = new HashSet<string>(options.RedactedFields, StringComparer.OrdinalIgnoreCase);
        var keys = node.Select(kvp => kvp.Key).ToArray();
        foreach (var key in keys)
        {
            if (redactedSet.Contains(key))
            {
                node[key] = JsonValue.Create(options.RedactionSentinel);
            }
        }
    }
}
