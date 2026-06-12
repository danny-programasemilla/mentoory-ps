# Tasks: List/Edit UX Consistency

**Feature**: `025-list-edit-ux-consistency` | **Plan**: [plan.md](./plan.md) | **Spec**: [spec.md](./spec.md)

**Input**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

Tasks are organized by user story (priority order). Each story is independently
implementable and testable. `[P]` = parallelizable (different file, no incomplete deps).

---

## Phase 1: Setup

- [x] T001 Confirm clean baseline: run `dotnet build` (zero warnings — `TreatWarningsAsErrors`) and `dotnet test` green before any change.

## Phase 2: Foundational

No cross-story blocking prerequisites. The three user stories are independent; the only
shared artifact is the JS option constants, which belong to US1 and are the first task there.

---

## Phase 3: User Story 1 — Dropdown status filters (Priority: P1)

**Goal**: The status column on all 5 affected list tables filters via a Spanish dropdown
(Todos + valid values) instead of a non-functional free-text input. No server change.

**Independent test**: On each table, open **Filtros**; the status field is a `<select>` with
the correct options; selecting a value filters the table; **Todos**/**Limpiar** resets it.
Administration/Users + Platform/Users remain unchanged.

- [x] T002 [US1] Add three shared filter-option constants to `Mentoory.Web/wwwroot/js/datatable-helper.js` (module-level, near `FILTER_TYPE_REGISTRY`): `FILTER_OPTIONS_ACTIVE_FEM` (`''→Todos`, `'true'→Activa`, `'false'→Inactiva`), `FILTER_OPTIONS_ACTIVE_MASC` (`''→Todos`, `'true'→Activo`, `'false'→Inactivo`), `FILTER_OPTIONS_SYNC_MODE` (`''→Todos`, `'0'→Desconectado`, `'1'→Sincronización parcial`).
- [x] T003 [P] [US1] Add `filters: [{ column: 'isActive', type: 'select', options: FILTER_OPTIONS_ACTIVE_FEM }]` to the `initDataTable` config in `Mentoory.Web/Areas/Platform/Views/Incubators/Index.cshtml`.
- [x] T004 [P] [US1] Add `filters: [{ column: 'isActive', type: 'select', options: FILTER_OPTIONS_ACTIVE_FEM }]` to the `initDataTable` config in `Mentoory.Web/Areas/Platform/Views/Templates/Diagnostics.cshtml`.
- [x] T005 [P] [US1] Add `filters: [{ column: 'isActive', type: 'select', options: FILTER_OPTIONS_ACTIVE_MASC }]` to the `initDataTable` config in `Mentoory.Web/Areas/Administration/Views/Projects/Index.cshtml`.
- [x] T006 [P] [US1] Add `filters: [{ column: 'isActive', type: 'select', options: FILTER_OPTIONS_ACTIVE_MASC }]` to the `initDataTable` config in `Mentoory.Web/Areas/Coordination/Views/Projects/Index.cshtml`.
- [x] T007 [P] [US1] Add `filters: [{ column: 'syncMode', type: 'select', options: FILTER_OPTIONS_SYNC_MODE }]` to the `initDataTable` config in `Mentoory.Web/Areas/Coordination/Views/Diagnostics/Index.cshtml`.
- [x] T008 [US1] Validate per quickstart §US1: dropdowns render on all 5 tables, filtering works for Incubators + Coordination Diagnostics, and confirm `'0'` (Desconectado) is treated as an active filter value (string `'0'` is truthy — verify `getActiveFilters`/`syncFiltersToUrl` do not drop it). Confirm no regression on Administration/Users and Platform/Users.

**Checkpoint**: US1 deliverable complete and demoable on its own.

---

## Phase 4: User Story 2 — Cancel → list (Priority: P2)

**Goal**: Cancel on the Incubators Edit page returns to the list, matching every other
Create/Edit page.

**Independent test**: From the Incubators list, click the edit icon, click **Cancelar** →
land on `/Platform/Incubators`.

- [x] T009 [US2] In `Mentoory.Web/Areas/Platform/Views/Incubators/Edit.cshtml`, change the Cancel link from `asp-action="Details" asp-route-externalId="@Model.ExternalId"` to `asp-action="Index"` (keep the `btn btn-ghost-secondary` styling and the text "Cancelar").
- [x] T010 [US2] Verify-only (no change): confirm all other Create/Edit pages already target their list action for Cancel (`Administration/Projects/Create`, `Administration/Users/Enroll` + `RegisterInternal`, `Coordination/Diagnostics/Clone`, `Coordination/Knowledge/CreateTemplate`, `Platform/Incubators/Create`). Document confirmation in the PR.

**Checkpoint**: US2 deliverable complete and demoable on its own.

---

## Phase 5: User Story 3 — Estado editing for incubators (Priority: P3)

**Goal**: The Incubators Edit form lets the user set estado (Activa/Inactiva), persisted with
name/description on save. No schema change.

**Independent test**: Edit an active incubator, flip estado to Inactiva, save → Details and
list show Inactiva and the list filter finds it under Inactiva; Cancel discards an unsaved
toggle.

- [x] T011 [P] [US3] Add `bool IsActive` with `[Display(Name = "Estado")]` to `EditIncubatorViewModel` in `Mentoory.Web/Areas/Platform/Models/IncubatorViewModels.cs`.
- [x] T012 [P] [US3] Add `bool IsActive` as the final parameter of `UpdateIncubatorCommand` in `Mentoory.Tenant.Application/Commands/UpdateIncubator/UpdateIncubatorCommand.cs`.
- [x] T013 [US3] In `Mentoory.Tenant.Application/Commands/UpdateIncubator/UpdateIncubatorHandler.cs`, after `incubator.Update(...)`, apply estado: `if (request.IsActive) incubator.Activate(timeProvider.UtcNow); else incubator.Deactivate(timeProvider.UtcNow);` (depends on T012).
- [x] T014 [US3] In `Mentoory.Web/Areas/Platform/Controllers/IncubatorsController.cs`: in `Edit` (GET) set `IsActive = incubator.IsActive` on the view model; in `Edit` (POST) pass `model.IsActive` to `UpdateIncubatorCommand` (depends on T011, T012).
- [x] T015 [US3] In `Mentoory.Web/Areas/Platform/Views/Incubators/Edit.cshtml`, add a Tabler form-switch bound to `asp-for="IsActive"` labeled "Estado" (on = Activa, off = Inactiva), placed with Name/Description inside the card body (depends on T011).
- [x] T016 [P] [US3] Integration test (existing test project, e.g. `tests/Mentoory.Tests.Integration`): `UpdateIncubatorCommand` with `IsActive=false` deactivates an active incubator and `IsActive=true` reactivates, persisting `IsActive` and updating `UpdatedAtUtc`, leaving Name/Description as provided.

**Checkpoint**: US3 deliverable complete and demoable on its own.

---

## Phase 6: Polish & Cross-Cutting

- [x] T017 Run `dotnet build` (zero warnings) and `dotnet test` (all green) across the solution.
- [x] T018 Run `/simplify` on the changed code per the project Code Review Standards (methods <30 lines, no duplication, no magic strings).
- [ ] T019 [P] (Optional — SKIPPED to keep scope tight) Playwright E2E in `tests/Mentoory.Tests.E2E`: edit an incubator → toggle estado → save → assert Details badge; and assert Cancel on Edit lands on the Incubators list.

---

## Dependencies & Execution Order

- **Setup (T001)** → before everything.
- **US1 (P1)**: T002 first, then T003–T007 in parallel `[P]`, then T008 validation.
- **US2 (P2)**: T009 then T010 — independent of US1 and US3.
- **US3 (P3)**: T011 + T012 in parallel `[P]`; T013 after T012; T014 after T011+T012;
  T015 after T011; T016 after T012/T013. Independent of US1/US2.
- **Note**: US2 (T009) and US3 (T015) both edit `Incubators/Edit.cshtml` — sequence them
  (do not run those two as parallel `[P]` against each other).
- **Polish (T017–T019)**: after all stories.

## Parallel Opportunities

- US1: T003, T004, T005, T006, T007 (5 different view files) run together after T002.
- US3: T011 and T012 (different files) run together.
- Across stories: US1 and US2 and US3 can proceed concurrently except for the shared
  `Edit.cshtml` note above.

## Implementation Strategy

- **MVP = US1** (the originally reported, highest-value fix) — shippable alone.
- Then US2 (trivial consistency fix), then US3 (new capability).
- Each story is committed/verifiable independently; the full set ships as one PR to
  `develop`.
