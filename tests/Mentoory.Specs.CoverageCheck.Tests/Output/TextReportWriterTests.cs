using FluentAssertions;
using Mentoory.Specs.CoverageCheck.Output;
using Xunit;

namespace Mentoory.Specs.CoverageCheck.Tests.Output;

public class TextReportWriterTests
{
    [Fact]
    public void Write_PopulatedReport_MatchesGoldenFile()
    {
        var report = GoldenFixture.BuildReport();

        var actual = TextReportWriter.ToString(
            report,
            GoldenFixture.ToolVersion,
            GoldenFixture.ElapsedSeconds,
            GoldenFixture.ExitCode);

        var goldenPath = FixturePaths.GoldenFile("expected-text-output.txt");
        File.Exists(goldenPath).Should().BeTrue($"the golden file at {goldenPath} must be present alongside the test assembly");
        var expected = File.ReadAllText(goldenPath);

        Normalise(actual).Should().Be(Normalise(expected));
    }

    [Fact]
    public void Write_CleanReport_StillEmitsResultLine()
    {
        var clean = new Mentoory.Specs.CoverageCheck.Coverage.CoverageReport(
            ScannedSpecCount: 0,
            ScannedAssemblyCount: 0,
            ScannedTestMethodCount: 0,
            ScannedTraitClaimCount: 0,
            UnclaimedIds: Array.Empty<Mentoory.Specs.CoverageCheck.Parsing.RequirementId>(),
            DanglingTraits: Array.Empty<Mentoory.Specs.CoverageCheck.Reflection.TestClaim>(),
            Exclusions: Array.Empty<Mentoory.Specs.CoverageCheck.Parsing.ExclusionMarker>(),
            MissingFloorCategories: Array.Empty<Mentoory.Specs.CoverageCheck.Coverage.MissingFloor>(),
            DuplicateIds: Array.Empty<Mentoory.Specs.CoverageCheck.Coverage.DuplicateIdentifier>(),
            MalformedExclusions: Array.Empty<Mentoory.Specs.CoverageCheck.Parsing.MalformedExclusion>(),
            ReflectionErrors: Array.Empty<Mentoory.Specs.CoverageCheck.Reflection.ReflectionError>());

        var actual = TextReportWriter.ToString(clean, "1.0.0", 0.001, 0);

        actual.Should().Contain("RESULT: PASSED (exit code 0)");
    }

    /// <summary>Normalise CR/LF differences so the test is deterministic across OSes.</summary>
    private static string Normalise(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal);
}
