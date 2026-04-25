using Mentoory.Specs.CoverageCheck.Coverage;
using Mentoory.Specs.CoverageCheck.Parsing;
using Mentoory.Specs.CoverageCheck.Reflection;

namespace Mentoory.Specs.CoverageCheck.Tests.Output;

/// <summary>
/// Builds the deterministic <see cref="CoverageReport"/> used by both golden-file
/// tests (text and JSON). One synthetic entry per report section so every output
/// branch is exercised in a single byte-identical render.
/// </summary>
internal static class GoldenFixture
{
    public const string ToolVersion = "1.0.0";
    public const double ElapsedSeconds = 0.123;
    public const int ExitCode = 1;

    public const string CleanSpecPath = "specs/100-sample/spec.md";
    public const string OtherSpecPath = "specs/199-other/spec.md";
    public const string AssemblyPath = "tests/Sample.Tests/bin/Debug/net10.0/Sample.Tests.dll";

    public static CoverageReport BuildReport()
    {
        var unclaimedId = new RequirementId(RequirementKind.Fr, "FR-100-99", CleanSpecPath, 110);
        var excludedId = new RequirementId(RequirementKind.Fr, "FR-100-50", CleanSpecPath, 118);
        var duplicateA = new RequirementId(RequirementKind.Fr, "FR-100-01", CleanSpecPath, 42);
        var duplicateB = new RequirementId(RequirementKind.Fr, "FR-100-01", OtherSpecPath, 108);

        var spec = new FeatureSpec(
            Path: CleanSpecPath,
            FeatureNumber: "100",
            Slug: "sample",
            AccessSecurityOptIn: true,
            RequirementIds: new[] { unclaimedId, excludedId, duplicateA },
            Exclusions: new[] { new ExclusionMarker(excludedId, "covered exclusively by an external manual review process today", 119) });

        var floorCategory = FloorCategories.FindByName("form-state-preservation")!;

        var method = new TestMethodMetadata(
            DeclaringTypeFullName: "Mentoory.Sample.Tests.SomeTestClass",
            MethodName: "DanglingTest",
            IsSkipped: false);
        var danglingClaim = new TestClaim(TraitKind.Spec, "Spec", "FR-999", method);
        method.TraitClaims.Add(danglingClaim);

        return new CoverageReport(
            ScannedSpecCount: 1,
            ScannedAssemblyCount: 1,
            ScannedTestMethodCount: 1,
            ScannedTraitClaimCount: 1,
            UnclaimedIds: new[] { unclaimedId },
            DanglingTraits: new[] { danglingClaim },
            Exclusions: new[] { new ExclusionMarker(excludedId, "covered exclusively by an external manual review process today", 119) },
            MissingFloorCategories: new[] { new MissingFloor(spec, floorCategory) },
            DuplicateIds: new[] { new DuplicateIdentifier(RequirementKind.Fr, "FR-100-01", new[] { CleanSpecPath, OtherSpecPath }) },
            MalformedExclusions: new[] { new MalformedExclusion(CleanSpecPath, 93, "justification shorter than 20 characters (got 9)") },
            ReflectionErrors: new[] { new ReflectionError(AssemblyPath, "Could not resolve 'Some.Dependency' via configured PathAssemblyResolver") });
    }
}
