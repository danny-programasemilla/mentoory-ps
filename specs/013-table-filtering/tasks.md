# Tasks: Table Filtering

**Input**: Design documents from `/specs/013-table-filtering/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md

**Tests**: Not requested — no test tasks included.

**Organization**: Tasks grouped by user story. US1 and US2 share Phase 3 since the "Filtrar" and "Limpiar" buttons are part of the same panel.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: No setup tasks needed — all changes modify existing files. No new projects, packages, or configuration.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Filter type registry and CSS foundation that ALL user stories depend on.

**CRITICAL**: No user story work can begin until this phase is complete.

- [ ] T001 Add FILTER_TYPE_REGISTRY object and FILTER_EXCLUDE_LIST array to Mentoory.Web/wwwroot/js/datatable-helper.js
- [ ] T002 [P] Add filter panel CSS styles (panel container, toggle link, badge, slide animation, responsive grid) to Mentoory.Web/wwwroot/css/mentoory.css

**Checkpoint**: Registry and styling ready — user story implementation can begin.

---

## Phase 3: User Story 1 + User Story 2 — Toggle, Apply & Clear Filters (Priority: P1)

**Goal**: Auto-generate a collapsible filter panel from column definitions. Users can toggle it, apply filters via "Filtrar", and reset via "Limpiar". Table reloads with filtered/unfiltered results. Badge shows active filter count.

**Independent Test**: Navigate to `/Administration/Users`, click "Filtros", select "Activo" in Estado dropdown, click "Filtrar" → table shows only active users, badge reads "Filtros (1)". Click "Limpiar" → all fields clear, full table loads, badge disappears.

### Implementation

- [ ] T003 [US1] Add buildFilterPanel(tableId, columns, overrides) function that generates filter form HTML and inserts it before the table card in Mentoory.Web/wwwroot/js/datatable-helper.js
- [ ] T004 [US1] Add updateFilterBadge(tableId) function that counts non-empty fields and updates toggle link badge in Mentoory.Web/wwwroot/js/datatable-helper.js
- [ ] T005 [US1] [US2] Modify initDataTable() to call buildFilterPanel() in initComplete, auto-set filterId to generated form, and wire "Filtrar" submit + "Limpiar" reset handlers in Mentoory.Web/wwwroot/js/datatable-helper.js
- [ ] T006 [US1] Add Filters consumption (email, firstName, lastName, accountStatus) to ListIncubatorMembersHandler in Mentoory.Access.Application/Queries/ListIncubatorMembers/ListIncubatorMembersHandler.cs
- [ ] T007 [P] [US1] Add Filters consumption (name, description, stage, status) to ListProjectsHandler in Mentoory.Tenant.Application/Queries/ListProjects/ListProjectsHandler.cs
- [ ] T008 [P] [US1] Add Filters consumption (email, firstName, lastName, accountStatus) to ListUsersHandler in Mentoory.Access.Application/Queries/ListUsers/ListUsersHandler.cs
- [ ] T009 [P] [US1] Add Filters consumption (name, description, status) to ListIncubatorsHandler in Mentoory.Tenant.Application/Queries/ListIncubators/ListIncubatorsHandler.cs
- [ ] T010 [P] [US1] Add Filters consumption (name, description, subscriptionLevel, status) to ListFormTemplatesHandler in Mentoory.Diagnostic.Application/Queries/ListFormTemplates/ListFormTemplatesHandler.cs
- [ ] T011 [P] [US1] Add Filters consumption (name, syncMode) to ListProjectFormsHandler in Mentoory.Diagnostic.Application/Queries/ListProjectForms/ListProjectFormsHandler.cs

**Checkpoint**: All 7 DataTable views show filter panel, filters work end-to-end, "Limpiar" resets everything.

---

## Phase 4: User Story 3 — URL Persistence and Sharing (Priority: P2)

**Goal**: Filter values sync to URL query params. Sharing a filtered URL restores the exact filtered view.

**Independent Test**: Apply filters on Users table, copy URL with `f_*` params, open in new tab → filter panel opens with values pre-filled, table shows filtered data. Click "Limpiar" → URL params removed.

### Implementation

- [ ] T012 [US3] Add syncFiltersToUrl(tableId) function that writes active filter values as f_{key} URL query params via history.replaceState in Mentoory.Web/wwwroot/js/datatable-helper.js
- [ ] T013 [US3] Add loadFiltersFromUrl(tableId) function that parses f_* query params, populates form fields, opens panel, and triggers table reload in Mentoory.Web/wwwroot/js/datatable-helper.js
- [ ] T014 [US3] Wire syncFiltersToUrl into "Filtrar" handler, clearFiltersFromUrl into "Limpiar" handler, and loadFiltersFromUrl into initDataTable page-load flow in Mentoory.Web/wwwroot/js/datatable-helper.js

**Checkpoint**: Filtered URLs are bookmarkable and shareable. "Limpiar" cleans URL.

---

## Phase 5: User Story 4 — Per-View Overrides (Priority: P2)

**Goal**: Views can pass a `filters` config array to exclude columns or provide custom dropdown options.

**Independent Test**: Add `filters: [{ column: 'actions', filterable: false }]` to any `initDataTable()` call → "Acciones" column has no filter field. Add `{ column: 'customField', type: 'select', options: [...] }` → custom dropdown renders.

### Implementation

- [ ] T015 [US4] Add override merge logic to buildFilterPanel() — process config.filters array, apply filterable:false exclusions and custom type/options overrides in Mentoory.Web/wwwroot/js/datatable-helper.js

**Checkpoint**: Per-view overrides work without breaking auto-detection on other columns.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Verification across all views and build validation.

- [ ] T016 Verify all 7 DataTable views display filter panel and produce correct filtered results
- [ ] T017 Verify build passes with zero warnings (TreatWarningsAsErrors)
- [ ] T018 Run quickstart.md validation scenarios on Administration/Users

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 2 (Foundational)**: No dependencies — start immediately
  - T001 and T002 can run in parallel (JS and CSS are separate files)
- **Phase 3 (US1+US2)**: Depends on Phase 2 completion — BLOCKS on T001 (registry)
  - T003-T005 are sequential (build panel → badge → wire into initDataTable)
  - T006-T011 (server handlers) can ALL run in parallel with each other
  - T006-T011 can start in parallel with T003-T005 (different files)
- **Phase 4 (US3)**: Depends on Phase 3 T005 completion (needs filter form wired)
- **Phase 5 (US4)**: Depends on Phase 3 T003 completion (needs buildFilterPanel to exist)
  - Can run in parallel with Phase 4
- **Phase 6 (Polish)**: Depends on all prior phases

### User Story Dependencies

- **US1+US2 (P1)**: Start after Foundational — no dependencies on other stories
- **US3 (P2)**: Requires US1 filter form to be wired (T005)
- **US4 (P2)**: Requires US1 panel generation function (T003). Can run in parallel with US3.

### Parallel Opportunities

```
Phase 2:  T001 ─────┐
          T002 ──[P]─┤
                     ▼
Phase 3:  T003 → T004 → T005 (sequential JS logic)
          T006 ──[P]─┐
          T007 ──[P]─┤
          T008 ──[P]─┤  (all 6 handlers in parallel)
          T009 ──[P]─┤
          T010 ──[P]─┤
          T011 ──[P]─┘
                     ▼
Phase 4:  T012 → T013 → T014 (sequential, same file)
Phase 5:  T015 ──────────[P with Phase 4]
                     ▼
Phase 6:  T016 → T017 → T018
```

---

## Implementation Strategy

### MVP First (US1 + US2 Only)

1. Complete Phase 2: Foundational (registry + CSS)
2. Complete Phase 3: US1+US2 (panel + server handlers)
3. **STOP and VALIDATE**: Test filter panel on all 7 views
4. Deploy/demo if ready — filtering works end-to-end

### Incremental Delivery

1. Phase 2 → Foundation ready
2. Phase 3 → US1+US2 complete → Test → Deploy (MVP!)
3. Phase 4 → US3 (URL persistence) → Test → Deploy
4. Phase 5 → US4 (per-view overrides) → Test → Deploy
5. Phase 6 → Polish → Final validation

---

## Summary

| Metric | Value |
|--------|-------|
| Total tasks | 18 |
| Phase 2 (Foundational) | 2 tasks |
| Phase 3 (US1+US2) | 6 tasks (JS) + 6 tasks (handlers) = 9 tasks |
| Phase 4 (US3) | 3 tasks |
| Phase 5 (US4) | 1 task |
| Phase 6 (Polish) | 3 tasks |
| Parallelizable tasks | 8 (T002, T007-T011, T015, T006) |
| Files modified | 8 (1 JS, 1 CSS, 6 C# handlers) |
| Files created | 0 |
| MVP scope | Phases 2+3 (11 tasks) |

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- US1 and US2 share Phase 3 because "Filtrar" and "Limpiar" are built in the same panel
- 6 unique query handlers (views 6 and 7 share ListProjectFormsHandler)
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
