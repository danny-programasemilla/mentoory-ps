using FluentAssertions;
using Mentoory.Specs.CoverageCheck.Output;
using Xunit;

namespace Mentoory.Specs.CoverageCheck.Tests.Output;

public class JsonReportWriterTests
{
    [Fact]
    public void Write_PopulatedReport_MatchesGoldenJsonFile()
    {
        var report = GoldenFixture.BuildReport();

        var actual = JsonReportWriter.ToString(
            report,
            GoldenFixture.ToolVersion,
            GoldenFixture.ElapsedSeconds,
            GoldenFixture.ExitCode);

        var goldenPath = FixturePaths.GoldenFile("expected-json-output.json");
        File.Exists(goldenPath).Should().BeTrue($"the golden file at {goldenPath} must be present alongside the test assembly");
        var expected = File.ReadAllText(goldenPath);

        Normalise(actual).Should().Be(Normalise(expected));
    }

    private static string Normalise(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal);
}
