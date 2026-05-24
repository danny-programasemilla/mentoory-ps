namespace Mentoory.Web.Infrastructure;

/// <summary>
/// Resolves the decorative header-band theme slug for the current request from its route
/// values. Pure, stateless and side-effect free — evaluated once per render in
/// <c>_Layout.cshtml</c>. The returned slug becomes the <c>header-band--{slug}</c> CSS
/// modifier on the decorative band element, which CSS binds to a swappable SVG.
/// See <c>specs/021-themed-header-band/data-model.md</c> for the mapping table.
/// </summary>
public static class HeaderTheme
{
    /// <summary>The fallback theme. Always has a CSS rule and an SVG asset.</summary>
    public const string Default = "default";

    /// <summary>
    /// Controller name (case-insensitive) → theme slug. One slug may serve several
    /// related controllers; anything absent here resolves to <see cref="Default"/>.
    /// </summary>
    private static readonly Dictionary<string, string> ControllerToSlug =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Dashboard"] = "dashboard",
            ["Projects"] = "proyectos",
            ["Knowledge"] = "conocimiento",
            ["Templates"] = "conocimiento",
            ["Diagnostics"] = "diagnostico",
            ["Diagnostic"] = "diagnostico",
            ["AnswerCorrection"] = "diagnostico",
            ["Users"] = "personas",
            ["Sponsor"] = "personas",
            ["BatchUpload"] = "personas",
            ["Incubators"] = "incubadoras",
            ["AuditLog"] = "auditoria",
        };

    /// <summary>
    /// Returns a non-empty theme slug for the given route. Keyed on <paramref name="controller"/>;
    /// <paramref name="area"/> is part of the contract but not currently needed to disambiguate.
    /// Null, blank or unmapped controller → <see cref="Default"/>.
    /// </summary>
    public static string Resolve(string? area, string? controller)
    {
        if (!string.IsNullOrWhiteSpace(controller) &&
            ControllerToSlug.TryGetValue(controller, out var slug))
        {
            return slug;
        }

        return Default;
    }
}
