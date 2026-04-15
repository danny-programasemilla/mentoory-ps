# Research: Pipeline Editor UX Polish

**Date**: 2026-04-14

## R-1: Cross-domain query for form names

**Decision**: New `GetStageFormNamesQuery(long projectId)` in `Mentoory.Diagnostic.Application`. Returns `Dictionary<long, List<string>>` keyed by ProjectStageId (internal).

**Rationale**: Keeps domain boundaries clean — Diagnostic owns StageFormAssignment and ProjectForm. Controller orchestrates two MediatR queries and merges. `PipelineStageDto` gains internal `StageId` for joining.

**Alternatives considered**:
- Cross-domain join in Tenant handler — rejected, violates Clean Architecture (Tenant.Application would reference Diagnostic repositories)
- Lazy AJAX load — rejected, causes flash of empty content and adds JS complexity
- Shared read model — rejected, overkill for a single view

**Existing pattern**: `ListStageFormAssignmentsHandler` already does the StageFormAssignment → ProjectForm join for the StageConfig view. The new query follows the same pattern but aggregates by projectId instead of individual stageId.

## R-2: Tabler status-dot icon

**Decision**: Use `ti ti-point-filled` from Tabler Icons Webfont v3.41.1. Fallback: CSS-only dot with `border-radius: 50%`.

**Rationale**: Icon font already loaded project-wide in `_Layout.cshtml`. `ti-point-filled` is the smallest circle icon, suitable for inline status indicators.

**Alternatives considered**:
- `ti-circle-filled` — too large for inline use
- `ti-circle-dot` — has a ring + dot, visually heavier than needed
- Pure CSS dot — viable fallback but inconsistent with icon-based UI elsewhere

## R-3: Anti-forgery token in AJAX

**Decision**: Reuse existing `getAntiForgeryToken()` from `site.js`. Send via `RequestVerificationToken` header.

**Rationale**: Established project pattern used in `context-switcher.js`, `context-selector.js`, and `form-helper.js`. The Reorder action already has `[ValidateAntiForgeryToken]` and checks `XRequestedWith == "XMLHttpRequest"`.

**Key files**:
- Helper: `wwwroot/js/site.js` → `getAntiForgeryToken()`
- Example: `wwwroot/js/context-switcher.js` → `switchContextAjax()`

## R-4: CSS grid layout approach

**Decision**: 7-column CSS grid on `#pipelineList`, rows use `display: contents`.

**Rationale**: CSS grid gives natural vertical alignment without converting to `<table>`. `display: contents` on row wrappers allows children to participate in the parent grid while keeping row-level styling (borders, hover) via a nested visual wrapper.

**Column widths**: `24px 120px 100px 1fr 1fr auto auto` — handle, state, type, name, form, dates, actions.

**Alternatives considered**:
- `<table>` — simpler but loses card-like visual feel
- Flexbox with fixed widths — fragile, doesn't adapt to content
- CSS grid without `display: contents` — requires flat DOM, loses row grouping

## R-5: Reorder endpoint contract

**Decision**: POST to `/Coordination/ProjectPipeline/Reorder` with `FormData` containing `orderedStageIds[]` (list of Guids). Existing endpoint already supports AJAX (returns 200/400 for XMLHttpRequest).

**Controller signature**: `Reorder([FromForm] List<Guid> orderedStageIds, CancellationToken ct)`

**Key detail**: The `[FromForm]` binding expects the form data keys to be `orderedStageIds` (matching the parameter name). The JS should send `FormData` with repeated `orderedStageIds` entries, or use `application/x-www-form-urlencoded` with `orderedStageIds[0]=...&orderedStageIds[1]=...`.
