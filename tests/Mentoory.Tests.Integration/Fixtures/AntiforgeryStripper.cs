using System.Text.RegularExpressions;

namespace Mentoory.Tests.Integration.Fixtures;

/// <summary>
/// Deterministically removes ASP.NET Core antiforgery entropy from HTML bodies and
/// <c>Set-Cookie</c> header values so that two responses can be byte-compared by the
/// FR-018-19 50-probe sweep without the inherently-random token values causing false
/// negatives.
/// <para>
/// Per spec 018 research.md #7:
/// </para>
/// <list type="bullet">
/// <item>The hidden input shape is stable
/// (<c>&lt;input ... name="__RequestVerificationToken" ... value="..." /&gt;</c>) — the
/// whole element is removed, not just the value attribute.</item>
/// <item>The antiforgery cookie name is <c>.AspNetCore.Antiforgery.&lt;random&gt;</c>;
/// cookie *names* are kept (so the comparison still notices a name-shape change), but
/// cookie *values* are blanked.</item>
/// </list>
/// </summary>
internal static class AntiforgeryStripper
{
    /// <summary>
    /// Matches the entire hidden antiforgery input element. Tolerates attribute order
    /// and intra-tag whitespace, then consumes the closing <c>/&gt;</c> or <c>&gt;</c>.
    /// </summary>
    private static readonly Regex HiddenInput = new(
        "<input[^>]*name=\"__RequestVerificationToken\"[^>]*/?>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Matches a <c>__RequestVerificationToken=&lt;value&gt;</c> cookie segment plus its
    /// trailing <c>"; "</c> separator. Handles both forms (<c>...; name=value; ...</c> and
    /// trailing <c>...; name=value</c>).
    /// </summary>
    private static readonly Regex RequestVerificationCookieSegment = new(
        @"__RequestVerificationToken=[^;]*(?:;\s*)?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Matches a <c>.AspNetCore.Antiforgery.&lt;random&gt;=&lt;value&gt;</c> cookie pair
    /// and captures the cookie name (group 1). The value is replaced; the name is kept
    /// so a name-shape change is still observable to the comparison.
    /// </summary>
    private static readonly Regex AspNetAntiforgeryCookie = new(
        @"(\.AspNetCore\.Antiforgery\.[^=]+)=[^;]*",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Removes the entire hidden antiforgery <c>&lt;input&gt;</c> element from a
    /// rendered HTML body so that the surrounding markup can be byte-compared.
    /// </summary>
    public static string StripFromHtml(string html)
    {
        if (string.IsNullOrEmpty(html))
        {
            return html;
        }

        return HiddenInput.Replace(html, string.Empty);
    }

    /// <summary>
    /// Returns the input <c>Set-Cookie</c> headers with antiforgery entropy removed:
    /// the <c>__RequestVerificationToken</c> segment is dropped entirely, and any
    /// <c>.AspNetCore.Antiforgery.&lt;random&gt;</c> cookie has its value blanked
    /// while preserving the cookie name. Headers without antiforgery content pass
    /// through untouched.
    /// </summary>
    public static IEnumerable<string> StripCookieHeader(IEnumerable<string> setCookieHeaders)
    {
        if (setCookieHeaders is null)
        {
            yield break;
        }

        foreach (var header in setCookieHeaders)
        {
            if (string.IsNullOrEmpty(header))
            {
                yield return header;
                continue;
            }

            var stripped = RequestVerificationCookieSegment.Replace(header, string.Empty);
            stripped = AspNetAntiforgeryCookie.Replace(stripped, "$1=");

            yield return stripped;
        }
    }
}
