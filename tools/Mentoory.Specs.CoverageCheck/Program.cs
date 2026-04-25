using System.CommandLine;
using System.Diagnostics;
using Mentoory.Specs.CoverageCheck.Coverage;
using Mentoory.Specs.CoverageCheck.Output;
using Mentoory.Specs.CoverageCheck.Parsing;
using Mentoory.Specs.CoverageCheck.Reflection;
using Microsoft.Extensions.FileSystemGlobbing;

namespace Mentoory.Specs.CoverageCheck;

internal static class Program
{
    private const int ExitSuccess = 0;
    private const int ExitCoverageViolation = 1;
    private const int ExitParseViolation = 2;
    private const int ExitReflectionViolation = 3;
    private const int ExitUsageError = 64;

    private const string ToolVersion = "1.0.0";
    private const string WarnModeEnvVar = "MENTOORY_COVERAGECHECK_MODE";

    public static int Main(string[] args)
    {
        var specsRootOption = new Option<DirectoryInfo>("--specs-root")
        {
            Description = "Root of the specs tree to scan.",
            Required = true,
        };

        var testAssembliesOption = new Option<string[]>("--test-assemblies")
        {
            Description = "One or more glob patterns matching test assemblies to inspect.",
            Required = true,
            AllowMultipleArgumentsPerToken = true,
        };

        var modeOption = new Option<string?>("--mode")
        {
            Description = "Set to 'warn' to log violations without exiting non-zero.",
        };

        var reportFormatOption = new Option<string>("--report-format")
        {
            Description = "Output format: text or json.",
            DefaultValueFactory = _ => "text",
        };

        var rootCommand = new RootCommand("Mentoory.Specs.CoverageCheck — spec-to-test coverage gate.")
        {
            specsRootOption,
            testAssembliesOption,
            modeOption,
            reportFormatOption,
        };

        rootCommand.SetAction(parseResult =>
        {
            var specsRoot = parseResult.GetValue(specsRootOption);
            var testAssemblies = parseResult.GetValue(testAssembliesOption);
            var mode = parseResult.GetValue(modeOption);
            var format = parseResult.GetValue(reportFormatOption) ?? "text";

            return Run(specsRoot, testAssemblies, mode, format);
        });

        return rootCommand.Parse(args).Invoke();
    }

    private static int Run(DirectoryInfo? specsRoot, string[]? testAssemblies, string? mode, string format)
    {
        if (specsRoot is null)
        {
            Console.Error.WriteLine("Usage error: --specs-root is required. Run with --help for details.");
            return ExitUsageError;
        }

        if (testAssemblies is null || testAssemblies.Length == 0)
        {
            Console.Error.WriteLine("Usage error: --test-assemblies is required (one or more glob patterns). Run with --help for details.");
            return ExitUsageError;
        }

        if (!specsRoot.Exists)
        {
            Console.Error.WriteLine(FormattableString.Invariant(
                $"Usage error: --specs-root '{specsRoot.FullName}' does not exist."));
            return ExitUsageError;
        }

        if (!string.Equals(format, "text", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine(FormattableString.Invariant(
                $"Usage error: --report-format must be 'text' or 'json' (got '{format}')."));
            return ExitUsageError;
        }

        var warnMode = ResolveWarnMode(mode);
        var stopwatch = Stopwatch.StartNew();

        // Parse specs.
        var specResults = SpecParser.ParseDirectory(specsRoot.FullName);
        var specs = new List<FeatureSpec>();
        var malformed = new List<MalformedExclusion>();
        var hardParseErrors = new List<SpecParseError>();
        foreach (var result in specResults)
        {
            if (result.Spec is not null)
            {
                specs.Add(result.Spec);
            }

            malformed.AddRange(result.MalformedExclusions);
            hardParseErrors.AddRange(result.Errors);
        }

        // Resolve assembly globs.
        var resolvedPaths = ResolveAssemblyGlobs(testAssemblies);

        // Reflect.
        var reflectionResults = TraitReflector.Inspect(resolvedPaths);
        var assemblies = new List<TestAssembly>();
        var reflectionErrors = new List<ReflectionError>();
        foreach (var result in reflectionResults)
        {
            if (result.Assembly is not null)
            {
                assemblies.Add(result.Assembly);
            }

            if (result.Error is not null)
            {
                reflectionErrors.Add(result.Error);
            }
        }

        // Aggregate hard spec errors into the malformed list so they show up in the parse-violation bucket.
        foreach (var hardError in hardParseErrors)
        {
            malformed.Add(new MalformedExclusion(hardError.SpecPath, hardError.LineNumber, hardError.Reason));
        }

        var report = CoverageAnalyzer.Analyze(specs, assemblies, malformed, reflectionErrors);

        stopwatch.Stop();
        var elapsedSeconds = stopwatch.Elapsed.TotalSeconds;

        var realExitCode = ComputeExitCode(report);
        var effectiveExitCode = warnMode ? ExitSuccess : realExitCode;

        if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
        {
            JsonReportWriter.Write(Console.Out, report, ToolVersion, elapsedSeconds, effectiveExitCode);
        }
        else
        {
            TextReportWriter.Write(Console.Out, report, ToolVersion, elapsedSeconds, effectiveExitCode);
        }

        return effectiveExitCode;
    }

    private static int ComputeExitCode(CoverageReport report)
    {
        if (report.ReflectionErrors.Count > 0)
        {
            return ExitReflectionViolation;
        }

        if (report.MalformedExclusions.Count > 0)
        {
            return ExitParseViolation;
        }

        if (report.UnclaimedIds.Count > 0 ||
            report.DanglingTraits.Count > 0 ||
            report.MissingFloorCategories.Count > 0 ||
            report.DuplicateIds.Count > 0)
        {
            return ExitCoverageViolation;
        }

        return ExitSuccess;
    }

    private static bool ResolveWarnMode(string? modeFlag)
    {
        if (!string.IsNullOrEmpty(modeFlag))
        {
            return string.Equals(modeFlag, "warn", StringComparison.OrdinalIgnoreCase);
        }

        var envValue = Environment.GetEnvironmentVariable(WarnModeEnvVar);
        return !string.IsNullOrEmpty(envValue) && string.Equals(envValue, "warn", StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<string> ResolveAssemblyGlobs(string[] patterns)
    {
        var cwd = Directory.GetCurrentDirectory();
        var results = new List<string>();

        foreach (var raw in patterns)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            var pattern = raw.Trim();
            if (Path.IsPathRooted(pattern) && File.Exists(pattern))
            {
                results.Add(Path.GetFullPath(pattern));
                continue;
            }

            var combined = Path.IsPathRooted(pattern) ? pattern : Path.Combine(cwd, pattern);
            if (File.Exists(combined))
            {
                results.Add(Path.GetFullPath(combined));
                continue;
            }

            // Glob: split into a stable base directory and a relative pattern.
            var (baseDir, relativePattern) = SplitGlob(pattern, cwd);
            if (!Directory.Exists(baseDir))
            {
                continue;
            }

            var matcher = new Matcher(StringComparison.Ordinal);
            matcher.AddInclude(relativePattern);

            var matchResult = matcher.Execute(new Microsoft.Extensions.FileSystemGlobbing.Abstractions.DirectoryInfoWrapper(new DirectoryInfo(baseDir)));
            foreach (var match in matchResult.Files)
            {
                var full = Path.GetFullPath(Path.Combine(baseDir, match.Path));
                results.Add(full);
            }
        }

        return results
            .Distinct(StringComparer.Ordinal)
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToArray();
    }

    private static (string BaseDir, string RelativePattern) SplitGlob(string pattern, string cwd)
    {
        // Walk the pattern's leading components until we hit a wildcard; everything before
        // becomes the base directory, everything after stays the glob expression.
        var normalized = pattern.Replace('\\', '/');
        var firstWildcard = normalized.IndexOfAny(new[] { '*', '?', '[' });

        string basePart;
        string relPart;
        if (firstWildcard < 0)
        {
            basePart = Path.GetDirectoryName(normalized) ?? cwd;
            relPart = Path.GetFileName(normalized);
        }
        else
        {
            var lastSlashBeforeWildcard = normalized.LastIndexOf('/', firstWildcard);
            if (lastSlashBeforeWildcard < 0)
            {
                basePart = string.Empty;
                relPart = normalized;
            }
            else
            {
                basePart = normalized[..lastSlashBeforeWildcard];
                relPart = normalized[(lastSlashBeforeWildcard + 1)..];
            }
        }

        var rooted = Path.IsPathRooted(basePart) ? basePart : Path.Combine(cwd, basePart);
        var fullBase = string.IsNullOrEmpty(rooted) ? cwd : Path.GetFullPath(rooted);
        if (string.IsNullOrEmpty(relPart))
        {
            relPart = "*";
        }

        return (fullBase, relPart);
    }
}
