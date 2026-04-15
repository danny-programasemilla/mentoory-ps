# Data Model: Configurable Stage Pipeline & Flexible Diagnosis Module

**Date**: 2026-04-14
**Branch**: `014-diagnosis-stage-pipeline`

## Entity Relationship Overview

```
BusinessIncubator (1) ──── (*) Project (1) ──── (*) ProjectStage
                                    │                    │
                                    │                    │ (ProjectStageId, by ID)
                                    │                    ▼
                                    ├──── (*) ProjectForm ◄──── StageFormAssignment (*)
                                    │         │                      │
                                    │         │ (ProjectFormId)      │
                                    │         │                      ├──── (*) AssignedQuestion
                                    │         │                      │
                                    │         │                      ▼
                                    │         │              DiagnosticResponse (*)
                                    │         │                      │
                                    │         ▼                      ├──── (*) QuestionResponse
                                    │    Question (*)                │         │
                                    │         │                      │         └──── (*) AnswerCorrection
                                    │         └──── (*) AnswerOption  │
                                    │         └──── (*) FollowUpQuestion
                                    │
                                    └──── FormTemplate (global, via clone)
                                              └──── QuestionTemplate
                                                      └──── AnswerOptionTemplate
```

## Modified Entities

### ProjectStage (Tenant Domain — `[tenant].ProjectStages`)

**Action**: MODIFY existing entity

| Field | Type | Nullable | Change | Notes |
|-------|------|----------|--------|-------|
| Id | long | No | KEEP | PK, auto-increment |
| **ExternalId** | **uniqueidentifier** | **No** | **ADD** | External-facing GUID |
| ProjectId | long | No | KEEP | FK to Projects |
| StageType | int | No | MODIFY | Values change: 0=Registration, 1=Diagnosis, 2=Mentorship, 3=Closure |
| State | int | No | KEEP | 0=NotStarted, 1=InProgress, 2=Completed |
| **Position** | **int** | **No** | **ADD** | Order in pipeline (0-based) |
| **DisplayName** | **nvarchar(200)** | **No** | **ADD** | Auto-generated or admin-overridden |
| **PlannedStartDate** | **datetime2** | **Yes** | **ADD** | Informational only |
| **PlannedEndDate** | **datetime2** | **Yes** | **ADD** | Informational only |
| StartedAtUtc | datetime2 | Yes | KEEP | Set on advancement |
| CompletedAtUtc | datetime2 | Yes | KEEP | Set on advancement |
| AdvancedByUserId | long | Yes | KEEP | Who advanced it |

**Indexes**:
- UNIQUE: (ProjectId, Position) — no two stages at same position
- UNIQUE: ExternalId
- IX_ProjectStages_ProjectId (existing, keep)

**Validation Rules**:
- Position >= 0
- DisplayName max 200 chars, non-empty
- First stage (Position 0) must be StageType.Registration
- Last stage must be StageType.Closure

### StageType Enum (Tenant Domain)

**Action**: MODIFY — replace values

| Old Value | Old Int | New Value | New Int |
|-----------|---------|-----------|---------|
| Registration | 0 | Registration | 0 |
| Forms | 1 | Diagnosis | 1 |
| Analysis | 2 | Mentorship | 2 |
| LearningAssignment | 3 | Closure | 3 |
| Mentoring | 4 | *(deleted)* | — |
| FinalEvaluation | 5 | *(deleted)* | — |
| Closure | 6 | *(deleted)* | — |

### Project Aggregate (Tenant Domain)

**Action**: MODIFY — refactor stage management methods

**New/Modified Methods**:
- `Create()`: Initialize default pipeline [Reg(0), Diag(1), Mentorship(2), Diag(3), Closure(4)] with auto-generated DisplayNames
- `AddStage(StageType type, int position, DateTime utcNow)`: Insert stage, shift positions of subsequent stages
- `RemoveStage(long stageId)`: Remove stage, shift positions. Validate not first/last if Registration/Closure
- `ReorderStages(IReadOnlyList<long> orderedStageIds)`: Replace all positions based on ordered ID list
- `RenameStage(long stageId, string displayName)`: Update display name
- `AdvanceStage(long userId, DateTime utcNow)`: Advance to next stage by position (not by type)

**DisplayName Auto-Generation**: When multiple stages of same type exist, append ordinal: "Diagnóstico 1", "Diagnóstico 2". Single instances use base name: "Registro", "Diagnóstico", "Mentoría", "Cierre".

### QuestionTemplate (Diagnostic Domain — `[diagnostic].QuestionTemplates`)

**Action**: MODIFY — remove StageApplicability

| Field | Change |
|-------|--------|
| StageApplicability (int) | DELETE column |

### Question (Diagnostic Domain — `[diagnostic].Questions`)

**Action**: MODIFY — remove StageApplicability

| Field | Change |
|-------|--------|
| StageApplicability (int) | DELETE column |

### DiagnosticResponse (Diagnostic Domain — `[diagnostic].DiagnosticResponses`)

**Action**: MODIFY — replace EvaluationStage with StageFormAssignmentId

| Field | Type | Nullable | Change | Notes |
|-------|------|----------|--------|-------|
| EvaluationStage | int | — | DELETE | Replaced by StageFormAssignmentId |
| **StageFormAssignmentId** | **long** | **No** | **ADD** | FK to StageFormAssignments |

**Indexes**:
- UNIQUE: (StageFormAssignmentId, EntrepreneurUserId) — one response per entrepreneur per assignment

### EvaluationStage Enum (Diagnostic Domain)

**Action**: DELETE entirely

### StageApplicability Enum (Diagnostic Domain)

**Action**: DELETE entirely

## New Entities

### StageFormAssignment (Diagnostic Domain — `[diagnostic].StageFormAssignments`)

**Action**: NEW aggregate root

| Field | Type | Nullable | Notes |
|-------|------|----------|-------|
| Id | long | No | PK, auto-increment |
| ExternalId | uniqueidentifier | No | External-facing GUID |
| ProjectId | long | No | Denormalized for query performance |
| IncubatorId | long | No | Multi-tenant filter |
| ProjectStageId | long | No | Cross-domain ref to [tenant].ProjectStages |
| ProjectFormId | long | No | FK to [diagnostic].ProjectForms |
| IsActive | bit | No | Default true. Soft-deactivation |
| CreatedAtUtc | datetime2 | No | Factory method sets via parameter |

**Indexes**:
- UNIQUE: ExternalId
- IX_StageFormAssignments_ProjectStageId: (ProjectStageId) — list assignments per stage
- IX_StageFormAssignments_ProjectFormId: (ProjectFormId) — list stages using a form
- IX_StageFormAssignments_IncubatorId: (IncubatorId) — tenant filter

**Multi-tenant Query Filter**: `entity.IncubatorId == _tenantContext.CurrentIncubatorId`

**Domain Methods**:
- `Create(projectId, incubatorId, projectStageId, projectFormId, selectedQuestionIds, utcNow)`: Factory. Validates at least one question selected.
- `UpdateQuestionSelection(IReadOnlyList<long> questionIds)`: Replace full selection. Validates non-empty.
- `Deactivate()`: Set IsActive = false. Idempotent.

### AssignedQuestion (Diagnostic Domain — `[diagnostic].AssignedQuestions`)

**Action**: NEW child entity of StageFormAssignment

| Field | Type | Nullable | Notes |
|-------|------|----------|-------|
| Id | long | No | PK, auto-increment |
| StageFormAssignmentId | long | No | FK to StageFormAssignments (cascade delete) |
| QuestionId | long | No | Ref to [diagnostic].Questions.Id |
| SortOrder | int | No | Display order within assignment |

**Indexes**:
- UNIQUE: (StageFormAssignmentId, QuestionId) — no duplicate question per assignment
- IX_AssignedQuestions_QuestionId: (QuestionId) — reverse lookup

### ScoreDelta (Diagnostic Domain — Value Object)

**Action**: NEW value object (not persisted, computed in queries)

| Property | Type | Notes |
|----------|------|-------|
| TopicId | long | Reference to topic |
| PreviousScore | decimal | Score from earlier execution |
| CurrentScore | decimal | Score from later execution |
| Delta | decimal | CurrentScore - PreviousScore |
| PercentageChange | decimal | (Delta / PreviousScore) * 100, or 0 if PreviousScore is 0 |

## Permission Enum Additions (Access Domain)

**Action**: ADD 3 new values

| Permission | Value | Description |
|------------|-------|-------------|
| ManageProjectPipeline | 304 | Add, remove, reorder, rename stages |
| AssignDiagnosticForms | 305 | Assign forms to stages, select questions |
| ViewDiagnosticComparison | 404 | View comparison and timeline views |

**Role Mapping**:
- GlobalAdmin: All permissions (implicit)
- IncubatorAdmin: ManageProjectPipeline, AssignDiagnosticForms, ViewDiagnosticComparison
- ProjectCoordinator: ManageProjectPipeline, AssignDiagnosticForms, ViewDiagnosticComparison
- Mentor: ViewDiagnosticComparison
- Entrepreneur: (no new permissions — implicit access to own diagnosis via existing CompleteDiagnostic)

## State Transitions

### ProjectStage State Machine

```
  NotStarted ──(AdvanceStage)──► InProgress ──(AdvanceStage)──► Completed
       ▲                              │
       │                              │
  (initial state)              (only one stage
   for all stages               InProgress at
   except first)                a time)
```

- On project creation: first stage (Registration) set to InProgress, all others NotStarted
- AdvanceStage: current InProgress → Completed, next by position → InProgress
- Last stage (Closure) completion: project marked complete

### StageFormAssignment Lifecycle

```
  Created (IsActive=true) ──(Deactivate)──► Deactivated (IsActive=false)
```

- Created when admin assigns form to stage
- Deactivated when admin removes assignment (responses preserved)
- No reactivation — create new assignment instead

### DiagnosticResponse Lifecycle

```
  Created ──(AddResponse * N)──► Responses Added ──(MarkAsCompleted)──► Completed
                                                                            │
                                                                    (CorrectAnswer)
                                                                            │
                                                                            ▼
                                                                    Correction Recorded
```

- Created when entrepreneur starts filling questionnaire
- MarkAsCompleted: one-way, idempotent
- CorrectAnswer: available at any time after completion, records audit trail

## SSDT Schema Changes Summary

### New Tables
- `[diagnostic].StageFormAssignments` — NEW
- `[diagnostic].AssignedQuestions` — NEW

### Modified Tables
- `[tenant].ProjectStages` — ADD: ExternalId, Position, DisplayName, PlannedStartDate, PlannedEndDate
- `[diagnostic].DiagnosticResponses` — ADD: StageFormAssignmentId, DROP: EvaluationStage
- `[diagnostic].QuestionTemplates` — DROP: StageApplicability
- `[diagnostic].Questions` — DROP: StageApplicability

### PostDeployment Scripts
- `019.MigrateStageTypes.sql` — Migrate existing stage type values (7→4), add positions, generate display names
- `020.SeedNewPermissions.sql` — Seed ManageProjectPipeline, AssignDiagnosticForms, ViewDiagnosticComparison with role mappings
