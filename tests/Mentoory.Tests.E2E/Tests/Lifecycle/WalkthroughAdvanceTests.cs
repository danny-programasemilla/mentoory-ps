using System.Text.Json;
using FluentAssertions;
using Mentoory.Tenant.Domain.Enums;
using Mentoory.Tests.E2E.Infrastructure;
using Mentoory.Tests.E2E.Infrastructure.Lifecycle;
using Microsoft.Playwright;
using Xunit;

namespace Mentoory.Tests.E2E.Tests.Lifecycle;

[Collection(E2ETestCollection.Name)]
public class WalkthroughAdvanceTests : IAsyncLifetime
{
    private readonly PlaywrightFixture _host;
    private readonly LifecycleFixtures _fixtures;

    public WalkthroughAdvanceTests(PlaywrightFixture host)
    {
        _host = host;
        _fixtures = new LifecycleFixtures(host);
    }

    public Task InitializeAsync() => _fixtures.ResetStateAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Coordinator_AdvancesProjectFromRegistrationToForms()
    {
        var incubatorExternalId = await _fixtures.GetSeededIncubatorAlphaExternalIdAsync();
        var projectExternalId = await _fixtures.CreateProjectAsync(
            incubatorExternalId,
            LifecycleFixtures.ProjectNamePrefix + "advance-1",
            StageType.Registration);

        var page = await _host.CreatePageAsync();
        try
        {
            await LifecycleLoginHelpers.AsCoordinatorAsync(page, _host, userNumber: 1);
            var lifecycle = new LifecyclePageObject(page, _host);
            await lifecycle.GotoAsync(projectExternalId);

            await lifecycle.ClickAdvanceAndConfirmAsync();

            page.Url.Should().Contain($"/Coordination/Projects/Lifecycle/{projectExternalId}");

            var toast = await lifecycle.ReadSuccessToastAsync();
            toast.Should().Be("Proyecto avanzado a Formularios.");

            (await lifecycle.ReadStageStateAsync(StageType.Registration))
                .Should().Be(LifecyclePageObject.StageStateCompleted);
            (await lifecycle.ReadStageStateAsync(StageType.Forms))
                .Should().Be(LifecyclePageObject.StageStateInProgress);
        }
        finally
        {
            await _host.TakeScreenshotOnFailureAsync(page, nameof(Coordinator_AdvancesProjectFromRegistrationToForms));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task Coordinator_CannotAdvanceProjectAtClosure()
    {
        var incubatorExternalId = await _fixtures.GetSeededIncubatorAlphaExternalIdAsync();
        var closureProjectId = await _fixtures.CreateProjectAsync(
            incubatorExternalId,
            LifecycleFixtures.ProjectNamePrefix + "closure-1",
            StageType.Closure);
        var tokenCarrierProjectId = await _fixtures.CreateProjectAsync(
            incubatorExternalId,
            LifecycleFixtures.ProjectNamePrefix + "token-carrier",
            StageType.Registration);

        var page = await _host.CreatePageAsync();
        try
        {
            await LifecycleLoginHelpers.AsCoordinatorAsync(page, _host, userNumber: 1);
            var lifecycle = new LifecyclePageObject(page, _host);

            await lifecycle.GotoAsync(closureProjectId);
            (await lifecycle.IsAdvanceButtonVisibleAsync())
                .Should().BeFalse("el botón de avance no debe existir en la etapa final (Cierre).");

            var token = await HarvestAntiforgeryTokenAsync(page, lifecycle, tokenCarrierProjectId);
            var responseType = await PostAdvanceWithManualRedirectAsync(page, closureProjectId, token);
            responseType.Should().Be("opaqueredirect",
                "el handler debe responder con redirección (302) a Lifecycle tras rechazar el avance.");

            await lifecycle.GotoAsync(closureProjectId);
            var toast = await lifecycle.ReadErrorToastAsync();
            toast.Should().Be("El proyecto ya está en la etapa final (Cierre).");
        }
        finally
        {
            await _host.TakeScreenshotOnFailureAsync(page, nameof(Coordinator_CannotAdvanceProjectAtClosure));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task Coordinator_CannotAdvanceWhenCurrentStageIsCompleted()
    {
        var incubatorExternalId = await _fixtures.GetSeededIncubatorAlphaExternalIdAsync();
        var brokenProjectId = await _fixtures.CreateProjectAsync(
            incubatorExternalId,
            LifecycleFixtures.ProjectNamePrefix + "broken-state-1",
            StageType.Registration);
        await _fixtures.ForceCurrentStageStateCompletedAsync(brokenProjectId);

        var tokenCarrierProjectId = await _fixtures.CreateProjectAsync(
            incubatorExternalId,
            LifecycleFixtures.ProjectNamePrefix + "token-carrier",
            StageType.Registration);

        var page = await _host.CreatePageAsync();
        try
        {
            await LifecycleLoginHelpers.AsCoordinatorAsync(page, _host, userNumber: 1);
            var lifecycle = new LifecyclePageObject(page, _host);

            var token = await HarvestAntiforgeryTokenAsync(page, lifecycle, tokenCarrierProjectId);
            var responseType = await PostAdvanceWithManualRedirectAsync(page, brokenProjectId, token);
            responseType.Should().Be("opaqueredirect",
                "el handler debe responder con redirección (302) a Lifecycle tras rechazar el avance.");

            await lifecycle.GotoAsync(brokenProjectId);
            var toast = await lifecycle.ReadErrorToastAsync();
            toast.Should().Be("La etapa actual no está en progreso. Actualice la página.");
        }
        finally
        {
            await _host.TakeScreenshotOnFailureAsync(page, nameof(Coordinator_CannotAdvanceWhenCurrentStageIsCompleted));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task NonCoordinator_CannotSeeAdvanceButtonOrInvokeAdvance()
    {
        var incubatorExternalId = await _fixtures.GetSeededIncubatorAlphaExternalIdAsync();
        var projectExternalId = await _fixtures.CreateProjectAsync(
            incubatorExternalId,
            LifecycleFixtures.ProjectNamePrefix + "mentor-target",
            StageType.Registration);

        var page = await _host.CreatePageAsync();
        try
        {
            await LifecycleLoginHelpers.AsMentorAsync(page, _host);
            var lifecycle = new LifecyclePageObject(page, _host);

            await lifecycle.GotoAsync(projectExternalId);

            page.Url.Should().NotContain(
                $"/Coordination/Projects/Lifecycle/{projectExternalId}",
                "el Mentor no está en la lista de roles del controlador y no debe poder cargar la vista de ciclo de vida.");

            var finalUrl = await PostAdvanceFollowingRedirectsAsync(page, projectExternalId);
            finalUrl.Should().NotContain(
                $"/Coordination/Projects/Lifecycle/{projectExternalId}",
                "un POST directo de Mentor a AdvanceStage no debe aterrizar en la vista de Lifecycle del proyecto.");
        }
        finally
        {
            await _host.TakeScreenshotOnFailureAsync(page, nameof(NonCoordinator_CannotSeeAdvanceButtonOrInvokeAdvance));
            await page.Context.DisposeAsync();
        }
    }

    private static async Task<string> HarvestAntiforgeryTokenAsync(
        IPage page,
        LifecyclePageObject lifecycle,
        Guid tokenCarrierProjectId)
    {
        await lifecycle.GotoAsync(tokenCarrierProjectId);
        var token = await page.Locator("#advanceStageModal input[name='__RequestVerificationToken']")
            .First
            .GetAttributeAsync("value");
        token.Should().NotBeNullOrWhiteSpace(
            "la página de Lifecycle de un proyecto avanzable debe exponer el token antiforgery.");
        return token!;
    }

    // Posts via fetch inside the browser so the auth cookie is always attached;
    // Playwright's IAPIRequestContext does not share cookies with the page context in this host setup.
    private async Task<string> PostAdvanceWithManualRedirectAsync(IPage page, Guid projectExternalId, string antiforgeryToken)
    {
        var response = await ExecuteAdvancePostAsync(page, projectExternalId, antiforgeryToken, followRedirects: false);
        return response.GetProperty("type").GetString() ?? string.Empty;
    }

    private async Task<string> PostAdvanceFollowingRedirectsAsync(IPage page, Guid projectExternalId)
    {
        var response = await ExecuteAdvancePostAsync(page, projectExternalId, antiforgeryToken: null, followRedirects: true);
        return response.GetProperty("url").GetString() ?? string.Empty;
    }

    private Task<JsonElement> ExecuteAdvancePostAsync(IPage page, Guid projectExternalId, string? antiforgeryToken, bool followRedirects)
    {
        var relativeUrl = $"/Coordination/Projects/AdvanceStage/{projectExternalId}";
        return page.EvaluateAsync<JsonElement>(@"
            async ({ url, token, followRedirects }) => {
                const body = token ? ('__RequestVerificationToken=' + encodeURIComponent(token)) : '';
                const res = await fetch(url, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                    body: body,
                    redirect: followRedirects ? 'follow' : 'manual',
                });
                return { type: res.type, url: res.url };
            }", new { url = relativeUrl, token = antiforgeryToken, followRedirects });
    }
}
