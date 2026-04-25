# Contract: `AdvanceProjectStageCommand`

**Layer**: Application (`Mentoory.Tenant.Application.Commands.AdvanceProjectStage`)
**Pattern**: CQRS command (`IBaseRequest<AdvanceProjectStageResult>`), handler inherits `BaseCommandHandler<AdvanceProjectStageCommand, AdvanceProjectStageResult>`.

## Request

```csharp
public sealed record AdvanceProjectStageCommand(
    Guid ProjectExternalId,
    long ActingUserId,
    long ActingUserIncubatorId,
    bool ActingUserIsGlobalAdmin
) : IBaseRequest<AdvanceProjectStageResult>;
```

### Field semantics

| Field                       | Required | Constraint                              |
|-----------------------------|----------|------------------------------------------|
| `ProjectExternalId`         | Yes      | Not `Guid.Empty`.                        |
| `ActingUserId`              | Yes      | `> 0`. Identity of the user performing advancement; stored in the completed `ProjectStage.AdvancedByUserId` via the domain method. |
| `ActingUserIncubatorId`     | Yes      | `>= 0`. Zero permitted only when `ActingUserIsGlobalAdmin == true`. |
| `ActingUserIsGlobalAdmin`   | Yes      | When true, the handler skips the incubator-scope check. |

### Validator (FluentValidation)

- `ProjectExternalId`: `NotEmpty().WithMessage("ProjectExternalId is required")`.
- `ActingUserId`: `GreaterThan(0).WithMessage("ActingUserId must be greater than 0")`.

Business-rule errors (not validator errors) are returned as typed failures below.

## Response

### Success

```csharp
public sealed record AdvanceProjectStageResult(
    StageType NewCurrentStageType,
    StageState NewCurrentStageState  // always InProgress on success
);
```

Returned via `Success(new AdvanceProjectStageResult(...))`.

### Failure codes

| Code                           | HTTP-surfaced meaning | Toast message (Spanish)                                                                 |
|--------------------------------|-----------------------|------------------------------------------------------------------------------------------|
| `ProjectNotFound`              | 404 → UI redirect     | `"El proyecto no existe o ya no es accesible."`                                          |
| `ProjectOutOfScope`            | 403                   | `"No tiene permisos para gestionar el ciclo de vida de este proyecto."`                 |
| `ProjectInactive`              | 409                   | `"El proyecto está inactivo. Active el proyecto antes de avanzar de etapa."`            |
| `StageNotInProgress`           | 409                   | `"La etapa actual no está en progreso. Actualice la página."`                           |
| `ProjectAlreadyClosed`         | 409                   | `"El proyecto ya está en la etapa final (Cierre)."`                                     |
| `LifecycleConcurrencyConflict` | 409                   | `"Otra operación modificó este proyecto. Actualice la página e intente de nuevo."`      |

## Handler flow

```
1. Load Project by ExternalId via IProjectRepository.GetByExternalIdAsync
   (tracked — we will mutate).
   → null ⇒ Failure(ProjectNotFound)

2. If !ActingUserIsGlobalAdmin AND project.IncubatorId != resolved internal
   incubator id for ActingUserIncubatorId
   → Failure(ProjectOutOfScope)

3. If !project.IsActive → Failure(ProjectInactive)

4. If project.CurrentStageState != StageState.InProgress
   → Failure(StageNotInProgress)

5. If project.CurrentStageType is the max enum value (Closure)
   → Failure(ProjectAlreadyClosed)

6. project.AdvanceStage(ActingUserId, timeProvider.UtcNow)
   projectRepository.Update(project)
   try: unitOfWork.SaveChangesAsync(ct)
   catch DbUpdateConcurrencyException
       → Failure(LifecycleConcurrencyConflict)

7. Return Success(new AdvanceProjectStageResult(
       project.CurrentStageType,
       project.CurrentStageState))
```

**Note on step 2**: `ActingUserIncubatorId` is an internal id (long). The scope check is a direct equality against `project.IncubatorId`. If the web layer only carries `ExternalId` for incubators in some contexts, the command stays on internal id — the controller is responsible for providing it from the authenticated user's active-incubator claim.

## Logging (via `[LoggerMessage]`)

- `LogProjectNotFound(Guid externalId)` — Warning
- `LogAdvanceOutOfScope(Guid externalId, long userId)` — Warning
- `LogStageAdvanced(Guid externalId, StageType from, StageType to, long userId)` — Information
- `LogConcurrencyConflict(Guid externalId, long userId)` — Warning

## Idempotency & retries

Not idempotent — advancing twice advances twice. Concurrency conflicts are surfaced so the caller can refresh and retry if appropriate. The UI page reload after a success intentionally prevents a double submission via the browser back button.

## Permissions

Enforced at the controller ingress via `[Authorize(Roles = "ProjectCoordinator,IncubatorAdmin,GlobalAdmin")]`. The handler additionally enforces tenant-scope isolation (step 2). No `CheckPermissionQuery` is used in this release (see research R7).
