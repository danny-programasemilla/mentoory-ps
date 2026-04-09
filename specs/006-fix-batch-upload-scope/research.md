# Research: Fix Batch Upload Project Scope Authorization Bug

**Branch**: `006-fix-batch-upload-scope` | **Date**: 2026-04-08

## 1. Root Cause Analysis

### Decision: The bug is in `ListRegistrationProjectsHandler` — it queries all registration-stage projects without filtering by user's role assignment scope.

**Rationale**: The handler at `Mentoory.Tenant.Application/Queries/ListRegistrationProjects/ListRegistrationProjectsHandler.cs` (lines 26-33) filters only by `IsActive`, `StageType.Registration`, and `StageState.InProgress`. It never checks:
- Whether the user has a `RoleAssignment` to the returned projects
- Whether the requested `IncubatorId` matches the user's `ITenantContext.CurrentIncubatorId`

**Contributing factors**:
1. The `BatchUploadController` `[Authorize]` attribute only includes `IncubatorAdmin,GlobalAdmin` — ProjectCoordinator cannot currently access the page, but the handler has no scope filtering regardless
2. The EF Core global query filter on `TenantDbContext` filters by `CurrentIncubatorId`, which provides incubator-level isolation but NOT project-level filtering within the incubator
3. The handler accepts `IncubatorId` directly from the request without validating against `ITenantContext`

**Alternatives considered**:
- Filtering at the controller level only: Rejected — violates Constitution Rule 5 (backend enforcement is sole source of truth)
- Adding a new middleware for scope validation: Rejected — over-engineering; handler-level validation follows existing patterns
- Creating a separate query for ProjectCoordinator: Rejected — a single query with role-aware filtering is simpler and more maintainable

## 2. Query Design: Role-Aware Project Filtering

### Decision: Extend `ListRegistrationProjectsQuery` to accept optional role/userId/projectId parameters, and filter within the handler based on role.

**Rationale**: The handler needs to know the caller's role to decide filtering strategy:
- **GlobalAdmin/IncubatorAdmin**: Return all registration-stage projects in the incubator (current behavior)
- **ProjectCoordinator**: Return only projects where the user has an active `RoleAssignment` with `ProjectId` matching

The query should accept the active role and user ID from the controller (extracted from claims). The handler uses `ITenantContext` to validate incubator scope and the role/user context to filter projects.

**Alternatives considered**:
- Querying `RoleAssignment` from within the Tenant module: Rejected — cross-module domain access violates Clean Architecture. Instead, pass the list of authorized project IDs from the controller.
- Using `CheckPermissionQuery` in the handler: Rejected — CheckPermission verifies a single permission, not project list filtering. Wrong abstraction level.

### Decision: Pass authorized project IDs as a parameter to the query, not cross-module repository access.

**Rationale**: The Tenant.Application layer cannot reference Access.Domain repositories. The controller (Web layer) can query both modules. The controller fetches the user's active `RoleAssignment` records, extracts the `ProjectId` values, and passes them as an `IReadOnlyList<long>?` filter to the query. When null (GlobalAdmin/IncubatorAdmin), no project filtering is applied. When non-null (ProjectCoordinator), only matching projects are returned.

**Alternatives considered**:
- Shared application service: Rejected — unnecessary abstraction for a single use case
- Integration event: Rejected — over-engineering; this is a synchronous query

## 3. Server-Side Submission Validation

### Decision: Add project-scope validation in `BatchRegisterUsersHandler` (or at the controller level before dispatching the command) to verify the user is authorized for the target project.

**Rationale**: Even with the dropdown fixed, a crafted POST request could target an unauthorized project. The controller should validate that `model.ProjectExternalId` is within the user's authorized scope before dispatching `BatchRegisterUsersCommand`. This keeps validation close to the entry point (Web layer) while the handler continues to handle business logic.

**Alternatives considered**:
- Handler-level validation only: Rejected — the handler is in `Access.Application` and doesn't have access to the caller's role context. The controller is the natural place for role-aware validation.
- MediatR pipeline behavior: Rejected — too generic; not all commands need this specific scope check.

## 4. Constitution Amendment

### Decision: Amend Access & Security Constitution §6.2 to change Batch Upload Users from NONE to SCOPED for ProjectCoordinator.

**Rationale**: The business requires ProjectCoordinators to batch-upload users into their assigned projects. The constitution currently disallows this. The amendment adds a constraint: "PC can upload for assigned projects only."

**Change**: Row `Batch Upload Users`, column `PC`: `NONE` → `SCOPED`, Constraint updated to: "GA: unrestricted. IA: own incubator. PC: assigned projects only."

## 5. Platform-Wide Endpoint Audit Results

### Audit scope: All 19 controllers, 30+ query/command handlers

| Endpoint | Classification | Issue | Action Required |
|----------|---------------|-------|-----------------|
| `ListRegistrationProjectsHandler` | **CONFIRMED ISSUE** | No incubator ownership validation, no project-scope filtering | Fix in this feature |
| `GetProjectByExternalIdHandler` | **CONFIRMED ISSUE** | No scope validation on retrieval — any ExternalId returns data | Fix in this feature |
| `GetIncubatorByExternalIdHandler` | **CONFIRMED ISSUE** | No scope validation on retrieval | Fix in this feature |
| `BatchUploadController` | **RISKY** | Handler lacks scope validation; controller missing PC role | Fix in this feature |
| `DiagnosticsController.Clone()` | **RISKY** | Template selection in `PopulateTemplatesViewBag()` not scoped | Fix in this feature |
| `TemplatesController.DiagnosticsData()` | **RISKY (acceptable)** | Returns all templates globally — mitigated by GlobalAdmin-only access | Document as intentional |
| All other endpoints (14) | **SAFE** | Proper scope enforcement via claims + handler validation + query filters | No action needed |

### Safe patterns identified:
- Controllers that extract `ActiveIncubatorId`/`ActiveProjectId` from claims AND pass to handler AND handler filters by those IDs
- EF Core global query filters on `TenantDbContext` and `DiagnosticDbContext` provide incubator-level baseline isolation
- `CheckPermissionQuery` used for fine-grained permission checks

### Vulnerable pattern identified:
- Handler accepts resource ID without validating caller owns that resource
- No `ITenantContext` validation in handler
- Controller trusts claim value without cross-checking against `RoleAssignment`

## 6. Cross-Module Communication Strategy

### Decision: Controller fetches user's project assignments via `GetActiveByUserIdAsync` (Access module), extracts ProjectId list, passes to Tenant module query.

**Rationale**: The Web layer has access to both modules. This avoids cross-module domain references while keeping the filtering logic explicit. The controller already has the user's `ActiveRole` from claims, so it can decide whether to pass the project filter (PC) or null (GA/IA).

**Flow**:
```
Controller (Web)
├── Read ActiveRole, ActiveIncubatorId, UserId from claims
├── If ProjectCoordinator:
│   ├── Query RoleAssignment.GetActiveByUserIdAsync(userId)
│   ├── Filter to assignments matching incubatorId + role
│   ├── Extract ProjectId list
│   └── Pass projectIds to ListRegistrationProjectsQuery
├── If IncubatorAdmin/GlobalAdmin:
│   └── Pass projectIds = null to ListRegistrationProjectsQuery
└── ListRegistrationProjectsHandler filters projects by projectIds (if provided)
```
