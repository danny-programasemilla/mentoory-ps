namespace Mentoory.Web.Infrastructure;

/// <summary>
/// Resolves the decorative page-banner icon slug for the current request from its MVC action
/// name. Pure, stateless and side-effect free — evaluated once per render in
/// <c>_Layout.cshtml</c>. The returned slug becomes the trailing <c>ti ti-{slug}</c> Tabler
/// webfont glyph on the banner strip (023-page-content-banner). It is a peer to
/// <see cref="HeaderTheme"/>: section colour and action icon are separate single-responsibility
/// resolvers. See <c>specs/023-page-content-banner/data-model.md</c> for the mapping table.
/// </summary>
public static class PageBannerIcon
{
    /// <summary>The fallback icon. A neutral, non-CRUD Tabler glyph that always renders.</summary>
    public const string Default = "layout-2";

    /// <summary>
    /// MVC action name (case-insensitive) → Tabler icon slug (without the <c>ti ti-</c> prefix).
    /// Anything absent here resolves to <see cref="Default"/>.
    /// </summary>
    private static readonly Dictionary<string, string> ActionToSlug =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Index"] = "list",
            ["Create"] = "plus",
            ["Edit"] = "edit",
            ["Details"] = "eye",
            ["Delete"] = "trash",
        };

    /// <summary>
    /// Returns a non-empty Tabler icon slug for the given action. Null, blank or unmapped
    /// action → <see cref="Default"/>.
    /// </summary>
    public static string Resolve(string? action)
    {
        if (!string.IsNullOrWhiteSpace(action) &&
            ActionToSlug.TryGetValue(action, out var slug))
        {
            return slug;
        }

        return Default;
    }
}
