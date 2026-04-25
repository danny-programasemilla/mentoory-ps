# Phase 1 Data Model: Project Lifecycle Application + UI Completion

**Feature**: 016-project-lifecycle-finish
**Date**: 2026-04-18

This document catalogs every data artifact this feature introduces or modifies: domain entities, DTOs, view models, enums, and the one schema change. Persisted structures, transient transport structures, and view-only structures are kept in separate sections.

---

## 1. Persisted / Domain Model Changes

### 1.1 `Project` aggregate (existing — Tenant domain)

**Modification**: Add one property, map as EF concurrency token.

| Property        | Type      | Notes |
|-----------------|-----------|-------|
| `RowVersion`    | `byte[]`  | NEW. Populated by SQL Server `ROWVERSION`. Private setter. Used by EF as the aggregate's optimistic-concurrency token per R1. |

**Invariants** (unchanged from existing code):
- Exactly seven `ProjectStage` records per project (one per `StageType` value).
- `CurrentStageType` and `CurrentStageState` are kept in sync with the corresponding row in `_stages` by `Project.AdvanceStage()`.
- Advancement is only valid when `CurrentStageState == InProgress` **and** `CurrentStageType` is not the last enum value; the existing domain method enforces these.

**State transition** (unchanged — documented for completeness):

```
            (factory)
               │
               ▼
        Registration (InProgress)
               │ AdvanceStage
               ▼
        Forms (InProgress)  ◄── Registration (Completed)
               │ AdvanceStage
               ▼
        Analysis (InProgress) ◄── Forms (Completed)
               │ AdvanceStage
               ▼
    LearningAssignment (InProgress) ◄── Analysis (Completed)
               │ AdvanceStage
               ▼
        Mentoring (InProgress) ◄── LearningAssignment (Completed)
               │ AdvanceStage
               ▼
     FinalEvaluation (InProgress) ◄── Mentoring (Completed)
               │ AdvanceStage
               ▼
        Closure (InProgress) ◄── FinalEvaluation (Completed)
               │ AdvanceStage  ←── REJECTED (already at final stage)
```

### 1.2 `ProjectStage` entity (existing — Tenant domain)

**No changes.** Fields `StageType`, `State`, `StartedAtUtc`, `CompletedAtUtc`, `AdvancedByUserId` already exist and are already set by `Project.AdvanceStage(long advancedByUserId, DateTime utcNow)`. The feature only needs to surface them through the new query.

### 1.3 `StageType` enum (existing — Tenant domain)

**No changes.** Seven ordered values, used unchanged.

### 1.4 `StageState` enum (existing — Tenant domain)

**No changes.** `NotStarted`, `InProgress`, `Completed`.

### 1.5 Schema — `tenant.Projects` table

**Modification**: Add one column in `Mentoory.Db/tenant/Tables/Projects.sql`:

```sql
[RowVersion] ROWVERSION NOT NULL
```

No other column changes. No index changes. No PostDeployment script needed (SQL Server populates `ROWVERSION` for existing rows implicitly on the next UPDATE).

---

## 2. New Enums

### 2.1 `StageGatedAction` (NEW — `Mentoory.Access.Application.StageActions`)

```csharp
public enum StageGatedAction
{
    DiagnosticForms = 0,        // Gated to Forms
    AnswerCorrection = 1,       // Gated to Analysis
    LearningAssignment = 2,     // Gated to LearningAssignment
    MentoringCoordination = 3,  // Gated to Mentoring
    FinalEvaluation = 4,        // Gated to FinalEvaluation
    Closure = 5,                // Gated to Closure
}
```

**Rationale**: Closed set of coordination-area actions whose visibility and accessibility are governed by the project's current stage. New actions require a new enum member **and** a registry entry in one commit (R2).

### 2.2 `StageGatedActionState` (NEW — `Mentoory.Access.Application.StageActions`)

```csharp
public enum StageGatedActionState
{
    Available = 0,   // Current stage matches the gating stage
    Locked = 1,      // Gating stage is in the future
    Past = 2,        // Gating stage is already completed
}
```

---

## 3. New Static Registry

### 3.1 `StageActionRegistry` (NEW — `Mentoory.Access.Application.StageActions`)

**Purpose**: Single authoritative mapping from `StageType` → set of `StageGatedAction` enums available in that stage. Also provides the inverse lookup (`StageGatedAction` → `StageType`) used by the web filter.

**Shape**:
- `IReadOnlyDictionary<StageType, IReadOnlySet<StageGatedAction>> ActionsByStage` — compile-time immutable, populated once.
- `StageType GetGatingStage(StageGatedAction action)` — returns the stage in which the action is available.
- `StageGatedActionState GetState(StageType currentStage, StageGatedAction action)` — returns `Available` / `Locked` / `Past` based on the project's current stage and the action's gating stage (uses the numeric order of `StageType`).

**Initial data**:

| Stage              | Available actions         |
|--------------------|---------------------------|
| Registration       | (none)                    |
| Forms              | `DiagnosticForms`         |
| Analysis           | `AnswerCorrection`        |
| LearningAssignment | `LearningAssignment`      |
| Mentoring          | `MentoringCoordination`   |
| FinalEvaluation    | `FinalEvaluation`         |
| Closure            | `Closure`                 |

---

## 4. DTOs (Application layer — transport shape)

### 4.1 `ProjectLifecycleDto` (NEW — `Mentoory.Tenant.Application.Projects.Queries.GetProjectLifecycle`)

| Field                | Type                                  | Notes |
|----------------------|---------------------------------------|-------|
| `ExternalId`         | `Guid`                                | |
| `Name`               | `string`                              | |
| `Description`        | `string?`                             | |
| `IncubatorExternalId`| `Guid`                                | For tenant-isolation display / deep links |
| `IncubatorName`      | `string`                              | |
| `IsActive`           | `bool`                                | |
| `CurrentStageType`   | `StageType`                           | |
| `CurrentStageState`  | `StageState`                          | |
| `CanAdvance`         | `bool`                                | True iff advancement would be accepted (R8) |
| `CannotAdvanceReason`| `string?`                             | Null when `CanAdvance`; otherwise a Spanish explanation (`"El proyecto ya está en la etapa final."`, `"El proyecto está inactivo."`, `"La etapa actual no está en progreso."`) |
| `Stages`             | `IReadOnlyList<ProjectLifecycleStageDto>` | 7 items in canonical order |
| `Actions`            | `IReadOnlyList<StageActionDto>`       | All registry actions with their current state for this project |

### 4.2 `ProjectLifecycleStageDto` (NEW)

| Field              | Type          | Notes |
|--------------------|---------------|-------|
| `StageType`        | `StageType`   | |
| `DisplayName`      | `string`      | Spanish (from `StageTypeDisplay`) — resolved server-side to avoid Razor duplication |
| `State`            | `StageState`  | |
| `StartedAtUtc`     | `DateTime?`   | |
| `CompletedAtUtc`   | `DateTime?`   | |
| `AdvancedByDisplay`| `string?`     | User name or email of the coordinator who started the stage. Null when `State == NotStarted`. |

### 4.3 `StageActionDto` (NEW)

| Field                    | Type                      | Notes |
|--------------------------|---------------------------|-------|
| `Action`                 | `StageGatedAction`        | |
| `DisplayName`            | `string`                  | Spanish name of the action (e.g., "Diagnósticos", "Corrección de Respuestas") |
| `GatingStageType`        | `StageType`               | |
| `GatingStageDisplayName` | `string`                  | Spanish |
| `State`                  | `StageGatedActionState`   | `Available` / `Locked` / `Past` |
| `LinkUrl`                | `string`                  | Server-resolved URL to the action's entry point (e.g., `/Coordination/Diagnostics`). For locked actions still populated — the filter rejects the request if followed. |

---

## 5. View Models (Web layer)

### 5.1 `LifecycleProjectViewModel` (NEW — `Mentoory.Web.Areas.Coordination.Models`)

Thin wrapper over `ProjectLifecycleDto` that the Razor view binds to. Uses the same field names to keep mapping trivial; Mapperly maps DTO → view model. View helpers (CSS class for state, icon class for action state) live in the view itself or in `StageTypeDisplay` / `StageActionDisplay` extension methods.

### 5.2 `LifecycleStageViewModel` / `StageActionViewModel`

One-to-one with the DTO shapes, same fields. The view model layer exists so the view can be tested/rendered independently of Application DTOs if needed (consistent with existing patterns in `Areas/*/Models`).

---

## 6. Commands (Application layer)

### 6.1 `AdvanceProjectStageCommand` (NEW)

```csharp
public sealed record AdvanceProjectStageCommand(
    Guid ProjectExternalId,
    long ActingUserId,
    long ActingUserIncubatorId,
    bool ActingUserIsGlobalAdmin
) : IBaseRequest<AdvanceProjectStageResult>;

public sealed record AdvanceProjectStageResult(
    StageType NewCurrentStageType,
    StageState NewCurrentStageState
);
```

**Notes**:
- The command carries the acting user's identity and incubator as parameters rather than injecting an ambient user service into the handler — consistent with the project's approach elsewhere.
- `ActingUserIsGlobalAdmin` short-circuits tenant isolation for `GlobalAdmin` (constitution X — role hierarchy).
- Result returns the new current stage so the controller can surface a precise success message ("Proyecto avanzado a *Formularios*.").

### 6.2 Command validation (`AdvanceProjectStageValidator`)

- `ProjectExternalId` must not be `Guid.Empty`.
- `ActingUserId > 0`.
- `ActingUserIncubatorId >= 0` (zero allowed for GlobalAdmin operating in global scope).

Business-rule validation (project exists, user in-scope, stage state allows advance) lives in the handler, not the validator — consistent with platform pattern.

---

## 7. Queries (Application layer)

### 7.1 `GetProjectLifecycleQuery`

```csharp
public sealed record GetProjectLifecycleQuery(
    Guid ProjectExternalId,
    long ActingUserIncubatorId,
    bool ActingUserIsGlobalAdmin
) : IBaseRequest<ProjectLifecycleDto>;
```

**Handler behavior**:
1. Load `Project` by `ExternalId` with stages (`AsNoTracking`, `Include("_stages")`).
2. If null → return `Failure(NotFound)`.
3. If `!ActingUserIsGlobalAdmin && project.IncubatorId != ActingUserIncubatorId` → return `Failure(Forbidden)`.
4. Map to `ProjectLifecycleDto` via Mapperly + manual projection for the `CanAdvance` + `CannotAdvanceReason` + `Actions` composition.

---

## 8. New Failure Codes

Added to `ResultErrorCodes` (or feature-local constants if the platform uses per-handler error codes):

| Code                         | Meaning                                                           |
|------------------------------|-------------------------------------------------------------------|
| `ProjectNotFound`            | Project with the given `ExternalId` does not exist.               |
| `ProjectOutOfScope`          | Acting user's incubator context does not contain this project.    |
| `ProjectInactive`            | Project is inactive; lifecycle operations are refused.            |
| `StageNotInProgress`         | Current stage is not `InProgress`; advancement refused.           |
| `ProjectAlreadyClosed`       | Current stage is `Closure`; no further stages exist.              |
| `LifecycleConcurrencyConflict` | Row version mismatch; another coordinator advanced concurrently. |

Every code maps in the controller to a distinct Spanish toast message per the spec (FR-023).

---

## 9. Contracts between layers — summary diagram

```
Razor View  ◄── LifecycleProjectViewModel ◄── Mapperly ◄── ProjectLifecycleDto
                                                              ▲
                                                              │ handler-composed
                                                              │
                           GetProjectLifecycleHandler  ◄─── Project aggregate
                                                              + StageActionRegistry
                                                              + StageTypeDisplay

Form POST   ──► Controller ──► AdvanceProjectStageCommand
                                     │
                                     ▼
                     AdvanceProjectStageHandler
                          │ Project.AdvanceStage(userId, timeProvider.UtcNow)
                          ▼
                     IProjectRepository.Update + UnitOfWork
                          │
                          ▼
                     EF Core (RowVersion check) → SQL Server
```
