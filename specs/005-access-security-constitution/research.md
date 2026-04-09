# Research: Access & Security Constitution

**Feature**: 005-access-security-constitution  
**Date**: 2026-04-08  
**Purpose**: Consolidate codebase analysis to inform governance document authoring

## R1: Current Role Model

**Decision**: The platform uses a 6-role hierarchical model defined in `PlatformRole` enum.

**Findings**:
- **Source**: `Mentoory.Access.Domain/Enums/PlatformRole.cs`
- Roles (ordinal): GlobalAdmin (0), IncubatorAdmin (1), ProjectCoordinator (2), Mentor (3), Entrepreneur (4), Sponsor (5)
- Higher ordinal = lower privilege (hierarchy is inverse of enum value)
- `ProjectCoordinator` is the codebase term; "Project Admin" from business requirements is a deprecated alias

**Rationale**: Enum is the single source of truth for role definitions. The governance document must mirror this exactly.

**Alternatives considered**: None — the enum is already established in the codebase.

## R2: Authorization Model Type

**Decision**: Hierarchical RBAC with scoped tenancy (incubator + optional project).

**Findings**:
- **Source**: `Mentoory.Access.Domain/Aggregates/RoleAssignment/RoleAssignment.cs`
- RoleAssignment binds: UserId + IncubatorId + ProjectId (nullable) + Role + IsActive
- Users can have multiple RoleAssignments across incubators
- Constraint: one active Entrepreneur role per user per incubator
- `[Authorize(Roles = "...")]` on controllers includes target role + all higher roles (Principle X)
- No ABAC or policy-based authorization is implemented; pure role-based with scope filtering

**Rationale**: The combination of hierarchical role inclusion (`ProjectCoordinator,IncubatorAdmin,GlobalAdmin`) and tenant-scoped assignment (`IncubatorId`, `ProjectId`) confirms hierarchical RBAC + scoped tenancy — not pure RBAC, not ABAC.

**Alternatives considered**:
- Pure RBAC: Rejected — scope (incubator/project) is a first-class access dimension, not just a role
- RBAC + ABAC: Rejected — no attribute-based policies exist; all access is role + scope
- Pure tenancy: Rejected — roles within a tenant carry different permissions

## R3: Permission Model

**Decision**: Granular permission enum with role-to-permission mapping in handler code.

**Findings**:
- **Source**: `Mentoory.Access.Domain/Enums/Permission.cs`, `Mentoory.Access.Application/Queries/CheckPermission/CheckPermissionHandler.cs`
- Permission categories: Platform (4), Incubator (3), Project (5), Mentoring (4), Participant (3), Sponsor (1)
- Role-permission mapping:
  - GlobalAdmin: ALL permissions
  - IncubatorAdmin: ManageProjects, ManageIncubatorUsers, EnrollParticipants, ManageDiagnostics, ManageKnowledge, ManageLifecycle, ManageProjectParticipants, ViewProjectProgress
  - ProjectCoordinator: ManageDiagnostics, ManageKnowledge, ManageLifecycle, ManageProjectParticipants, ViewProjectProgress
  - Mentor: ManageMentoringPlans, ManageSessions, ManageAssignments, CorrectAnswers, ViewProjectProgress
  - Entrepreneur: CompleteDiagnostic, ViewMentoringPlan, SubmitAssignments
  - Sponsor: ViewProjectProgress

**Rationale**: This mapping is the authoritative permission baseline for the governance document's permission matrix.

**Alternatives considered**: None — mapping is established in code.

## R4: Tenant Isolation Mechanisms

**Decision**: Middleware-based tenant context + claims-based session context.

**Findings**:
- **TenantContextMiddleware** (`Mentoory.Web/Infrastructure/Authorization/TenantContextMiddleware.cs`): Extracts `ActiveIncubatorId` claim, populates scoped `ITenantContext`
- **SessionAuthenticationMiddleware** (`Mentoory.Web/Infrastructure/Authentication/SessionAuthenticationMiddleware.cs`): Validates session token, checks account status, auto-signout on invalid session
- **PasswordResetRequiredFilter** (`Mentoory.Web/Infrastructure/Authentication/PasswordResetRequiredFilter.cs`): Redirects to password change if AccountStatus is PasswordResetRequired
- **ContextController** (`Mentoory.Web/Controllers/ContextController.cs`): `SetActiveContextCommand` validates RoleAssignment ownership, stores in claims: ActiveRole, ActiveIncubatorId, ActiveProjectId
- Claims used: ActiveIncubatorId, ActiveProjectId, ActiveRole, ClaimTypes.Role

**Rationale**: The governance document's enforcement model section must describe this exact pattern as the current state.

**Alternatives considered**: None — documenting what exists.

## R5: Controller Authorization Patterns

**Decision**: Role-based `[Authorize]` at controller level; no custom policies.

**Findings**:
- Platform area (GlobalAdmin only): IncubatorsController, UsersController (Platform), ConfigurationController, TemplatesController
- Administration area (IncubatorAdmin + GlobalAdmin): UsersController (Admin), ProjectsController, DashboardController, BatchUploadController
- Coordination area (ProjectCoordinator + Mentor + IncubatorAdmin + GlobalAdmin): DiagnosticsController, AnswerCorrectionController
- Participant area (all roles): DiagnosticController (Participant)
- Sponsor area (Sponsor + IncubatorAdmin + GlobalAdmin): SponsorController
- Universal (all authenticated): ChangePasswordController, ContextController

**Rationale**: This mapping directly feeds the permission matrix's "current state" column.

**Alternatives considered**: None — documenting what exists.

## R6: User Aggregate & Account Lifecycle

**Decision**: User aggregate manages identity; RoleAssignment manages access.

**Findings**:
- **Source**: `Mentoory.Access.Domain/Aggregates/User/User.cs`
- User properties: ExternalId, Email, NationalIdentity, FirstName, LastName, AccountStatus
- AccountStatus enum: PendingVerification, Active, Locked, Disabled, PasswordResetRequired
- Methods: Register, VerifyEmail, Lock/Unlock, Activate/Deactivate, ChangePassword
- User creation flows enforce email verification (onboarding)

**Rationale**: The governance document's secure onboarding/offboarding section must reference these lifecycle states.

**Alternatives considered**: None — documenting what exists.

## R7: Project & Participant Model

**Decision**: Project aggregate owns participant enrollment separately from RoleAssignment.

**Findings**:
- **Source**: `Mentoory.Tenant.Domain/Aggregates/Project/Project.cs`, `ProjectParticipant.cs`
- Project belongs to single Incubator (IncubatorId FK)
- ProjectParticipant: ExternalId, UserId, Role, IsActive, EnrolledAtUtc
- MentorAssignment embedded in Project aggregate
- Dual relationship: RoleAssignment (access scope) + ProjectParticipant (project enrollment)

**Rationale**: The governance document must address this dual-binding model in the scope section — a user needs both a valid RoleAssignment AND project enrollment to access project-scoped data.

**Alternatives considered**: None — documenting what exists.

## R8: Document Placement & Versioning

**Decision**: Separate file at `.specify/memory/access-security-constitution.md`, linked from `constitution.md`, with independent semantic versioning.

**Findings**:
- Confirmed via clarification (session 2026-04-08)
- Constitution.md uses semantic versioning (MAJOR.MINOR.PATCH) with Sync Impact Report
- Access constitution will follow the same versioning convention independently
- Amendment procedure mirrors constitution.md governance section

**Rationale**: Keeps the main constitution focused on code/architecture principles while the access constitution handles security governance.

**Alternatives considered**:
- Embedding in constitution.md: Rejected — would make constitution.md too large and mix concerns
- Storing in specs directory: Rejected — governance documents belong in memory, not feature specs

## R9: Checklist Enforcement

**Decision**: Dual enforcement — PR template + SpecKit checklist.

**Findings**:
- Confirmed via clarification (session 2026-04-08)
- PR template: `.github/PULL_REQUEST_TEMPLATE.md` with required access section
- SpecKit: `/speckit.checklist` generates access checklist from constitution during feature workflow
- Reviewers reject PRs with incomplete access sections

**Rationale**: Covers both planned features (SpecKit pipeline) and ad-hoc changes (direct PRs).

**Alternatives considered**:
- Code review only: Rejected — too easy to skip, no structured enforcement
- SpecKit only: Rejected — doesn't cover ad-hoc bugfixes and hotfixes
