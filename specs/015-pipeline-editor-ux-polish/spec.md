# Feature Specification: Pipeline Editor UX Polish

**Feature Branch**: `015-pipeline-editor-ux-polish`
**Created**: 2026-04-14
**Status**: Draft
**Input**: User description: "Fix 4 UX issues in the pipeline editor: sort persistence, badge contrast, vertical alignment, and inline form names for Diagnosis stages"

## User Scenarios & Testing

### User Story 1 - Persist Stage Reorder (Priority: P1)

A project coordinator drags a Diagnosis stage to a new position in the pipeline editor. After dropping, a "Guardar Orden" button appears. They click it, and the new order is saved. On page refresh, the stages remain in the new order.

**Why this priority**: Without persistence, the drag-and-drop feature is broken — users see visual feedback but nothing saves. This is a bug fix, not an enhancement.

**Independent Test**: Drag any middle stage to a different position, click "Guardar Orden", refresh the page, and confirm the new order persists.

**Acceptance Scenarios**:

1. **Given** a pipeline with 5+ stages, **When** the coordinator drags a middle stage to a new position, **Then** a "Guardar Orden" button appears in the header area.
2. **Given** the "Guardar Orden" button is visible, **When** the coordinator clicks it, **Then** the new order is saved via AJAX, a success message appears briefly, and the button hides.
3. **Given** the coordinator has dragged a stage, **When** they drag it back to its original position, **Then** the "Guardar Orden" button hides (order matches server state).
4. **Given** the coordinator has reordered stages and clicked "Guardar Orden", **When** they refresh the page, **Then** the stages appear in the saved order.
5. **Given** the AJAX save fails, **When** the coordinator clicks "Guardar Orden", **Then** an error message appears and the button remains visible for retry.

---

### User Story 2 - Readable Stage State Indicators (Priority: P2)

A coordinator opens the pipeline editor and can immediately distinguish which stage is active (In Progress), which are completed, and which are pending — without squinting at low-contrast badges.

**Why this priority**: Readability directly affects how quickly coordinators can assess pipeline status. Current badges blend into the background.

**Independent Test**: Open the pipeline editor and confirm each state (Pendiente, En Progreso, Completada) is visually distinct at a glance using colored dots and text labels.

**Acceptance Scenarios**:

1. **Given** a pipeline with stages in all three states, **When** the coordinator views the list, **Then** each state shows a small colored dot (gray=Pendiente, blue=En Progreso, green=Completada) followed by a text label.
2. **Given** one stage is "En Progreso", **When** the coordinator scans the list, **Then** the active stage's dot and label stand out with stronger visual emphasis compared to other states.

---

### User Story 3 - Vertically Aligned Stage Layout (Priority: P2)

A coordinator views the pipeline editor and all columns (drag handle, state, type, name, form info, dates, actions) align vertically across every row, making the list easy to scan.

**Why this priority**: Misaligned columns make it hard to scan and compare stages. Alignment is a basic readability requirement.

**Independent Test**: Open the pipeline editor with 5+ stages and confirm that drag handles, state indicators, type labels, names, and action buttons all line up in consistent columns.

**Acceptance Scenarios**:

1. **Given** a pipeline with stages of varying name lengths, **When** the coordinator views the list, **Then** all columns align vertically across rows using CSS grid tracks.
2. **Given** the Registration stage (no action buttons) and a Diagnosis stage (3 action buttons), **When** both are visible, **Then** the action button column still aligns — Registration simply has empty space.

---

### User Story 4 - Inline Form Name for Diagnosis Stages (Priority: P2)

A coordinator views the pipeline editor and can see which form is linked to each Diagnosis stage without clicking any button. The form name appears inline, next to the stage name.

**Why this priority**: Currently requires clicking the config button to see the assigned form — an unnecessary extra step that slows down pipeline review.

**Independent Test**: Open the pipeline editor for a project with Diagnosis stages that have assigned forms, and confirm form names appear inline in a dedicated column.

**Acceptance Scenarios**:

1. **Given** a Diagnosis stage with one form assigned, **When** the coordinator views the pipeline, **Then** the form name appears in the "form info" column (e.g., "Formulario de Innovacion").
2. **Given** a Diagnosis stage with multiple forms assigned, **When** the coordinator views the pipeline, **Then** all form names appear comma-separated.
3. **Given** a Diagnosis stage with no form assigned, **When** the coordinator views the pipeline, **Then** the form info column shows "Sin formulario" in muted text.
4. **Given** a non-Diagnosis stage (Registration, Mentorship, Closure), **When** the coordinator views the pipeline, **Then** the form info column is empty for that row.

---

### Edge Cases

- Project with only Registration + Closure (2 stages): no drag handles appear, no "Guardar Orden" button, grid renders correctly with just 2 rows.
- Diagnosis stage with 3+ forms: comma-separated list renders without truncation.
- User drags multiple times before saving: button stays visible, final DOM order is what gets POSTed.
- Deleted form still referenced by StageFormAssignment: query handler skips it gracefully (no crash, no phantom entry).

## Requirements

### Functional Requirements

- **FR-001**: System MUST show a "Guardar Orden" button only when the current DOM stage order differs from the original server order.
- **FR-002**: System MUST POST the reordered stage ExternalIds via AJAX to the existing `Reorder` action when the "Guardar Orden" button is clicked, including the anti-forgery token.
- **FR-003**: System MUST hide the "Guardar Orden" button and show success feedback after a successful save; on failure, display an error message and keep the button visible.
- **FR-004**: System MUST hide the "Guardar Orden" button if the user drags stages back to the original server order.
- **FR-005**: System MUST display stage state as a small colored dot (gray=Pendiente, blue=En Progreso, green=Completada) followed by a text label, replacing the current full-color badge.
- **FR-006**: System MUST give the "En Progreso" state stronger visual emphasis (bolder dot/text) to distinguish the active stage.
- **FR-007**: System MUST render the stage list using CSS grid with fixed column tracks: drag handle, state dot, type label, stage name, form info, dates, actions.
- **FR-008**: System MUST extend `PipelineStageDto` and `StageViewModel` with an `AssignedFormNames` property (list of strings).
- **FR-009**: The pipeline query handler MUST join to `StageFormAssignment` and `ProjectForm` to populate form names for Diagnosis stages.
- **FR-010**: System MUST display assigned form names inline in the form info column for Diagnosis stages, comma-separated if multiple.
- **FR-011**: System MUST display "Sin formulario" in muted text for Diagnosis stages with no form assigned.
- **FR-012**: System MUST leave the form info column empty for non-Diagnosis stages.

### Key Entities

- **PipelineStageDto**: Extended with `AssignedFormNames` (read-only list of form name strings). Populated via cross-domain join in the query handler.
- **StageViewModel**: Extended with `AssignedFormNames` to carry form names from DTO to view.

## Success Criteria

### Measurable Outcomes

- **SC-001**: Coordinators can reorder stages and persist the new order in under 5 seconds (drag + click save).
- **SC-002**: All three stage states are visually distinguishable at arm's length — no two states share the same color or visual weight.
- **SC-003**: All 7 column tracks (handle, state, type, name, form, dates, actions) align vertically across every row in the pipeline editor.
- **SC-004**: Coordinators can identify which form is linked to a Diagnosis stage without leaving the pipeline editor page.
- **SC-005**: Zero compiler warnings after all changes.
- **SC-006**: All UI text remains in Spanish.

## Assumptions

- The existing `Reorder` AJAX endpoint works correctly — this spec only fixes the client-side submission gap.
- The anti-forgery token is available in the page via `@Html.AntiForgeryToken()` in existing forms.
- Tabler icon set includes a filled circle icon (e.g., `ti ti-circle-filled` or `ti ti-point-filled`) suitable for status dots.
- The cross-domain join (Tenant stage + Diagnostic assignment + Diagnostic form) is acceptable for a read-only query since no domain invariants are crossed.
- CSS grid is supported by all target browsers (modern evergreen browsers).
