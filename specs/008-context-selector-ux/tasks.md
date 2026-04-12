# Tasks: Cascading Context Selector UX

**Input**: Design documents from `/specs/008-context-selector-ux/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Not explicitly requested in the spec. Test tasks included in Phase 7 (Polish) as integration/E2E validation.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup

**Purpose**: Ensure build works and Toast component is wired into the layout

- [X] T001 Verify project builds cleanly with `dotnet build` — zero warnings baseline
- [X] T002 Add Toast ViewComponent invocation to `Mentoory.Web/Views/Shared/_Layout.cshtml` (add `@await Component.InvokeAsync("Toast")` before closing body tag)
- [X] T003 Add `<script src="~/js/context-selector.js"></script>` to global scripts section in `Mentoory.Web/Views/Shared/_Layout.cshtml`

---

## Phase 2: Foundational (Cascade API Endpoints)

**Purpose**: Backend API endpoints that ALL user stories depend on. MUST be complete before any frontend work.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T004 [P] Create `ContextRoleDto` record in `Mentoory.Access.Application/Queries/ListContextRoles/ContextRoleDto.cs` — `sealed record ContextRoleDto(string Role, string DisplayName)`
- [X] T005 [P] Create `ContextIncubatorDto` record in `Mentoory.Access.Application/Queries/ListContextIncubators/ContextIncubatorDto.cs` — `sealed record ContextIncubatorDto(long Id, string Name, Guid RoleAssignmentExternalId)`
- [X] T006 [P] Create `ContextProjectDto` record in `Mentoory.Access.Application/Queries/ListContextProjects/ContextProjectDto.cs` — `sealed record ContextProjectDto(long Id, string Name, Guid RoleAssignmentExternalId)`
- [X] T007 Create `ListContextRolesQuery` and `ListContextRolesHandler` in `Mentoory.Access.Application/Queries/ListContextRoles/` — query `IRoleAssignmentRepository.GetActiveByUserIdAsync(userId)`, extract distinct roles, map to ContextRoleDto with Spanish display names, order by role hierarchy
- [X] T008 Create `ListContextIncubatorsQuery` and `ListContextIncubatorsHandler` — orchestrated in ContextController (Access.Application can't reference Tenant.Application for cross-domain name resolution)
- [X] T009 Create `ListContextProjectsQuery` and `ListContextProjectsHandler` — orchestrated in ContextController (same cross-domain constraint)
- [X] T010 Add `GET /api/context/roles` endpoint to `Mentoory.Web/Controllers/ContextController.cs` — `[HttpGet] [Route("api/context/roles")]`, call `ListContextRolesQuery`, return JSON array
- [X] T011 Add `GET /api/context/incubators` endpoint to `Mentoory.Web/Controllers/ContextController.cs` — `[HttpGet] [Route("api/context/incubators")]`, accept `role` query param, orchestrate cross-domain data, return JSON array
- [X] T012 Add `GET /api/context/projects` endpoint to `Mentoory.Web/Controllers/ContextController.cs` — `[HttpGet] [Route("api/context/projects")]`, accept `role` and `incubatorId` query params, orchestrate cross-domain data, return JSON array
- [X] T013 Extend `ContextSwitchRequest` record in `Mentoory.Web/Controllers/ContextController.cs` to include optional `long? IncubatorId`, `string? IncubatorName`, `long? ProjectId`, `string? ProjectName` fields
- [X] T014 Update `Switch` action in `Mentoory.Web/Controllers/ContextController.cs` to apply override fields when resolved role is GlobalAdmin (reuse `SetGlobalAdminContext` pattern)
- [X] T015 Verify build passes with zero warnings after all backend changes

**Checkpoint**: All 3 cascade GET endpoints and the extended switch POST are functional. Test with curl or browser dev tools.

---

## Phase 3: User Story 1 — Multi-Role Cascade Dropdowns (Priority: P1) 🎯 MVP

**Goal**: Replace the card grid with 3 cascading dropdowns on the context selection page. Selecting a role filters incubators; selecting an incubator filters projects.

**Independent Test**: Log in with a multi-role user, navigate to `/Context/Select`, verify 3 dropdowns appear, select each in sequence, click Confirmar — context is set and user is redirected.

### Implementation for User Story 1

- [X] T016 [US1] Create `Mentoory.Web/wwwroot/js/context-selector.js` with `initContextSelector(container)` function — wire role change → fetch incubators, incubator change → fetch projects, project change → update hidden fields. Use `getAntiForgeryToken()` for fetch headers. Include loading spinner per dropdown and inline error handling ("Error al cargar opciones. Intente nuevamente." with retry)
- [X] T017 [US1] Create `Mentoory.Web/Views/Shared/_ContextSelector.cshtml` partial — 3 `<select>` elements (Rol, Incubadora, Proyecto) with placeholder options ("Seleccione un rol...", "Seleccione una incubadora...", "Seleccione un proyecto..."), hidden inputs for `roleAssignmentExternalId`/`selectedIncubatorId`/`selectedIncubatorName`/`selectedProjectId`/`selectedProjectName`/`returnUrl`, Confirmar button disabled by default, `data-mode` attribute for page vs modal behavior, error containers per dropdown
- [X] T018 [US1] Rewrite `Mentoory.Web/Views/Context/Select.cshtml` — replace card grid with `<form asp-action="Select" method="post">` wrapping `_ContextSelector` partial with `data-mode="page"`, preserve `@Html.AntiForgeryToken()`, keep heading "Seleccionar Contexto de Trabajo" and subtitle
- [X] T019 [US1] Update GET `Select` action in `Mentoory.Web/Controllers/ContextController.cs` — preserve auto-skip logic, remove `BuildGlobalAdminContextsAsync` card generation, pass returnUrl via ViewBag (dropdowns now populate via AJAX)
- [X] T020 [US1] Wire page initialization in `context-selector.js` — on DOMContentLoaded, find any `[data-mode="page"]` container, call `initContextSelector()`, trigger initial roles fetch to populate role dropdown
- [X] T021 [US1] Verify `Select.cshtml` form POST submits correct hidden field values (`roleAssignmentExternalId`, `selectedIncubatorId`, etc.) to existing POST `Select` action — test full redirect flow including returnUrl

**Checkpoint**: Multi-role users see cascading dropdowns. Selecting role→incubator→project and clicking Confirmar sets context correctly. Auto-skip works for single-context users.

---

## Phase 4: User Story 2 — Auto-Select Single Options (Priority: P1)

**Goal**: When a dropdown has exactly one option, auto-select it and show as read-only. Full auto-skip when entire context resolves to one combination.

**Independent Test**: Log in with a user that has one role but multiple incubators — role dropdown shows pre-selected and disabled.

### Implementation for User Story 2

- [X] T022 [US2] Add auto-select logic to `initContextSelector` in `Mentoory.Web/wwwroot/js/context-selector.js` — after populating a dropdown via AJAX, if `options.length === 1`: set `selectedIndex = 0`, add `disabled` attribute, dispatch `change` event to trigger next cascade automatically
- [X] T023 [US2] Handle initial auto-cascade in `context-selector.js` — when roles endpoint returns 1 role: auto-select it, mark disabled, immediately fetch incubators; if incubators returns 1: auto-select, fetch projects; if projects returns 0 or 1: auto-select if 1, enable Confirmar button
- [X] T024 [US2] Verify controller auto-skip in `Mentoory.Web/Controllers/ContextController.cs` still works — single-context users (1 role, 1 incubator, 0-or-1 projects) bypass the page entirely

**Checkpoint**: Single-option dropdowns auto-select as disabled/read-only. Full auto-skip preserved for single-context users.

---

## Phase 5: User Story 3 — GlobalAdmin Browses All Incubators/Projects (Priority: P2)

**Goal**: GlobalAdmin role populates incubator dropdown with ALL active incubators in the system, and project dropdown with ALL projects under the selected incubator.

**Independent Test**: Log in as GlobalAdmin, select GlobalAdmin role, verify all incubators appear, select one, verify all projects under it appear, proceed without selecting a project.

### Implementation for User Story 3

- [X] T025 [US3] Verify `ListContextIncubatorsHandler` GlobalAdmin path works — controller GetIncubators endpoint returns all active incubators from `ListIncubatorContextOptionsQuery`, each with the GlobalAdmin's base `RoleAssignmentExternalId`
- [X] T026 [US3] Verify `ListContextProjectsHandler` GlobalAdmin path works — controller GetProjects endpoint returns all active projects under the selected incubator, each with GlobalAdmin's base ExternalId
- [X] T027 [US3] Verify `_ContextSelector.cshtml` project dropdown shows "Sin proyectos disponibles" (disabled placeholder) when no projects exist, and Confirmar button is still enabled when role+incubator are selected without a project
- [X] T028 [US3] Test GlobalAdmin full-page flow end-to-end — select GlobalAdmin role → all incubators appear → select incubator → all projects appear → click Confirmar without selecting project → context set at incubator level

**Checkpoint**: GlobalAdmin can browse into any incubator/project. Optional project selection works.

---

## Phase 6: User Story 4 — Top-Bar Modal Context Switching (Priority: P2)

**Goal**: Add a Bootstrap 5 modal to the top-bar with the same cascading dropdowns for in-app context switching without page navigation.

**Independent Test**: From any authenticated page, click "Cambiar contexto", complete the cascade in the modal, confirm — context switches via AJAX, toast shows, page reloads.

### Implementation for User Story 4

- [X] T029 [US4] Add modal markup to `Mentoory.Web/Views/Shared/_TopBar.cshtml` — replace "Cambiar contexto" form/link with `<button data-bs-toggle="modal" data-bs-target="#contextSwitcherModal">Cambiar contexto</button>`, add `<div class="modal fade" id="contextSwitcherModal">` containing `_ContextSelector` partial with `data-mode="modal"`, modal header "Cambiar Contexto de Trabajo"
- [X] T030 [US4] Add modal cascade initialization to `Mentoory.Web/wwwroot/js/context-switcher.js` — listen for `shown.bs.modal` event on `#contextSwitcherModal`, call `initContextSelector(modalContainer)` to wire up cascade, fetch roles on open
- [X] T031 [US4] Add modal confirm handler to `Mentoory.Web/wwwroot/js/context-switcher.js` — on Confirmar click in modal: read hidden fields, check `hasUnsavedChanges()` with warning prompt, call `POST /api/context/switch` with roleAssignmentExternalId + optional override fields, on success: show `showToast("Contexto actualizado exitosamente.", "success")`, close modal, check `canAccessCurrentArea(newRole)` → reload or redirect to home
- [X] T032 [US4] Ensure `Mentoory.Web/wwwroot/js/context-switcher.js` is loaded globally in `Mentoory.Web/Views/Shared/_Layout.cshtml` (add script tag after context-selector.js if not already done)
- [X] T033 [US4] Handle modal error states — on AJAX failure: show `showToast("No se pudo cambiar el contexto.", "danger")`, keep modal open; on 401: redirect to login page

**Checkpoint**: Modal opens from top-bar, cascade works within modal, AJAX switch with toast notification and page reload.

---

## Phase 7: User Story 5 — Shared Partial Verification + Polish (Priority: P3)

**Goal**: Verify the shared partial pattern works correctly, then polish and test.

### Implementation for User Story 5

- [X] T034 [US5] Verify `_ContextSelector.cshtml` partial is used identically by both `Select.cshtml` (page mode) and `_TopBar.cshtml` (modal mode) — confirm `data-mode` attribute drives behavior correctly, no duplication of dropdown markup
- [X] T035 [P] Add integration tests to `tests/Mentoory.Tests.Integration/Authorization/ContextSelectionTests.cs` — test `ListContextRolesQuery` returns distinct roles with display names and correct hierarchy ordering
- [X] T036 [P] Extend E2E tests in `tests/Mentoory.Tests.E2E/Tests/ContextSelectionTests.cs` — cascade UI tests, tenant isolation (non-GlobalAdmin sees only assigned data), API security (unauthenticated 401, invalid role 400, unassigned role empty), GlobalAdmin sees all incubators. Updated all 12 existing E2E test files to use new dropdown-based context selection.
- [X] T037 Verify zero warnings with `dotnet build` — ensure no new compiler/StyleCop warnings introduced
- [ ] T038 Manual testing per quickstart.md — test mobile viewport, keyboard navigation, returnUrl preservation, unsaved-changes warning, area redirect on incompatible role switch

**Checkpoint**: All user stories work independently and together. Build passes with zero warnings. Integration and E2E tests pass.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 — BLOCKS all user stories
- **US1 (Phase 3)**: Depends on Phase 2 — core cascade UI
- **US2 (Phase 4)**: Depends on Phase 3 (US1) — extends cascade with auto-select logic
- **US3 (Phase 5)**: Depends on Phase 2 — can run in parallel with US1/US2 (backend verification only), but Phase 3 needed for full-page testing
- **US4 (Phase 6)**: Depends on Phase 3 (US1) — reuses the shared partial and JS from US1
- **US5 (Phase 7)**: Depends on Phases 3, 4, 5, 6 — verification and polish

### User Story Dependencies

- **US1 (P1)**: Foundation → core implementation. All other stories depend on this.
- **US2 (P1)**: US1 → adds auto-select behavior to the cascade JS
- **US3 (P2)**: Foundation → verifies GlobalAdmin backend paths, US1 → tests in full UI
- **US4 (P2)**: US1 → reuses partial and JS, adds modal wrapper
- **US5 (P3)**: All stories → verification and tests

### Within Each User Story

- DTOs before query handlers (Phase 2)
- Query handlers before controller endpoints (Phase 2)
- Backend endpoints before frontend JS/views (Phase 2 → Phase 3+)
- Shared partial before page-specific views
- Core implementation before integration/polish

### Parallel Opportunities

- T004, T005, T006 (all DTOs) can run in parallel
- T010, T011, T012 (controller endpoints) can run in parallel after their handlers exist
- T035, T036 (tests) can run in parallel
- US3 backend verification (T025-T026) can start as soon as Phase 2 is complete
- US4 can start as soon as US1 is complete (independent from US2 and US3)

---

## Parallel Example: Phase 2 (Foundational)

```text
# Launch all DTOs in parallel:
T004: Create ContextRoleDto in Mentoory.Access.Application/Queries/ListContextRoles/ContextRoleDto.cs
T005: Create ContextIncubatorDto in Mentoory.Access.Application/Queries/ListContextIncubators/ContextIncubatorDto.cs
T006: Create ContextProjectDto in Mentoory.Access.Application/Queries/ListContextProjects/ContextProjectDto.cs

# Then handlers sequentially (each depends on its DTO):
T007: ListContextRolesHandler (depends on T004)
T008: ListContextIncubatorsHandler (depends on T005)
T009: ListContextProjectsHandler (depends on T006)

# Then controller endpoints in parallel:
T010: GET /api/context/roles (depends on T007)
T011: GET /api/context/incubators (depends on T008)
T012: GET /api/context/projects (depends on T009)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational API endpoints
3. Complete Phase 3: User Story 1 (cascade dropdowns on full page)
4. **STOP and VALIDATE**: Multi-role user can select context via cascading dropdowns
5. Deploy/demo if ready — card grid is replaced with dropdowns

### Incremental Delivery

1. Phase 1 + Phase 2 → Backend ready
2. Add US1 (Phase 3) → Cascade dropdowns on full page → **MVP!**
3. Add US2 (Phase 4) → Auto-select single options → Better UX
4. Add US3 (Phase 5) → GlobalAdmin full browsing → Admin coverage
5. Add US4 (Phase 6) → Top-bar modal → In-app switching
6. Add US5 (Phase 7) → Tests + polish → Production-ready

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- All UI text in Spanish (constitution Principle IX)
- Zero warnings required (constitution Principle V)
