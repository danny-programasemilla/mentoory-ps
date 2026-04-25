namespace Mentoory.Specs.CoverageCheck.Parsing;

/// <summary>
/// Discriminator for requirement-identifier kinds parsed from a spec.
/// </summary>
public enum RequirementKind
{
    /// <summary>Functional Requirement (FR-###).</summary>
    Fr,

    /// <summary>Success Criterion (SC-###).</summary>
    Sc,
}

/// <summary>
/// A single FR-### or SC-### token declared in a spec. Equality is by (Kind, Value)
/// only — SpecPath and LineNumber are metadata, not identity.
/// </summary>
public sealed record RequirementId(RequirementKind Kind, string Value, string SpecPath, int LineNumber)
{
    /// <inheritdoc/>
    public bool Equals(RequirementId? other)
    {
        if (other is null)
        {
            return false;
        }

        return Kind == other.Kind && string.Equals(Value, other.Value, StringComparison.Ordinal);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(Kind, Value);
    }
}

/// <summary>
/// An identifier deliberately marked <c>Coverage: N/A</c> in a spec.
/// </summary>
public sealed record ExclusionMarker(RequirementId Target, string Justification, int LineNumber);

/// <summary>
/// A single <c>spec.md</c> file under the <c>--specs-root</c> tree.
/// </summary>
public sealed record FeatureSpec(
    string Path,
    string FeatureNumber,
    string Slug,
    bool AccessSecurityOptIn,
    IReadOnlyList<RequirementId> RequirementIds,
    IReadOnlyList<ExclusionMarker> Exclusions);
