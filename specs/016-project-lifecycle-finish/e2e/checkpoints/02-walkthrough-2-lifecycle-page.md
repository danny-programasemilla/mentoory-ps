# Checkpoint C2 — Walkthrough 2 — Lifecycle page

> Self-contained resume prompt. Paste into a fresh session.

## Goal

Cover the three User Story 2 acceptance scenarios: how the Lifecycle view renders for projects at three distinct lifecycle positions (brand-new, mid-lifecycle, fully-closed).

## Prior state

- C0 and C1 committed. Last two commits on the branch:
  - `Add E2E coverage for Walkthrough 1 (advance project stage)`
  - `Add E2E foundation for project lifecycle tests`
- Full suite: 451 tests passing.
- `tests/Mentoory.Tests.E2E/Tests/Lifecycle/WalkthroughAdvanceTests.cs` present.

## This chunk — tests to write

File: `tests/Mentoory.Tests.E2E/Tests/Lifecycle/WalkthroughLifecyclePageTests.cs`

| Test name | Parent spec ref | Intent |
|-----------|-----------------|--------|
| `Lifecycle_MidProject_RegistrationCompleted_FormsInProgress` | US2 §1 | Seed project at Forms (fixture advances Registration→Forms in setup); open Lifecycle; assert: Registration row shows `Completada` + both timestamps rendered in `dd/MM/yyyy HH:mm` format; Forms row shows `En progreso` + start timestamp only; rows Analysis..Closure show `No iniciada` with no timestamps. |
| `Lifecycle_BrandNewProject_OnlyRegistrationInProgress` | US2 §2 | Seed project in Registration (default); open Lifecycle; assert Registration row shows `En progreso` with start timestamp; all 6 remaining rows show `No iniciada` with no timestamps. Advance button is visible. |
| `Lifecycle_ClosedProject_AllStagesCompleted` | US2 §3 | Seed project at Closure (fixture advances through all 6 transitions); open Lifecycle; assert all 7 rows show `Completada`, each with both timestamps. Advance button is NOT visible. `CannotAdvanceReason` muted text matches `El proyecto ya está en la etapa final (Cierre).` |

Tests MUST use `LifecyclePageObject.ReadStageStateAsync(StageType)` and `StageRow(StageType)` — not raw CSS selectors. The Spanish badge strings are the ones from `StageTypeDisplay.ToSpanish` / the StageState translation used in `Lifecycle.cshtml`.

## Fixtures / helpers this chunk may use

- `LifecycleFixtures.CreateProjectAsync(targetStage: ...)`.
- `LifecycleLoginHelpers.AsCoordinatorAsync(page)`.
- `LifecyclePageObject.GotoAsync`, `StageRow`, `ReadStageStateAsync`, `IsAdvanceButtonVisibleAsync`.

Likely to need: a `LifecyclePageObject.ReadStageTimestampsAsync(StageType)` helper that parses the two timestamps per row. If missing, add it in this chunk as a fixture extension.

## Invariants

- **No product code changes.**
- **No modification to the Lifecycle view's HTML structure.** If the page object needs a data-attribute it doesn't have (`data-stage="Forms"`), and that attribute isn't in the view, the page object must adapt to existing structure (e.g., nth-of-type) or the chunk surfaces and stops — do NOT add attributes to the view.
- **Timestamp format assertion tolerance.** The view renders `dd/MM/yyyy HH:mm`. The test asserts format structure, not exact values (since times are seeded at test-run time). A regex `^\d{2}/\d{2}/\d{4} \d{2}:\d{2}$` is acceptable.

## Pre-flight checklist

1. `git log --oneline -3 | head -2` matches `Walkthrough 1 (advance)` then `E2E foundation`.
2. `dotnet test /p:NuGetAudit=false --no-build` → 451 passed.
3. `docker info` → responsive.
4. `ls tests/Mentoory.Tests.E2E/Tests/Lifecycle/` → `LifecycleSmokeTests.cs`, `WalkthroughAdvanceTests.cs` — no `WalkthroughLifecyclePageTests.cs`.
5. `git status --short` empty.

## Execution steps

1. Create `WalkthroughLifecyclePageTests.cs`.
2. Implement the three tests.
3. Extend `LifecyclePageObject` with `ReadStageTimestampsAsync` if missing — extension only, no signature changes.
4. Run new tests in isolation.
5. Run full suite.
6. Apply `/simplify`.

## Exit gate

- 3 new tests pass.
- Full suite: 454 tests (451 + 3), 0 failed, 0 skipped.
- `dotnet build` — 0 errors, ≤ 1 pre-existing warning.
- Coverage matrix rows 5–7 filled in.

## Commit message template

```
Add E2E coverage for Walkthrough 2 (Lifecycle page rendering)

3 tests covering US2 acceptance scenarios — the Lifecycle view at three
distinct project states:
- Mid-lifecycle (Registration completed, Forms in progress)
- Brand-new (only Registration in progress)
- Closed (all 7 stages completed, no advance button)

Chunk: C2 — specs/016-project-lifecycle-finish/e2e/

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
```

## Stop conditions

- Pre-flight mismatch.
- The Lifecycle view cannot be queried reliably without modifying its HTML — stop and surface; do NOT add data attributes.
- A test reveals a product bug (wrong state badge, wrong timestamp, advance button shown at Closure) — write skipped test per E2, do NOT fix.
- Test duration >10 minutes.

## After push

- Coverage matrix rows 5–7 updated.
- Next: `checkpoints/03-walkthrough-3-gated-actions.md`.
