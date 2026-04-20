using FluentAssertions;
using Mentoory.Specs.CoverageCheck.Coverage;
using Mentoory.Specs.CoverageCheck.Parsing;
using Mentoory.Specs.CoverageCheck.Reflection;
using Xunit;

namespace Mentoory.Specs.CoverageCheck.Tests.Coverage;

public class CoverageAnalyzerTests
{
    [Fact]
    public void Analyze_FullySatisfiedSpec_ProducesCleanReport()
    {
        var spec = SpecParser.ParseFile(FixturePaths.Spec("sample-clean.md")).Spec!;
        var assembly = LoadSampleAssembly();

        var report = CoverageAnalyzer.Analyze(
            new[] { spec },
            new[] { assembly },
            Array.Empty<MalformedExclusion>(),
            Array.Empty<ReflectionError>());

        // FR-100-03 is only "claimed" by a quarantined-flaky test, so it must surface as Unclaimed.
        report.UnclaimedIds.Should().ContainSingle(i => i.Value == "FR-100-03");
        // Every other declared identifier is claimed, so nothing else is unclaimed.
        report.UnclaimedIds.Should().HaveCount(1);
        // Floor enforcement (US3) is now active. The synthetic fixture only claims
        // `content-policy-rules`, so the other five floors are reported as missing
        // for this opted-in spec. Detailed per-category coverage is asserted by
        // `Analyze_OptedInSpec_MissingFloorCategories_AreReported` below.
        report.DuplicateIds.Should().BeEmpty();
        report.MalformedExclusions.Should().BeEmpty();
        report.ReflectionErrors.Should().BeEmpty();
    }

    [Fact]
    [Trait("Spec", "FR-018-07")]
    [Trait("Sc", "SC-018-06")]
    public void Analyze_UnclaimedSpec_PopulatesUnclaimedIds()
    {
        var spec = SpecParser.ParseFile(FixturePaths.Spec("sample-unclaimed.md")).Spec!;
        var assembly = LoadSampleAssembly();

        var report = CoverageAnalyzer.Analyze(
            new[] { spec },
            new[] { assembly },
            Array.Empty<MalformedExclusion>(),
            Array.Empty<ReflectionError>());

        report.UnclaimedIds.Should().Contain(i => i.Value == "FR-101-01");
    }

    [Fact]
    [Trait("Spec", "FR-018-07")]
    public void Analyze_DanglingTrait_IsReported()
    {
        var spec = SpecParser.ParseFile(FixturePaths.Spec("sample-dangling.md")).Spec!;
        var assembly = LoadSampleAssembly();

        var report = CoverageAnalyzer.Analyze(
            new[] { spec },
            new[] { assembly },
            Array.Empty<MalformedExclusion>(),
            Array.Empty<ReflectionError>());

        report.DanglingTraits.Should().Contain(c => c.Value == "FR-999");
    }

    [Fact]
    public void Analyze_ExclusionMarker_PassedThroughAndSuppressesUnclaimed()
    {
        var spec = SpecParser.ParseFile(FixturePaths.Spec("sample-excluded.md")).Spec!;
        var assembly = LoadSampleAssembly();

        var report = CoverageAnalyzer.Analyze(
            new[] { spec },
            new[] { assembly },
            Array.Empty<MalformedExclusion>(),
            Array.Empty<ReflectionError>());

        report.Exclusions.Should().ContainSingle(e => e.Target.Value == "FR-102-01");
        report.UnclaimedIds.Should().NotContain(i => i.Value == "FR-102-01");
    }

    [Fact]
    [Trait("Spec", "FR-018-08")]
    public void Analyze_OptedInSpec_MissingFloorCategories_AreReported()
    {
        var spec = SpecParser.ParseFile(FixturePaths.Spec("sample-clean.md")).Spec!;
        var assembly = LoadSampleAssembly();

        var report = CoverageAnalyzer.Analyze(
            new[] { spec },
            new[] { assembly },
            Array.Empty<MalformedExclusion>(),
            Array.Empty<ReflectionError>());

        // The synthetic fixture's MultiClaim_FR100_01_And_FR100_02 carries
        // [Trait("Floor", "content-policy-rules")] — that one is satisfied.
        // The other five floor categories have no claim → reported as missing for the opted-in spec.
        var missingNames = report.MissingFloorCategories
            .Where(m => m.Spec.Path == spec.Path)
            .Select(m => m.Category.Name)
            .ToHashSet();

        missingNames.Should().NotContain("content-policy-rules");
        missingNames.Should().Contain("response-indistinguishability");
        missingNames.Should().Contain("outcome-audit-logging");
        missingNames.Should().Contain("public-vs-admin-attribution");
        missingNames.Should().Contain("form-state-preservation");
        missingNames.Should().Contain("defense-in-depth-controls");
    }

    [Fact]
    [Trait("Spec", "FR-018-08")]
    public void Analyze_NonOptedInSpec_FloorEnforcementIsSkipped()
    {
        var spec = SpecParser.ParseFile(FixturePaths.Spec("sample-non-optedin.md")).Spec!;
        var assembly = LoadSampleAssembly();

        var report = CoverageAnalyzer.Analyze(
            new[] { spec },
            new[] { assembly },
            Array.Empty<MalformedExclusion>(),
            Array.Empty<ReflectionError>());

        report.MissingFloorCategories.Should().BeEmpty();
    }

    [Fact]
    public void Analyze_SameIdentifierAcrossTwoSpecs_IsReportedAsDuplicate()
    {
        var specA = SpecParser.ParseFile(FixturePaths.Spec("sample-clean.md")).Spec!;
        // Synthesize a second spec that re-declares FR-100-01 to trigger the cross-spec
        // duplicate path inside the analyzer.
        var clashing = new FeatureSpec(
            Path: "/synthetic/other-spec.md",
            FeatureNumber: "199",
            Slug: "synthetic-clashing",
            AccessSecurityOptIn: true,
            RequirementIds: new[]
            {
                new RequirementId(RequirementKind.Fr, "FR-100-01", "/synthetic/other-spec.md", 5),
            },
            Exclusions: Array.Empty<ExclusionMarker>());

        var report = CoverageAnalyzer.Analyze(
            new[] { specA, clashing },
            new[] { LoadSampleAssembly() },
            Array.Empty<MalformedExclusion>(),
            Array.Empty<ReflectionError>());

        report.DuplicateIds.Should().ContainSingle(d => d.Value == "FR-100-01");
        report.DuplicateIds.Single().SpecPaths.Should().HaveCount(2);
    }

    [Fact]
    public void Analyze_MalformedExclusionsFromParser_ThreadedThrough()
    {
        var spec = SpecParser.ParseFile(FixturePaths.Spec("sample-clean.md")).Spec!;
        var malformed = new[] { new MalformedExclusion("/some/spec.md", 42, "justification shorter than 20 characters (got 9)") };

        var report = CoverageAnalyzer.Analyze(
            new[] { spec },
            new[] { LoadSampleAssembly() },
            malformed,
            Array.Empty<ReflectionError>());

        report.MalformedExclusions.Should().BeEquivalentTo(malformed);
    }

    [Fact]
    public void Analyze_ReflectionErrorsFromInspector_ThreadedThrough()
    {
        var spec = SpecParser.ParseFile(FixturePaths.Spec("sample-clean.md")).Spec!;
        var errors = new[] { new ReflectionError("/missing/asm.dll", "Assembly file not found.") };

        var report = CoverageAnalyzer.Analyze(
            new[] { spec },
            new[] { LoadSampleAssembly() },
            Array.Empty<MalformedExclusion>(),
            errors);

        report.ReflectionErrors.Should().BeEquivalentTo(errors);
    }

    /// <summary>
    /// SC-005 canary: starting from a green-for-FR-100-01 baseline, removing the
    /// claiming test must cause that identifier to surface in <c>UnclaimedIds</c>.
    /// </summary>
    [Fact]
    [Trait("Sc", "SC-018-01")]
    [Trait("Sc", "SC-018-05")]
    public void Analyze_RemovingTheOnlyClaimForAnIdentifier_SurfacesItAsUnclaimed_Sc005Canary()
    {
        var spec = SpecParser.ParseFile(FixturePaths.Spec("sample-clean.md")).Spec!;
        var realAssembly = LoadSampleAssembly();

        // Baseline: FR-100-01 is claimed twice (Claims_FR100_01 and the multi-claim method).
        var baselineReport = CoverageAnalyzer.Analyze(
            new[] { spec },
            new[] { realAssembly },
            Array.Empty<MalformedExclusion>(),
            Array.Empty<ReflectionError>());
        baselineReport.UnclaimedIds.Should().NotContain(i => i.Value == "FR-100-01");

        // Canary: drop every method that claims FR-100-01 and re-run the analyzer.
        var stripped = StripClaimsFor(realAssembly, "FR-100-01");

        var stripepdReport = CoverageAnalyzer.Analyze(
            new[] { spec },
            new[] { stripped },
            Array.Empty<MalformedExclusion>(),
            Array.Empty<ReflectionError>());

        // EXACT ASSERTION: FR-100-01 must now appear once in UnclaimedIds, with kind = Fr,
        // sourced from sample-clean.md.
        stripepdReport.UnclaimedIds.Should().ContainSingle(i =>
            i.Kind == RequirementKind.Fr &&
            i.Value == "FR-100-01" &&
            i.SpecPath.EndsWith("sample-clean.md", StringComparison.Ordinal));
    }

    [Fact]
    public void Analyze_QuarantinedFlakyMethod_DoesNotClaimItsTraits_Nfr002()
    {
        var spec = SpecParser.ParseFile(FixturePaths.Spec("sample-clean.md")).Spec!;
        var assembly = LoadSampleAssembly();

        var report = CoverageAnalyzer.Analyze(
            new[] { spec },
            new[] { assembly },
            Array.Empty<MalformedExclusion>(),
            Array.Empty<ReflectionError>());

        // FR-100-03 is only carried by Quarantined_FR100_03 (Flaky=true). It must show as Unclaimed.
        report.UnclaimedIds.Should().Contain(i => i.Value == "FR-100-03");

        // And the FR-999 trait on a normal (non-quarantined) test should still be dangling — proving
        // dangling detection only applies to claiming methods.
        var dangling = LoadSampleAssembly();
        var danglingSpec = SpecParser.ParseFile(FixturePaths.Spec("sample-dangling.md")).Spec!;
        var danglingReport = CoverageAnalyzer.Analyze(
            new[] { danglingSpec },
            new[] { dangling },
            Array.Empty<MalformedExclusion>(),
            Array.Empty<ReflectionError>());
        danglingReport.DanglingTraits.Should().Contain(c => c.Value == "FR-999");
    }

    [Fact]
    public void Analyze_MultiTraitMethod_CountsEachIdentifierExactlyOnce()
    {
        var spec = SpecParser.ParseFile(FixturePaths.Spec("sample-clean.md")).Spec!;
        var assembly = LoadSampleAssembly();

        var report = CoverageAnalyzer.Analyze(
            new[] { spec },
            new[] { assembly },
            Array.Empty<MalformedExclusion>(),
            Array.Empty<ReflectionError>());

        // FR-100-01 is claimed by two methods (Claims_FR100_01 + the multi-claim method) but should
        // only appear once in any "claimed" projection — surface that by asserting it is NOT in
        // Unclaimed and NOT in DanglingTraits, even though the underlying claim count > 1.
        report.UnclaimedIds.Should().NotContain(i => i.Value == "FR-100-01");
        report.DanglingTraits.Should().NotContain(c => c.Value == "FR-100-01");

        // The multi-trait method also claims FR-100-02 — no other claiming test does (the other
        // test on FR-100-02 is skipped). So FR-100-02 must also be claimed exactly once.
        report.UnclaimedIds.Should().NotContain(i => i.Value == "FR-100-02");
    }

    [Fact]
    public void Analyze_NoSpecsNoAssemblies_IsCleanAndScanCountsAreZero()
    {
        var report = CoverageAnalyzer.Analyze(
            Array.Empty<FeatureSpec>(),
            Array.Empty<TestAssembly>(),
            Array.Empty<MalformedExclusion>(),
            Array.Empty<ReflectionError>());

        report.IsClean.Should().BeTrue();
        report.ScannedSpecCount.Should().Be(0);
        report.ScannedAssemblyCount.Should().Be(0);
        report.ScannedTestMethodCount.Should().Be(0);
        report.ScannedTraitClaimCount.Should().Be(0);
    }

    private static TestAssembly LoadSampleAssembly()
    {
        var path = FixturePaths.SampleXunitAssembly;
        File.Exists(path).Should().BeTrue($"the BuildFixtures target should produce {path}");
        var result = TraitReflector.Inspect(new[] { path }).Single();
        result.Error.Should().BeNull();
        return result.Assembly!;
    }

    /// <summary>
    /// Returns a <see cref="TestAssembly"/> equivalent to <paramref name="assembly"/> but with
    /// every method that carries a <c>Spec=</c><paramref name="value"/> trait removed entirely —
    /// the SC-005 canary's "delete the only claimant" gesture.
    /// </summary>
    private static TestAssembly StripClaimsFor(TestAssembly assembly, string value)
    {
        var kept = assembly.TestMethods
            .Where(m => !m.TraitClaims.Any(c => c.Kind == TraitKind.Spec && string.Equals(c.Value, value, StringComparison.Ordinal)))
            .ToArray();
        return new TestAssembly(assembly.Path, assembly.AssemblyName, kept);
    }
}
