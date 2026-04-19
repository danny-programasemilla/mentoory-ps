# Checkpoint C1 — Walkthrough 1 — Advance

> Self-contained resume prompt. Paste into a fresh session.

## Goal

Cover User Story 1 of feature 016 (advancing a project through the lifecycle) end-to-end in a real browser. Four tests mapped to the 4 US1 acceptance scenarios in `specs/016-project-lifecycle-finish/spec.md`.

## Prior state — what must already be on the branch

- C0 committed. Commit message starts with `Add E2E foundation for project lifecycle tests`.
- `tests/Mentoory.Tests.E2E/Infrastructure/Lifecycle/` directory exists with `LifecycleFixtures`, `LifecycleLoginHelpers`, `LifecyclePageObject`, `CoordinationProjectsPageObject`.
- `tests/Mentoory.Tests.E2E/Tests/Lifecycle/LifecycleSmokeTests.cs` passes.
- Full suite at this point: 447 tests, all pass.

## This chunk — tests to write

File: `tests/Mentoory.Tests.E2E/Tests/Lifecycle/WalkthroughAdvanceTests.cs`

| Test name | Parent spec ref | Intent |
|-----------|-----------------|--------|
| `Coordinator_AdvancesProjectFromRegistrationToForms` | US1 §1 | Seed project in Registration; login as `ProjectCoordinator` with correct incubator context; go to Lifecycle view; click advance; confirm modal; expect `Proyecto avanzado a Formularios.` toast + URL stays on Lifecycle + timeline now shows Registration Completed / Forms In Progress. |
| `Coordinator_CannotAdvanceProjectAtClosure` | US1 §2 | Seed project at Closure (use `CreateProjectAsync(targetStage: Closure)`); login; open Lifecycle; assert advance button is NOT visible. Additionally issue a direct `POST /Coordination/Projects/AdvanceStage/{id}` and expect a redirect to Lifecycle with `El proyecto ya está en la etapa final (Cierre).` as the error toast. |
| `Coordinator_CannotAdvanceWhenCurrentStageIsCompleted` | US1 §3 | Seed a project and mutate its `CurrentStageState` to `Completed` via a fixture helper (document this as a test-only "broken-state" constructor — it exists to cover the invariant; the UI flow never produces this state). Attempt advance; expect `La etapa actual no está en progreso. Actualice la página.` |
| `NonCoordinator_CannotSeeAdvanceButtonOrInvokeAdvance` | US1 §4 | Login as `Mentor`. Navigate to the Lifecycle URL (direct — Mentor isn't in the controller's role list). Expect 403 / redirect per the controller's `[Authorize]` behavior. Additionally issue a direct POST to `/Coordination/Projects/AdvanceStage/{id}` and assert it is NOT accepted. |

**Assertions must be on exact Spanish literals** from `Mentoory.Web/Areas/Coordination/Controllers/ProjectsController.cs::ResolveSpanishMessage`.

## Fixtures / helpers this chunk may use

- `LifecycleFixtures.CreateProjectAsync` — seed at any `StageType`.
- `LifecycleFixtures.ResetStateAsync` — per-test isolation.
- `LifecycleLoginHelpers.AsCoordinatorAsync(page)`, `AsMentorAsync(page)`.
- `LifecyclePageObject` — all timeline / advance-button / toast interactions.

If a helper is missing for test 3 ("broken-state" fixture), it's part of this chunk to add it (fixture change, in-scope per E3).

## Invariants

- **No product code changes.** If the advance-button visibility logic turns out to be wrong for the Closure case, write a skipped test and surface per E2.
- **No modification to C0 fixtures' public API.** Adding new methods is OK; renaming or changing signatures is not.
- **Spanish literals in product code are not modified** — tests assert against them as-is.
- **Do not touch `Mentoory.Db.PostDeployment/004.SeedTestData.sql`** unless a test user is missing; if it is, document in the commit message.

## Pre-flight checklist

1. `git log --oneline -2 | head -1` contains `E2E foundation`.
2. `dotnet test /p:NuGetAudit=false --no-build` at head of branch → 447 passed, 0 failed.
3. `docker info` → Docker daemon responds.
4. `ls tests/Mentoory.Tests.E2E/Tests/Lifecycle/` → `LifecycleSmokeTests.cs` present, no `WalkthroughAdvanceTests.cs` yet.
5. `git status --short` → empty.

## Execution steps

1. Create `WalkthroughAdvanceTests.cs` with the 4 tests.
2. Each test calls `await fixtures.ResetStateAsync()` in setup and uses `LifecycleFixtures.CreateProjectAsync` to seed the state it needs.
3. Run the new tests in isolation: `dotnet test /mnt/D/repos/mentoory-ps-lifecycle-finish/tests/Mentoory.Tests.E2E/Mentoory.Tests.E2E.csproj --filter "FullyQualifiedName~WalkthroughAdvanceTests" /p:NuGetAudit=false`.
4. Run full suite: `dotnet test /mnt/D/repos/mentoory-ps-lifecycle-finish/Mentoory.sln /p:NuGetAudit=false`.
5. Apply `/simplify` to the new file only.

## Exit gate

- 4 new tests pass.
- Full suite: 451 tests (447 + 4), 0 failed, 0 skipped.
- `dotnet build /p:NuGetAudit=false` — 0 errors, ≤ 1 pre-existing warning.
- Only files modified: `WalkthroughAdvanceTests.cs` (new), possibly `LifecycleFixtures.cs` (new method for the broken-state constructor), possibly `coverage-matrix.md` (this session updates it).
- Coverage matrix rows 1–4 now filled in.

## Commit message template

```
Add E2E coverage for Walkthrough 1 (advance project stage)

4 tests covering US1 acceptance scenarios from feature 016:
- Happy-path Registration→Forms advance with audit capture
- Closure project rejects advance (button hidden + direct POST refused)
- Completed-state stage rejects advance with Spanish toast
- Non-coordinator role blocked at controller level

Chunk: C1 — specs/016-project-lifecycle-finish/e2e/

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
```

## Stop conditions

- Any pre-flight mismatch.
- A test reveals a real product-code bug (e.g., Closure advance actually succeeds, or non-coordinator POST is accepted) — write the skipped test per E2, do NOT fix the product code.
- Fixture work in this chunk requires modifying the `LifecyclePageObject` public API.
- Total test duration exceeds 10 minutes for the new 4 tests (indicates a non-determinism bug — surface, don't paper over with retries).

If any stop condition triggers: update this file's Prior-state section with `## Blocked`, commit nothing, surface.

## After push

- `coverage-matrix.md` rows 1–4 updated.
- Next: `checkpoints/02-walkthrough-2-lifecycle-page.md`.
