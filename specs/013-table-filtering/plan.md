# Implementation Plan: Table Filtering

**Branch**: `013-table-filtering` | **Date**: 2026-04-13 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/013-table-filtering/spec.md`

## Summary

Add smart, convention-based filtering to all 7 DataTable views. A filter type registry maps known render functions (e.g., `renderAccountStatus`) to filter descriptors (dropdown with status options). `initDataTable()` auto-generates a collapsible filter panel with per-column fields, toggle link with badge counter, "Filtrar"/"Limpiar" buttons, and URL query param persistence. Per-view overrides allow opt-out and custom field types. Server-side query handlers are updated to consume the existing `Filters` dictionary.

## Technical Context

**Language/Version**: C# / .NET 10.0 + JavaScript (vanilla, ES5-compatible)
**Primary Dependencies**: DataTables 2.3.4, jQuery, Tabler v1.4.0 (Bootstrap 5), Tabler Icons Webfont
**Storage**: N/A (no schema changes)
**Testing**: Manual browser testing + E2E verification on all 7 DataTable views
**Target Platform**: ASP.NET Core MVC web application
**Project Type**: Web application (modular monolith)
**Performance Goals**: Filter panel generation < 50ms; filtered server response equivalent to current table load times
**Constraints**: Zero view-level markup changes (NFR-001); all UI text in Spanish (NFR-002); no new JS dependencies (NFR-004)
**Scale/Scope**: 7 DataTable views across 5 areas; 7 query handlers need filter consumption

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| # | Principle | Status | Notes |
|---|-----------|--------|-------|
| I | Clean Architecture Layer Boundaries | PASS | JS/CSS changes are Web Layer; query handler changes are Application Layer. No layer violations. |
| II | CQRS Pattern Requirements | PASS | Query handlers remain side-effect-free. Filter logic adds WHERE clauses to existing queries. |
| III | Domain-Driven Design Constraints | N/A | No domain entity changes. |
| IV | Integration Events | N/A | No cross-domain communication. |
| V | Zero-Warnings Policy | PASS | No C# file additions; handler modifications use existing types. |
| VI | DateTime Handling | N/A | No DateTime operations added. |
| VII | Naming Conventions | PASS | No new classes/commands/queries. |
| VIII | File Organization | PASS | JS in `/wwwroot/js/`, CSS in `/wwwroot/css/`. |
| IX | Spanish-First UI | PASS | NFR-002 mandates all filter UI text in Spanish. |
| X | Role Hierarchy & Session Context | N/A | No authorization changes. |
| XI | SSDT/DACPAC Database Strategy | N/A | No database changes. |

**Gate result**: ALL PASS — no violations.

## Project Structure

### Documentation (this feature)

```text
specs/013-table-filtering/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0: research findings
├── data-model.md        # Phase 1: client-side data structures
├── quickstart.md        # Phase 1: developer guide
├── implementation-notes.md  # Design decisions from brainstorming
├── review_brief.md      # Reviewer guide
├── REVIEW-SPEC.md       # Spec review results
└── checklists/
    └── requirements.md  # Quality checklist
```

### Source Code (repository root)

```text
Mentoory.Web/
├── wwwroot/
│   ├── js/
│   │   └── datatable-helper.js     # Filter registry, panel generation, toggle, URL sync
│   └── css/
│       └── mentoory.css            # Filter panel styling
└── Views/Shared/Components/DataTable/
    └── Default.cshtml              # No changes needed

Mentoory.Tenant.Application/        # Query handlers for tenant-scoped views
├── Queries/ListProjects/
│   └── ListProjectsHandler.cs      # Add filter consumption
├── Queries/.../                     # Other handlers

Mentoory.Identity.Application/      # Query handlers for user views
├── Queries/.../
│   └── List*Handler.cs             # Add filter consumption

Mentoory.Platform.Application/      # Query handlers for platform views
├── Queries/.../
│   └── List*Handler.cs             # Add filter consumption
```

**Structure Decision**: No new files or projects. All changes modify existing files. JS and CSS extend `datatable-helper.js` and `mentoory.css`. Server-side changes modify existing query handlers.

## Complexity Tracking

> No constitution violations — table not needed.

## Implementation Phases

### Phase 1: Filter Type Registry + Panel Generation (FR-001 to FR-009, NFR-001 to NFR-004)

**Goal**: Auto-generate the filter panel for all DataTable views from column definitions.

**Changes to `datatable-helper.js`**:

1. **Add `FILTER_TYPE_REGISTRY`** — maps render function signatures to filter descriptors
   - Key: substring to match in `render.toString()` (e.g., `'renderAccountStatus'`)
   - Value: `{ type: 'select', options: [{ value: '', label: 'Todos' }, ...] }`
   - Initial entries: `renderAccountStatus` (dropdown with account statuses)

2. **Add `FILTER_EXCLUDE_LIST`** — column header keywords to skip
   - Initial entries: `'acciones'`

3. **Add `buildFilterPanel(tableId, columns, overrides)`** function:
   - Iterate `columns`, detect filter type per column
   - Apply overrides from `config.filters` array (merge logic)
   - Skip excluded columns (global exclude list + `filterable: false` overrides)
   - Generate HTML: toggle link + collapsible panel with form fields in responsive grid
   - Insert panel before the table's `.card` parent element
   - Wire "Filtrar" submit and "Limpiar" reset handlers

4. **Modify `initDataTable()`**:
   - After DataTable init, call `buildFilterPanel()`
   - Auto-generate a `filterId` pointing to the generated form
   - Wire filter form submit to `DataTable.ajax.reload()`

**Changes to `mentoory.css`**:
- Filter panel card styling (subtle background, border)
- Toggle link styling (icon, hover, positioning)
- Badge styling for active filter count
- Slide animation for panel show/hide

### Phase 2: Active Filter Feedback + URL Persistence (FR-013 to FR-017)

**Goal**: Badge counter on toggle link + filter state in URL query params.

**Changes to `datatable-helper.js`**:

1. **Add `updateFilterBadge(tableId)`** — count non-empty form fields, update badge text and visibility

2. **Add `syncFiltersToUrl(tableId)`** — write active filter values as `f_{key}` query params via `history.replaceState`

3. **Add `loadFiltersFromUrl(tableId)`** — on page load, parse `f_*` query params, populate form fields, open panel if any present, trigger table reload

4. **Wire into filter flow**:
   - On "Filtrar": update badge → sync to URL → reload table
   - On "Limpiar": clear form → update badge → clear URL params → reload table
   - On page load: load from URL → if filters present, open panel + apply

### Phase 3: Server-Side Filter Consumption (enables end-to-end filtering)

**Goal**: Query handlers consume the `Filters` dictionary to apply WHERE clauses.

**Note**: The spec assumed this already worked. Research (R4) discovered handlers don't consume `Filters`. This phase is required for the feature to function.

**Pattern per handler**:
```csharp
// After search value filtering, before sorting:
if (dt.Filters is { Count: > 0 })
{
    if (dt.Filters.TryGetValue("accountStatus", out var statusFilter)
        && !string.IsNullOrEmpty(statusFilter))
    {
        query = query.Where(u => u.AccountStatus.ToString() == statusFilter);
    }
    if (dt.Filters.TryGetValue("email", out var emailFilter)
        && !string.IsNullOrEmpty(emailFilter))
    {
        query = query.Where(u => u.Email.Contains(emailFilter));
    }
    // ... per filterable column
}
```

**Handlers to modify**:
1. Administration/Users — filter by email, firstName, lastName, accountStatus
2. Administration/Projects — filter by name, description, stage, status
3. Platform/Users — filter by email, firstName, lastName, accountStatus
4. Platform/Incubators — filter by name, description, status
5. Platform/Templates/Diagnostics — filter by name, description, subscriptionLevel, status
6. Coordination/Diagnostics — filter by name, syncMode
7. Participant/Diagnostic — filter by name

### Phase 4: Verification

**Goal**: Validate all 7 views work end-to-end.

**Checklist**:
- [ ] All 7 DataTable views show filter toggle link without view changes
- [ ] Users table: Estado dropdown auto-detects with correct options
- [ ] Filter submit returns correct filtered results
- [ ] Badge counter updates accurately
- [ ] "Limpiar" resets everything (fields, table, URL, badge)
- [ ] URL sharing restores filtered state in new tab
- [ ] Per-view override (filterable: false) works
- [ ] Per-view override (custom dropdown) works
- [ ] Empty filter submit = unfiltered load
- [ ] Build passes with zero warnings
