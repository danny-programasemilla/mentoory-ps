# Feature Specification: Controlled Pause for Validation, Correction, and Production Readiness (Phase 1-4)

**Feature Branch**: `002-phase1-4-hardening`  
**Created**: 2026-04-01  
**Status**: Draft  
**Input**: User description: "Addendum Specification — Controlled Pause for Validation, Correction, and Production Readiness (Phase 1–4)"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Role-Based Dashboard Routing (Priority: P1)

As any authenticated user, when I log in, the system automatically routes me to the dashboard corresponding to my role. If I hold multiple roles, I am prompted to select one before proceeding. GlobalAdmin users always land on the Platform dashboard with full menu visibility.

**Why this priority**: Login routing is the first interaction every user has with the system. Incorrect routing breaks trust and blocks all downstream workflows.

**Independent Test**: Log in as each role type and verify the correct dashboard appears with the expected menu items visible.

**Acceptance Scenarios**:

1. **Given** a GlobalAdmin user, **When** they log in, **Then** they are routed to the Platform dashboard and see all menu options across all modules.
2. **Given** a user with a single role (e.g., ProjectCoordinator), **When** they log in, **Then** they are routed to the role-specific dashboard with only permitted menu items visible.
3. **Given** a user with multiple roles, **When** they log in, **Then** they are presented with a role selection screen before being routed to the corresponding dashboard.
4. **Given** a user with invalid or expired credentials, **When** they attempt to log in, **Then** they see a clear error message in Spanish and remain on the login page.

---

### User Story 2 - Context Selection Flow (Priority: P1)

As a context-scoped user (any role below GlobalAdmin in global scope), when I need to work within a specific incubator and project, the system guides me through context selection in the correct order: Role, then Incubator, then Project. Until context is fully established, context-dependent features are inaccessible.

**Why this priority**: Context selection is the gateway to all operational features. An incorrect or incomplete flow leaves users stranded without access to their work.

**Independent Test**: Log in as a multi-incubator, multi-project user and verify the selection follows Role > Incubator > Project order, and context-scoped pages are blocked until selection completes.

**Acceptance Scenarios**:

1. **Given** a user with access to multiple incubators, **When** they reach the context selection screen, **Then** they must first select an incubator before the project list is shown.
2. **Given** a user who has selected an incubator, **When** the project list appears, **Then** only projects belonging to that incubator are shown.
3. **Given** a user who has not completed context selection, **When** they try to access a context-scoped page directly (e.g., via deep link), **Then** they are redirected to context selection with a message explaining why, and the return URL is preserved.
4. **Given** a GlobalAdmin operating in global scope (no specific incubator/project), **When** they access listing or read-only views, **Then** they see data across all incubators without needing to select a context.

---

### User Story 3 - Context Switching and Safe Return Navigation (Priority: P1)

As a user with an active context, I can switch to a different incubator or project from the top navigation bar at any time. After switching, the system navigates me safely: if the previous page is valid in the new context, I stay there; otherwise, I am returned to the dashboard.

**Why this priority**: Users working across multiple projects need fluid context switching. Unsafe transitions (broken pages, data leaks, crashes) make the system unusable for multi-context users.

**Independent Test**: Switch context while on a context-scoped page and verify the system either keeps you on the equivalent page or redirects to the dashboard.

**Acceptance Scenarios**:

1. **Given** a user on a context-scoped page, **When** they switch to a different project via the top bar, **Then** the system updates the active context and navigates to the dashboard if the current page does not exist in the new context.
2. **Given** a user on a non-context-scoped page (e.g., profile settings), **When** they switch context, **Then** they remain on the same page with the new context applied.
3. **Given** a user who switches context, **When** the new context loads, **Then** no data from the previous context is visible or accessible anywhere on the page.
4. **Given** a user mid-action (e.g., filling a form), **When** they attempt to switch context, **Then** they receive a confirmation prompt warning about unsaved changes before the switch proceeds.

---

### User Story 4 - Authorization Boundary Enforcement (Priority: P1)

As any user, the system strictly enforces that I can only see menu items, access pages, and perform actions permitted by my role. Higher-privilege roles (GlobalAdmin, IncubatorAdmin) inherit access to all lower-role features. There are no unauthorized access paths.

**Why this priority**: Authorization is a security fundamental. Any gap exposes sensitive data or allows unauthorized actions, making the system unfit for production.

**Independent Test**: For each role, attempt to access pages and actions above, at, and below their privilege level; verify access is granted or denied correctly.

**Acceptance Scenarios**:

1. **Given** a ProjectCoordinator user, **When** they attempt to access an IncubatorAdmin-only page via URL, **Then** they receive an access denied response and are not shown the page content.
2. **Given** a GlobalAdmin user, **When** they navigate the system, **Then** every menu item across all modules is visible and every page is accessible.
3. **Given** an Entrepreneur user, **When** they view the navigation menu, **Then** they see only menu items for diagnostic submission, learning, and session attendance — no management features.
4. **Given** any page restricted to a specific role, **When** a user with a higher-privilege role attempts access, **Then** the system grants access because higher roles inherit all lower-role permissions.

---

### User Story 5 - Platform vs Administration Separation (Priority: P2)

As a GlobalAdmin or IncubatorAdmin, the navigation clearly distinguishes between platform-level management (cross-incubator operations like user management, incubator creation, system settings) and administration-level management (operations within a specific incubator like project management, diagnostic forms). The UI makes this distinction obvious through navigation structure, labeling, and visual cues.

**Why this priority**: Confusing platform-level and incubator-level operations leads to errors (e.g., editing the wrong incubator's settings). Clear separation prevents costly mistakes.

**Independent Test**: Log in as GlobalAdmin and verify that platform-level and administration-level sections are visually and structurally distinct in navigation.

**Acceptance Scenarios**:

1. **Given** a GlobalAdmin on the Platform dashboard, **When** they view the navigation, **Then** platform-level items (user management, incubator management, system settings) are grouped separately from context-scoped administration items.
2. **Given** an IncubatorAdmin with context selected, **When** they view the navigation, **Then** they see administration items for their incubator but not platform-level management items reserved for GlobalAdmin.
3. **Given** a GlobalAdmin who selects an incubator context, **When** they view the navigation, **Then** administration items for that incubator appear alongside persistent platform-level items, with clear visual distinction between the two.

---

### User Story 6 - Diagnostic Module End-to-End Correctness (Priority: P2)

As a ProjectCoordinator, I can manage diagnostic form templates, clone them, customize them for projects, and view responses. As an Entrepreneur, I can submit diagnostic responses within my assigned project. All diagnostic operations respect context boundaries and authorization rules.

**Why this priority**: The diagnostic module (Phase 4) is the first domain-specific feature. Its correctness validates that the foundational layers (auth, context, routing) work together under real domain logic.

**Independent Test**: Create a diagnostic form template, clone it to a project, submit a response as an Entrepreneur, and verify the entire flow completes with correct data.

**Acceptance Scenarios**:

1. **Given** a ProjectCoordinator with active project context, **When** they create a diagnostic form template, **Then** the template is saved and visible in the project's form list.
2. **Given** a ProjectCoordinator, **When** they clone a form template to another project, **Then** the cloned form appears in the target project with all questions and answer options preserved.
3. **Given** an Entrepreneur with active project context, **When** they submit a diagnostic response, **Then** the response is recorded with correct associations (project, form, user) and a confirmation is displayed.
4. **Given** a user without ProjectCoordinator or higher role, **When** they attempt to access diagnostic management pages, **Then** access is denied.
5. **Given** a ProjectCoordinator, **When** they view diagnostic responses, **Then** only responses from the active project context are shown — no cross-project data leakage.

---

### User Story 7 - Automated Regression Safety Net (Priority: P2)

As a quality stakeholder, all critical user flows (login, role routing, context selection, dashboard rendering, menu visibility, authorization boundaries, context switching, diagnostic operations) have end-to-end tests that run against deterministic seed data. These tests serve as a regression safety net before future development continues.

**Why this priority**: Without automated verification, corrections cannot be proven and regressions are inevitable during future phases. This story enforces the "prove it works" mandate.

**Independent Test**: Run the full end-to-end test suite against a freshly seeded environment and verify all tests pass.

**Acceptance Scenarios**:

1. **Given** a clean deployment with seed data, **When** the end-to-end test suite runs, **Then** all tests pass without manual intervention.
2. **Given** a test for login routing, **When** it executes for each seed user, **Then** each user lands on the correct dashboard.
3. **Given** a test for authorization boundaries, **When** it attempts unauthorized access for each role, **Then** access is correctly denied.
4. **Given** a test for context switching, **When** it switches between projects, **Then** no data leaks between contexts.

---

### User Story 8 - Deterministic Seed Data and Manual Validation Playbooks (Priority: P3)

As a tester (developer or non-developer), I have deterministic seed data representing all test personas and enough data to exercise all features. I also have step-by-step manual validation playbooks that allow me to verify each corrected behavior without needing technical knowledge.

**Why this priority**: Seed data and playbooks are the verification infrastructure. They support all other stories but don't deliver user-visible functionality on their own.

**Independent Test**: Follow a manual playbook end-to-end using the seed data and verify every step produces the documented expected result.

**Acceptance Scenarios**:

1. **Given** the seed data script, **When** it runs against an empty database, **Then** it creates all required users (GlobalAdmin, multi-role, multi-incubator, multi-project), incubators, projects, and diagnostic data without errors.
2. **Given** a manual validation playbook, **When** a non-developer follows it step-by-step, **Then** every step has clear preconditions, actions, expected results, and pass/fail criteria.
3. **Given** the seed data, **When** all end-to-end tests reference seed users and data, **Then** tests are fully deterministic and produce consistent results across runs.
4. **Given** multiple playbooks, **When** they are executed in any order, **Then** they do not interfere with each other (no shared mutable state that one playbook changes and another depends on).

---

### Edge Cases

- **User with multiple roles at different scopes**: System must correctly present role selection and scope appropriate menus after selection.
- **Context becomes invalid mid-session** (e.g., project deactivated by admin): User receives a clear message in Spanish and is redirected to context selection.
- **Deep-link to context-scoped page without context**: Redirect to context selection with preserved return URL; after context is set, user is taken to the original page.
- **Concurrent sessions with different contexts**: Each browser session maintains independent context without interference.
- **GlobalAdmin switching between global scope and specific context**: All navigation and data visibility updates correctly without stale menu items or data from the previous scope.
- **Empty state pages**: When a context has no data (no projects, no diagnostic forms), the UI shows helpful empty-state messages in Spanish rather than blank pages or errors.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST route each authenticated user to their role-specific dashboard upon login.
- **FR-002**: GlobalAdmin users MUST always see the Platform dashboard with all menu options visible across all modules.
- **FR-003**: Users with multiple roles MUST be presented with a role selection screen before dashboard routing.
- **FR-004**: Context selection MUST enforce the order: Role > Incubator > Project, where each step depends on the previous.
- **FR-005**: System MUST restrict context-scoped pages to users who have completed context selection, redirecting others to the context selection flow with a clear message in Spanish.
- **FR-006**: System MUST preserve the intended destination URL when redirecting to context selection, returning the user there after selection completes.
- **FR-007**: Context switching MUST be accessible from the top navigation bar on every authenticated page.
- **FR-008**: After context switching, the system MUST navigate the user safely: remain on the current page if valid in the new context, or fall back to the dashboard.
- **FR-009**: No data from a previous context MUST be visible or accessible after a context switch completes.
- **FR-010**: Every authorization rule MUST grant access to the target role AND all higher-privilege roles in the hierarchy (GlobalAdmin > IncubatorAdmin > ProjectCoordinator > Mentor > Entrepreneur > Sponsor).
- **FR-011**: Menu items MUST only be visible to authorized roles, and GlobalAdmin MUST appear in every menu group.
- **FR-012**: System features that depend on active context MUST handle missing context gracefully by redirecting to context selection, never by crashing or showing empty pages without explanation.
- **FR-013**: Platform-level navigation (cross-incubator management) MUST be visually and structurally separated from administration-level navigation (within-incubator management).
- **FR-014**: Diagnostic form management (create, clone, customize, view responses) MUST work end-to-end within the correct project context.
- **FR-015**: Diagnostic response submission MUST correctly associate responses with the active project, form, and user.
- **FR-016**: Diagnostic data MUST be isolated by project context — no cross-project data visibility for context-scoped roles.
- **FR-017**: End-to-end tests MUST cover: login routing, role selection, context selection, dashboard rendering, menu visibility, authorization boundaries, context switching, and diagnostic operations.
- **FR-018**: Deterministic seed data MUST include users for every role, multi-role users, multi-incubator users, multi-project users, and enough domain data to exercise all features.
- **FR-019**: Manual validation playbooks MUST be provided for each correction group with preconditions, steps, expected results, and pass/fail criteria.
- **FR-020**: Each implemented phase (1, 3, 4) MUST be independently production-ready with no broken flows, partial implementations, or missing validation.

### Key Entities *(existing — no new entities introduced)*

- **User**: Authenticated user with one or more assigned roles. Key attributes: roles, incubator assignments, project assignments.
- **RoleAssignment**: Links a user to a specific role within a specific incubator and optionally a project. Key attributes: role name (GlobalAdmin, IncubatorAdmin, ProjectCoordinator, Mentor, Entrepreneur, Sponsor), incubator, project, active status.
- **Incubator**: Top-level organizational container. Users are assigned to one or more incubators.
- **Project**: Belongs to an incubator. Context-scoped operations target a specific project.
- **Session Context**: Runtime state holding the user's active role, incubator, and project selections. Scopes all data visibility and operations.
- **FormTemplate**: Diagnostic form template with questions and answer options. Managed by ProjectCoordinators at the platform level.
- **ProjectForm**: A form template cloned into a specific project. Key attributes: project, incubator, source template. All diagnostic queries must filter by project context.
- **DiagnosticResponse**: Entrepreneur's submission against a project form, containing question responses and answer selections.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Every role-based login results in the correct dashboard appearing within 3 seconds, 100% of the time.
- **SC-002**: 100% of context-scoped pages are inaccessible without an active context — zero unguarded paths.
- **SC-003**: Context switching between projects completes with zero data leakage — no cross-context data visible at any point.
- **SC-004**: 100% of authorization attributes include all higher-privilege roles — zero authorization gaps.
- **SC-005**: All end-to-end tests pass on a clean deployment with seed data in a single run without manual intervention.
- **SC-006**: A non-developer can complete all manual validation playbooks with a 100% success rate when the system is correct.
- **SC-007**: Zero broken flows, zero partial implementations, and zero unhandled error states remain across Phases 1, 3, and 4.
- **SC-008**: Diagnostic form creation, cloning, customization, and response submission complete end-to-end with correct data persistence in under 2 minutes per operation.

## Assumptions

- The existing codebase from Phases 1, 3, and 4 is the baseline. This specification introduces no new features — only corrections, hardening, and verification of existing functionality.
- Authentication (login/logout) is already implemented and functional. This spec addresses post-authentication routing and authorization, not the authentication mechanism itself.
- The Phoenix Admin Template is the UI framework providing navigation structure, top bar, and dashboard layouts.
- An end-to-end testing framework will be configured as part of this effort.
- Seed data follows existing PostDeployment script conventions (idempotent, numbered scripts).
- The role hierarchy (GlobalAdmin > IncubatorAdmin > ProjectCoordinator > Mentor > Entrepreneur > Sponsor) is established and will not change during this effort.
- Phase 2 (if it exists between 1 and 3) is out of scope for this specification.
