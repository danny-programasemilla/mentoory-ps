using FluentAssertions;
using Mentoory.Tenant.Domain.Enums;
using Mentoory.Tests.E2E.Infrastructure;
using Mentoory.Tests.E2E.Infrastructure.Lifecycle;
using Xunit;

namespace Mentoory.Tests.E2E.Tests.Lifecycle;

[Collection(E2ETestCollection.Name)]
public class WalkthroughLifecyclePageTests : IAsyncLifetime
{
    private const string TimestampPattern = @"^\d{2}/\d{2}/\d{4} \d{2}:\d{2}$";

    private static readonly StageType[] AllStages = Enum.GetValues<StageType>();

    private readonly PlaywrightFixture _host;
    private readonly LifecycleFixtures _fixtures;

    public WalkthroughLifecyclePageTests(PlaywrightFixture host)
    {
        _host = host;
        _fixtures = new LifecycleFixtures(host);
    }

    public Task InitializeAsync() => _fixtures.ResetStateAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Lifecycle_MidProject_RegistrationCompleted_FormsInProgress()
    {
        var incubatorExternalId = await _fixtures.GetSeededIncubatorAlphaExternalIdAsync();
        var projectExternalId = await _fixtures.CreateProjectAsync(
            incubatorExternalId,
            LifecycleFixtures.ProjectNamePrefix + "mid-forms",
            StageType.Forms);

        var page = await _host.CreatePageAsync();
        try
        {
            await LifecycleLoginHelpers.AsCoordinatorAsync(page, _host, userNumber: 1);
            var lifecycle = new LifecyclePageObject(page, _host);
            await lifecycle.GotoAsync(projectExternalId);

            await AssertStageCompletedAsync(lifecycle, StageType.Registration);

            (await lifecycle.ReadStageStateAsync(StageType.Forms))
                .Should().Be(LifecyclePageObject.StageStateInProgress);
            var formsTs = await lifecycle.ReadStageTimestampsAsync(StageType.Forms);
            formsTs.StartedAt.Should().MatchRegex(TimestampPattern);
            formsTs.CompletedAt.Should().BeNull("Forms is still in progress and has no completion timestamp.");

            await AssertStagesPendingAsync(lifecycle, AllStages.Where(s => s > StageType.Forms));
        }
        finally
        {
            await _host.TakeScreenshotOnFailureAsync(page, nameof(Lifecycle_MidProject_RegistrationCompleted_FormsInProgress));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task Lifecycle_BrandNewProject_OnlyRegistrationInProgress()
    {
        var incubatorExternalId = await _fixtures.GetSeededIncubatorAlphaExternalIdAsync();
        var projectExternalId = await _fixtures.CreateProjectAsync(
            incubatorExternalId,
            LifecycleFixtures.ProjectNamePrefix + "brand-new",
            StageType.Registration);

        var page = await _host.CreatePageAsync();
        try
        {
            await LifecycleLoginHelpers.AsCoordinatorAsync(page, _host, userNumber: 1);
            var lifecycle = new LifecyclePageObject(page, _host);
            await lifecycle.GotoAsync(projectExternalId);

            (await lifecycle.ReadStageStateAsync(StageType.Registration))
                .Should().Be(LifecyclePageObject.StageStateInProgress);
            var registrationTs = await lifecycle.ReadStageTimestampsAsync(StageType.Registration);
            registrationTs.StartedAt.Should().MatchRegex(TimestampPattern);
            registrationTs.CompletedAt.Should().BeNull("Registration is still in progress.");

            await AssertStagesPendingAsync(lifecycle, AllStages.Where(s => s > StageType.Registration));

            (await lifecycle.IsAdvanceButtonVisibleAsync())
                .Should().BeTrue("el proyecto recién creado en Registro debe permitir avanzar.");
        }
        finally
        {
            await _host.TakeScreenshotOnFailureAsync(page, nameof(Lifecycle_BrandNewProject_OnlyRegistrationInProgress));
            await page.Context.DisposeAsync();
        }
    }

    // AdvanceProjectStageHandler.cs:48 rejects any advance at Closure, so Closure never transitions to Completed via the UI.
    [Fact]
    public async Task Lifecycle_ClosedProject_PriorStagesCompleted_ClosureInProgress()
    {
        var incubatorExternalId = await _fixtures.GetSeededIncubatorAlphaExternalIdAsync();
        var projectExternalId = await _fixtures.CreateProjectAsync(
            incubatorExternalId,
            LifecycleFixtures.ProjectNamePrefix + "closed",
            StageType.Closure);

        var page = await _host.CreatePageAsync();
        try
        {
            await LifecycleLoginHelpers.AsCoordinatorAsync(page, _host, userNumber: 1);
            var lifecycle = new LifecyclePageObject(page, _host);
            await lifecycle.GotoAsync(projectExternalId);

            foreach (var stage in AllStages.Where(s => s < StageType.Closure))
            {
                await AssertStageCompletedAsync(lifecycle, stage);
            }

            (await lifecycle.ReadStageStateAsync(StageType.Closure))
                .Should().Be(LifecyclePageObject.StageStateInProgress);
            var closureTs = await lifecycle.ReadStageTimestampsAsync(StageType.Closure);
            closureTs.StartedAt.Should().MatchRegex(TimestampPattern);
            closureTs.CompletedAt.Should().BeNull("Closure has no completion flow in the current product.");

            (await lifecycle.IsAdvanceButtonVisibleAsync())
                .Should().BeFalse("el botón de avance no debe existir en la etapa final (Cierre).");

            (await lifecycle.ReadCannotAdvanceReasonAsync())
                .Should().Be(LifecyclePageObject.ClosureFinalStageMessage);
        }
        finally
        {
            await _host.TakeScreenshotOnFailureAsync(page, nameof(Lifecycle_ClosedProject_PriorStagesCompleted_ClosureInProgress));
            await page.Context.DisposeAsync();
        }
    }

    private static async Task AssertStageCompletedAsync(LifecyclePageObject lifecycle, StageType stage)
    {
        (await lifecycle.ReadStageStateAsync(stage))
            .Should().Be(LifecyclePageObject.StageStateCompleted, $"{stage} should be completed.");
        var ts = await lifecycle.ReadStageTimestampsAsync(stage);
        ts.StartedAt.Should().MatchRegex(TimestampPattern, $"{stage} has a start timestamp.");
        ts.CompletedAt.Should().MatchRegex(TimestampPattern, $"{stage} has a completion timestamp.");
    }

    private static async Task AssertStagesPendingAsync(LifecyclePageObject lifecycle, IEnumerable<StageType> stages)
    {
        foreach (var stage in stages)
        {
            (await lifecycle.ReadStageStateAsync(stage))
                .Should().Be(LifecyclePageObject.StageStatePending, $"{stage} has not started yet.");
            var ts = await lifecycle.ReadStageTimestampsAsync(stage);
            ts.StartedAt.Should().BeNull($"{stage} has no start timestamp.");
            ts.CompletedAt.Should().BeNull($"{stage} has no completion timestamp.");
        }
    }
}
