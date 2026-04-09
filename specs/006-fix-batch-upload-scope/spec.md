# Feature Specification: Fix Batch Upload Project Scope Authorization Bug

**Feature Branch**: `006-fix-batch-upload-scope`  
**Created**: 2026-04-08  
**Status**: Draft  
**Input**: User description: "Fix batch upload project dropdown showing unauthorized projects to Project Coordinators. The dropdown shows all incubator projects instead of only the ones the user is assigned to manage. This is a security/authorization bug requiring backend enforcement and broader hardening review."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Project Coordinator sees only assigned projects in batch upload (Priority: P1)

A Project Coordinator navigates to the batch upload users screen. The project dropdown must display only the projects explicitly assigned to them, not every project in the incubator. This prevents unauthorized data exposure and enforces least-privilege access.

**Why this priority**: This is the core security bug. An overly broad project listing exposes data outside the user's authorized scope, violating tenant isolation and least-privilege principles. Fixing this eliminates the immediate unauthorized data exposure.

**Independent Test**: Can be fully tested by logging in as a Project Coordinator with assignments to 2 of 5 incubator projects, opening the batch upload screen, and verifying the dropdown shows exactly 2 projects.

**Acceptance Scenarios**:

1. **Given** a Project Coordinator assigned to Project A and Project B within Incubator X, **When** they open the batch upload users screen, **Then** the project dropdown shows only Project A and Project B.
2. **Given** a Project Coordinator assigned to Project A only, **When** they open the batch upload users screen, **Then** the project dropdown shows only Project A.
3. **Given** a Project Coordinator with no project assignments in their active incubator, **When** they attempt to open the batch upload users screen, **Then** the system prevents access entirely and redirects with a message explaining no projects are assigned.
4. **Given** a Project Coordinator assigned to Project A, **When** they attempt to submit a batch upload for Project B (to which they are not assigned), **Then** the system rejects the request at the server level.

---

### User Story 2 - IncubatorAdmin retains full incubator-scoped access to batch upload (Priority: P1)

An Incubator Admin navigates to the batch upload users screen. The project dropdown must continue to show all active registration-stage projects within their incubator. The fix for Project Coordinators must not regress existing Incubator Admin behavior.

**Why this priority**: Equal to P1 because breaking existing admin workflows would be a regression. IncubatorAdmin access is the established baseline that must remain intact.

**Independent Test**: Can be fully tested by logging in as an IncubatorAdmin, opening batch upload, and verifying all active registration-stage projects in the incubator appear.

**Acceptance Scenarios**:

1. **Given** an Incubator Admin for Incubator X which has 5 active registration-stage projects, **When** they open the batch upload users screen, **Then** the project dropdown shows all 5 projects.
2. **Given** an Incubator Admin for Incubator X, **When** they submit a batch upload for any project in Incubator X, **Then** the system processes the upload successfully.
3. **Given** an Incubator Admin for Incubator X, **When** they attempt to submit a batch upload for a project in Incubator Y, **Then** the system rejects the request.

---

### User Story 3 - GlobalAdmin retains unrestricted batch upload access (Priority: P2)

A Global Admin navigates to the batch upload users screen after selecting an incubator context. The project dropdown must show all active registration-stage projects in the selected incubator. No new restrictions should be applied to GlobalAdmin.

**Why this priority**: P2 because GlobalAdmin access is already established and less likely to break, but must be validated as a non-regression.

**Independent Test**: Can be fully tested by logging in as GlobalAdmin, selecting an incubator context, and verifying all projects in that incubator appear in the dropdown.

**Acceptance Scenarios**:

1. **Given** a Global Admin who has selected Incubator X as their active context, **When** they open the batch upload users screen, **Then** the project dropdown shows all active registration-stage projects in Incubator X.

---

### User Story 4 - Server-side enforcement prevents scope bypass (Priority: P1)

Regardless of what the frontend displays, the server must enforce that no user can batch-upload users into a project outside their authorized scope. This applies to all roles and must be enforced at the backend, not just via UI filtering.

**Why this priority**: P1 because even if the dropdown is fixed, a crafted API request could bypass UI restrictions. Server-side enforcement is the actual security control.

**Independent Test**: Can be tested by sending a direct API request (bypassing the UI) attempting to batch-upload into an unauthorized project, and verifying the server rejects it.

**Acceptance Scenarios**:

1. **Given** a Project Coordinator assigned to Project A only, **When** they submit a batch upload request targeting Project B via direct API call, **Then** the server returns an authorization error and does not process the upload.
2. **Given** an Incubator Admin for Incubator X, **When** they submit a batch upload request targeting a project in Incubator Y via direct API call, **Then** the server returns an authorization error and does not process the upload.

---

### User Story 5 - Broader authorization hardening for similar list/dropdown endpoints (Priority: P2)

Other list or dropdown endpoints in the platform that serve role-scoped data are reviewed and hardened to ensure they also enforce proper scope filtering at the backend. This prevents the same class of bug from appearing in other screens.

**Why this priority**: P2 because the immediate batch upload bug is the most urgent, but similar patterns elsewhere represent systemic risk that should be addressed in the same pass.

**Independent Test**: Can be tested by reviewing each identified list endpoint and verifying that role-scoped users only see data within their authorized scope.

**Acceptance Scenarios**:

1. **Given** a platform-wide audit has been completed, **When** the audit results are reviewed, **Then** every endpoint returning project-scoped or incubator-scoped data has been identified and classified as safe, risky, or confirmed issue.
2. **Given** any endpoint that returns project-scoped data, **When** accessed by a Project Coordinator, **Then** it returns only projects assigned to that coordinator.
3. **Given** any endpoint that returns incubator-scoped data, **When** accessed by an Incubator Admin, **Then** it returns only data from their incubator.

---

### Edge Cases

- What happens when a Project Coordinator's assignment is revoked while they have the batch upload screen open? The system should reject submission attempts for projects they are no longer assigned to.
- How does the system handle a user with multiple role assignments (e.g., Project Coordinator for Project A and Mentor for Project B)? Only the active role's scope should apply.
- What happens when a Project Coordinator is assigned to a project that is not in Registration or InProgress stage? That project should not appear in the dropdown.
- What if a user has both IncubatorAdmin and ProjectCoordinator roles for the same incubator? The active role context determines the filtering behavior.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST filter the batch upload project dropdown to show only projects the current user is explicitly authorized to manage, based on their active role and project assignments.
- **FR-002**: System MUST enforce project-scope authorization at the server level for all batch upload submissions, rejecting any request targeting a project outside the user's authorized scope.
- **FR-003**: System MUST continue to allow IncubatorAdmin users to see all active registration-stage projects within their incubator in the batch upload dropdown.
- **FR-004**: System MUST continue to allow GlobalAdmin users to see all active registration-stage projects within their selected incubator context.
- **FR-005**: System MUST validate that the requested incubator in any project-listing query matches the user's active session context, preventing cross-incubator data access.
- **FR-006**: System MUST apply the same scope-aware filtering pattern to every endpoint in the platform that returns project-scoped or incubator-scoped data. A platform-wide audit MUST be performed to identify and harden all such endpoints.
- **FR-007**: System MUST NOT rely solely on frontend UI hiding or filtering as an authorization control; backend enforcement is the source of truth.
- **FR-008**: System MUST deny batch upload access to roles that are not authorized for this operation (Mentor, Entrepreneur, Sponsor).
- **FR-009**: System MUST grant ProjectCoordinator SCOPED access to batch upload, restricted to their explicitly assigned projects only. The platform Constitution MUST be updated to reflect this access level change from NONE to SCOPED for ProjectCoordinator.

### Key Entities

- **RoleAssignment**: Represents a user's assignment to a specific role within a specific incubator and optionally a specific project. The ProjectId field determines project-scoped access for roles like ProjectCoordinator.
- **Project**: An incubation project within an incubator, with lifecycle stage tracking. Only projects in Registration or InProgress stages appear in batch upload.
- **Incubator**: The top-level organizational tenant. All data is scoped to an incubator boundary.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A Project Coordinator sees exactly and only their assigned projects in every project selection dropdown across the platform, with zero unauthorized projects visible.
- **SC-002**: 100% of batch upload submissions targeting an unauthorized project are rejected by the server before any data processing occurs.
- **SC-003**: IncubatorAdmin and GlobalAdmin batch upload workflows continue to function identically to their current behavior with zero regressions.
- **SC-004**: All list/dropdown endpoints identified in the hardening review enforce backend scope filtering, with zero endpoints relying solely on frontend restrictions.
- **SC-005**: No user can infer, enumerate, or access project information outside their authorized scope through any batch upload-related interaction.

## Clarifications

### Session 2026-04-08

- Q: When a ProjectCoordinator has no project assignments, should the system prevent access entirely or show an empty page? → A: Prevent access entirely — redirect with a message explaining no projects are assigned.
- Q: Should the hardening review scope cover only batch-upload-adjacent endpoints or the entire platform? → A: Platform-wide — audit and harden every endpoint that returns project-scoped or incubator-scoped data.

## Assumptions

- The active role context (stored in session claims) accurately reflects the user's current role assignment at the time of request. Stale session data is a separate concern.
- The existing RoleAssignment aggregate correctly tracks which users are assigned to which projects.
- The fix should follow the existing authorization patterns in the codebase (Authorize attributes, permission checks, tenant context) rather than introducing new authorization mechanisms.
- The "registration-stage" filter (projects in Registration or InProgress stages) remains a valid business rule for the batch upload dropdown regardless of role.
- The broader hardening review in User Story 5 covers endpoints within the current codebase. Future endpoints should follow the hardened patterns established by this fix.
