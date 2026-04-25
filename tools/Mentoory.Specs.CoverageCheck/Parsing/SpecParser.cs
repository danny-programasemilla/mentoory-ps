using System.Text.RegularExpressions;

namespace Mentoory.Specs.CoverageCheck.Parsing;

/// <summary>
/// Result of a single spec.md parse — paired with any malformed-exclusion or front-matter
/// errors encountered. Front-matter parse errors map to exit code 2.
/// </summary>
public sealed record SpecParseResult(
    FeatureSpec? Spec,
    IReadOnlyList<MalformedExclusion> MalformedExclusions,
    IReadOnlyList<SpecParseError> Errors);

/// <summary>
/// A line that matched the exclusion-marker intent but failed validation per
/// research.md #10 (justification too short, missing trailing period, etc.).
/// </summary>
public sealed record MalformedExclusion(string SpecPath, int LineNumber, string Reason);

/// <summary>
/// A structural parse error that prevents producing a usable <see cref="FeatureSpec"/>.
/// </summary>
public sealed record SpecParseError(string SpecPath, int LineNumber, string Reason);

/// <summary>
/// Parses a directory tree of <c>spec.md</c> files into <see cref="FeatureSpec"/> instances.
/// Pure with respect to the filesystem read; no other side effects.
/// </summary>
public static class SpecParser
{
    private const int FrontMatterScanLimit = 100;
    private const int FrontMatterCloseSearchLimit = 30;

    private static readonly Regex IdentifierRegex = new(
        @"\*\*(FR|SC)-\d{3}(?:-\d{2})?\*\*",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ExclusionRegex = new(
        @"\*Coverage:\s*N/A\s*[\u2014-]{1,2}\s*(.{20,}?)\.\s*\*",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ExclusionIntentRegex = new(
        @"\*Coverage:\s*N/A",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex FrontMatterFenceRegex = new(
        @"^---\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex FrontMatterPairRegex = new(
        @"^([A-Za-z][A-Za-z0-9_\-]*)\s*:\s*(.+?)\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Walks <paramref name="specsRoot"/> recursively for files named <c>spec.md</c>
    /// and parses each. Returns one <see cref="SpecParseResult"/> per file, in
    /// deterministic path-sorted order.
    /// </summary>
    public static IReadOnlyList<SpecParseResult> ParseDirectory(string specsRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(specsRoot);

        if (!Directory.Exists(specsRoot))
        {
            return Array.Empty<SpecParseResult>();
        }

        var files = Directory
            .EnumerateFiles(specsRoot, "spec.md", SearchOption.AllDirectories)
            .Select(System.IO.Path.GetFullPath)
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToArray();

        var results = new List<SpecParseResult>(files.Length);
        foreach (var file in files)
        {
            results.Add(ParseFile(file));
        }

        return results;
    }

    /// <summary>
    /// Parses a single <c>spec.md</c> file at <paramref name="specPath"/>.
    /// </summary>
    public static SpecParseResult ParseFile(string specPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(specPath);

        string[] lines;
        try
        {
            lines = File.ReadAllLines(specPath);
        }
        catch (IOException ex)
        {
            return new SpecParseResult(
                Spec: null,
                MalformedExclusions: Array.Empty<MalformedExclusion>(),
                Errors: new[] { new SpecParseError(specPath, 0, $"I/O error reading file: {ex.Message}") });
        }

        var (featureNumber, slug) = ExtractFeatureNumberAndSlug(specPath);

        var (accessSecurityOptIn, frontMatterEndIndex, frontMatterErrors) = ParseFrontMatter(specPath, lines);
        if (frontMatterErrors.Count > 0)
        {
            return new SpecParseResult(
                Spec: null,
                MalformedExclusions: Array.Empty<MalformedExclusion>(),
                Errors: frontMatterErrors);
        }

        var (requirementIds, duplicateErrors) = ExtractRequirementIds(specPath, lines, frontMatterEndIndex);
        var (exclusions, malformed) = ExtractExclusions(specPath, lines, requirementIds);

        var errors = new List<SpecParseError>();
        errors.AddRange(duplicateErrors);

        var spec = new FeatureSpec(
            Path: specPath,
            FeatureNumber: featureNumber,
            Slug: slug,
            AccessSecurityOptIn: accessSecurityOptIn,
            RequirementIds: requirementIds,
            Exclusions: exclusions);

        return new SpecParseResult(spec, malformed, errors);
    }

    private static (string FeatureNumber, string Slug) ExtractFeatureNumberAndSlug(string specPath)
    {
        var dirName = new DirectoryInfo(System.IO.Path.GetDirectoryName(specPath) ?? string.Empty).Name;
        var dashIndex = dirName.IndexOf('-', StringComparison.Ordinal);
        if (dashIndex <= 0)
        {
            return (dirName, string.Empty);
        }

        var prefix = dirName[..dashIndex];
        var slug = dirName[(dashIndex + 1)..];
        return (prefix, slug);
    }

    private static (bool AccessSecurityOptIn, int EndIndex, IReadOnlyList<SpecParseError> Errors) ParseFrontMatter(
        string specPath,
        string[] lines)
    {
        if (lines.Length == 0 || !FrontMatterFenceRegex.IsMatch(lines[0]))
        {
            return (false, -1, Array.Empty<SpecParseError>());
        }

        var scanCeiling = Math.Min(lines.Length, FrontMatterScanLimit);
        var closeCeiling = Math.Min(scanCeiling, 1 + FrontMatterCloseSearchLimit);

        var closeIndex = -1;
        for (var i = 1; i < closeCeiling; i++)
        {
            if (FrontMatterFenceRegex.IsMatch(lines[i]))
            {
                closeIndex = i;
                break;
            }
        }

        if (closeIndex < 0)
        {
            return (
                false,
                -1,
                new[] { new SpecParseError(specPath, 1, "Front-matter opens with '---' but does not close within 30 lines.") });
        }

        var optIn = false;
        for (var i = 1; i < closeIndex; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var match = FrontMatterPairRegex.Match(line);
            if (!match.Success)
            {
                continue;
            }

            var key = match.Groups[1].Value;
            var value = match.Groups[2].Value.Trim();

            if (string.Equals(key, "access-security", StringComparison.Ordinal) &&
                string.Equals(value, "true", StringComparison.Ordinal))
            {
                optIn = true;
            }
        }

        return (optIn, closeIndex, Array.Empty<SpecParseError>());
    }

    private static (IReadOnlyList<RequirementId> Ids, IReadOnlyList<SpecParseError> Errors) ExtractRequirementIds(
        string specPath,
        string[] lines,
        int frontMatterEndIndex)
    {
        var ids = new List<RequirementId>();
        var firstSeen = new Dictionary<(RequirementKind Kind, string Value), int>();
        var duplicateLines = new Dictionary<(RequirementKind Kind, string Value), List<int>>();

        var startIndex = frontMatterEndIndex + 1;
        for (var i = startIndex; i < lines.Length; i++)
        {
            var line = lines[i];
            var matches = IdentifierRegex.Matches(line);
            foreach (Match match in matches)
            {
                var token = match.Value.Trim('*');
                var kind = token.StartsWith("FR", StringComparison.Ordinal) ? RequirementKind.Fr : RequirementKind.Sc;
                var key = (kind, token);
                var lineNumber = i + 1;

                if (firstSeen.ContainsKey(key))
                {
                    if (!duplicateLines.TryGetValue(key, out var list))
                    {
                        list = new List<int> { firstSeen[key] };
                        duplicateLines[key] = list;
                    }

                    list.Add(lineNumber);
                    continue;
                }

                firstSeen[key] = lineNumber;
                ids.Add(new RequirementId(kind, token, specPath, lineNumber));
            }
        }

        var errors = new List<SpecParseError>();
        foreach (var entry in duplicateLines.OrderBy(e => e.Key.Value, StringComparer.Ordinal))
        {
            var formattedLines = string.Join(", ", entry.Value);
            errors.Add(new SpecParseError(
                specPath,
                entry.Value[0],
                FormattableString.Invariant(
                    $"Identifier {entry.Key.Value} declared multiple times within the same spec at lines {formattedLines}.")));
        }

        return (ids, errors);
    }

    private static (IReadOnlyList<ExclusionMarker> Markers, IReadOnlyList<MalformedExclusion> Malformed) ExtractExclusions(
        string specPath,
        string[] lines,
        IReadOnlyList<RequirementId> ids)
    {
        var markers = new List<ExclusionMarker>();
        var malformed = new List<MalformedExclusion>();

        if (ids.Count == 0)
        {
            // Still scan the document for stray exclusion markers so we can report malformed ones.
            for (var i = 0; i < lines.Length; i++)
            {
                if (ExclusionIntentRegex.IsMatch(lines[i]) && !ExclusionRegex.IsMatch(lines[i]))
                {
                    malformed.Add(new MalformedExclusion(specPath, i + 1, DescribeMalformedExclusion(lines[i])));
                }
            }

            return (markers, malformed);
        }

        // Index identifiers by line number for O(1) reverse lookup.
        var idsByLine = new Dictionary<int, List<RequirementId>>();
        foreach (var id in ids)
        {
            if (!idsByLine.TryGetValue(id.LineNumber, out var list))
            {
                list = new List<RequirementId>();
                idsByLine[id.LineNumber] = list;
            }

            list.Add(id);
        }

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (!ExclusionIntentRegex.IsMatch(line))
            {
                continue;
            }

            var match = ExclusionRegex.Match(line);
            if (!match.Success)
            {
                malformed.Add(new MalformedExclusion(specPath, i + 1, DescribeMalformedExclusion(line)));
                continue;
            }

            // Locate the identifier this exclusion targets: the identifier on the same line,
            // or the most recent identifier on a preceding line within a small window.
            var target = ResolveExclusionTarget(idsByLine, i + 1);
            if (target is null)
            {
                malformed.Add(new MalformedExclusion(
                    specPath,
                    i + 1,
                    "Exclusion marker is not attached to a requirement identifier."));
                continue;
            }

            var justification = match.Groups[1].Value.Trim();
            markers.Add(new ExclusionMarker(target, justification, i + 1));
        }

        return (markers, malformed);
    }

    private static RequirementId? ResolveExclusionTarget(
        Dictionary<int, List<RequirementId>> idsByLine,
        int markerLineNumber)
    {
        // Same line first (rare — Markdown nesting may put both on one line).
        if (idsByLine.TryGetValue(markerLineNumber, out var sameLine) && sameLine.Count > 0)
        {
            return sameLine[^1];
        }

        // Otherwise the immediately preceding line is the canonical case.
        const int LookbackLimit = 3;
        for (var delta = 1; delta <= LookbackLimit; delta++)
        {
            var candidateLine = markerLineNumber - delta;
            if (candidateLine <= 0)
            {
                break;
            }

            if (idsByLine.TryGetValue(candidateLine, out var prior) && prior.Count > 0)
            {
                return prior[^1];
            }
        }

        return null;
    }

    private static string DescribeMalformedExclusion(string line)
    {
        // Best-effort diagnostic — distinguish the common failure modes for clearer reports.
        if (!line.Contains('*', StringComparison.Ordinal))
        {
            return "exclusion marker is not wrapped in italic asterisks";
        }

        if (!line.Contains('.', StringComparison.Ordinal))
        {
            return "justification is missing a trailing period";
        }

        // Extract content between 'N/A' and the trailing italic close.
        var content = line.AsSpan();
        var naIndex = content.IndexOf("N/A".AsSpan(), StringComparison.Ordinal);
        if (naIndex >= 0)
        {
            var rest = content[(naIndex + 3)..].ToString();
            var trimmed = rest.TrimEnd('*', ' ', '\t').TrimEnd('.');
            var dashStripped = trimmed.TrimStart(' ', '\t', '\u2014', '-');
            if (dashStripped.Length < 20)
            {
                return FormattableString.Invariant($"justification shorter than 20 characters (got {dashStripped.Length})");
            }
        }

        return "exclusion marker does not match the required `*Coverage: N/A — <justification>.*` shape";
    }
}
