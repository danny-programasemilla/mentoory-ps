# Checkpoint C4 — Walkthrough 4 — Role and scope enforcement

> Self-contained resume prompt. Paste into a fresh session.

## Goal

Cover the role-based access and tenant-scope edge cases from feature 016: a Mentor blocked by `[Authorize(Roles=...)]`; an IncubatorAdmin of incubator A blocked from fetching incubator B's project; a coordinator without incubator context redirected to the context selector.

## Prior state

- C0, C1, C2, C3 committed. Last four commit subjects:
  - `Add E2E coverage for Walkthrough 3 (stage-gated actions)`
  - `Add E2E coverage for Walkthrough 2 (Lifecycle page rendering)`
  - `Add E2E coverage for Walkthrough 1 (advance project stage)`
  - `Add E2E foundation for project lifecycle tests`
- Full suite: 458 tests passing.

## This chunk — tests to write

File: `tests/Mentoory.Tests.E2E/Tests/Lifecycle/WalkthroughRoleScopeTests.cs`

| Test name | Parent spec ref | Intent |
|-----------|-----------------|--------|
| `IncubatorAdminA_CannotFetchIncubatorBProjectLifecycle` | Edge case: cross-incubator | Seed two incubators (use `LifecycleFixtures.EnsureTwoIncubatorsAsync`), create a project in each. Login as `IncubatorAdmin` of A (context = incubator A). Navigate to `/Coordination/Projects/Lifecycle/{externalId-of-incubator-B-project}`. Assert 403 (or the platform's equivalent denial rendering) — NOT a 500, NOT the other incubator's data. |
| `Coordinator_WithoutIncubatorContext_RedirectedToSelector` | Edge case: no context | Login as `coord1` but do NOT select an incubator context (bypass the context-selector step if helpers auto-select). Navigate to `/Coordination/Projects`. Assert the final URL is the context selector + the standard warning toast `Debe seleccionar una incubadora antes de continuar.` |
| `Mentor_CannotAccessCoordinationProjectsList` | US1 §4 (generalized) | Login as `Mentor` (menu group includes Coordinación but controller role list is `ProjectCoordinator,IncubatorAdmin,GlobalAdmin`). Navigate to `/Coordination/Projects`. Assert 403 or denial page in Spanish. |

## Fixtures / helpers this chunk may use

- `LifecycleFixtures.CreateProjectAsync` and `EnsureTwoIncubatorsAsync`.
- `LifecycleLoginHelpers.AsIncubatorAdminAsync(page, incubatorNumber)`, `AsCoordinatorAsync(page, autoSelectContext: false)`, `AsMentorAsync(page)`.

`AsCoordinatorAsync` likely default-auto-selects context. For test 2, introduce an `autoSelectContext: bool = true` parameter on the login helper. Backward-compatible fixture extension — allowed per E3.

## Invariants

- **No product code changes.** If cross-incubator access actually leaks data (test 1 surfaces the other incubator's info), STOP — this is a critical security bug. Skip-mark and surface per E2, but flag the severity in the resume prompt's `## Blocked` section.
- **Seed users for IncubatorAdmin A, IncubatorAdmin B, Mentor, Coord all exist in `004.SeedTestData.sql`.** Verify before writing tests.
- **Test 2 must not rely on destroying claims after login.** Use a login path that returns a session without an active incubator selection (pre-selector state).

## Pre-flight checklist

1. `git log --oneline -5 | head -4` matches WT3, WT2, WT1, E2E foundation.
2. `dotnet test /p:NuGetAudit=false --no-build` → 458 passed.
3. `docker info` → responsive.
4. `ls tests/Mentoory.Tests.E2E/Tests/Lifecycle/` → 4 walkthrough files, no `WalkthroughRoleScopeTests.cs`.
5. `grep -E "IncAdmin2|incubator-admin-2|IncubatorAdmin.*2" Mentoory.Db.PostDeployment/004.SeedTestData.sql` → returns a match. If not, confirm at least two IncubatorAdmin users exist across two different incubators. If not, add one via idempotent `INSERT ... WHERE NOT EXISTS` — fixture change, in-scope per E3.

## Execution steps

1. Verify seed users per pre-flight step 5.
2. If missing, add to `004.SeedTestData.sql`, rebuild DACPAC (`cd Mentoory.Db && ./publish-mentoorydb.sh`), verify full suite still passes before adding new tests.
3. Extend `LifecycleLoginHelpers` for `IncubatorAdmin` selection and the `autoSelectContext` flag on coordinator login.
4. Create `WalkthroughRoleScopeTests.cs` with the 3 tests.
5. Run new tests; full suite; `/simplify`.

## Exit gate

- 3 new tests pass.
- Full suite: 461 tests (458 + 3), 0 failed, 0 skipped.
- `dotnet build` — 0 errors, ≤ 1 pre-existing warning.
- Coverage matrix rows 12–14 filled in.
- If seed SQL modified: noted in commit message AND the DACPAC is rebuilt as part of pre-flight-to-green.

## Commit message template

```
Add E2E coverage for Walkthrough 4 (role and scope enforcement)

3 tests covering feature 016 edge cases — IncubatorAdmin cannot cross
incubators; coordinator without context is redirected; Mentor is blocked
from the Coordination projects list at the controller level.

Chunk: C4 — specs/016-project-lifecycle-finish/e2e/

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
```

If the seed SQL was modified, add a separate line: `Adds IncubatorAdmin2 (or whichever) seed user to support cross-incubator coverage.`

## Stop conditions

- Pre-flight mismatch.
- A test reveals cross-tenant data leakage — STOP, flag severity as critical in `## Blocked`, surface immediately.
- Login helpers for a role can't be built without product-code changes — stop.
- Test duration >10 minutes.

## After push

- Coverage matrix rows 12–14 updated.
- Next: `checkpoints/05-walkthrough-5-audit-concurrency.md`.
