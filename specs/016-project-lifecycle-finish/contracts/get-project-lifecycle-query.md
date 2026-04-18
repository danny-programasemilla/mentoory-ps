# Contract: `GetProjectLifecycleQuery`

**Layer**: Application (`Mentoory.Tenant.Application.Projects.Queries.GetProjectLifecycle`)
**Pattern**: CQRS query (`IBaseRequest<ProjectLifecycleDto>`), side-effect-free.

## Request

```csharp
public sealed record GetProjectLifecycleQuery(
    Guid ProjectExternalId,
    long ActingUserIncubatorId,
    bool ActingUserIsGlobalAdmin
) : IBaseRequest<ProjectLifecycleDto>;
```

### Field semantics

| Field                       | Required | Constraint                                         |
|-----------------------------|----------|-----------------------------------------------------|
| `ProjectExternalId`         | Yes      | Not `Guid.Empty`.                                   |
| `ActingUserIncubatorId`     | Yes      | Acting user's active incubator internal id; 0 allowed only for `GlobalAdmin`. |
| `ActingUserIsGlobalAdmin`   | Yes      | When true, tenant-scope filter is skipped.          |

## Response

```csharp
public sealed record ProjectLifecycleDto(
    Guid ExternalId,
    string Name,
    string? Description,
    Guid IncubatorExternalId,
    string IncubatorName,
    bool IsActive,
    StageType CurrentStageType,
    StageState CurrentStageState,
    bool CanAdvance,
    string? CannotAdvanceReason,
    IReadOnlyList<ProjectLifecycleStageDto> Stages,
    IReadOnlyList<StageActionDto> Actions
);

public sealed record ProjectLifecycleStageDto(
    StageType StageType,
    string DisplayName,
    StageState State,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc,
    string? AdvancedByDisplay
);

public sealed record StageActionDto(
    StageGatedAction Action,
    string DisplayName,
    StageType GatingStageType,
    string GatingStageDisplayName,
    StageGatedActionState State,
    string LinkUrl
);
```

### Failure codes

| Code                  | Meaning                                              | UI disposition                                            |
|-----------------------|------------------------------------------------------|------------------------------------------------------------|
| `ProjectNotFound`     | Project with given `ExternalId` does not exist.     | 404 page with link back to coordination projects index.    |
| `ProjectOutOfScope`   | Incubator scope mismatch.                            | 403; redirect to `/Context/Select` with explanatory toast. |

## Handler flow

```
1. Load Project via IProjectRepository.Query()
   .AsNoTracking()
   .Include("_stages")
   .Where(p => p.ExternalId == ProjectExternalId)
   .SingleOrDefaultAsync(ct)
   → null ⇒ Failure(ProjectNotFound)

2. If !ActingUserIsGlobalAdmin
       AND project.IncubatorId != ActingUserIncubatorId
   → Failure(ProjectOutOfScope)

3. Load related Incubator (name, external id) by project.IncubatorId
   — one extra round-trip is acceptable; alternative is a join in the query.

4. Resolve AdvancedByDisplay for each stage: for stages with AdvancedByUserId,
   look up the user's display name. The handler delegates to a user-lookup
   helper; unresolved ids render as null (the UI treats null as "Sistema" or
   blank per the Razor view).

5. Compose Stages:
       seven ProjectLifecycleStageDto in canonical StageType order,
       each with DisplayName from StageTypeDisplay, timestamps from the
       stage entity, and AdvancedByDisplay from step 4.

6. Compose Actions:
       for each StageGatedAction in the enum,
       state = StageActionRegistry.GetState(project.CurrentStageType, action)
       gatingStage = StageActionRegistry.GetGatingStage(action)
       linkUrl = StageActionLinks.Resolve(action)  // static helper returning /Coordination/... URL
       DisplayName = StageActionDisplay.ToSpanish(action)

7. Compute CanAdvance + CannotAdvanceReason:
       if !project.IsActive → (false, "El proyecto está inactivo.")
       elif project.CurrentStageState != InProgress
                 → (false, "La etapa actual no está en progreso.")
       elif project.CurrentStageType == Closure
                 → (false, "El proyecto ya está en la etapa final (Cierre).")
       else    → (true, null)

8. Return Success(new ProjectLifecycleDto(...)).
```

## Authorization

The query itself does not enforce user role; the calling controller does via `[Authorize(Roles = "ProjectCoordinator,IncubatorAdmin,GlobalAdmin")]`. Tenant isolation is enforced by the handler (step 2). `CanAdvance` does not encode the role check — it encodes only the business rules. The controller hides the advance button unless `CanAdvance` is true; the command separately validates role at the endpoint.

## Performance

- One project + 7 stages + 1 incubator lookup per request. Well under the 2 s p95 target.
- `AsNoTracking()` is mandatory (code review standard). Mapperly projection is synchronous — no extra DB round-trips.
