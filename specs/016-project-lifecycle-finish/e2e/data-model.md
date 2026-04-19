# Phase 1 Data Model: Fixtures, Page Objects, and Test Class Shape

**Feature**: `016-project-lifecycle-finish/e2e`
**Date**: 2026-04-19

The E2E suite introduces no domain entities. "Data model" here means the test-support types — fixtures, page objects, and test-class shapes — that make the scenario tests readable and the chunks composable. This file is informative: C0 owns their creation. These are the contracts between chunks.

---

## 1. `LifecycleFixtures`

**Purpose**: Seed / advance / deactivate / reset project state for the test, without driving the UI. Owns the fixture-scoped service provider from `PlaywrightFixture`.

**Lifetime**: Instantiated once per test class via shared state on the collection fixture (or a per-class `LifecycleFixtures` constructed from `PlaywrightFixture`). Method `ResetStateAsync` is called at the start of every test's constructor.

**API (contract for C0 to implement; later chunks may extend additively)**:

```csharp
public sealed class LifecycleFixtures
{
    public LifecycleFixtures(PlaywrightFixture host);

    /// <summary>
    /// Deletes every project/incubator created by this suite (name prefix
    /// "e2e-lifecycle-"). Leaves PostDeployment seed data untouched. Called
    /// from every test's constructor / setup.
    /// </summary>
    public Task ResetStateAsync(CancellationToken ct = default);

    /// <summary>
    /// Idempotent: returns the same two incubator external IDs across calls
    /// within a single test-class run, unless ResetStateAsync was called.
    /// </summary>
    public Task<(Guid incubatorA, Guid incubatorB)> EnsureTwoIncubatorsAsync(CancellationToken ct = default);

    /// <summary>
    /// Creates a project in the given incubator and advances it to targetStage
    /// by repeatedly dispatching AdvanceProjectStageCommand via IMediator.
    /// Returns the project's ExternalId. Active = true by default.
    /// </summary>
    public Task<Guid> CreateProjectAsync(
        Guid incubatorExternalId,
        string name,
        StageType targetStage = StageType.Registration,
        bool active = true,
        CancellationToken ct = default);

    /// <summary>
    /// Marks an existing project inactive. Used for the inactive-project toast test.
    /// </summary>
    public Task DeactivateProjectAsync(Guid projectExternalId, CancellationToken ct = default);

    // --- Added by C1 (test #3 "broken-state" scenario) ---

    /// <summary>
    /// TEST-ONLY: forces CurrentStageState = Completed without advancing.
    /// Produces a state the domain cannot reach through normal flow, to
    /// exercise the handler's StageNotInProgress guard. See research.md R2.
    /// </summary>
    public Task ForceCurrentStageStateCompletedAsync(
        Guid projectExternalId,
        CancellationToken ct = default);

    // --- Added by C5 (concurrency test) ---

    /// <summary>
    /// TEST-ONLY: dispatches AdvanceProjectStageCommand via IMediator directly
    /// (not via the UI). Used by the concurrency test to bump the DB RowVersion
    /// behind the Playwright browser's back. See research.md R4.
    /// </summary>
    public Task<Result<AdvanceProjectStageResult>> IssueAdvanceAsync(
        Guid projectExternalId,
        long actingUserId,
        long actingUserIncubatorId,
        bool actingUserIsGlobalAdmin = false,
        CancellationToken ct = default);
}
```

**Internal invariants**:

- Every created project's name starts with `e2e-lifecycle-`.
- Every created incubator's name starts with `e2e-lifecycle-inc-`.
- `ResetStateAsync` uses `DELETE FROM tenant.Projects WHERE [Name] LIKE 'e2e-lifecycle-%'` and corresponding incubator cleanup (FK cascades handle child rows).
- No method throws on "already exists" — all seeders are idempotent.
- No method commits across multiple scopes; every seed operation is a single `SaveChangesAsync` per dispatched command.

---

## 2. `LifecycleLoginHelpers`

**Purpose**: Log a Playwright `IPage` in as a specific role, optionally skipping the incubator-context selection step.

**API**:

```csharp
public static class LifecycleLoginHelpers
{
    /// <summary>
    /// Logs in as coord{userNumber}@test.mentoory.com. userNumber ∈ {1, 2, 3}.
    /// If autoSelectContext is true (default), proceeds through the context
    /// selector and lands on the Coordinación landing page. If false, returns
    /// control immediately after login, before context selection — used by the
    /// "no incubator context → redirect" test.
    /// </summary>
    public static Task AsCoordinatorAsync(
        IPage page,
        PlaywrightFixture host,
        int userNumber = 1,
        bool autoSelectContext = true);

    /// <summary>
    /// Logs in as incubator-admin-{userNumber}@test.mentoory.com. userNumber ∈ {1, 2}.
    /// autoSelectContext true selects the admin's bound incubator.
    /// </summary>
    public static Task AsIncubatorAdminAsync(
        IPage page,
        PlaywrightFixture host,
        int userNumber = 1,
        bool autoSelectContext = true);

    /// <summary>
    /// Logs in as mentor1@test.mentoory.com. No context auto-selection option
    /// because Mentor is blocked at controller level before context matters.
    /// </summary>
    public static Task AsMentorAsync(IPage page, PlaywrightFixture host);

    /// <summary>
    /// Logs in as global-admin@test.mentoory.com. GlobalAdmin does not need a
    /// context selection (per constitution X).
    /// </summary>
    public static Task AsGlobalAdminAsync(IPage page, PlaywrightFixture host);
}
```

**Internal invariants**:

- All helpers take an already-created `IPage`. They do not own page lifetime.
- Passwords match `004.SeedTestData.sql`: `Test123!@#` (current convention in `DiagnosticWorkflowTests.LoginAsync`).
- Each helper asserts `page.Url` matches the expected post-login URL before returning; fails fast if the login flow regresses.

---

## 3. `LifecyclePageObject`

**Purpose**: All Playwright selectors and DOM reads for `/Coordination/Projects/Lifecycle/{externalId}`. Tests MUST NOT raw-select against this page.

**API**:

```csharp
public sealed class LifecyclePageObject
{
    public LifecyclePageObject(IPage page, PlaywrightFixture host);

    public Task GotoAsync(Guid projectExternalId);
    public Task GotoUrlAsync(string relativeUrl);  // for direct-URL-rejection tests

    // --- Timeline ---
    public ILocator StageRow(StageType stage);
    public Task<string> ReadStageStateAsync(StageType stage);        // e.g., "En progreso"
    public Task<(string? StartedAt, string? CompletedAt)> ReadStageTimestampsAsync(StageType stage);
    public Task<string?> ReadStageAdvancedByAsync(StageType stage);

    // --- Advance button + modal ---
    public ILocator AdvanceButton { get; }
    public Task<bool> IsAdvanceButtonVisibleAsync();
    public Task ClickAdvanceAndConfirmAsync();

    // --- Actions grid ---
    public ILocator ActionCard(StageGatedAction action);
    public Task<StageGatedActionState> ReadActionStateAsync(StageGatedAction action);
    public Task<string?> ReadActionTooltipAsync(StageGatedAction action);  // reads data-bs-title
    public Task<string?> ReadActionHrefAsync(StageGatedAction action);

    // --- Toast / TempData-rendered feedback ---
    public Task<string?> ReadSuccessToastAsync();
    public Task<string?> ReadErrorToastAsync();
    public Task<string?> ReadWarningToastAsync();
}
```

**Internal invariants**:

- Selectors prefer semantic attributes (`data-stage`, `data-action`) if the view provides them; otherwise, structural selectors (nth-of-type inside a known container). Per C2's stop conditions, the page object adapts to existing HTML — it does NOT cause view HTML changes.
- `ReadStageStateAsync` returns the rendered Spanish string (not the enum), because the spec asserts Spanish literals.
- `ClickAdvanceAndConfirmAsync` handles the Bootstrap confirmation modal: clicks the button, waits for the modal, clicks the confirm action, waits for the redirect.

---

## 4. `CoordinationProjectsPageObject`

**Purpose**: Selectors for `/Coordination/Projects` (the DataTable list). Thinner than `LifecyclePageObject`.

**API**:

```csharp
public sealed class CoordinationProjectsPageObject
{
    public CoordinationProjectsPageObject(IPage page, PlaywrightFixture host);

    public Task GotoAsync();
    public Task<int> RowCountAsync();
    public Task<bool> ContainsProjectNamedAsync(string name);
    public Task ClickProjectRowAsync(string name);  // navigates to Lifecycle page
}
```

---

## 5. Test class shape

All scenario-test classes in `tests/Mentoory.Tests.E2E/Tests/Lifecycle/` follow this skeleton:

```csharp
[Collection(E2ETestCollection.Name)]
public class Walkthrough{N}{Topic}Tests : IAsyncLifetime
{
    private readonly PlaywrightFixture _host;
    private readonly LifecycleFixtures _fixtures;

    public Walkthrough{N}{Topic}Tests(PlaywrightFixture host)
    {
        _host = host;
        _fixtures = new LifecycleFixtures(host);
    }

    public async Task InitializeAsync() => await _fixtures.ResetStateAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task SomeScenarioName()
    {
        // Arrange: seed via _fixtures
        // Act: drive page via page object
        // Assert: FluentAssertions against page object reads + Spanish literals
        //   from ProjectsController.ResolveSpanishMessage / StageTypeDisplay / StageActionDisplay
    }
}
```

**Why `IAsyncLifetime`**: xUnit calls `InitializeAsync` before the first `[Fact]` in each class instance and `DisposeAsync` after. Since xUnit creates a new instance per `[Fact]` by default, the reset runs per test.

---

## 6. Test-class → Chunk mapping

| Test class | Chunk | Test count |
|---|---|---|
| `LifecycleSmokeTests` | C0 | 1 |
| `WalkthroughAdvanceTests` | C1 | 4 |
| `WalkthroughLifecyclePageTests` | C2 | 3 |
| `WalkthroughGatedActionsTests` | C3 | 4 |
| `WalkthroughRoleScopeTests` | C4 | 3 |
| `WalkthroughAuditConcurrencyTests` | C5 | 3 (test #17 conditional) |
| **Total** | — | **18** (17 scenarios + 1 smoke) |

Each class in its own file. No shared state across test classes beyond the `PlaywrightFixture` collection fixture and the Lifecycle infrastructure helpers.

---

## 7. Extension points beyond these contracts

- If a later chunk needs a new fixture method, it's added **additively** to `LifecycleFixtures`. Renaming or retyping existing methods is a stop condition (per spec Invariants).
- If a later chunk finds the page object is missing a read, the page object is extended additively. Same invariant applies.
- Product-code type imports are one-way: the tests import from `Mentoory.Access.Application.StageActions` (for `StageGatedAction`, `StageActionRegistry`, `StageTypeDisplay`), `Mentoory.Tenant.Domain.Enums` (for `StageType`, `StageState`), and `Mentoory.Tenant.Application.Commands.AdvanceProjectStage` (for types the concurrency helper uses). Product code has zero compile-time dependency on test code.
