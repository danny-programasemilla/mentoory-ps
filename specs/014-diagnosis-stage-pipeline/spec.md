# Feature Specification: Configurable Stage Pipeline & Flexible Diagnosis Module

**Feature Branch**: `014-diagnosis-stage-pipeline`
**Created**: 2026-04-14
**Status**: Draft
**Input**: Redesign the project stage pipeline and diagnosis module to support configurable, repeatable diagnosis stages with flexible form-to-stage assignment and explicit question selection per stage.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Configurable Stage Pipeline (Priority: P1)

An admin or project coordinator creates a new project. The system automatically generates the default 5-stage pipeline (Registration, Diagnosis, Mentorship, Diagnosis, Closure). The admin then customizes the pipeline by adding a mid-project Diagnosis stage between two Mentorship stages, renaming stages for clarity (e.g., "Diagnóstico Inicial", "Diagnóstico Final"), and reordering them. As the project progresses, the admin manually advances through stages one at a time.

**Why this priority**: The configurable stage pipeline is the foundation for all other features. Without flexible stages, diagnosis assignments and executions cannot be decoupled from the hardcoded Initial/Final model.

**Independent Test**: Can be fully tested by creating a project, verifying default pipeline, adding/removing/reordering stages, and advancing through them. Delivers value as a standalone project lifecycle management tool.

**Acceptance Scenarios**:

1. **Given** a new project is created, **When** the system initializes, **Then** the default pipeline contains 5 stages: Registration → Diagnosis → Mentorship → Diagnosis → Closure, each with state NotStarted.
2. **Given** a project with the default pipeline, **When** the admin adds a Diagnosis stage at position 4 (between Mentorship and existing Diagnosis), **Then** the pipeline contains 6 stages with correct positions and auto-generated display names (e.g., "Diagnóstico 1", "Diagnóstico 2", "Diagnóstico 3").
3. **Given** a project with stages, **When** the admin reorders stages via drag-and-drop or up/down controls, **Then** positions update correctly and the pipeline reflects the new order.
4. **Given** a project with stages, **When** the admin renames a stage to "Evaluación Inicial", **Then** the display name updates and persists.
5. **Given** a stage with associated form assignments, **When** the admin attempts to remove it, **Then** the system shows a confirmation dialog with the count of affected data before allowing removal.
6. **Given** the first stage (Registration) is InProgress, **When** the admin clicks "Avanzar etapa", **Then** the current stage moves to Completed and the next stage moves to InProgress.
7. **Given** the last stage (Closure) is InProgress, **When** the admin advances it, **Then** the project is marked as completed with no further advancement possible.
8. **Given** a pipeline, **When** the admin attempts to create one that does not start with Registration or end with Closure, **Then** the system shows a validation error.

---

### User Story 2 - Stage-Form Assignment with Question Selection (Priority: P1)

A project coordinator navigates to a Diagnosis stage in the pipeline editor and clicks "Configurar diagnóstico". They assign one or more existing ProjectForms to that stage. For each assignment, they select which specific questions from the form should appear when entrepreneurs fill out the diagnosis at that stage. The same form can be assigned to multiple Diagnosis stages with different question selections.

**Why this priority**: This is the core mechanism that replaces the hardcoded EvaluationStage/StageApplicability model. Without it, diagnosis execution cannot be flexible.

**Independent Test**: Can be tested by assigning forms to stages, selecting questions, and verifying the assignments persist correctly. Delivers value as a diagnosis configuration tool even before entrepreneurs submit responses.

**Acceptance Scenarios**:

1. **Given** a Diagnosis stage with no form assignments, **When** the coordinator clicks "Agregar formulario", **Then** a modal shows all available ProjectForms for the project.
2. **Given** a coordinator selects a ProjectForm, **When** the assignment is created, **Then** all questions from the form are selected by default.
3. **Given** an existing assignment, **When** the coordinator opens question selection, **Then** they see a checkbox list grouped by topic with select-all toggles per group and globally.
4. **Given** a form with 20 questions, **When** the coordinator deselects 5 questions and saves, **Then** the assignment records 15 selected questions.
5. **Given** the same ProjectForm, **When** the coordinator assigns it to two different Diagnosis stages with different question selections, **Then** both assignments coexist independently.
6. **Given** an assignment with existing DiagnosticResponses, **When** the coordinator attempts to remove it, **Then** the system shows a warning that responses exist and requires confirmation.
7. **Given** a non-Diagnosis stage, **When** the system attempts to create a form assignment, **Then** a validation error is raised: "Solo se pueden asignar formularios a etapas de tipo Diagnóstico".

---

### User Story 3 - Diagnosis Execution by Entrepreneur (Priority: P1)

An entrepreneur logs into the system and navigates to "Diagnósticos". They see all Diagnosis stages for their project with completion status per form. They click on a pending form, answer the selected questions (grouped by topic), and submit. The response is stored independently for that specific stage-form assignment.

**Why this priority**: This is the primary user-facing feature — entrepreneurs filling out diagnoses is the core workflow of the platform.

**Independent Test**: Can be tested end-to-end by having an entrepreneur access the diagnosis landing, fill out a questionnaire, submit, and verify the response is stored linked to the correct StageFormAssignment.

**Acceptance Scenarios**:

1. **Given** an entrepreneur with access to a project, **When** they navigate to "Diagnósticos", **Then** they see all Diagnosis stages with per-form completion status (Pendiente/Completado).
2. **Given** a Diagnosis stage with 2 assigned forms, **When** the entrepreneur views the stage, **Then** they see both forms with individual completion status.
3. **Given** a StageFormAssignment with 15 of 20 questions selected, **When** the entrepreneur opens the questionnaire, **Then** only the 15 selected questions appear, grouped by topic.
4. **Given** a questionnaire with required questions, **When** the entrepreneur attempts to submit without answering all required questions, **Then** validation prevents submission with a clear message.
5. **Given** a completed questionnaire, **When** the entrepreneur submits, **Then** the DiagnosticResponse is created with IsCompleted = true and linked to the specific StageFormAssignment.
6. **Given** a response already submitted for a StageFormAssignment, **When** the entrepreneur attempts to submit again, **Then** the system shows a message: "Ya existe un diagnóstico completado para esta etapa".
7. **Given** a Diagnosis stage with no form assignments, **When** the entrepreneur accesses it, **Then** an informational message appears: "No hay formularios asignados para esta etapa. Contacte al coordinador".
8. **Given** a deactivated StageFormAssignment, **When** the entrepreneur attempts to submit, **Then** a validation error is shown: "Esta asignación de diagnóstico ya no está activa".

---

### User Story 4 - Results Comparison Across Stages (Priority: P2)

A coordinator or mentor selects an entrepreneur and views their diagnostic timeline — a chronological list of all diagnosis executions across stages. They select two or more executions to compare. The system displays topic-level score aggregation with deltas (improvement/decline) and a question-level drill-down showing side-by-side answers for shared questions only.

**Why this priority**: Comparison is the analytical payoff of the flexible diagnosis model. It depends on stories 1-3 being complete, but delivers the key insight that justifies multiple diagnosis stages.

**Independent Test**: Can be tested by having an entrepreneur complete diagnoses at two different stages (with overlapping questions), then verifying the comparison view shows correct topic-level deltas and question-level side-by-side answers.

**Acceptance Scenarios**:

1. **Given** an entrepreneur with completed diagnoses at 2 different stages, **When** the coordinator opens the diagnostic timeline, **Then** both executions appear chronologically with stage name, form name, date, and overall score summary.
2. **Given** the timeline view, **When** the coordinator selects 2 executions and clicks "Comparar", **Then** the comparison view opens with topic-level and question-level tabs.
3. **Given** the topic-level tab, **When** comparing two executions, **Then** each topic shows previous score, current score, delta, and percentage change with visual indicators (green for improvement, red for decline).
4. **Given** two executions from the same form with the same questions, **When** viewing the question-level tab, **Then** all questions appear with side-by-side answers and score deltas.
5. **Given** two executions from different forms with only 5 shared questions out of 15 and 20 total, **When** viewing the question-level tab, **Then** only the 5 shared questions appear, with a note: "Mostrando 5 preguntas compartidas entre las evaluaciones seleccionadas".
6. **Given** two executions with zero shared questions, **When** viewing the comparison, **Then** the topic-level tab shows available data and the question-level tab shows: "No hay preguntas compartidas entre las evaluaciones seleccionadas".

---

### User Story 5 - Answer Correction with Audit Trail (Priority: P2)

A coordinator, mentor, or admin views a completed diagnostic response and corrects an individual answer. The system records the previous value, the new value, who corrected it, when, and an optional reason. The corrected score is reflected in topic aggregations.

**Why this priority**: Answer correction is existing functionality that must continue working with the new StageFormAssignment-based model. It is critical for data integrity but depends on responses existing first.

**Independent Test**: Can be tested by submitting a response, correcting an answer, and verifying the audit trail records previous values, the correction appears in the response detail, and topic scores update.

**Acceptance Scenarios**:

1. **Given** a completed DiagnosticResponse, **When** an authorized user navigates to the correction view, **Then** all question responses are shown with current values and an "edit" action.
2. **Given** a question response with a SingleSelect answer, **When** the user changes the selected option and provides a reason, **Then** an AnswerCorrection record is created with previous value, new value, correctedByUserId, correctedAtUtc, and reason.
3. **Given** a corrected answer, **When** viewing the response detail, **Then** the correction history is visible with all corrections ordered chronologically.
4. **Given** a correction that changes a scored answer, **When** topic score aggregation is recalculated, **Then** the new score reflects the corrected answer.

---

### User Story 6 - Single Result Detail View (Priority: P2)

A coordinator, mentor, or admin selects a specific diagnosis execution from the timeline and views the full detail: topic-level score aggregation and per-question answers with scores.

**Why this priority**: Detailed view of individual results is required before comparison makes sense, and supports day-to-day monitoring of entrepreneur progress.

**Independent Test**: Can be tested by submitting a response and viewing its detail page with topic aggregation and individual answers.

**Acceptance Scenarios**:

1. **Given** a completed DiagnosticResponse, **When** the coordinator clicks "Ver detalle" from the timeline, **Then** the detail view shows: stage name, form name, completion date, entrepreneur name.
2. **Given** the detail view, **When** scores are aggregated by topic, **Then** each topic shows total score, question count, and average score.
3. **Given** the detail view, **When** viewing individual answers, **Then** each question shows the response value, selected option(s), and associated score.
4. **Given** a response with corrections, **When** viewing the detail, **Then** corrected answers are visually flagged and correction history is accessible.

---

### User Story 7 - Template Sync with Assignment Awareness (Priority: P3)

A coordinator has a ProjectForm connected to a global FormTemplate with PartialSync enabled. When the template is updated with new questions, the coordinator syncs the ProjectForm. New questions appear in the ProjectForm but are NOT automatically added to any existing StageFormAssignments — the coordinator must explicitly add them to question selections.

**Why this priority**: Template sync is an existing feature that must work correctly with the new assignment model, but is not critical path for the core diagnosis workflow.

**Independent Test**: Can be tested by syncing a form, verifying new questions appear in the form but not in existing assignments, then manually adding them.

**Acceptance Scenarios**:

1. **Given** a ProjectForm with PartialSync connected to a template, **When** the template adds 3 new questions and the coordinator syncs, **Then** the ProjectForm gains the 3 new questions.
2. **Given** the synced form has existing StageFormAssignments, **When** sync completes, **Then** the assignments' selected question lists remain unchanged (new questions are NOT auto-included).
3. **Given** the coordinator opens question selection for an assignment after sync, **When** they see the updated list, **Then** the new questions appear as unselected and can be added.

---

### Edge Cases

- What happens when all stages except Registration and Closure are removed? The system allows it — no diagnosis functionality is available, but it is a valid pipeline.
- What happens when a stage is reordered while InProgress? Allowed — position changes do not affect state. The active stage stays active at its new position.
- What happens when a ProjectForm is deleted or deactivated while it has active StageFormAssignments? Assignments referencing that form are deactivated. Existing responses remain as historical records.
- What happens when two entrepreneurs submit to the same StageFormAssignment concurrently? Each gets their own DiagnosticResponse — no conflict, as uniqueness is per entrepreneur per assignment.
- What happens when a stage advances past a Diagnosis stage where not all entrepreneurs have completed? Allowed — stage advancement is admin-controlled and not blocked by incomplete diagnoses. UX shows a warning with completion statistics.

## Requirements *(mandatory)*

### Functional Requirements

#### Stage Pipeline (Tenant Domain)

- **FR-001**: System MUST support exactly 4 predefined stage types: Registration, Diagnosis, Mentorship, Closure.
- **FR-002**: System MUST allow duplicate stage types in a project's pipeline (e.g., multiple Diagnosis stages).
- **FR-003**: System MUST auto-create a default 5-stage pipeline when a project is created: Registration → Diagnosis → Mentorship → Diagnosis → Closure.
- **FR-004**: System MUST allow admins to add stages to the pipeline at any position.
- **FR-005**: System MUST allow admins to remove stages, with confirmation required when associated data exists.
- **FR-006**: System MUST allow admins to reorder stages within the pipeline.
- **FR-007**: System MUST allow admins to rename stages (override auto-generated display names).
- **FR-008**: System MUST support manual stage advancement by admins (not date-driven).
- **FR-009**: System MUST store planned start/end dates as informational only — dates MUST NOT block any system actions.
- **FR-010**: System MUST enforce that the pipeline starts with a Registration stage and ends with a Closure stage.
- **FR-011**: Each stage MUST track state (NotStarted, InProgress, Completed), timestamps, and who advanced it.

#### Diagnosis Form Management (Diagnostic Domain)

- **FR-012**: System MUST preserve FormTemplate (global) functionality unchanged — platform-level templates with questions, answer options, SWOT/ODSR scoring.
- **FR-013**: System MUST preserve ProjectForm (project-level) clone, customize, and sync functionality.
- **FR-014**: System MUST remove StageApplicability from QuestionTemplate and Question entities, replacing it with per-assignment question selection.

#### Stage-Form Assignment

- **FR-015**: System MUST allow admins to assign one or more ProjectForms to each Diagnosis-type stage via StageFormAssignment.
- **FR-016**: Each StageFormAssignment MUST include an explicit selection of which questions from the form are active for that stage.
- **FR-017**: When a form is first assigned to a stage, all questions MUST be selected by default.
- **FR-018**: The same ProjectForm MUST be assignable to multiple Diagnosis stages with different question selections.
- **FR-019**: StageFormAssignment creation/modification/removal MUST NOT be blocked by stage state.
- **FR-020**: System MUST prevent form assignment to non-Diagnosis stage types.
- **FR-021**: System MUST prevent removal of assignments that have associated DiagnosticResponses without explicit confirmation.

#### Diagnosis Execution

- **FR-022**: Entrepreneurs MUST see all Diagnosis stages for their project with per-form completion status.
- **FR-023**: When filling a questionnaire, only questions selected in the StageFormAssignment MUST be displayed.
- **FR-024**: Each DiagnosticResponse MUST be linked to a specific StageFormAssignment.
- **FR-025**: System MUST enforce one response per entrepreneur per StageFormAssignment.
- **FR-026**: Submission MUST mark the response as completed (one-way, idempotent).
- **FR-027**: Authorized users MUST be able to correct answers at any time with full audit trail.

#### Results & Comparison

- **FR-028**: System MUST aggregate scores per topic within a single DiagnosticResponse.
- **FR-029**: System MUST support cross-stage topic-level comparison with score deltas and percentage changes.
- **FR-030**: System MUST support cross-stage question-level comparison for shared questions only (matched by QuestionId).
- **FR-031**: System MUST display a chronological timeline of all diagnosis executions for an entrepreneur within a project.
- **FR-032**: Comparison view MUST indicate when executions have no shared questions.

### Key Entities

- **ProjectStage**: An instance of a stage type within a project's pipeline. Has a position, display name, state, and informational dates. Child entity of Project.
- **StageFormAssignment**: Links a ProjectForm to a Diagnosis-type ProjectStage with an explicit selection of active questions. Aggregate root in Diagnostic domain.
- **AssignedQuestion**: Records which specific question from a ProjectForm is active for a given StageFormAssignment. Child entity of StageFormAssignment.
- **DiagnosticResponse**: An entrepreneur's completed answers for a specific StageFormAssignment. Refactored to reference StageFormAssignment instead of EvaluationStage.
- **FormTemplate**: Global diagnostic form template (unchanged).
- **ProjectForm**: Project-level clone of a FormTemplate (StageApplicability removed from questions).
- **ScoreDelta**: Value object representing the change in score between two diagnosis executions for a topic.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Admins can create, customize, and manage project stage pipelines with any combination of the 4 stage types in under 2 minutes.
- **SC-002**: Coordinators can assign a form to a Diagnosis stage and configure question selection in under 1 minute.
- **SC-003**: Entrepreneurs can complete a diagnosis questionnaire from landing page to submission confirmation in under 10 minutes (for a 30-question form).
- **SC-004**: Coordinators can compare two diagnosis executions and identify topic-level score changes within 30 seconds.
- **SC-005**: 100% of data access operations enforce multi-tenant isolation — no cross-incubator data leakage under any scenario.
- **SC-006**: All E2E integration tests pass covering: full pipeline lifecycle, form assignment, diagnosis execution, comparison, correction, multi-tenant isolation, template sync, and concurrent submissions.
- **SC-007**: Zero compiler warnings across all modified and new code.
- **SC-008**: All user-facing text displayed in Spanish.
- **SC-009**: Question-level comparison correctly identifies shared questions and omits non-shared ones with appropriate messaging.
- **SC-010**: Answer corrections update topic score aggregations and maintain complete audit trail.

## Out of Scope

- Mentorship Plan Generation — future spec, consumes diagnosis results but not part of this work
- Knowledge Structure integration — topic linkage exists but knowledge module is scaffolded only
- Pipeline templates (predefined pipeline configurations for quick project setup) — future enhancement
- Export/PDF of comparison results — future enhancement
- Notification triggers on stage advancement or diagnosis completion — future spec
- Bulk diagnosis assignment (assign same form to all Diagnosis stages at once) — future UX optimization

## Dependencies

- **Tenant domain** (Project, ProjectStage) — exists, requires refactoring for configurable pipeline
- **Diagnostic domain** (FormTemplate, ProjectForm, DiagnosticResponse) — exists, requires refactoring for StageFormAssignment model
- **Access domain** (Permission, RoleAssignment) — exists, requires 3 new permissions
- **SSDT/DACPAC schema** — exists, requires new tables and altered columns
- **Tabler Admin Template** (Bootstrap 5) — exists, used for all new UI
- **DataTables** — exists, used for list views
- **MediatR 14.1, FluentValidation 12.1, EF Core 10.x** — exist, no version changes

## Assumptions

- This is a pre-production system — breaking refactoring of existing Diagnostic and Tenant domain models is acceptable without backward compatibility.
- The existing FormTemplate, ProjectForm clone/customize/sync functionality is stable and well-tested — modifications are limited to removing StageApplicability.
- The 4 stage types (Registration, Diagnosis, Mentorship, Closure) cover all current and foreseeable use cases — custom stage types are not needed.
- Drag-and-drop reordering for stages may fall back to up/down arrow controls if implementation complexity is too high — functional parity is the requirement, not a specific UX mechanism.
- Entrepreneurs have a single active project context selected via the existing cascading context selector.
- The existing multi-tenant infrastructure (ITenantContext, global query filters) is sufficient for new entities — no changes to the tenancy framework are needed.
- The Access domain's existing role-permission model supports the 3 new permissions without structural changes.
