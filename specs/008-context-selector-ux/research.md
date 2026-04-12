# Research: Cascading Context Selector UX

**Feature**: 008-context-selector-ux  
**Date**: 2026-04-11  
**Status**: Complete — all unknowns resolved

## R-001: Cascade API Design — How to Resolve roleAssignmentExternalId

**Decision**: Include `roleAssignmentExternalId` directly in cascade API responses.

**Rationale**: The current card-based UI embeds `roleAssignmentExternalId` as a hidden field per card. With dropdowns, the frontend needs to know which assignment to submit when the user clicks Confirmar. Including the ExternalId in each dropdown option's data eliminates the need for a separate resolution endpoint.

**Alternatives Considered**:
- **Separate resolution endpoint** (`GET /api/context/resolve?role=X&incubatorId=Y&projectId=Z`): Adds an extra round-trip after all dropdowns are selected. Rejected — unnecessary network overhead.
- **Client-side context tree**: Load all assignments as JSON on page load and filter client-side. Rejected — user preferred server-side AJAX.

**Implementation**: 
- `ContextIncubatorDto` includes `RoleAssignmentExternalId`
- `ContextProjectDto` includes `RoleAssignmentExternalId`
- For GlobalAdmin, the base assignment's ExternalId is used (overrides applied via extended switch endpoint)

## R-002: GlobalAdmin Incubator/Project Loading

**Decision**: Reuse existing `ListIncubatorContextOptionsQuery` for GlobalAdmin cascade.

**Rationale**: This query already fetches all active incubators with their projects using `QueryUnfiltered()` (bypasses tenant filters). It's the exact data shape needed for the incubator and project dropdowns when role=GlobalAdmin.

**Alternatives Considered**:
- **New GlobalAdmin-specific queries**: Would duplicate the `QueryUnfiltered()` pattern. Rejected — unnecessary code.

**Implementation**:
- `ListContextIncubatorsHandler` delegates to `ListIncubatorContextOptionsQuery` when role is GlobalAdmin
- `ListContextProjectsHandler` filters the same data by IncubatorId for the projects cascade

## R-003: Shared Partial View — Page vs Modal Mode

**Decision**: Single `_ContextSelector.cshtml` partial with a `data-mode` attribute controlling behavior.

**Rationale**: Both the full page and the modal need identical dropdown markup, cascade wiring, and hidden fields. The only difference is the submit action: form POST (page) vs AJAX (modal).

**Implementation**:
- `data-mode="page"`: Confirm button is a form submit button inside a `<form>` tag
- `data-mode="modal"`: Confirm button triggers JavaScript AJAX call
- `context-selector.js` reads `data-mode` to determine behavior
- Hidden fields are identical in both modes

## R-004: Initial Role Dropdown Population

**Decision**: New `GET /api/context/roles` endpoint returns distinct roles for the authenticated user.

**Rationale**: Both the full page and the modal need to populate the first dropdown. A dedicated API endpoint ensures consistency and works in both contexts (server-rendered page and dynamically-opened modal).

**Alternatives Considered**:
- **Server-render roles into ViewBag/ViewData**: Works for the full page but not for the modal (which needs AJAX). Would require two different initialization paths.
- **Embed all assignment data as JSON in the layout**: Leaks more data than needed to the client.

**Implementation**: 
- Controller GET action for full page: on load, JS calls `/api/context/roles` to populate the role dropdown
- Modal: on `shown.bs.modal` event, JS calls the same endpoint
- Single code path for both scenarios

## R-005: Extended Switch Endpoint for GlobalAdmin Modal

**Decision**: Extend `ContextSwitchRequest` with optional override fields.

**Rationale**: When GlobalAdmin switches context via the modal, they select an incubator/project from the cascade that may differ from their base role assignment. The switch endpoint needs to accept these overrides to set the correct scoped context (same pattern as existing `SetGlobalAdminContext` in the controller).

**Implementation**:
- `ContextSwitchRequest` gains: `long? IncubatorId`, `string? IncubatorName`, `long? ProjectId`, `string? ProjectName`
- Switch handler: if override fields present AND resolved role is GlobalAdmin, apply overrides to UserContext before updating auth cookie
- Non-GlobalAdmin requests with override fields are ignored (security guard)

## R-006: Auto-Select Cascade Behavior

**Decision**: When a dropdown has exactly one option, auto-select it, mark it disabled/read-only, and trigger the next cascade automatically.

**Rationale**: Reduces clicks for users with limited options while maintaining transparency (they see what was auto-selected).

**Implementation**:
- After populating a dropdown via AJAX, check `options.length === 1`
- If so: set `selectedIndex = 0`, add `disabled` attribute, dispatch `change` event to trigger next cascade
- The auto-skip at page level (controller) still handles the case where the entire context resolves to a single combination — the page is never shown

## R-007: Error Handling in Cascade

**Decision**: Inline error messages per dropdown with retry capability.

**Rationale**: AJAX cascade failures should be recoverable without reloading the page. Each dropdown has its own error state independent of others.

**Implementation**:
- Each dropdown has an adjacent `<div class="text-danger small d-none">` error container
- On fetch failure: show container with "Error al cargar opciones. Intente nuevamente." and a clickable retry link
- Retry link re-triggers the same fetch call
- On successful fetch: hide any previous error message
