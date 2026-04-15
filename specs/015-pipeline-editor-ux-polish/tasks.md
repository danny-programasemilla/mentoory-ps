# Tasks: Pipeline Editor UX Polish

**Input**: Design documents from `/specs/015-pipeline-editor-ux-polish/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md

**Tests**: Not requested in spec — no test tasks included.

**Organization**: Tasks grouped by user story. View changes are incremental: US3 (grid layout) restructures the view first, then US2/US4/US1 modify it in place.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3, US4)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: No setup needed — existing project with established structure.

(No tasks)

---

## Phase 2: Foundational (Backend)

**Purpose**: Extend DTOs and create cross-domain query for form names. MUST complete before US4 view work.

- [x] T001 [P] Extend PipelineStageDto with StageId as first positional parameter in Mentoory.Tenant.Application/Queries/GetProjectPipeline/ProjectPipelineDto.cs
- [x] T002 Update GetProjectPipelineHandler to pass ProjectStage.Id into StageId when mapping stages in Mentoory.Tenant.Application/Queries/GetProjectPipeline/GetProjectPipelineHandler.cs
- [x] T003 [P] Create StageFormNamesDto record with IReadOnlyDictionary<long, IReadOnlyList<string>> FormNamesByStageId in Mentoory.Diagnostic.Application/Queries/GetStageFormNames/StageFormNamesDto.cs
- [x] T004 [P] Create GetStageFormNamesQuery record implementing IBaseRequest<StageFormNamesDto> in Mentoory.Diagnostic.Application/Queries/GetStageFormNames/GetStageFormNamesQuery.cs
- [x] T005 Create GetStageFormNamesHandler extending BaseCommandHandler — query IStageFormAssignmentRepository and IProjectFormRepository, group active form names by ProjectStageId, skip deleted forms in Mentoory.Diagnostic.Application/Queries/GetStageFormNames/GetStageFormNamesHandler.cs
- [x] T006 [P] Add List<string> AssignedFormNames property (default empty) to StageViewModel in Mentoory.Web/Areas/Coordination/Models/ProjectPipelineViewModels.cs
- [x] T007 Update ProjectPipelineController.Index to dispatch GetProjectPipelineQuery and GetStageFormNamesQuery via Task.WhenAll, then merge form names into StageViewModel using StageId-to-ExternalId mapping in Mentoory.Web/Areas/Coordination/Controllers/ProjectPipelineController.cs

**Checkpoint**: Backend ready — build passes, controller serves merged data.

---

## Phase 3: User Story 3 - Vertically Aligned Stage Layout (Priority: P2) — Layout Foundation

**Goal**: Replace list-group markup with CSS grid for vertical column alignment across all rows.

**Independent Test**: Open pipeline editor with 5+ stages and confirm drag handles, state indicators, type labels, names, and action buttons all line up in consistent columns.

**Why first**: The grid layout is the structural foundation for all subsequent view changes (US2 status dots, US4 form column, US1 save button). Must be in place before other stories modify the view.

### Implementation for User Story 3

- [x] T008 [US3] Add .pipeline-grid (7-column grid: 24px 120px 100px 1fr 1fr auto auto) and .pipeline-row (display: contents) CSS styles in Mentoory.Web/wwwroot/css/site.css
- [x] T009 [US3] Rewrite Index.cshtml — replace list-group with .pipeline-grid container, convert each stage to .pipeline-row with 7 grid cells (handle, state, type, name, form placeholder, dates, actions), preserve existing drag-and-drop data attributes and modal markup in Mentoory.Web/Areas/Coordination/Views/ProjectPipeline/Index.cshtml

**Checkpoint**: Grid layout renders with aligned columns. Drag-and-drop still works. Modals still function.

---

## Phase 4: User Story 2 - Readable Stage State Indicators (Priority: P2)

**Goal**: Replace low-contrast badges with colored status dots for immediate state recognition.

**Independent Test**: Open pipeline editor and confirm each state (Pendiente, En Progreso, Completada) is visually distinct using colored dots and text labels.

### Implementation for User Story 2

- [x] T010 [US2] Add .status-dot CSS styles — gray dot for Pendiente, blue for En Progreso (with font-weight: 600 emphasis), green for Completada — using ti-point-filled icon sizing in Mentoory.Web/wwwroot/css/site.css
- [x] T011 [US2] Replace badge markup with status-dot indicators (<span class="status-dot ..."><i class="ti ti-point-filled"></i> Label</span>) in the state column of Index.cshtml, remove GetStateBadgeClass helper in Mentoory.Web/Areas/Coordination/Views/ProjectPipeline/Index.cshtml

**Checkpoint**: All three states visually distinct at a glance. No badge remnants.

---

## Phase 5: User Story 4 - Inline Form Name for Diagnosis Stages (Priority: P2)

**Goal**: Show assigned form names inline in the pipeline editor for Diagnosis stages without clicking config.

**Independent Test**: Open pipeline editor with Diagnosis stages that have assigned forms and confirm form names appear in the form info column.

**Depends on**: Phase 2 (backend query and controller merge)

### Implementation for User Story 4

- [x] T012 [US4] Add form name rendering in the form info grid cell of Index.cshtml — show comma-joined AssignedFormNames for Diagnosis stages, "Sin formulario" in muted text when empty, empty cell for non-Diagnosis stages in Mentoory.Web/Areas/Coordination/Views/ProjectPipeline/Index.cshtml

**Checkpoint**: Form names visible for Diagnosis stages. Non-Diagnosis rows have empty form column.

---

## Phase 6: User Story 1 - Persist Stage Reorder (Priority: P1)

**Goal**: Wire drag-and-drop to the existing Reorder endpoint with an explicit save button.

**Independent Test**: Drag a middle stage to a new position, click "Guardar Orden", refresh, and confirm the new order persists.

**Why last in execution**: The save button UI needs the grid layout from US3 in place. The JS logic (T014) is file-independent and can run in parallel with Phases 3-5.

### Implementation for User Story 1

- [x] T013 [US1] Add "Guardar Orden" button (d-none by default) to the header area next to existing action buttons, include @Html.AntiForgeryToken() form in Mentoory.Web/Areas/Coordination/Views/ProjectPipeline/Index.cshtml
- [x] T014 [P] [US1] Rewrite pipeline-editor.js — on DOMContentLoaded capture original ExternalId order, after each drop compare to original and show/hide save button, on click POST orderedStageIds via fetch with RequestVerificationToken header to Reorder action, handle success (hide button, update originalOrder, show toast) and error (show alert, keep button) in Mentoory.Web/wwwroot/js/pipeline-editor.js

**Checkpoint**: Drag-drop + save + refresh preserves order. Drag back to original hides button. Network error shows alert.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Build verification and manual testing of all acceptance scenarios.

- [x] T015 Build verification — run dotnet build and confirm zero warnings
- [ ] T016 Manual verification of all acceptance scenarios per spec.md test matrix

---

## Dependencies & Execution Order

### Phase Dependencies

- **Foundational (Phase 2)**: No dependencies — can start immediately
- **US3 (Phase 3)**: Can start immediately (no backend dependency) — BLOCKS US2, US4 view tasks, and US1 button placement
- **US2 (Phase 4)**: Depends on US3 (Phase 3) — modifies view within grid structure
- **US4 (Phase 5)**: Depends on Phase 2 (backend) AND US3 (Phase 3) — needs form data + grid layout
- **US1 (Phase 6)**: Button markup depends on US3 (Phase 3); JS (T014) is file-independent
- **Polish (Phase 7)**: Depends on all previous phases

### Task Dependencies

```
T001 ──┐
T003 ──┤ (parallel)
T004 ──┤
T006 ──┘
       │
T002 ──┤ (T001 must complete first — same file)
T005 ──┤ (T003, T004 must complete first — references both)
       │
T007 ──┘ (all T001-T006 must complete)

T008 ──→ T009 (CSS before view rewrite)

T009 ──→ T011 ──→ T012 ──→ T013 (sequential view modifications)

T010 ── (parallel with T009-T011, different file — CSS only)

T014 ── (parallel with T008-T013, independent JS file)

T015 ── (after all implementation tasks)
T016 ── (after T015)
```

### Parallel Opportunities

- **Phase 2**: T001, T003, T004, T006 can run in parallel (different files)
- **Cross-phase**: T014 (JS rewrite) can run in parallel with Phases 3-5 (view/CSS changes)
- **CSS tasks**: T008 and T010 can run in parallel (same file but additive, no conflicts)

---

## Parallel Example: Phase 2 Foundational

```bash
# Launch all independent DTO/query file creations together:
Task: "T001 — Extend PipelineStageDto with StageId in ProjectPipelineDto.cs"
Task: "T003 — Create StageFormNamesDto in StageFormNamesDto.cs"
Task: "T004 — Create GetStageFormNamesQuery in GetStageFormNamesQuery.cs"
Task: "T006 — Add AssignedFormNames to StageViewModel in ProjectPipelineViewModels.cs"

# Then sequential:
Task: "T002 — Update handler (depends on T001)"
Task: "T005 — Create handler (depends on T003, T004)"
Task: "T007 — Update controller (depends on all above)"
```

---

## Implementation Strategy

### Recommended Execution Order (Single Developer)

1. Complete Phase 2: Foundational backend (T001-T007)
2. Complete Phase 3: US3 grid layout (T008-T009) + start T014 JS in parallel
3. Complete Phase 4: US2 status dots (T010-T011)
4. Complete Phase 5: US4 form names (T012)
5. Complete Phase 6: US1 save button (T013, T014 if not already done)
6. Complete Phase 7: Polish (T015-T016)
7. **STOP and VALIDATE**: All 4 UX fixes working together

### MVP Scope

US1 (persist reorder) is marked P1 but its view placement depends on US3 (grid layout). For true MVP: complete Phase 2 + Phase 3 + Phase 6 to get the save button working in the grid layout. US2 and US4 are P2 enhancements.

### Incremental Delivery

1. Backend ready (Phase 2) → foundation for form names
2. Grid layout (US3) → structural improvement, all columns aligned
3. Status dots (US2) → readability improvement
4. Form names (US4) → information density improvement
5. Save button (US1) → critical bug fix for drag-and-drop persistence

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- All view modifications to Index.cshtml are sequential (same file)
- T014 (JS rewrite) is the only task that can truly run in parallel across phases
- CSS tasks (T008, T010) target site.css — additive changes, low conflict risk
- No database schema changes — all changes are DTO/view/JS/CSS level
- Commit after each phase completion for clean git history
