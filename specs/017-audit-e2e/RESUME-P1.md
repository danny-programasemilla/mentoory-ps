# Resume — Phase 1: Viewer shell

**Previous phase:** initial bootstrap
**Branch:** 017-audit-e2e
**Base commit:** 6a7922c
**Authored:** 2026-04-19
**Status:** ready

---

## What has been shipped before

- **Feature 016-audit-pipeline** merged to `develop` via PR #12. Admin viewer at `/Administration/AuditLog`, AuditingBehavior MediatR pipeline behavior, architecture tests, five retrofitted commands (`SetActiveContextCommand`, `AssignRoleCommand`, `RegisterUserCommand`, `LoginUserCommand`, `CorrectAnswerCommand` — Manual-mode), plus `AssignMentorCommand` / `RegisterInternalUserCommand` decorated when discovered by the architecture test. Integration coverage lives in `tests/Mentoory.Tests.Integration/Audit/`.
- **Spec 017** authored on branch `017-audit-e2e` at HEAD `6a7922c`:
  - `specs/017-audit-e2e/spec.md` — 4 user stories, 18 FRs, 8 measurable SCs
  - `specs/017-audit-e2e/plan.md` — constitution check (all gates pass; test-only scope)
  - `specs/017-audit-e2e/research.md` — 10 unknowns resolved (see R-01 below)
  - `specs/017-audit-e2e/data-model.md` — Phase/Checkpoint/ResumePrompt logical model + test coverage matrix
  - `specs/017-audit-e2e/contracts/resume-prompt-schema.md` — canonical RESUME file structure
  - `specs/017-audit-e2e/contracts/session-checkpoint-protocol.md` — seven-step exit ritual + kickoff procedure
  - `specs/017-audit-e2e/quickstart.md` — one-screen phase-kickoff runbook
  - `specs/017-audit-e2e/tasks.md` — 36 tasks mapped 1:1 to session checkpoints
- **Branch state**: `017-audit-e2e` exists both locally and on origin. No production code touched yet. No E2E test files created yet.

---

## What to do in this session (P1)

**Objective**: Ship `AuditLogViewerTests.cs` (9 tests) proving the admin audit-log viewer renders for GlobalAdmin, is denied to every lower role, and exposes the correct Spanish shell + menu visibility. Plus the shared test-infrastructure helpers (fixture Respawn, LoginHelper, AuditSpanishCopy) needed by every subsequent phase.

**Spec reference**: `specs/017-audit-e2e/spec.md § User Story 1` and `specs/017-audit-e2e/tasks.md § Phase 1 Setup + Phase 2 US1`

**Task range (from tasks.md)**: T001 through T013 inclusive (13 tasks total for this session).

**Setup (must complete before any US1 test)**:

- `T001` Extend `tests/Mentoory.Tests.E2E/Infrastructure/PlaywrightFixture.cs` to add `Respawner` with `SchemasToInclude = ["access", "tenant", "diagnostic", "example", "subscription", "audit"]`. Expose `ResetDatabaseAsync()`. Create `E2ETestBase` (or wire per-class `IAsyncLifetime.InitializeAsync`) so every test starts against a clean DB. Reference implementation: `tests/Mentoory.Tests.Integration/Fixtures/MentooryWebApplicationFactory.cs` lines 62-67 and 141-146.
- `T002` [P] Create `tests/Mentoory.Tests.E2E/Infrastructure/LoginHelper.cs` — static `Task LoginAsync(IPage page, string email, string password, string baseUrl)`. Body = verbatim copy of `AuthorizationTests.LoginAsync` (lines 124-161 of that file, includes context-selector handling for GlobalAdmin). Update `AuthorizationTests.cs` to call `LoginHelper.LoginAsync(...)` instead of its private copy.
- `T003` [P] Create `tests/Mentoory.Tests.E2E/Infrastructure/AuditSpanishCopy.cs` — static constants for every Spanish string P1-P4 tests assert (column headers, filter buttons, pagination, Outcome options, empty state, menu entry, page title). Exact values listed in `tasks.md § T003`.

**Test methods to add (verbatim from `data-model.md § Phase 1` coverage matrix)**:

- `GlobalAdmin_CanOpenAuditLog_TableRenders`
- `IncubatorAdmin_IsDenied`
- `Entrepreneur_IsDenied`
- `Mentor_IsDenied`
- `Sponsor_IsDenied`
- `GlobalAdmin_SeesMenuEntryUnderPlataforma`
- `IncubatorAdmin_DoesNotSeeMenuEntry`
- `FilterWithNoMatch_ShowsSpanishEmptyState`
- `OutcomeDropdown_HasSpanishOptions`

All nine live in a single new file: `tests/Mentoory.Tests.E2E/Tests/AuditLogViewerTests.cs`.

---

## Invariants to preserve (do NOT violate)

- **Seeded users** (from DACPAC PostDeployment, verified present):
  - `admin@mentoory.com` — GlobalAdmin, password `123abc987`
  - `incadmin1@test.mentoory.com` — IncubatorAdmin, password `Test123!@#`
  - `entrepreneur1@test.mentoory.com` — Entrepreneur, password `Test123!@#`
  - `mentor1@test.mentoory.com` — Mentor, password `Test123!@#`
  - `sponsor1@test.mentoory.com` — Sponsor, password `Test123!@#`
- **Shared fixture**: `PlaywrightFixture` via `[Collection(E2ETestCollection.Name)]`. Do not create new fixtures (FR-014). After T001, the fixture respawns the `audit` schema between tests — assume it.
- **Login helper**: shared `LoginHelper.LoginAsync(page, email, password, baseUrl)` under `tests/Mentoory.Tests.E2E/Infrastructure/`. Do not duplicate; do not write parallel helpers (FR-014).
- **Spanish copy pinning**: shared `AuditSpanishCopy` static class under `tests/Mentoory.Tests.E2E/Infrastructure/` holds every Spanish string asserted by audit tests. All assertions reference constants; NO inline literals.
- **Playwright timeouts**: every `WaitForLoadStateAsync` / `WaitForFunctionAsync` / `WaitForSelectorAsync` call MUST pass an explicit timeout ≤ 15 s (FR-015).
- **Scope boundary**: no file outside `tests/Mentoory.Tests.E2E/` and `specs/017-audit-e2e/` may be modified. Any deviation MUST be called out in the commit body AND the next RESUME's Gotchas section.
- **Manual-mode JSON shape**: `Details` for `CorrectAnswerCommand` carries `Before.TextValue` / `After.NewTextValue` — pinned by `specs/016-audit-pipeline/data-model.md`. (Relevant for P2, not P1, but do not change this shape.)

---

## Gotchas discovered in P0 (planning)

- **`PlaywrightFixture` currently has no Respawn** (research R-01). Grep of the fixture returns zero hits for `Respawn` or `SchemasToInclude`. `InitializeDatabase()` deploys the DACPAC once at fixture startup and never resets. T001 is the fix; without it every test inherits the prior test's audit rows and the "exactly one row for CorrectAnswer" assertion in P2 cannot work. Verify the fix by running any single test twice and confirming clean state each time.
- **`AuthorizationTests.LoginAsync` contains the only existing pattern for the GlobalAdmin context-selector**. The post-login `/Context/Select` flow requires two cascading dropdowns (role, incubator) before the Confirmar button becomes enabled. The existing implementation uses `WaitForFunctionAsync` to wait for `options.length > 1` on each select. Copy this pattern verbatim — do not reinvent.
- **`CorrectAnswer` UI lives at `/Coordination/AnswerCorrection/{diagnosticExternalId:guid}`** (research R-02). Not needed for P1 but useful context — do not look for it under `/Administration`.
- **E2E test project has been depending on `WarningsNotAsErrors=NU1902`** (pre-existing MailKit CVE carried from 016). Every `dotnet build` / `dotnet test` invocation MUST pass `-p:WarningsNotAsErrors=NU1902` or it fails on the CVE alone.
- **Existing E2E test cost**: the 97 existing tests take ~3 min wall-clock. Adding 24 tests at the ≤7 s/test budget keeps the suite under ~5 min. Session P1 adds 9 tests — should complete well under the budget.

---

## Definition of done for P1

- All nine US1 tests green:
  ```
  dotnet test tests/Mentoory.Tests.E2E/ \
    --filter "FullyQualifiedName~AuditLogViewerTests" \
    -p:WarningsNotAsErrors=NU1902
  ```
- All lower-layer suites still green:
  ```
  dotnet test tests/Mentoory.Shared.Application.Tests/ \
               tests/Mentoory.Tests.Architecture/ \
               tests/Mentoory.Tests.Integration/ \
    -p:WarningsNotAsErrors=NU1902
  ```
- Total `AuditLog*` runtime under 3 min (expect ≤ 1 min for just 9 tests).
- No diff outside `tests/Mentoory.Tests.E2E/` and `specs/017-audit-e2e/`:
  ```
  git diff develop..HEAD -- ':!tests' ':!specs'
  ```
  Must emit zero lines.
- **Commit message** (exact title, load-bearing for SC-004):
  ```
  Add E2E coverage for audit pipeline — phase 1 (Viewer shell)
  ```
  Body lists the 9 tests + 3 shared-infrastructure files added.

---

## Next handoff

After the phase commit is green and pushed:

1. Write `specs/017-audit-e2e/RESUME-P2.md` following `contracts/resume-prompt-schema.md` verbatim. Theme: `Capture flows`. Copy this file's Invariants section VERBATIM into it. Populate Section 1 with the 9 test methods shipped + the 3 shared helper files. Populate Gotchas with anything non-obvious learned during P1. Section 2's test-method list comes from `tasks.md § Phase 3 US2` (T015-T021).
2. Commit with title `Add RESUME-P2 after phase 1 checkpoint`, push.
3. **End the session.** Do NOT start Phase 2 in this context. The next session claims it by reading `RESUME-P2.md` fresh.

## Final session message (paste verbatim)

```
Phase 1 shipped: 9 new tests + 3 shared-infrastructure helpers.
Phase commit: {sha}
RESUME commit: {sha}
Next session: read specs/017-audit-e2e/RESUME-P2.md
Cumulative AuditLog* tests: 9, runtime {seconds}s
```
