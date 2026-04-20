using System.Text.RegularExpressions;

namespace Mentoory.Tests.Integration.Fixtures;

/// <summary>
/// Shared helpers for ASP.NET Core antiforgery token handling in integration tests.
/// </summary>
internal static class AntiforgeryHelper
{
    private static readonly Regex TokenInput = new(
        "<input[^>]*name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Extracts an antiforgery token value from a rendered form. The hidden input shape is
    /// stable: <c>&lt;input ... name="__RequestVerificationToken" ... value="..." /&gt;</c>.
    /// </summary>
    public static string ExtractToken(string html)
    {
        var match = TokenInput.Match(html);
        if (!match.Success)
        {
            throw new InvalidOperationException(
                "Antiforgery token not found in form HTML. The page either did not render a token input " +
                "or the input shape changed.");
        }

        return match.Groups[1].Value;
    }
}
