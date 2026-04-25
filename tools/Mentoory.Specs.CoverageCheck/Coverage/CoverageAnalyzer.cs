using Mentoory.Specs.CoverageCheck.Parsing;
using Mentoory.Specs.CoverageCheck.Reflection;

namespace Mentoory.Specs.CoverageCheck.Coverage;

/// <summary>
/// Joins parsed specs and reflected test assemblies into a single
/// <see cref="CoverageReport"/>. Pure: no side effects, deterministic ordering.
/// </summary>
public static class CoverageAnalyzer
{
    /// <summary>
    /// Produces the coverage report for a complete tool run.
    /// </summary>
    public static CoverageReport Analyze(
        IReadOnlyList<FeatureSpec> specs,
        IReadOnlyList<TestAssembly> assemblies,
        IReadOnlyList<MalformedExclusion> malformedExclusions,
        IReadOnlyList<ReflectionError> reflectionErrors)
    {
        ArgumentNullException.ThrowIfNull(specs);
        ArgumentNullException.ThrowIfNull(assemblies);
        ArgumentNullException.ThrowIfNull(malformedExclusions);
        ArgumentNullException.ThrowIfNull(reflectionErrors);

        var allMethods = assemblies.SelectMany(a => a.TestMethods).ToArray();
        var allClaims = allMethods.SelectMany(m => m.TraitClaims).ToArray();
        var claimingMethods = allMethods.Where(m => !m.IsNonClaiming).ToArray();

        var claimedSpecValues = ClaimedValues(claimingMethods, TraitKind.Spec);
        var claimedScValues = ClaimedValues(claimingMethods, TraitKind.Sc);

        // Traceability and duplicate-detection are scoped to opted-in specs per
        // constitution Section 13.1. Non-opted-in specs are still parsed and their
        // identifiers are accepted as legitimate claim targets, but they are not
        // required to be claimed and ID collisions among purely-non-opted-in specs
        // are tolerated (those specs have not adopted the prefixed-ID convention yet).
        var optedInSpecs = specs.Where(s => s.AccessSecurityOptIn).ToArray();
        var optedInIds = optedInSpecs.SelectMany(s => s.RequirementIds).ToArray();
        var allIds = specs.SelectMany(s => s.RequirementIds).ToArray();
        var excludedKeys = optedInSpecs
            .SelectMany(s => s.Exclusions)
            .Select(e => (e.Target.Kind, e.Target.Value))
            .ToHashSet();

        var unclaimed = new List<RequirementId>();
        foreach (var id in optedInIds)
        {
            if (excludedKeys.Contains((id.Kind, id.Value)))
            {
                continue;
            }

            var claimed = id.Kind == RequirementKind.Fr
                ? claimedSpecValues.Contains(id.Value)
                : claimedScValues.Contains(id.Value);
            if (!claimed)
            {
                unclaimed.Add(id);
            }
        }

        // Group identifiers by (Kind, Value) to detect cross-spec collisions.
        // A collision is reported only when at least one of the colliding specs is
        // opted-in; that's where the namespace ambiguity actually breaks claim mapping.
        var duplicates = new List<DuplicateIdentifier>();
        var byKey = allIds.GroupBy(id => (id.Kind, id.Value));
        foreach (var group in byKey)
        {
            var distinctSpecs = group
                .Select(id => id.SpecPath)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(p => p, StringComparer.Ordinal)
                .ToArray();
            if (distinctSpecs.Length <= 1)
            {
                continue;
            }

            var anyOptedIn = group.Any(id => specs.First(s => s.Path == id.SpecPath).AccessSecurityOptIn);
            if (!anyOptedIn)
            {
                continue;
            }

            duplicates.Add(new DuplicateIdentifier(group.Key.Kind, group.Key.Value, distinctSpecs));
        }

        // Dangling traits resolve against ALL declared identifiers (not just opted-in),
        // so a test that claims a non-opted-in spec's ID is still valid. This allows
        // organic adoption of the gate without forcing all-at-once retrofit.
        var declaredFr = allIds.Where(i => i.Kind == RequirementKind.Fr).Select(i => i.Value).ToHashSet(StringComparer.Ordinal);
        var declaredSc = allIds.Where(i => i.Kind == RequirementKind.Sc).Select(i => i.Value).ToHashSet(StringComparer.Ordinal);

        var dangling = new List<TestClaim>();
        foreach (var claim in allClaims)
        {
            if (claim.Method.IsNonClaiming)
            {
                continue;
            }

            if (claim.Kind == TraitKind.Spec && !declaredFr.Contains(claim.Value))
            {
                dangling.Add(claim);
            }
            else if (claim.Kind == TraitKind.Sc && !declaredSc.Contains(claim.Value))
            {
                dangling.Add(claim);
            }
        }

        // Floor enforcement is the US3 deliverable. Today: keep the slot, return an empty list.
        // The hook is in place so US3 only needs to populate the collection.
        var missingFloor = ComputeMissingFloors(specs, claimingMethods);

        var allExclusions = optedInSpecs.SelectMany(s => s.Exclusions).ToArray();

        var sortedUnclaimed = unclaimed
            .OrderBy(i => i.Kind)
            .ThenBy(i => i.Value, StringComparer.Ordinal)
            .ThenBy(i => i.SpecPath, StringComparer.Ordinal)
            .ThenBy(i => i.LineNumber)
            .ToArray();

        var sortedDangling = dangling
            .OrderBy(c => c.Kind)
            .ThenBy(c => c.Value, StringComparer.Ordinal)
            .ThenBy(c => c.Method.FullyQualifiedName, StringComparer.Ordinal)
            .ToArray();

        var sortedExclusions = allExclusions
            .OrderBy(e => e.Target.Kind)
            .ThenBy(e => e.Target.Value, StringComparer.Ordinal)
            .ThenBy(e => e.Target.SpecPath, StringComparer.Ordinal)
            .ToArray();

        var sortedDuplicates = duplicates
            .OrderBy(d => d.Kind)
            .ThenBy(d => d.Value, StringComparer.Ordinal)
            .ToArray();

        var sortedMalformed = malformedExclusions
            .OrderBy(m => m.SpecPath, StringComparer.Ordinal)
            .ThenBy(m => m.LineNumber)
            .ToArray();

        var sortedReflectionErrors = reflectionErrors
            .OrderBy(r => r.AssemblyPath, StringComparer.Ordinal)
            .ToArray();

        var sortedMissingFloors = missingFloor
            .OrderBy(m => m.Spec.Path, StringComparer.Ordinal)
            .ThenBy(m => m.Category.Name, StringComparer.Ordinal)
            .ToArray();

        return new CoverageReport(
            ScannedSpecCount: specs.Count,
            ScannedAssemblyCount: assemblies.Count,
            ScannedTestMethodCount: allMethods.Length,
            ScannedTraitClaimCount: allClaims.Length,
            UnclaimedIds: sortedUnclaimed,
            DanglingTraits: sortedDangling,
            Exclusions: sortedExclusions,
            MissingFloorCategories: sortedMissingFloors,
            DuplicateIds: sortedDuplicates,
            MalformedExclusions: sortedMalformed,
            ReflectionErrors: sortedReflectionErrors);
    }

    private static HashSet<string> ClaimedValues(IEnumerable<TestMethodMetadata> methods, TraitKind kind)
    {
        return methods
            .SelectMany(m => m.TraitClaims)
            .Where(c => c.Kind == kind)
            .Select(c => c.Value)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static IReadOnlyList<MissingFloor> ComputeMissingFloors(
        IReadOnlyList<FeatureSpec> specs,
        IReadOnlyList<TestMethodMetadata> claimingMethods)
    {
        // For each opted-in spec, every canonical floor category MUST have at
        // least one non-skipped, non-quarantined test carrying the matching
        // [Trait("Floor", <name>)]. The applicability set is "all six categories
        // apply" per data-model.md — per-feature narrowing is a future enhancement
        // (would require associating tests to features, which is currently global).
        var optedInSpecs = specs.Where(s => s.AccessSecurityOptIn).ToArray();
        if (optedInSpecs.Length == 0)
        {
            return Array.Empty<MissingFloor>();
        }

        var claimedFloors = claimingMethods
            .SelectMany(m => m.TraitClaims)
            .Where(c => c.Kind == TraitKind.Floor)
            .Select(c => c.Value)
            .ToHashSet(StringComparer.Ordinal);

        var missing = new List<MissingFloor>();
        foreach (var spec in optedInSpecs)
        {
            foreach (var category in FloorCategories.All)
            {
                if (!claimedFloors.Contains(category.Name))
                {
                    missing.Add(new MissingFloor(spec, category));
                }
            }
        }

        return missing;
    }
}
