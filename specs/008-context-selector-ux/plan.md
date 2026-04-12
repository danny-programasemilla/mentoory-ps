# Implementation Plan: Cascading Context Selector UX

**Branch**: `008-context-selector-ux` | **Date**: 2026-04-11 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/008-context-selector-ux/spec.md`

## Summary

Replace the card-based context selection page with 3 cascading dropdowns (Role → Incubator → Project) using server-side AJAX for filtering. Add a Bootstrap 5 modal with the same cascading selector to the top-bar for in-app context switching. Implement via a shared `_ContextSelector.cshtml` partial, 3 new GET API endpoints for cascade data, and extension of the existing POST switch endpoint.

## Technical Context

**Language/Version**: C# / .NET 10.0 (SDK 10.0.0) + ASP.NET Core MVC  
**Primary Dependencies**: MediatR 14.1, FluentValidation 12.1, EF Core 10.x, Bootstrap 5 (Phoenix Admin Template)  
**Storage**: SQL Server (existing RoleAssignments table — no schema changes)  
**Testing**: xUnit + Moq + FluentAssertions (unit), Respawn (integration)  
**Target Platform**: ASP.NET Core web server, all modern browsers  
**Project Type**: Web application (modular monolith)  
**Performance Goals**: Cascade AJAX responses in under 500ms  
**Constraints**: Zero compiler warnings, all UI text in Spanish, no domain/schema changes  
**Scale/Scope**: Small data volumes (few roles, handful of incubators/projects per user)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| # | Principle | Status | Notes |
|---|-----------|--------|-------|
| I | Clean Architecture Layer Boundaries | ✅ PASS | New queries in Application layer, endpoints in Web layer, no layer violations |
| II | CQRS Pattern Requirements | ✅ PASS | New queries use `IBaseRequest<T>` + `BaseCommandHandler<TQuery, TResult>`, no validators needed (read-only) |
| III | DDD Constraints | ✅ PASS | No new entities, no schema changes. New DTOs are read models |
| IV | Integration Events | ✅ N/A | No cross-domain events needed |
| V | Zero-Warnings Policy | ✅ PASS | All new code must compile with zero warnings |
| VI | DateTime Handling | ✅ N/A | No DateTime usage in this feature |
| VII | Naming Conventions | ✅ PASS | Queries: `List*Query`, Handlers: `*Handler`, DTOs: `*Dto` |
| VIII | File Organization | ✅ PASS | JS in `/wwwroot/js/`, one class per file |
| IX | Spanish-First UI | ✅ PASS | All labels, placeholders, toasts, error messages in Spanish |
| X | Role Hierarchy & Session Context | ✅ PASS | GlobalAdmin sees all, `[Authorize]` on endpoints, area-role checks preserved |
| XI | SSDT/DACPAC Database Strategy | ✅ N/A | No database changes |

**Gate result**: ALL PASS — proceed to Phase 0.

## Project Structure

### Documentation (this feature)

```text
specs/008-context-selector-ux/
├── spec.md                    # Feature specification
├── plan.md                    # This file
├── research.md                # Phase 0 output
├── data-model.md              # Phase 1 output
├── contracts/                 # Phase 1 output
│   └── cascade-api.md         # API endpoint contracts
├── quickstart.md              # Phase 1 output
├── implementation-notes.md    # Design decisions from brainstorming
├── review_brief.md            # Reviewer guide
└── REVIEW-SPEC.md             # Spec review result
```

### Source Code (repository root)

```text
# Application Layer — New queries for cascade endpoints
Mentoory.Access.Application/
└── Queries/
    ├── ListContextRoles/
    │   ├── ListContextRolesQuery.cs
    │   ├── ListContextRolesHandler.cs
    │   └── ContextRoleDto.cs
    ├── ListContextIncubators/
    │   ├── ListContextIncubatorsQuery.cs
    │   ├── ListContextIncubatorsHandler.cs
    │   └── ContextIncubatorDto.cs
    └── ListContextProjects/
        ├── ListContextProjectsQuery.cs
        ├── ListContextProjectsHandler.cs
        └── ContextProjectDto.cs

# Web Layer — Controller changes, views, JS
Mentoory.Web/
├── Controllers/
│   └── ContextController.cs               # Extended: 3 GET endpoints + switch extension
├── Views/
│   ├── Context/
│   │   └── Select.cshtml                  # Rewritten: dropdowns via partial
│   └── Shared/
│       ├── _ContextSelector.cshtml        # NEW: shared partial (dropdowns + confirm)
│       ├── _TopBar.cshtml                 # Modified: modal trigger + modal markup
│       └── _Layout.cshtml                 # Modified: Toast component + script loading
└── wwwroot/js/
    ├── context-selector.js                # NEW: cascade logic for dropdowns
    └── context-switcher.js                # Modified: modal integration

# Tests
tests/
├── Mentoory.Tests.Integration/Authorization/
│   └── ContextSelectionTests.cs           # Extended: cascade API tests
└── Mentoory.Tests.E2E/Tests/
    └── ContextSelectionTests.cs           # Extended: cascade E2E tests
```

**Structure Decision**: All new code follows existing project patterns. Application queries go in the Access domain (where RoleAssignment lives). The Tenant domain's existing `ListIncubatorContextOptionsQuery` is reused for GlobalAdmin cascade data. New JS file `context-selector.js` handles cascade dropdown logic; existing `context-switcher.js` is extended for modal integration.

## Complexity Tracking

No constitution violations. No complexity justifications needed.

## Implementation Phases

### Phase 1: Cascade API Endpoints (Backend)

Build the 3 GET endpoints that power the cascade dropdowns.

**Files to create:**
- `Mentoory.Access.Application/Queries/ListContextRoles/ListContextRolesQuery.cs`
- `Mentoory.Access.Application/Queries/ListContextRoles/ListContextRolesHandler.cs`
- `Mentoory.Access.Application/Queries/ListContextRoles/ContextRoleDto.cs`
- `Mentoory.Access.Application/Queries/ListContextIncubators/ListContextIncubatorsQuery.cs`
- `Mentoory.Access.Application/Queries/ListContextIncubators/ListContextIncubatorsHandler.cs`
- `Mentoory.Access.Application/Queries/ListContextIncubators/ContextIncubatorDto.cs`
- `Mentoory.Access.Application/Queries/ListContextProjects/ListContextProjectsQuery.cs`
- `Mentoory.Access.Application/Queries/ListContextProjects/ListContextProjectsHandler.cs`
- `Mentoory.Access.Application/Queries/ListContextProjects/ContextProjectDto.cs`

**Files to modify:**
- `Mentoory.Web/Controllers/ContextController.cs` — add 3 GET API endpoints + extend Switch

**Design:**

1. **`GET /api/context/roles`** → `ListContextRolesQuery(long UserId)`
   - Handler: call `IRoleAssignmentRepository.GetActiveByUserIdAsync(userId)`, extract distinct roles
   - Map each role to `ContextRoleDto(string Role, string DisplayName)` using static display name map (same as `GetRoleDisplayName` in current view)
   - Returns `List<ContextRoleDto>` ordered by hierarchy (GlobalAdmin first → Sponsor last)

2. **`GET /api/context/incubators?role={role}`** → `ListContextIncubatorsQuery(long UserId, string Role)`
   - For GlobalAdmin: use existing `ListIncubatorContextOptionsQuery` to get all active incubators, attach the GlobalAdmin's `RoleAssignmentExternalId` to each
   - For other roles: query `IRoleAssignmentRepository.Query()` filtered by userId + role + isActive, join with incubator names, group by IncubatorId
   - Returns `List<ContextIncubatorDto(long Id, string Name, Guid RoleAssignmentExternalId)>`
   - Note: for project-scoped roles, `RoleAssignmentExternalId` at incubator level is the first matching assignment (actual resolution happens at project level)

3. **`GET /api/context/projects?role={role}&incubatorId={id}`** → `ListContextProjectsQuery(long UserId, string Role, long IncubatorId)`
   - For GlobalAdmin: query all active projects under incubatorId (via `QueryUnfiltered()`), use GlobalAdmin's base `RoleAssignmentExternalId`
   - For other roles: query role assignments filtered by userId + role + incubatorId + ProjectId != null
   - Returns `List<ContextProjectDto(long Id, string Name, Guid RoleAssignmentExternalId)>`

4. **`POST /api/context/switch` extension**:
   - Extend `ContextSwitchRequest` record to: `ContextSwitchRequest(Guid RoleAssignmentExternalId, long? IncubatorId, string? IncubatorName, long? ProjectId, string? ProjectName)`
   - When override fields are present and resolved role is GlobalAdmin, apply overrides to the UserContext (same pattern as existing `SetGlobalAdminContext`)

### Phase 2: Shared Partial + Full Page (Frontend)

Build the cascading dropdown UI and rewrite the selection page.

**Files to create:**
- `Mentoory.Web/Views/Shared/_ContextSelector.cshtml`
- `Mentoory.Web/wwwroot/js/context-selector.js`

**Files to modify:**
- `Mentoory.Web/Views/Context/Select.cshtml`
- `Mentoory.Web/Controllers/ContextController.cs` (GET Select action — update view model)

**Design:**

1. **`_ContextSelector.cshtml`** partial:
   - Three `<select>` elements: `#ctx-role`, `#ctx-incubator`, `#ctx-project`
   - Hidden inputs: `roleAssignmentExternalId`, `selectedIncubatorId`, `selectedIncubatorName`, `selectedProjectId`, `selectedProjectName`, `returnUrl`
   - "Confirmar" button, disabled by default
   - `data-mode` attribute: `"page"` (form POST) or `"modal"` (AJAX)
   - Loading spinner per dropdown during AJAX fetch
   - Error container per dropdown for inline error messages

2. **`context-selector.js`**:
   - `initContextSelector(container)` — wires cascade events for a given container element
   - Role change → fetch `/api/context/incubators?role=X`, populate incubator select, reset project select, update hidden fields
   - Incubator change → fetch `/api/context/projects?role=X&incubatorId=Y`, populate project select, update hidden fields
   - Project change → update hidden fields
   - Auto-select: if only 1 option, select it, mark as disabled, trigger next cascade automatically
   - Enable confirm button when role + incubator are selected
   - Error handling: show `"Error al cargar opciones. Intente nuevamente."` with retry link on fetch failure
   - Uses `getAntiForgeryToken()` from site.js for all fetch calls

3. **`Select.cshtml`** rewrite:
   - Same heading: "Seleccionar Contexto de Trabajo"
   - `<form asp-action="Select" method="post">` wrapping the `_ContextSelector` partial with `data-mode="page"`
   - `@Html.AntiForgeryToken()` inside the form
   - Partial renders dropdowns + hidden fields + confirm button

4. **Controller GET action update**:
   - Preserve auto-skip: single-context users bypass page
   - For multi-context users: pass distinct roles as initial data to the view (via `ViewBag.InitialRoles` JSON or similar)
   - If user has one role, page JS auto-selects it and triggers incubator fetch on load

### Phase 3: Top-Bar Modal (Frontend)

Add the modal context switcher to the top-bar.

**Files to modify:**
- `Mentoory.Web/Views/Shared/_TopBar.cshtml`
- `Mentoory.Web/Views/Shared/_Layout.cshtml`
- `Mentoory.Web/wwwroot/js/context-switcher.js`

**Design:**

1. **`_TopBar.cshtml`** changes:
   - Replace "Cambiar contexto" form/link with `<button data-bs-toggle="modal" data-bs-target="#contextSwitcherModal">Cambiar contexto</button>`
   - Add modal markup: `<div class="modal fade" id="contextSwitcherModal">` containing `_ContextSelector` partial with `data-mode="modal"`
   - Modal header: "Cambiar Contexto de Trabajo"
   - Modal body: the shared partial

2. **`_Layout.cshtml`** changes:
   - Add `@await Component.InvokeAsync("Toast")` (required for `showToast()` to work)
   - Add `<script src="~/js/context-selector.js"></script>` in the global scripts section
   - Ensure `<script src="~/js/context-switcher.js"></script>` is loaded after

3. **`context-switcher.js`** extension:
   - On modal shown (`shown.bs.modal` event): call `initContextSelector(modalContainer)` to wire up cascade
   - On modal confirm click: read hidden fields from modal, call `POST /api/context/switch` with full payload
   - Check `hasUnsavedChanges()` before confirming switch
   - Check `canAccessCurrentArea(newRole)` after switch success to determine reload vs redirect
   - Show toast via `showToast()` on success/failure
   - Close modal on success via `bootstrap.Modal.getInstance().hide()`

### Phase 4: Testing & Polish

**Files to modify:**
- `tests/Mentoory.Tests.Integration/Authorization/ContextSelectionTests.cs`
- `tests/Mentoory.Tests.E2E/Tests/ContextSelectionTests.cs`

**Integration tests:**
- `GET /api/context/roles` returns distinct roles for a multi-role user
- `GET /api/context/incubators?role=Mentor` returns only user's Mentor-assigned incubators
- `GET /api/context/incubators?role=GlobalAdmin` returns ALL active incubators
- `GET /api/context/projects?role=Mentor&incubatorId=1` returns correct projects
- `POST /api/context/switch` with GlobalAdmin override fields works correctly
- Unauthorized requests return 401

**E2E tests:**
- Multi-role user sees cascading dropdowns (not card grid)
- Single-context user auto-skips the selector page
- Role change resets incubator and project dropdowns
- Modal opens from top-bar and cascade works
- Context switch via modal shows toast and reloads

**Manual testing checklist:**
- Mobile viewport responsiveness
- Keyboard navigation through dropdowns
- returnUrl preservation through the flow
- Unsaved-changes warning on modal switch
- Area redirect when switching to incompatible role
