# Checkpoint C5 — Walkthrough 5 — Audit trail + concurrency + inactive

> Self-contained resume prompt. Paste into a fresh session. Final chunk.

## Goal

Close out the E2E suite with the three remaining scenarios: the audit trail on a project advanced by multiple coordinators (SC-005); the concurrency-conflict UI toast when two coordinators race (parent spec edge case); the inactive-project message (parent spec edge case).

## Prior state

- C0, C1, C2, C3, C4 committed. Last five commit subjects:
  - `Add E2E coverage for Walkthrough 4 (role and scope enforcement)`
  - `Add E2E coverage for Walkthrough 3 (stage-gated actions)`
  - `Add E2E coverage for Walkthrough 2 (Lifecycle page rendering)`
  - `Add E2E coverage for Walkthrough 1 (advance project stage)`
  - `Add E2E foundation for project lifecycle tests`
- Full suite: 461 tests passing.

## This chunk — tests to write

File: `tests/Mentoory.Tests.E2E/Tests/Lifecycle/WalkthroughAuditConcurrencyTests.cs`

| Test name | Parent spec ref | Intent |
|-----------|-----------------|--------|
| `AuditTrail_ThreeAdvancesByThreeCoordinators_AllNamesVisibleOnLifecyclePage` | SC-005 | Seed project in Registration. Login as coord1, advance to Forms. Logout. Login as coord2, advance to Analysis. Logout. Login as coord3, advance to LearningAssignment. Reload Lifecycle page. Assert the Registration row shows coord1's display name, Forms row shows coord2's, Analysis row shows coord3's. No navigation to a separate audit screen needed. |
| `ConcurrentAdvance_SecondAttemptShowsConcurrencyToast` | Edge case: concurrency | Seed project in Registration. Login as coord1 in context A, open Lifecycle page (project's row-version is now cached in the browser/server session). In parallel (or via a test-only helper that issues a stale Advance command from a second session), issue a second Advance. One succeeds, the other surfaces the Spanish toast `Otra operación modificó este proyecto. Actualice la página e intente de nuevo.` |
| `InactiveProject_AdvanceAttemptShowsInactiveToast` | Edge case: inactive | Seed project; call `LifecycleFixtures.DeactivateProjectAsync`. Login as coord1. Navigate to Lifecycle. Expected behavior: advance button either hidden (per FR-015 + `CanAdvance` = false) with the `El proyecto está inactivo.` muted text, OR — if button is present due to the server-authority pattern — clicking it produces the Spanish toast `El proyecto está inactivo. Active el proyecto antes de avanzar de etapa.` Test for both states — the test asserts whichever is the consistent user-facing behavior on this branch. |

The concurrency test is the trickiest. Plan-phase will confirm the exact mechanism (parallel Playwright contexts vs. direct DbContext write through the test host). If the mechanism requires product-code changes, surface per E2.

## Fixtures / helpers this chunk may use

- `LifecycleFixtures.CreateProjectAsync`, `DeactivateProjectAsync`.
- `LifecycleLoginHelpers.AsCoordinatorAsync(page, userNumber: 1|2|3)`.
- A new helper `LifecycleFixtures.IssueStaleAdvanceAsync(Guid externalId)` may be needed for the concurrency test — it simulates a second coordinator's action bypass the UI. Addition is in-scope (fixture extension).

## Invariants

- **No product code changes.** If the audit trail shows wrong names, skip test per E2.
- **Seed users coord1, coord2, coord3 must exist in `004.SeedTestData.sql`.** Confirm before starting. If coord3 is missing, add idempotently (fixture change, in-scope).
- **No changes to the concurrency-conflict Spanish string** — the test consumes it from `ProjectsController.ResolveSpanishMessage` (or a shared constant if the plan phase introduces one, but not as part of this spec).

## Pre-flight checklist

1. `git log --oneline -6 | head -5` matches WT4, WT3, WT2, WT1, E2E foundation in order.
2. `dotnet test /p:NuGetAudit=false --no-build` → 461 passed.
3. `docker info` → responsive.
4. `grep -E "coord3|coordinator3" Mentoory.Db.PostDeployment/004.SeedTestData.sql` → at least coord1 + coord2 exist; coord3 exists or will be added.
5. `ls tests/Mentoory.Tests.E2E/Tests/Lifecycle/` → 5 walkthrough files, no `WalkthroughAuditConcurrencyTests.cs`.
6. `git status --short` empty.

## Execution steps

1. Verify seed users.
2. If missing, add to seed SQL, rebuild DACPAC, verify suite green.
3. Implement `IssueStaleAdvanceAsync` on `LifecycleFixtures` if needed.
4. Create `WalkthroughAuditConcurrencyTests.cs` with the 3 tests.
5. Run new tests; full suite; `/simplify`.
6. **Additional step for the final chunk**: update `specs/016-project-lifecycle-finish/tasks.md` to close T042, T051, T056 per SC-E7.
7. **Additional step**: update `specs/016-project-lifecycle-finish/implementation-notes.md` with a "§ E2E coverage complete" paragraph pointing at this chunk's PR range.

## Exit gate

- 3 new tests pass.
- Full suite: 464 tests (461 + 3), 0 failed, 0 skipped.
- `dotnet build` — 0 errors, ≤ 1 pre-existing warning.
- Coverage matrix rows 15–17 filled in; matrix is 100% complete per SC-E6.
- `specs/016-project-lifecycle-finish/tasks.md` T042 / T051 / T056 closed per SC-E7.
- `specs/016-project-lifecycle-finish/implementation-notes.md` updated.

## Commit message template

```
Add E2E coverage for Walkthrough 5 and close out feature 016 E2E work

3 final tests: audit-trail rendering across multiple coordinators (SC-005),
concurrency-conflict UI toast on a lost race, inactive-project messaging.

Closes T042, T051, T056 in specs/016-project-lifecycle-finish/tasks.md
with E2E equivalents. Implementation notes updated.

Chunk: C5 (final) — specs/016-project-lifecycle-finish/e2e/

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
```

## Stop conditions

- Pre-flight mismatch.
- Concurrency test mechanism requires product-code changes — stop, surface.
- Audit trail shows wrong names (test 1 fails) — skip per E2, stop, surface critical.
- A Spanish literal this chunk relies on doesn't match the one in product code — stop, diagnose before writing test.
- Test duration >15 minutes (concurrency tests may be slower — allowance).

## After push

- Coverage matrix complete. `coverage-matrix.md` committed in this same commit.
- `tasks.md` T042/T051/T056 closed.
- Post-chunk: notify the user that C5 is done. Suggest the review sequence: (a) manually run the full suite, (b) open PR #11 and verify the 6 new commits on top of C0's predecessor, (c) run `/simplify` globally as a sanity check, (d) merge when ready.
