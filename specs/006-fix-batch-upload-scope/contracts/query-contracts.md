# Query & Handler Contracts: Batch Upload Scope Fix

**Branch**: `006-fix-batch-upload-scope` | **Date**: 2026-04-08

## Modified Contracts

### 1. ListRegistrationProjectsQuery

**Module**: `Mentoory.Tenant.Application`

```
Record: ListRegistrationProjectsQuery
  - IncubatorId: long (required)
  - AuthorizedProjectIds: IReadOnlyList<long>? (optional, null = no project filter)
Implements: IBaseRequest<RegistrationProjectsResult>
```

**Behavior contract**:
- When `AuthorizedProjectIds` is null → return all registration-stage projects in the incubator (GA/IA path)
- When `AuthorizedProjectIds` is non-null → return only projects whose `Id` is in the list AND match stage filters
- When `AuthorizedProjectIds` is empty list → return empty project list
- Handler MUST validate `IncubatorId` matches `ITenantContext.CurrentIncubatorId` (when context is set)

### 2. ListRegistrationProjectsHandler

**Module**: `Mentoory.Tenant.Application`

**Dependencies added**: `ITenantContext` (injected via constructor)

**Validation steps** (in order):
1. If `ITenantContext.CurrentIncubatorId` is set and does not match `request.IncubatorId` → return `Failure(Unauthorized)`
2. Resolve incubator by ID → return `Failure` if not found
3. Query projects: `IsActive && StageType.Registration && StageState.InProgress`
4. If `request.AuthorizedProjectIds` is non-null → add `.Where(p => authorizedProjectIds.Contains(p.Id))`
5. Return filtered project list

### 3. BatchUploadController

**Module**: `Mentoory.Web`

**Authorization change**: `[Authorize(Roles = "ProjectCoordinator,IncubatorAdmin,GlobalAdmin")]`

**New behavior in `PopulateProjectsAsync` and `Index` (POST)**:
1. Read `ActiveRole`, `ActiveIncubatorId`, `UserId` from claims
2. If role is `ProjectCoordinator`:
   a. Query `IRoleAssignmentRepository.GetActiveByUserIdAsync(userId)`
   b. Filter to assignments where `IncubatorId == activeIncubatorId` and `Role == "ProjectCoordinator"`
   c. Extract `ProjectId` values (non-null only) → `authorizedProjectIds`
   d. If empty → redirect with message "No tiene proyectos asignados para carga masiva."
   e. Pass `authorizedProjectIds` to `ListRegistrationProjectsQuery`
3. If role is `IncubatorAdmin` or `GlobalAdmin`:
   a. Pass `null` as `authorizedProjectIds` (unchanged behavior)

**New validation in `Index` (POST) before command dispatch**:
1. If role is `ProjectCoordinator`:
   a. Resolve `model.ProjectExternalId` against the filtered project list
   b. If project not in authorized list → return `Forbid()` or redirect with error
2. If role is `IncubatorAdmin`:
   a. Verify target project belongs to current incubator (already enforced by query)

## Hardened Contracts (from platform audit)

### 4. GetProjectByExternalIdHandler

**Module**: `Mentoory.Tenant.Application`

**Dependencies added**: `ITenantContext`

**New validation**: After resolving project, verify `project.IncubatorId == ITenantContext.CurrentIncubatorId` (when context is set). Return `Failure` if mismatch.

### 5. GetIncubatorByExternalIdHandler

**Module**: `Mentoory.Tenant.Application`

**Dependencies added**: `ITenantContext`

**New validation**: After resolving incubator, verify `incubator.Id == ITenantContext.CurrentIncubatorId` (when context is set). Return `Failure` if mismatch.

### 6. DiagnosticsController.Clone — Template Scoping

**Module**: `Mentoory.Web`

**Change**: `PopulateTemplatesViewBag()` should filter templates to current incubator scope when user is not GlobalAdmin. (Lower priority — documented for implementation.)
