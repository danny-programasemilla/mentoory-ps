using System.Globalization;
using System.Text;
using Mentoory.Specs.CoverageCheck.Coverage;
using Mentoory.Specs.CoverageCheck.Parsing;
using Mentoory.Specs.CoverageCheck.Reflection;

namespace Mentoory.Specs.CoverageCheck.Output;

/// <summary>
/// Renders a <see cref="CoverageReport"/> to the deterministic text format
/// specified in <c>contracts/coverage-check-cli.md</c>.
/// </summary>
public static class TextReportWriter
{
    /// <summary>Writes the report to <paramref name="writer"/>.</summary>
    public static void Write(TextWriter writer, CoverageReport report, string toolVersion, double elapsedSeconds, int exitCode)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(toolVersion);

        writer.WriteLine(FormattableString.Invariant($"Mentoory.Specs.CoverageCheck v{toolVersion}"));
        writer.WriteLine(FormattableString.Invariant(
            $"Scanned: {report.ScannedSpecCount} spec files, {report.ScannedAssemblyCount} test assemblies, {report.ScannedTestMethodCount} test methods, {report.ScannedTraitClaimCount} trait claims"));
        writer.WriteLine(FormattableString.Invariant($"Elapsed: {elapsedSeconds.ToString("0.000", CultureInfo.InvariantCulture)}s"));

        WriteUnclaimed(writer, report.UnclaimedIds);
        WriteDangling(writer, report.DanglingTraits);
        WriteDuplicates(writer, report.DuplicateIds);
        WriteMissingFloors(writer, report.MissingFloorCategories);
        WriteExclusions(writer, report.Exclusions);
        WriteMalformed(writer, report.MalformedExclusions);
        WriteReflectionErrors(writer, report.ReflectionErrors);

        writer.WriteLine();
        var resultWord = report.IsClean ? "PASSED" : "FAILED";
        writer.WriteLine(FormattableString.Invariant($"RESULT: {resultWord} (exit code {exitCode})"));
    }

    /// <summary>Convenience for tests / diagnostics: render to a string.</summary>
    public static string ToString(CoverageReport report, string toolVersion, double elapsedSeconds, int exitCode)
    {
        var sb = new StringBuilder();
        using var writer = new StringWriter(sb, CultureInfo.InvariantCulture);
        Write(writer, report, toolVersion, elapsedSeconds, exitCode);
        return sb.ToString();
    }

    private static void WriteUnclaimed(TextWriter writer, IReadOnlyList<RequirementId> ids)
    {
        if (ids.Count == 0)
        {
            return;
        }

        writer.WriteLine();
        writer.WriteLine("== Unclaimed Identifiers (coverage violation) ==");
        foreach (var id in ids)
        {
            var traitKey = id.Kind == RequirementKind.Fr ? "Spec" : "Sc";
            writer.WriteLine(FormattableString.Invariant($"  [{id.Value}] {id.SpecPath}:{id.LineNumber}"));
            writer.WriteLine(FormattableString.Invariant($"    Note: no test carries [Trait(\"{traitKey}\",\"{id.Value}\")]"));
        }
    }

    private static void WriteDangling(TextWriter writer, IReadOnlyList<TestClaim> claims)
    {
        if (claims.Count == 0)
        {
            return;
        }

        writer.WriteLine();
        writer.WriteLine("== Dangling Traits (coverage violation) ==");
        foreach (var claim in claims)
        {
            writer.WriteLine(FormattableString.Invariant($"  [Trait(\"{claim.Key}\",\"{claim.Value}\")]"));
            writer.WriteLine(FormattableString.Invariant($"    on {claim.Method.FullyQualifiedName}"));
            writer.WriteLine(FormattableString.Invariant($"    Note: no spec declares {claim.Value}"));
        }
    }

    private static void WriteDuplicates(TextWriter writer, IReadOnlyList<DuplicateIdentifier> duplicates)
    {
        if (duplicates.Count == 0)
        {
            return;
        }

        writer.WriteLine();
        writer.WriteLine("== Duplicate Identifiers (coverage violation) ==");
        foreach (var dup in duplicates)
        {
            writer.WriteLine(FormattableString.Invariant($"  {dup.Value} declared in:"));
            foreach (var path in dup.SpecPaths)
            {
                writer.WriteLine(FormattableString.Invariant($"    {path}"));
            }
        }
    }

    private static void WriteMissingFloors(TextWriter writer, IReadOnlyList<MissingFloor> missing)
    {
        if (missing.Count == 0)
        {
            return;
        }

        writer.WriteLine();
        writer.WriteLine("== Missing Floor Categories (coverage violation) ==");
        var grouped = missing
            .GroupBy(m => m.Spec.Path, StringComparer.Ordinal)
            .OrderBy(g => g.Key, StringComparer.Ordinal);

        foreach (var group in grouped)
        {
            writer.WriteLine(FormattableString.Invariant($"  {group.Key} — access-security: true"));
            foreach (var item in group.OrderBy(m => m.Category.Name, StringComparer.Ordinal))
            {
                writer.WriteLine(FormattableString.Invariant($"    - {item.Category.Name}"));
                writer.WriteLine(FormattableString.Invariant(
                    $"      No test carries [Trait(\"Floor\",\"{item.Category.Name}\")] referencing this feature's types."));
            }
        }
    }

    private static void WriteExclusions(TextWriter writer, IReadOnlyList<ExclusionMarker> exclusions)
    {
        if (exclusions.Count == 0)
        {
            return;
        }

        writer.WriteLine();
        writer.WriteLine("== Exclusions (audit only — not a violation) ==");
        foreach (var exclusion in exclusions)
        {
            writer.WriteLine(FormattableString.Invariant(
                $"  [{exclusion.Target.Value}] {exclusion.Target.SpecPath}:{exclusion.LineNumber}"));
            writer.WriteLine(FormattableString.Invariant($"    Justification: \"{exclusion.Justification}\""));
        }
    }

    private static void WriteMalformed(TextWriter writer, IReadOnlyList<MalformedExclusion> malformed)
    {
        if (malformed.Count == 0)
        {
            return;
        }

        writer.WriteLine();
        writer.WriteLine("== Malformed Exclusions (parse violation) ==");
        foreach (var item in malformed)
        {
            writer.WriteLine(FormattableString.Invariant($"  {item.SpecPath}:{item.LineNumber}"));
            writer.WriteLine(FormattableString.Invariant($"    Reason: {item.Reason}"));
        }
    }

    private static void WriteReflectionErrors(TextWriter writer, IReadOnlyList<ReflectionError> errors)
    {
        if (errors.Count == 0)
        {
            return;
        }

        writer.WriteLine();
        writer.WriteLine("== Reflection Errors (reflection violation) ==");
        foreach (var error in errors)
        {
            writer.WriteLine(FormattableString.Invariant($"  {error.AssemblyPath}"));
            writer.WriteLine(FormattableString.Invariant($"    Reason: {error.Reason}"));
        }
    }
}
