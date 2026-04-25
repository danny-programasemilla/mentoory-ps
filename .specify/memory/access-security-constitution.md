<!--
  ============================================================
  SYNC IMPACT REPORT
  ============================================================
  Version: 1.1.0 (feature 018 amendment)

  Added sections:
    - 11.9 Response Indistinguishability (floor category)
    - 11.10 Outcome Audit Logging (floor category)
    - 11.11 Public-vs-Admin Attribution (floor category)
    - 11.12 Form-State Preservation (floor category)
    - 11.13 Defense-in-Depth Controls (floor category)
    - 11.14 Content-Policy Rules (floor category)
    - 13. Delivery Quality Gate (CI workflow governance)

  Sections preserved without renumbering:
    - 1-10 unchanged; 11.1-11.8 unchanged; 12 unchanged; Appendices A/B/C unchanged.

  Follow-up TODOs:
    - Configure `coverage-check` as a required status check on `develop` (DEP-005).
    - Resolve open questions for Mentor, Entrepreneur, Sponsor roles
    - Audit existing endpoints against permission matrix
    - Integrate PR template into repository workflow
  ============================================================
-->

# Mentoory Access & Security Constitution

**Version**: 1.1.0 | **Ratified**: 2026-04-08 | **Last Amended**: 2026-04-19  
**Status**: Draft — Pending Stakeholder Ratification  
**Linked from**: [constitution.md](constitution.md)

---

## 1. Executive Summary

Mentoory is a multi-tenant business incubator management platform structured around a **hierarchical role model** with **scoped tenancy**. The platform manages incubators, projects, mentoring relationships, diagnostics, and participants — all behind access boundaries that must prevent unauthorized data visibility across organizational units.

### Current Access Structure

The platform defines six roles, ordered by decreasing privilege:

1. **GlobalAdmin** — unrestricted platform-wide access
2. **IncubatorAdmin** — full access within a single incubator and its projects
3. **ProjectCoordinator** — operational access within assigned projects
4. **Mentor** — mentoring-specific access within assigned projects
5. **Entrepreneur** — participant-level access to own data within a project
6. **Sponsor** — read-only dashboard access

Access is scoped through a tree-shaped hierarchy: **Platform > Incubator > Project**. Each user's visibility is bounded by their role assignment to a specific incubator and, optionally, a specific project. No user may see data outside the branch of the hierarchy to which they are assigned.

### Major Security Goals

- **Tenant isolation**: data belonging to one incubator must never be visible to users of another incubator
- **Scope enforcement**: every action must be validated against the actor's assigned scope
- **Least privilege**: each role receives only the permissions required for its business function
- **Backend authority**: all authorization decisions are enforced at the backend; frontend visibility is never a substitute for backend checks

### Primary Risks

If access control is implemented inconsistently, the platform faces:

- **Cross-tenant data leakage** — an IncubatorAdmin seeing data from another incubator
- **Role escalation** — a lower-privilege user gaining access to higher-privilege operations (e.g., during user creation)
- **IDOR/BOLA attacks** — direct object reference manipulation to access resources outside assigned scope
- **Stale permissions** — users retaining access after role revocation or reassignment
- **Inconsistent enforcement** — features that check authorization at the UI layer but not at the backend

### Purpose of This Constitution

This document exists to ensure that **every person involved in building, reviewing, testing, or extending Mentoory** has a single authoritative reference for access control decisions. It establishes non-negotiable rules, catalogs role capabilities, defines a sustainable permission matrix, models threats, and provides a mandatory checklist for secure feature delivery.

## 2. Confirmed Model vs Inferred Model

### 2.1 Confirmed Facts (from codebase)

The following are verified facts derived from the existing codebase:

- **[CONFIRMED]** The platform defines six roles via the `PlatformRole` enum: GlobalAdmin (0), IncubatorAdmin (1), ProjectCoordinator (2), Mentor (3), Entrepreneur (4), Sponsor (5).
- **[CONFIRMED]** Authorization is enforced at the controller level using `[Authorize(Roles = "...")]` attributes. Each controller includes the target role and all higher-privilege roles (e.g., `ProjectCoordinator,IncubatorAdmin,GlobalAdmin`).
- **[CONFIRMED]** Role assignments are modeled via the `RoleAssignment` aggregate, binding a user to a specific role within a specific incubator, with an optional project scope (`UserId + IncubatorId + ProjectId? + Role + IsActive`).
- **[CONFIRMED]** A user may have multiple active `RoleAssignment` records across different incubators and roles.
- **[CONFIRMED]** A constraint enforces that a user can only hold one active Entrepreneur role per incubator.
- **[CONFIRMED]** Granular permissions are defined via a `Permission` enum organized by domain: Platform (4 permissions), Incubator (3), Project (5), Mentoring (4), Participant (3), Sponsor (1).
- **[CONFIRMED]** Role-to-permission mapping is implemented in `CheckPermissionHandler`, which grants GlobalAdmin all permissions and assigns subsets to each lower role.
- **[CONFIRMED]** Tenant isolation is partially implemented via `TenantContextMiddleware`, which extracts `ActiveIncubatorId` from claims and populates a scoped `ITenantContext` service.
- **[CONFIRMED]** Session context is managed via `ContextController`, which validates `RoleAssignment` ownership before setting claims: `ActiveRole`, `ActiveIncubatorId`, `ActiveProjectId`.
- **[CONFIRMED]** `SessionAuthenticationMiddleware` validates session tokens and checks account status on every authenticated request, with auto-signout on invalid sessions.
- **[CONFIRMED]** `PasswordResetRequiredFilter` redirects users with `AccountStatus.PasswordResetRequired` to the password change page.
- **[CONFIRMED]** User lifecycle states are managed via `AccountStatus` enum: PendingVerification, Active, Locked, Disabled, PasswordResetRequired.
- **[CONFIRMED]** Project enrollment is tracked separately from role assignment via `ProjectParticipant` (embedded in the Project aggregate): `UserId + Role + IsActive + EnrolledAtUtc`.
- **[CONFIRMED]** Platform-scoped controllers (incubator management, platform users, configuration, templates) restrict access to GlobalAdmin only.
- **[CONFIRMED]** No custom authorization policies exist. All authorization is role-based via `[Authorize]` attributes.

### 2.2 Inferred Assumptions

The following are reasonable inferences drawn from codebase patterns but not explicitly documented:

- **[INFERRED]** A user needs both a valid `RoleAssignment` (for access scope) AND a `ProjectParticipant` record (for project enrollment) to fully access project-scoped data. These are maintained as separate but related bindings.
- **[INFERRED]** The `CheckPermissionQuery` mechanism is intended for fine-grained permission checks at the handler/service level, supplementing the coarser controller-level `[Authorize]` checks. However, its usage across all handlers is not yet universal.
- **[INFERRED]** The Sponsor role is minimally defined — it has only `ViewProjectProgress` permission and appears in one controller (`SponsorController`). Its full scope and capabilities have not been formally specified.
- **[INFERRED]** `ITenantContext.CurrentIncubatorId` is expected to be used by all repository queries that return tenant-scoped data, but consistent enforcement across all query handlers has not been verified.
- **[INFERRED]** When a `RoleAssignment` is revoked (`IsActive = false`), the session claims may not be immediately invalidated. The user could retain access until their session expires or they switch context.

### 2.3 Recommended Formal Model

**[RECOMMENDED]** The platform should be formally classified as using **Hierarchical RBAC with Scoped Tenancy**:

- **Hierarchical RBAC**: Higher-privilege roles inherit the access of lower-privilege roles. GlobalAdmin inherits all; IncubatorAdmin inherits ProjectCoordinator-level access within their incubator. This is enforced by including higher roles in `[Authorize]` attributes.
- **Scoped Tenancy**: Access is not just role-based but scope-based. A role assignment is meaningless without its scope context (incubator + optional project). The same user with the same role in two different incubators has two independent access boundaries.

This is neither pure RBAC (which lacks scope), nor ABAC (which evaluates arbitrary attributes at runtime), nor pure tenancy (which lacks differentiated roles within a tenant). The combination is the platform's defining access characteristic and must be treated as such in all design decisions.

**[RECOMMENDED]** The `CheckPermissionQuery` mechanism should be promoted to a mandatory check in all command/query handlers that access scoped data, not just as a supplementary mechanism. Controller-level `[Authorize]` provides role gating; handler-level permission checks provide scope and capability validation.

## 3. Access & Security Constitution Rules

The following rules are **non-negotiable**. Any feature, endpoint, query, or UI element that violates these rules must be corrected before deployment.

1. **All functionality must be classified before implementation.** Every feature must be categorized as Platform, Incubator, or Project scope before design work begins. Unclassified features must not proceed to development.

2. **Every endpoint must define actor, action, resource, and scope.** No controller action, command handler, or query handler may exist without an explicit declaration of: which role(s) may invoke it, what operation it performs, what resource it targets, and at what scope level.

3. **Platform-category features are GlobalAdmin only.** Any functionality classified as Platform scope (incubator creation, platform configuration, global user management, subscription management, global templates) must be restricted exclusively to GlobalAdmin. No exceptions.

4. **No user may access data outside their assigned scope.** An IncubatorAdmin in Incubator A must never see data from Incubator B. A ProjectCoordinator in Project X must never see data from Project Y (even within the same incubator). A Mentor must never see data for entrepreneurs they are not assigned to.

5. **Frontend visibility never replaces backend authorization.** Hiding a menu item, button, or page from the UI is a convenience, not a security control. Every action that is hidden in the frontend must also be blocked at the backend. Backend enforcement is the sole source of truth.

6. **Role inheritance must be explicit in authorization attributes.** Every `[Authorize]` declaration must include the target role and all higher-privilege roles. A controller scoped to ProjectCoordinator must also authorize IncubatorAdmin and GlobalAdmin. Omitting a higher role is a security defect.

7. **User creation is constrained by the creator's scope.** A user can only create other users within or below their own scope. An IncubatorAdmin may create users for their own incubator only. A GlobalAdmin may create users for any scope. No role may create a user with higher privileges than its own.

8. **All high-risk actions must be auditable.** User creation, role assignment, role revocation, permission changes, account status changes, and data exports must produce audit records that identify the actor, the action, the target, and the timestamp.

9. **Permission changes must invalidate or refresh affected sessions.** When a user's role assignment is revoked, deactivated, or modified, their active session claims must be refreshed or invalidated. Stale permissions are a security vulnerability.

10. **Open questions block implementation.** If a role's permissions, a feature's scope classification, or a security boundary is marked as an open question in this constitution, that area must not be implemented until the question is formally resolved and this document is updated.

11. **Scope validation must occur at the query layer.** Every database query that returns tenant-scoped data must include an explicit scope filter (IncubatorId, ProjectId, or both). Queries without scope filters for scoped data are forbidden.

12. **Deny by default.** If a role is not explicitly granted a permission in this constitution's permission matrix, that role does not have the permission. Absence of a grant is a denial.

## 4. Domain Scope Model

The platform's authorization boundaries follow a tree-shaped hierarchy. Each scope level defines a data visibility ceiling — users at a given scope can see data at that level and below, but never laterally or upward.

### 4.1 Scope Levels

```
Platform (GlobalAdmin only)
└── Incubator (IncubatorAdmin)
    └── Project (ProjectCoordinator)
        ├── Mentor Relationships (Mentor)
        └── Entrepreneur Data (Entrepreneur)
                                (Sponsor — read-only at Project level)
```

**Platform Scope** — **[CONFIRMED]** System-wide operations: incubator creation, platform configuration, global user management, subscription management, global templates. Accessible exclusively to GlobalAdmin. No other role may access platform-scoped data or operations.

**Incubator Scope** — **[CONFIRMED]** All data and operations within a single incubator: its projects, participants, user management (within the incubator), and incubator-level configuration. IncubatorAdmin has full access here. GlobalAdmin also has full access. All other roles see only the subset of incubator data relevant to their project-level scope.

**Project Scope** — **[CONFIRMED]** All data and operations within a single project: diagnostic forms, knowledge structures, project lifecycle, participant management. ProjectCoordinator has operational access. IncubatorAdmin and GlobalAdmin also have access.

**Mentor Relationship Scope** — **[CONFIRMED]** A Mentor's visibility is narrowed beyond project scope to only the entrepreneurs they are explicitly assigned to within that project. A Mentor in Project X who is assigned to Entrepreneurs A and B must not see data for Entrepreneur C in the same project (unless also assigned). Permissions: ManageMentoringPlans, ManageSessions, ManageAssignments, CorrectAnswers, ViewProjectProgress.

**Entrepreneur Data Scope** — **[CONFIRMED]** An Entrepreneur sees only their own data within their enrolled project: their diagnostic submissions, their mentoring plan, their assignments. They must not see other entrepreneurs' data. Permissions: CompleteDiagnostic, ViewMentoringPlan, SubmitAssignments.

### 4.2 Downward Visibility

Higher-scoped roles see everything at their level and below:

- **[CONFIRMED]** GlobalAdmin sees all incubators, all projects, all participants, all data
- **[CONFIRMED]** IncubatorAdmin sees their incubator and all its projects, participants, and data
- **[CONFIRMED]** ProjectCoordinator sees their assigned project(s) and all participants within

**[CONFIRMED]** This is enforced by including higher roles in `[Authorize]` attributes and by the session context mechanism that allows GlobalAdmin to select any incubator/project context.

### 4.3 Prohibited Lateral Access

**[CONFIRMED]** No role may access data in a sibling scope:

- **[CONFIRMED]** IncubatorAdmin of Incubator A must never see data from Incubator B
- **[CONFIRMED]** ProjectCoordinator of Project X must never see data from Project Y, even if both projects are in the same incubator
- **[INFERRED]** Mentor assigned to Entrepreneur A must never see data for Entrepreneur B (unless also assigned)

Lateral access violations are among the highest-severity security defects. They are typically caused by missing scope filters in database queries.

### 4.4 Prohibited Upward Access

**[CONFIRMED]** No role may access data at a scope above its own:

- **[CONFIRMED]** IncubatorAdmin must never access platform-level configuration
- **[CONFIRMED]** ProjectCoordinator must never access incubator-level user management
- **[CONFIRMED]** Mentor and Entrepreneur must never access project administration functions

**[CONFIRMED]** Upward access is prevented by the `[Authorize(Roles)]` attribute pattern — platform controllers only include GlobalAdmin.

### 4.5 Multi-Assignment Implications

**[CONFIRMED]** A user can hold multiple `RoleAssignment` records across different incubators and roles.

- **[CONFIRMED]** A user who is IncubatorAdmin in Incubator A and Mentor in Incubator B has two independent scope boundaries. When operating in Incubator A context, they have IncubatorAdmin privileges. When in Incubator B context, they have Mentor privileges. These scopes never merge.
- **[INFERRED]** The session context mechanism (`SetActiveContextCommand`) ensures that at any given time, a user operates under exactly one role-in-scope. Context switching changes the active scope entirely — there is no "combined view" across assignments.
- **[CONFIRMED]** A user cannot hold more than one active Entrepreneur role per incubator (enforced constraint), but can hold Entrepreneur roles in different incubators.

## 5. Role Catalog

### 5.1 GlobalAdmin

**Business Purpose**: Platform owner/operator responsible for managing the entire Mentoory instance — all incubators, all users, all configuration, and all system-level operations.

**Allowed Scope**: Platform (unrestricted)

**High-Risk Permissions**: **[CONFIRMED]** All permissions. GlobalAdmin holds every permission in the Permission enum. This includes all Platform, Incubator, Project, Mentoring, Participant, and Sponsor permissions.

**User Creation Capabilities**: **[CONFIRMED]** Can create users of any role for any incubator and any project. User creation still follows standard onboarding flows (email verification, account activation).

**Operational Capabilities**:
- **[CONFIRMED]** Create, view, update, and deactivate incubators
- **[CONFIRMED]** Manage platform configuration and global templates
- **[CONFIRMED]** Manage subscription plans and assign them to incubators
- **[CONFIRMED]** Create and manage users across all incubators and projects
- **[CONFIRMED]** Access all project-level operations in any incubator (diagnostics, knowledge, mentoring)
- **[CONFIRMED]** Switch context to any incubator/project via the context selection mechanism

**Restrictions**: **[CONFIRMED]** None — GlobalAdmin has unrestricted access by design.

**Must Never**:
- **[RECOMMENDED]** Must never be assignable by any role other than another GlobalAdmin
- **[RECOMMENDED]** Must never have its access silently downgraded or scoped without explicit session context selection
- **[CONFIRMED]** Must never be excluded from any `[Authorize]` attribute or menu group (Constitution Principle X)

---

### 5.2 IncubatorAdmin

**Business Purpose**: Manages a single incubator — its projects, users, and operational configuration. Acts as the "local administrator" for an organizational unit within the platform.

**Allowed Scope**: Incubator (own incubator and all projects within it)

**High-Risk Permissions**: **[CONFIRMED]** ManageProjects, ManageIncubatorUsers, EnrollParticipants, ManageDiagnostics, ManageKnowledge, ManageLifecycle, ManageProjectParticipants, ViewProjectProgress.

**User Creation Capabilities**: **[CONFIRMED]** Can create the following users within their own incubator scope:
- IncubatorAdmin (for the same incubator)
- ProjectCoordinator (for projects in the same incubator)
- Mentor (for projects in the same incubator)
- Entrepreneur (for projects in the same incubator)

**Operational Capabilities**:
- **[CONFIRMED]** View and manage all projects within their incubator
- **[CONFIRMED]** Manage diagnostic forms for any project in their incubator
- **[CONFIRMED]** Manage knowledge structures and project lifecycle
- **[CONFIRMED]** Enroll participants and manage project participants
- **[CONFIRMED]** View all user profiles within their incubator
- **[CONFIRMED]** Batch upload users for their incubator

**Restrictions**:
- **[CONFIRMED]** Cannot access data from other incubators
- **[CONFIRMED]** Cannot access platform-level features (configuration, incubator creation, subscriptions, global templates)
- **[CONFIRMED]** Cannot create GlobalAdmin users

**Must Never**:
- **[CONFIRMED]** Must never see, query, or export data belonging to another incubator
- **[CONFIRMED]** Must never create a user with a scope outside their own incubator
- **[CONFIRMED]** Must never access platform configuration or subscription management
- **[CONFIRMED]** Must never create a GlobalAdmin role assignment

---

### 5.3 ProjectCoordinator

**Business Purpose**: Manages the day-to-day operations of assigned projects — diagnostic forms, knowledge structures, mentoring plans, and project lifecycle. Acts as the "project manager" within the incubation process.

**Allowed Scope**: Project (assigned project(s) within their incubator)

**High-Risk Permissions**: **[CONFIRMED]** ManageDiagnostics, ManageKnowledge, ManageLifecycle, ManageProjectParticipants, ViewProjectProgress.

**User Creation Capabilities**: **[OPEN]** It is not confirmed whether ProjectCoordinator can directly create users (e.g., enroll entrepreneurs) or only manage existing assignments made by IncubatorAdmin. See Open Questions (Section 12).

**Operational Capabilities**:
- **[CONFIRMED]** Define, edit, and manage diagnostic forms for assigned projects
- **[CONFIRMED]** Manage knowledge structures within assigned projects
- **[CONFIRMED]** Manage project lifecycle (stages, transitions)
- **[CONFIRMED]** Manage project participants (assign mentors to project, assign mentors to entrepreneurs)
- **[CONFIRMED]** View project progress and participant data within assigned projects
- **[CONFIRMED]** One entrepreneur can have more than one mentor assigned

**Restrictions**:
- **[CONFIRMED]** Cannot access projects they are not assigned to (even within the same incubator)
- **[CONFIRMED]** Cannot access incubator-level user management
- **[CONFIRMED]** Cannot access platform-level features
- **[CONFIRMED]** Cannot create incubators or manage incubator configuration

**Must Never**:
- **[CONFIRMED]** Must never see or modify data from projects they are not assigned to
- **[CONFIRMED]** Must never perform incubator-level operations (user management across projects)
- **[CONFIRMED]** Must never access platform configuration, subscriptions, or global templates
- **[RECOMMENDED]** Must never create users with a role higher than their own

---

### 5.4 Mentor

**Business Purpose**: Provides mentoring services to assigned entrepreneurs within specific projects. Conducts sessions, reviews assignments, and guides entrepreneurs through the incubation process.

**Allowed Scope**: Project (assigned project(s)), narrowed to assigned entrepreneur relationships

**High-Risk Permissions**: **[CONFIRMED]** ManageMentoringPlans, ManageSessions, ManageAssignments, CorrectAnswers, ViewProjectProgress.

**User Creation Capabilities**: **[CONFIRMED]** None. Mentors do not create users.

**Operational Capabilities**:
- **[CONFIRMED]** Manage mentoring plans for assigned entrepreneurs
- **[CONFIRMED]** Create and manage mentoring sessions
- **[CONFIRMED]** Manage and review assignments
- **[CONFIRMED]** Correct diagnostic answers for assigned entrepreneurs
- **[CONFIRMED]** View project progress

**Restrictions**:
- **[INFERRED]** Can only see data for entrepreneurs they are explicitly assigned to within a project
- **[CONFIRMED]** Cannot manage diagnostic forms, knowledge structures, or project lifecycle
- **[CONFIRMED]** Cannot manage project participants or assign other mentors
- **[CONFIRMED]** Cannot access incubator or platform operations

**Must Never**:
- **[INFERRED]** Must never see data for entrepreneurs they are not assigned to
- **[CONFIRMED]** Must never modify diagnostic form definitions (only correct answers)
- **[CONFIRMED]** Must never manage project participants or lifecycle stages
- **[CONFIRMED]** Must never access data outside assigned project(s)

**Open Questions**:
- **[OPEN]** Can a Mentor see a list of all entrepreneurs in a project, or only those assigned to them?
- **[OPEN]** Can a Mentor view project-level aggregate progress, or only per-entrepreneur progress?
- **[OPEN]** What data does "ViewProjectProgress" expose to a Mentor — full project dashboards or filtered to their assigned entrepreneurs?
- **[OPEN]** Can a Mentor be assigned to multiple projects across different incubators simultaneously?

---

### 5.5 Entrepreneur

**Business Purpose**: A program participant enrolled in an incubation project. Completes diagnostics, follows mentoring plans, submits assignments, and progresses through the incubation lifecycle.

**Allowed Scope**: Project (enrolled project), narrowed to own data only

**High-Risk Permissions**: **[CONFIRMED]** CompleteDiagnostic, ViewMentoringPlan, SubmitAssignments.

**User Creation Capabilities**: **[CONFIRMED]** None. Entrepreneurs do not create users.

**Operational Capabilities**:
- **[CONFIRMED]** Complete diagnostic forms assigned to them
- **[CONFIRMED]** View their own mentoring plan
- **[CONFIRMED]** Submit assignments

**Restrictions**:
- **[INFERRED]** Can only see their own data (diagnostics, mentoring plan, assignments)
- **[CONFIRMED]** Cannot manage diagnostic forms, knowledge structures, or project settings
- **[CONFIRMED]** Cannot assign mentors or manage other participants
- **[CONFIRMED]** Cannot access incubator or platform operations
- **[CONFIRMED]** Limited to one active Entrepreneur role per incubator

**Must Never**:
- **[INFERRED]** Must never see another entrepreneur's diagnostic results, mentoring plan, or assignments
- **[CONFIRMED]** Must never modify diagnostic form definitions
- **[CONFIRMED]** Must never access project administration functions
- **[CONFIRMED]** Must never access data outside their enrolled project

**Open Questions**:
- **[OPEN]** Can an Entrepreneur see the profiles of their assigned mentor(s)?
- **[OPEN]** Can an Entrepreneur see any project-level information (e.g., project name, stage, general announcements)?
- **[OPEN]** What happens when an Entrepreneur is enrolled in multiple projects within the same incubator — can they switch between projects?
- **[OPEN]** Can an Entrepreneur view historical data from completed diagnostic cycles or only current/active ones?

---

### 5.6 Sponsor

**Business Purpose**: External stakeholder (e.g., investor, government agency, corporate partner) who monitors incubation progress through read-only dashboards. Does not participate in operations.

**Allowed Scope**: **[INFERRED]** Project-level read-only access (based on `ViewProjectProgress` permission and `SponsorController` authorization)

**High-Risk Permissions**: **[CONFIRMED]** ViewProjectProgress only.

**User Creation Capabilities**: **[CONFIRMED]** None. Sponsors do not create users.

**Operational Capabilities**:
- **[CONFIRMED]** View project progress dashboards

**Restrictions**:
- **[INFERRED]** Read-only access — cannot modify any data
- **[CONFIRMED]** Cannot access diagnostic forms, knowledge structures, mentoring, or project lifecycle management
- **[CONFIRMED]** Cannot access incubator or platform operations

**Must Never**:
- **[INFERRED]** Must never modify any data in the system
- **[INFERRED]** Must never access individual entrepreneur data (only aggregate/anonymized progress)
- **[CONFIRMED]** Must never access project administration or participant management
- **[CONFIRMED]** Must never access data outside their authorized scope

**Open Questions**:
- **[OPEN]** At what scope does Sponsor access operate — per-project, per-incubator, or a custom set of projects?
- **[OPEN]** What data granularity does the Sponsor dashboard expose — aggregate metrics only, or individual participant progress?
- **[OPEN]** Can a Sponsor see entrepreneur names/identities, or only anonymized data?
- **[OPEN]** How is a Sponsor's access provisioned — via RoleAssignment to a specific incubator/project, or a separate mechanism?

---

### 5.7 Glossary / Terminology

| Term | Definition | Notes |
|------|-----------|-------|
| **GlobalAdmin** | Platform-wide administrator with unrestricted access | Highest privilege role |
| **IncubatorAdmin** | Administrator scoped to a single incubator and its projects | Cannot access other incubators |
| **ProjectCoordinator** | Operational manager of assigned projects within an incubator | Canonical term; "Project Admin" is a **deprecated alias** from early business requirements — do not use in code, specs, or documentation |
| **Mentor** | Mentoring professional assigned to specific projects and entrepreneurs | Access limited to assigned relationships |
| **Entrepreneur** | Program participant enrolled in a project | Access limited to own data within enrolled project |
| **Sponsor** | External stakeholder with read-only dashboard access | Minimally defined; see Open Questions |
| **Scope** | Hierarchical access boundary: Platform > Incubator > Project | Determines data visibility ceiling |
| **RoleAssignment** | Binding of a user to a role within a specific incubator (and optionally a project) | Primary access control record |
| **ProjectParticipant** | Enrollment record binding a user to a project with a participant role | Separate from RoleAssignment; tracks project-level enrollment |
| **Permission** | Granular capability granted to a role (e.g., ManageDiagnostics) | Defined in Permission enum; mapped to roles in CheckPermissionHandler |
| **Permission Matrix** | Structured table mapping actions to roles, scopes, and constraints | Section 6 of this document; the authoritative permission reference |
| **Tenant** | An incubator and all its contained projects, participants, and data | The primary isolation boundary |
| **Context** | The active incubator and optional project selected by the user for their current session | Stored in session claims; determines query scope |

## 6. Sustainable Permission Matrix

### 6.1 Matrix Format

The permission matrix uses a tabular format with one row per action. This format is sustainable because:

- **Extensible**: New actions are added as new rows without restructuring
- **Searchable**: Each row is self-contained — find an action and immediately see all role grants
- **Reviewable**: During code review, compare the implemented authorization against the matrix row
- **Auditable**: Each row declares audit requirements, making compliance verification systematic

**Column definitions**:
- **Action**: Verb-noun capability being authorized
- **Resource**: The entity or data being acted upon
- **Platform?**: Whether this is a platform-level action (GlobalAdmin exclusive)
- **GA/IA/PC/M/E/S**: Access level per role — FULL (create/read/update/delete), SCOPED (within assigned scope), READ (view only), NONE (denied)
- **Scope**: The scope level at which this action operates
- **Constraints**: Additional conditions beyond role and scope
- **Audit**: REQUIRED (must log), RECOMMENDED (should log), NONE (no audit needed)
- **Notes**: Open questions or clarifications

### 6.2 Initial Permission Matrix

| Action | Resource | Platform? | GA | IA | PC | M | E | S | Scope | Constraints | Audit | Notes |
|--------|----------|:---------:|:--:|:--:|:--:|:-:|:-:|:-:|-------|-------------|:-----:|-------|
| Create Incubator | Incubator | Yes | FULL | NONE | NONE | NONE | NONE | NONE | Platform | — | REQUIRED | GlobalAdmin exclusive |
| Create User | User | No | FULL | SCOPED | NONE | NONE | NONE | NONE | Incubator | Creator scope must contain target scope; cannot create higher-privilege role | REQUIRED | **[OPEN]** Can PC create entrepreneurs? |
| Assign Role | RoleAssignment | No | FULL | SCOPED | NONE | NONE | NONE | NONE | Incubator | Cannot assign role higher than own; target must be within own scope | REQUIRED | |
| Assign User to Incubator | RoleAssignment | No | FULL | SCOPED | NONE | NONE | NONE | NONE | Incubator | IA can only assign to own incubator | REQUIRED | |
| Assign User to Project | RoleAssignment | No | FULL | SCOPED | SCOPED | NONE | NONE | NONE | Project | PC can assign within own project(s) only | REQUIRED | **[OPEN]** Confirm PC can assign |
| View Incubator | Incubator | No | FULL | SCOPED | NONE | NONE | NONE | NONE | Incubator | IA sees own incubator only | NONE | |
| Update Incubator | Incubator | No | FULL | SCOPED | NONE | NONE | NONE | NONE | Incubator | IA can update own incubator only | REQUIRED | |
| Create Project | Project | No | FULL | SCOPED | NONE | NONE | NONE | NONE | Incubator | **[OPEN]** Can IA create projects? | REQUIRED | See Open Questions |
| View Project | Project | No | FULL | SCOPED | SCOPED | READ | READ | READ | Project | M/E see own project; S sees authorized projects | NONE | |
| Update Project | Project | No | FULL | SCOPED | SCOPED | NONE | NONE | NONE | Project | PC can update own assigned project(s) | REQUIRED | |
| Create Diagnostic Form | DiagnosticForm | No | FULL | SCOPED | SCOPED | NONE | NONE | NONE | Project | IA and PC both can create within scope | REQUIRED | |
| Edit Diagnostic Form | DiagnosticForm | No | FULL | SCOPED | SCOPED | NONE | NONE | NONE | Project | Only within assigned project scope | RECOMMENDED | |
| Publish Diagnostic Form | DiagnosticForm | No | FULL | SCOPED | SCOPED | NONE | NONE | NONE | Project | Publishing makes form available to entrepreneurs | REQUIRED | |
| Assign Mentor to Project | MentorAssignment | No | FULL | SCOPED | SCOPED | NONE | NONE | NONE | Project | PC manages mentor assignments in own project | REQUIRED | |
| Assign Mentor to Entrepreneur | MentorAssignment | No | FULL | SCOPED | SCOPED | NONE | NONE | NONE | Project | One entrepreneur can have multiple mentors | REQUIRED | |
| View Entrepreneur Data | EntrepreneurData | No | FULL | SCOPED | SCOPED | SCOPED | SCOPED | NONE | Project | M sees only assigned entrepreneurs; E sees only own data | RECOMMENDED | **[OPEN]** Sponsor access? |
| Manage Platform Settings | Configuration | Yes | FULL | NONE | NONE | NONE | NONE | NONE | Platform | — | REQUIRED | GlobalAdmin exclusive |
| Batch Upload Users | User | No | FULL | SCOPED | SCOPED | NONE | NONE | NONE | Incubator | GA: unrestricted. IA: own incubator. PC: assigned projects only | REQUIRED | |
| View User Profile | UserProfile | No | FULL | SCOPED | SCOPED | READ | READ | NONE | Incubator | PC/M/E see profiles within project scope | NONE | |
| Manage Templates | Template | Yes | FULL | NONE | NONE | NONE | NONE | NONE | Platform | — | REQUIRED | GlobalAdmin exclusive |
| Manage Subscriptions | Subscription | Yes | FULL | NONE | NONE | NONE | NONE | NONE | Platform | — | REQUIRED | GlobalAdmin exclusive |
| Correct Diagnostic Answers | DiagnosticAnswer | No | FULL | SCOPED | SCOPED | SCOPED | NONE | NONE | Project | M corrects only for assigned entrepreneurs | RECOMMENDED | |
| Complete Diagnostic | DiagnosticSubmission | No | FULL | NONE | NONE | NONE | SCOPED | NONE | Project | E completes own diagnostics only | RECOMMENDED | |

**Legend**: GA = GlobalAdmin, IA = IncubatorAdmin, PC = ProjectCoordinator, M = Mentor, E = Entrepreneur, S = Sponsor

## 7. Security Design Principles

### 7.1 Least Privilege

Every role receives only the permissions required for its business function. Mentor does not get ProjectCoordinator capabilities. Entrepreneur does not get Mentor capabilities. Permissions are granted explicitly in the permission matrix; everything else is denied.

**In this platform**: The `CheckPermissionHandler` maps each role to a specific permission set. This mapping is the authoritative source. If a role does not appear in the mapping for a permission, it does not have that permission.

### 7.2 Deny by Default

If a role is not explicitly granted access, access is denied. There is no "default allow" state. New features start with no role having access; permissions are added deliberately.

**In this platform**: Constitution Rule 12 codifies this. The permission matrix uses NONE as the default; FULL, SCOPED, and READ are explicit grants.

### 7.3 Explicit Scope Validation

Every operation that accesses scoped data must explicitly validate that the actor's scope includes the target resource. Scope is not implied by authentication alone — a valid session does not mean access to all data.

**In this platform**: `ITenantContext.CurrentIncubatorId` must be used in every repository query for tenant-scoped data. Handlers must verify that the requested resource belongs to the active incubator/project context.

### 7.4 Backend Enforcement as Source of Truth

Authorization decisions are made at the backend. Frontend role checks are a UX convenience, not a security mechanism. Every action that is hidden in the UI must be independently blocked at the controller/handler level.

**In this platform**: `[Authorize(Roles)]` at the controller level, `CheckPermissionQuery` at the handler level, and scope filtering at the query level form a three-layer defense. Frontend menu configuration supplements but never replaces these checks.

### 7.5 Defense in Depth

Multiple independent layers must enforce authorization. If one layer fails, others prevent unauthorized access:

1. **Controller layer**: `[Authorize(Roles)]` — blocks users without the required role
2. **Handler layer**: `CheckPermissionQuery` — validates granular permission for the action
3. **Query layer**: `ITenantContext` scope filters — ensures queries only return data within the active scope
4. **Database layer**: foreign key relationships (RoleAssignment.IncubatorId, Project.IncubatorId) establish structural scope boundaries

### 7.6 Separation of Duties

Critical operations should require different actors or elevated verification:

- User creation and role assignment should be performed by administrators, not by the users themselves
- GlobalAdmin creation should require existing GlobalAdmin authorization
- Platform configuration changes should be restricted to a single role (GlobalAdmin)

**In this platform**: Constitution Rule 7 enforces that user creation is constrained by the creator's scope. No role can create a user with higher privileges than its own.

### 7.7 Auditability

All high-risk actions must produce tamper-resistant audit records. The system must be able to answer: "who did what, to whom, and when?"

**In this platform**: The permission matrix (Section 6) marks each action's audit requirement. REQUIRED actions must log: actor identity (UserId, Role), action performed, target resource (ExternalId), and timestamp. Audit records must be immutable — actors cannot delete their own audit trail.

### 7.8 Secure Onboarding / Identity Verification

User accounts must go through a verification process before gaining operational access:

- Email verification before account activation
- Password must be set by the user (not a default password left in place)
- Account status must progress through: PendingVerification -> Active

**In this platform**: The `AccountStatus` enum tracks lifecycle states. `PasswordResetRequiredFilter` forces password change on first login for batch-created users. `SessionAuthenticationMiddleware` validates account status on every request.

### 7.9 Secure Role Assignment and Reassignment

Role assignments must be validated at creation time and their effects must be immediate:

- The assigner must have sufficient privileges to create the target role
- The target scope must be within the assigner's scope
- Role assignment changes should refresh or invalidate affected sessions

**In this platform**: `RoleAssignment` aggregate validates constraints at creation. Constitution Rule 9 mandates session refresh on permission changes.

### 7.10 Secure Deprovisioning / Offboarding

When a user's access is removed, the removal must take effect completely and promptly:

- Revoking a `RoleAssignment` must set `IsActive = false`
- Deactivating a user account must set `AccountStatus = Disabled`
- Active sessions for the affected user must be invalidated or refreshed
- The user must not retain access through cached claims, stale sessions, or background job contexts

**In this platform**: `SessionAuthenticationMiddleware` checks `AccountStatus` on every request and auto-signs out disabled users. However, session claim invalidation on RoleAssignment revocation may require additional implementation (see Threat 9 in Section 8).

## 8. Threat and Failure Analysis

### Threat 1: Broken Access Control

**Why it matters here**: The platform has six roles across three scope levels. Missing or incorrect `[Authorize]` attributes on a single controller can expose entire functional areas to unauthorized roles.

**Typical implementation mistakes**:
- Forgetting to add `[Authorize]` on a new controller or action method
- Including the target role but omitting higher-privilege roles (e.g., `[Authorize(Roles = "ProjectCoordinator")]` without IncubatorAdmin and GlobalAdmin)
- Adding authorization at the controller level but not validating scope in the handler

**Required safeguards**:
- Constitution Rule 6: role inheritance must be explicit in all authorize attributes
- Code review must verify authorize attribute completeness (Section 10 checklist, item 6)
- Automated test: every controller action must have an `[Authorize]` attribute (CI check)

### Threat 2: IDOR / BOLA (Insecure Direct Object Reference / Broken Object-Level Authorization)

**Why it matters here**: The platform uses `ExternalId` (Guid) for all external-facing entity references. An attacker who discovers or guesses an ExternalId from another incubator could attempt to access that resource directly.

**Typical implementation mistakes**:
- Accepting an ExternalId in a request and loading the entity without verifying it belongs to the actor's active scope
- Using internal database IDs in URLs or API responses (exposing sequential identifiers)
- Trusting the frontend to only send IDs for authorized resources

**Required safeguards**:
- Every handler that loads an entity by ExternalId must verify the entity's IncubatorId/ProjectId matches the active context
- Never expose internal database IDs in routes, responses, or client-side code
- Cross-scope isolation tests (Section 11.3) specifically target this threat

### Threat 3: Trusting Frontend Role Filtering

**Why it matters here**: The platform uses menu configuration and UI visibility rules to show/hide features based on role. If backend enforcement is missing, a user can bypass the UI and call endpoints directly.

**Typical implementation mistakes**:
- Hiding a menu item for a role but not adding `[Authorize]` to the corresponding controller
- Using client-side JavaScript to filter data instead of server-side scope enforcement
- Showing/hiding form fields based on role without backend validation of submitted data

**Required safeguards**:
- Constitution Rule 5: frontend visibility never replaces backend authorization
- UI hiding vs backend enforcement tests (Section 11.7)
- Code review must verify that every UI-hidden action has a corresponding backend check

### Threat 4: Incorrect Scope Joins in Database Queries

**Why it matters here**: Queries that retrieve tenant-scoped data must include an explicit IncubatorId (and/or ProjectId) filter. A missing filter returns data from all incubators.

**Typical implementation mistakes**:
- Writing a LINQ query like `dbContext.Projects.Where(p => p.Name == name)` without filtering by IncubatorId
- Using `ITenantContext` for some queries but not others
- Forgetting to scope subqueries (e.g., loading a project's participants without verifying the project belongs to the active incubator)

**Required safeguards**:
- Constitution Rule 11: scope validation must occur at the query layer
- `ITenantContext.CurrentIncubatorId` must be applied in every repository method that returns scoped data
- Code review must verify every database query includes appropriate scope filters

### Threat 5: Role Escalation During User Creation or Assignment

**Why it matters here**: IncubatorAdmin can create users and assign roles. If the system does not validate that the created role is within the creator's authority, an IncubatorAdmin could create a GlobalAdmin.

**Typical implementation mistakes**:
- Not validating the target role against the creator's role (allowing upward escalation)
- Not validating the target scope against the creator's scope (allowing cross-incubator assignment)
- Allowing batch import to bypass role validation checks

**Required safeguards**:
- Constitution Rule 7: user creation is constrained by the creator's scope
- Validation in the CreateUser/AssignRole command handler must verify: `targetRole <= creatorRole` and `targetScope ⊆ creatorScope`
- Batch upload flows must apply the same validation as individual user creation

### Threat 6: Cross-Incubator Data Leakage

**Why it matters here**: This is the most severe tenant isolation failure. If an IncubatorAdmin or any user in Incubator A can see data from Incubator B, the multi-tenant model is fundamentally broken.

**Typical implementation mistakes**:
- Missing `ITenantContext` filter in a new repository method
- A report or export endpoint that aggregates data across all incubators without scope filtering
- A search feature that queries globally instead of within the active incubator context
- Background jobs or notifications that process data without tenant scoping

**Required safeguards**:
- `ITenantContext.CurrentIncubatorId` is mandatory for every query that returns tenant data
- Cross-incubator isolation tests (Section 11.3) must exist for every data access endpoint
- Exports, reports, and search must apply the same scope filters as regular queries

### Threat 7: Cross-Project Data Leakage

**Why it matters here**: Within an incubator, ProjectCoordinator should only see their assigned projects. A coordinator assigned to Project X must not see data from Project Y, even though both are in the same incubator.

**Typical implementation mistakes**:
- Filtering by IncubatorId but not by ProjectId for project-scoped queries
- Loading all projects in an incubator and filtering in the UI instead of the query
- Not verifying project assignment when loading project-specific resources (diagnostic forms, participants)

**Required safeguards**:
- Handlers for project-scoped operations must verify the actor has a valid RoleAssignment or ProjectParticipant for the target project
- Queries must include ProjectId filter for project-scoped data
- Cross-project isolation tests (Section 11.3)

### Threat 8: Mentor Seeing Unauthorized Entrepreneur Data

**Why it matters here**: Mentors are scoped to their assigned entrepreneurs within a project. A mentor assigned to Entrepreneurs A and B must not see Entrepreneur C's diagnostic results, mentoring plan, or assignments.

**Typical implementation mistakes**:
- Loading all entrepreneurs in a project and displaying them to the mentor
- Not filtering by MentorAssignment when retrieving entrepreneur data
- Showing aggregate project progress that includes data from unassigned entrepreneurs

**Required safeguards**:
- Every mentor-facing query must join through the MentorAssignment relationship to filter to assigned entrepreneurs only
- Mentor-facing project progress views must be filtered or anonymized to exclude unassigned entrepreneurs
- Cross-entrepreneur isolation tests specific to the mentor role

### Threat 9: Stale Permissions After Reassignment

**Why it matters here**: When a user's RoleAssignment is revoked or changed, their session may still contain the old role and scope claims. Until the session is refreshed, they retain access they should no longer have.

**Typical implementation mistakes**:
- Revoking a RoleAssignment in the database but not invalidating the user's session
- Relying on session expiration (which could be hours) instead of immediate invalidation
- Not refreshing claims when the active context changes

**Required safeguards**:
- Constitution Rule 9: permission changes must invalidate or refresh affected sessions
- `SessionAuthenticationMiddleware` should check for role assignment changes (e.g., via a version counter or last-modified timestamp)
- Assignment change tests (Section 11.5) must verify that revoked access is immediately denied

### Threat 10: Soft-Deleted or Archived Entity Leakage

**Why it matters here**: Incubators, projects, users, and RoleAssignments all use `IsActive` flags instead of hard deletes. If queries do not filter by `IsActive`, deactivated entities appear in results, potentially exposing historical data.

**Typical implementation mistakes**:
- Writing queries without `IsActive == true` filter
- Loading related entities (navigation properties) without checking their `IsActive` status
- Showing deactivated users in participant lists or search results
- Allowing context selection for deactivated incubators or projects

**Required safeguards**:
- Repository methods must include `IsActive == true` as a default filter for all entity queries
- Global query filters in EF Core configuration should be considered for all entities with `IsActive`
- Deactivated entities must be excluded from search, reports, exports, and context selection

### Threat 11: Unauthorized Access via Secondary Data Paths

**Why it matters here**: Authorization is typically enforced on primary CRUD endpoints. But data can also be accessed through exports, reports, notifications, search results, and background jobs — and these secondary paths may bypass primary authorization checks.

**Typical implementation mistakes**:
- An export endpoint that dumps all data without applying the same scope filters as the source view
- A notification system that sends data from Incubator A to a user who has since been reassigned to Incubator B
- A search feature that returns results across all incubators because it queries a global index
- A background job that processes data without tenant context (no `ITenantContext` available in non-HTTP contexts)

**Required safeguards**:
- Every data export, report, and search feature must apply the same scope filters as the corresponding primary data access
- Notification dispatch must verify the recipient still has access to the scoped data at send time
- Background jobs must establish tenant context before accessing scoped data
- Secondary data path authorization tests should be included in the test suite

## 9. Enforcement Model Recommendations

The following recommendations describe how authorization should be enforced across the stack. They are implementation-aware but not framework-specific — they describe patterns, not exact code.

### 9.1 Backend Authorization Architecture

Authorization should be enforced at three independent layers:

1. **Controller layer**: Role-based gating via `[Authorize(Roles)]` — prevents users without the required role from reaching the handler
2. **Handler layer**: Permission and scope validation via `CheckPermissionQuery` — validates the actor has the specific capability for the specific resource in the specific scope
3. **Query layer**: Implicit scope filtering via `ITenantContext` — ensures database queries never return data outside the active scope

Each layer must be able to deny access independently. A failure at one layer must not be compensated by another.

### 9.2 Route / Endpoint Guards

- Every controller must have an `[Authorize]` attribute specifying the permitted roles
- Role lists must include the target role and all higher-privilege roles (Principle X)
- Controllers without `[Authorize]` must be explicitly justified (only for truly public endpoints, if any)
- Area-based organization (Platform, Administration, Coordination, Participant) should align with scope levels

### 9.3 Service-Layer Policy Checks

- Handlers that perform scoped operations should issue a `CheckPermissionQuery` before executing the operation
- Permission checks should validate: (1) the actor has the required permission, (2) the actor has an active RoleAssignment for the target scope
- Permission denials should return a clear error without leaking information about the resource

### 9.4 Query-Layer Scope Enforcement

- Every repository method that returns tenant-scoped data must accept or inject the active scope (IncubatorId, ProjectId)
- `ITenantContext.CurrentIncubatorId` should be the standard mechanism for injecting scope into queries
- For project-scoped data, both IncubatorId and ProjectId must be validated
- For mentor-scoped data, the query must join through MentorAssignment to filter to assigned entrepreneurs

### 9.5 Database Considerations

- All tenant-scoped tables should include an IncubatorId column (directly or through a parent entity's foreign key)
- Consider row-level security policies as a defense-in-depth measure for critical tables
- Indexes on (IncubatorId, ...) and (ProjectId, ...) should support efficient scoped queries
- Soft-delete columns (IsActive) should be included in filtered indexes to avoid returning deactivated entities

### 9.6 Token / Session Claims

The platform uses claims-based session context. The following claims are critical:

- `ActiveRole`: The role the user is currently operating under
- `ActiveIncubatorId`: The incubator the user is currently operating in
- `ActiveProjectId`: The project the user is currently operating in (optional)
- `ClaimTypes.Role`: The ASP.NET role claim used by `[Authorize]`

Claims must be refreshed when: (1) the user switches context, (2) a RoleAssignment is created/revoked, (3) the user's AccountStatus changes.

### 9.7 Role-to-Scope Resolution

When a user authenticates, the system must resolve their available contexts:

1. Load all active `RoleAssignment` records for the user
2. Present available contexts (incubator + optional project) for selection
3. Set session claims based on the selected context
4. GlobalAdmin users may select any incubator/project context

The `SetActiveContextCommand` must validate that the selected RoleAssignment belongs to the authenticated user and is active.

### 9.8 Validation for User Creation and Assignment Flows

User creation and role assignment are high-risk operations. The following validations are mandatory:

- **Creator authority**: The creator's role must be >= the target role (no upward escalation)
- **Scope containment**: The target scope must be within the creator's scope (IncubatorAdmin cannot create users for another incubator)
- **Role validity**: The target role must be a valid PlatformRole value
- **Uniqueness constraints**: Enforce one active Entrepreneur per user per incubator
- **Onboarding flow**: New users must go through email verification and password setup regardless of how they are created (individual or batch)

### 9.9 Audit Logs

- Audit records must be stored in a separate, append-only data store or table
- Required fields: ActorId, ActorRole, Action, TargetResourceType, TargetResourceId, Timestamp, Outcome (success/failure)
- Failed authorization attempts should be logged with the denied action and the actor's claimed role/scope
- Audit data must be retained according to the platform's data retention policy
- Audit records must not be modifiable by the actor who triggered them

### 9.10 Testing Strategy

Authorization tests should be structured as outlined in Section 11. At minimum:

- Each permission matrix row should have corresponding positive and negative tests
- Cross-scope isolation tests should exist for every scope boundary (cross-incubator, cross-project, cross-entrepreneur)
- Assignment change tests should verify immediate effect of role revocation
- A dedicated authorization test suite should run on every build as a regression gate

### 9.11 Admin Action Traceability

All GlobalAdmin actions must be traceable:

- Every platform-level operation (incubator creation, global configuration change, subscription modification) must produce an audit record
- GlobalAdmin operating in another incubator's context should be logged with both their identity and the target context
- Impersonation (if implemented) must produce a separate, clearly distinguishable audit trail

## 10. Secure Feature Design Workflow

### 10.1 Mandatory Access Checklist

Every feature, endpoint, or UI element must pass through this checklist **before** it is submitted for code review. This checklist is a **delivery gate** — incomplete checklists must be rejected during review.

**For planned features** (going through SpecKit pipeline): This checklist is auto-generated via `/speckit.checklist` and tracked in the feature's `checklists/` directory.

**For ad-hoc changes** (direct PRs): The PR template includes an access section (see `.github/PULL_REQUEST_TEMPLATE.md`) that mirrors these questions. Reviewers reject PRs with incomplete access sections.

#### Checklist Questions

1. **What business capability is being added or modified?**
   - Describe the operation in business terms (e.g., "allow project coordinators to publish diagnostic forms").

2. **Which role(s) may perform this action?**
   - List every role that should have access. Consult the Permission Matrix (Section 6).
   - Remember: higher roles inherit lower-role access. If ProjectCoordinator can do it, IncubatorAdmin and GlobalAdmin can too.

3. **What scope applies?**
   - Platform, Incubator, or Project?
   - If Incubator or Project: what scope filter ensures data isolation?

4. **Does this feature belong to the Platform, Incubator, or Project category?**
   - Platform = GlobalAdmin only (Rule 3).
   - Incubator = scoped to one incubator.
   - Project = scoped to one project within an incubator.

5. **Can this action affect users, permissions, or data visibility?**
   - If yes: what safeguards prevent escalation, leakage, or unauthorized modification?
   - User creation, role assignment, and participant enrollment require additional validation (Rule 7).

6. **What backend checks enforce authorization?**
   - Specify: `[Authorize(Roles = "...")]` on the controller, CheckPermissionQuery in the handler, ITenantContext scope filtering in queries.
   - Rule 5: frontend hiding alone is never sufficient.

7. **What audit events are required?**
   - Consult the Permission Matrix "Audit" column (Section 6).
   - REQUIRED actions must produce audit records with actor, action, target, and timestamp.

8. **What tests prove the security boundaries hold?**
   - At minimum: one positive test (authorized user succeeds) and one negative test (unauthorized user is denied).
   - For scoped actions: a cross-scope isolation test (user in Incubator A cannot access Incubator B data).

### 10.2 Enforcement Mechanisms

| Workflow | Enforcement Point | Tool |
|----------|------------------|------|
| Planned features (SpecKit) | `/speckit.checklist` generates access checklist | SpecKit pipeline |
| Ad-hoc changes (PRs) | PR template with required access section | GitHub PR template |
| Code review | Reviewer verifies checklist completeness | Manual review gate |
| CI/CD | Authorization test suite must pass | Automated test gate |

### 10.3 Reviewer Responsibilities

Code reviewers must verify:
- Every `[Authorize]` attribute includes the correct role list (target + all higher roles)
- Every handler that accesses scoped data uses `ITenantContext` or explicit scope filters
- The access checklist is complete and accurate for the feature being reviewed
- No endpoint relies solely on frontend visibility for access control

## 11. Testing and Verification Requirements

Every feature that involves authorization, scoped data, or role-gated operations must include the following test categories. These are not optional — they are the minimum standard for verifying that access boundaries hold.

### 11.1 Positive Authorization Tests

**Purpose**: Verify that users with the correct role and scope can successfully perform the action.

- For each permission matrix row: test that every role marked FULL or SCOPED can execute the action within a valid scope
- Test with the exact role (e.g., ProjectCoordinator) and at least one inherited role (e.g., GlobalAdmin)
- Verify the operation completes successfully and returns expected data

### 11.2 Negative Authorization Tests

**Purpose**: Verify that users without the correct role are denied access.

- For each permission matrix row: test that every role marked NONE receives a 403 Forbidden response
- Test with unauthenticated requests (expect 401 Unauthorized)
- Test with authenticated users who have the correct role but wrong scope (e.g., IncubatorAdmin from a different incubator)
- Verify denial responses do not leak information about the resource's existence

### 11.3 Cross-Scope Isolation Tests

**Purpose**: Verify that tenant boundaries are not violated.

- **Cross-incubator**: IncubatorAdmin in Incubator A attempts to access data in Incubator B — must be denied
- **Cross-project**: ProjectCoordinator in Project X attempts to access data in Project Y — must be denied
- **Cross-entrepreneur**: Mentor assigned to Entrepreneur A attempts to access Entrepreneur B's data — must be denied
- Test by directly calling endpoints with valid authentication but a resource ID from another scope
- These tests specifically catch IDOR/BOLA vulnerabilities

### 11.4 Multi-Role Tests

**Purpose**: Verify that users with multiple role assignments see only what their active context allows.

- User with IncubatorAdmin in Incubator A and Mentor in Incubator B: when operating in Incubator B context, verify they have Mentor-level access only (not IncubatorAdmin)
- Verify context switching correctly updates the active scope and role
- Verify no data from a previous context leaks into the new context

### 11.5 Assignment Change Tests

**Purpose**: Verify that revoking or modifying a role assignment immediately affects access.

- Revoke a user's RoleAssignment and verify they lose access to the corresponding scope
- Deactivate a ProjectParticipant and verify they lose access to project data
- Change a user's role (e.g., from ProjectCoordinator to Mentor) and verify their permissions change accordingly
- **Critical**: Verify that session claims are refreshed or invalidated after assignment changes (addresses stale permission risk)

### 11.6 Regression Tests

**Purpose**: Verify that new features do not break existing authorization boundaries.

- Maintain a suite of authorization tests that runs on every build
- Each new feature must not cause existing authorization tests to fail
- When new roles or permissions are added, existing tests must be updated to verify the new role is correctly denied or granted

### 11.7 UI Hiding vs Backend Enforcement Tests

**Purpose**: Verify that every UI-hidden element is also blocked at the backend.

- For each menu item, button, or page restricted by role in the frontend: call the corresponding backend endpoint directly with a user who should not see the UI element
- Verify the backend returns 403 Forbidden (not a success response)
- This test category specifically addresses the "frontend visibility as security" anti-pattern

### 11.8 Audit Logging Verification

**Purpose**: Verify that high-risk actions produce the required audit records.

- For each permission matrix row with Audit = REQUIRED: perform the action and verify an audit record is created
- Verify audit records contain: actor identity, action performed, target resource, timestamp
- Verify audit records cannot be modified or deleted by the actor who triggered them
- Verify failed authorization attempts are also logged (for security monitoring)

### 11.9 Response Indistinguishability

**Purpose**: Public endpoints whose specification masks an outcome (e.g. masked-success on uniqueness conflict) must produce visibly identical responses across every masked outcome. This category protects the masking contract from regression by drift in headers, cookies, or rendered HTML. Adversaries probe these endpoints for enumeration oracles; a single divergent header reopens the oracle.

**Trigger condition**: A feature whose specification declares that two or more outcome paths must be indistinguishable to an unauthenticated observer (status, headers, body bytes after deterministic stripping).

**Minimum automated assertion**: ≥ 1 integration test runs ≥ 50 probes mixing every relevant outcome and asserts pairwise equality across every probe of: HTTP status, `Location` header, `Cache-Control`, `Content-Type`, the *set of names* of `Set-Cookie` cookies, and post-redirect body bytes after a deterministic strip of inherently-random tokens (antiforgery cookie/value).

**Canonical example**: Feature 018 FR-018-19 — `/Access/Register` 50-probe sweep mixing fresh, duplicate-email, and duplicate-national-ID submissions. Test name pattern: `PublicRegistration_FiftyProbeSweep_*`.

### 11.10 Outcome Audit Logging

**Purpose**: When a public-facing endpoint masks its outcome to the caller, the server-side log must still record the *true* outcome with enough detail to support post-hoc forensics and abuse-rate alerting. The log is the only artefact that distinguishes a duplicate-conflict probe from a genuine new registration. Loss of this log means loss of the operator's only signal that someone is iterating against the form.

**Trigger condition**: A feature whose specification requires that the visible response masks the underlying outcome (success vs failure vs conflict).

**Minimum automated assertion**: ≥ 1 unit or integration test pins the outcome-code log entry's required fields — the spec's complete outcome enum is exercised, and for each value the test asserts the log line carries the required structured fields (outcome code, correlation ID, optional submitter identity per the spec's redaction rules).

**Canonical example**: Feature 018 FR-018-14 — `RegisterUserHandler` always returns `Success()` to the caller, but logs the true outcome with the structured fields required by 016 FR-016-06. Test name pattern: `RegisterUserHandler_*_LogsOutcomeWith*`.

### 11.11 Public-vs-Admin Attribution

**Purpose**: Where the same domain operation is exposed both to an unauthenticated public path AND to an authenticated admin path, the public path must mask conflict outcomes while the admin path must surface field-attributed errors. The asymmetry must be tested explicitly so that a refactor cannot silently leak admin specificity onto the public surface or strip admin specificity from the admin surface.

**Trigger condition**: A feature whose specification splits one capability across a public endpoint and an admin endpoint with different response-shaping rules.

**Minimum automated assertion**: ≥ 1 admin-side test asserts each conflict surface returns a field-attributed error with the exact message the spec requires; ≥ 1 public-side test asserts the same conflict surface returns the masked-success response and no field attribution.

**Canonical example**: Feature 018 FR-018-15 — `AdminEnrollUserCommand` returns `Result.Failure` with `("NationalId", "Ya existe una cuenta con este número de identificación.")`; the public path's `RegisterUserCommand` masks the same conflict to `Success`. Test name patterns: `AdminEnrollment_DuplicateNationalId_ReturnsAttributedError` and `RegisterUserHandler_DuplicateNationalId_*MasksOutcome`.

### 11.12 Form-State Preservation

**Purpose**: When an access-security validator failure causes the server to re-render a form with a generic banner (rather than redirect), every non-secret field the user typed must be re-populated, and every secret field must be cleared. Failure of either side is a UX regression *and* a leak vector — re-rendered passwords disclose them in browser DOM caches and to over-the-shoulder observers.

**Trigger condition**: A feature whose specification renders a generic in-form failure response after server-side validation (rather than redirecting on failure) AND whose form contains both non-secret and secret fields.

**Minimum automated assertion**: ≥ 1 E2E test submits a server-side failure path; asserts every non-secret field input contains the previously-submitted value; asserts every secret field input value is empty.

**Canonical example**: Feature 018 FR-018-20 — `/Access/Register` POST with a server-side validator failure re-renders the form with the generic banner; Email/FirstName/LastName/Country/NationalId carry the prior submission, Password and ConfirmPassword are blank. Test name: `Registration_GenericBannerRender_PreservesNonSecretFields`.

### 11.13 Defense-in-Depth Controls

**Purpose**: Antiforgery enforcement and rate limiting are the two unconditional layers below the application's access-security logic. Their presence is mandatory; their absence is a CWE-class regression. Tests assert both layers actually engage on the endpoints the spec says they protect — not that the configuration *exists*, but that a request without a valid token gets blocked and a request burst gets throttled.

**Trigger condition**: A feature whose specification declares that an endpoint is protected by antiforgery AND/OR by a rate-limit policy.

**Minimum automated assertion**: ≥ 1 integration test asserts a POST to the endpoint with no antiforgery token returns HTTP 400; ≥ 1 integration test asserts a synthetic-tight rate-limit policy engages and returns HTTP 429 after the configured threshold is exceeded.

**Canonical example**: Feature 018 FR-018-21 — `POST /Access/Register` and `POST /Administration/Users/Enroll` reject missing antiforgery tokens; `/Access/Register` rate-limit policy engages within configured probe count. Test name patterns: `*_AntiforgeryMissing_Returns400`, `*_RateLimitExceeded_Returns429`.

### 11.14 Content-Policy Rules

**Purpose**: When the password policy (or other content-policy validator) rejects user-supplied content based on what it contains rather than its structure, every rejection branch must be tested. Branch coverage of the verbatim form, the normalised form, the case-folded form, and below-threshold short-circuits is required because a missing branch means the policy is silently weaker than the spec promises.

**Trigger condition**: A feature whose specification adds a containment-style validator (password contains email, password contains national ID, message contains banned phrase, etc.) with multiple normalisation forms or threshold short-circuits.

**Minimum automated assertion**: ≥ 1 unit test per rejection branch named in the spec, plus ≥ 1 unit test per below-threshold short-circuit named in the spec, asserting the rejection message is the shared user-facing string and does not disclose which branch matched.

**Canonical example**: Feature 018 FR-018-18 — `PasswordIdentifyingDataRule` rejection of password containing the user's email (full and local part), national ID (verbatim and separator-stripped), with the 4-character threshold short-circuits. Test name pattern: `PasswordIdentifyingData_*`.

## 12. Open Questions / Decisions Needed

The following items must be resolved by stakeholders before implementing features that depend on them. Per Constitution Rule 10, unresolved open questions block implementation of the affected area.

### 12.1 Mentor Permissions

- **[OPEN]** Can a Mentor see a list of all entrepreneurs in their project, or only those specifically assigned to them?
- **[OPEN]** Does "ViewProjectProgress" for Mentors show full project dashboards or only data filtered to their assigned entrepreneurs?
- **[OPEN]** Can a Mentor view read-only project-level information (announcements, schedules) beyond their mentoring-specific data?
- **[OPEN]** Can a Mentor be assigned to multiple projects across different incubators simultaneously, and if so, are there limits?

### 12.2 Entrepreneur Permissions

- **[OPEN]** Can an Entrepreneur see the profiles of their assigned mentor(s) (name, contact info, availability)?
- **[OPEN]** Can an Entrepreneur see any project-level information (project name, current stage, general announcements)?
- **[OPEN]** When enrolled in multiple projects within the same incubator, can an Entrepreneur switch between projects or must they use separate sessions?
- **[OPEN]** Can an Entrepreneur view historical data from completed diagnostic cycles or only current/active ones?

### 12.3 Sponsor Permissions

- **[OPEN]** At what scope does Sponsor access operate — per-project, per-incubator, or a custom set of projects?
- **[OPEN]** What data granularity does the Sponsor dashboard expose — aggregate metrics only, or individual participant progress?
- **[OPEN]** Can a Sponsor see entrepreneur names/identities, or only anonymized/aggregated data?
- **[OPEN]** How is a Sponsor's access provisioned — via standard RoleAssignment to a specific incubator/project, or a separate mechanism?

### 12.4 IncubatorAdmin Project Creation

- **[OPEN]** Can an IncubatorAdmin create new projects within their incubator, or can only GlobalAdmin create projects?
- **[OPEN]** If IncubatorAdmin can create projects, are there limits (e.g., based on subscription plan)?

### 12.5 ProjectCoordinator User Management

- **[OPEN]** Can a ProjectCoordinator directly create/enroll entrepreneurs in their project, or can they only manage assignments for users already created by IncubatorAdmin?
- **[OPEN]** Can a ProjectCoordinator invite mentors to their project, or must mentors be assigned by IncubatorAdmin?

### 12.6 Multi-Role Across Scopes

- **[OPEN]** Can a user hold conflicting roles in the same incubator (e.g., both ProjectCoordinator for Project A and Entrepreneur in Project B)?
- **[OPEN]** When a user has multiple roles, is there a "primary" role or are all roles equally weighted?
- **[OPEN]** Should the platform warn administrators when assigning a user a role that might conflict with existing assignments?

### 12.7 Impersonation / Support Access

- **[OPEN]** Should GlobalAdmin be able to impersonate another user (act as them) for support or debugging purposes?
- **[OPEN]** If impersonation is supported, what audit trail is required? Must the impersonated user be notified?
- **[OPEN]** Should there be a "support" role that is lower than GlobalAdmin but can view (not modify) any scope for customer support?

### 12.8 Archived Entity Access

- **[OPEN]** Who can see deactivated incubators — only GlobalAdmin, or also the former IncubatorAdmin?
- **[OPEN]** Who can see deactivated projects — GlobalAdmin, IncubatorAdmin, or also former ProjectCoordinator?
- **[OPEN]** Can deactivated entities be reactivated, and by whom?
- **[OPEN]** Should there be a "read-only archive" access level for historical data?

### 12.9 Notification Visibility Rules

- **[OPEN]** Do notifications respect scope boundaries (e.g., IncubatorAdmin notifications only contain data from their incubator)?
- **[OPEN]** If a user's role is revoked, do their pending/unread notifications get removed or marked as inaccessible?
- **[OPEN]** Can notifications contain links to resources — and if so, must the link target be re-validated for access at click time?

### 12.10 Reporting / Export Permissions

- **[OPEN]** Which roles can export data, and at what scope level?
- **[OPEN]** Should exports be limited to the active incubator/project context, or can GlobalAdmin export cross-incubator data?
- **[OPEN]** Are there data fields that must be redacted or anonymized in exports (e.g., entrepreneur personal data)?
- **[OPEN]** Must export actions be logged as audit events with the full scope of exported data?

## 13. Delivery Quality Gate

This section governs the CI workflow that protects every access-security feature from coverage drift. The gate consists of: a coverage-enforcement tool (`Mentoory.Specs.CoverageCheck`), an MSBuild integration that runs the tool on every `dotnet build`, and a GitHub Actions stage that fails the workflow on coverage violations. Together they make "spec-vs-test drift" a build-time error rather than a review-time observation.

### 13.1 Scope anchor

This section binds **only** access-security features. A specification opts in by declaring `access-security: true` in its YAML front-matter (`spec.md`, between `---` fences at line 1). Specifications without the front-matter flag are scanned for traceability (Section 13.2) but are exempt from floor-category enforcement (Section 13.3). Areas outside access-security (Tenant, Diagnostic, Mentoring, Knowledge, Subscription, Example) MAY adopt this gate later by adding the front-matter to their own spec files; this amendment does not retroactively force them.

### 13.2 Traceability rule

Every functional-requirement (`FR-DDD` or `FR-DDD-DD`) and success-criterion (`SC-DDD` or `SC-DDD-DD`) identifier declared in an opted-in `spec.md` MUST have at least one non-skipped xUnit test method carrying the corresponding `[Trait("Spec", "FR-…")]` or `[Trait("Sc", "SC-…")]` attribute. Multi-claim is permitted: a single test method MAY claim multiple identifiers via repeated `[Trait]` attributes, and each identifier counts once. The drift assertion is two-sided:

- **Unclaimed Identifier**: a spec declares an identifier that no test claims. Build-fail.
- **Dangling Trait**: a test claims an identifier that no spec declares. Build-fail.

### 13.3 Floor-category rule

Every opted-in feature MUST have at least one non-skipped test claiming each applicable floor category (Sections 11.9 through 11.14) via `[Trait("Floor", "<category-name>")]`. The applicable set is determined by the trigger conditions defined in Sections 11.9–11.14; the canonical names are:

- `response-indistinguishability`
- `outcome-audit-logging`
- `public-vs-admin-attribution`
- `form-state-preservation`
- `defense-in-depth-controls`
- `content-policy-rules`

For the inaugural rollout (feature 016), all six categories apply. Future opted-in features whose specifications do not match every trigger condition MAY narrow the applicable set explicitly; the narrowing MUST be motivated in the feature's spec body. The default is "all six apply."

### 13.4 CI stage sequence

The required GitHub Actions pipeline runs in this fixed order; each stage's pass is the entry criterion for the next:

1. **build** — `dotnet build Mentoory.sln --configuration Release`. Fails on compiler error or analyzer violation.
2. **coverage-check** — invokes `Mentoory.Specs.CoverageCheck` against `specs/` and the test-assembly glob. Fails on Unclaimed, Dangling, MissingFloorCategories, DuplicateIds, MalformedExclusions, or assembly-load errors. Runs **before** any test job because drift detection costs ~2 s and fails fast.
3. **unit-tests** — `dotnet test` against the unit-test projects (`Mentoory.*.Tests`).
4. **integration-tests** — `dotnet test` against `Mentoory.Tests.Integration`.
5. **e2e-tests** — `dotnet test` against `Mentoory.Tests.E2E`.

Each stage is a required status check on `develop`. A failure at stage N skips stages N+1..5 — the workflow short-circuits to surface the earliest signal.

### 13.5 Exclusion marker format

A specification MAY exempt an identifier from traceability via an inline marker placed on the line immediately following the identifier line:

```
- **FR-018-03** …requirement text…
  *Coverage: N/A — <≥ 20-character justification sentence ending in a period>.*
```

The exclusion is parsed by regex `\*Coverage:\s*N/A\s*[—-]{1,2}\s*(.{20,}?)\.\s*\*`. Justifications shorter than 20 characters or missing the trailing period are MalformedExclusions and fail the build. Excluded identifiers are listed in a separate audit section of the tool's output, never in Unclaimed.

### 13.6 Flaky-test policy

A test that fails non-deterministically in CI MUST be quarantined within 24 hours by adding `[Trait("Flaky", "true")]` to the method. Once tagged, the coverage tool treats the test as **non-claiming** for every other trait it carries. If the quarantined test was the only claimant for an identifier, the gate immediately reports that identifier as Unclaimed → build-fail → the team is forced to repair the test, find another claim, or mark the identifier `Coverage: N/A` with a justification. This behaviour is intentional: quarantine without immediate visibility is silent erosion. The 24-hour quarantine clock starts when CI surfaces the first non-deterministic failure of an identifier-claiming test.

## Appendix A: Permission Matrix (Compact)

Quick-reference version. For full details (constraints, audit, notes), see Section 6.

| Action | GA | IA | PC | M | E | S |
|--------|:--:|:--:|:--:|:-:|:-:|:-:|
| Create Incubator | FULL | - | - | - | - | - |
| Create User | FULL | SCOPED | - | - | - | - |
| Assign Role | FULL | SCOPED | - | - | - | - |
| Assign User to Incubator | FULL | SCOPED | - | - | - | - |
| Assign User to Project | FULL | SCOPED | SCOPED | - | - | - |
| View Incubator | FULL | SCOPED | - | - | - | - |
| Update Incubator | FULL | SCOPED | - | - | - | - |
| Create Project | FULL | SCOPED | - | - | - | - |
| View Project | FULL | SCOPED | SCOPED | READ | READ | READ |
| Update Project | FULL | SCOPED | SCOPED | - | - | - |
| Create Diagnostic Form | FULL | SCOPED | SCOPED | - | - | - |
| Edit Diagnostic Form | FULL | SCOPED | SCOPED | - | - | - |
| Publish Diagnostic Form | FULL | SCOPED | SCOPED | - | - | - |
| Assign Mentor to Project | FULL | SCOPED | SCOPED | - | - | - |
| Assign Mentor to Entrepreneur | FULL | SCOPED | SCOPED | - | - | - |
| View Entrepreneur Data | FULL | SCOPED | SCOPED | SCOPED | SCOPED | - |
| Manage Platform Settings | FULL | - | - | - | - | - |
| Batch Upload Users | FULL | SCOPED | SCOPED | - | - | - |
| View User Profile | FULL | SCOPED | SCOPED | READ | READ | - |
| Manage Templates | FULL | - | - | - | - | - |
| Manage Subscriptions | FULL | - | - | - | - | - |
| Correct Diagnostic Answers | FULL | SCOPED | SCOPED | SCOPED | - | - |
| Complete Diagnostic | FULL | - | - | - | SCOPED | - |

`-` = NONE (denied). SCOPED = within assigned scope only.

## Appendix B: Developer Checklist (Standalone)

Copy this checklist into your feature spec, PR description, or design document. All items must be answered before code review.

```markdown
## Access & Security Checklist

- [ ] **Business capability**: _______________________________________________
- [ ] **Permitted roles**: ___________________________________________________
- [ ] **Scope** (Platform / Incubator / Project): ____________________________
- [ ] **Category** (Platform / Incubator / Project feature): _________________
- [ ] **Affects users, permissions, or visibility?** (yes/no): _______________
      If yes, safeguards: __________________________________________________
- [ ] **Backend enforcement**:
      - [ ] [Authorize(Roles = "...")] on controller: ______________________
      - [ ] CheckPermissionQuery in handler: ________________________________
      - [ ] ITenantContext scope filter in queries: _________________________
- [ ] **Audit events** (REQUIRED / RECOMMENDED / NONE): _____________________
- [ ] **Security tests**:
      - [ ] Positive authorization test: ___________________________________
      - [ ] Negative authorization test: ___________________________________
      - [ ] Cross-scope isolation test (if applicable): ____________________
```

## Appendix C: Immediate Next Actions

The following actions should be taken to formalize this constitution in the existing application:

1. **Ratify this document with stakeholders** — Present to engineering leads, product owners, and security reviewers for approval. Update status from "Draft" to "Ratified" upon approval.

2. **Resolve open questions for Mentor, Entrepreneur, and Sponsor roles** — Schedule a decision meeting with product ownership to answer the questions in Section 12.1-12.3. Update the Role Catalog (Section 5) and Permission Matrix (Section 6) with confirmed answers.

3. **Integrate PR template into repository workflow** — Merge the `.github/PULL_REQUEST_TEMPLATE.md` file and communicate the new access checklist requirement to all contributors.

4. **Add SpecKit checklist hook** — Configure `/speckit.checklist` to auto-generate the access checklist from Section 10 for all new features going through the SpecKit pipeline.

5. **Audit existing endpoints against permission matrix** — Review every existing controller's `[Authorize]` attributes and compare against the permission matrix (Section 6). Document gaps and create remediation tasks.

6. **Add missing authorization checks** — For any endpoint where the permission matrix grants differ from the current `[Authorize]` configuration, create tasks to add or correct authorization attributes and handler-level checks.

7. **Implement session invalidation on role changes** — Address Threat 9 (stale permissions) by implementing session claim refresh or invalidation when RoleAssignment records are modified.

8. **Establish authorization test suite** — Create the test categories defined in Section 11 as an automated regression suite that runs on every build.
