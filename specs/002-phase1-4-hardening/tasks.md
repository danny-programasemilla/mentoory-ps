# Tasks: Phase 1-4 Hardening

**Input**: Design documents from `/specs/002-phase1-4-hardening/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, quickstart.md

**Tests**: E2E tests are explicitly required by FR-017. Manual validation playbooks are required by FR-019.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (E2E Test Infrastructure)

**Purpose**: Configure Playwright and establish E2E test project foundation

- [X] T001 Add Playwright NuGet packages (Microsoft.Playwright, Microsoft.Playwright.NUnit or xUnit adapter) to tests/Mentoory.Tests.E2E/Mentoory.Tests.E2E.csproj
- [X] T002 Create Playwright base test class and configuration (base URL, browser settings, screenshot on failure) in tests/Mentoory.Tests.E2E/Infrastructure/PlaywrightFixture.cs

---

## Phase 2: Foundational (Critical Bug Fixes)

**Purpose**: Fix critical bugs that BLOCK all user stories. These are security and crash fixes.

**CRITICAL**: No user story work can begin until this phase is complete.

- [X] T003 Fix Participant DiagnosticController: (a) replace `User.FindFirst("UserId")` with `User.FindFirst(ClaimTypes.NameIdentifier)` (Line 81) and (b) fix [Authorize] roles to `[Authorize(Roles = "Entrepreneur,Mentor,ProjectCoordinator,IncubatorAdmin,GlobalAdmin")]` (Line 12) in Mentoory.Web/Areas/Participant/Controllers/DiagnosticController.cs — ref UF-02, DC-01
- [X] T004 [P] Fix "UserId" claim bug: replace `User.FindFirst("UserId")` with `User.FindFirst(ClaimTypes.NameIdentifier)` in Mentoory.Web/Areas/Coordination/Controllers/AnswerCorrectionController.cs (Line 41) — ref UF-02
- [X] T005 [P] Register TenantContextMiddleware: add `app.UseTenantContext()` in Mentoory.Web/Program.cs after `UseAuthorization()` — ref UF-03
- [X] T006 [P] Fix ContextController.GetUserId() force unwrap: replace null-forgiving operator with safe TryParse and redirect to login on failure in Mentoory.Web/Controllers/ContextController.cs (Line 109) — ref UF-05
- [X] T007 [P] Fix MenuConfiguration Participant menu: add "Mentor" and "ProjectCoordinator" to Participant group roles array in Mentoory.Web/Infrastructure/Menu/MenuConfiguration.cs (Line 32) — ref DC-02
- [X] T008 [P] Add ActiveIncubatorId context validation to Administration DashboardController: redirect to context selection if claim missing in Mentoory.Web/Areas/Administration/Controllers/DashboardController.cs — ref UF-08
- [X] T009 [P] Add ActiveIncubatorId context validation to Administration ProjectsController: redirect to context selection with message if claim missing or invalid, instead of silently returning empty results, in Mentoory.Web/Areas/Administration/Controllers/ProjectsController.cs — ref FR-005, FR-012
- [X] T010 [P] Add ActiveIncubatorId context validation to Administration UsersController: redirect to context selection with message if claim missing or invalid, instead of silently returning empty results, in Mentoory.Web/Areas/Administration/Controllers/UsersController.cs — ref FR-005, FR-012
- [X] T011 [P] Document SessionAuthenticationMiddleware as known limitation: add security risk comment explaining session tokens are not validated server-side in Mentoory.Web/Infrastructure/Authentication/SessionAuthenticationMiddleware.cs — ref UF-04

**Checkpoint**: All critical bugs fixed. Build must pass with zero warnings. Foundation ready for user story work.

---

## Phase 3: User Story 1 — Role-Based Dashboard Routing (Priority: P1)

**Goal**: Every role lands on the correct dashboard after login. GlobalAdmin always sees Platform dashboard with all menus.

**Independent Test**: Log in as each role and verify the correct dashboard appears.

### Implementation for User Story 1

- [X] T012 [US1] Implement role-based redirect logic in HomeController.Index(): read ActiveRole claim and redirect to role-appropriate area dashboard (GlobalAdmin→Platform/Incubators, IncubatorAdmin→Administration/Dashboard, ProjectCoordinator→Coordination/Diagnostics, Entrepreneur→Participant/Diagnostic, Mentor→Coordination area, Sponsor→Sponsor placeholder) in Mentoory.Web/Controllers/HomeController.cs
- [X] T013 [US1] Update ContextController.SetContext() to redirect through HomeController role-based routing instead of hardcoded Home/Index in Mentoory.Web/Controllers/ContextController.cs (Line 71)
- [X] T014 [US1] Create Sponsor placeholder dashboard view at Mentoory.Web/Areas/Platform/Views/Sponsor/Index.cshtml (or equivalent) with a "Proximamente" message and basic layout — the Sponsor role currently has no dashboard in the codebase

**Checkpoint**: Each role lands on its designated dashboard after login + context selection. Sponsor sees placeholder.

---

## Phase 4: User Story 2 — Context Selection Flow (Priority: P1)

**Goal**: Context selection follows Role→Incubator→Project order. Deep-links redirect to context selection with return URL preserved.

**Independent Test**: Access a context-scoped page without context; verify redirect to selection with return after completion.

### Implementation for User Story 2

- [X] T015 [US2] Add returnUrl query parameter to ContextController.Select GET action and pass it to the view in Mentoory.Web/Controllers/ContextController.cs
- [X] T016 [US2] Add local URL validation helper to prevent open redirects (reject absolute URLs, external domains) and thread returnUrl through Select POST and SetContext in Mentoory.Web/Controllers/ContextController.cs
- [X] T017 [US2] Redirect to validated returnUrl (or fallback to role-based dashboard) after successful context selection in ContextController.SetContext() in Mentoory.Web/Controllers/ContextController.cs

**Checkpoint**: Deep-links redirect to context selection and return to original page after context is set.

---

## Phase 5: User Story 3 — Context Switching and Safe Return Navigation (Priority: P1)

**Goal**: Users can see and switch their active context from the top bar. Switching is safe: no data leakage, appropriate fallback navigation.

**Independent Test**: Switch context from the top bar while on a context-scoped page; verify navigation is safe and no stale data appears.

**Dependencies**: Requires US2 (context selection flow must work for return URL support)

### Implementation for User Story 3

- [X] T018 [US3] Add current context display (active role, incubator name, project name) to the top navigation bar in Mentoory.Web/Views/Shared/_TopBar.cshtml — all labels in Spanish
- [X] T019 [US3] Create context-switch dropdown UI in Mentoory.Web/Views/Shared/_TopBar.cshtml showing user's available contexts with a "Cambiar contexto" link
- [X] T020 [US3] Create context-switcher JavaScript module in Mentoory.Web/wwwroot/js/context-switcher.js: call POST /api/context/switch, handle response, reload page or redirect to dashboard
- [X] T021 [US3] Implement safe return navigation in context-switcher.js: after switch, check if current URL is under an Area that matches the user's new ActiveRole (e.g., /Administration/ requires IncubatorAdmin+, /Coordination/ requires ProjectCoordinator+, /Participant/ requires Entrepreneur+, /Platform/ requires GlobalAdmin); if the user's new role has access to the current Area, reload the page; otherwise redirect to the role-based dashboard via HomeController
- [X] T022 [US3] Add unsaved-changes warning (JavaScript beforeunload-style confirmation) in Mentoory.Web/wwwroot/js/context-switcher.js before executing context switch when forms have dirty state

**Checkpoint**: Top bar shows current context. Users can switch context safely with appropriate navigation.

---

## Phase 6: User Story 4 — Authorization Boundary Enforcement (Priority: P1)

**Goal**: All diagnostic queries and operations enforce project-context boundaries. No cross-project data access is possible.

**Independent Test**: As a ProjectCoordinator in Project A, attempt to access a form/response from Project B by GUID — verify access is denied.

### Implementation for User Story 4

#### Repository Layer (Domain + Infrastructure)

- [X] T023 [P] [US4] Add project-scoped query overloads to IProjectFormRepository: `GetByExternalIdAsync(Guid externalId, long projectId)` and `GetByExternalIdWithQuestionsAsync(Guid externalId, long projectId)` in Mentoory.Diagnostic.Domain and implement in Mentoory.Diagnostic.Infrastructure/Persistence/Repositories/ProjectFormRepository.cs
- [X] T024 [P] [US4] Add project-scoped query overloads to IDiagnosticResponseRepository: `GetByExternalIdAsync(Guid externalId, long projectId)` and `GetByExternalIdWithResponsesAsync(Guid externalId, long projectId)` in Mentoory.Diagnostic.Domain and implement in Mentoory.Diagnostic.Infrastructure/Persistence/Repositories/DiagnosticResponseRepository.cs

#### Handler Layer (Application)

- [X] T025 [US4] Update GetProjectFormQuery to include ProjectId parameter and update GetProjectFormHandler to use project-scoped repository method; return failure if form not found for project in Mentoory.Diagnostic.Application/Queries/GetProjectForm/
- [X] T026 [US4] Update GetDiagnosticResponseQuery to include ProjectId parameter and update GetDiagnosticResponseHandler to use project-scoped repository method; return failure if response not found for project in Mentoory.Diagnostic.Application/Queries/GetDiagnosticResponse/
- [X] T027 [US4] Update SubmitDiagnosticResponseHandler to validate that the resolved ProjectForm belongs to the command's ProjectId before creating the response in Mentoory.Diagnostic.Application/Commands/SubmitDiagnosticResponse/SubmitDiagnosticResponseHandler.cs
- [X] T028 [US4] Update CorrectAnswerHandler to load response with project-scoped method using the corrector's project context; reject if response not found for project in Mentoory.Diagnostic.Application/Commands/CorrectAnswer/CorrectAnswerHandler.cs

#### Controller Layer (Web)

- [X] T029 [US4] Add ActiveProjectId context validation and pass projectId to GetProjectFormQuery in DiagnosticsController.Details() in Mentoory.Web/Areas/Coordination/Controllers/DiagnosticsController.cs (Lines 98-105)
- [X] T030 [US4] Add ActiveProjectId context validation and pass projectId to queries in AnswerCorrectionController.Index() and Correct() in Mentoory.Web/Areas/Coordination/Controllers/AnswerCorrectionController.cs
- [X] T031 [US4] Add ActiveProjectId context validation and pass projectId to GetProjectFormQuery in Participant DiagnosticController.Index() in Mentoory.Web/Areas/Participant/Controllers/DiagnosticController.cs

#### Unit Tests

- [X] T032 [P] [US4] Add unit tests verifying GetProjectFormHandler returns failure when form does not belong to requested project in tests/Mentoory.Diagnostic.Tests/Handlers/
- [X] T033 [P] [US4] Add unit tests verifying GetDiagnosticResponseHandler returns failure when response does not belong to requested project in tests/Mentoory.Diagnostic.Tests/Handlers/
- [X] T034 [P] [US4] Add unit tests verifying SubmitDiagnosticResponseHandler rejects submission when form belongs to a different project in tests/Mentoory.Diagnostic.Tests/Handlers/
- [X] T035 [P] [US4] Add unit tests verifying CorrectAnswerHandler rejects correction when response belongs to a different project in tests/Mentoory.Diagnostic.Tests/Handlers/

**Checkpoint**: All diagnostic data access is project-scoped. Cross-project GUID access returns failure. Unit tests prove isolation.

---

## Phase 7: User Story 5 — Platform vs Administration Separation (Priority: P2)

**Goal**: Navigation clearly distinguishes platform-level (cross-incubator) items from context-scoped administration items through visual cues.

**Independent Test**: Log in as GlobalAdmin with context selected; verify both sections appear with clear visual separation.

### Implementation for User Story 5

- [X] T036 [US5] Add visual section dividers or group headers (e.g., "Gestion de Plataforma" / "Administracion del Contexto") in Mentoory.Web/Views/Shared/_Navigation.cshtml to distinguish platform-level from context-scoped menu groups — all labels in Spanish
- [X] T037 [US5] Add CSS styles for navigation section separation (divider line, different background/opacity for platform vs context sections) in Mentoory.Web/wwwroot/css/ or inline in _Navigation.cshtml

**Checkpoint**: GlobalAdmin sees clearly separated Platform and Administration sections in navigation.

---

## Phase 8: User Story 6 — Diagnostic Module End-to-End Correctness (Priority: P2)

**Goal**: Diagnostic form template management, cloning, response submission, and answer correction all work end-to-end within correct context.

**Independent Test**: Create a form template, clone to a project, submit a response as Entrepreneur, correct an answer as Mentor — all with correct data associations.

**Dependencies**: Requires US4 (authorization boundary fixes must be in place)

### Implementation for User Story 6

> Note: US4 (T023-T035) already added project-context filtering to handlers, repositories, and controllers.
> These tasks verify that all controller callers correctly pass context after the US4 changes.

- [X] T038 [US6] Verify that DiagnosticsController.Data() and DiagnosticsController.Clone() correctly pass ActiveProjectId to their queries after the US4 handler signature changes — fix any callers that were missed by T029 in Mentoory.Web/Areas/Coordination/Controllers/DiagnosticsController.cs
- [X] T039 [US6] Verify that Participant DiagnosticController.Submit() passes correct projectId to SubmitDiagnosticResponseCommand and all context claims are correctly mapped after the US4 changes — fix any gaps missed by T031 in Mentoory.Web/Areas/Participant/Controllers/DiagnosticController.cs
- [X] T040 [US6] Verify that AnswerCorrectionController passes ActiveProjectId to CorrectAnswerCommand and GetDiagnosticResponseQuery after the US4 changes — fix any gaps missed by T030 in Mentoory.Web/Areas/Coordination/Controllers/AnswerCorrectionController.cs
- [X] T041 [US6] Run end-to-end manual verification of full diagnostic workflow: clone template → view form → submit response → correct answer — fix any remaining issues

**Checkpoint**: Full diagnostic lifecycle works correctly with context enforcement.

---

## Phase 9: User Story 8 — Deterministic Seed Data and Manual Validation Playbooks (Priority: P3)

**Goal**: Deterministic seed data for all test personas. Step-by-step manual playbooks for non-developer validation.

**Independent Test**: Run seed script against empty database; follow a manual playbook end-to-end.

**Note**: Placed before US7 because E2E tests depend on seed data.

### Implementation for User Story 8

- [X] T042 [US8] Create seed script Mentoory.Db.PostDeployment/004.SeedTestData.sql with test users (GlobalAdmin, IncubatorAdmin×2, ProjectCoordinator×2, Mentor, Entrepreneur×2, Sponsor, multi-role user), credentials, incubators (2), projects (3), and role assignments (12+) — all idempotent with MERGE or IF NOT EXISTS
- [X] T043 [US8] Add diagnostic seed data to Mentoory.Db.PostDeployment/004.SeedTestData.sql: form templates (2), project forms (3 across projects), questions (5+ per form, mixed types), answer option templates, and sample diagnostic responses
- [X] T044 [US8] Update Mentoory.Db.PostDeployment/Script.PostDeployment.sql to include :r .\004.SeedTestData.sql
- [X] T045 [US8] Create manual validation playbooks document at specs/002-phase1-4-hardening/playbooks.md covering: (1) Login routing by role, (2) Context selection flow, (3) Context switching, (4) Authorization boundaries, (5) Diagnostic workflow, (6) Cross-project isolation — each with preconditions, seed user to use, steps, expected results, pass/fail criteria

**Checkpoint**: Seed script creates all test data. Playbooks are followable by non-developers.

---

## Phase 10: User Story 7 — Automated Regression Safety Net (Priority: P2)

**Goal**: Playwright E2E tests cover all critical user flows and serve as regression safety net.

**Independent Test**: Run full Playwright suite against fresh deployment with seed data — all tests pass.

**Dependencies**: Requires US1-US6 (features must be correct), US8 (seed data must exist)

### Implementation for User Story 7

- [X] T046 [US7] Write Playwright login routing tests: verify each seed user role lands on correct dashboard; include a timing assertion that dashboard appears within 3 seconds (ref SC-001) in tests/Mentoory.Tests.E2E/Tests/LoginRoutingTests.cs
- [X] T047 [P] [US7] Write Playwright context selection tests: verify flow order (Role→Incubator→Project), deep-link redirect with return URL, auto-select for single-context users, and multi-context users seeing the selection screen (ref FR-003) in tests/Mentoory.Tests.E2E/Tests/ContextSelectionTests.cs
- [X] T048 [P] [US7] Write Playwright context switching tests: verify top-bar UI, switch execution, safe return navigation, no data leakage after switch in tests/Mentoory.Tests.E2E/Tests/ContextSwitchingTests.cs
- [X] T049 [P] [US7] Write Playwright authorization boundary tests: verify denied access for lower roles, granted access for higher roles, menu visibility per role in tests/Mentoory.Tests.E2E/Tests/AuthorizationTests.cs
- [X] T050 [P] [US7] Write Playwright diagnostic workflow tests: clone template, view form, submit response, correct answer in tests/Mentoory.Tests.E2E/Tests/DiagnosticWorkflowTests.cs
- [X] T051 [P] [US7] Write Playwright cross-project isolation tests: attempt to access form/response from another project by URL, verify denied in tests/Mentoory.Tests.E2E/Tests/ProjectIsolationTests.cs
- [X] T052 [P] [US7] Write Playwright menu visibility tests: verify each role sees only permitted menu items, GlobalAdmin sees all in tests/Mentoory.Tests.E2E/Tests/MenuVisibilityTests.cs
- [X] T053 [US7] Write Playwright dashboard rendering tests: verify each role's dashboard loads with correct content and seed data in tests/Mentoory.Tests.E2E/Tests/DashboardRenderingTests.cs

**Checkpoint**: All Playwright tests pass on clean deployment with seed data.

---

## Phase 11: Polish & Cross-Cutting Concerns

**Purpose**: Final quality gates and verification

- [X] T054 Run `dotnet build` and fix any compiler warnings (TreatWarningsAsErrors) across entire solution
- [X] T055 Audit all user-facing text in modified files for Spanish compliance (labels, error messages, toast notifications, empty-state messages) — ref Constitution Principle IX
- [X] T056 Run `dotnet test` for all unit test projects and fix any failures caused by handler/repository signature changes
- [X] T057 Audit all `[Authorize(Roles = "...")]` attributes across the entire Mentoory.Web project to verify every attribute includes the target role AND all higher-privilege roles per Constitution Principle X — document any remaining gaps and fix them
- [X] T058 Execute quickstart.md verification checklist with per-phase checkpoints: (1) build succeeds with zero warnings, (2) database publishes with seed data, (3) Phase 1 — login/logout works for all seed users, (4) Phase 3 — context selection + dashboard routing correct per role, (5) Phase 4 — diagnostic workflow end-to-end, (6) Playwright suite passes in single run

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: No dependencies on Phase 1 — can start immediately in parallel
- **US1-US4 (Phases 3-6)**: Depend on Foundational (Phase 2) completion
- **US5 (Phase 7)**: Depends on Foundational — independent of US1-US4
- **US6 (Phase 8)**: Depends on US4 (authorization boundary fixes)
- **US8 (Phase 9)**: Depends on Foundational — independent of US1-US6
- **US7 (Phase 10)**: Depends on US1-US6 + US8 (tests need features + seed data)
- **Polish (Phase 11)**: Depends on all prior phases

### User Story Dependencies

```
Phase 2 (Foundational) ──┬──> Phase 3 (US1) ──> Phase 4 (US2) ──> Phase 5 (US3)
                         │
                         ├──> Phase 6 (US4) ──> Phase 8 (US6)
                         │
                         ├──> Phase 7 (US5)   [independent]
                         │
                         └──> Phase 9 (US8)   [independent]
                         
All Phases 3-9 ──> Phase 10 (US7) ──> Phase 11 (Polish)
```

### Within Each User Story

- Repository changes before handler changes
- Handler changes before controller changes
- Controller changes before view/JavaScript changes
- Unit tests can run in parallel with each other [P]
- Story complete = all tasks in phase done + checkpoint verified

### Parallel Opportunities

- All Foundational tasks (T003-T011) can run in parallel (different files, except T003 which touches same file as removed T007)
- US4 repository tasks (T023-T024) can run in parallel
- US4 unit tests (T032-T035) can run in parallel
- US7 Playwright test files (T047-T052) can run in parallel
- US5 can run in parallel with US1-US4 (independent)
- US8 can run in parallel with US1-US6 (independent)

---

## Parallel Example: Phase 2 (Foundational)

```
# All fixes touch different files — launch all in parallel:
Task T003: Fix UserId claim + [Authorize] in Participant/DiagnosticController.cs
Task T004: Fix UserId claim in Coordination/AnswerCorrectionController.cs
Task T005: Register TenantContextMiddleware in Program.cs
Task T006: Fix GetUserId() in ContextController.cs
Task T007: Fix MenuConfiguration.cs
Task T008: Fix DashboardController.cs
Task T009: Fix ProjectsController.cs (context validation)
Task T010: Fix UsersController.cs (context validation)
Task T011: Document SessionAuthenticationMiddleware.cs
```

## Parallel Example: Phase 6 (US4 — Authorization)

```
# Repository layer — parallel (different repos):
Task T023: IProjectFormRepository + ProjectFormRepository
Task T024: IDiagnosticResponseRepository + DiagnosticResponseRepository

# Then handler layer — sequential after repos:
Task T025: GetProjectFormHandler (depends on T023)
Task T026: GetDiagnosticResponseHandler (depends on T024)
Task T027: SubmitDiagnosticResponseHandler (depends on T023)
Task T028: CorrectAnswerHandler (depends on T024)

# Then controller layer — sequential after handlers:
Task T029-T031: Controllers (depend on T025-T028)

# Unit tests — parallel with each other, after handlers:
Task T032-T035: All test files in parallel
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (E2E infrastructure)
2. Complete Phase 2: Foundational (critical bug fixes)
3. Complete Phase 3: US1 (dashboard routing)
4. **STOP and VALIDATE**: Each role lands on correct dashboard
5. This alone delivers visible user value

### Incremental Delivery

1. Foundational → critical bugs fixed
2. US1 → dashboard routing works → validate
3. US2 → return URL support → validate
4. US3 → context switching UI → validate (major UX improvement)
5. US4 → authorization boundaries sealed → validate (security milestone)
6. US5 → navigation separation → validate
7. US6 → diagnostic E2E verified → validate
8. US8 → seed data + playbooks → validate
9. US7 → E2E test suite → validate (quality gate)
10. Polish → production ready

### Security-First Alternative

If security is the top priority:
1. Phase 2 (Foundational) — fix claim bugs, register middleware, add admin context validation
2. Phase 6 (US4) — seal all authorization boundaries
3. Phase 8 (US6) — verify diagnostic correctness
4. Then proceed with UX improvements (US1-US3, US5)

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- All UI text must be in Spanish (Constitution Principle IX)
- All [Authorize] must include higher-privilege roles (Constitution Principle X)
- Zero compiler warnings required (Constitution Principle V)
