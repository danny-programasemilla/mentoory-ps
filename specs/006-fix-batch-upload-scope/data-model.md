# Data Model: Fix Batch Upload Project Scope Authorization Bug

**Branch**: `006-fix-batch-upload-scope` | **Date**: 2026-04-08

## Affected Entities

No new entities are introduced. This feature modifies query contracts and validation logic over existing entities.

### RoleAssignment (existing — read-only access)

| Field | Type | Relevance |
|-------|------|-----------|
| UserId | long | Identifies the authenticated user |
| IncubatorId | long | Scope boundary — must match active incubator context |
| ProjectId | long? | **Key field** — when non-null, defines project-scope boundary for ProjectCoordinator |
| Role | string | Determines filtering strategy: "ProjectCoordinator" triggers project-scoped filtering |
| IsActive | bool | Only active assignments grant access |

**Usage in this feature**: Controller queries active assignments for the current user, filters to current incubator and role, extracts `ProjectId` values to pass as scope filter.

### Project (existing — filtered read)

| Field | Type | Relevance |
|-------|------|-----------|
| Id | long | Internal ID, matched against RoleAssignment.ProjectId |
| ExternalId | Guid | Exposed in dropdown, submitted in batch upload form |
| Name | string | Display text in dropdown |
| IsActive | bool | Must be true to appear in dropdown |
| CurrentStageType | StageType | Must be `Registration` |
| CurrentStageState | StageState | Must be `InProgress` |
| IncubatorId | long | Filtered by EF Core global query filter via ITenantContext |

**Usage in this feature**: Query filters projects by stage, activity, and (for ProjectCoordinator) by the authorized project ID list.

### Incubator (existing — validation only)

| Field | Type | Relevance |
|-------|------|-----------|
| Id | long | Validated against ITenantContext.CurrentIncubatorId |
| ExternalId | Guid | Returned in query result for batch upload command |

## Query Contract Changes

### ListRegistrationProjectsQuery (modified)

**Current**:
```
ListRegistrationProjectsQuery(long IncubatorId)
```

**Proposed**:
```
ListRegistrationProjectsQuery(long IncubatorId, IReadOnlyList<long>? AuthorizedProjectIds)
```

- `AuthorizedProjectIds = null` → return all registration-stage projects (GlobalAdmin/IncubatorAdmin behavior, unchanged)
- `AuthorizedProjectIds = [1, 5, 12]` → return only projects whose `Id` is in the list AND match stage filters
- `AuthorizedProjectIds = []` → return empty list (ProjectCoordinator with no assignments)

### RegistrationProjectsResult (unchanged)

```
RegistrationProjectsResult(Guid IncubatorExternalId, List<RegistrationProjectDto> Projects)
RegistrationProjectDto(Guid ExternalId, string Name)
```

No changes needed to the result shape.

## Validation Rules

| Rule | Enforced At | Description |
|------|-------------|-------------|
| Incubator scope match | Handler | `request.IncubatorId` must match `ITenantContext.CurrentIncubatorId` (when non-null) |
| Project scope for PC | Controller + Handler | ProjectCoordinator only sees projects matching their active RoleAssignment.ProjectId values |
| Submission scope | Controller | `model.ProjectExternalId` must be in the user's authorized project set before dispatching command |
| Role gating | Controller | `[Authorize(Roles = "ProjectCoordinator,IncubatorAdmin,GlobalAdmin")]` |
| Empty scope redirect | Controller | ProjectCoordinator with zero authorized projects is redirected with message |

## State Transitions

No entity state transitions are affected. This feature only modifies read-path filtering and write-path validation.
