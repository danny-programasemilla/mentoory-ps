# Checkpoint C3 — Walkthrough 3 — Gated actions

> Self-contained resume prompt. Paste into a fresh session.

## Goal

Cover the User Story 3 acceptance scenarios: the Actions grid's three states (Available / Locked / Past) across stage transitions, the locked-card helper text, and the server-side guard (`RequiresStageAttribute`) rejecting direct URL access to out-of-stage actions.

## Prior state

- C0, C1, C2 committed. Last three commit subjects on branch:
  - `Add E2E coverage for Walkthrough 2 (Lifecycle page rendering)`
  - `Add E2E coverage for Walkthrough 1 (advance project stage)`
  - `Add E2E foundation for project lifecycle tests`
- Full suite: 454 tests passing.

## This chunk — tests to write

File: `tests/Mentoory.Tests.E2E/Tests/Lifecycle/WalkthroughGatedActionsTests.cs`

| Test name | Parent spec ref | Intent |
|-----------|-----------------|--------|
| `GatedActions_RegistrationStage_AllSixCardsLocked` | US3 §1 | Seed project in Registration; open Lifecycle; assert all 6 cards in the Actions grid have `data-bs-toggle="tooltip"`, `aria-disabled="true"`, `tabindex="-1"`; tooltip's `data-bs-title` for each card matches `Disponible desde la etapa <StageTypeDisplay.ToSpanish(gatingStage)>`. |
| `GatedActions_FlipStatesOnAdvance` | US3 §2 | Seed project in Forms; open Lifecycle; assert `DiagnosticForms` card is Available (primary styling + active href to `/Coordination/Diagnostics`). Advance stage to Analysis via the UI. After redirect, assert `DiagnosticForms` is Past (muted + checkmark badge) and `AnswerCorrection` is Available. |
| `GatedActions_DirectUrlToLockedAction_RedirectsToLifecycleWithSpanishToast` | US3 §3 | Seed project in Registration; login as coord1; GET `/Coordination/AnswerCorrection` directly (Answer Correction is gated by `[RequiresStage(StageGatedAction.AnswerCorrection)]` and requires Analysis). Assert 302 redirect to `/Coordination/Projects/Lifecycle/{externalId}` and that the rendered Lifecycle page shows the TempData warning toast `Esta acción estará disponible desde la etapa Análisis.` |
| `GatedActions_LockedCardTooltipNamesUnlockingStage` | US3 §4 | Seed project in Registration; open Lifecycle; for each of the 6 locked cards, read `data-bs-title` via `LifecyclePageObject.ReadTooltipAsync`; assert the string exactly matches `Disponible desde la etapa <StageTypeDisplay.ToSpanish(StageActionRegistry.GetGatingStage(action))>`. |

Tests 1 and 4 have overlap but assert different things: test 1 asserts the card's ARIA/visual state; test 4 asserts the tooltip text mapping per action. Keeping them separate makes failures diagnosable.

## Fixtures / helpers this chunk may use

- `LifecycleFixtures.CreateProjectAsync(targetStage: Registration | Forms)`.
- `LifecycleLoginHelpers.AsCoordinatorAsync`.
- `LifecyclePageObject.ActionCard(StageGatedAction)`, `ReadTooltipAsync`, `ReadToastAsync`.

For test 3, the helper to issue a direct GET (not via the page object) may need to be added — do it on `LifecyclePageObject` as `GotoUrlAsync(string url)` so all HTTP gesturing stays inside the page object.

## Invariants

- **No product code changes.** If a card missing `aria-disabled="true"` is discovered, write skipped test per E2.
- **Tooltip text assertion against exact Spanish** — consume `StageTypeDisplay.ToSpanish` in the test itself to build the expected string, not a hardcoded copy (so translation changes in production are caught by one test, not all four).
- **No changes to `StageActionRegistry`**, `StageTypeDisplay`, or any helper. Tests consume them read-only.

## Pre-flight checklist

1. `git log --oneline -4 | head -3` matches WT2, WT1, E2E foundation, in order.
2. `dotnet test /p:NuGetAudit=false --no-build` → 454 passed.
3. `docker info` → responsive.
4. `ls tests/Mentoory.Tests.E2E/Tests/Lifecycle/` → 3 walkthrough files present, no `WalkthroughGatedActionsTests.cs`.
5. `git status --short` empty.

## Execution steps

1. Create `WalkthroughGatedActionsTests.cs`.
2. Build the expected tooltip map by iterating `Enum.GetValues<StageGatedAction>()` and calling `StageActionRegistry.GetGatingStage` + `StageTypeDisplay.ToSpanish`. Use that map in tests 1 and 4.
3. For test 2, use the `LifecyclePageObject.AdvanceButton` (click + confirm modal) — reuse the advance helper from C1 if it exists, or promote it to the page object now.
4. For test 3, navigate to the absolute URL `/Coordination/AnswerCorrection` via Playwright; assert the final URL contains `Lifecycle/` and the toast region renders the Spanish text.
5. Run new tests; run full suite; `/simplify`.

## Exit gate

- 4 new tests pass.
- Full suite: 458 tests (454 + 4), 0 failed, 0 skipped.
- `dotnet build` — 0 errors, ≤ 1 pre-existing warning.
- Coverage matrix rows 8–11 filled in.

## Commit message template

```
Add E2E coverage for Walkthrough 3 (stage-gated actions)

4 tests covering US3 acceptance scenarios — the Actions grid rendering,
state transitions on advance, direct-URL rejection by RequiresStageAttribute,
and the locked-card tooltip text.

Chunk: C3 — specs/016-project-lifecycle-finish/e2e/

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
```

## Stop conditions

- Pre-flight mismatch.
- A card lacks an ARIA attribute the spec requires — surface per E2.
- The direct-URL redirect target or toast text differs from what `RequiresStageAttribute` sets in `TempData` — surface per E2.
- Test duration >10 minutes.

## After push

- Coverage matrix rows 8–11 updated.
- Next: `checkpoints/04-walkthrough-4-role-scope.md`.
