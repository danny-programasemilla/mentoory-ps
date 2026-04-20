using FluentAssertions;
using Mentoory.Tenant.Domain.Enums;
using Mentoory.Tests.E2E.Infrastructure;
using Mentoory.Tests.E2E.Infrastructure.Lifecycle;
using Microsoft.Playwright;
using Xunit;

namespace Mentoory.Tests.E2E.Tests.Lifecycle;

[Collection(E2ETestCollection.Name)]
public class WalkthroughAuditConcurrencyTests : IAsyncLifetime
{
    private const string Coord1DisplayName = "Ana Rodríguez";
    private const string Coord2DisplayName = "Luis Paredes";
    private const string Coord3DisplayName = "Sofía Navarro";

    private const string ConcurrencyToastMessage =
        "Otra operación modificó este proyecto. Actualice la página e intente de nuevo.";
    private const string InactiveToastMessage =
        "El proyecto está inactivo. Active el proyecto antes de avanzar de etapa.";
    private const string InactiveMutedText = "El proyecto está inactivo.";
    private const string ForwardAdvanceSuccessToast = "Proyecto avanzado a Formularios.";

    private readonly PlaywrightFixture _host;
    private readonly LifecycleFixtures _fixtures;

    public WalkthroughAuditConcurrencyTests(PlaywrightFixture host)
    {
        _host = host;
        _fixtures = new LifecycleFixtures(host);
    }

    public Task InitializeAsync() => _fixtures.ResetStateAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AuditTrail_ThreeAdvancesByThreeCoordinators_AllNamesVisibleOnLifecyclePage()
    {
        var incubatorExternalId = await _fixtures.GetSeededIncubatorAlphaExternalIdAsync();
        var projectExternalId = await _fixtures.CreateProjectAsync(
            incubatorExternalId,
            LifecycleFixtures.ProjectNamePrefix + "audit-trail",
            StageType.Registration);

        await AdvanceAsCoordinatorAsync(projectExternalId, userNumber: 1);
        await AdvanceAsCoordinatorAsync(projectExternalId, userNumber: 2);
        await AdvanceAsCoordinatorAsync(projectExternalId, userNumber: 3);

        var page = await _host.CreatePageAsync();
        try
        {
            await LifecycleLoginHelpers.AsCoordinatorAsync(page, _host, userNumber: 1);
            var lifecycle = new LifecyclePageObject(page, _host);
            await lifecycle.GotoAsync(projectExternalId);

            // Project.AdvanceStage (Mentoory.Tenant.Domain/.../Project.cs:93) stamps
            // AdvancedByUserId on the stage being *started*, not the one being *completed*.
            // So coord1's Registration→Forms advance attributes Forms to coord1, and Registration
            // (created without a user via Project.Create) has no attribution at all.
            (await lifecycle.ReadStageAdvancedByAsync(StageType.Registration))
                .Should().BeNull("Registration is initialized by Project.Create without an acting user.");
            (await lifecycle.ReadStageAdvancedByAsync(StageType.Forms))
                .Should().Be(Coord1DisplayName);
            (await lifecycle.ReadStageAdvancedByAsync(StageType.Analysis))
                .Should().Be(Coord2DisplayName);
            (await lifecycle.ReadStageAdvancedByAsync(StageType.LearningAssignment))
                .Should().Be(Coord3DisplayName);

            (await lifecycle.ReadStageStateAsync(StageType.LearningAssignment))
                .Should().Be(LifecyclePageObject.StageStateInProgress);
        }
        finally
        {
            await _host.TakeScreenshotOnFailureAsync(page, nameof(AuditTrail_ThreeAdvancesByThreeCoordinators_AllNamesVisibleOnLifecyclePage));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task ConcurrentAdvance_SecondAttemptShowsConcurrencyToast()
    {
        var incubatorExternalId = await _fixtures.GetSeededIncubatorAlphaExternalIdAsync();
        var projectExternalId = await _fixtures.CreateProjectAsync(
            incubatorExternalId,
            LifecycleFixtures.ProjectNamePrefix + "concurrency",
            StageType.Registration);

        var (page1, page2) = await CreatePagePairAsync();
        try
        {
            await Task.WhenAll(
                LifecycleLoginHelpers.AsCoordinatorAsync(page1, _host, userNumber: 1),
                LifecycleLoginHelpers.AsCoordinatorAsync(page2, _host, userNumber: 2));

            var lifecycle1 = new LifecyclePageObject(page1, _host);
            var lifecycle2 = new LifecyclePageObject(page2, _host);

            await Task.WhenAll(
                lifecycle1.GotoAsync(projectExternalId),
                lifecycle2.GotoAsync(projectExternalId));

            var tokenTasks = await Task.WhenAll(
                lifecycle1.ReadAdvanceAntiforgeryTokenAsync(),
                lifecycle2.ReadAdvanceAntiforgeryTokenAsync());
            tokenTasks[0].Should().NotBeNullOrWhiteSpace();
            tokenTasks[1].Should().NotBeNullOrWhiteSpace();

            await Task.WhenAll(
                lifecycle1.PostAdvanceStageAsync(projectExternalId, tokenTasks[0], followRedirects: false),
                lifecycle2.PostAdvanceStageAsync(projectExternalId, tokenTasks[1], followRedirects: false));

            await Task.WhenAll(
                lifecycle1.GotoAsync(projectExternalId),
                lifecycle2.GotoAsync(projectExternalId));

            var (outcome1, outcome2) = (await ReadOutcomeAsync(lifecycle1), await ReadOutcomeAsync(lifecycle2));

            var successToast = outcome1.Success ?? outcome2.Success;
            var errorToast = outcome1.Error ?? outcome2.Error;

            successToast.Should().Be(ForwardAdvanceSuccessToast,
                "exactamente un POST debe ganar la carrera de optimistic concurrency.");
            errorToast.Should().Be(ConcurrencyToastMessage,
                "exactamente un POST debe perder la carrera y mostrar el mensaje de concurrencia.");
        }
        finally
        {
            await _host.TakeScreenshotOnFailureAsync(page1, nameof(ConcurrentAdvance_SecondAttemptShowsConcurrencyToast) + "_page1");
            await _host.TakeScreenshotOnFailureAsync(page2, nameof(ConcurrentAdvance_SecondAttemptShowsConcurrencyToast) + "_page2");
            await Task.WhenAll(page1.Context.DisposeAsync().AsTask(), page2.Context.DisposeAsync().AsTask());
        }
    }

    [Fact]
    public async Task InactiveProject_AdvanceAttemptShowsInactiveToast()
    {
        var incubatorExternalId = await _fixtures.GetSeededIncubatorAlphaExternalIdAsync();
        var inactiveProjectId = await _fixtures.CreateProjectAsync(
            incubatorExternalId,
            LifecycleFixtures.ProjectNamePrefix + "inactive",
            StageType.Registration);
        await _fixtures.DeactivateProjectAsync(inactiveProjectId);

        // Inactive projects render without the advance button (and therefore without an embedded
        // antiforgery token); an active sibling project carries the token for the bypass POST.
        var tokenCarrierProjectId = await _fixtures.CreateProjectAsync(
            incubatorExternalId,
            LifecycleFixtures.ProjectNamePrefix + "inactive-token-carrier",
            StageType.Registration);

        var page = await _host.CreatePageAsync();
        try
        {
            await LifecycleLoginHelpers.AsCoordinatorAsync(page, _host, userNumber: 1);
            var lifecycle = new LifecyclePageObject(page, _host);
            await lifecycle.GotoAsync(inactiveProjectId);

            (await lifecycle.IsAdvanceButtonVisibleAsync())
                .Should().BeFalse("un proyecto inactivo no debe exponer el botón de avance (CanAdvance=false).");
            (await lifecycle.ReadCannotAdvanceReasonAsync())
                .Should().Be(InactiveMutedText);

            var token = await lifecycle.HarvestAdvanceTokenFromAsync(tokenCarrierProjectId);
            token.Should().NotBeNullOrWhiteSpace();
            await lifecycle.PostAdvanceStageAsync(inactiveProjectId, token, followRedirects: false);

            await lifecycle.GotoAsync(inactiveProjectId);
            (await lifecycle.ReadErrorToastAsync()).Should().Be(InactiveToastMessage);
        }
        finally
        {
            await _host.TakeScreenshotOnFailureAsync(page, nameof(InactiveProject_AdvanceAttemptShowsInactiveToast));
            await page.Context.DisposeAsync();
        }
    }

    private static async Task<(string? Success, string? Error)> ReadOutcomeAsync(LifecyclePageObject lifecycle)
    {
        var success = await lifecycle.ReadSuccessToastAsync();
        var error = await lifecycle.ReadErrorToastAsync();
        return (success, error);
    }

    private async Task<(IPage Page1, IPage Page2)> CreatePagePairAsync()
    {
        var pages = await Task.WhenAll(_host.CreatePageAsync(), _host.CreatePageAsync());
        return (pages[0], pages[1]);
    }

    private async Task AdvanceAsCoordinatorAsync(Guid projectExternalId, int userNumber)
    {
        var page = await _host.CreatePageAsync();
        try
        {
            await LifecycleLoginHelpers.AsCoordinatorAsync(page, _host, userNumber);
            var lifecycle = new LifecyclePageObject(page, _host);
            await lifecycle.GotoAsync(projectExternalId);
            await lifecycle.ClickAdvanceAndConfirmAsync();
        }
        finally
        {
            await page.Context.DisposeAsync();
        }
    }
}
