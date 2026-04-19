# Contract: Shared E2E Helpers

**File**: `tests/Mentoory.Tests.E2E/Infrastructure/KnowledgeTestHelpers.cs` (new)

Shared helpers extracted to eliminate duplication across `KnowledgeTemplatesTests`, `KnowledgeProjectStructureTests`, `ProjectCreationTests`, `AvailableProjectsTests`, and the four new files introduced by this spec.

## Public API

```csharp
namespace Mentoory.Tests.E2E.Infrastructure;

public static class KnowledgeTestHelpers
{
    /// <summary>
    /// POSTs the login form with email + password. Does NOT navigate after login;
    /// the caller is responsible for either landing on the home dashboard or
    /// proceeding through <see cref="SelectContextAsync"/>.
    /// </summary>
    public static Task LoginAsync(IPage page, string baseUrl, string email, string password);

    /// <summary>
    /// Drives the <c>/Context/Select</c> page. Each field in <paramref name="selection"/>
    /// is matched by visible text; null fields fall back to "first enabled option"
    /// (mirroring today's inline pattern). Asserts the confirm button becomes
    /// enabled within 15 seconds before clicking.
    /// </summary>
    public static Task SelectContextAsync(IPage page, ContextSelection selection);

    /// <summary>
    /// Convenience wrapper: <see cref="LoginAsync"/> followed by
    /// <see cref="SelectContextAsync"/>. If the login lands directly on the
    /// dashboard (single-context user), the context-select step is a no-op.
    /// </summary>
    public static Task LoginAndSelectAsync(IPage page, string baseUrl, string email, string password, ContextSelection selection);

    /// <summary>
    /// Captures a full-page screenshot to <c>screenshots/</c> on failure. Filename
    /// pattern: <c>{testName}_{yyyyMMdd_HHmmss}.png</c>. Mirrors the existing
    /// <see cref="PlaywrightFixture.TakeScreenshotOnFailureAsync"/> but is callable
    /// from helpers that don't hold a fixture reference.
    /// </summary>
    public static Task ScreenshotOnFailureAsync(IPage page, string testName);

    /// <summary>
    /// Waits for a Tabler toast containing <paramref name="substring"/> to appear
    /// OR for the page content to contain the substring (covers both inline
    /// validation summaries and toast notifications). Returns when either is
    /// visible; times out after 5 seconds.
    /// </summary>
    public static Task<bool> WaitForSpanishMessageAsync(IPage page, string substring, int timeoutMs = 5000);
}

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
```

## Behavioral Contracts

1. **XHR-dropdown race safety** — `SelectContextAsync` MUST internally call `page.WaitForFunctionAsync("sel => sel.options.length > 1", handle, { Timeout: 10000 })` on every dropdown before selecting an option. No test may call `SelectOptionAsync` directly against `data-cs` selectors.
2. **Timeout defaults** — Every network-idle wait uses Playwright's default (`WaitForLoadStateAsync(LoadState.NetworkIdle)`); every function-wait uses 10 000 ms; every element-wait uses 5 000 ms unless the test explicitly overrides.
3. **Silent-skip contract** — If `SelectContextAsync` is invoked but the login already routed to the home page (no `/Context/Select` segment in the URL), the helper returns immediately without error. Single-context users (e.g., `coord1` after their Coordinator role/incubator auto-select) must pass the same helper call as multirole users.
4. **Role label matching** — When `RoleLabel` is non-null, `SelectContextAsync` finds the `<option>` whose text CONTAINS the label (case-insensitive). This tolerates suffix variations like `"GlobalAdmin (Plataforma)"` in future polish.
5. **No global state** — Helpers MUST NOT hold static state between calls (no caching of login cookies, no memoized Playwright browser). Each call creates its own page context via the fixture.
6. **FluentAssertions-compatible returns** — `WaitForSpanishMessageAsync` returns `Task<bool>` so the test can write `(await WaitForSpanishMessageAsync(...)).Should().BeTrue("...")` with a descriptive `because` string.

## Extraction Scope

**In scope for extraction** (delete from existing files after the new helper is in place):
- `LoginAndSelectContextAsync` in `KnowledgeTemplatesTests.cs:170–225`
- `LoginAsCoordinatorAsync` in `KnowledgeProjectStructureTests.cs:130–180`
- `LoginAndSelectContextAsync` in `ProjectCreationTests.cs:204–243`
- `LoginAndSelectContextAsync` in `AvailableProjectsTests.cs` (similar shape)

**Out of scope** (keep inline):
- Test-specific navigation helpers (e.g., `OpenTemplateDetailAsync`) that are only used within one file.
- Assertion helpers that wrap FluentAssertions one-liners (not worth the indirection).

## Migration Plan for Existing Tests

Migrate test-by-test as part of Phase 2 tasks:

1. Add the new `KnowledgeTestHelpers.cs` file.
2. In each existing file, replace the private `LoginAndSelectContextAsync` call-site with `KnowledgeTestHelpers.LoginAndSelectAsync(page, _fixture.BaseUrl, email, password, ContextSelection.X)`.
3. Delete the now-unused private helper method.
4. Run the E2E suite; no behavioral change expected.

Each migration is a mechanical diff reviewable in isolation.
