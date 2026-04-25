namespace Mentoory.Specs.CoverageCheck.Tests;

/// <summary>
/// Resolves on-disk paths to the synthetic fixtures shipped alongside the test
/// assembly (spec markdowns under <c>Fixtures/specs/</c> and the synthetic
/// <c>SampleXunitTests.dll</c> built before this assembly).
/// </summary>
internal static class FixturePaths
{
    /// <summary>Directory containing the synthetic spec.md fixtures.</summary>
    public static string SpecsDirectory => Path.Combine(AppContext.BaseDirectory, "Fixtures", "specs");

    /// <summary>
    /// Absolute path to the synthetic xUnit assembly produced by the
    /// <c>BuildFixtures</c> MSBuild target. Resolved relative to the test
    /// assembly's location so it works from any working directory.
    /// </summary>
    public static string SampleXunitAssembly
    {
        get
        {
            var configuration = GetConfigurationName();
            return Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "Fixtures", "assemblies", "SampleXunitTests", "bin", configuration, "net10.0", "SampleXunitTests.dll"));
        }
    }

    /// <summary>Absolute path to a single fixture spec markdown file.</summary>
    public static string Spec(string fileName) => Path.Combine(SpecsDirectory, fileName);

    /// <summary>Path to a golden-file fixture (text or json).</summary>
    public static string GoldenFile(string fileName) => Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);

    private static string GetConfigurationName()
    {
        // Mirrors the MSBuild $(Configuration) used to build the synthetic fixture project.
#if DEBUG
        return "Debug";
#else
        return "Release";
#endif
    }
}
