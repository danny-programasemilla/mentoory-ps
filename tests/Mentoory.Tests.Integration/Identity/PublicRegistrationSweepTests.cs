using System.Diagnostics;
using System.Net;
using FluentAssertions;
using Mentoory.Tests.Integration.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Mentoory.Tests.Integration.Identity;

[Collection(IntegrationTestCollection.Name)]
public class PublicRegistrationSweepTests : IntegrationTestBase
{
    // 50 probes is a CI-affordable canary sized for <= 15 s wall time (NFR-004).
    // It is NOT a statistical proof of indistinguishability — see spec 018
    // research.md #13 for the rationale and Out-of-Scope on parameterisation.
    private const int ProbeCount = 50;

    private const string DuplicateEmail = "sweep-dup-email@example.com";
    private const string DuplicateNidSeedEmail = "sweep-dup-nid-source@example.com";
    private const string DuplicateNationalId = "888777666";
    private const string Country = "CO";
    private const string Password = "SecureP@ss12345!";

    public PublicRegistrationSweepTests(MentooryWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    [Trait("Spec", "FR-018-19")]
    [Trait("Sc", "SC-018-04")]
    [Trait("Floor", "response-indistinguishability")]
    public async Task PublicRegistration_FiftyProbeSweep_ResponsesAreIndistinguishable()
    {
        // Pre-seed deterministic conflict targets so the probe loop can reliably
        // exercise the duplicate-email and duplicate-NID code paths.
        (await RegisterUserAsync(email: DuplicateEmail, nationalId: "999000111"))
            .IsSuccess.Should().BeTrue("seed for duplicate-email probes must succeed");
        (await RegisterUserAsync(email: DuplicateNidSeedEmail, nationalId: DuplicateNationalId))
            .IsSuccess.Should().BeTrue("seed for duplicate-NID probes must succeed");

        var client = Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var probes = new List<ProbeResult>(ProbeCount);
        var stopwatch = Stopwatch.StartNew();

        for (var i = 0; i < ProbeCount; i++)
        {
            var (email, nationalId) = BuildProbeIdentifiers(i);
            probes.Add(await ExecuteProbeAsync(client, email, nationalId));
        }

        stopwatch.Stop();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(15000,
            "NFR-004 caps the FR-018-19 sweep at 15 s wall time so it is affordable on every PR");

        var baseline = probes[0];
        for (var i = 1; i < probes.Count; i++)
        {
            var probe = probes[i];

            probe.StatusCode.Should().Be(baseline.StatusCode, $"probe {i} status differs");
            probe.Location.Should().Be(baseline.Location, $"probe {i} Location header differs");
            probe.CacheControl.Should().Be(baseline.CacheControl, $"probe {i} Cache-Control differs");
            probe.ContentType.Should().Be(baseline.ContentType, $"probe {i} Content-Type differs");
            probe.SortedSetCookieNames.Should().BeEquivalentTo(baseline.SortedSetCookieNames,
                $"probe {i} Set-Cookie name set differs");
            probe.StrippedBody.Should().Be(baseline.StrippedBody,
                BuildBodyMismatchMessage(i, baseline.StrippedBody, probe.StrippedBody));
        }
    }

    private static (string Email, string NationalId) BuildProbeIdentifiers(int index)
    {
        return (index % 3) switch
        {
            // Fresh probe: unique email + unique NID. Should succeed at the
            // application layer and short-circuit through the same redirect.
            0 => ($"sweep-fresh-{index}@example.com", $"700{index:D6}"),

            // Duplicate-email probe: collides with the pre-seeded duplicate
            // email; NID stays unique so the conflict is unambiguously the email.
            1 => (DuplicateEmail, $"701{index:D6}"),

            // Duplicate-NID probe: fresh email, pre-seeded NID. Mirror image of
            // the duplicate-email case so both conflict paths are sampled.
            _ => ($"sweep-fresh-{index}@example.com", DuplicateNationalId),
        };
    }

    private static async Task<ProbeResult> ExecuteProbeAsync(HttpClient client, string email, string nationalId)
    {
        var getResponse = await client.GetAsync("/Access/Register");
        getResponse.EnsureSuccessStatusCode();
        var getHtml = await getResponse.Content.ReadAsStringAsync();
        var antiforgeryToken = AntiforgeryHelper.ExtractToken(getHtml);

        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("__RequestVerificationToken", antiforgeryToken),
            new KeyValuePair<string, string>("Email", email),
            new KeyValuePair<string, string>("FirstName", "Sweep"),
            new KeyValuePair<string, string>("LastName", "Probe"),
            new KeyValuePair<string, string>("Country", Country),
            new KeyValuePair<string, string>("NationalId", nationalId),
            new KeyValuePair<string, string>("Password", Password),
            new KeyValuePair<string, string>("ConfirmPassword", Password),
        });

        var postResponse = await client.PostAsync("/Access/Register", form);

        var statusCode = postResponse.StatusCode;
        var location = postResponse.Headers.Location?.ToString() ?? string.Empty;
        var cacheControl = postResponse.Headers.CacheControl?.ToString() ?? string.Empty;
        var contentType = postResponse.Content.Headers.ContentType?.ToString() ?? string.Empty;

        var rawSetCookies = postResponse.Headers.TryGetValues("Set-Cookie", out var cookies)
            ? cookies.ToList()
            : new List<string>();

        // Sort cookie *names* only — values vary per request (session ids,
        // antiforgery cookie suffix entropy) and would defeat the comparison.
        var sortedCookieNames = rawSetCookies
            .Select(ExtractCookieName)
            .Where(n => !string.IsNullOrEmpty(n))
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        // Capture the post-redirect body so the comparison covers both the
        // immediate POST response headers and the rendered destination page.
        string strippedBody;
        if (postResponse.StatusCode == HttpStatusCode.Redirect ||
            postResponse.StatusCode == HttpStatusCode.Found ||
            postResponse.StatusCode == HttpStatusCode.SeeOther)
        {
            var redirectTarget = postResponse.Headers.Location;
            if (redirectTarget is null)
            {
                strippedBody = string.Empty;
            }
            else
            {
                var followUp = await client.GetAsync(redirectTarget);
                var followBody = await followUp.Content.ReadAsStringAsync();
                strippedBody = AntiforgeryStripper.StripFromHtml(followBody);
            }
        }
        else
        {
            var directBody = await postResponse.Content.ReadAsStringAsync();
            strippedBody = AntiforgeryStripper.StripFromHtml(directBody);
        }

        return new ProbeResult(
            statusCode,
            location,
            strippedBody,
            cacheControl,
            contentType,
            sortedCookieNames);
    }

    private static string ExtractCookieName(string setCookieHeader)
    {
        if (string.IsNullOrEmpty(setCookieHeader))
        {
            return string.Empty;
        }

        var equalsIndex = setCookieHeader.IndexOf('=');
        return equalsIndex < 0 ? setCookieHeader.Trim() : setCookieHeader[..equalsIndex].Trim();
    }

    /// <summary>
    /// Builds an EC-007 diff message: probe index plus the first 500 differing
    /// characters of the bodies, so a CI failure points directly at the drift.
    /// </summary>
    private static string BuildBodyMismatchMessage(int probeIndex, string baseline, string actual)
    {
        var firstDiff = FindFirstDifferenceIndex(baseline, actual);
        const int window = 500;

        var baselineSnippet = SafeSubstring(baseline, firstDiff, window);
        var actualSnippet = SafeSubstring(actual, firstDiff, window);

        return $"probe {probeIndex} body differs at offset {firstDiff} (baseline len={baseline.Length}, actual len={actual.Length}). " +
               $"--- baseline (truncated 500 chars from diff)\n{baselineSnippet}\n" +
               $"+++ actual (truncated 500 chars from diff)\n{actualSnippet}";
    }

    private static int FindFirstDifferenceIndex(string a, string b)
    {
        var min = Math.Min(a.Length, b.Length);
        for (var i = 0; i < min; i++)
        {
            if (a[i] != b[i])
            {
                return i;
            }
        }

        return min;
    }

    private static string SafeSubstring(string source, int start, int length)
    {
        if (string.IsNullOrEmpty(source) || start >= source.Length)
        {
            return string.Empty;
        }

        var available = source.Length - start;
        return source.Substring(start, Math.Min(length, available));
    }

    private sealed record ProbeResult(
        HttpStatusCode StatusCode,
        string Location,
        string StrippedBody,
        string CacheControl,
        string ContentType,
        IReadOnlyList<string> SortedSetCookieNames);
}
