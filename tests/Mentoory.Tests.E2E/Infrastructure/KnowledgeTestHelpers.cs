using Microsoft.Playwright;

namespace Mentoory.Tests.E2E.Infrastructure;

/// <summary>
/// Shared E2E helpers extracted from the per-file login/context-select duplication across
/// KnowledgeTemplatesTests, KnowledgeProjectStructureTests, ProjectCreationTests, and
/// AvailableProjectsTests. Contract: <c>specs/017-knowledge-e2e-tests/contracts/e2e-helpers.md</c>.
/// </summary>
public static class KnowledgeTestHelpers
{
    private const int DropdownWaitMs = 10_000;
    private const int ConfirmButtonWaitMs = 15_000;
    private const int DefaultSpanishMessageWaitMs = 5_000;

    /// <summary>
    /// POSTs the login form with <paramref name="email"/> and <paramref name="password"/>.
    /// Does NOT proceed through context-select; the caller handles either landing directly
    /// on the home dashboard (single-context user) or invoking <see cref="SelectContextAsync"/>.
    /// </summary>
    public static async Task LoginAsync(IPage page, string baseUrl, string email, string password)
    {
        await page.GotoAsync($"{baseUrl}/Access/Login");
        await page.FillAsync("input[name='Email']", email);
        await page.FillAsync("input[name='Password']", password);
        await page.ClickAsync("button[type='submit']");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await page.WaitForURLAsync(
            url => !url.Contains("/Access/Login"),
            new PageWaitForURLOptions { Timeout = DropdownWaitMs });
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Drives the /Context/Select page according to <paramref name="selection"/>. Each
    /// non-null label is matched by case-insensitive substring against the option text;
    /// null labels fall back to the first enabled option. Silent no-op if the page is not
    /// currently on /Context/Select (i.e., the login already routed a single-context user
    /// directly to the dashboard).
    /// </summary>
    public static async Task SelectContextAsync(IPage page, ContextSelection selection)
    {
        if (!page.Url.Contains("/Context/Select"))
        {
            return;
        }

        var roleDropdown = page.Locator("[data-mode='page'] [data-cs='role']");
        await WaitForDropdownPopulatedAsync(page, roleDropdown);
        if (await roleDropdown.IsEnabledAsync())
        {
            await SelectByLabelOrFirstEnabledAsync(roleDropdown, selection.RoleLabel);
        }

        var incubatorDropdown = page.Locator("[data-mode='page'] [data-cs='incubator']");
        await WaitForDropdownPopulatedAsync(page, incubatorDropdown);
        if (await incubatorDropdown.IsEnabledAsync())
        {
            await SelectByLabelOrFirstEnabledAsync(incubatorDropdown, selection.IncubatorName);
        }

        var projectDropdown = page.Locator("[data-mode='page'] [data-cs='project']");
        if (await projectDropdown.CountAsync() > 0 && await projectDropdown.IsEnabledAsync())
        {
            await WaitForDropdownPopulatedAsync(page, projectDropdown);
            await SelectByLabelOrFirstEnabledAsync(projectDropdown, selection.ProjectName);
        }

        var confirmBtn = page.Locator("[data-mode='page'] [data-cs='confirm']");
        await Assertions.Expect(confirmBtn).ToBeEnabledAsync(new() { Timeout = ConfirmButtonWaitMs });
        await confirmBtn.ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Convenience: <see cref="LoginAsync"/> then <see cref="SelectContextAsync"/>.
    /// </summary>
    public static async Task LoginAndSelectAsync(
        IPage page,
        string baseUrl,
        string email,
        string password,
        ContextSelection selection)
    {
        await LoginAsync(page, baseUrl, email, password);
        await SelectContextAsync(page, selection);
    }

    /// <summary>
    /// Clicks a control whose JS handler ultimately calls <c>window.location.reload()</c>
    /// (or a delayed equivalent) and reliably waits for the subsequent navigation. Stamps the
    /// <c>&lt;html&gt;</c> element before the click and waits for the stamp to disappear —
    /// <see cref="IPage.WaitForLoadStateAsync"/> alone returns immediately when the page is
    /// already idle at dispatch time, which races the reload.
    /// </summary>
    public static async Task ClickAndWaitForReloadAsync(IPage page, ILocator locator)
    {
        await page.EvaluateAsync("document.documentElement.setAttribute('data-e2e-pre-reload', '1')");
        await locator.ClickAsync();
        await page.WaitForFunctionAsync(
            "() => !document.documentElement.hasAttribute('data-e2e-pre-reload')",
            null,
            new PageWaitForFunctionOptions { Timeout = 15_000 });
        await page.WaitForLoadStateAsync(
            LoadState.NetworkIdle,
            new PageWaitForLoadStateOptions { Timeout = 10_000 });
    }

    /// <summary>
    /// Submits the shared <c>#knowledgeModal</c> used by the template- and project-structure
    /// editors and waits for the editor's success-reload to settle.
    /// </summary>
    public static Task SubmitModalAndWaitReloadAsync(IPage page) =>
        ClickAndWaitForReloadAsync(page, page.Locator("#knowledgeModalSubmit"));

    /// <summary>
    /// Captures a full-page screenshot to <c>screenshots/{testName}_{utc}.png</c>. Mirrors
    /// <see cref="PlaywrightFixture.TakeScreenshotOnFailureAsync"/> for helpers that don't
    /// hold a fixture reference.
    /// </summary>
    public static async Task ScreenshotOnFailureAsync(IPage page, string testName)
    {
        var screenshotDir = Path.Combine(AppContext.BaseDirectory, "screenshots");
        Directory.CreateDirectory(screenshotDir);

        var filePath = Path.Combine(
            screenshotDir,
            $"{testName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png");

        await page.ScreenshotAsync(new PageScreenshotOptions { Path = filePath, FullPage = true });
    }

    /// <summary>
    /// Returns <c>true</c> when the given Spanish substring appears either in a Tabler toast
    /// or in the page body within <paramref name="timeoutMs"/>. Tests call this as
    /// <c>(await WaitForSpanishMessageAsync(...)).Should().BeTrue("...")</c>.
    /// </summary>
    public static async Task<bool> WaitForSpanishMessageAsync(
        IPage page,
        string substring,
        int timeoutMs = DefaultSpanishMessageWaitMs)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);

        while (DateTime.UtcNow < deadline)
        {
            var toastLocator = page.Locator($".toast, .alert, .invalid-feedback, .validation-summary-errors")
                .Filter(new LocatorFilterOptions { HasText = substring });
            if (await toastLocator.CountAsync() > 0)
            {
                return true;
            }

            var body = await page.ContentAsync();
            if (body.Contains(substring, StringComparison.Ordinal))
            {
                return true;
            }

            await Task.Delay(100);
        }

        return false;
    }

    private static async Task WaitForDropdownPopulatedAsync(IPage page, ILocator dropdown)
    {
        await page.WaitForFunctionAsync(
            "sel => sel.options.length > 1",
            await dropdown.ElementHandleAsync(),
            new PageWaitForFunctionOptions { Timeout = DropdownWaitMs });
    }

    private static async Task SelectByLabelOrFirstEnabledAsync(ILocator dropdown, string? label)
    {
        if (!string.IsNullOrEmpty(label))
        {
            var options = await dropdown.Locator("option").EvaluateAllAsync<OptionEntry[]>(
                "nodes => nodes.map(n => ({ Value: n.value, Text: n.textContent }))");
            var match = options.FirstOrDefault(o =>
                !string.IsNullOrEmpty(o.Value)
                && (o.Text?.Contains(label, StringComparison.OrdinalIgnoreCase) ?? false));
            if (!string.IsNullOrEmpty(match.Value))
            {
                await dropdown.SelectOptionAsync(match.Value);
                return;
            }
        }

        await dropdown.SelectOptionAsync(new SelectOptionValue { Index = 1 });
    }

    private record struct OptionEntry(string Value, string? Text);
}

/// <summary>
/// Describes how <see cref="KnowledgeTestHelpers.SelectContextAsync"/> should drive the
/// /Context/Select dropdowns. Null fields fall back to the first enabled option.
/// </summary>
public sealed record ContextSelection(
    string? RoleLabel = null,
    string? IncubatorName = null,
    string? ProjectName = null)
{
    public static ContextSelection FirstEnabled { get; } = new();

    public static ContextSelection GlobalAdmin { get; } = new(RoleLabel: "GlobalAdmin");

    public static ContextSelection CoordinatorForProject(string projectName) =>
        new(RoleLabel: "ProjectCoordinator", ProjectName: projectName);
}
