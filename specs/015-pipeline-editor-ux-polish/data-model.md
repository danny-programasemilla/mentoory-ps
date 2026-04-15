# Data Model: Pipeline Editor UX Polish

**Date**: 2026-04-14

## Overview

No database schema changes. All changes are DTO-level extensions for the view layer.

## DTO Extensions

### PipelineStageDto (MODIFIED)

**File**: `Mentoory.Tenant.Application/Queries/GetProjectPipeline/ProjectPipelineDto.cs`

```
PipelineStageDto(
    long StageId,              ← NEW (internal join key, not for routes)
    Guid ExternalId,
    string StageType,
    string State,
    int Position,
    string DisplayName,
    DateTime? PlannedStartDate,
    DateTime? PlannedEndDate,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc)
```

**Notes**:
- `StageId` is the internal `ProjectStage.Id`, added to enable controller-level joining with Diagnostic domain data
- NOT used in routes or exposed to the client — purely a server-side join key

### StageFormNamesDto (NEW)

**File**: `Mentoory.Diagnostic.Application/Queries/GetStageFormNames/StageFormNamesDto.cs`

```
StageFormNamesDto(
    IReadOnlyDictionary<long, IReadOnlyList<string>> FormNamesByStageId)
```

**Notes**:
- Key = `ProjectStageId` (internal ID matching `StageFormAssignment.ProjectStageId`)
- Value = list of form names (from `ProjectForm.Name`) for active assignments only
- Inactive assignments are excluded
- If a form is deleted but assignment still references it, that entry is skipped

### StageViewModel (MODIFIED)

**File**: `Mentoory.Web/Areas/Coordination/Models/ProjectPipelineViewModels.cs`

```
StageViewModel {
    ... existing properties ...
    List<string> AssignedFormNames { get; set; } = new();   ← NEW
}
```

**Notes**:
- Populated by controller after merging pipeline and form-names queries
- Empty list for non-Diagnosis stages
- Contains form names for Diagnosis stages (or empty if no forms assigned)

## Entity Relationships (Read-Only)

```
ProjectStage (Tenant domain)
  └── StageFormAssignment (Diagnostic domain)  [via ProjectStageId]
        └── ProjectForm (Diagnostic domain)     [via ProjectFormId]
```

The pipeline view reads this chain to display form names inline. No write operations cross domain boundaries.

## Query Flow

```
Controller.Index()
  ├── GetProjectPipelineQuery(projectId)         → Tenant domain
  │     └── returns ProjectPipelineDto with stages (including StageId)
  │
  └── GetStageFormNamesQuery(projectId)           → Diagnostic domain
        └── returns StageFormNamesDto (stageId → formNames)
  
  Controller merges: stageId from pipeline → formNames from diagnostic → StageViewModel.AssignedFormNames
```
