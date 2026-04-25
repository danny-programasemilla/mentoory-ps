using FluentAssertions;
using Mentoory.Access.Application.StageActions;
using Mentoory.Tenant.Domain.Enums;
using Mentoory.Tests.E2E.Infrastructure;
using Mentoory.Tests.E2E.Infrastructure.Lifecycle;
using Xunit;

namespace Mentoory.Tests.E2E.Tests.Lifecycle;

[Collection(E2ETestCollection.Name)]
public class WalkthroughGatedActionsTests : IAsyncLifetime
{
    private static readonly StageGatedAction[] AllActions = Enum.GetValues<StageGatedAction>();

    private readonly PlaywrightFixture _host;
    private readonly LifecycleFixtures _fixtures;

    public WalkthroughGatedActionsTests(PlaywrightFixture host)
    {
        _host = host;
        _fixtures = new LifecycleFixtures(host);
    }

    public Task InitializeAsync() => _fixtures.ResetStateAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(Skip = "Blocked by product bug — see specs/016-project-lifecycle-finish/e2e/open-questions.md US3 §1 (C3).")]
    public async Task GatedActions_RegistrationStage_AllSixCardsLocked()
    {
        var incubatorExternalId = await _fixtures.GetSeededIncubatorAlphaExternalIdAsync();
        var projectExternalId = await _fixtures.CreateProjectAsync(
            incubatorExternalId,
            LifecycleFixtures.ProjectNamePrefix + "gated-reg",
            StageType.Registration);

        var page = await _host.CreatePageAsync();
        try
        {
            await LifecycleLoginHelpers.AsCoordinatorAsync(page, _host, userNumber: 1);
            var lifecycle = new LifecyclePageObject(page, _host);
            await lifecycle.GotoAsync(projectExternalId);

            foreach (var action in AllActions)
            {
                var card = lifecycle.ActionCard(action).First;
                (await card.GetAttributeAsync("aria-disabled")).Should().Be("true");
                (await card.GetAttributeAsync("tabindex")).Should().Be("-1");
                (await card.GetAttributeAsync("data-bs-toggle")).Should().Be("tooltip");
                (await lifecycle.ReadActionTooltipAsync(action)).Should().Be(ExpectedTooltip(action));
            }
        }
        finally
        {
            await _host.TakeScreenshotOnFailureAsync(page, nameof(GatedActions_RegistrationStage_AllSixCardsLocked));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task GatedActions_FlipStatesOnAdvance()
    {
        var incubatorExternalId = await _fixtures.GetSeededIncubatorAlphaExternalIdAsync();
        var projectExternalId = await _fixtures.CreateProjectAsync(
            incubatorExternalId,
            LifecycleFixtures.ProjectNamePrefix + "gated-flip",
            StageType.Forms);

        var page = await _host.CreatePageAsync();
        try
        {
            await LifecycleLoginHelpers.AsCoordinatorAsync(page, _host, userNumber: 1);
            var lifecycle = new LifecyclePageObject(page, _host);
            await lifecycle.GotoAsync(projectExternalId);

            (await lifecycle.ReadActionStateAsync(StageGatedAction.DiagnosticForms))
                .Should().Be(StageGatedActionState.Available);
            (await lifecycle.ReadActionHrefAsync(StageGatedAction.DiagnosticForms))
                .Should().Be(StageActionLinks.Resolve(StageGatedAction.DiagnosticForms));

            await lifecycle.ClickAdvanceAndConfirmAsync();

            page.Url.Should().Contain($"/Coordination/Projects/Lifecycle/{projectExternalId}");

            (await lifecycle.ReadActionStateAsync(StageGatedAction.DiagnosticForms))
                .Should().Be(StageGatedActionState.Past);
            (await lifecycle.ReadActionStateAsync(StageGatedAction.AnswerCorrection))
                .Should().Be(StageGatedActionState.Available);
        }
        finally
        {
            await _host.TakeScreenshotOnFailureAsync(page, nameof(GatedActions_FlipStatesOnAdvance));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task GatedActions_DirectUrlToLockedAction_RedirectsToLifecycleWithSpanishToast()
    {
        // AnswerCorrection's route has no projectExternalId, so RequiresStageAttribute falls
        // back to the session-active project claim — which auto-selects to Innovación.
        var innovacionExternalId = await _fixtures.GetSeededInnovacionProjectExternalIdAsync();

        var page = await _host.CreatePageAsync();
        try
        {
            await LifecycleLoginHelpers.AsCoordinatorAsync(page, _host, userNumber: 1);
            var lifecycle = new LifecyclePageObject(page, _host);

            // The filter rejects before the action runs, so any syntactically valid Guid works.
            await lifecycle.GotoUrlAsync($"/Coordination/AnswerCorrection/{Guid.NewGuid()}");

            page.Url.Should().Contain($"/Coordination/Projects/Lifecycle/{innovacionExternalId}");

            var gatingStage = StageActionRegistry.GetGatingStage(StageGatedAction.AnswerCorrection);
            var expectedToast = $"Esta acción estará disponible desde la etapa {StageTypeDisplay.ToSpanish(gatingStage)}.";
            (await lifecycle.ReadWarningToastAsync()).Should().Be(expectedToast);
        }
        finally
        {
            await _host.TakeScreenshotOnFailureAsync(page, nameof(GatedActions_DirectUrlToLockedAction_RedirectsToLifecycleWithSpanishToast));
            await page.Context.DisposeAsync();
        }
    }

    [Fact(Skip = "Blocked by product bug — see specs/016-project-lifecycle-finish/e2e/open-questions.md US3 §4 (C3).")]
    public async Task GatedActions_LockedCardTooltipNamesUnlockingStage()
    {
        var incubatorExternalId = await _fixtures.GetSeededIncubatorAlphaExternalIdAsync();
        var projectExternalId = await _fixtures.CreateProjectAsync(
            incubatorExternalId,
            LifecycleFixtures.ProjectNamePrefix + "gated-tooltips",
            StageType.Registration);

        var page = await _host.CreatePageAsync();
        try
        {
            await LifecycleLoginHelpers.AsCoordinatorAsync(page, _host, userNumber: 1);
            var lifecycle = new LifecyclePageObject(page, _host);
            await lifecycle.GotoAsync(projectExternalId);

            foreach (var action in AllActions)
            {
                (await lifecycle.ReadActionTooltipAsync(action))
                    .Should().Be(ExpectedTooltip(action));
            }
        }
        finally
        {
            await _host.TakeScreenshotOnFailureAsync(page, nameof(GatedActions_LockedCardTooltipNamesUnlockingStage));
            await page.Context.DisposeAsync();
        }
    }

    private static string ExpectedTooltip(StageGatedAction action) =>
        $"Disponible desde la etapa {StageTypeDisplay.ToSpanish(StageActionRegistry.GetGatingStage(action))}";
}
