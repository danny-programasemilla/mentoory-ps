# Feature Specification: E2E coverage for Project Lifecycle (feature 016)

**Parent feature**: `016-project-lifecycle-finish`
**Feature Branch**: `016-project-lifecycle-finish` (this work extends the existing PR #11)
**Created**: 2026-04-19
**Status**: Draft
**Input**: User description: "Create all the E2E necessary to guarantee alignment between requirements and delivery quality. Include checkpoints where to pause and provide instructions on each pause to guarantee quality and prevent drift. Cover all possible scenarios from the recent work."

## Purpose

Feature 016 ships the Coordinator UI for project lifecycle advancement, the 7-stage timeline, stage-gated actions, and the `RequiresStageAttribute` server-side guard. Unit and integration tests cover the handler layer, the stage-action registry matrix, and the rowversion concurrency path. The **user-facing surface** — pages, redirects, toasts, locked-action affordances, role/scope denial paths, and audit-trail rendering — has zero automated coverage. Tasks T042, T051, and T056 in the parent feature's `tasks.md` are marked as manual walkthroughs and have not been executed on this branch.

This spec delivers:

1. **Automated E2E coverage (Playwright)** for every acceptance scenario, testable edge case, and automatable success criterion in `specs/016-project-lifecycle-finish/spec.md`.
2. **A checkpoint protocol** that lets the implementation execute safely across multiple AI sessions without drift. Each chunk self-contains its goal, invariants, and handoff in a pre-authored resume prompt.

The problem solved is twofold: (1) the UI-regression risk that shipped yesterday's `Coordinator_ShouldClone_FormTemplate` failure unchecked, and (2) the quality risk of long-running AI sessions producing inconsistent work across a 15–20-test suite.

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Test suite covers the coordinator's advance flow (Priority: P1)

A maintainer needs confidence that future changes to the coordination area, the advance command, or the `Project` aggregate do not silently break the core coordinator workflow (open project → see timeline → advance → see new state).

**Why this priority**: Advance is the P1 scenario in feature 016 and the one with the highest regression surface (command + controller + view + toast + audit). Without E2E coverage, any refactor or middleware change touches it blindly.

**Independent Test**: Run the Playwright suite. The suite asserts that a coordinator, given a seed project in Registration, can drive it to Forms through the UI; that the audit trail renders; and that every failure branch named in `spec.md` produces the exact Spanish message a user would see.

**Acceptance Scenarios**:

1. **Given** the full test suite is run on a clean Testcontainers + DACPAC environment, **When** all Walkthrough-1 tests execute, **Then** the 4 US1 scenarios from the parent spec all pass, including the Registration→Forms happy path, Closure-rejection, Completed-stage-rejection, and role-based action hiding.

### User Story 2 — Test suite covers the Lifecycle view across project states (Priority: P2)

**Why this priority**: The Lifecycle page aggregates multiple sources (stage data, audit trail, stage-gated actions, role/scope checks). A UI regression here is hard to catch without an end-to-end check that loads the page in a real browser against the real schema.

**Independent Test**: The Walkthrough-2 test file, run in isolation, asserts the page renders correctly for three distinct project states: brand-new, mid-lifecycle, closed.

**Acceptance Scenarios**:

1. **Given** the Walkthrough-2 tests run against a project seeded at each of the three states (brand-new Registration, Forms-with-Registration-completed, all-7-completed), **When** the tests inspect the rendered page, **Then** each stage row shows the correct state badge and timestamp per feature 016's FR-010/FR-011, and the advance button is shown or hidden per FR-015.

### User Story 3 — Test suite covers stage-gated action affordances and the server-side guard (Priority: P3)

**Why this priority**: Stage gating is the most rule-dense part of feature 016 and the part most likely to silently erode under refactors (e.g., an attribute moved off a controller, a registry entry reordered, an ARIA attribute lost). Coverage here prevents the "I can still open it via URL" class of bugs.

**Independent Test**: The Walkthrough-3 and Walkthrough-4 test files assert the six-card Actions grid renders in the correct state for each stage, that the locked-card tooltip names the unlocking stage, that direct URLs to locked actions redirect with a Spanish toast, and that cross-role / cross-tenant / no-context attempts are blocked with the platform-consistent denial paths.

**Acceptance Scenarios**:

1. **Given** a project in Registration, **When** Walkthrough-3 runs, **Then** all 6 cards appear in Locked state with the "Disponible desde la etapa *<Spanish stage name>*" helper text.
2. **Given** the same project is advanced to Analysis, **When** the Actions grid reloads, **Then** `AnswerCorrection` is Available (primary styling, active URL) and `DiagnosticForms` is Past (muted + checkmark).
3. **Given** a coordinator without the active project's incubator context, **When** Walkthrough-4 runs, **Then** the navigation redirects to the context selector with the standard warning toast.

### Edge Cases

Execution-time edge cases for the E2E-writing process itself are covered in the spec body (Error Handling + Edge Cases sections below). Edge cases from feature 016 that **are** automated here:

- Concurrent double-advance surfaces the `LifecycleConcurrencyConflict` Spanish toast on the second attempt (Walkthrough 5).
- Advance attempt against an inactive project surfaces the "inactive" Spanish message (Walkthrough 5).
- Cross-incubator project lookup returns a 403-equivalent denial (Walkthrough 4).
- Coordinator with no incubator context is redirected to the context selector (Walkthrough 4).

Edge cases from feature 016 that **are not** automated here with rationale:

- "Legacy stage-gated action started before this feature shipped" — no realistic seed path exists in the current codebase. Parked to `open-questions.md` with `Cannot-test-as-specified`.

## Requirements *(mandatory)*

### Functional Requirements

**R1 — Test coverage.** The suite MUST have one Playwright test per scenario below. Total ≈ 16 tests.

| # | Walkthrough | Covers (from parent spec) | Test intent |
|---|-------------|--------------------------|-------------|
| 1 | WT1 — Advance | US1 §1 | Registration→Forms advance via UI, asserts toast "Proyecto avanzado a Formularios." and URL stays on Lifecycle |
| 2 | WT1 — Advance | US1 §2 | Closure project: advance button absent; direct POST returns rejection |
| 3 | WT1 — Advance | US1 §3 | Stage in Completed state: advance returns `StageNotInProgress` Spanish message |
| 4 | WT1 — Advance | US1 §4 | Non-coordinator role: advance button absent on the view; direct POST to `/Coordination/Projects/AdvanceStage/{id}` returns 403 or login redirect |
| 5 | WT2 — Lifecycle page | US2 §1 | Mid-lifecycle (Registration completed, Forms in progress): correct state badges + timestamps on exactly those stages |
| 6 | WT2 — Lifecycle page | US2 §2 | Brand-new project: only Registration shows `En progreso`; others show `No iniciada` with no timestamps |
| 7 | WT2 — Lifecycle page | US2 §3 | Closed project: all 7 rows show `Completada`; no advance button |
| 8 | WT3 — Gated actions | US3 §1 | Project in Registration: 6 cards all Locked with "Disponible desde la etapa *<stage>*" tooltip |
| 9 | WT3 — Gated actions | US3 §2 | Advance Forms→Analysis: Forms-cards become Past (muted + check), Analysis-card becomes Available |
| 10 | WT3 — Gated actions | US3 §3 | Direct GET `/Coordination/AnswerCorrection` while in Registration → 302 to Lifecycle + TempData warning rendered as Spanish toast |
| 11 | WT3 — Gated actions | US3 §4 | Locked-card tooltip's text exactly matches `StageTypeDisplay.ToSpanish(gatingStage)` |
| 12 | WT4 — Role / scope | Edge case: cross-incubator | IncubatorAdmin A attempts `/Coordination/Projects/Lifecycle/{incubator-B-project-id}` → 403 (not 500, not data leak) |
| 13 | WT4 — Role / scope | Edge case: no context | Coordinator without active incubator claim → redirect to context selector with standard warning |
| 14 | WT4 — Role / scope | US1 §4 via Mentor | Mentor (listed in menu group but not controller role list) hitting `/Coordination/Projects` → 403 or denial page in Spanish |
| 15 | WT5 — Audit + concurrency | SC-005 | After 3 sequential advances by 3 distinct coordinators, the Lifecycle page shows each advancer's display name next to the corresponding completed stage |
| 16 | WT5 — Audit + concurrency | Edge case: concurrency | Two coordinators issue Advance at nearly the same time — first succeeds, second surfaces `LifecycleConcurrencyConflict` Spanish toast |
| 17 | WT5 — Audit + concurrency | Edge case: inactive | Advance attempt against a deactivated project surfaces the "inactive" Spanish message and does not change stage |

Test #17 is optional if US2 §3 (all-stages-complete rendering) already covers the "no advance" visual; it's listed separately because the message text differs from the Closure branch.

**R2 — Fixtures.** Chunk C0 MUST deliver:

- A test-time helper that seeds or promotes a project to a given `StageType` without touching the UI. Implementation mechanism is a plan-phase decision (direct DbContext write, scoped command send, or API call) — spec is neutral.
- A helper that creates an inactive project.
- A helper that creates two incubators with at least one project in each, for cross-incubator tests.
- Login helpers for each of the four roles used by the suite (`ProjectCoordinator`, `Mentor`, `IncubatorAdmin`, `GlobalAdmin`), plus a helper to switch between coordinators for the audit-trail test.
- A `LifecyclePageObject` that exposes: the timeline rows keyed by `StageType`, the advance button, the Actions grid keyed by `StageGatedAction`, and the toast region. Tests MUST NOT reach into raw Playwright selectors outside this page object.
- One smoke test that proves the fixtures wire correctly: login as `ProjectCoordinator`, navigate to `/Coordination/Projects`, assert the list renders with at least the seeded project's name.

**R3 — Checkpoint protocol.** Execution is chunked strictly into six sessions, executed in order:

- **C0 — Foundation.** Fixtures + page objects + smoke test only. No scenario tests.
- **C1 — Walkthrough 1 (Advance).** Tests 1–4 from R1.
- **C2 — Walkthrough 2 (Lifecycle page).** Tests 5–7 from R1.
- **C3 — Walkthrough 3 (Gated actions).** Tests 8–11 from R1.
- **C4 — Walkthrough 4 (Role / scope).** Tests 12–14 from R1.
- **C5 — Walkthrough 5 (Audit + concurrency + inactive).** Tests 15–17 from R1.

Each chunk: `dotnet build` green (0 errors, 0 **new** warnings — one pre-existing SDK-version hint on `Mentoory.Db` is tolerated) → this-chunk's tests green → **full** `dotnet test` green → `/simplify` applied → single commit → push → update `coverage-matrix.md`. The resume prompt for the next chunk is NOT modified by the current session (it was authored during the spec phase).

Chunks execute strictly in order. C0 → C1 → C2 → C3 → C4 → C5. A session that finds C(N-1) incomplete MUST stop and surface, not proceed.

**R4 — Resume prompt structure.** Each `checkpoints/CN-*.md` MUST contain the following sections, in order:

1. **Goal** — one paragraph stating what this chunk must produce and why.
2. **Prior state** — which commits (by chunk, not hash — hashes are filled during execution) should be on the branch, and what shipped in them.
3. **This chunk — tests to write** — explicit list with test name, scenario ref, and key assertions.
4. **Fixtures / helpers this chunk may use** — references C0's artifacts.
5. **Invariants** — what MUST NOT change (product code, C0 fixtures, existing tests, Spanish string literals).
6. **Pre-flight checklist** — commands to run and their expected output.
7. **Execution steps** — ordered bullets.
8. **Exit gate** — conditions that must be true before committing.
9. **Commit message template** — exact wording shell.
10. **Stop conditions** — list of scenarios that require stopping and surfacing rather than continuing.
11. **After-push** — pointer to the next chunk file.

### Non-Functional Requirements

- **Determinism.** No `Thread.Sleep`, no fixed millisecond waits on DOM. Use Playwright's `WaitFor` primitives on concrete DOM states.
- **Placement.** All test files under `tests/Mentoory.Tests.E2E/Tests/Lifecycle/`. Fixtures under `tests/Mentoory.Tests.E2E/Infrastructure/Lifecycle/`.
- **Zero new warnings.** `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` stays enforced.
- **Spanish-only UI assertions.** Toast strings and labels asserted against exact literals from `ProjectsController.ResolveSpanishMessage`, `StageTypeDisplay.ToSpanish`, `StageActionDisplay.ToSpanish`. No paraphrase.
- **Shared fixture, per-test isolation.** Testcontainers container starts once per test class; state reset between tests via the C0 helper — not by rebuilding the container.
- **No new NuGet packages.** Uses the existing Playwright / Testcontainers.MsSql / xUnit / FluentAssertions / Respawn stack.

## Success Criteria *(mandatory)*

- **SC-E1** — All 16–17 tests exist at the paths in R2/R3 and pass 100% on `dotnet test` against a clean Testcontainers + DACPAC environment.
- **SC-E2** — `dotnet build` 0 errors and 0 **new** warnings (the single pre-existing `Microsoft.Build.Sql` SDK-version hint is tolerated). `dotnet test` 0 failed across **every** test project (not just E2E). Zero unexplained skips: any `[Fact(Skip=...)]` added under E2 MUST have a matching entry in `open-questions.md` citing the product-code issue and the chunk that added it.
- **SC-E3** — Six chunks land as exactly six sequential commits on `016-project-lifecycle-finish`, in order, each with the commit message from its resume prompt template. No squash-rewrites.
- **SC-E4** — Each resume prompt is self-contained: a fresh AI session with no prior conversation context can paste the prompt and complete the chunk without grepping prior history.
- **SC-E5** — No product code modified by chunks C1–C5. Fixtures (C0 + any reveals) and documentation are the only non-test changes. Product-code bugs surface as `[Fact(Skip=...)]` + TODO in the chunk's resume prompt + user notification.
- **SC-E6** — Every acceptance scenario in parent `spec.md`, every automatable edge case, and SC-001 / SC-004 / SC-005 map to at least one test via `coverage-matrix.md`. Non-automatable criteria (SC-002, SC-003, SC-006, SC-007) are explicitly marked as such in the matrix.
- **SC-E7** — After C5, parent `tasks.md` T042 / T051 / T056 are closed (`[X]` if covered verbatim, `[~]` with rationale if covered differently).

## Error Handling

How the **execution of this spec** responds to problems:

- **E1 — Build breaks during a chunk.** Not committed. Session stops, updates its resume prompt with a `## Blocked` section naming the error and relevant file:line, surfaces to user. Next session re-enters the same chunk.
- **E2 — A new test fails because it exposed a product-code bug.** Session does NOT fix product code. Writes test as `[Fact(Skip = "blocked by <short description>")]`, adds TODO in the chunk's resume prompt under `## Blocked by product-code issue`, commits the skip + TODO together, pushes, surfaces for triage.
- **E3 — A new test fails because an existing fixture is wrong.** Session MAY fix the fixture; fixtures are test infrastructure, not product code. Fix is included in the same commit that revealed it; commit message names the fixture change.
- **E4 — Testcontainers / Docker unavailable.** Session does not run the suite. Surfaces exact command that failed and exact Docker error. Commits nothing.
- **E5 — `/simplify` reveals cross-chunk duplication.** Session MUST NOT silently rename or move prior-chunk helpers. Records the refactoring opportunity in `open-questions.md` for a post-suite cleanup pass.
- **E6 — Resume prompt drift (expected commit not present, expected helper renamed).** Session STOPS immediately with a one-paragraph diagnostic. No reconstruction attempts.
- **E7 — Full-suite regression.** A previously-passing test (in any project) now failing ⇒ chunk is broken. Revert working-tree, do not commit, surface regression.

## Edge Cases

Unusual situations during execution:

- **EC1 — Session interrupted mid-chunk.** Partial work not pushed. Next session inspects working tree: if intact, continues; if reset, restarts the chunk cleanly. Resume prompt states which path applies based on git state.
- **EC2 — Test count for a chunk drifts.** If a chunk plans 4 tests and needs 6, expansion stays in-chunk. Net reduction works the same way. Noted in the chunk's resume prompt under `## Scope adjustment`. Plan is not reshaped across chunks.
- **EC3 — Feature 016 code changes during execution.** Next session's pre-flight runs `git log --oneline origin/develop..HEAD -- <relevant paths>` and stops if anything unexpected appears.
- **EC4 — Spec scenario infeasible in current code.** Recorded in `open-questions.md` with `Cannot-test-as-specified` tag. Coverage matrix shows `Not covered (reason)` rather than a fake pass.
- **EC5 — Concurrent branch activity.** Someone else commits to `016-project-lifecycle-finish` between chunks ⇒ pre-flight detects divergence and stops. No auto-rebase, no force-push.

## Dependencies

- **D1 — Feature 016 code landed.** Controllers, filter, handler, views must be present. True at commit `7a7942d`.
- **D2 — Seed data baseline.** Seed projects have their 7 `ProjectStages` rows. Fixed at commit `7a7942d`.
- **D3 — Testcontainers + DACPAC infrastructure.** `MentooryWebApplicationFactory`, `PlaywrightFixture`, `Respawn`. Present; 97 existing E2E tests green at `7a7942d`.
- **D4 — Docker daemon running on executing host.** Validated in each chunk's pre-flight.
- **D5 — Spanish display strings stable.** Literals in `ProjectsController.ResolveSpanishMessage`, `StageTypeDisplay.ToSpanish`, `StageActionDisplay.ToSpanish` do not change during execution; if they do, EC3 applies.
- **D6 — PR #11 stays open until C5 lands.** Current branch receives the 6 additional commits.

## Out of Scope

- Non-automatable success criteria (SC-002 timing, SC-003 / SC-006 usability, SC-007 support metric).
- Performance / load testing of the Lifecycle page.
- Accessibility audit beyond the functional tooltip text assertion.
- Visual regression / pixel diffing.
- Cross-browser matrix (Chromium only, matching existing `PlaywrightFixture` convention).
- General HTTP controller test infrastructure (the thing T026 wanted and deferred — still out of scope).
- Preserving T042 / T051 / T056 as standalone manual playbooks once automated equivalents ship.
- Any product-code changes across chunks C1–C5.
- Modifications to feature 016's own spec.

## Open Questions

No questions remain open at spec-creation time. Execution-time parking lot is `open-questions.md` in this directory.

## Assumptions

- The existing `PlaywrightFixture` exposes (or can be extended to expose without new product-code changes) a service provider or HTTP client that allows programmatic project creation and stage advancement for test setup. Plan phase will verify and, if not, extend the fixture as part of C0.
- Seeded test users (`coord1@`, `coord2@`, `coord3@`, `incubator-admin-1@`, `mentor1@`, `global-admin@` or equivalents from `004.SeedTestData.sql`) are sufficient to drive all role/scope tests. If any needed test user is missing, C0 adds them to the seed file — that fixture change is in-scope per E3.
- The Spanish literal strings in `ResolveSpanishMessage` / `StageTypeDisplay` / `StageActionDisplay` are the authoritative UI copy for this release and won't change during the ~1-day execution window.
- Chunks complete in order with no parallel sessions. A single human orchestrates handoffs.
