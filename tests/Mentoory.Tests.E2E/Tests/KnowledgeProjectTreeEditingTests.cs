using FluentAssertions;
using Mentoory.Knowledge.Infrastructure.Persistence;
using Mentoory.Tests.E2E.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using Xunit;

namespace Mentoory.Tests.E2E.Tests;

/// <summary>
/// E2E coverage for spec 017 US4 — Coordinator project-tree editing. Exercises project-side
/// CRUD (add nodes at each hierarchy level), rename-does-not-leak-to-template, priority-range
/// save/overlap, delete-guard against referencing diagnostic questions, and tenant-isolation
/// on the detail route. Non-coordinator-role access is covered in KnowledgeAuthorizationTests.
///
/// | Spec 017 scenario | Method |
/// |---|---|
/// | US4-1            | AddNodesAtEachLevel_PersistsAfterReload |
/// | US4-2            | RenameClonedTopic_PersistsOnCloneOnly |
/// | US4-3            | ProjectTopicPriorityRanges_SaveAndReload_PersistsBands |
/// | US4-4            | ProjectTopicPriorityRanges_OverlappingBands_ShowsValidationError |
/// | US4-5            | DeleteProjectTopic_Referenced_Blocked |
/// | US4-6            | DeleteProjectTopic_Unreferenced_Succeeds |
/// | US4-7            | ProjectTopic_TenantIsolation_CrossProjectReturnsNotFound |
/// </summary>
[Collection(E2ETestCollection.Name)]
[Trait("Category", "E2E")]
public class KnowledgeProjectTreeEditingTests
{
    // Seeded coord1 project KS ExternalId (see 004.SeedTestData.sql § "Proyecto Innovación").
    // Stable GUID, exposed here so the tests can navigate directly to the detail page without
    // first walking the list surface (which would just add a click race for nothing).
    private static readonly Guid SeededProjectKsExternalId =
        new("99999999-9999-9999-9999-999999999901");

    // Seeded Proyecto Norte Uno KS ExternalId (see 004.SeedTestData.sql § "Proyecto Norte Uno").
    // Its KS row exists but has NO modules/topics — which is enough for the US4-7 tenant-isolation
    // check: coord1 must see none of Norte's content at Norte's KS URL.
    private static readonly Guid ForeignProjectKsExternalId =
        new("88888888-8888-8888-8888-888888880002");

    // Seeded global KS template ExternalId (005.SeedKnowledgeData.sql § "Emprendimiento Básico").
    // Used by US4-2's "template unchanged" assertion to open the template detail as GlobalAdmin.
    private static readonly Guid SeededKsTemplateExternalId =
        new("11111111-1111-1111-1111-111111111111");

    private readonly PlaywrightFixture _fixture;

    public KnowledgeProjectTreeEditingTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddNodesAtEachLevel_PersistsAfterReload()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsCoord1Async(page);
            await OpenSeededProjectKsDetailAsync(page);

            var moduleName = $"PM-{Guid.NewGuid():N}"[..16];
            await AddModuleViaUiAsync(page, moduleName);

            var moduleItem = page.Locator(".module-item").Filter(new LocatorFilterOptions { HasText = moduleName }).First;
            await moduleItem.Locator("button.accordion-button").First.ClickAsync();

            var topicName = $"PT-{Guid.NewGuid():N}"[..16];
            await moduleItem.Locator("[data-action='add-topic']").ClickAsync();
            await WaitForModalAsync(page);
            await page.FillAsync("#tp-name", topicName);
            await SubmitModalAndWaitReloadAsync(page);

            await ExpandModuleByNameAsync(page, moduleName);
            var topicItem = page.Locator(".topic-item").Filter(new LocatorFilterOptions { HasText = topicName }).First;
            await topicItem.Locator("button.accordion-button").First.ClickAsync();

            var subjectName = $"PS-{Guid.NewGuid():N}"[..16];
            await topicItem.Locator("[data-action='add-subject']").First.ClickAsync();
            await WaitForModalAsync(page);
            await page.FillAsync("#sj-name", subjectName);
            await SubmitModalAndWaitReloadAsync(page);

            await ExpandModuleByNameAsync(page, moduleName);
            var topicItem2 = page.Locator(".topic-item").Filter(new LocatorFilterOptions { HasText = topicName }).First;
            await topicItem2.Locator("button.accordion-button").First.ClickAsync();
            var subjectItem = topicItem2.Locator(".subject-item").Filter(new LocatorFilterOptions { HasText = subjectName }).First;

            var resourceTitle = $"PR-{Guid.NewGuid():N}"[..16];
            await subjectItem.Locator("[data-action='add-resource']").First.ClickAsync();
            await WaitForModalAsync(page);
            await page.FillAsync("#rs-title", resourceTitle);
            await page.FillAsync("#rs-url", "https://example.com/project-resource");
            await page.SelectOptionAsync("#rs-type", "1"); // Enlace
            await SubmitModalAndWaitReloadAsync(page);

            await ExpandModuleByNameAsync(page, moduleName);
            var finalTopic = page.Locator(".topic-item").Filter(new LocatorFilterOptions { HasText = topicName }).First;
            await finalTopic.Locator("button.accordion-button").First.ClickAsync();

            var body = await page.ContentAsync();
            body.Should().Contain(moduleName, "the added module must survive full-page reload");
            body.Should().Contain(topicName, "the added topic must survive full-page reload");
            body.Should().Contain(subjectName, "the added subject must survive full-page reload");
            body.Should().Contain(resourceTitle, "the added resource must survive full-page reload");

            // Clone-only invariant: each new node carries the green "Local" badge, not the
            // blue "Origen: plantilla" badge. Scoping to the module row transitively covers
            // the topic/subject/resource since they render inside the module's accordion body.
            var finalModule = page.Locator(".module-item").Filter(new LocatorFilterOptions { HasText = moduleName }).First;
            (await finalModule.Locator("span.badge").Filter(new LocatorFilterOptions { HasText = "Local" }).CountAsync())
                .Should().BeGreaterThan(0,
                    "locally-added nodes must render the 'Local' origin badge (source refs are NULL)");
            (await finalModule.Locator("span.badge").Filter(new LocatorFilterOptions { HasText = "Origen: plantilla" }).CountAsync())
                .Should().Be(0,
                    "locally-added nodes must NOT carry a template-origin badge");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(AddNodesAtEachLevel_PersistsAfterReload));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task RenameClonedTopic_PersistsOnCloneOnly()
    {
        var renamedTopicName = $"Equipo-Renombrado-{Guid.NewGuid():N}"[..28];

        var coordPage = await _fixture.CreatePageAsync();
        IPage? adminPage = null;
        try
        {
            await LoginAsCoord1Async(coordPage);
            await OpenSeededProjectKsDetailAsync(coordPage);
            await ExpandModuleByNameAsync(coordPage, "Diagnóstico");

            var equipoTopic = coordPage.Locator(".topic-item").Filter(new LocatorFilterOptions { HasText = "Equipo" }).First;
            await equipoTopic.Locator("button.accordion-button").First.ClickAsync();
            await equipoTopic.Locator("[data-action='edit-topic']").First.ClickAsync();
            await WaitForModalAsync(coordPage);

            await coordPage.FillAsync("#tp-name", renamedTopicName);
            await SubmitModalAndWaitReloadAsync(coordPage);

            var coordBody = await coordPage.ContentAsync();
            coordBody.Should().Contain(renamedTopicName,
                "the renamed topic must render with its new name on the project tree");

            // Second browser context as GlobalAdmin — proves the rename stayed on the clone
            // and did not mutate the shared template row.
            adminPage = await _fixture.CreatePageAsync();
            await KnowledgeTestHelpers.LoginAndSelectAsync(
                adminPage, _fixture.BaseUrl,
                "multirole@test.mentoory.com", "Test123!@#",
                ContextSelection.GlobalAdmin);

            await adminPage.GotoAsync($"{_fixture.BaseUrl}/Coordination/Knowledge/Templates/{SeededKsTemplateExternalId}");
            await adminPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
            // Expand the Ideación module to force the Propuesta de valor row into the DOM.
            await adminPage.Locator("[data-module-id] button.accordion-button").First.ClickAsync();

            var templateBody = await adminPage.ContentAsync();
            templateBody.Should().Contain("Propuesta de valor",
                "the template's original TopicTemplate name must still be present — renames are clone-local");
            templateBody.Should().NotContain(renamedTopicName,
                "the coordinator's rename must NOT leak into the global KS template tree");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(coordPage, nameof(RenameClonedTopic_PersistsOnCloneOnly) + "-coord");
            if (adminPage is not null)
            {
                await _fixture.TakeScreenshotOnFailureAsync(adminPage, nameof(RenameClonedTopic_PersistsOnCloneOnly) + "-admin");
                await adminPage.Context.DisposeAsync();
            }

            await coordPage.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task ProjectTopicPriorityRanges_SaveAndReload_PersistsBands()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsCoord1Async(page);
            await OpenSeededProjectKsDetailAsync(page);

            var (moduleName, topicName, topicId) = await AddTestLocalTopicAsync(page, "US4-3");

            await SetPriorityRangesAsync(page, topicId,
                high: (80, 100), medium: (50, 79), low: (0, 49),
                waitForReload: true);

            await ExpandModuleByNameAsync(page, moduleName);
            var topic = page.Locator(".topic-item").Filter(new LocatorFilterOptions { HasText = topicName }).First;
            await topic.Locator("button.accordion-button").First.ClickAsync();

            var editor = page.Locator($".priority-range-editor[data-topic-id='{topicId}']");
            var (highMin, highMax, medMin, medMax, lowMin, lowMax) = await ReadBandsAsync(editor);
            highMin.Should().Be(80);
            highMax.Should().Be(100);
            medMin.Should().Be(50);
            medMax.Should().Be(79);
            lowMin.Should().Be(0);
            lowMax.Should().Be(49);
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(ProjectTopicPriorityRanges_SaveAndReload_PersistsBands));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task ProjectTopicPriorityRanges_OverlappingBands_ShowsValidationError()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsCoord1Async(page);
            await OpenSeededProjectKsDetailAsync(page);

            var (moduleName, topicName, topicId) = await AddTestLocalTopicAsync(page, "US4-4");

            // First save a valid set so we can verify it survives the rejected overlap.
            await SetPriorityRangesAsync(page, topicId,
                high: (80, 100), medium: (50, 79), low: (0, 49),
                waitForReload: true);
            await ExpandModuleByNameAsync(page, moduleName);
            var topic = page.Locator(".topic-item").Filter(new LocatorFilterOptions { HasText = topicName }).First;
            await topic.Locator("button.accordion-button").First.ClickAsync();

            // Overlap: JS rejects client-side (no POST, no reload) and surfaces the exact toast.
            await SetPriorityRangesAsync(page, topicId,
                high: (70, 100), medium: (65, 80), low: (0, 60),
                waitForReload: false);
            var showed = await KnowledgeTestHelpers.WaitForSpanishMessageAsync(
                page, "Los rangos de prioridad se solapan.", timeoutMs: 5000);
            showed.Should().BeTrue(
                "overlapping bands must surface the Spanish 'Los rangos de prioridad se solapan.' toast");

            // Previously-saved bands must remain intact after the rejection.
            await OpenSeededProjectKsDetailAsync(page);
            await ExpandModuleByNameAsync(page, moduleName);
            var topicAgain = page.Locator(".topic-item").Filter(new LocatorFilterOptions { HasText = topicName }).First;
            await topicAgain.Locator("button.accordion-button").First.ClickAsync();

            var editor = page.Locator($".priority-range-editor[data-topic-id='{topicId}']");
            var (highMin, _, _, _, _, _) = await ReadBandsAsync(editor);
            highMin.Should().Be(80,
                "the previously-saved High min=80 must survive the rejected overlap save");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(ProjectTopicPriorityRanges_OverlappingBands_ShowsValidationError));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task DeleteProjectTopic_Referenced_Blocked()
    {
        // Fresh project (not the seeded one) so the provisioner populates
        // SourceTemplateTopicExternalId — CloneFormTemplateHandler's TopicId rewrite requires
        // those back-references, which the hand-coded seed KS doesn't carry.
        var projectName = $"E2E-US4-5-{Guid.NewGuid():N}"[..24];
        var project = await KnowledgeIntegrationHelpers.CreateProjectWithCoordinatorAsync(
            _fixture,
            incubatorName: "Incubadora Alpha",
            ksTemplateExternalId: SeededKsTemplateExternalId,
            projectName: projectName);

        var projectKsExternalId = await GetProjectKsExternalIdAsync(project.ProjectId);

        var boundFormTemplateId = await KnowledgeIntegrationHelpers.GetSeededBoundFormTemplateExternalIdAsync(_fixture);
        var cloneResult = await KnowledgeIntegrationHelpers.CloneFormIntoProjectAsync(
            _fixture, boundFormTemplateId, project.ProjectId, project.IncubatorId);
        cloneResult.IsSuccess.Should().BeTrue(
            "the happy-path clone must succeed so the project topic gains referencing questions");

        var page = await _fixture.CreatePageAsync();
        try
        {
            page.Dialog += async (_, dialog) => await dialog.AcceptAsync();

            await KnowledgeTestHelpers.LoginAndSelectAsync(
                page, _fixture.BaseUrl,
                project.CoordinatorEmail, project.CoordinatorPassword,
                ContextSelection.CoordinatorForProject(project.Name));

            await OpenProjectKsDetailAsync(page, projectKsExternalId);

            // The provisioner clones the "Ideación" module with a "Propuesta de valor" topic —
            // that's exactly the topic the bound FormTemplate's Questions now reference.
            await ExpandModuleByNameAsync(page, "Ideación");
            var topic = page.Locator(".topic-item").Filter(new LocatorFilterOptions { HasText = "Propuesta de valor" }).First;
            await topic.Locator("button.accordion-button").First.ClickAsync();
            await topic.Locator("[data-action='delete-topic']").First.ClickAsync();

            var blocked = await KnowledgeTestHelpers.WaitForSpanishMessageAsync(
                page, "pregunta(s) de diagnóstico", timeoutMs: 5000);
            blocked.Should().BeTrue(
                "delete on a topic referenced by diagnostic questions must surface the Spanish guard "
                + "'el tema está referenciado por N pregunta(s) de diagnóstico'");

            // The topic must still be in the tree after the rejected delete.
            await OpenProjectKsDetailAsync(page, projectKsExternalId);
            await ExpandModuleByNameAsync(page, "Ideación");
            var stillThere = page.Locator(".topic-item").Filter(new LocatorFilterOptions { HasText = "Propuesta de valor" });
            (await stillThere.CountAsync()).Should().BeGreaterThan(0,
                "the referenced topic must remain visible after the blocked delete");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(DeleteProjectTopic_Referenced_Blocked));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task DeleteProjectTopic_Unreferenced_Succeeds()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            page.Dialog += async (_, dialog) => await dialog.AcceptAsync();

            await LoginAsCoord1Async(page);
            await OpenSeededProjectKsDetailAsync(page);

            var (moduleName, topicName, _) = await AddTestLocalTopicAsync(page, "US4-6");

            var topic = page.Locator(".topic-item").Filter(new LocatorFilterOptions { HasText = topicName }).First;
            await topic.Locator("button.accordion-button").First.ClickAsync();
            var deleteBtn = topic.Locator("[data-action='delete-topic']").First;
            await ClickAndWaitForReloadAsync(page, deleteBtn);

            await ExpandModuleByNameAsync(page, moduleName);

            var goneTopic = page.Locator(".topic-item").Filter(new LocatorFilterOptions { HasText = topicName });
            (await goneTopic.CountAsync()).Should().Be(0,
                "an unreferenced topic must disappear from the tree after deletion and reload");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(DeleteProjectTopic_Unreferenced_Succeeds));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task ProjectTopic_TenantIsolation_CrossProjectReturnsNotFound()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsCoord1Async(page);

            var response = await page.GotoAsync(
                $"{_fixture.BaseUrl}/Coordination/Knowledge/Projects/{ForeignProjectKsExternalId}");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            response.Should().NotBeNull("Playwright must surface a Response for any completed navigation");
            var status = response!.Status;
            var body = await page.ContentAsync();

            // Two-sided tenant-isolation check. Either KnowledgeDbContext's incubator-scope
            // query filter blocked the lookup (≥400 / empty-state view), or the page rendered
            // without surfacing any identifier from the foreign KS. The KS Name in the seed
            // ("Estructura de Conocimiento - Proyecto Norte Uno") is a reliable marker — if
            // the filter ever regresses, this substring will appear in the <h2> and fail.
            var blocked = status >= 400;
            var noNameLeak = !body.Contains("Proyecto Norte Uno", StringComparison.Ordinal);
            (blocked || noNameLeak).Should().BeTrue(
                $"coord1 must not reach Proyecto Norte Uno's KS detail (status {status})");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(ProjectTopic_TenantIsolation_CrossProjectReturnsNotFound));
            await page.Context.DisposeAsync();
        }
    }

    private static async Task WaitForModalAsync(IPage page)
    {
        await page.Locator("#knowledgeModal.show").WaitForAsync(new LocatorWaitForOptions { Timeout = 5000 });
    }

    private static Task ClickAndWaitForReloadAsync(IPage page, ILocator locator) =>
        KnowledgeTestHelpers.ClickAndWaitForReloadAsync(page, locator);

    private static Task SubmitModalAndWaitReloadAsync(IPage page) =>
        KnowledgeTestHelpers.SubmitModalAndWaitReloadAsync(page);

    private static async Task AddModuleViaUiAsync(IPage page, string moduleName)
    {
        await page.Locator("[data-action='add-module']").First.ClickAsync();
        await WaitForModalAsync(page);
        await page.FillAsync("#mod-name", moduleName);
        await SubmitModalAndWaitReloadAsync(page);
    }

    private static async Task ExpandModuleByNameAsync(IPage page, string moduleName)
    {
        var module = page.Locator(".module-item").Filter(new LocatorFilterOptions { HasText = moduleName }).First;
        await module.Locator("button.accordion-button").First.ClickAsync();
    }

    private static async Task SetPriorityRangesAsync(
        IPage page,
        string topicId,
        (decimal Min, decimal Max) high,
        (decimal Min, decimal Max) medium,
        (decimal Min, decimal Max) low,
        bool waitForReload)
    {
        var editor = page.Locator($".priority-range-editor[data-topic-id='{topicId}']");
        await editor.Locator("[data-band='high'] [data-field='min']").FillAsync(Fmt(high.Min));
        await editor.Locator("[data-band='high'] [data-field='max']").FillAsync(Fmt(high.Max));
        await editor.Locator("[data-band='medium'] [data-field='min']").FillAsync(Fmt(medium.Min));
        await editor.Locator("[data-band='medium'] [data-field='max']").FillAsync(Fmt(medium.Max));
        await editor.Locator("[data-band='low'] [data-field='min']").FillAsync(Fmt(low.Min));
        await editor.Locator("[data-band='low'] [data-field='max']").FillAsync(Fmt(low.Max));

        var saveBtn = editor.Locator("[data-action='update-topic-ranges']");
        if (waitForReload)
        {
            await ClickAndWaitForReloadAsync(page, saveBtn);
        }
        else
        {
            await saveBtn.ClickAsync();
        }
    }

    private static async Task<(decimal HighMin, decimal HighMax, decimal MedMin, decimal MedMax, decimal LowMin, decimal LowMax)>
        ReadBandsAsync(ILocator editor)
    {
        var highMin = await editor.Locator("[data-band='high'] [data-field='min']").InputValueAsync();
        var highMax = await editor.Locator("[data-band='high'] [data-field='max']").InputValueAsync();
        var medMin = await editor.Locator("[data-band='medium'] [data-field='min']").InputValueAsync();
        var medMax = await editor.Locator("[data-band='medium'] [data-field='max']").InputValueAsync();
        var lowMin = await editor.Locator("[data-band='low'] [data-field='min']").InputValueAsync();
        var lowMax = await editor.Locator("[data-band='low'] [data-field='max']").InputValueAsync();

        return (Parse(highMin), Parse(highMax), Parse(medMin), Parse(medMax), Parse(lowMin), Parse(lowMax));
    }

    private static string Fmt(decimal value) =>
        value.ToString(System.Globalization.CultureInfo.InvariantCulture);

    private static decimal Parse(string value) =>
        decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);

    // Instance helpers (need the fixture for BaseUrl / DI scope).
    private Task LoginAsCoord1Async(IPage page) =>
        KnowledgeTestHelpers.LoginAndSelectAsync(
            page, _fixture.BaseUrl,
            "coord1@test.mentoory.com", "Test123!@#",
            ContextSelection.FirstEnabled);

    private Task OpenSeededProjectKsDetailAsync(IPage page) =>
        OpenProjectKsDetailAsync(page, SeededProjectKsExternalId);

    private async Task OpenProjectKsDetailAsync(IPage page, Guid ksExternalId)
    {
        await page.GotoAsync($"{_fixture.BaseUrl}/Coordination/Knowledge/Projects/{ksExternalId}");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
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

    private async Task<(string ModuleName, string TopicName, string TopicId)>
        AddTestLocalTopicAsync(IPage page, string prefix)
    {
        var moduleName = $"{prefix}-M-{Guid.NewGuid():N}"[..18];
        await AddModuleViaUiAsync(page, moduleName);

        var moduleItem = page.Locator(".module-item").Filter(new LocatorFilterOptions { HasText = moduleName }).First;
        await moduleItem.Locator("button.accordion-button").First.ClickAsync();

        var topicName = $"{prefix}-T-{Guid.NewGuid():N}"[..18];
        await moduleItem.Locator("[data-action='add-topic']").ClickAsync();
        await WaitForModalAsync(page);
        await page.FillAsync("#tp-name", topicName);
        await SubmitModalAndWaitReloadAsync(page);

        await ExpandModuleByNameAsync(page, moduleName);
        var topicItem = page.Locator(".topic-item").Filter(new LocatorFilterOptions { HasText = topicName }).First;
        await topicItem.Locator("button.accordion-button").First.ClickAsync();

        var topicId = await topicItem.GetAttributeAsync("data-topic-id");
        topicId.Should().NotBeNullOrEmpty("the new project topic must expose its ExternalId via data-topic-id");
        return (moduleName, topicName, topicId!);
    }
}
