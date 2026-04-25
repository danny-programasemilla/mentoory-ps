# Feature Specification: Audit Pipeline — Browser E2E Coverage

**Feature Branch**: `017-audit-e2e`
**Created**: 2026-04-19
**Status**: Draft
**Input**: Add comprehensive browser-driven E2E coverage for feature 016-audit-pipeline using Playwright, phased across four independently shippable checkpoints to prevent AI-session drift.

## Purpose

Close the E2E gap around feature 016-audit-pipeline by adding browser-driven Playwright tests that prove — from a real user's seat — that every requirement delivered in the feature actually works in the running app. Equally, make the work itself resilient to AI-session drift by splitting execution into four independently shippable phases, each ending in a mandatory commit+push+clear+resume checkpoint so no single session ever tries to hold the whole backlog in context.

Integration tests assert contracts at the handler and middleware seams, but no test confirms a GlobalAdmin can actually see the audit row through the viewer, or that a non-admin actually cannot. Feedback loop on regressions in the viewer's Spanish copy, filter dropdowns, menu visibility, and redaction is currently manual — which means drift between the spec and delivered UX is only caught at release.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Viewer, menu, and authorization proven in a real browser (Priority: P1)

A Platform Admin opens the browser, navigates to the audit log viewer, and sees the table render with all Spanish labels in place. A non-admin attempting the same URL is blocked. The menu entry appears under "Plataforma" for GlobalAdmin and is absent for every lower role.

**Why this priority**: The viewer is the single user-visible surface of feature 016. If it regresses silently (CSS break, controller `[Authorize]` attribute drift, menu misconfiguration), the feature is functionally dead even if the capture pipeline works. P1 locks that baseline.

**Independent Test**: Run the P1 test file in isolation against a fresh DACPAC-seeded database. Green means the viewer's authorization surface and Spanish shell are correct; red names the exact missing element.

**Acceptance Scenarios**:

1. **Given** a GlobalAdmin session, **When** the user navigates to `/Administration/AuditLog`, **Then** the page returns HTTP 200 and the table shows six Spanish column headers (`Fecha (UTC)`, `Evento`, `Usuario`, `Acción`, `Resultado`, `Rol`).
2. **Given** an IncubatorAdmin / Entrepreneur / Mentor / Sponsor session, **When** the user attempts `/Administration/AuditLog`, **Then** the response is HTTP 403 or a redirect to AccessDenied/Login.
3. **Given** a GlobalAdmin session, **When** the page's left menu renders, **Then** "Registro de auditoría" is visible under the "Plataforma" group.
4. **Given** an IncubatorAdmin session, **When** the menu renders on any page, **Then** "Registro de auditoría" is not present.
5. **Given** the viewer is open with a filter that matches zero rows, **When** the table refreshes, **Then** the Spanish empty-state copy appears.
6. **Given** the viewer is open, **When** the Outcome filter dropdown is inspected, **Then** it offers exactly `Todos`, `Éxito`, `Fallo`.

### User Story 2 - End-to-end capture flows visible in the viewer (Priority: P1)

Auditable actions taken through the UI (register, login, assign role, correct answer) produce correctly-shaped rows that a Platform Admin can locate, filter, and expand in the viewer. Sensitive payload fields never appear verbatim in the Details column.

**Why this priority**: The value of feature 016 is that sensitive actions leave a trustworthy trail. P2 proves that trail forms through the real user journey, not just through a dispatched MediatR command.

**Independent Test**: Drive each retrofitted command through its UI, then open the viewer in the same browser session and assert the row's fields. Green means the capture path is intact end-to-end.

**Acceptance Scenarios**:

1. **Given** the registration form, **When** a user submits it with a password, **Then** the viewer shows a row with `EventType = User.Registered`, `Outcome = Éxito`, `UserEmail` populated, and the expanded Details panel contains `***REDACTED***` in place of the plaintext password.
2. **Given** an existing user, **When** someone submits the login form with the wrong password, **Then** the viewer shows a row with `EventType = User.LoggedIn`, `Outcome = Fallo` for that email.
3. **Given** an existing user, **When** they submit the login form with the correct password, **Then** the viewer shows a row with `EventType = User.LoggedIn`, `Outcome = Éxito`.
4. **Given** a GlobalAdmin on the admin Users page, **When** they assign a role to a user, **Then** the viewer shows `EventType = Role.Assigned`, `Outcome = Éxito`, `UserEmail` equal to the actor's email.
5. **Given** a completed diagnostic response, **When** a coordinator corrects an answer, **Then** the viewer shows a row with `EventType = Answer.Corrected` whose expanded Details contains both the previous answer (`Before.TextValue`) and the new answer (`After.NewTextValue`).
6. **Given** a CorrectAnswer command was dispatched exactly once, **When** the viewer is filtered by that action, **Then** exactly one row is present — proving Manual-mode does not double-write.
7. **Given** any audit row in the viewer, **When** the expand button is clicked, **Then** a pretty-printed JSON Details panel unfolds below the row.

### User Story 3 - Correlation headers propagate from the browser (Priority: P2)

Every response from the application carries an `X-Correlation-Id` header containing a valid GUID, and a client-supplied header is echoed back verbatim when it is a valid GUID.

**Why this priority**: Correlation is the auditor's breadcrumb trail when reconstructing incidents. Without browser-level proof, a middleware registration change could silently drop the header and the integration test alone might not catch it.

**Independent Test**: Issue raw HTTP requests via Playwright's network API, inspect response headers.

**Acceptance Scenarios**:

1. **Given** no `X-Correlation-Id` is sent, **When** the browser GETs any page, **Then** the response carries a valid-GUID `X-Correlation-Id` header.
2. **Given** the client sends `X-Correlation-Id: {known-guid}`, **When** the browser GETs any page, **Then** the response's `X-Correlation-Id` equals that known GUID.
3. **Given** two sequential GET requests without the header, **When** responses return, **Then** their `X-Correlation-Id` values are different GUIDs.

### User Story 4 - Negative regression harness + Spanish copy QA (Priority: P3)

A final tier of tests encodes invariants that should never break: the plaintext password never leaks, every Spanish copy string is exact, validation-rejected commands do not produce audit rows.

**Why this priority**: These are defensive — most of them overlap with unit-level coverage. At the E2E layer they exist to catch regressions that slip past lower tiers (e.g., a frontend framework change that re-serializes the form and leaks fields).

**Independent Test**: Run the P4 file; all assertions should be declarative and side-effect-free.

**Acceptance Scenarios**:

1. **Given** all rows captured during the P2 test run, **When** each Details body is scanned, **Then** the literal test password never appears.
2. **Given** the filter bar is visible, **When** its buttons are inspected, **Then** the labels are exactly `Filtrar` and `Limpiar`.
3. **Given** the table pagination controls render, **When** their labels are read, **Then** they are exactly `Primero`, `Siguiente`, `Anterior`, `Ultimo`.
4. **Given** an `Outcome = Success` row, **When** the badge renders, **Then** its text is `Éxito` with the success color; for `Outcome = Failure` it is `Fallo` with the danger color.
5. **Given** a command that fails FluentValidation (e.g., invalid email in the registration form), **When** the viewer is queried afterward, **Then** no audit row exists for that attempt — proving the pipeline order is `Validator → Auditing → Transaction`.

### Edge Cases

- The E2E fixture must respawn the `audit` schema between tests (confirmed on the integration fixture but not on `PlaywrightFixture`). Phase 1 starts by verifying this.
- Playwright timing: every `WaitForLoadStateAsync` / `WaitForFunctionAsync` MUST carry an explicit timeout ≤ 15 s. Default-timeout waits are forbidden.
- If a single session cannot complete its phase (context exhaustion), it writes a partial `RESUME-P{N}.md` with `status: in-progress` and the remaining test-method list; the next session resumes at those tests with no reset.
- If DACPAC seed-data changes mid-project, P1 tests MUST re-confirm the seeded user list via the invariants block in the resume prompt before any phase resumes.
- The `Details` JSON shape for Manual-mode (`Before.TextValue` / `After.NewTextValue`) is pinned by `specs/016-audit-pipeline/data-model.md § AuditLogDto`; changes there require a coordinated P2 update.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The E2E suite MUST add exactly 24 new Playwright tests under `tests/Mentoory.Tests.E2E/Tests/AuditLog*.cs`, distributed across four files (one per phase).
- **FR-002**: Phase 1 MUST cover viewer render, five role-based authorization denials, menu visibility (GlobalAdmin positive + IncubatorAdmin negative), empty-state Spanish copy, and the Outcome filter option list.
- **FR-003**: Phase 2 MUST drive the real UI for Register, Login (success + failure), Assign Role, and Correct Answer, and MUST then assert the resulting row's EventType, Outcome, UserEmail, and (for CorrectAnswer) Before/After Details values via the viewer.
- **FR-004**: Phase 2 MUST assert that the password field in the registration Details appears only as `***REDACTED***` and that the plaintext password string is absent from the Details body.
- **FR-005**: Phase 2 MUST prove Manual-mode single-write by asserting exactly one audit row per CorrectAnswer dispatch.
- **FR-006**: Phase 3 MUST assert that every response carries a valid-GUID `X-Correlation-Id`, that a client-supplied valid GUID is echoed verbatim, and that two header-less requests receive distinct GUIDs.
- **FR-007**: Phase 4 MUST include a negative regression assertion that a validation-rejected command produces no audit row.
- **FR-008**: Phase 4 MUST assert Spanish copy for the filter bar buttons (`Filtrar`, `Limpiar`), pagination labels (`Primero`, `Siguiente`, `Anterior`, `Ultimo`), and Outcome badges (`Éxito`, `Fallo`).
- **FR-009**: Each phase MUST end in a mandatory session checkpoint consisting of: running the new tests green, running the lower-layer suites green, committing with the prescribed message format, pushing to origin, writing the next phase's resume prompt, committing and pushing that prompt, then ending the session.
- **FR-010**: Each `RESUME-P{N}.md` file MUST contain the five canonical sections: What was shipped, What to do next, Invariants to preserve, Gotchas discovered, Definition of done. The invariants block is duplicated across resume files intentionally (drift signal).
- **FR-011**: A session that cannot complete its phase MUST write a partial `RESUME-P{N}.md` with `status: in-progress` listing the pending test methods, commit what is green, push, and end.
- **FR-012**: The next session MUST begin by reading the spec, reading the latest `RESUME-P{N}.md`, pulling latest from origin, and running the prior phase's tests to confirm a green baseline BEFORE writing any new code.
- **FR-013**: All new tests MUST rely only on DACPAC-seeded users and Respawn-cleaned state; no hardcoded IDs, no fixed timestamps.
- **FR-014**: All new tests MUST reuse the existing `PlaywrightFixture` and the login helper pattern from `AuthorizationTests.LoginAsync`. No new fixtures, no parallel login helpers.
- **FR-015**: All Playwright waits MUST carry an explicit timeout of 15 seconds or less.
- **FR-016**: Phase 1 MUST verify that `PlaywrightFixture` respawns the `audit` schema between tests; if not, extend the fixture as the first task of Phase 1.
- **FR-017**: No production code outside `tests/Mentoory.Tests.E2E/` and `specs/017-audit-e2e/` may be modified across the four phases.
- **FR-018**: The session that closes Phase 4 MUST write `RESUME-COMPLETE.md` summarizing all four phases in human-readable Markdown.

### Key Entities

- **Phase**: One of four independently-shippable delivery units (P1-P4), each producing a single test file, a commit, a push, and a resume prompt for the next phase.
- **Resume Prompt**: A file at `specs/017-audit-e2e/RESUME-P{N}.md` with exactly five sections that allows a fresh AI session to bootstrap deterministically.
- **Invariants Block**: The subsection of a resume prompt listing seeded users, fixture conventions, login helper source, and Spanish copy constraints. Duplicated across all resume files so each file is self-sufficient.
- **Checkpoint**: The seven-step exit ritual at the end of each phase (run, verify, commit, push, write-resume, commit-resume, push-resume, end-session).
- **Test Coverage Matrix**: The grouping of the 24 tests across four phases, one row per test, binding each to its acceptance scenario.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 24 new E2E tests exist under `tests/Mentoory.Tests.E2E/Tests/AuditLog*.cs` and `dotnet test tests/Mentoory.Tests.E2E/ --filter "FullyQualifiedName~AuditLog"` exits 0.
- **SC-002**: The new `AuditLog*` tests together complete in under 3 minutes wall-clock on the reference developer machine.
- **SC-003**: `Mentoory.Shared.Application.Tests`, `Mentoory.Tests.Architecture`, and `Mentoory.Tests.Integration` remain green after each phase's checkpoint — no regression in the layers below E2E.
- **SC-004**: `git log --oneline develop..HEAD -- specs/017-audit-e2e/ tests/Mentoory.Tests.E2E/Tests/AuditLog*.cs` shows exactly four phase commits, one per phase, with the prescribed message format. No squashes, no amends.
- **SC-005**: `git diff develop..HEAD -- ':!tests' ':!specs'` emits zero lines — production code is untouched across the delivery.
- **SC-006**: All four resume prompt files (`RESUME-P2.md`, `RESUME-P3.md`, `RESUME-P4.md`, `RESUME-COMPLETE.md`) exist in git history, each authored by a different session (distinct commit timestamps, ideally distinct `git log --format=%aI` dates).
- **SC-007**: Across the four phases, zero session writes to code outside `tests/Mentoory.Tests.E2E/` without flagging the deviation in its commit body and resume prompt's "Gotchas" section.
- **SC-008**: A fresh AI session given only `spec.md` and the latest `RESUME-P{N}.md` can bootstrap Phase N+1 within the first five tool calls — no archaeology of prior transcripts required.

## Assumptions

- Feature 016-audit-pipeline is already merged to `develop` (PR #12).
- DACPAC-seeded test users remain unchanged from their current definitions: `admin@mentoory.com` (GlobalAdmin, password `123abc987`), `incadmin1@test.mentoory.com` / `entrepreneur1@test.mentoory.com` / `sponsor1@test.mentoory.com` / `mentor1@test.mentoory.com` (all password `Test123!@#`).
- The admin viewer UI at `/Administration/AuditLog` is the single user-facing surface for audit data.
- `PlaywrightFixture` continues to run Chromium only; cross-browser coverage is explicitly out of scope.
- The DACPAC database schema is built before E2E tests run (either via `dotnet build Mentoory.Db/` or via the existing CI pipeline).
- `RESUME-COMPLETE.md` is human-readable Markdown, not a structured CI artifact. If a CI gate is added later, a companion `RESUME-COMPLETE.json` can be generated then.

## Out of Scope

- Performance / load testing of the viewer — SC-007 from feature 016 remains a manual sign-off task.
- Visual regression / screenshot baselines — Spanish copy assertions use DOM text, not image diffs.
- E2E tests for commands that have no UI flow (`AssignMentor`, `RegisterInternalUser` were decorated with `[Audited]` in 016 but are covered only at the integration layer until a UI surface exists).
- Test coverage for background-hosted service dispatches (no HTTP surface to test via browser).
- Cross-browser coverage — Chromium only, matching the existing E2E convention.
- Test coverage for audit rows produced by commands dispatched OUTSIDE a browser request (e.g., background workers). Such commands are not exercised by the UI layer.
