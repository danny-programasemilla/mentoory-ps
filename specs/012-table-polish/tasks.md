# Tasks: Table Polish System-Wide

**Input**: Design documents from `/specs/012-table-polish/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, quickstart.md

**Tests**: Not requested — no test tasks included.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Web app**: `Mentoory.Web/` at repository root
- CSS: `Mentoory.Web/wwwroot/css/mentoory.css`
- JS: `Mentoory.Web/wwwroot/js/datatable-helper.js`
- Views: `Mentoory.Web/Areas/{Area}/Views/{Controller}/Index.cshtml`
- Component: `Mentoory.Web/Views/Shared/Components/DataTable/`

---

## Phase 1: Foundational (CSS + JS Infrastructure)

**Purpose**: Add global table styling rules and the header icon registry. These changes are prerequisites for all user stories.

**Why this is critical**: The CSS rules (zebra, hover, info padding) and the JS icon registry must exist before any view migration can deliver the polished experience.

- [x] T001 [P] Add table zebra striping and hover CSS rules to Mentoory.Web/wwwroot/css/mentoory.css — add `.card-table.table-striped` and `.card-table.table-hover` overrides if needed for brand-tinted colors, plus `.table thead th .ti` icon spacing (margin-right: 0.35rem)
- [x] T002 [P] Fix info line left padding in Mentoory.Web/wwwroot/css/mentoory.css — add `padding-left: 1rem` to `.dt-info` selector to align with table cell content, and matching `padding-right: 1rem` to `.dt-paging` for symmetry
- [x] T003 Add header icon registry (`COLUMN_ICON_MAP`) and `applyHeaderIcons(tableId)` function to Mentoory.Web/wwwroot/js/datatable-helper.js — keyword-to-icon map (correo/email→ti-mail, nombre→ti-user, apellido→ti-users, estado→ti-circle-check, fecha→ti-calendar, descripcion→ti-file-text, acciones→ti-settings, etapa→ti-list-check, proyecto→ti-briefcase, preguntas→ti-help-circle, version→ti-git-branch, suscripcion/nivel→ti-crown), called automatically from `initDataTable()` via `initComplete` callback

**Checkpoint**: CSS rules and icon registry in place. `dotnet build` passes with zero warnings.

---

## Phase 2: US5 — Shared Component Adoption (Priority: P1)

**Goal**: Rewrite the DataTable ViewComponent with a strongly-typed model that automatically applies zebra, hover, and header icon classes.

**Independent Test**: Invoke the component from any single view and verify the rendered `<table>` element includes `table-striped table-hover` classes, and headers show icons.

- [x] T004 Create strongly-typed ViewComponent model in Mentoory.Web/Views/Shared/Components/DataTable/DataTableViewComponent.cs — properties: `TableId` (string, required), `Columns` (string[], required), `Title` (string, optional); returns `View(model)`
- [x] T005 Update Mentoory.Web/Views/Shared/Components/DataTable/Default.cshtml — accept the strongly-typed model instead of ViewBag, render `<table>` with classes `table table-vcenter table-striped table-hover card-table w-100`, render `<th>` elements from model Columns array

**Checkpoint**: Shared component renders correctly when invoked with the new API. Build passes.

---

## Phase 3: US1 + US2 + US3 — View Migration (Priority: P1/P2)

**Goal**: Migrate all 7 DataTable views to use the shared component (US5/US1), which gives them zebra/hover/icons automatically (US1/US2), and replace all badge renders with `renderStatus()` calls (US3).

**Independent Test**: Navigate to each migrated view, verify zebra rows, hover effect, header icons, and status dots are all present. Verify sorting, pagination, and empty states still work.

### Administration Area

- [x] T006 [P] [US1] Migrate Mentoory.Web/Areas/Administration/Views/Users/Index.cshtml — replace inline `<div class="card"><div class="table-responsive"><table ...>` with `@await Component.InvokeAsync("DataTable", new { tableId = "usersTable", columns = new[] { "Correo Electronico", "Nombre", "Apellido", "Estado", "Fecha de Registro" } })`, replace badge render function for accountStatus with `renderStatus()` calls (Active→success, Locked→danger, PendingVerification→warning+animated)
- [x] T007 [P] [US1] Migrate Mentoory.Web/Areas/Administration/Views/Projects/Index.cshtml — replace inline table markup with `@await Component.InvokeAsync("DataTable", new { tableId = "projectsTable", columns = new[] { "Nombre", "Descripcion", "Etapa", "Estado", "Fecha de Creacion", "Acciones" } })`, replace isActive badge render with `renderStatus()` (Activo→success, Inactivo→secondary), keep description truncation and action button renders unchanged

### Coordination Area

- [x] T008 [P] [US1] Migrate Mentoory.Web/Areas/Coordination/Views/Diagnostics/Index.cshtml — replace inline table markup with `@await Component.InvokeAsync("DataTable", new { tableId = "diagnosticFormsTable", columns = new[] { "Nombre", "Modo Sincronizacion", "Preguntas", "Fecha de Creacion", "Acciones" } })`, replace syncMode badge render with `renderStatus()` (Desconectado→secondary, Sincronizacion parcial→info), keep action button render unchanged

### Participant Area

- [x] T009 [P] [US1] Migrate Mentoory.Web/Areas/Participant/Views/Diagnostic/List.cshtml — replace inline table markup with `@await Component.InvokeAsync("DataTable", new { tableId = "participantFormsTable", columns = new[] { "Nombre", "Preguntas", "Fecha de Creacion", "Acciones" } })`, no status column to update, keep action button render unchanged

### Platform Area

- [x] T010 [P] [US1] Migrate Mentoory.Web/Areas/Platform/Views/Users/Index.cshtml — replace inline table markup with `@await Component.InvokeAsync("DataTable", new { tableId = "usersTable", columns = new[] { "Correo Electronico", "Nombre", "Apellido", "Estado", "Fecha de Registro" } })`, replace accountStatus badge render with `renderStatus()` calls (same mapping as Admin Users)
- [x] T011 [P] [US1] Migrate Mentoory.Web/Areas/Platform/Views/Incubators/Index.cshtml — replace inline table markup with `@await Component.InvokeAsync("DataTable", new { tableId = "incubatorsTable", columns = new[] { "Nombre", "Descripcion", "Estado", "Fecha de Creacion", "Acciones" } })`, replace isActive badge render with `renderStatus()`, keep action button renders unchanged
- [x] T012 [P] [US1] Migrate Mentoory.Web/Areas/Platform/Views/Templates/Diagnostics.cshtml — replace inline table markup with `@await Component.InvokeAsync("DataTable", new { tableId = "templatesTable", columns = new[] { "Nombre", "Descripcion", "Nivel de Suscripcion", "Version", "Estado", "Fecha de Creacion" } })`, replace isActive badge render with `renderStatus()`

**Checkpoint**: All 7 DataTable views use the shared component. All status columns show dots instead of badges. Zebra, hover, and icons visible on all. Build passes.

---

## Phase 4: US4 — Static Table Polish (Priority: P3)

**Goal**: Apply zebra striping and hover to the 4 static HTML tables via CSS classes only.

**Independent Test**: Navigate to each static table view and verify alternating row backgrounds and hover effect.

- [x] T013 [P] [US4] Add `table-striped table-hover` classes to the `<table>` element in Mentoory.Web/Areas/Administration/Views/BatchUpload/Results.cshtml
- [x] T014 [P] [US4] Add `table-striped table-hover` classes to the correction history `<table>` elements in Mentoory.Web/Areas/Coordination/Views/AnswerCorrection/Index.cshtml
- [x] T015 [P] [US4] Add `table-striped table-hover` classes to the answer options `<table>` elements in Mentoory.Web/Areas/Coordination/Views/Diagnostics/Details.cshtml
- [x] T016 [P] [US4] Add `table-striped table-hover` classes to the `<table>` element in Mentoory.Web/Views/Home/Index.cshtml

**Checkpoint**: All static tables have zebra and hover. Build passes.

---

## Phase 5: Polish & Visual QA

**Purpose**: Final verification across all table views and build validation.

- [x] T017 Run `dotnet build` and verify zero warnings
- [x] T018 Visual QA: Navigate to all 7 DataTable views in browser — verify zebra rows, hover, header icons, status dots, info line padding, sorting, pagination, empty states, and skeleton loading are all correct
- [x] T019 Visual QA: Navigate to all 4 static table views — verify zebra rows and hover effect
- [x] T020 Verify no regression in DataTable features: test sorting by clicking column headers, test pagination by navigating pages, test empty state by filtering to no results (if filter available)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Foundational)**: No dependencies — can start immediately
- **Phase 2 (Component Rewrite)**: No dependency on Phase 1 (component is structural, CSS is presentational) — can run in parallel with Phase 1
- **Phase 3 (View Migration)**: Depends on Phase 1 AND Phase 2 completion
- **Phase 4 (Static Tables)**: Depends on Phase 1 only (CSS rules) — can run in parallel with Phase 2 and Phase 3
- **Phase 5 (QA)**: Depends on all previous phases

### User Story Dependencies

- **US5 (Component Adoption, P1)**: Phase 2 — no dependencies on other stories
- **US1 (Zebra/Hover, P1)**: Phase 1 (CSS) + Phase 2 (component classes) + Phase 3 (migration) — coupled with US5
- **US2 (Header Icons, P2)**: Phase 1 (icon registry) + Phase 3 (migration activates icons) — delivered automatically
- **US3 (Status Dots, P2)**: Phase 3 (badge→renderStatus replacement per view)
- **US4 (Info Line, P3)**: Phase 1 (CSS fix) — independent, delivered by T002 alone

### Within Each Phase

- Phase 1: T001 and T002 are parallel (different CSS sections); T003 depends on neither
- Phase 2: T004 before T005 (model before view)
- Phase 3: All view migration tasks (T006–T012) are parallel (different files)
- Phase 4: All static table tasks (T013–T016) are parallel (different files)

### Parallel Opportunities

```
Phase 1 + Phase 2 can run in parallel:
  Thread A: T001 [P] + T002 [P] (CSS rules)
  Thread B: T003 (icon registry)
  Thread C: T004 → T005 (component rewrite)

Phase 3 — all 7 views in parallel:
  T006 [P] + T007 [P] + T008 [P] + T009 [P] + T010 [P] + T011 [P] + T012 [P]

Phase 4 — all 4 static tables in parallel:
  T013 [P] + T014 [P] + T015 [P] + T016 [P]
```

---

## Implementation Strategy

### MVP First (US1 + US5 — Zebra/Hover + Component)

1. Complete Phase 1: CSS rules + icon registry
2. Complete Phase 2: ViewComponent rewrite
3. Migrate 1 view (e.g., Admin Users — T006)
4. **STOP and VALIDATE**: Verify zebra, hover, icons, status dots on that single view
5. If good, migrate remaining 6 views in parallel

### Incremental Delivery

1. Phase 1 + Phase 2 → Foundation ready
2. Phase 3 (one view at a time) → Each view is independently verifiable
3. Phase 4 → Static tables polished
4. Phase 5 → Full QA pass

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- US1, US2, and US5 are delivered simultaneously through the view migration — they are architecturally inseparable
- US3 (status dots) is a per-view change done during migration but could be done independently
- US4 (info padding) is a single CSS rule delivered in Phase 1
- Total: 20 tasks across 5 phases
- Commit after each phase or logical group
