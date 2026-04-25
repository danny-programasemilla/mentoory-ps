using System.Net;
using FluentAssertions;
using Mentoory.Tests.Integration.Fixtures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Mentoory.Tests.Integration.Identity;

/// <summary>
/// Defense-in-depth checks (FR-018-21) for the registration / admin-enrollment endpoints:
/// antiforgery enforcement and rate-limit enforcement on the public registration endpoint.
///
/// Layout decision: Tests 1 and 2 share <see cref="IntegrationTestBase"/> (and therefore the
/// shared Testcontainers fixture). Test 3 (rate-limit) lives in <see cref="DefenseInDepthRateLimitTests"/>
/// further down in this same file because it needs a per-test <see cref="WebApplicationFactory{TEntryPoint}"/>
/// override that the shared collection-fixture cannot provide. Keeping both classes in the same
/// file keeps the FR-018-21 surface co-located for reviewers; the second class only spins up its
/// own SQL container if/when its rate-limit tests are unskipped.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class DefenseInDepthTests : IntegrationTestBase
{
    public DefenseInDepthTests(MentooryWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    [Trait("Spec", "FR-018-21")]
    [Trait("Floor", "defense-in-depth-controls")]
    public async Task PublicRegistration_AntiforgeryMissing_Returns400()
    {
        var client = Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var formContent = new FormUrlEncodedContent(new[]
        {
            // Intentionally omit __RequestVerificationToken so the antiforgery filter rejects
            // the request before it reaches model binding or the rate limiter.
            new KeyValuePair<string, string>("Email", "noaf@example.com"),
            new KeyValuePair<string, string>("Country", "CO"),
            new KeyValuePair<string, string>("NationalId", "111222333"),
            new KeyValuePair<string, string>("FirstName", "No"),
            new KeyValuePair<string, string>("LastName", "Antiforgery"),
            new KeyValuePair<string, string>("Password", "SecureP@ss12345!"),
            new KeyValuePair<string, string>("ConfirmPassword", "SecureP@ss12345!"),
        });

        var response = await client.PostAsync("/Access/Register", formContent);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "the [ValidateAntiForgeryToken] filter must reject any POST to /Access/Register that lacks a token");
    }

    [Fact]
    [Trait("Spec", "FR-018-21")]
    [Trait("Floor", "defense-in-depth-controls")]
    public async Task AdminEnrollment_AntiforgeryMissing_Returns400()
    {
        var client = await CreateAuthenticatedAdminClientAsync();

        var formContent = new FormUrlEncodedContent(new[]
        {
            // Authenticated client carries the session cookie but we deliberately drop the
            // antiforgery token to confirm the [ValidateAntiForgeryToken] filter blocks the
            // request even for legitimate admins.
            new KeyValuePair<string, string>("Email", "noaf-admin@example.com"),
            new KeyValuePair<string, string>("Country", "CO"),
            new KeyValuePair<string, string>("NationalId", "444555666"),
            new KeyValuePair<string, string>("FirstName", "No"),
            new KeyValuePair<string, string>("LastName", "Antiforgery"),
            new KeyValuePair<string, string>("Password", "SecureP@ss12345!"),
            new KeyValuePair<string, string>("ConfirmPassword", "SecureP@ss12345!"),
        });

        var response = await client.PostAsync("/Administration/Users/Enroll", formContent);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "the [ValidateAntiForgeryToken] filter must reject any POST to /Administration/Users/Enroll that lacks a token");
    }
}

/// <summary>
/// Rate-limit defense-in-depth assertion (FR-018-21). Lives in a separate class so its tests
/// can use a private <see cref="WebApplicationFactory{TEntryPoint}"/> subclass that overrides
/// the registration rate-limit policy to a test-friendly window — the shared collection
/// fixture cannot expose per-test config overrides.
///
/// CURRENT STATUS: Skipped. <c>Mentoory.Web/Program.cs</c> hard-codes the rate-limit policy
/// (PermitLimit + Window) inside <c>AddRateLimiter(...)</c>; no <c>IConfiguration</c> binding
/// reads <c>RateLimiting:Registration:*</c> keys today. Per spec 018 EC-009 / research.md #8,
/// the configuration-override approach requires a small production refactor (move the literals
/// into bound options) before this test can run. That refactor is intentionally out of scope
/// for feature 016 — see report attached to this PR.
/// </summary>
public class DefenseInDepthRateLimitTests
{
    [Fact(Skip = "TODO(018 EC-009 / research.md #8): Mentoory.Web/Program.cs hard-codes the 'registration' rate-limit policy " +
                 "(PermitLimit, Window) and does not bind RateLimiting:Registration:* from IConfiguration. The config-based " +
                 "override approach in research #8 cannot be exercised without first refactoring AddRateLimiter to read " +
                 "those keys. Unskip once the production rate-limit configuration is moved behind IOptions/IConfiguration.")]
    [Trait("Spec", "FR-018-21")]
    [Trait("Floor", "defense-in-depth-controls")]
    public Task PublicRegistration_RateLimitExceeded_Returns429()
    {
        // Intended shape once production binds RateLimiting:Registration:{PermitLimit,WindowSeconds}:
        //
        //   await using var factory = new DefenseInDepthFactory();
        //   await ((IAsyncLifetime)factory).InitializeAsync();
        //   var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        //   // GET /Access/Register once to mint an antiforgery cookie + token, then fire 4 sequential
        //   // POSTs carrying the token. Assert the first 3 are not 429 and the 4th is 429.
        //
        // Kept as documentation only; the [Fact(Skip = ...)] above prevents execution.
        return Task.CompletedTask;
    }

    /// <summary>
    /// Per-test <see cref="WebApplicationFactory{TEntryPoint}"/> subclass that layers a tight
    /// rate-limit override on top of the shared factory's plumbing. Activated only when the
    /// production code learns to bind <c>RateLimiting:Registration:*</c> from configuration.
    /// </summary>
    private sealed class DefenseInDepthFactory : MentooryWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureAppConfiguration((_, cb) => cb.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["RateLimiting:Registration:PermitLimit"] = "3",
                    ["RateLimiting:Registration:WindowSeconds"] = "10",
                }));
        }
    }
}
