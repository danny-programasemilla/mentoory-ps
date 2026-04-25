# Checkpoint C0 — Foundation

> This file is a self-contained resume prompt. Paste it into a fresh AI session to start this chunk.

## Goal

Deliver the test-time fixtures and page objects that Walkthroughs 1–5 depend on, plus **one** smoke test that proves the fixtures are wired correctly. No scenario tests yet. This chunk is the substrate that keeps every later chunk from re-inventing setup.

## Prior state — what must already be on the branch

- Branch: `016-project-lifecycle-finish`, based on `develop`.
- Last commit before this chunk: `7a7942d` (Add relational concurrency test and fix seed data for project lifecycle).
- Parent feature code landed (Coordination area, `RequiresStageAttribute`, `GetProjectLifecycleHandler`, Lifecycle view).
- Seed data has 7 `ProjectStages` rows per seed project (fixed in `7a7942d`).
- Full suite at `7a7942d`: 446 tests, all pass. `dotnet build` 0 warnings.

If `git log --oneline -5` doesn't match — **stop** (see Stop Conditions).

## This chunk — what to produce

**Directory layout to create:**

```
tests/Mentoory.Tests.E2E/Infrastructure/Lifecycle/
├── LifecycleFixtures.cs          # seed/promote/deactivate a project to a given StageType
├── LifecycleLoginHelpers.cs      # login-as-role helpers (coord1, coord2, coord3, mentor1, incAdmin1, globalAdmin)
├── LifecyclePageObject.cs        # selectors/assertions for the /Coordination/Projects/Lifecycle/{id} view
└── CoordinationProjectsPageObject.cs  # selectors for the /Coordination/Projects list (used by WT1/WT2)

tests/Mentoory.Tests.E2E/Tests/Lifecycle/
└── LifecycleSmokeTests.cs        # one test
```

**`LifecycleFixtures`** must expose (signatures are suggestions, not prescriptive — plan phase decides mechanism):

- `Task<Guid> CreateProjectAsync(long incubatorId, string name, StageType targetStage = Registration, bool active = true, CancellationToken ct = default)` — creates a project and advances it programmatically to `targetStage`. Returns the `ExternalId`. Must NOT drive the UI.
- `Task DeactivateProjectAsync(Guid externalId, CancellationToken ct = default)`.
- `Task<(Guid incubator1, Guid incubator2)> EnsureTwoIncubatorsAsync(CancellationToken ct = default)` — idempotent creation for cross-tenant tests.
- `Task ResetStateAsync(CancellationToken ct = default)` — called from test constructors to ensure per-test isolation (Respawn-backed or targeted-cleanup; mechanism is plan-phase).

**`LifecyclePageObject`** must expose:

- `Task GotoAsync(Guid projectExternalId)`.
- `Locator StageRow(StageType stage)` — returns the timeline row for a given stage.
- `Task<StageState> ReadStageStateAsync(StageType stage)` — parses the rendered Spanish badge.
- `Locator AdvanceButton { get; }` / `Task<bool> IsAdvanceButtonVisibleAsync()`.
- `Locator ActionCard(StageGatedAction action)` — returns a card from the Actions grid.
- `Task<string?> ReadTooltipAsync(StageGatedAction action)` — reads `data-bs-title`.
- `Task<string?> ReadToastAsync()` — reads the Spanish toast rendered from `TempData`.

**Smoke test (`LifecycleSmokeTests.cs`)**:

```csharp
[Fact]
public async Task Fixtures_CanLoginAsCoordinatorAndOpenCoordinationProjectsList()
{
    // Arrange: ensure one seeded project exists via the fixture
    // Act: login as coord1, navigate to /Coordination/Projects
    // Assert: page loads (no Internal Server Error), list contains the seeded project's name
}
```

The smoke test is the only scenario test in this chunk. It exists to prove fixtures work before C1 builds on them.

## Fixtures / helpers this chunk may use

N/A — this chunk creates them.

## Invariants

- **No product code changes.** If Playwright needs a back door for `CreateProjectAsync` that doesn't exist, plan-phase decides whether to extend `PlaywrightFixture` (test infrastructure — OK) or add a product-side test hook (NOT OK — stop and surface).
- **No new NuGet packages.**
- **No modification to existing E2E tests.** If an existing test happens to use a name like `LifecycleFixtures`, rename the new one; do not touch the existing.
- **No changes to Spanish string literals anywhere.**
- **No changes to `Mentoory.Db.PostDeployment/004.SeedTestData.sql` unless a test user needed for Walkthroughs 4/5 is missing.** If it is, add it idempotently; record the change in the commit message.

## Pre-flight checklist

Run each command; match the expected output. Any mismatch ⇒ stop.

1. `git status --short` → empty.
2. `git log --oneline -1` → starts with `7a7942d` (or later chunks — see EC1).
3. `git branch --show-current` → `016-project-lifecycle-finish`.
4. `docker info` → Docker daemon responds (first line contains `Client:` then `Server:` details). If not, **stop** per E4.
5. `dotnet build /p:NuGetAudit=false` → `0 Error(s)`. Warnings should be 1 (pre-existing Microsoft.Build.Sql SDK update hint) — any other warning ⇒ stop.
6. `ls /mnt/D/repos/mentoory-ps-lifecycle-finish/Mentoory.Db/bin/Debug/MentooryDb.dacpac` → file exists.
7. `ls tests/Mentoory.Tests.E2E/Infrastructure/Lifecycle/ 2>/dev/null` → directory does NOT yet exist. If it does, pre-flight failed — surface.

## Execution steps

1. Create the directory structure above.
2. Read `tests/Mentoory.Tests.E2E/Infrastructure/PlaywrightFixture.cs` to understand the existing fixture and its capabilities.
3. Read `tests/Mentoory.Tests.E2E/Tests/DiagnosticWorkflowTests.cs` for the canonical test-class shape.
4. Read `tests/Mentoory.Tests.Integration/Fixtures/IntegrationTestBase.cs` for the `SendAsync`/DbContext pattern — the fixture may adopt or adapt it.
5. Read `Mentoory.Db.PostDeployment/004.SeedTestData.sql` to inventory available test users.
6. Implement `LifecycleFixtures` — mechanism choice: prefer a scoped DbContext through the web host's service provider (if exposed) over direct SQL. If neither is feasible, surface and pause.
7. Implement `LifecycleLoginHelpers` and both page objects.
8. Implement the smoke test.
9. Run the smoke test: `dotnet test /mnt/D/repos/mentoory-ps-lifecycle-finish/tests/Mentoory.Tests.E2E/Mentoory.Tests.E2E.csproj --filter "FullyQualifiedName~LifecycleSmokeTests" /p:NuGetAudit=false`.
10. Run the **full** suite: `dotnet test /mnt/D/repos/mentoory-ps-lifecycle-finish/Mentoory.sln /p:NuGetAudit=false`.
11. Apply `/simplify` to the new files only.

## Exit gate — all must be true before committing

- New smoke test passes.
- Full suite passes: 447 tests (446 baseline + 1 smoke), 0 failed, 0 skipped.
- `dotnet build /p:NuGetAudit=false` — 0 errors, ≤ 1 pre-existing SDK warning.
- No file outside `tests/Mentoory.Tests.E2E/` modified (except possibly `Mentoory.Db.PostDeployment/004.SeedTestData.sql` if a test user was added — and that's explicit in the commit message).
- `git diff --stat` shows only expected paths.

## Commit message template

```
Add E2E foundation for project lifecycle tests

Lifecycle fixtures and page objects for Walkthroughs 1–5, plus a smoke
test that proves the fixtures are wired correctly. No scenario tests in
this chunk — C1 onwards build on this substrate.

Chunk: C0 (foundation) — specs/016-project-lifecycle-finish/e2e/

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
```

## Stop conditions

- Pre-flight mismatch (any of the seven checks).
- The existing `PlaywrightFixture` cannot be extended for programmatic project creation without product-code changes.
- A seed test user needed by C4/C5 is missing AND adding it requires product-code changes (e.g., a new role not in `004.SeedTestData.sql`).
- Any test outside this chunk fails.
- `/simplify` asks to modify product code.

If any stop condition triggers: update this file's Prior-state section with a `## Blocked` block, commit NOTHING, surface to user.

## After push

- Mark T042 / T051 / T056 UNCHANGED (C0 doesn't close them yet).
- Update `specs/016-project-lifecycle-finish/e2e/coverage-matrix.md` — record C0's smoke test under SC-E1 foundation row.
- Next chunk: `checkpoints/01-walkthrough-1-advance.md`.
