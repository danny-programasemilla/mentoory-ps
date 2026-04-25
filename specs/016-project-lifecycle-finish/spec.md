# Feature Specification: Project Lifecycle Application + UI Completion

**Feature Branch**: `016-project-lifecycle-finish`
**Created**: 2026-04-18
**Status**: Draft
**Input**: User description: "Project lifecycle application + UI completion: AdvanceProjectStageCommand + Coordination area UI + stage-gated action filters (FR-037, FR-039)"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Advance a project through the lifecycle (Priority: P1)

A Project Coordinator needs to move a project forward through its seven mandatory stages (Registration → Forms → Analysis → Learning Assignment → Mentoring → Final Evaluation → Closure). Stage transitions are manual, irreversible decisions that record who advanced the project and when. Today, the underlying stage advancement behavior exists on the project entity, but there is no user-facing path to trigger it — coordinators cannot actually move projects forward.

**Why this priority**: Without the ability to advance stages, the lifecycle model exists only on paper. Every downstream capability (stage-gated actions, progress reporting, notifications) depends on being able to transition a project from one stage to the next. This is the minimum viable slice that makes the lifecycle real for users.

**Independent Test**: A user with coordination permissions opens a project in the coordination area, sees the current stage, clicks "advance stage," confirms the action, and the project immediately reflects the new current stage. The previous stage is recorded as completed with a timestamp and the user identity; the newly started stage is recorded as in progress. Can be demonstrated end-to-end on a single project without any other stories in place.

**Acceptance Scenarios**:

1. **Given** a project is in stage "Registration" (In Progress) and the acting user has permission to manage the project's lifecycle, **When** the user advances the stage from the coordination area and confirms, **Then** "Registration" is recorded as completed, "Forms" is recorded as in progress, the timestamp and acting user are captured on both records, and the project's visible current stage becomes "Forms."
2. **Given** a project is in the final stage "Closure" (In Progress), **When** the user attempts to advance again, **Then** the system refuses the action, keeps the stage unchanged, and surfaces a clear message that the project has already reached the final stage.
3. **Given** a project's current stage has already been marked completed (edge state from an older transition), **When** the user attempts to advance, **Then** the system refuses and explains that the current stage is not in progress.
4. **Given** a user without lifecycle management permission on this project's incubator, **When** the user attempts to open the advance-stage action, **Then** the action is not offered in the UI and any direct attempt is rejected with an authorization error.

---

### User Story 2 - See stage-aware project overview in the coordination area (Priority: P2)

Project Coordinators need a dedicated project overview inside the coordination area that shows where the project stands in its lifecycle, what has been completed, and what is next. Today coordinators have no coordination-area entry point for a project — they can only see the administrative details page, which shows the current stage as text with no timeline context and no coordination actions.

**Why this priority**: Advancing stages (Story 1) is the core action, but coordinators need to see the project's full lifecycle position before they decide to advance. This view is what makes the advance decision informed rather than blind. It also becomes the landing page for every other stage-specific action (Story 3).

**Independent Test**: A coordinator navigates to the coordination area, opens a project, and sees the seven-stage lifecycle with each stage's state (Not Started / In Progress / Completed), start and completion timestamps for stages that have them, and a clear indicator of the current stage. The page is fully readable and informative even before the advance action or stage-gated filters are implemented.

**Acceptance Scenarios**:

1. **Given** a coordinator opens a project that has progressed through Registration and is now in Forms, **When** the overview loads, **Then** Registration is shown as completed (with its start and completion timestamps), Forms is shown as in progress (with its start timestamp), and the remaining five stages are shown as not started.
2. **Given** a brand-new project, **When** the coordinator opens the overview, **Then** Registration is shown as in progress and all other stages are shown as not started with no timestamps.
3. **Given** a project has reached Closure and is completed, **When** the coordinator opens the overview, **Then** all seven stages appear in the completed state with their timestamps, and no advance action is offered.

---

### User Story 3 - Stage-gated actions and guidance (Priority: P3)

The coordination area offers multiple stage-specific actions (for example: diagnostic form management, answer correction, learning assignments, mentoring coordination). Today these actions are visible regardless of whether the project has reached the relevant stage, which lets coordinators attempt work that does not yet apply — such as correcting diagnostic answers before Forms has started, or configuring mentoring before the Mentoring stage. Coordinators need the UI to reflect the rule that actions belong to specific stages.

**Why this priority**: Story 3 depends on Stories 1 and 2 being in place (you need a current stage and an overview page from which to offer actions). It delivers compounding value — every existing and future stage-specific action gets the same consistent treatment. Without it, coordinators can still do their jobs via the detail pages individually, so it is an enhancement rather than a blocker.

**Independent Test**: A coordinator opens a project that is in the Registration stage and sees that stage-gated actions belonging to later stages (answer correction, learning assignments, mentoring) are either hidden or clearly marked as not yet available, while actions belonging to the current stage are prominent and actionable. After advancing the stage, the set of available actions updates accordingly without any page-specific workarounds.

**Acceptance Scenarios**:

1. **Given** a project is in Registration, **When** a coordinator views the coordination overview, **Then** actions tied to the Forms, Analysis, Learning Assignment, Mentoring, Final Evaluation, and Closure stages are presented as locked or hidden with a clear indication of which stage unlocks them.
2. **Given** a project is in Forms, **When** the coordinator advances the stage to Analysis, **Then** on the next view Forms-only actions become locked, Analysis actions become available, and the change is consistent across every place that lists coordination actions for this project.
3. **Given** a coordinator attempts to open a stage-gated action directly by URL while the project is not in the gating stage, **When** the request is processed, **Then** the system refuses the action with a message explaining which stage is required, and returns the coordinator to the coordination overview.
4. **Given** a coordinator hovers or focuses on a locked action, **When** the tooltip or helper text is shown, **Then** the text names the stage that unlocks the action (for example, "Available from the Mentoring stage").

---

### Edge Cases

- A coordinator tries to advance a project while another coordinator advances the same project concurrently — only one advancement must succeed; the second must be rejected with a clear message and the overview must show the true current state on refresh.
- A project is inactive (deactivated) — advancing must be refused and stage-gated actions must be locked with a clear "project is inactive" message rather than a generic stage error.
- The acting user has coordination permission only in a different incubator — all lifecycle actions and the coordination overview for this project must be denied.
- A stage-gated action was already started before this feature shipped (legacy data) — locking rules apply going forward; already-completed work is not retroactively invalidated.
- The final stage (Closure) has no "next stage" — the advance action must not appear for a project whose current stage is Closure.
- The acting user lacks a valid incubator context selection — the coordination overview must redirect the user to the context selector with a clear message, consistent with other coordination-area pages.

## Requirements *(mandatory)*

### Functional Requirements

**Stage Advancement**

- **FR-001**: The system MUST expose a single coordinator-facing action that advances a project from its current stage to the immediately following stage in the fixed seven-stage sequence (Registration, Forms, Analysis, Learning Assignment, Mentoring, Final Evaluation, Closure).
- **FR-002**: The system MUST require the acting user to have lifecycle-management permission on the project's incubator before offering or accepting the advance action.
- **FR-003**: The system MUST reject any advance attempt when the project's current stage is not "In Progress," returning a clear message that identifies the reason.
- **FR-004**: The system MUST reject any advance attempt when the project is already at the final stage (Closure), returning a clear message that no further stages exist.
- **FR-005**: The system MUST record, for every successful stage advancement: the completed stage's completion timestamp, the newly started stage's start timestamp, and the identity of the user who performed the advancement.
- **FR-006**: The system MUST guarantee that concurrent advance attempts on the same project cannot both succeed — only one transition is applied and the other is rejected with a conflict message.
- **FR-007**: The system MUST refuse to advance the lifecycle of a project that is marked inactive, returning a message that identifies the inactive state.
- **FR-008**: Stage advancement MUST be immediately reflected in the coordination overview and in any stage-derived UI affordances for that project (stage-gated actions) without requiring a full re-authentication or context switch.

**Coordination Area Overview**

- **FR-009**: The system MUST provide a coordination-area page per project that shows the project identity (name, description, incubator context) and the full seven-stage lifecycle in canonical order.
- **FR-010**: For each of the seven stages, the overview MUST display the stage's current state (Not Started, In Progress, or Completed).
- **FR-011**: For stages that have been started, the overview MUST display the start timestamp, and for stages that have been completed, the overview MUST display the completion timestamp.
- **FR-012**: The overview MUST visually highlight the project's current stage so that a coordinator can identify it at a glance.
- **FR-013**: The overview MUST be reachable only by users with lifecycle-management permission on the project's incubator; other users must be redirected or shown an authorization error consistent with the rest of the coordination area.
- **FR-014**: The overview MUST redirect to the incubator-context selector when the acting user has no incubator context selected, using the same pattern as other coordination-area pages.
- **FR-015**: The overview MUST expose the advance-stage action only when the advancement is legally allowed (in-progress current stage, not at Closure, project active, user has permission).

**Stage-Gated Actions**

- **FR-016**: The system MUST associate each coordination-area action (diagnostic form management, answer correction, learning assignments, mentoring coordination, final evaluation activities, closure activities) with the specific lifecycle stage (or stages) in which it is valid.
- **FR-017**: The coordination overview MUST render each stage-gated action in one of three states: available (the project's current stage gates this action), locked (the project has not yet reached the gating stage), or past (the gating stage has already been completed and the action is no longer a current-stage activity).
- **FR-018**: Locked actions MUST surface a clear indication of which stage unlocks them, both visually and via accessible helper text.
- **FR-019**: The system MUST reject any attempt to execute a stage-gated action when the project is not in the gating stage, regardless of whether the user reaches the action via UI navigation or a direct URL, and MUST return the user to the coordination overview with an explanatory message.
- **FR-020**: The mapping between actions and gating stages MUST be defined once in a single place so that changes to the mapping automatically propagate to every surface that offers, filters, or enforces stage-gated actions.
- **FR-021**: The stage-gated filter MUST re-evaluate as soon as a stage advancement succeeds, without requiring the user to re-enter the coordination area or refresh beyond the standard post-action navigation.

**Auditability and Feedback**

- **FR-022**: After a successful advance, the system MUST present a clear success message naming the new current stage.
- **FR-023**: After a rejected advance, the system MUST present a clear error message that names the reason (not in progress, already at final stage, project inactive, permission denied, concurrency conflict) and leaves the project unchanged.
- **FR-024**: The stage history (per-stage start user, start timestamp, completion timestamp) MUST be queryable for display on the overview.

### Key Entities

- **Project Lifecycle Position**: Represents where a given project stands across the seven mandatory stages. Attributes include the current stage, the current stage's state, and the full per-stage history (state, start timestamp, completion timestamp, advanced-by user for each stage).
- **Stage-Gated Action Registry**: Represents the set of coordination-area actions together with the lifecycle stage(s) in which each action is valid. Provides the single source of truth for "which actions are available given the current stage" decisions.
- **Lifecycle Transition Record**: Represents one successful advancement from one stage to the next — who performed it, when, which stage was completed, and which stage was started. Used for audit and display.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of projects created via the platform have their lifecycle position advanceable through the coordinator UI by any user with lifecycle-management permission, with no need to use database tools or support intervention.
- **SC-002**: A Project Coordinator can complete a stage advancement in under 30 seconds from the coordination-area project overview, including confirmation.
- **SC-003**: In usability testing, at least 90% of coordinators correctly identify the project's current stage and the next allowed action within 10 seconds of opening the coordination overview for an unfamiliar project.
- **SC-004**: Zero stage-gated actions are executable when the project is not in the gating stage, verified by attempting every action from every stage in pre-release validation.
- **SC-005**: Every successful stage advancement produces a displayable audit trail entry (who, when, completed stage, started stage) visible on the project overview without navigating to a separate audit page.
- **SC-006**: When an action is locked due to stage gating, 95% of coordinators in usability testing correctly identify which stage would unlock it based on the UI affordance alone.
- **SC-007**: Support tickets related to "I cannot find how to move my project forward" drop to zero within one release cycle after launch, compared to the pre-feature baseline.

## Assumptions

- The seven-stage lifecycle and its ordering are fixed by platform governance and will not change within the scope of this feature (ref: FR-036 in the platform core spec). This feature does not introduce stage reordering, skipping, or custom stages.
- The Project Coordinator role (and any higher role that inherits lifecycle-management permission) is the sole initiator of stage advancement; self-service advancement by participants is out of scope.
- Stage advancement is irreversible from the user's perspective. Rollback or un-advancing is out of scope and would require a separate spec.
- The project's current stage and per-stage history are already captured by the existing project entity, so the data model does not need new attributes for advancement recording — the gap is in the coordinator-facing command and UI.
- The lifecycle-management permission already exists in the platform's permission model; this feature uses it rather than introducing a new permission.
- Notifications about stage changes (email, in-app alerts) are handled by the platform's existing notification system and are out of scope here, except that the stage advance action must leave the system in a state where any downstream notification triggers behave correctly.
- Stage-gated actions refer to existing coordination-area capabilities plus any future ones. The scope of this feature is the gating mechanism; the underlying action implementations are owned by their respective features.
- Users always operate in the context of exactly one incubator at a time; the coordination overview uses the already-established incubator context selection pattern.
- Translations and UI labels are in Spanish, consistent with the rest of the platform.
- Performance targets for the overview page match standard platform expectations (page loads visibly complete in under 2 seconds at p95 under normal load); no custom capacity planning is introduced by this feature.
