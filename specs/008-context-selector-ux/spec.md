# Feature Specification: Cascading Context Selector UX

**Feature Branch**: `008-context-selector-ux`  
**Created**: 2026-04-11  
**Status**: Draft  
**Input**: User description: "Improve the UX/UI of the context selector with 3 cascading dropdowns (Role, Incubator, Project) replacing the current card grid, plus a top-bar modal for in-app switching."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Multi-Role User Selects Context via Cascading Dropdowns (Priority: P1)

A user with multiple role assignments logs in and is presented with 3 cascading dropdowns instead of a grid of cards. They select their role first, which filters the incubator dropdown to only show incubators available for that role. After selecting an incubator, the project dropdown shows only projects for that role+incubator combination. They click "Confirmar" and are redirected to the application.

**Why this priority**: This is the core UX improvement — replacing the card grid with progressive filtering. Without this, no other stories deliver value.

**Independent Test**: Can be fully tested by logging in with a multi-role user, selecting each dropdown in sequence, and confirming context is set correctly.

**Acceptance Scenarios**:

1. **Given** a user with 2+ role assignments across different incubators, **When** they arrive at /Context/Select, **Then** they see 3 vertically stacked dropdowns (Rol, Incubadora, Proyecto) and a disabled "Confirmar" button
2. **Given** the role dropdown is displayed, **When** the user selects a role, **Then** the incubator dropdown is populated via AJAX with only incubators available for that role
3. **Given** an incubator is selected, **When** the user selects it, **Then** the project dropdown is populated via AJAX with projects for that role+incubator
4. **Given** role and incubator are selected, **When** the user clicks "Confirmar", **Then** the context is set and the user is redirected (to returnUrl or home)
5. **Given** the user changes the role dropdown, **When** a new role is selected, **Then** the incubator and project dropdowns reset to their placeholder state

---

### User Story 2 - Auto-Select and Read-Only for Single Options (Priority: P1)

When a dropdown has exactly one option, it auto-selects that option and renders as a read-only (disabled) field. If the entire context resolves to a single combination, the page is bypassed entirely.

**Why this priority**: Tied to P1 because the cascade must handle single-option cases from the start — it's not an add-on, it's core behavior.

**Independent Test**: Can be tested by logging in with a user that has exactly one role but multiple incubators, verifying the role dropdown shows pre-selected and disabled.

**Acceptance Scenarios**:

1. **Given** a user with exactly one role but multiple incubators, **When** they arrive at /Context/Select, **Then** the role dropdown shows the single role pre-selected and disabled, and the incubator dropdown is ready for selection
2. **Given** a user with exactly one role, one incubator, and one project, **When** they log in, **Then** context is auto-set and the selector page is never shown
3. **Given** a user with exactly one role, one incubator, and zero projects, **When** they log in, **Then** context is auto-set at incubator level and the selector page is never shown

---

### User Story 3 - GlobalAdmin Browses All Incubators and Projects (Priority: P2)

A GlobalAdmin user selects the GlobalAdmin role, and the incubator dropdown shows ALL active incubators in the system (not just their assigned ones). After picking an incubator, the project dropdown shows ALL projects under that incubator. The project selection is optional — GlobalAdmin can proceed at incubator level.

**Why this priority**: Important for platform administrators but only applies to one role; the cascading framework from P1 must exist first.

**Independent Test**: Can be tested by logging in as GlobalAdmin, verifying all incubators appear in the dropdown, and confirming that proceeding without a project selection works.

**Acceptance Scenarios**:

1. **Given** a GlobalAdmin user, **When** they select the GlobalAdmin role, **Then** the incubator dropdown shows ALL active incubators in the system
2. **Given** a GlobalAdmin has selected an incubator, **When** the incubator is selected, **Then** the project dropdown shows ALL projects under that incubator
3. **Given** a GlobalAdmin has selected role and incubator but no project, **When** they click "Confirmar", **Then** the context is set at incubator level (no project scope)

---

### User Story 4 - Top-Bar Modal for In-App Context Switching (Priority: P2)

While using the application, a user clicks "Cambiar contexto" in the top-bar. A Bootstrap modal opens with the same 3 cascading dropdowns. They select a new context and click "Confirmar". The context switches via AJAX, a toast notification confirms success, and the page reloads.

**Why this priority**: High value for daily use but depends on the cascading dropdown implementation from P1.

**Independent Test**: Can be tested by navigating to any authenticated page, clicking "Cambiar contexto", completing the cascade in the modal, and verifying the context changes without full page navigation.

**Acceptance Scenarios**:

1. **Given** a logged-in user on any page, **When** they click "Cambiar contexto" in the top-bar, **Then** a Bootstrap modal opens with the 3 cascading dropdowns
2. **Given** the modal is open with role+incubator selected, **When** the user clicks "Confirmar", **Then** context switches via AJAX, a success toast is shown, and the page reloads
3. **Given** the user has unsaved form changes (data-track-changes), **When** they try to switch context via modal, **Then** a warning is shown before proceeding
4. **Given** the user switches to a role that cannot access the current area, **When** the context switch completes, **Then** the user is redirected to the home page instead of reloading

---

### User Story 5 - Shared Partial View for Both Contexts (Priority: P3)

The 3 cascading dropdowns and "Confirmar" button are implemented as a single shared partial view (_ContextSelector.cshtml) used by both the full page and the top-bar modal.

**Why this priority**: Implementation detail that ensures consistency and avoids duplication. Can be deferred to cleanup if needed, but should be part of the design.

**Independent Test**: Can be verified by confirming that both the full page and the modal render identical dropdown behavior from the same partial.

**Acceptance Scenarios**:

1. **Given** the full-page selector and the top-bar modal, **When** both are rendered, **Then** they share the same _ContextSelector partial view
2. **Given** a change to the dropdown layout, **When** the partial is updated, **Then** both the page and modal reflect the change

---

### Edge Cases

- **User has roles across different incubators with no overlap**: Each role selection shows a completely different set of incubators — cascade resets Incubator and Project when Role changes
- **GlobalAdmin with zero incubators in the system**: Incubator dropdown shows "Sin incubadoras disponibles", Confirmar stays disabled
- **User's role assignment is revoked while on the selector page**: POST validation catches it, shows error message
- **Incubator selection changes after project was already selected**: Project dropdown resets to placeholder
- **AJAX cascade failure**: Inline error message below dropdown with retry link
- **Session expired during modal interaction**: AJAX returns 401, user is redirected to login page
- **Browser back button after setting context**: Standard behavior, revisits GET /Context/Select

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST display 3 cascading dropdowns (Rol, Incubadora, Proyecto) vertically on the context selection page
- **FR-002**: Selecting a Role MUST filter the Incubator dropdown to incubators available for that role, via server-side AJAX
- **FR-003**: Selecting an Incubator MUST filter the Project dropdown to projects available for that role+incubator, via server-side AJAX
- **FR-004**: Project selection MUST be optional — user can confirm with only Role + Incubator selected
- **FR-005**: If a dropdown has exactly one option, it MUST auto-select and render as read-only (disabled field)
- **FR-006**: If the entire context resolves to a single combination, the selector page MUST be bypassed (auto-skip)
- **FR-007**: GlobalAdmin role MUST populate the Incubator dropdown with ALL active incubators in the system
- **FR-008**: GlobalAdmin incubator selection MUST populate the Project dropdown with ALL projects under that incubator
- **FR-009**: System MUST expose `GET /api/context/roles` returning `[{ role, displayName }]` — the distinct roles for the authenticated user
- **FR-010**: System MUST expose `GET /api/context/incubators?role={role}` returning `[{ id, name, roleAssignmentExternalId }]` for the authenticated user. For incubator-scoped roles (where RoleAssignment has no ProjectId), `roleAssignmentExternalId` is the assignment to use directly
- **FR-011**: System MUST expose `GET /api/context/projects?role={role}&incubatorId={id}` returning `[{ id, name, roleAssignmentExternalId }]` for the authenticated user. Each project entry includes the specific role assignment's ExternalId
- **FR-012**: All three cascade API endpoints MUST be secured with `[Authorize]` and scoped to the authenticated user's assignments
- **FR-013**: The "Confirmar" button MUST be disabled until at least Role + Incubator are selected
- **FR-014**: The top-bar "Cambiar contexto" button MUST open a Bootstrap 5 modal with the same cascading dropdowns
- **FR-015**: Modal context switch MUST use AJAX via `POST /api/context/switch`, show a toast notification, and reload the page
- **FR-016**: The `POST /api/context/switch` endpoint MUST be extended to accept optional `incubatorId`, `incubatorName`, `projectId`, and `projectName` override parameters for GlobalAdmin modal switching (in addition to the existing `roleAssignmentExternalId`)
- **FR-017**: Modal MUST preserve existing unsaved-changes check from context-switcher.js
- **FR-018**: If the new role cannot access the current area, the page MUST redirect to home after context switch
- **FR-019**: A shared `_ContextSelector.cshtml` partial MUST be used by both the full page and the modal

### Key Entities

- **UserContext** (read model): Represents a user's available context — RoleAssignmentExternalId, UserId, IncubatorId, IncubatorName, ProjectId, ProjectName, Role
- **IncubatorContextOptionDto**: Incubator option for GlobalAdmin cascade — IncubatorId, IncubatorName, Projects list
- **ProjectContextOptionDto**: Project option within an incubator — ProjectId, ProjectName

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Multi-role users see a clean 3-dropdown form instead of a grid of cards
- **SC-002**: Selecting a role filters incubator options in under 500ms (server-side AJAX)
- **SC-003**: Selecting an incubator filters project options in under 500ms (server-side AJAX)
- **SC-004**: GlobalAdmin can browse any incubator and any project in the system via dropdowns
- **SC-005**: Single-option dropdowns appear pre-selected and read-only without user interaction
- **SC-006**: Single-context users never see the selector page (auto-skip preserved)
- **SC-007**: Top-bar modal allows context switching from any page without full page navigation
- **SC-008**: Existing returnUrl flow continues to work correctly
- **SC-009**: Existing unsaved-changes warnings continue to work in the modal
- **SC-010**: No regressions in area-based role access checks after switching context

## Out of Scope

- Changing the role assignment domain model or database schema
- Changing authentication/cookie mechanics (reuse existing `UpdateAuthCookie`)
- Role management UI (adding/removing roles)
- Real-time permission refresh (if role is revoked mid-session, existing session behavior applies)
- Redesigning the Available Projects / enrollment page

## Assumptions

- Existing authentication and cookie mechanics (UpdateAuthCookie) are reused without modification
- The role assignment domain model and database schema are not changed
- Bootstrap 5 modal component is available (Phoenix Admin Template already includes it)
- The data volume for incubators and projects per user is small enough that 500ms server-side responses are achievable
- context-switcher.js is extended, not replaced — existing AJAX switch and area-role mapping logic is preserved
