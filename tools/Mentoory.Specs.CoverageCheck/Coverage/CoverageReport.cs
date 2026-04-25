using Mentoory.Specs.CoverageCheck.Parsing;
using Mentoory.Specs.CoverageCheck.Reflection;

namespace Mentoory.Specs.CoverageCheck.Coverage;

/// <summary>
/// Identifier (Kind+Value) declared in two or more distinct spec files.
/// </summary>
public sealed record DuplicateIdentifier(RequirementKind Kind, string Value, IReadOnlyList<string> SpecPaths);

/// <summary>
/// A spec opted in to access-security but missing at least one floor-category claim.
/// </summary>
public sealed record MissingFloor(FeatureSpec Spec, FloorCategory Category);

/// <summary>
/// Aggregated, deterministic output of a single tool run. Empty collections
/// indicate a green run; the program's exit code is derived from these.
/// </summary>
public sealed record CoverageReport(
    int ScannedSpecCount,
    int ScannedAssemblyCount,
    int ScannedTestMethodCount,
    int ScannedTraitClaimCount,
    IReadOnlyList<RequirementId> UnclaimedIds,
    IReadOnlyList<TestClaim> DanglingTraits,
    IReadOnlyList<ExclusionMarker> Exclusions,
    IReadOnlyList<MissingFloor> MissingFloorCategories,
    IReadOnlyList<DuplicateIdentifier> DuplicateIds,
    IReadOnlyList<MalformedExclusion> MalformedExclusions,
    IReadOnlyList<ReflectionError> ReflectionErrors)
{
    /// <summary>True when every violation collection is empty.</summary>
    public bool IsClean =>
        UnclaimedIds.Count == 0 &&
        DanglingTraits.Count == 0 &&
        MissingFloorCategories.Count == 0 &&
        DuplicateIds.Count == 0 &&
        MalformedExclusions.Count == 0 &&
        ReflectionErrors.Count == 0;
}
