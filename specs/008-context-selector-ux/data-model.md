# Data Model: Cascading Context Selector UX

**Feature**: 008-context-selector-ux  
**Date**: 2026-04-11

## Existing Entities (No Changes)

### RoleAssignment (Aggregate Root)
- **Location**: `Mentoory.Access.Domain/Aggregates/RoleAssignment/RoleAssignment.cs`
- **Table**: `access.RoleAssignments`

| Field | Type | Notes |
|-------|------|-------|
| Id | long | PK, internal |
| ExternalId | Guid | UNIQUE, used in routes and API responses |
| UserId | long | FK to Users |
| IncubatorId | long | FK to Incubators |
| ProjectId | long? | FK to Projects (null for incubator-scoped roles) |
| Role | string | One of Roles.All constants |
| IsActive | bool | Soft-delete flag |
| CreatedAtUtc | DateTime | |
| UpdatedAtUtc | DateTime | |

**UNIQUE constraint**: `(UserId, IncubatorId, ProjectId, Role) WHERE IsActive = 1`

### UserContext (Read Model — No Changes)
- **Location**: `Mentoory.Access.Domain/ReadModels/UserContext.cs`

```csharp
public record UserContext(
    Guid RoleAssignmentExternalId,
    long UserId,
    long IncubatorId,
    string? IncubatorName,
    long? ProjectId,
    string? ProjectName,
    string Role);
```

## New DTOs

### ContextRoleDto
- **Location**: `Mentoory.Access.Application/Queries/ListContextRoles/ContextRoleDto.cs`

```csharp
public sealed record ContextRoleDto(string Role, string DisplayName);
```

**Display name mapping** (Spanish):

| Role | DisplayName |
|------|-------------|
| GlobalAdmin | Administrador Global |
| IncubatorAdmin | Administrador de Incubadora |
| ProjectCoordinator | Coordinador de Proyecto |
| Mentor | Mentor |
| Entrepreneur | Emprendedor |
| Sponsor | Patrocinador |

### ContextIncubatorDto
- **Location**: `Mentoory.Access.Application/Queries/ListContextIncubators/ContextIncubatorDto.cs`

```csharp
public sealed record ContextIncubatorDto(
    long Id,
    string Name,
    Guid RoleAssignmentExternalId);
```

**Notes**:
- For incubator-scoped roles (IncubatorAdmin, GlobalAdmin): `RoleAssignmentExternalId` is the direct assignment's ExternalId
- For project-scoped roles (Mentor, Entrepreneur, ProjectCoordinator): `RoleAssignmentExternalId` is from the first matching assignment under that incubator (actual resolution happens at project level via `ContextProjectDto`)

### ContextProjectDto
- **Location**: `Mentoory.Access.Application/Queries/ListContextProjects/ContextProjectDto.cs`

```csharp
public sealed record ContextProjectDto(
    long Id,
    string Name,
    Guid RoleAssignmentExternalId);
```

**Notes**:
- Each project entry carries the exact `RoleAssignmentExternalId` for that (user, role, incubator, project) combination
- For GlobalAdmin: carries the GlobalAdmin's base assignment ExternalId (overrides applied via switch endpoint)

### ContextSwitchRequest (Extended)
- **Location**: `Mentoory.Web/Controllers/ContextController.cs` (inline record)

```csharp
public sealed record ContextSwitchRequest(
    Guid RoleAssignmentExternalId,
    long? IncubatorId = null,
    string? IncubatorName = null,
    long? ProjectId = null,
    string? ProjectName = null);
```

**Notes**:
- Override fields are only used when the resolved role is GlobalAdmin
- Non-GlobalAdmin requests with override fields: overrides are silently ignored

## New Queries

### ListContextRolesQuery
- **Location**: `Mentoory.Access.Application/Queries/ListContextRoles/`

```csharp
public sealed record ListContextRolesQuery(long UserId) 
    : IBaseRequest<List<ContextRoleDto>>;
```

**Handler logic**:
1. `repository.GetActiveByUserIdAsync(userId)` → get all active assignments
2. Extract distinct roles
3. Map to `ContextRoleDto` with Spanish display names
4. Order by role hierarchy (GlobalAdmin first → Sponsor last)

### ListContextIncubatorsQuery
- **Location**: `Mentoory.Access.Application/Queries/ListContextIncubators/`

```csharp
public sealed record ListContextIncubatorsQuery(long UserId, string Role) 
    : IBaseRequest<List<ContextIncubatorDto>>;
```

**Handler logic — GlobalAdmin path**:
1. Get user's GlobalAdmin assignment ExternalId via `GetActiveByUserIdAsync`
2. Delegate to `ListIncubatorContextOptionsQuery` for all active incubators
3. Map to `ContextIncubatorDto` with the GlobalAdmin's ExternalId on each

**Handler logic — Other roles**:
1. Query `IRoleAssignmentRepository.Query()` filtered by userId + role + isActive
2. Group by IncubatorId, join with incubator names
3. Map to `ContextIncubatorDto` (using first assignment's ExternalId per incubator group)
4. Order by incubator name

### ListContextProjectsQuery
- **Location**: `Mentoory.Access.Application/Queries/ListContextProjects/`

```csharp
public sealed record ListContextProjectsQuery(long UserId, string Role, long IncubatorId) 
    : IBaseRequest<List<ContextProjectDto>>;
```

**Handler logic — GlobalAdmin path**:
1. Get user's GlobalAdmin assignment ExternalId
2. Query all active projects under IncubatorId via `QueryUnfiltered()`
3. Map to `ContextProjectDto` with the GlobalAdmin's ExternalId

**Handler logic — Other roles**:
1. Query assignments filtered by userId + role + incubatorId + ProjectId != null
2. Join with project names
3. Map to `ContextProjectDto` with each assignment's ExternalId
4. Order by project name

## Relationships

```text
User (1) ──── (*) RoleAssignment
RoleAssignment (*) ──── (1) Incubator
RoleAssignment (*) ──── (0..1) Project

Cascade flow:
  Roles API → distinct roles from user's RoleAssignments
  Incubators API → RoleAssignments filtered by role (or all incubators for GlobalAdmin)
  Projects API → RoleAssignments filtered by role + incubator (or all projects for GlobalAdmin)
```
