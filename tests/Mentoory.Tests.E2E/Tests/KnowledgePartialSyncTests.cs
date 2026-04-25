using FluentAssertions;
using Mentoory.Knowledge.Infrastructure.Persistence;
using Mentoory.Tests.E2E.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using Xunit;

namespace Mentoory.Tests.E2E.Tests;

/// <summary>
/// E2E coverage for spec 017 US5 — PartialSync lifecycle. Exercises the SyncMode toggle
/// (Disconnected hides/disables the sync action; PartialSync enables it), the append-new-
/// template-items cascade, preservation of locally-added items, and preservation of locally-
/// renamed items during a sync. Backstop domain-level invariants live in
/// <c>tests/Mentoory.Knowledge.Tests/Handlers/SyncFromTemplateHandlerTests.cs</c>; this file
/// is the HTTP-boundary contract.
///
/// | Spec 017 scenario | Method |
/// |---|---|
/// | US5-1            | Disconnected_SyncActionHiddenOrDisabled |
/// | US5-2            | SwitchToPartialSync_PersistsAndEnablesSyncAction |
/// | US5-3            | PartialSync_AppendsNewTemplateTopic_UnderMatchingParent |
/// | US5-4            | PartialSync_LocalAddedTopic_Untouched |
/// | US5-5            | PartialSync_LocallyRenamedTopic_RetainsLocalName |
/// </summary>
[Collection(E2ETestCollection.Name)]
[Trait("Category", "E2E")]
public class KnowledgePartialSyncTests
{
    // Verbatim Spanish substring from project-structure-editor.js's success-toast template.
    // Asserting this ties the UI behavior to the JS contract — if either side drifts, the
    // wait times out with a clear diagnostic instead of racing past a silent regression.
    private const string SyncSummaryToastSubstring = "Sincronización completada:";

    // Seeded coord1 project KS ExternalId (004.SeedTestData.sql § "Proyecto Innovación").
    // This KS has SyncMode=Disconnected and every Module/Topic with SourceTemplate* = NULL,
    // which is exactly what US5-1 needs and exactly why US5-3/-4/-5 cannot use it — the
    // PartialSync walker matches by SourceTemplateModuleExternalId, so NULL back-refs produce
    // zero matches and the whole tree would be re-appended instead of merged.
    private static readonly Guid SeededProjectKsExternalId =
        new("99999999-9999-9999-9999-999999999901");

    // Seeded KS template ExternalId (005.SeedKnowledgeData.sql § "Emprendimiento Básico").
    private static readonly Guid SeededKsTemplateExternalId =
        new("11111111-1111-1111-1111-111111111111");

    // Seeded ModuleTemplate ExternalId under the KS template (005 § "Ideación"). Target parent
    // for US5-3's new template topic; the fresh project's provisioned clone module carries
    // this same ExternalId in SourceTemplateModuleExternalId, which is how the walker matches.
    private static readonly Guid SeededIdeacionModuleTemplateExternalId =
        new("22222222-2222-2222-2222-222222222222");

    private readonly PlaywrightFixture _fixture;

    public KnowledgePartialSyncTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    private enum SyncModeRadio
    {
        Disconnected,
        Partial,
    }

    [Fact]
    public async Task Disconnected_SyncActionHiddenOrDisabled()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsCoord1Async(page);
            await OpenProjectKsDetailAsync(page, SeededProjectKsExternalId);

            // Two-sided assertion: the SyncMode radio pair must be visible (so coord can
            // toggle), AND the sync action must be either absent OR present-but-disabled
            // (the view uses the latter shape so FR-T12's "broad selector" rule applies).
            var syncModeToggle = page.Locator("input[name='syncMode']");
            (await syncModeToggle.CountAsync()).Should().Be(2,
                "both Disconnected and PartialSync radios must render — coord must be able to toggle");

            var disconnectedRadio = page.Locator("#sync-disconnected");
            await Assertions.Expect(disconnectedRadio).ToBeCheckedAsync();

            var syncBtn = page.Locator("[data-action='sync-from-template']");
            var syncBtnCount = await syncBtn.CountAsync();
            if (syncBtnCount == 0)
            {
                return; // Absent satisfies the acceptance criterion on its own.
            }

            (await syncBtn.IsDisabledAsync())
                .Should().BeTrue(
                    "the 'Sincronizar desde plantilla' action must be disabled while SyncMode = Disconnected "
                    + "— otherwise the POST would slip past the handler's validation into a noisy 400");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Disconnected_SyncActionHiddenOrDisabled));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task SwitchToPartialSync_PersistsAndEnablesSyncAction()
    {
        var project = await ProvisionFreshProjectAsync("US5-2");

        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsProjectCoordinatorAsync(page, project.CoordinatorEmail, project.CoordinatorPassword, project.Name);
            var projectKsExternalId = await GetProjectKsExternalIdAsync(project.ProjectId);
            await OpenProjectKsDetailAsync(page, projectKsExternalId);

            await SetSyncModeViaUiAsync(page, SyncModeRadio.Partial);

            // After the save's handleResponse() reload, both (a) the PartialSync radio must
            // still be checked (persistence) and (b) the sync action must be enabled
            // (behavior gated on SyncMode at view-render time).
            await Assertions.Expect(page.Locator("#sync-partial")).ToBeCheckedAsync();

            var syncBtn = page.Locator("[data-action='sync-from-template']");
            (await syncBtn.CountAsync()).Should().BeGreaterThan(0,
                "the 'Sincronizar desde plantilla' action must be present once SyncMode=PartialSync");
            (await syncBtn.IsEnabledAsync())
                .Should().BeTrue(
                    "SyncMode=PartialSync must enable the sync action — otherwise coord cannot trigger it");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(SwitchToPartialSync_PersistsAndEnablesSyncAction));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task PartialSync_AppendsNewTemplateTopic_UnderMatchingParent()
    {
        // Fresh project bound to the seeded KS template → provisioner stamps
        // SourceTemplateModuleExternalId on the clone's Ideación module, which is what the
        // PartialSync walker matches on when deciding to merge-vs-append.
        var project = await ProvisionFreshProjectAsync("US5-3");

        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsProjectCoordinatorAsync(page, project.CoordinatorEmail, project.CoordinatorPassword, project.Name);
            var projectKsExternalId = await GetProjectKsExternalIdAsync(project.ProjectId);
            await OpenProjectKsDetailAsync(page, projectKsExternalId);
            await SetSyncModeViaUiAsync(page, SyncModeRadio.Partial);

            // Template mutation AFTER the clone was materialized — that's the whole point of
            // PartialSync. A unique Guid-suffixed name keeps this insertion from colliding
            // with parallel tests that read the seeded template's shape (US1-2, US4-2).
            var newTopicName = $"Topic_Nuevo_{Guid.NewGuid():N}"[..22];
            await KnowledgeIntegrationHelpers.AddTopicToTemplateAsync(
                _fixture, SeededKsTemplateExternalId, SeededIdeacionModuleTemplateExternalId, newTopicName);

            await TriggerSyncAndAssertSummaryAsync(page);

            // Post-reload: the newly-appended topic is visible under the matching clone module.
            // TriggerSyncAndAssertSummaryAsync already lands on the reloaded KS detail page, so
            // no re-navigation is needed — just expand the accordion to surface the new row.
            await ExpandModuleByNameAsync(page, "Ideación");
            var appended = page.Locator(".topic-item").Filter(new LocatorFilterOptions { HasText = newTopicName });
            (await appended.CountAsync()).Should().BeGreaterThan(0,
                $"the new template topic '{newTopicName}' must be appended under the clone's Ideación module");

            // Sort-order check — the walker assigns SortOrder = max(existing)+1. The clone's
            // Ideación module starts with "Propuesta de valor" (SortOrder=1), so the appended
            // topic must land at SortOrder=2 regardless of how many tests ran before.
            var appendedSortOrder = await GetTopicSortOrderAsync(projectKsExternalId, newTopicName);
            appendedSortOrder.Should().Be(2,
                "the appended topic's SortOrder must follow the walker's max+1 rule (Propuesta de valor already holds slot 1)");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(PartialSync_AppendsNewTemplateTopic_UnderMatchingParent));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task PartialSync_LocalAddedTopic_Untouched()
    {
        var project = await ProvisionFreshProjectAsync("US5-4");

        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsProjectCoordinatorAsync(page, project.CoordinatorEmail, project.CoordinatorPassword, project.Name);
            var projectKsExternalId = await GetProjectKsExternalIdAsync(project.ProjectId);
            await OpenProjectKsDetailAsync(page, projectKsExternalId);

            // Local-only module (no SourceTemplateModuleExternalId) added BEFORE the sync
            // flips on — establishes the "pre-existing local item" precondition of US5-4.
            var localModuleName = $"M-Local-{Guid.NewGuid():N}"[..18];
            await AddModuleViaUiAsync(page, localModuleName);

            await SetSyncModeViaUiAsync(page, SyncModeRadio.Partial);
            await TriggerSyncAndAssertSummaryAsync(page);

            var localModule = page.Locator(".module-item")
                .Filter(new LocatorFilterOptions { HasText = localModuleName });
            (await localModule.CountAsync()).Should().Be(1,
                $"the locally-added module '{localModuleName}' must survive sync exactly once");

            (await localModule.Locator("span.badge")
                .Filter(new LocatorFilterOptions { HasText = "Local" })
                .CountAsync())
                .Should().BeGreaterThan(0,
                    "the local module must still render the 'Local' origin badge — sync never stamps a template ref on existing local items");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(PartialSync_LocalAddedTopic_Untouched));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task PartialSync_LocallyRenamedTopic_RetainsLocalName()
    {
        var project = await ProvisionFreshProjectAsync("US5-5");

        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsProjectCoordinatorAsync(page, project.CoordinatorEmail, project.CoordinatorPassword, project.Name);
            var projectKsExternalId = await GetProjectKsExternalIdAsync(project.ProjectId);
            await OpenProjectKsDetailAsync(page, projectKsExternalId);

            // Rename the cloned-from-template topic "Propuesta de valor" to a unique local
            // name. After sync, ApplyPartialSync on the matching-by-SourceTemplateTopicExternalId
            // cloneTopic must NOT mutate Name (walker only appends children; never writes back).
            await ExpandModuleByNameAsync(page, "Ideación");
            var propuesta = page.Locator(".topic-item")
                .Filter(new LocatorFilterOptions { HasText = "Propuesta de valor" }).First;
            await propuesta.Locator("button.accordion-button").First.ClickAsync();

            var renamedTopicName = $"PV-Local-{Guid.NewGuid():N}"[..22];
            await propuesta.Locator("[data-action='edit-topic']").First.ClickAsync();
            await WaitForModalAsync(page);
            await page.FillAsync("#tp-name", renamedTopicName);
            await KnowledgeTestHelpers.SubmitModalAndWaitReloadAsync(page);

            await SetSyncModeViaUiAsync(page, SyncModeRadio.Partial);
            await TriggerSyncAndAssertSummaryAsync(page);

            await ExpandModuleByNameAsync(page, "Ideación");

            var renamed = page.Locator(".topic-item")
                .Filter(new LocatorFilterOptions { HasText = renamedTopicName });
            (await renamed.CountAsync()).Should().BeGreaterThan(0,
                "the locally-renamed topic must retain its new Spanish name post-sync");

            // DB-level backstop: no Topic row on this clone still carries the template-side name.
            // A UI-level substring scan can't disambiguate against the seeded Subject description
            // "Formular la propuesta de valor." which Playwright's case-insensitive HasText would
            // match — the clone's Topic.Name is the invariant we actually care about.
            (await CountTopicsWithNameAsync(projectKsExternalId, "Propuesta de valor"))
                .Should().Be(0,
                    "the template-side name must NOT reappear on the clone — sync never reverts local edits");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(PartialSync_LocallyRenamedTopic_RetainsLocalName));
            await page.Context.DisposeAsync();
        }
    }

    // Static helpers (pure DOM orchestration — no fixture state).
    private static async Task WaitForModalAsync(IPage page)
    {
        await page.Locator("#knowledgeModal.show").WaitForAsync(new LocatorWaitForOptions { Timeout = 5000 });
    }

    private static async Task SetSyncModeViaUiAsync(IPage page, SyncModeRadio mode)
    {
        // Tabler's .btn-check radios are visually hidden (opacity:0, pointer-events:none);
        // Playwright CheckAsync() would fail actionability. Click the associated <label>.
        var labelSelector = mode == SyncModeRadio.Partial
            ? "label[for='sync-partial']"
            : "label[for='sync-disconnected']";
        await page.Locator(labelSelector).ClickAsync();
        var saveBtn = page.Locator("[data-action='save-sync-mode']");
        await KnowledgeTestHelpers.ClickAndWaitForReloadAsync(page, saveBtn);
    }

    private static async Task AddModuleViaUiAsync(IPage page, string moduleName)
    {
        await page.Locator("[data-action='add-module']").First.ClickAsync();
        await WaitForModalAsync(page);
        await page.FillAsync("#mod-name", moduleName);
        await KnowledgeTestHelpers.SubmitModalAndWaitReloadAsync(page);
    }

    private static async Task ExpandModuleByNameAsync(IPage page, string moduleName)
    {
        var module = page.Locator(".module-item").Filter(new LocatorFilterOptions { HasText = moduleName }).First;
        await module.Locator("button.accordion-button").First.ClickAsync();
    }

    private static async Task TriggerSyncAndAssertSummaryAsync(IPage page)
    {
        // syncFromTemplate() in project-structure-editor.js pops confirm() before POSTing,
        // renders the Bootstrap toast on success, and schedules window.location.reload() 1200ms
        // later. We need to (a) capture the toast BEFORE reload wipes the DOM, and (b) wait
        // for the reload so follow-up assertions read the post-sync tree. Stamp-and-wait +
        // toast-attach-wait cover both halves atomically.
        page.Dialog += async (_, dialog) => await dialog.AcceptAsync();
        await page.EvaluateAsync("document.documentElement.setAttribute('data-e2e-pre-reload', '1')");
        await page.Locator("[data-action='sync-from-template']").ClickAsync();

        var toast = page.Locator("#toastContainer .toast")
            .Filter(new LocatorFilterOptions { HasText = SyncSummaryToastSubstring });
        await toast.WaitForAsync(new LocatorWaitForOptions
        {
            Timeout = 5000,
            State = WaitForSelectorState.Attached,
        });

        await page.WaitForFunctionAsync(
            "() => !document.documentElement.hasAttribute('data-e2e-pre-reload')",
            null,
            new PageWaitForFunctionOptions { Timeout = 15000 });
        await page.WaitForLoadStateAsync(
            LoadState.NetworkIdle,
            new PageWaitForLoadStateOptions { Timeout = 10000 });
    }

    // Instance helpers (need the fixture for BaseUrl / DI scope).
    private Task LoginAsCoord1Async(IPage page) =>
        KnowledgeTestHelpers.LoginAndSelectAsync(
            page, _fixture.BaseUrl,
            "coord1@test.mentoory.com", "Test123!@#",
            ContextSelection.FirstEnabled);

    private Task LoginAsProjectCoordinatorAsync(IPage page, string email, string password, string projectName) =>
        KnowledgeTestHelpers.LoginAndSelectAsync(
            page, _fixture.BaseUrl,
            email, password,
            ContextSelection.CoordinatorForProject(projectName));

    private async Task OpenProjectKsDetailAsync(IPage page, Guid ksExternalId)
    {
        await page.GotoAsync($"{_fixture.BaseUrl}/Coordination/Knowledge/Projects/{ksExternalId}");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    private async Task<(long ProjectId, long IncubatorId, Guid ExternalId, string Name, string CoordinatorEmail, string CoordinatorPassword, long CoordinatorUserId)>
        ProvisionFreshProjectAsync(string prefix)
    {
        return await KnowledgeIntegrationHelpers.CreateProjectWithCoordinatorAsync(
            _fixture,
            incubatorName: "Incubadora Alpha",
            ksTemplateExternalId: SeededKsTemplateExternalId,
            projectName: $"E2E-{prefix}-{Guid.NewGuid():N}"[..24]);
    }

    private async Task<Guid> GetProjectKsExternalIdAsync(long projectId)
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KnowledgeDbContext>();
        return await db.KnowledgeStructures
            .AsNoTracking()
            .Where(s => s.ProjectId == projectId)
            .Select(s => s.ExternalId)
            .FirstAsync();
    }

    private async Task<int> GetTopicSortOrderAsync(Guid ksExternalId, string topicName)
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KnowledgeDbContext>();
        return await db.KnowledgeStructures
            .AsNoTracking()
            .Where(s => s.ExternalId == ksExternalId)
            .SelectMany(s => s.Modules)
            .SelectMany(m => m.Topics)
            .Where(t => t.Name == topicName)
            .Select(t => t.SortOrder)
            .FirstAsync();
    }

    private async Task<int> CountTopicsWithNameAsync(Guid ksExternalId, string topicName)
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KnowledgeDbContext>();
        return await db.KnowledgeStructures
            .AsNoTracking()
            .Where(s => s.ExternalId == ksExternalId)
            .SelectMany(s => s.Modules)
            .SelectMany(m => m.Topics)
            .CountAsync(t => t.Name == topicName);
    }
}
