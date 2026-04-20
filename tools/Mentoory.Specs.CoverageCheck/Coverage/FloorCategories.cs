namespace Mentoory.Specs.CoverageCheck.Coverage;

/// <summary>
/// A canonical floor category as defined in the access-security constitution
/// (Section 11.9–11.14). The six names are stable for the life of feature 018.
/// </summary>
public sealed record FloorCategory(string Name, string TriggerDescription);

/// <summary>
/// The hard-coded six canonical floor categories. Mirrors the constitution
/// amendment; drift is caught manually during amendment review.
/// </summary>
public static class FloorCategories
{
    /// <summary>The single source of truth for canonical floor-category names.</summary>
    public static readonly IReadOnlyList<FloorCategory> All = new[]
    {
        new FloorCategory(
            "response-indistinguishability",
            "Public endpoint with masked outcome — proves equal HTTP shape across success and failure."),
        new FloorCategory(
            "outcome-audit-logging",
            "Outcome audit logging — proves every outcome (success, failure, conflict) is logged with the correct code."),
        new FloorCategory(
            "public-vs-admin-attribution",
            "Dual public/admin surface — proves admin path attributes errors and public path stays generic."),
        new FloorCategory(
            "form-state-preservation",
            "Form-state render after failure — proves non-secret fields repopulate and secret fields blank."),
        new FloorCategory(
            "defense-in-depth-controls",
            "Antiforgery / rate-limit controls — proves middleware blocks malformed and abusive traffic."),
        new FloorCategory(
            "content-policy-rules",
            "Content-policy rules (e.g., password identity check) — proves rule rejects payload-bearing failures."),
    };

    /// <summary>Set of canonical floor names for fast membership tests.</summary>
    public static readonly IReadOnlySet<string> Names =
        new HashSet<string>(All.Select(c => c.Name), StringComparer.Ordinal);

    /// <summary>Lookup a category by its canonical kebab-case name.</summary>
    public static FloorCategory? FindByName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        foreach (var category in All)
        {
            if (string.Equals(category.Name, name, StringComparison.Ordinal))
            {
                return category;
            }
        }

        return null;
    }
}
