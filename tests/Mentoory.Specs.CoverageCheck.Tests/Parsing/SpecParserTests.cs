using FluentAssertions;
using Mentoory.Specs.CoverageCheck.Parsing;
using Xunit;

namespace Mentoory.Specs.CoverageCheck.Tests.Parsing;

public class SpecParserTests
{
    [Fact]
    public void ParseFile_CleanSpec_ProducesOptedInFeatureWithExpectedIds()
    {
        var path = FixturePaths.Spec("sample-clean.md");

        var result = SpecParser.ParseFile(path);

        result.Errors.Should().BeEmpty();
        result.MalformedExclusions.Should().BeEmpty();
        result.Spec.Should().NotBeNull();
        result.Spec!.AccessSecurityOptIn.Should().BeTrue();
        result.Spec.RequirementIds.Should().HaveCount(4);
        result.Spec.RequirementIds.Select(id => id.Value)
            .Should().BeEquivalentTo(new[] { "FR-100-01", "FR-100-02", "FR-100-03", "SC-100-01" });
        result.Spec.Exclusions.Should().BeEmpty();
    }

    [Fact]
    public void ParseFile_ValidExclusionMarker_IsAccepted()
    {
        var path = FixturePaths.Spec("sample-excluded.md");

        var result = SpecParser.ParseFile(path);

        result.Errors.Should().BeEmpty();
        result.MalformedExclusions.Should().BeEmpty();
        result.Spec.Should().NotBeNull();
        result.Spec!.Exclusions.Should().HaveCount(1);
        var marker = result.Spec.Exclusions.Single();
        marker.Target.Value.Should().Be("FR-102-01");
        marker.Justification.Should().Contain("external manual review");
    }

    [Fact]
    public void ParseFile_ExclusionWithShortJustification_IsMalformed()
    {
        var path = FixturePaths.Spec("sample-malformed-exclusion.md");

        var result = SpecParser.ParseFile(path);

        result.MalformedExclusions.Should().HaveCount(1);
        result.MalformedExclusions.Single().Reason
            .Should().Contain("justification shorter than 20 characters");
        result.Spec.Should().NotBeNull();
        result.Spec!.Exclusions.Should().BeEmpty();
    }

    [Fact]
    public void ParseFile_FrontMatterNeverCloses_ProducesParseError()
    {
        var path = FixturePaths.Spec("sample-malformed-frontmatter.md");

        var result = SpecParser.ParseFile(path);

        result.Spec.Should().BeNull();
        result.Errors.Should().HaveCount(1);
        result.Errors.Single().Reason.Should().Contain("does not close within 30 lines");
    }

    [Fact]
    public void ParseFile_DuplicateIdentifierInSameSpec_IsParseError()
    {
        var path = FixturePaths.Spec("sample-duplicate-id.md");

        var result = SpecParser.ParseFile(path);

        result.Errors.Should().ContainSingle(e => e.Reason.Contains("FR-104-01") && e.Reason.Contains("multiple times"));
        result.Spec.Should().NotBeNull("the parser still returns the deduplicated id list alongside the error");
        result.Spec!.RequirementIds.Should().ContainSingle(id => id.Value == "FR-104-01");
    }

    [Fact]
    public void ParseFile_NoFrontMatter_IsTreatedAsNonOptedIn()
    {
        var path = FixturePaths.Spec("sample-non-optedin.md");

        var result = SpecParser.ParseFile(path);

        result.Errors.Should().BeEmpty();
        result.Spec.Should().NotBeNull();
        result.Spec!.AccessSecurityOptIn.Should().BeFalse();
        result.Spec.RequirementIds.Select(id => id.Value)
            .Should().BeEquivalentTo(new[] { "FR-107-01", "SC-107-01" });
    }

    [Fact]
    public void ParseFile_FrontMatterWithoutAccessSecurityKey_IsNotOptedIn()
    {
        var temp = WriteTempSpec(
            "---",
            "owner: someone@example.test",
            "---",
            string.Empty,
            "# Title",
            string.Empty,
            "- **FR-108-01**: An identifier in a non-opted-in spec.");

        try
        {
            var result = SpecParser.ParseFile(temp);

            result.Errors.Should().BeEmpty();
            result.Spec.Should().NotBeNull();
            result.Spec!.AccessSecurityOptIn.Should().BeFalse();
            result.Spec.RequirementIds.Single().Value.Should().Be("FR-108-01");
        }
        finally
        {
            File.Delete(temp);
        }
    }

    [Fact]
    public void ParseFile_BodyWithoutAnyIdentifiers_ReturnsEmptyIdList()
    {
        var temp = WriteTempSpec(
            "---",
            "access-security: true",
            "---",
            string.Empty,
            "# Empty body",
            string.Empty,
            "Just paragraphs and bullets without any FR or SC tokens.",
            "- a bullet",
            "- another");

        try
        {
            var result = SpecParser.ParseFile(temp);

            result.Errors.Should().BeEmpty();
            result.Spec.Should().NotBeNull();
            result.Spec!.RequirementIds.Should().BeEmpty();
        }
        finally
        {
            File.Delete(temp);
        }
    }

    [Fact]
    [Trait("Spec", "FR-018-06")]
    public void ParseFile_PrefixedIdentifierFormat_IsRecognised()
    {
        var temp = WriteTempSpec(
            "---",
            "access-security: true",
            "---",
            string.Empty,
            "- **FR-016-01**: Prefixed feature/sub identifier per the FR-DDD-DD shape.");

        try
        {
            var result = SpecParser.ParseFile(temp);

            result.Spec.Should().NotBeNull();
            result.Spec!.RequirementIds.Should().ContainSingle(id => id.Value == "FR-016-01" && id.Kind == RequirementKind.Fr);
        }
        finally
        {
            File.Delete(temp);
        }
    }

    [Fact]
    [Trait("Spec", "FR-018-06")]
    public void ParseFile_UnprefixedIdentifierFormat_IsRecognised()
    {
        var temp = WriteTempSpec(
            "---",
            "access-security: true",
            "---",
            string.Empty,
            "- **FR-001**: Three-digit unprefixed FR identifier per the FR-DDD shape.",
            "- **SC-002**: And the matching SC-DDD shape.");

        try
        {
            var result = SpecParser.ParseFile(temp);

            result.Spec.Should().NotBeNull();
            result.Spec!.RequirementIds.Select(id => (id.Kind, id.Value))
                .Should().BeEquivalentTo(new[]
                {
                    (RequirementKind.Fr, "FR-001"),
                    (RequirementKind.Sc, "SC-002"),
                });
        }
        finally
        {
            File.Delete(temp);
        }
    }

    [Fact]
    public void ParseDirectory_NonExistentRoot_ReturnsEmpty()
    {
        var bogus = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        var results = SpecParser.ParseDirectory(bogus);

        results.Should().BeEmpty();
    }

    private static string WriteTempSpec(params string[] lines)
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + "-spec.md");
        File.WriteAllLines(path, lines);
        return path;
    }
}
