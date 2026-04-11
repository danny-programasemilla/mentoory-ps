# Tasks: Fix Batch Upload Project Scope Authorization Bug

**Input**: Design documents from `/specs/006-fix-batch-upload-scope/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/

**Tests**: Included — the feature specification explicitly requests unit tests, integration tests, authorization tests, and edge case coverage.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: Constitution amendment and governance alignment

- [X] T001 Amend Access & Security Constitution permission matrix — change Batch Upload Users row, ProjectCoordinator column from `NONE` to `SCOPED` with constraint "PC can upload for assigned projects only" in `.specify/memory/access-security-constitution.md`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core query contract changes that ALL user stories depend on. These modify the shared `ListRegistrationProjectsQuery` and handler.

**CRITICAL**: No user story work can begin until this phase is complete.

- [X] T002 Add `AuthorizedProjectIds` parameter to `ListRegistrationProjectsQuery` — change record to `ListRegistrationProjectsQuery(long IncubatorId, IReadOnlyList<long>? AuthorizedProjectIds)` in `Mentoory.Tenant.Application/Queries/ListRegistrationProjects/ListRegistrationProjectsQuery.cs`
- [X] T003 Inject `ITenantContext` into `ListRegistrationProjectsHandler` constructor and add incubator scope validation — if `ITenantContext.CurrentIncubatorId` is set and does not match `request.IncubatorId`, return `Failure(ResultErrorCodes.Unauthorized, ...)` with Spanish message in `Mentoory.Tenant.Application/Queries/ListRegistrationProjects/ListRegistrationProjectsHandler.cs`
- [X] T004 Add project-scope filtering to `ListRegistrationProjectsHandler` — when `request.AuthorizedProjectIds` is non-null, add `.Where(p => request.AuthorizedProjectIds.Contains(p.Id))` to the project query in `Mentoory.Tenant.Application/Queries/ListRegistrationProjects/ListRegistrationProjectsHandler.cs`
- [X] T005 Fix all existing callers of `ListRegistrationProjectsQuery` to pass `AuthorizedProjectIds: null` as second parameter — update `BatchUploadController.PopulateProjectsAsync()` and `BatchUploadController.Index(POST)` in `Mentoory.Web/Areas/Administration/Controllers/BatchUploadController.cs`

**Checkpoint**: Handler now supports role-aware filtering. Existing behavior unchanged (null = no filter). Build must pass with zero warnings.

---

## Phase 3: User Story 1 + User Story 4 — Project Coordinator Scoped Access + Server-Side Enforcement (Priority: P1) MVP

**Goal**: ProjectCoordinator sees only assigned projects in batch upload dropdown AND server rejects submissions targeting unauthorized projects.

**Independent Test**: Log in as ProjectCoordinator assigned to 2 of 5 incubator projects, open batch upload, verify dropdown shows exactly 2 projects. Submit a crafted request targeting an unassigned project and verify server rejects it.

### Tests for User Story 1 + 4

- [X] T006 [P] [US1] Create handler unit test: ProjectCoordinator with `AuthorizedProjectIds = [1, 3]` sees only projects 1 and 3 from 5 total registration-stage projects in `tests/Mentoory.Tenant.Tests/Handlers/ListRegistrationProjectsHandlerTests.cs`
- [X] T007 [P] [US1] Create handler unit test: `AuthorizedProjectIds = []` (empty list) returns empty project list in `tests/Mentoory.Tenant.Tests/Handlers/ListRegistrationProjectsHandlerTests.cs`
- [X] T008 [P] [US1] Create handler unit test: `AuthorizedProjectIds = null` returns all registration-stage projects (IA/GA path, non-regression) in `tests/Mentoory.Tenant.Tests/Handlers/ListRegistrationProjectsHandlerTests.cs`
- [X] T009 [P] [US1] Create handler unit test: mismatched `IncubatorId` vs `ITenantContext.CurrentIncubatorId` returns Unauthorized failure in `tests/Mentoory.Tenant.Tests/Handlers/ListRegistrationProjectsHandlerTests.cs`
- [X] T010 [P] [US4] Create handler unit test: `AuthorizedProjectIds` filters out projects not in the list even if they match stage filters in `tests/Mentoory.Tenant.Tests/Handlers/ListRegistrationProjectsHandlerTests.cs`

### Implementation for User Story 1 + 4

- [X] T011 [US1] Update `[Authorize]` attribute on `BatchUploadController` from `"IncubatorAdmin,GlobalAdmin"` to `"ProjectCoordinator,IncubatorAdmin,GlobalAdmin"` in `Mentoory.Web/Areas/Administration/Controllers/BatchUploadController.cs`
- [X] T012 [US1] Add `IRoleAssignmentRepository` injection to `BatchUploadController` constructor in `Mentoory.Web/Areas/Administration/Controllers/BatchUploadController.cs`
- [X] T013 [US1] Add helper methods to `BatchUploadController`: `GetActiveRole()` to read `ActiveRole` claim, `GetUserId()` to read user ID from claims in `Mentoory.Web/Areas/Administration/Controllers/BatchUploadController.cs`
- [X] T014 [US1] Refactor `PopulateProjectsAsync` to be role-aware — if ActiveRole is `ProjectCoordinator`, query `IRoleAssignmentRepository.GetActiveByUserIdAsync(userId)`, filter to assignments matching current incubator + role, extract non-null `ProjectId` values, pass as `AuthorizedProjectIds` to `ListRegistrationProjectsQuery`. If empty, redirect with message "No tiene proyectos asignados para carga masiva." in `Mentoory.Web/Areas/Administration/Controllers/BatchUploadController.cs`
- [X] T015 [US1] Update `Index` GET action to handle empty-scope redirect — if `PopulateProjectsAsync` returns no projects for PC, redirect to Administration dashboard with Spanish warning message via `TempData["WarningMessage"]` in `Mentoory.Web/Areas/Administration/Controllers/BatchUploadController.cs`
- [X] T016 [US4] Add server-side submission validation in `Index` POST action — before dispatching `BatchRegisterUsersCommand`, verify `model.ProjectExternalId` is in the user's authorized project set (for PC role). If not authorized, return `Forbid()` or redirect with error in `Mentoory.Web/Areas/Administration/Controllers/BatchUploadController.cs`
- [X] T017 [US4] Update `Index` POST action to pass `AuthorizedProjectIds` when re-populating projects on validation failure — ensure the filtered project list is used consistently throughout the POST flow in `Mentoory.Web/Areas/Administration/Controllers/BatchUploadController.cs`

**Checkpoint**: ProjectCoordinator sees only assigned projects. Server rejects unauthorized submissions. Build passes with zero warnings.

---

## Phase 4: User Story 2 — IncubatorAdmin Non-Regression (Priority: P1)

**Goal**: Verify IncubatorAdmin batch upload behavior is unchanged — sees all registration-stage projects in their incubator.

**Independent Test**: Log in as IncubatorAdmin, open batch upload, verify all active registration-stage projects appear.

### Tests for User Story 2

- [X] T018 [P] [US2] Create handler unit test: IncubatorAdmin path (`AuthorizedProjectIds = null`) returns all 5 registration-stage projects from incubator in `tests/Mentoory.Tenant.Tests/Handlers/ListRegistrationProjectsHandlerTests.cs`
- [X] T019 [P] [US2] Create handler unit test: IncubatorAdmin for Incubator X cannot query projects from Incubator Y (ITenantContext mismatch) in `tests/Mentoory.Tenant.Tests/Handlers/ListRegistrationProjectsHandlerTests.cs`

### Implementation for User Story 2

- [X] T020 [US2] Verify `PopulateProjectsAsync` passes `AuthorizedProjectIds: null` when ActiveRole is `IncubatorAdmin` — no code change expected if T014 is correct, but verify the role branching logic in `Mentoory.Web/Areas/Administration/Controllers/BatchUploadController.cs`

**Checkpoint**: IncubatorAdmin behavior unchanged. All IA acceptance scenarios pass.

---

## Phase 5: User Story 3 — GlobalAdmin Non-Regression (Priority: P2)

**Goal**: Verify GlobalAdmin batch upload behavior is unchanged — sees all registration-stage projects in the selected incubator context.

**Independent Test**: Log in as GlobalAdmin, select incubator context, open batch upload, verify all projects appear.

### Tests for User Story 3

- [X] T021 [P] [US3] Create handler unit test: GlobalAdmin path (`AuthorizedProjectIds = null`, `ITenantContext.CurrentIncubatorId = null`) returns all registration-stage projects in `tests/Mentoory.Tenant.Tests/Handlers/ListRegistrationProjectsHandlerTests.cs`

### Implementation for User Story 3

- [X] T022 [US3] Verify `PopulateProjectsAsync` passes `AuthorizedProjectIds: null` when ActiveRole is `GlobalAdmin` and that `ITenantContext` validation skips when `CurrentIncubatorId` is null — no code change expected if T014 and T003 are correct, but verify in `Mentoory.Web/Areas/Administration/Controllers/BatchUploadController.cs`

**Checkpoint**: GlobalAdmin behavior unchanged. All GA acceptance scenarios pass.

---

## Phase 6: User Story 5 — Platform-Wide Authorization Hardening (Priority: P2)

**Goal**: Harden all confirmed-issue and risky endpoints identified in the platform audit to enforce proper scope validation.

**Independent Test**: For each hardened endpoint, verify that scoped users only see data within their authorized scope by sending cross-scope requests.

### Tests for User Story 5

- [X] T023 [P] [US5] Create handler unit test: `GetProjectByExternalIdHandler` returns failure when project's IncubatorId does not match `ITenantContext.CurrentIncubatorId` in `tests/Mentoory.Tenant.Tests/Handlers/GetProjectByExternalIdHandlerTests.cs`
- [X] T024 [P] [US5] Create handler unit test: `GetProjectByExternalIdHandler` succeeds when IncubatorId matches or `ITenantContext.CurrentIncubatorId` is null in `tests/Mentoory.Tenant.Tests/Handlers/GetProjectByExternalIdHandlerTests.cs`
- [X] T025 [P] [US5] Create handler unit test: `GetIncubatorByExternalIdHandler` returns failure when incubator Id does not match `ITenantContext.CurrentIncubatorId` in `tests/Mentoory.Tenant.Tests/Handlers/GetIncubatorByExternalIdHandlerTests.cs`

### Implementation for User Story 5

- [X] T026 [P] [US5] Inject `ITenantContext` into `GetProjectByExternalIdHandler` and add validation — after resolving project, verify `project.IncubatorId == ITenantContext.CurrentIncubatorId` (when non-null). Return failure with Spanish message if mismatch in `Mentoory.Tenant.Application/Queries/GetProjectByExternalId/GetProjectByExternalIdHandler.cs`
- [X] T027 [P] [US5] Inject `ITenantContext` into `GetIncubatorByExternalIdHandler` and add validation — after resolving incubator, verify `incubator.Id == ITenantContext.CurrentIncubatorId` (when non-null). Return failure with Spanish message if mismatch in `Mentoory.Tenant.Application/Queries/GetIncubatorByExternalId/GetIncubatorByExternalIdHandler.cs`
- [X] T028 [US5] Scope template selection in `DiagnosticsController.Clone()` — update `PopulateTemplatesViewBag()` to filter template list by current incubator scope when user is not GlobalAdmin in `Mentoory.Web/Areas/Coordination/Controllers/DiagnosticsController.cs`
- [X] T029 [US5] Document `TemplatesController.DiagnosticsData()` as intentionally global — add XML comment explaining GlobalAdmin-only access makes unscoped template listing acceptable in `Mentoory.Web/Areas/Platform/Controllers/TemplatesController.cs`

**Checkpoint**: All confirmed issues and risky endpoints hardened. Platform-wide audit classification table updated.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Build verification, code review, and final validation

- [X] T030 Run `dotnet build` and verify zero warnings across entire solution
- [X] T031 Run `dotnet test` and verify all existing and new tests pass
- [X] T032 Run `/simplify` to review code for quality, reuse, and efficiency per CLAUDE.md code review standards
- [X] T033 Verify all `[Authorize]` attributes on modified controllers include higher-privilege roles per Constitution Principle X

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: No dependency on Phase 1 (code vs governance changes) — but logically should follow
- **User Stories (Phase 3-6)**: ALL depend on Foundational phase (Phase 2) completion
  - US1+US4 (Phase 3) can start immediately after Phase 2
  - US2 (Phase 4) depends on Phase 3 completion (validates non-regression of the changes)
  - US3 (Phase 5) depends on Phase 3 completion (validates non-regression of the changes)
  - US5 (Phase 6) can start in parallel with Phase 3 (independent endpoints)
- **Polish (Phase 7)**: Depends on all user stories being complete

### User Story Dependencies

- **US1+US4 (P1)**: Can start after Foundational (Phase 2) — no dependencies on other stories
- **US2 (P1)**: Depends on US1+US4 — validates that IA behavior is preserved after the fix
- **US3 (P2)**: Depends on US1+US4 — validates that GA behavior is preserved after the fix
- **US5 (P2)**: Independent of US1-US4 — hardens other endpoints, can run in parallel with Phase 3

### Within Each User Story

- Tests MUST be written and FAIL before implementation
- Handler changes before controller changes
- Core implementation before edge case handling
- Story complete before moving to next priority

### Parallel Opportunities

- T006-T010 (all US1+US4 tests) can run in parallel
- T018-T019 (US2 tests) can run in parallel
- T023-T025 (US5 tests) can run in parallel
- T026-T027 (US5 handler hardening) can run in parallel
- Phase 3 (US1+US4) and Phase 6 (US5) can run in parallel after Phase 2

---

## Parallel Example: User Story 1 + 4

```text
# Launch all tests together (T006-T010):
Task: "Handler unit test: PC with AuthorizedProjectIds = [1, 3]"
Task: "Handler unit test: AuthorizedProjectIds = [] returns empty"
Task: "Handler unit test: AuthorizedProjectIds = null returns all (non-regression)"
Task: "Handler unit test: mismatched IncubatorId returns Unauthorized"
Task: "Handler unit test: AuthorizedProjectIds filters correctly"

# After tests written and failing, sequential implementation:
T011 → T012 → T013 → T014 → T015 → T016 → T017
```

## Parallel Example: User Story 5

```text
# Launch all tests together (T023-T025):
Task: "GetProjectByExternalId scope mismatch test"
Task: "GetProjectByExternalId scope match test"
Task: "GetIncubatorByExternalId scope mismatch test"

# Launch handler hardening in parallel (T026-T027):
Task: "Harden GetProjectByExternalIdHandler"
Task: "Harden GetIncubatorByExternalIdHandler"
```

---

## Implementation Strategy

### MVP First (User Story 1 + 4 Only)

1. Complete Phase 1: Constitution amendment
2. Complete Phase 2: Query contract changes (foundational)
3. Complete Phase 3: US1+US4 — PC scoped access + server enforcement
4. **STOP and VALIDATE**: Test PC sees only assigned projects; server rejects unauthorized submissions
5. Deploy/demo if ready

### Incremental Delivery

1. Complete Setup + Foundational → Query contract ready
2. Add US1+US4 → PC scoped access working → Deploy/Demo (MVP!)
3. Add US2 → IA non-regression validated → Deploy/Demo
4. Add US3 → GA non-regression validated → Deploy/Demo
5. Add US5 → Platform hardened → Deploy/Demo
6. Polish → Build clean, tests green → Final release

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: US1+US4 (Phase 3) — core bug fix
   - Developer B: US5 (Phase 6) — platform hardening
3. After Phase 3 completes:
   - Developer A: US2 + US3 (Phases 4-5) — non-regression validation
4. Polish phase together

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- US1 and US4 are combined because server-side enforcement (US4) is inseparable from the dropdown fix (US1)
- US2 and US3 are primarily validation phases — minimal code changes expected
- All Spanish UI messages must follow existing patterns (TempData["WarningMessage"], ModelState errors)
- Commit after each phase checkpoint
- Stop at any checkpoint to validate story independently
