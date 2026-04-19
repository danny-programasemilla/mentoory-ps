using FluentAssertions;
using Mentoory.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Xunit;

namespace Mentoory.Tests.E2E.Tests;

/// <summary>
/// E2E coverage for spec 016 US1 — GlobalAdmin curates the knowledge catalog — as expanded
/// by spec 017 Phase 3 (T009–T018). Scenario-to-method mapping:
///
/// | Spec 017 scenario | Method |
/// |---|---|
/// | US1-1 (partial)   | Templates_PageLoads_ShowsSeededTemplate |
/// | US1-1 (remainder) | Templates_ListSurface_ShowsArchivedStateAndNewButton |
/// | US1-2             | TemplateDetail_PageLoads_ShowsTree |
/// | US1-3             | TemplateDetail_AddNodes_PersistsAfterReload |
/// | US1-4             | TopicPriorityRanges_SaveAndReload_PersistsThreeBands |
/// | US1-5             | TopicPriorityRanges_OverlappingBands_ShowsValidationError |
/// | US1-6             | TemplateModules_Reorder_PersistsAfterReload |
/// | US1-7             | Template_ArchiveAndUnarchive_ReflectsInListToggles |
/// | US1-8             | Template_HardDelete_BlockedWhenProjectCloneExists |
/// | US1-9 (smoke)     | Templates_CoordinatorCannotAccess |
/// | —                 | CreateTemplate_HappyPath_AppearsInList (existing regression) |
/// </summary>
[Collection(E2ETestCollection.Name)]
[Trait("Category", "E2E")]
public class KnowledgeTemplatesTests
{
    private readonly PlaywrightFixture _fixture;

    public KnowledgeTemplatesTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Templates_PageLoads_ShowsSeededTemplate()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsGlobalAdminAsync(page);
            await page.GotoAsync($"{_fixture.BaseUrl}/Coordination/Knowledge/Templates");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            page.Url.Should().Contain("/Coordination/Knowledge/Templates",
                "GlobalAdmin should reach the templates list without redirects");

            var content = await page.ContentAsync();
            content.Should().Contain("Emprendimiento Básico",
                "the seeded 'Emprendimiento Básico' template must be listed");

            var seededRow = page.Locator("tr").Filter(new LocatorFilterOptions
            {
                HasText = "Emprendimiento Básico",
            }).First;
            (await seededRow.Locator("span.badge").Filter(new LocatorFilterOptions { HasText = "Activo" }).CountAsync())
                .Should().Be(1, "the seeded template is active — the Estado column must render the 'Activo' badge");

            var newTemplateLink = page.Locator("a").Filter(new LocatorFilterOptions { HasText = "Crear plantilla" });
            (await newTemplateLink.CountAsync()).Should().BeGreaterThan(0,
                "a 'Crear plantilla' action must be reachable from the list header");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Templates_PageLoads_ShowsSeededTemplate));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task Templates_ListSurface_ShowsArchivedStateAndNewButton()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsGlobalAdminAsync(page);
            await page.GotoAsync($"{_fixture.BaseUrl}/Coordination/Knowledge/Templates");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var toggle = page.Locator("input[type='checkbox'][name='includeArchived']");
            (await toggle.CountAsync()).Should().Be(1,
                "list header must expose a 'Mostrar archivados' toggle (filter by archived state)");

            var toggleLabel = page.Locator("label.form-switch").Filter(new LocatorFilterOptions
            {
                HasText = "Mostrar archivados",
            });
            (await toggleLabel.CountAsync()).Should().BeGreaterThan(0,
                "the archived-state toggle is labeled 'Mostrar archivados' in Spanish");

            var activoBadge = page.Locator("span.badge.bg-success").Filter(new LocatorFilterOptions
            {
                HasText = "Activo",
            });
            (await activoBadge.CountAsync()).Should().BeGreaterThan(0,
                "active templates must surface an 'Activo' badge in the Estado column");

            var newTemplateLink = page.Locator("a.btn").Filter(new LocatorFilterOptions
            {
                HasText = "Crear plantilla",
            });
            (await newTemplateLink.CountAsync()).Should().BeGreaterThan(0,
                "the header must expose a primary 'Crear plantilla' CTA");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Templates_ListSurface_ShowsArchivedStateAndNewButton));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task TemplateDetail_PageLoads_ShowsTree()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsGlobalAdminAsync(page);
            await OpenSeededTemplateDetailAsync(page);

            page.Url.Should().MatchRegex(@"/Coordination/Knowledge/Templates/[0-9a-fA-F-]{36}",
                "template detail URL must include the template's ExternalId");

            var content = await page.ContentAsync();
            content.Should().Contain("Ideación",
                "seeded Module 'Ideación' should appear in the detail tree");

            // Expand module → topic to reach Subject/Resource nodes (Bootstrap collapse).
            await page.Locator("[data-module-id] button.accordion-button").First.ClickAsync();
            await page.Locator("[data-topic-id] button.accordion-button").First.ClickAsync();

            content = await page.ContentAsync();
            content.Should().Contain("Propuesta de valor",
                "seeded TopicTemplate 'Propuesta de valor' should appear in the detail tree");
            content.Should().Contain("Definición",
                "seeded SubjectTemplate 'Definición' should appear in the detail tree");
            content.Should().Contain("Introducción a Propuesta de Valor",
                "at least one seeded ResourceTemplate (the Video) should appear in the tree");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(TemplateDetail_PageLoads_ShowsTree));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task TemplateDetail_AddNodes_PersistsAfterReload()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsGlobalAdminAsync(page);
            var detailUrl = await CreateTestLocalTemplateAndOpenDetailAsync(page, "E2E-US1-3");

            var moduleName = $"M-{Guid.NewGuid():N}"[..16];
            await OpenModalAsync(page, "add-module");
            await page.FillAsync("#mod-name", moduleName);
            await SubmitModalAndWaitReloadAsync(page);

            var moduleRow = page.Locator(".module-item").Filter(new LocatorFilterOptions { HasText = moduleName }).First;
            await moduleRow.Locator("button.accordion-button").ClickAsync();

            var topicName = $"T-{Guid.NewGuid():N}"[..16];
            await moduleRow.Locator("[data-action='add-topic']").ClickAsync();
            await page.FillAsync("#tp-name", topicName);
            await SubmitModalAndWaitReloadAsync(page);

            // Re-open the module after reload — accordion collapses.
            var reloadedModule = page.Locator(".module-item").Filter(new LocatorFilterOptions { HasText = moduleName }).First;
            await reloadedModule.Locator("button.accordion-button").First.ClickAsync();
            var topicRow = reloadedModule.Locator(".topic-item").Filter(new LocatorFilterOptions { HasText = topicName }).First;
            await topicRow.Locator("button.accordion-button").First.ClickAsync();

            var subjectName = $"S-{Guid.NewGuid():N}"[..16];
            await topicRow.Locator("[data-action='add-subject']").First.ClickAsync();
            await page.FillAsync("#sj-name", subjectName);
            await SubmitModalAndWaitReloadAsync(page);

            // Re-expand to add the resource under the freshly-created subject.
            var reloadedModule2 = page.Locator(".module-item").Filter(new LocatorFilterOptions { HasText = moduleName }).First;
            await reloadedModule2.Locator("button.accordion-button").First.ClickAsync();
            var reloadedTopic = reloadedModule2.Locator(".topic-item").Filter(new LocatorFilterOptions { HasText = topicName }).First;
            await reloadedTopic.Locator("button.accordion-button").First.ClickAsync();
            var subjectRow = reloadedTopic.Locator(".subject-item").Filter(new LocatorFilterOptions { HasText = subjectName }).First;

            var resourceTitle = $"R-{Guid.NewGuid():N}"[..16];
            await subjectRow.Locator("[data-action='add-resource']").First.ClickAsync();
            await page.FillAsync("#rs-title", resourceTitle);
            await page.FillAsync("#rs-url", "https://example.com/video");
            await page.SelectOptionAsync("#rs-type", "0"); // Video
            await SubmitModalAndWaitReloadAsync(page);

            // Final verification — reload once more and confirm all four nodes exist.
            await page.GotoAsync(detailUrl);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var body = await page.ContentAsync();
            body.Should().Contain(moduleName, "the added module must survive full-page reload");
            body.Should().Contain(topicName, "the added topic must survive full-page reload");
            body.Should().Contain(subjectName, "the added subject must survive full-page reload");
            body.Should().Contain(resourceTitle, "the added resource must survive full-page reload");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(TemplateDetail_AddNodes_PersistsAfterReload));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task TopicPriorityRanges_SaveAndReload_PersistsThreeBands()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsGlobalAdminAsync(page);
            var (detailUrl, topicId) = await PrepareTopicOnTestLocalTemplateAsync(page, "E2E-US1-4");

            await SetPriorityRangesAsync(page, topicId, high: (80, 100), medium: (50, 79), low: (0, 49));
            await WaitForRangesToastAsync(page, "Rangos actualizados.");

            await page.GotoAsync(detailUrl);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await ExpandFirstTopicAsync(page);

            var editor = page.Locator($".priority-range-editor[data-topic-id='{topicId}']");
            var highMin = await editor.Locator("[data-band='high'] [data-field='min']").InputValueAsync();
            var highMax = await editor.Locator("[data-band='high'] [data-field='max']").InputValueAsync();
            var medMin = await editor.Locator("[data-band='medium'] [data-field='min']").InputValueAsync();
            var medMax = await editor.Locator("[data-band='medium'] [data-field='max']").InputValueAsync();
            var lowMin = await editor.Locator("[data-band='low'] [data-field='min']").InputValueAsync();
            var lowMax = await editor.Locator("[data-band='low'] [data-field='max']").InputValueAsync();

            decimal.Parse(highMin, System.Globalization.CultureInfo.InvariantCulture).Should().Be(80);
            decimal.Parse(highMax, System.Globalization.CultureInfo.InvariantCulture).Should().Be(100);
            decimal.Parse(medMin, System.Globalization.CultureInfo.InvariantCulture).Should().Be(50);
            decimal.Parse(medMax, System.Globalization.CultureInfo.InvariantCulture).Should().Be(79);
            decimal.Parse(lowMin, System.Globalization.CultureInfo.InvariantCulture).Should().Be(0);
            decimal.Parse(lowMax, System.Globalization.CultureInfo.InvariantCulture).Should().Be(49);
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(TopicPriorityRanges_SaveAndReload_PersistsThreeBands));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task TopicPriorityRanges_OverlappingBands_ShowsValidationError()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsGlobalAdminAsync(page);
            var (_, topicId) = await PrepareTopicOnTestLocalTemplateAsync(page, "E2E-US1-5");

            // First save a valid set so we can confirm it's preserved after the overlap rejection.
            // waitForReload: true awaits handleResponse's window.location.reload() via sentinel.
            await SetPriorityRangesAsync(page, topicId, high: (80, 100), medium: (50, 79), low: (0, 49), waitForReload: true);
            await ExpandFirstTopicAsync(page);

            // Overlap is rejected client-side: fields persist, no POST, no reload.
            await SetPriorityRangesAsync(page, topicId, high: (70, 100), medium: (65, 80), low: (0, 60));
            var showed = await KnowledgeTestHelpers.WaitForSpanishMessageAsync(
                page, "Los rangos de prioridad se solapan.", timeoutMs: 5000);
            showed.Should().BeTrue(
                "overlapping bands must surface the Spanish 'Los rangos de prioridad se solapan.' toast");

            // Assert previously-saved bands are preserved (reload and re-read).
            await page.ReloadAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await ExpandFirstTopicAsync(page);

            var editor = page.Locator($".priority-range-editor[data-topic-id='{topicId}']");
            var highMin = await editor.Locator("[data-band='high'] [data-field='min']").InputValueAsync();
            decimal.Parse(highMin, System.Globalization.CultureInfo.InvariantCulture).Should().Be(80,
                "the previously-saved High min=80 must survive the rejected overlap save");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(TopicPriorityRanges_OverlappingBands_ShowsValidationError));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task TemplateModules_Reorder_PersistsAfterReload()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsGlobalAdminAsync(page);
            var detailUrl = await CreateTestLocalTemplateAndOpenDetailAsync(page, "E2E-US1-6");

            var moduleA = $"A-{Guid.NewGuid():N}"[..16];
            var moduleB = $"B-{Guid.NewGuid():N}"[..16];
            await AddModuleAsync(page, moduleA);
            await AddModuleAsync(page, moduleB);

            // Resolve ExternalIds from the rendered DOM.
            var ids = await page.Locator("[data-module-id]").EvaluateAllAsync<string[]>(
                "nodes => nodes.map(n => n.getAttribute('data-module-id'))");
            ids.Should().HaveCountGreaterThanOrEqualTo(2);
            var moduleAId = await page.Locator(".module-item").Filter(new LocatorFilterOptions { HasText = moduleA })
                .First.GetAttributeAsync("data-module-id");
            var moduleBId = await page.Locator(".module-item").Filter(new LocatorFilterOptions { HasText = moduleB })
                .First.GetAttributeAsync("data-module-id");

            // No drag-drop UI exists today — reorder endpoint is API-only. Drive it via
            // fetch() from the page context (uses the existing antiforgery cookie/token).
            var templateId = page.Url.Split('/').Last();
            var reorderOk = await page.EvaluateAsync<bool>($@"
                async () => {{
                    const token = document.querySelector('input[name=""__RequestVerificationToken""]').value;
                    const r = await fetch('/Coordination/Knowledge/Templates/{templateId}/Modules/Reorder', {{
                        method: 'POST',
                        headers: {{ 'Content-Type': 'application/json', 'RequestVerificationToken': token }},
                        body: JSON.stringify({{ externalIds: ['{moduleBId}', '{moduleAId}'] }})
                    }});
                    return r.ok;
                }}");
            reorderOk.Should().BeTrue("reorder endpoint must accept the new order without error");

            await page.GotoAsync(detailUrl);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var moduleNamesInOrder = await page.Locator(".module-item strong").EvaluateAllAsync<string[]>(
                "nodes => nodes.map(n => n.textContent.trim())");
            var posA = Array.IndexOf(moduleNamesInOrder, moduleA);
            var posB = Array.IndexOf(moduleNamesInOrder, moduleB);
            posA.Should().BeGreaterThan(-1, "moduleA must still be in the list");
            posB.Should().BeGreaterThan(-1, "moduleB must still be in the list");
            posB.Should().BeLessThan(posA, "after reorder, moduleB should appear before moduleA");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(TemplateModules_Reorder_PersistsAfterReload));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task Template_ArchiveAndUnarchive_ReflectsInListToggles()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsGlobalAdminAsync(page);
            var templateName = $"E2E-US1-7-{Guid.NewGuid():N}"[..24];
            await CreateTemplateAsync(page, templateName);

            // Accept the archive-confirm dialog via a one-shot handler.
            page.Dialog += async (_, dialog) => await dialog.AcceptAsync();

            var archiveBtn = page.Locator("tr").Filter(new LocatorFilterOptions { HasText = templateName })
                .First.Locator("[data-action='archive-template']");
            await ClickAndWaitForReloadAsync(page, archiveBtn);

            // Default view should now hide the archived row.
            var rowAfterArchive = page.Locator("tr").Filter(new LocatorFilterOptions { HasText = templateName });
            (await rowAfterArchive.CountAsync()).Should().Be(0,
                "archived templates must not appear in the default (active-only) list view");

            // Navigate to the archived-included view — sidesteps the checkbox `onchange` form-submit
            // race (same class of issue as window.location.reload() — NetworkIdle returns too early).
            await page.GotoAsync($"{_fixture.BaseUrl}/Coordination/Knowledge/Templates?includeArchived=true");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var archivedRow = page.Locator("tr").Filter(new LocatorFilterOptions { HasText = templateName }).First;
            (await archivedRow.CountAsync()).Should().BeGreaterThan(0,
                "enabling 'Mostrar archivados' must re-surface the archived template");
            (await archivedRow.Locator("span.badge").Filter(new LocatorFilterOptions { HasText = "Archivado" }).CountAsync())
                .Should().BeGreaterThan(0, "archived row must carry the 'Archivado' badge in the Estado column");

            var unarchiveBtn = archivedRow.Locator("[data-action='unarchive-template']");
            await ClickAndWaitForReloadAsync(page, unarchiveBtn);

            // Back to the default (active-only) view to confirm unarchive surfaced the restored row.
            await page.GotoAsync($"{_fixture.BaseUrl}/Coordination/Knowledge/Templates");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var restored = page.Locator("tr").Filter(new LocatorFilterOptions { HasText = templateName }).First;
            (await restored.CountAsync()).Should().BeGreaterThan(0,
                "unarchiving must restore the template to the default view");
            (await restored.Locator("span.badge").Filter(new LocatorFilterOptions { HasText = "Activo" }).CountAsync())
                .Should().BeGreaterThan(0, "restored row must carry the 'Activo' badge again");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Template_ArchiveAndUnarchive_ReflectsInListToggles));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task Template_HardDelete_BlockedWhenProjectCloneExists()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsGlobalAdminAsync(page);
            await page.GotoAsync($"{_fixture.BaseUrl}/Coordination/Knowledge/Templates");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            page.Dialog += async (_, dialog) => await dialog.AcceptAsync();

            var seededRow = page.Locator("tr").Filter(new LocatorFilterOptions { HasText = "Emprendimiento Básico" }).First;
            await seededRow.Locator("[data-action='delete-template']").ClickAsync();

            // DeleteKnowledgeStructureTemplateHandler surfaces the verbatim:
            // "No se puede eliminar: la plantilla tiene clones en uso por {N} proyecto(s). Archívela en su lugar."
            var blocked = await KnowledgeTestHelpers.WaitForSpanishMessageAsync(
                page, "Archívela en su lugar", timeoutMs: 5000);
            blocked.Should().BeTrue(
                "hard-delete on a template with project clones must surface the Spanish guard 'Archívela en su lugar'");

            // Template must still be in the list.
            await page.ReloadAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            var stillPresent = page.Locator("tr").Filter(new LocatorFilterOptions { HasText = "Emprendimiento Básico" });
            (await stillPresent.CountAsync()).Should().BeGreaterThan(0,
                "the seeded template must remain in the list after the rejected hard-delete");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Template_HardDelete_BlockedWhenProjectCloneExists));
            await page.Context.DisposeAsync();
        }
    }

    [Fact]
    public async Task CreateTemplate_HappyPath_AppearsInList()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAsGlobalAdminAsync(page);
            var templateName = $"E2E KS Template {Guid.NewGuid():N}";
            await CreateTemplateAsync(page, templateName);

            var content = await page.ContentAsync();
            content.Should().Contain(templateName,
                "the newly created template must appear in the list after a successful save");
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(CreateTemplate_HappyPath_AppearsInList));
            await page.Context.DisposeAsync();
        }
    }

    // Smoke check kept as a breadcrumb. Full role×route matrix lives in
    // KnowledgeAuthorizationTests.ProtectedRoutes_CoordinatorDenied_ForTemplateRoutes (T045).
    [Fact]
    public async Task Templates_CoordinatorCannotAccess()
    {
        var page = await _fixture.CreatePageAsync();
        try
        {
            await LoginAndSelectContextAsync(page, "coord1@test.mentoory.com", "Test123!@#");
            var response = await page.GotoAsync($"{_fixture.BaseUrl}/Coordination/Knowledge/Templates");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var reachedTemplates = page.Url.Contains("/Coordination/Knowledge/Templates", StringComparison.OrdinalIgnoreCase)
                && !page.Url.Contains("/Access/Login", StringComparison.OrdinalIgnoreCase);
            if (reachedTemplates)
            {
                var status = response?.Status ?? 200;
                (status >= 400).Should().BeTrue(
                    $"ProjectCoordinator must not be allowed to view the GlobalAdmin-only templates list (got HTTP {status})");
            }
            else
            {
                page.Url.Should().NotContain("/Coordination/Knowledge/Templates",
                    "ProjectCoordinator should be redirected away from the GlobalAdmin-only templates list");
            }
        }
        finally
        {
            await _fixture.TakeScreenshotOnFailureAsync(page, nameof(Templates_CoordinatorCannotAccess));
            await page.Context.DisposeAsync();
        }
    }

    private static async Task OpenModalAsync(IPage page, string action)
    {
        await page.Locator($"[data-action='{action}']").First.ClickAsync();
        await page.Locator("#knowledgeModal.show").WaitForAsync(new LocatorWaitForOptions { Timeout = 5000 });
    }

    // Click a control whose JS handler ultimately calls window.location.reload() (or navigates
    // via window.location.href). Stamps the current <html> so the wait can observe the actual
    // navigation — WaitForLoadStateAsync(NetworkIdle) alone returns immediately when the page
    // is already idle at dispatch time, allowing the subsequent assertion/navigation to race
    // ahead of the POST and cancel it.
    private static async Task ClickAndWaitForReloadAsync(IPage page, ILocator locator)
    {
        await page.EvaluateAsync("document.documentElement.setAttribute('data-e2e-pre-reload', '1')");
        await locator.ClickAsync();
        await page.WaitForFunctionAsync(
            "() => !document.documentElement.hasAttribute('data-e2e-pre-reload')",
            null,
            new PageWaitForFunctionOptions { Timeout = 15_000 });
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 10_000 });
    }

    private static Task SubmitModalAndWaitReloadAsync(IPage page) =>
        ClickAndWaitForReloadAsync(page, page.Locator("#knowledgeModalSubmit"));

    private static async Task SetPriorityRangesAsync(
        IPage page,
        string topicId,
        (decimal Min, decimal Max) high,
        (decimal Min, decimal Max) medium,
        (decimal Min, decimal Max) low,
        bool waitForReload = false)
    {
        var editor = page.Locator($".priority-range-editor[data-topic-id='{topicId}']");
        await editor.Locator("[data-band='high'] [data-field='min']").FillAsync(high.Min.ToString(System.Globalization.CultureInfo.InvariantCulture));
        await editor.Locator("[data-band='high'] [data-field='max']").FillAsync(high.Max.ToString(System.Globalization.CultureInfo.InvariantCulture));
        await editor.Locator("[data-band='medium'] [data-field='min']").FillAsync(medium.Min.ToString(System.Globalization.CultureInfo.InvariantCulture));
        await editor.Locator("[data-band='medium'] [data-field='max']").FillAsync(medium.Max.ToString(System.Globalization.CultureInfo.InvariantCulture));
        await editor.Locator("[data-band='low'] [data-field='min']").FillAsync(low.Min.ToString(System.Globalization.CultureInfo.InvariantCulture));
        await editor.Locator("[data-band='low'] [data-field='max']").FillAsync(low.Max.ToString(System.Globalization.CultureInfo.InvariantCulture));

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

    private static async Task WaitForRangesToastAsync(IPage page, string substring)
    {
        var showed = await KnowledgeTestHelpers.WaitForSpanishMessageAsync(page, substring, timeoutMs: 5000);
        showed.Should().BeTrue($"expected Spanish toast containing '{substring}'");
    }

    private static async Task ExpandFirstTopicAsync(IPage page)
    {
        await page.Locator(".module-item button.accordion-button").First.ClickAsync();
        await page.Locator(".topic-item button.accordion-button").First.ClickAsync();
    }

    private Task LoginAsGlobalAdminAsync(IPage page) =>
        KnowledgeTestHelpers.LoginAndSelectAsync(
            page, _fixture.BaseUrl, "multirole@test.mentoory.com", "Test123!@#", ContextSelection.GlobalAdmin);

    private Task LoginAndSelectContextAsync(IPage page, string email, string password) =>
        KnowledgeTestHelpers.LoginAndSelectAsync(
            page, _fixture.BaseUrl, email, password, ContextSelection.FirstEnabled);

    private async Task OpenSeededTemplateDetailAsync(IPage page)
    {
        await page.GotoAsync($"{_fixture.BaseUrl}/Coordination/Knowledge/Templates");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var row = page.Locator("tr").Filter(new LocatorFilterOptions { HasText = "Emprendimiento Básico" }).First;
        await row.WaitForAsync(new LocatorWaitForOptions { Timeout = 5000 });
        await row.Locator("a[href*='/Templates/']").First.ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    private async Task<string> CreateTestLocalTemplateAndOpenDetailAsync(IPage page, string prefix)
    {
        var templateName = $"{prefix}-{Guid.NewGuid():N}"[..24];
        await CreateTemplateAsync(page, templateName);

        var row = page.Locator("tr").Filter(new LocatorFilterOptions { HasText = templateName }).First;
        await row.Locator("a[href*='/Templates/']").First.ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        return page.Url;
    }

    private async Task CreateTemplateAsync(IPage page, string templateName)
    {
        await page.GotoAsync($"{_fixture.BaseUrl}/Coordination/Knowledge/Templates/Create");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.FillAsync("input[name='Name']", templateName);

        var description = page.Locator("textarea[name='Description'], input[name='Description']").First;
        if (await description.CountAsync() > 0)
        {
            await description.FillAsync("E2E test template");
        }

        await page.Locator("button[type='submit']").Filter(new LocatorFilterOptions
        {
            HasText = "Crear plantilla",
        }).First.ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await page.GotoAsync($"{_fixture.BaseUrl}/Coordination/Knowledge/Templates");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    private async Task AddModuleAsync(IPage page, string moduleName)
    {
        await OpenModalAsync(page, "add-module");
        await page.FillAsync("#mod-name", moduleName);
        await SubmitModalAndWaitReloadAsync(page);
    }

    private async Task<(string DetailUrl, string TopicId)> PrepareTopicOnTestLocalTemplateAsync(IPage page, string prefix)
    {
        var detailUrl = await CreateTestLocalTemplateAndOpenDetailAsync(page, prefix);

        var moduleName = $"M-{Guid.NewGuid():N}"[..16];
        await AddModuleAsync(page, moduleName);

        var moduleItem = page.Locator(".module-item").Filter(new LocatorFilterOptions { HasText = moduleName }).First;
        await moduleItem.Locator("button.accordion-button").First.ClickAsync();

        var topicName = $"T-{Guid.NewGuid():N}"[..16];
        await moduleItem.Locator("[data-action='add-topic']").ClickAsync();
        await page.FillAsync("#tp-name", topicName);
        await SubmitModalAndWaitReloadAsync(page);

        var module2 = page.Locator(".module-item").Filter(new LocatorFilterOptions { HasText = moduleName }).First;
        await module2.Locator("button.accordion-button").First.ClickAsync();
        var topicItem = module2.Locator(".topic-item").Filter(new LocatorFilterOptions { HasText = topicName }).First;
        await topicItem.Locator("button.accordion-button").First.ClickAsync();

        var topicId = await topicItem.GetAttributeAsync("data-topic-id");
        topicId.Should().NotBeNullOrEmpty("the topic must expose its ExternalId via data-topic-id");
        return (detailUrl, topicId!);
    }
}
