using FluentAssertions;
using Mentoory.Specs.CoverageCheck.Reflection;
using Xunit;

namespace Mentoory.Specs.CoverageCheck.Tests.Reflection;

public class TraitReflectorTests
{
    [Fact]
    public void Inspect_SyntheticAssembly_EnumeratesEveryTestMethodAndItsTraits()
    {
        var path = FixturePaths.SampleXunitAssembly;
        File.Exists(path).Should().BeTrue($"the BuildFixtures target should produce {path}");

        var results = TraitReflector.Inspect(new[] { path });

        results.Should().HaveCount(1);
        var assembly = results[0].Assembly;
        results[0].Error.Should().BeNull();
        assembly.Should().NotBeNull();
        assembly!.TestMethods.Select(m => m.MethodName)
            .Should().BeEquivalentTo(new[]
            {
                "Claims_FR100_01",
                "Claims_SC100_01",
                "Dangling_FR999",
                "MultiClaim_FR100_01_And_FR100_02",
                "Other_TraitKey",
                "Quarantined_FR100_03",
                "Skipped_FR100_02",
            });

        var multi = assembly.TestMethods.Single(m => m.MethodName == "MultiClaim_FR100_01_And_FR100_02");
        multi.TraitClaims.Should().HaveCount(3);
        multi.TraitClaims.Where(t => t.Kind == TraitKind.Spec).Select(t => t.Value)
            .Should().BeEquivalentTo(new[] { "FR-100-01", "FR-100-02" });
        multi.TraitClaims.Should().ContainSingle(t => t.Kind == TraitKind.Floor && t.Value == "content-policy-rules");
    }

    [Fact]
    public void Inspect_SkippedFact_FlagsMethodAsSkippedAndNonClaiming()
    {
        var path = FixturePaths.SampleXunitAssembly;

        var assembly = TraitReflector.Inspect(new[] { path })[0].Assembly;

        var skipped = assembly!.TestMethods.Single(m => m.MethodName == "Skipped_FR100_02");
        skipped.IsSkipped.Should().BeTrue();
        skipped.IsNonClaiming.Should().BeTrue();
    }

    [Fact]
    public void Inspect_FlakyTrait_MarksMethodQuarantinedAndNonClaiming()
    {
        var path = FixturePaths.SampleXunitAssembly;

        var assembly = TraitReflector.Inspect(new[] { path })[0].Assembly;

        var quarantined = assembly!.TestMethods.Single(m => m.MethodName == "Quarantined_FR100_03");
        quarantined.IsQuarantined.Should().BeTrue();
        quarantined.IsSkipped.Should().BeFalse();
        quarantined.IsNonClaiming.Should().BeTrue();
    }

    [Fact]
    public void Inspect_UnknownTraitKey_IsCategorisedAsOther()
    {
        var path = FixturePaths.SampleXunitAssembly;

        var assembly = TraitReflector.Inspect(new[] { path })[0].Assembly;

        var other = assembly!.TestMethods.Single(m => m.MethodName == "Other_TraitKey");
        other.TraitClaims.Should().ContainSingle();
        other.TraitClaims.Single().Kind.Should().Be(TraitKind.Other);
        other.TraitClaims.Single().Key.Should().Be("Category");
        other.TraitClaims.Single().Value.Should().Be("smoke");
    }

    [Fact]
    public void Inspect_MissingAssemblyPath_ReportsReflectionError()
    {
        var bogus = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".dll");

        var results = TraitReflector.Inspect(new[] { bogus });

        results.Should().HaveCount(1);
        results[0].Assembly.Should().BeNull();
        results[0].Error.Should().NotBeNull();
        results[0].Error!.AssemblyPath.Should().Be(Path.GetFullPath(bogus));
        results[0].Error!.Reason.Should().Contain("not found");
    }

    [Fact]
    public void Inspect_EmptyInput_ReturnsEmptyResultList()
    {
        var results = TraitReflector.Inspect(Array.Empty<string>());

        results.Should().BeEmpty();
    }

    [Fact]
    public void Inspect_TraitWithUnusualValue_DoesNotCrashTheReflector()
    {
        var path = FixturePaths.SampleXunitAssembly;

        var assembly = TraitReflector.Inspect(new[] { path })[0].Assembly;

        // The dangling FR-999 carries a perfectly-valid string value but is not declared
        // in any spec. The reflector must surface it as a normal claim (the analyzer is
        // what classifies it as dangling later).
        var dangling = assembly!.TestMethods.Single(m => m.MethodName == "Dangling_FR999");
        dangling.TraitClaims.Should().ContainSingle(t => t.Kind == TraitKind.Spec && t.Value == "FR-999");
    }
}
