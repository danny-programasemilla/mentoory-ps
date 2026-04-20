using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mentoory.Specs.CoverageCheck.Coverage;
using Mentoory.Specs.CoverageCheck.Parsing;

namespace Mentoory.Specs.CoverageCheck.Output;

/// <summary>
/// Renders a <see cref="CoverageReport"/> to the deterministic JSON schema
/// specified in <c>contracts/coverage-check-cli.md</c>. Output is byte-stable
/// for byte-identical inputs.
/// </summary>
public static class JsonReportWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>Writes the JSON document to <paramref name="writer"/>.</summary>
    public static void Write(TextWriter writer, CoverageReport report, string toolVersion, double elapsedSeconds, int exitCode)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(toolVersion);

        var document = BuildDocument(report, toolVersion, elapsedSeconds, exitCode);
        var json = JsonSerializer.Serialize(document, SerializerOptions);
        writer.WriteLine(json);
    }

    /// <summary>Convenience: serialize to a string.</summary>
    public static string ToString(CoverageReport report, string toolVersion, double elapsedSeconds, int exitCode)
    {
        var sb = new StringBuilder();
        using var writer = new StringWriter(sb, CultureInfo.InvariantCulture);
        Write(writer, report, toolVersion, elapsedSeconds, exitCode);
        return sb.ToString();
    }

    private static CoverageReportDocument BuildDocument(
        CoverageReport report,
        string toolVersion,
        double elapsedSeconds,
        int exitCode)
    {
        return new CoverageReportDocument
        {
            Version = toolVersion,
            ElapsedMs = (long)Math.Round(elapsedSeconds * 1000.0),
            Scanned = new ScannedCounts
            {
                Specs = report.ScannedSpecCount,
                Assemblies = report.ScannedAssemblyCount,
                Methods = report.ScannedTestMethodCount,
                Claims = report.ScannedTraitClaimCount,
            },
            UnclaimedIds = report.UnclaimedIds.Select(i => new UnclaimedDto
            {
                Kind = KindString(i.Kind),
                Value = i.Value,
                SpecPath = i.SpecPath,
                LineNumber = i.LineNumber,
            }).ToArray(),
            DanglingTraits = report.DanglingTraits.Select(c => new DanglingDto
            {
                Key = c.Key,
                Value = c.Value,
                Method = c.Method.FullyQualifiedName,
            }).ToArray(),
            DuplicateIds = report.DuplicateIds.Select(d => new DuplicateDto
            {
                Kind = KindString(d.Kind),
                Value = d.Value,
                SpecPaths = d.SpecPaths,
            }).ToArray(),
            MissingFloorCategories = report.MissingFloorCategories.Select(m => new MissingFloorDto
            {
                SpecPath = m.Spec.Path,
                Category = m.Category.Name,
            }).ToArray(),
            Exclusions = report.Exclusions.Select(e => new ExclusionDto
            {
                Kind = KindString(e.Target.Kind),
                Value = e.Target.Value,
                SpecPath = e.Target.SpecPath,
                LineNumber = e.LineNumber,
                Justification = e.Justification,
            }).ToArray(),
            MalformedExclusions = report.MalformedExclusions.Select(m => new MalformedDto
            {
                SpecPath = m.SpecPath,
                LineNumber = m.LineNumber,
                Reason = m.Reason,
            }).ToArray(),
            ReflectionErrors = report.ReflectionErrors.Select(r => new ReflectionErrorDto
            {
                AssemblyPath = r.AssemblyPath,
                Reason = r.Reason,
            }).ToArray(),
            Result = report.IsClean ? "PASSED" : "FAILED",
            ExitCode = exitCode,
        };
    }

    private static string KindString(RequirementKind kind) => kind switch
    {
        RequirementKind.Fr => "Fr",
        RequirementKind.Sc => "Sc",
        _ => kind.ToString(),
    };

    // --- DTOs ordered to match contracts/coverage-check-cli.md exactly ---
    private sealed class CoverageReportDocument
    {
        [JsonPropertyOrder(1)]
        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        [JsonPropertyOrder(2)]
        [JsonPropertyName("elapsedMs")]
        public long ElapsedMs { get; set; }

        [JsonPropertyOrder(3)]
        [JsonPropertyName("scanned")]
        public ScannedCounts Scanned { get; set; } = new();

        [JsonPropertyOrder(4)]
        [JsonPropertyName("unclaimedIds")]
        public IReadOnlyList<UnclaimedDto> UnclaimedIds { get; set; } = Array.Empty<UnclaimedDto>();

        [JsonPropertyOrder(5)]
        [JsonPropertyName("danglingTraits")]
        public IReadOnlyList<DanglingDto> DanglingTraits { get; set; } = Array.Empty<DanglingDto>();

        [JsonPropertyOrder(6)]
        [JsonPropertyName("duplicateIds")]
        public IReadOnlyList<DuplicateDto> DuplicateIds { get; set; } = Array.Empty<DuplicateDto>();

        [JsonPropertyOrder(7)]
        [JsonPropertyName("missingFloorCategories")]
        public IReadOnlyList<MissingFloorDto> MissingFloorCategories { get; set; } = Array.Empty<MissingFloorDto>();

        [JsonPropertyOrder(8)]
        [JsonPropertyName("exclusions")]
        public IReadOnlyList<ExclusionDto> Exclusions { get; set; } = Array.Empty<ExclusionDto>();

        [JsonPropertyOrder(9)]
        [JsonPropertyName("malformedExclusions")]
        public IReadOnlyList<MalformedDto> MalformedExclusions { get; set; } = Array.Empty<MalformedDto>();

        [JsonPropertyOrder(10)]
        [JsonPropertyName("reflectionErrors")]
        public IReadOnlyList<ReflectionErrorDto> ReflectionErrors { get; set; } = Array.Empty<ReflectionErrorDto>();

        [JsonPropertyOrder(11)]
        [JsonPropertyName("result")]
        public string Result { get; set; } = string.Empty;

        [JsonPropertyOrder(12)]
        [JsonPropertyName("exitCode")]
        public int ExitCode { get; set; }
    }

    private sealed class ScannedCounts
    {
        [JsonPropertyOrder(1)]
        [JsonPropertyName("specs")]
        public int Specs { get; set; }

        [JsonPropertyOrder(2)]
        [JsonPropertyName("assemblies")]
        public int Assemblies { get; set; }

        [JsonPropertyOrder(3)]
        [JsonPropertyName("methods")]
        public int Methods { get; set; }

        [JsonPropertyOrder(4)]
        [JsonPropertyName("claims")]
        public int Claims { get; set; }
    }

    private sealed class UnclaimedDto
    {
        [JsonPropertyOrder(1)]
        [JsonPropertyName("kind")]
        public string Kind { get; set; } = string.Empty;

        [JsonPropertyOrder(2)]
        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;

        [JsonPropertyOrder(3)]
        [JsonPropertyName("specPath")]
        public string SpecPath { get; set; } = string.Empty;

        [JsonPropertyOrder(4)]
        [JsonPropertyName("lineNumber")]
        public int LineNumber { get; set; }
    }

    private sealed class DanglingDto
    {
        [JsonPropertyOrder(1)]
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        [JsonPropertyOrder(2)]
        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;

        [JsonPropertyOrder(3)]
        [JsonPropertyName("method")]
        public string Method { get; set; } = string.Empty;
    }

    private sealed class DuplicateDto
    {
        [JsonPropertyOrder(1)]
        [JsonPropertyName("kind")]
        public string Kind { get; set; } = string.Empty;

        [JsonPropertyOrder(2)]
        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;

        [JsonPropertyOrder(3)]
        [JsonPropertyName("specPaths")]
        public IReadOnlyList<string> SpecPaths { get; set; } = Array.Empty<string>();
    }

    private sealed class MissingFloorDto
    {
        [JsonPropertyOrder(1)]
        [JsonPropertyName("specPath")]
        public string SpecPath { get; set; } = string.Empty;

        [JsonPropertyOrder(2)]
        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;
    }

    private sealed class ExclusionDto
    {
        [JsonPropertyOrder(1)]
        [JsonPropertyName("kind")]
        public string Kind { get; set; } = string.Empty;

        [JsonPropertyOrder(2)]
        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;

        [JsonPropertyOrder(3)]
        [JsonPropertyName("specPath")]
        public string SpecPath { get; set; } = string.Empty;

        [JsonPropertyOrder(4)]
        [JsonPropertyName("lineNumber")]
        public int LineNumber { get; set; }

        [JsonPropertyOrder(5)]
        [JsonPropertyName("justification")]
        public string Justification { get; set; } = string.Empty;
    }

    private sealed class MalformedDto
    {
        [JsonPropertyOrder(1)]
        [JsonPropertyName("specPath")]
        public string SpecPath { get; set; } = string.Empty;

        [JsonPropertyOrder(2)]
        [JsonPropertyName("lineNumber")]
        public int LineNumber { get; set; }

        [JsonPropertyOrder(3)]
        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;
    }

    private sealed class ReflectionErrorDto
    {
        [JsonPropertyOrder(1)]
        [JsonPropertyName("assemblyPath")]
        public string AssemblyPath { get; set; } = string.Empty;

        [JsonPropertyOrder(2)]
        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;
    }
}
