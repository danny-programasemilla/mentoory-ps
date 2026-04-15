# Implementation Plan: Pipeline Editor UX Polish

**Branch**: `015-pipeline-editor-ux-polish` | **Date**: 2026-04-14 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/015-pipeline-editor-ux-polish/spec.md`

## Summary

Fix 4 UX issues in the pipeline editor view: (1) wire drag-and-drop to the existing Reorder AJAX endpoint with an explicit save button, (2) replace full-color badges with status-dot indicators, (3) convert list layout to CSS grid for vertical alignment, (4) add inline form names for Diagnosis stages via a new cross-domain query.

## Technical Context

**Language/Version**: C# / .NET 10.0 (SDK 10.0.0-preview), JavaScript (vanilla, ES5-compatible)
**Primary Dependencies**: ASP.NET Core MVC, MediatR 14.1, EF Core 10.x, Tabler v1.4.0 (Bootstrap 5), Tabler Icons Webfont v3.41.1
**Storage**: SQL Server (read-only queries — no schema changes)
**Testing**: xUnit, FluentAssertions, Moq
**Target Platform**: Web (modern evergreen browsers)
**Project Type**: Web application (modular monolith)
**Performance Goals**: Pipeline view loads in < 1s, reorder save in < 2s
**Constraints**: Zero compiler warnings, Spanish UI, vanilla JS only, no domain entity changes
**Scale/Scope**: Single view (ProjectPipeline/Index), 4 focused UI fixes

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| # | Gate | Status |
|---|------|--------|
| I | Clean Architecture layers respected | PASS — Query in Application, repository in Infrastructure, view in Web. Controller orchestrates two queries, never bypasses Application. |
| II | CQRS patterns followed | PASS — New query uses `IBaseRequest<TResult>` + `BaseCommandHandler<TRequest, TResult>`. No commands changed. |
| III | DDD constraints respected | PASS — No domain entity changes. Cross-domain read via separate query, not cross-aggregate reference. |
| V | Zero warnings policy | PASS — SC-005 enforces this. |
| VI | DateTime handling | N/A — No DateTime logic added. |
| VII | Naming conventions | PASS — `GetStageFormNamesQuery`, `GetStageFormNamesHandler`, `StageFormNamesDto`. |
| VIII | File organization | PASS — JS in `/wwwroot/js/`, one class per file. |
| IX | Spanish-first UI | PASS — "Guardar Orden", "Sin formulario", all error messages in Spanish. |
| X | Role hierarchy | N/A — No authorization changes. Existing `[Authorize(Roles = "ProjectCoordinator,IncubatorAdmin,GlobalAdmin")]` unchanged. |
| XI | SSDT/DACPAC | N/A — No database schema changes. |

**All gates PASS.**

## Project Structure

### Documentation (this feature)

```text
specs/015-pipeline-editor-ux-polish/
├── spec.md
├── plan.md              # This file
├── research.md
├── data-model.md
├── REVIEW-SPEC.md
├── review_brief.md
└── checklists/
    └── requirements.md
```

### Source Code (repository root)

```text
# Files MODIFIED (existing)
Mentoory.Web/Areas/Coordination/Views/ProjectPipeline/Index.cshtml    # FR-001..FR-012: full view rewrite
Mentoory.Web/Areas/Coordination/Models/ProjectPipelineViewModels.cs   # FR-008: add AssignedFormNames
Mentoory.Web/Areas/Coordination/Controllers/ProjectPipelineController.cs  # FR-004/FR-009: orchestrate 2 queries
Mentoory.Web/wwwroot/js/pipeline-editor.js                            # FR-001..FR-004: save button + order tracking
Mentoory.Web/wwwroot/css/site.css                                     # FR-007: CSS grid styles
Mentoory.Tenant.Application/Queries/GetProjectPipeline/ProjectPipelineDto.cs   # FR-008: add StageId for join
Mentoory.Tenant.Application/Queries/GetProjectPipeline/GetProjectPipelineHandler.cs  # FR-008: populate StageId

# Files CREATED (new)
Mentoory.Diagnostic.Application/Queries/GetStageFormNames/GetStageFormNamesQuery.cs     # FR-009: new query
Mentoory.Diagnostic.Application/Queries/GetStageFormNames/GetStageFormNamesHandler.cs   # FR-009: handler
Mentoory.Diagnostic.Application/Queries/GetStageFormNames/StageFormNamesDto.cs          # FR-009: DTO
```

**Structure Decision**: No new projects or directories beyond the standard Application/Queries path. All changes fit existing structure.

## Complexity Tracking

No constitution violations to justify.

---

## Phase 0: Research

### R-1: Cross-domain query for form names

**Decision**: Create a new `GetStageFormNamesQuery(long projectId)` in `Mentoory.Diagnostic.Application` that returns a mapping of `ProjectStageId (internal) → List<string> formNames`.

**Rationale**: Keeps domain boundaries clean. The Diagnostic domain owns StageFormAssignment and ProjectForm — it should be the one querying them. The Tenant pipeline query stays unchanged. The controller orchestrates both queries and merges results.

**Bridge**: `PipelineStageDto` gains an internal `StageId` property (the `ProjectStage.Id`). This is NOT used in routes (routes use ExternalId) — it's a DTO join key only. The controller builds a `stageId → externalId` map from the pipeline DTO, then uses it to assign form names to the correct view model entries.

**Alternatives considered**:
- Cross-domain join in Tenant handler → violates Clean Architecture (Tenant.Application would reference Diagnostic repositories)
- Lazy AJAX load of form names → extra roundtrip, flash of empty content, more JS complexity
- Shared read model / materialized view → overkill for a single view's read needs

### R-2: Tabler status-dot icon

**Decision**: Use `ti ti-point-filled` from Tabler Icons Webfont v3.41.1 for status dots. Fallback: use a CSS-only dot (small `<span>` with `border-radius: 50%` and background color).

**Rationale**: The icon font is already loaded project-wide. Available icons include `ti-point-filled`, `ti-circle-dot`, and `ti-circle`. The `ti-point-filled` is the smallest and most suitable for inline status indicators.

### R-3: Anti-forgery token in AJAX

**Decision**: Reuse the existing `getAntiForgeryToken()` helper from `site.js`. Send token via `RequestVerificationToken` header (established project pattern). The Reorder form already has `@Html.AntiForgeryToken()` — the JS will read the token from the nearest form's hidden input.

**Pattern** (from `context-switcher.js`):
```javascript
fetch(url, {
    method: 'POST',
    headers: {
        'Content-Type': 'application/json',
        'RequestVerificationToken': getAntiForgeryToken()
    },
    body: JSON.stringify(payload)
});
```

### R-4: CSS grid column specification

**Decision**: 7-column grid applied to `#pipelineList` container:
```css
.pipeline-grid {
    display: grid;
    grid-template-columns: 24px 120px 100px 1fr 1fr auto auto;
    gap: 0;
    align-items: center;
}
```
Columns: handle (24px) | state (120px) | type (100px) | name (1fr) | form (1fr) | dates (auto) | actions (auto).

Each row is a `div` with `display: contents` so children participate in the parent grid.

**Rationale**: CSS grid with `display: contents` on rows gives proper vertical alignment without converting to `<table>`. Keeps the card-like border/padding feel on each row wrapper.

---

## Phase 1: Design

### Data Model Changes

No database schema changes. DTO-level extensions only:

**`PipelineStageDto`** — add `long StageId` (internal ID for controller-level joining):
```
PipelineStageDto(
    long StageId,              ← NEW (internal join key)
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

**`StageFormNamesDto`** — new DTO in Diagnostic.Application:
```
StageFormNamesDto(
    IReadOnlyDictionary<long, IReadOnlyList<string>> FormNamesByStageId)
```
Key = ProjectStageId (internal), Value = list of active form names for that stage.

**`StageViewModel`** — add `List<string> AssignedFormNames`:
```
StageViewModel {
    ... existing properties ...
    List<string> AssignedFormNames { get; set; }    ← NEW
}
```

### Implementation Phases

#### Phase A: Backend — Extend DTOs and create new query (FR-008, FR-009)

1. **Extend `PipelineStageDto`**: Add `long StageId` as first positional parameter.
2. **Update `GetProjectPipelineHandler`**: Pass `s.Id` into the new `StageId` parameter.
3. **Create `GetStageFormNamesQuery`**: `sealed record GetStageFormNamesQuery(long ProjectId) : IBaseRequest<StageFormNamesDto>`
4. **Create `GetStageFormNamesHandler`**: Inject `IStageFormAssignmentRepository` + `IProjectFormRepository`. Query active assignments by projectId, batch-load form names, group by `ProjectStageId`, return `StageFormNamesDto`.
5. **Extend `StageViewModel`**: Add `List<string> AssignedFormNames`.
6. **Update `ProjectPipelineController.Index`**: Dispatch both queries in parallel (`Task.WhenAll`), merge form names into view model using StageId → ExternalId mapping.
7. **Build and verify zero warnings.**

#### Phase B: Frontend — CSS grid layout (FR-007)

1. **Add `.pipeline-grid` styles** to `site.css`: 7-column grid definition.
2. **Add `.pipeline-row` styles**: `display: contents` for grid participation, with a wrapper div for row borders/background.
3. **Rewrite `Index.cshtml`**: Replace `list-group` with grid markup. Each row becomes a grid-participating structure with 7 cells.
4. **Visual test**: Verify all columns align across rows with varying content lengths.

#### Phase C: Frontend — Status dots (FR-005, FR-006)

1. **Replace badge markup** in `Index.cshtml`: Change `<span class="badge ...">` to `<span class="status-dot ..."><i class="ti ti-point-filled"></i> Label</span>`.
2. **Add status-dot CSS**: Colored dots (gray/blue/green), "En Progreso" gets `font-weight: 600` and slightly larger dot.
3. **Remove old `GetStateBadgeClass` helper**, replace with `GetStatusDotClass`.

#### Phase D: Frontend — Inline form names (FR-010, FR-011, FR-012)

1. **Add form name cell** to each grid row in `Index.cshtml`.
2. **For Diagnosis stages**: Render `stage.AssignedFormNames` comma-joined, or "Sin formulario" in muted text if empty.
3. **For non-Diagnosis stages**: Render empty cell.

#### Phase E: Frontend — Save button for reorder (FR-001, FR-002, FR-003, FR-004)

1. **Add "Guardar Orden" button** to header area (next to "Agregar Etapa"), hidden by default (`d-none`).
2. **Update `pipeline-editor.js`**:
   - On `DOMContentLoaded`, capture original order as array of ExternalIds.
   - After each drop, compare current DOM order to original — show/hide button.
   - On button click: collect ordered ExternalIds, POST via `fetch` to `Reorder` action with `RequestVerificationToken` header.
   - On success: hide button, update `originalOrder` to new order, show brief success toast.
   - On error: show error alert, keep button visible.
3. **Add anti-forgery token source**: Ensure a form with `@Html.AntiForgeryToken()` exists on the page (already present in existing forms).

#### Phase F: Verification

1. **Build**: `dotnet build` — zero warnings.
2. **Manual test**: Start dev server, navigate to pipeline editor, verify all 4 fixes.
3. **Test matrix**:
   - Reorder + save + refresh → order persists
   - Reorder back to original → button hides
   - Save failure (disconnect network) → error shown, button stays
   - Status dots visible and distinct for all 3 states
   - Columns align across all rows
   - Form names visible for Diagnosis stages
   - "Sin formulario" for unassigned Diagnosis stages
   - Non-Diagnosis stages have empty form column
   - 2-stage pipeline (Reg + Closure only) → no drag handles, grid renders
