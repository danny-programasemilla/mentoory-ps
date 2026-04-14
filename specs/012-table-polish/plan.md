# Implementation Plan: Table Polish System-Wide

**Branch**: `012-table-polish` | **Date**: 2026-04-13 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/012-table-polish/spec.md`

## Summary

Polish all DataTable-based and static HTML tables across Mentoory by adding zebra striping, hover effects, header icons with a shared icon registry, Tabler status-dot indicators (replacing raw badges), and info line padding fixes. Consolidate all table views to use the shared DataTable ViewComponent, making polished defaults automatic for current and future tables.

## Technical Context

**Language/Version**: C# / .NET 10.0 + Razor Views + CSS + JavaScript
**Primary Dependencies**: Tabler v1.4.0 (Bootstrap 5), DataTables 2.3.4, jQuery, Tabler Icons Webfont
**Storage**: N/A (no database changes)
**Testing**: Visual QA across all table views; `dotnet build` for zero-warnings gate
**Target Platform**: Web (ASP.NET Core MVC, Razor Views)
**Project Type**: Web application
**Performance Goals**: No measurable perf impact — CSS/class additions only
**Constraints**: Must not regress existing DataTable functionality (sort, pagination, filter, empty state, skeleton loading)
**Scale/Scope**: 8 DataTable views + 4 static HTML tables across 5 areas

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Applicable? | Status | Notes |
|-----------|-------------|--------|-------|
| I. Clean Architecture Layer Boundaries | Yes | PASS | Changes confined to Web layer (Views, wwwroot CSS/JS) |
| II. CQRS Pattern Requirements | No | N/A | No new commands or queries |
| III. DDD Constraints | No | N/A | No domain changes |
| IV. Integration Events | No | N/A | No events |
| V. Zero-Warnings Policy | Yes | PASS | CSS/JS changes; must verify `dotnet build` has zero warnings |
| VI. DateTime Handling | No | N/A | No DateTime usage |
| VII. Naming Conventions | Yes | PASS | ViewComponent follows existing naming; JS functions use camelCase |
| VIII. File Organization | Yes | PASS | JS stays in `/wwwroot/js/`, CSS in `/wwwroot/css/` |
| IX. Spanish-First UI | Yes | PASS | All Spanish table headers and labels preserved unchanged |
| X. Role Hierarchy & Session Context | No | N/A | No authorization changes |
| XI. SSDT/DACPAC Database Strategy | No | N/A | No database changes |

**Result**: All applicable gates PASS. No violations.

## Project Structure

### Documentation (this feature)

```text
specs/012-table-polish/
├── spec.md              # Feature specification
├── plan.md              # This file
├── research.md          # Phase 0: Codebase inventory and design decisions
├── data-model.md        # Phase 1: N/A (no data model changes)
├── quickstart.md        # Phase 1: Development setup and verification
├── checklists/
│   └── requirements.md  # Quality checklist
└── tasks.md             # Phase 2 output (via /speckit-tasks)
```

### Source Code (repository root)

```text
Mentoory.Web/
├── wwwroot/
│   ├── css/
│   │   └── mentoory.css                          # Table CSS additions (zebra, hover, info padding)
│   └── js/
│       └── datatable-helper.js                   # Header icon registry, initDataTable enhancements
├── Views/
│   └── Shared/
│       └── Components/
│           └── DataTable/
│               ├── Default.cshtml                # Enhanced ViewComponent (icons, table classes)
│               └── DataTableViewComponent.cs     # New: strongly-typed ViewComponent model
├── Areas/
│   ├── Administration/Views/
│   │   ├── Users/Index.cshtml                    # Migrate to shared component + status dots
│   │   ├── Projects/Index.cshtml                 # Migrate to shared component + status dots
│   │   └── BatchUpload/Results.cshtml            # CSS-only: add table-striped table-hover classes
│   ├── Coordination/Views/
│   │   ├── Diagnostics/Index.cshtml              # Migrate to shared component + status dots
│   │   ├── Diagnostics/Details.cshtml            # CSS-only: add table classes
│   │   └── AnswerCorrection/Index.cshtml         # CSS-only: add table classes
│   ├── Participant/Views/
│   │   └── Diagnostic/List.cshtml                # Migrate to shared component
│   └── Platform/Views/
│       ├── Users/Index.cshtml                    # Migrate to shared component + status dots
│       ├── Incubators/Index.cshtml               # Migrate to shared component + status dots
│       └── Templates/Diagnostics.cshtml          # Migrate to shared component + status dots
└── Views/
    └── Home/Index.cshtml                         # CSS-only: add table classes
```

**Structure Decision**: No structural changes needed. All modifications are to existing files. One new file (`DataTableViewComponent.cs`) for the strongly-typed ViewComponent model to replace the ViewBag-based API.

## Complexity Tracking

No constitution violations to justify. This feature is entirely within the Web layer with no architectural complexity.

## Implementation Phases

### Phase 1: Foundation — CSS + Icon Registry

**Goal**: Add global table styling and the header icon registry.

**Changes**:

1. **mentoory.css** — Add table polish rules:
   - `.table-striped tbody tr:nth-of-type(odd)` with brand-tinted background
   - `.table-hover tbody tr:hover` with subtle highlight
   - `.dt-info` left padding fix (add `padding-left` to match table cell alignment)
   - `.table thead th .ti` icon spacing (margin-right between icon and text)

2. **datatable-helper.js** — Add header icon registry:
   - `COLUMN_ICON_MAP` object mapping column type keywords to Tabler icon classes
   - Keywords match against column header text (case-insensitive): correo/email → ti-mail, nombre → ti-user, apellido → ti-users, estado → ti-circle-check, fecha → ti-calendar, descripcion → ti-file-text, acciones → ti-settings, etapa → ti-list-check, proyecto → ti-briefcase, preguntas → ti-help-circle, version → ti-git-branch, suscripcion/nivel → ti-crown
   - `applyHeaderIcons(tableId)` function that scans `<th>` elements and prepends matched icons
   - Update `initDataTable()` to call `applyHeaderIcons()` after DataTable initialization (via `initComplete` callback)

### Phase 2: Component Rewrite — Shared DataTable ViewComponent

**Goal**: Create a strongly-typed ViewComponent that replaces the ViewBag-based approach.

**Changes**:

1. **DataTableViewComponent.cs** (new) — Strongly-typed model:
   - `TableId` (string, required)
   - `Columns` (string array, required) — column header labels
   - `Title` (string, optional) — card header title
   - Component renders `Default.cshtml` with the model

2. **Default.cshtml** (update) — Enhanced template:
   - Accept the strongly-typed model instead of ViewBag
   - Table element gets classes: `table table-vcenter table-striped table-hover card-table w-100`
   - Column headers render as `<th><i class="ti {icon} me-1"></i>{label}</th>` using the icon registry (server-side rendering via a dictionary lookup, or fallback to JS-based `applyHeaderIcons`)

### Phase 3: View Migration — DataTable Views

**Goal**: Migrate all 8 DataTable views to use the shared component and replace badge renders with `renderStatus()`.

**Views to migrate** (each follows the same pattern):

| View | Status Column Fix | Extra Custom Renders |
|------|-------------------|---------------------|
| Admin/Users/Index | badge → renderStatus (Active, Locked, PendingVerification) | Date formatting |
| Admin/Projects/Index | badge → renderStatus (Activo, Inactivo) | Description truncation, action button |
| Coord/Diagnostics/Index | badge → renderStatus (Desconectado, Sincronizacion parcial) | Action button |
| Participant/Diagnostic/List | None | Action button |
| Platform/Users/Index | badge → renderStatus (Active, Locked, PendingVerification) | Date formatting |
| Platform/Incubators/Index | badge → renderStatus (Activo, Inactivo) | Action buttons |
| Platform/Templates/Diagnostics | badge → renderStatus (Activo, Inactivo) | None |
| Admin/Dashboard/Index | N/A (not a table view, stat cards only) | N/A |

**Migration pattern per view**:
1. Replace inline `<div class="card"><div class="table-responsive"><table ...>` markup with `@await Component.InvokeAsync("DataTable", new { tableId = "...", columns = new[] { "...", ... }, title = "..." })`
2. Replace all inline badge render functions with calls to `renderStatus(text, color, animated)` from datatable-helper.js
3. Keep custom render functions (description truncation, action buttons) unchanged in the view's `<script>` section

### Phase 4: Static Table Polish

**Goal**: Apply CSS-only polish to the 4 static HTML tables.

**Views to update**:
1. `Admin/BatchUpload/Results.cshtml` — Add `table-striped table-hover` classes to `<table>` element
2. `Coord/AnswerCorrection/Index.cshtml` — Add `table-striped table-hover` classes
3. `Coord/Diagnostics/Details.cshtml` — Add `table-striped table-hover` classes to nested answer option tables
4. `Views/Home/Index.cshtml` — Add `table-striped table-hover` classes

No JS changes needed — these use server-side Razor rendering.

### Phase 5: Visual QA

**Goal**: Verify all changes across the system.

**QA checklist** (test in browser):
- [ ] Admin > Users — zebra, hover, icons, status dots, info padding
- [ ] Admin > Projects — zebra, hover, icons, status dots, action buttons
- [ ] Admin > BatchUpload Results — zebra, hover (static table)
- [ ] Coord > Diagnostics — zebra, hover, icons, status dots
- [ ] Coord > Diagnostics Details — zebra, hover (nested static tables)
- [ ] Coord > AnswerCorrection — zebra, hover (static table)
- [ ] Participant > Diagnostic List — zebra, hover, icons
- [ ] Platform > Users — zebra, hover, icons, status dots
- [ ] Platform > Incubators — zebra, hover, icons, status dots
- [ ] Platform > Templates — zebra, hover, icons, status dots
- [ ] Home — zebra, hover (static table)
- [ ] Empty state display (no zebra/hover artifacts)
- [ ] Skeleton loading display (no visual issues with striping)
- [ ] Sorting still works
- [ ] Pagination still works
- [ ] Build: `dotnet build` with zero warnings
