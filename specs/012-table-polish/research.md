# Research: Table Polish System-Wide

**Date**: 2026-04-13
**Feature**: 012-table-polish

## Codebase Inventory

### DataTable Views (8 total — use `initDataTable()` from `datatable-helper.js`)

| # | View Path | Table ID | Columns | Has Badge Renders | Uses Shared Component |
|---|-----------|----------|---------|-------------------|-----------------------|
| 1 | Areas/Administration/Views/Users/Index.cshtml | usersTable | Email, Nombre, Apellido, Estado, Fecha de Registro | Yes (Active, Locked, PendingVerification) | No — inlines markup |
| 2 | Areas/Administration/Views/Projects/Index.cshtml | projectsTable | Nombre, Descripcion, Etapa, Estado, Fecha de Creacion, Acciones | Yes (Activo, Inactivo) | No — inlines markup |
| 3 | Areas/Coordination/Views/Diagnostics/Index.cshtml | diagnosticFormsTable | Nombre, Modo Sincronizacion, Preguntas, Fecha de Creacion, Acciones | Yes (Desconectado, Sincronizacion parcial) | No — inlines markup |
| 4 | Areas/Participant/Views/Diagnostic/List.cshtml | participantFormsTable | Nombre, Preguntas, Fecha de Creacion, Acciones | No | No — inlines markup |
| 5 | Areas/Platform/Views/Users/Index.cshtml | usersTable | Email, Nombre, Apellido, Estado, Fecha de Registro | Yes (Active, Locked, PendingVerification) | No — inlines markup |
| 6 | Areas/Platform/Views/Incubators/Index.cshtml | incubatorsTable | Nombre, Descripcion, Estado, Fecha de Creacion, Acciones | Yes (Activo, Inactivo) | No — inlines markup |
| 7 | Areas/Platform/Views/Templates/Diagnostics.cshtml | templatesTable | Nombre, Descripcion, Nivel de Suscripcion, Version, Estado, Fecha de Creacion | Yes (Activo, Inactivo) | No — inlines markup |
| 8 | Areas/Administration/Views/Dashboard/Index.cshtml | — | N/A (stat cards, not a data table) | N/A | N/A |

**Note**: Dashboard (view #8) is not a table view — it contains stat cards and navigation cards only. Actual DataTable views total **7**.

### Static HTML Tables (4 total — server-side Razor rendering)

| # | View Path | Column Headers | Status Rendering |
|---|-----------|---------------|------------------|
| 1 | Areas/Administration/Views/BatchUpload/Results.cshtml | #, Pais, Identificacion, Correo, Estado, Contrasena temporal, Observaciones | Tabler status dots (already correct) |
| 2 | Areas/Coordination/Views/AnswerCorrection/Index.cshtml | Fecha, Valor Anterior, Razon, Corregido por | None |
| 3 | Areas/Coordination/Views/Diagnostics/Details.cshtml | Texto, Puntaje, FODA, ODSR (nested in accordions) | None |
| 4 | Views/Home/Index.cshtml | Title, Date created | None |

### Existing Helper Functions in datatable-helper.js

| Function | Purpose | Currently Used? |
|----------|---------|-----------------|
| `initDataTable(tableId, config)` | Initialize DataTable with server-side processing | Yes — all 7 DataTable views |
| `renderStatus(statusText, statusColor, animated)` | Render Tabler status-dot component | No — exists but unused; views use raw badge HTML instead |
| `renderAvatar(firstName, lastName, email)` | Render compound avatar cell | No — exists but unused |
| `renderActions(actions)` | Render icon action buttons | No — exists but unused |
| `formatRelativeDate(isoString)` | Format as Spanish relative time | No — exists but unused |
| `escapeHtml(str)` | XSS prevention | Yes — used by other helpers |
| `getActiveFilters(filterId)` | Collect filter form values | Yes — used by some views |

### Current DataTable DOM Layout

```javascript
dom: '<"row"<"col-sm-12"tr>><"row"<"col-sm-5"i><"col-sm-7"p>>'
```

The info line (`.dt-info`) is inside `col-sm-5` within a Bootstrap row. The row has no horizontal padding, causing the info text to sit flush with the card edge.

### Current CSS (mentoory.css) — Table Related

```css
.dt-info {
    font-size: var(--tblr-body-font-size);
    color: var(--tblr-secondary-color);
}
```

No padding defined — this is the root cause of the info line alignment issue.

## Design Decisions

### Decision 1: Zebra Striping Approach

- **Choice**: Add `table-striped` class to the shared DataTable component's `<table>` element
- **Rationale**: Tabler/Bootstrap 5 already defines `table-striped` styles that work with the theme. Using the built-in class avoids custom CSS and ensures compatibility with Tabler updates.
- **Alternatives considered**: Custom CSS `nth-of-type` rule — rejected because it duplicates built-in functionality

### Decision 2: Hover Effect Approach

- **Choice**: Add `table-hover` class to the shared DataTable component's `<table>` element
- **Rationale**: Same as zebra — Tabler/Bootstrap 5's `table-hover` is built-in and theme-aware
- **Alternatives considered**: Custom hover with brand color — rejected; built-in is sufficient and consistent

### Decision 3: Header Icon Implementation

- **Choice**: JavaScript-based icon injection via `initDataTable()` callback, with a keyword-to-icon map in `datatable-helper.js`
- **Rationale**: Server-side rendering in the Razor ViewComponent would require passing icon mappings from each view, increasing coupling. A JS-based approach auto-matches column headers by text content, requiring zero changes in individual views.
- **Alternatives considered**:
  - Server-side dictionary in ViewComponent: Would require each view to pass column metadata objects instead of simple strings. Increases view complexity.
  - CSS `::before` pseudo-elements: Cannot insert different icons per column via CSS alone.

### Decision 4: ViewComponent Rewrite Scope

- **Choice**: Create a strongly-typed ViewComponent model (`DataTableViewComponent.cs`) replacing the ViewBag-based API, but keep the model simple — `TableId`, `Columns` (string array), optional `Title`
- **Rationale**: ViewBag is weakly typed and error-prone. A model gives IntelliSense and compile-time checking. Keeping it simple (just strings) means icon mapping stays in JS, and the migration diff per view is minimal.
- **Alternatives considered**: Rich column model with icon, width, sortable properties — rejected as over-engineering for the current need

### Decision 5: Info Line Padding Fix

- **Choice**: Add `padding-left: 1rem` (matching `.card-body` padding) to the DataTables info row via CSS in mentoory.css
- **Rationale**: The info/pagination row sits inside the card but outside the `table-responsive` wrapper, so it doesn't inherit table cell padding. A targeted CSS rule aligns it.
- **Alternatives considered**: Wrapping in a `card-body` div — rejected because it would require DOM structure changes in DataTables' generated output

### Decision 6: Status Rendering Migration

- **Choice**: Replace all inline badge render functions with calls to the existing `renderStatus()` helper
- **Rationale**: `renderStatus()` already exists in `datatable-helper.js` and produces correct Tabler status-dot HTML. It was built in spec 010 but never adopted by the views.
- **Alternatives considered**: None — this is a straightforward adoption of existing infrastructure
