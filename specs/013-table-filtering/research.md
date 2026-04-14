# Research: Table Filtering

**Feature**: 013-table-filtering
**Date**: 2026-04-13

## R1: Existing Filter Plumbing

**Decision**: Reuse and extend existing infrastructure — no new plumbing needed.

**Findings**:
- `getActiveFilters(filterId)` already collects form `input`/`select` values (lines 270-282 of datatable-helper.js)
- `initDataTable()` already accepts `filterId` and sends `d.filters = getActiveFilters(config.filterId)` in POST (lines 108-110)
- `DataTableServerRequest.Filters` (`Dictionary<string, string>?`) is already bound from POST data
- `DataTableRequest` record already carries `Filters` to query handlers

**Gap Discovered**: No query handler consumes `Filters`. The `Filters` dictionary passes through the entire chain but handlers ignore it. Server-side filter logic MUST be added for the feature to work end-to-end.

**Rationale**: Building on existing plumbing minimizes risk and preserves backward compatibility.

## R2: DataTable Views Inventory

**Decision**: 7 views are candidates for auto-generated filtering.

| View | TableId | Columns | Render Functions |
|------|---------|---------|------------------|
| Administration/Users | usersTable | 5 | renderAccountStatus, formatDate |
| Administration/Projects | projectsTable | 6 | renderStatus, formatDate, custom placeholder |
| Platform/Users | usersTable | 5 | renderAccountStatus, formatDate |
| Platform/Incubators | incubatorsTable | 5 | renderStatus, formatDate, custom URL |
| Platform/Templates/Diagnostics | templatesTable | 6 | renderStatus, formatDate, custom placeholder |
| Coordination/Diagnostics | diagnosticFormsTable | 5 | renderStatus (sync mode), formatDate, custom URL |
| Participant/Diagnostic/List | participantFormsTable | 4 | formatDate, custom URL |

**Key observations**:
- None currently pass `filterId`
- "Acciones" column appears in 4/7 views — must be excluded
- `renderAccountStatus` is used in 2 views (Administration/Users, Platform/Users)
- `renderStatus` with custom inline mappings is used in 4 views

## R3: Filter Type Auto-Detection Strategy

**Decision**: Build a `FILTER_TYPE_REGISTRY` keyed by render function reference, similar to the existing `COLUMN_ICON_MAP` pattern.

**Registry design**:
```
renderAccountStatus → { type: 'select', options: [
  { value: '', label: 'Todos' },
  { value: 'Active', label: 'Activo' },
  { value: 'Locked', label: 'Bloqueado' },
  { value: 'PendingVerification', label: 'Verificación Pendiente' }
]}
```

**Detection algorithm**:
1. For each column in `config.columns`, check if `render` function matches a registry key
2. If match → use registered filter descriptor
3. If no match but column has data → text input (default)
4. If column name matches exclude list → skip

**Alternatives considered**:
- Data-attribute approach (column.className carries filter hints) — rejected: requires view changes
- Server-rendered filter metadata endpoint — rejected: adds unnecessary complexity

## R4: Server-Side Filter Consumption

**Decision**: Add filter consumption to each query handler that serves a DataTable view.

**Pattern**: Each handler checks `dt.Filters` for keys matching its known filterable columns and applies LINQ `.Where()` clauses.

**Example for ListUsersHandler**:
```
if (dt.Filters != null) {
  if (dt.Filters.TryGetValue("accountStatus", out var status) && !string.IsNullOrEmpty(status))
    query = query.Where(u => u.AccountStatus.ToString() == status);
  if (dt.Filters.TryGetValue("email", out var email) && !string.IsNullOrEmpty(email))
    query = query.Where(u => u.Email.Contains(email));
  // etc.
}
```

**Handlers to modify** (7 total, matching the 7 views):
1. ListIncubatorMembersHandler (Administration/Users)
2. ListProjectsHandler (Administration/Projects)
3. ListPlatformUsersHandler (Platform/Users) — if different from #1
4. ListIncubatorsHandler (Platform/Incubators)
5. ListDiagnosticTemplatesHandler (Platform/Templates)
6. ListProjectFormsHandler (Coordination/Diagnostics)
7. ListParticipantFormsHandler (Participant/Diagnostic)

**Rationale**: Spec assumed this was already working, but research confirms it's not. Without server-side consumption, filters POST to the server and return unfiltered data.

## R5: Filter Panel HTML Generation

**Decision**: Generate filter panel HTML entirely in JavaScript, injected before the table's `.card` parent.

**Approach**:
- After DataTables `initComplete`, build a `<div>` with filter form
- Insert it as a sibling before the table's `.card` container
- Use Bootstrap 5 grid (`row`/`col-md-*`) for responsive layout
- Toggle link placed in a `<div>` between page content and filter panel

**Panel HTML structure**:
```html
<div class="d-flex justify-content-end mb-2">
  <a href="#" id="{tableId}-filter-toggle">
    <i class="ti ti-filter"></i> Filtros <span class="badge bg-primary d-none">0</span>
  </a>
</div>
<div id="{tableId}-filter-panel" class="card card-body mb-3" style="display:none;">
  <form id="{tableId}-filter-form">
    <div class="row g-3">
      <!-- one col per filterable column -->
    </div>
    <div class="mt-3">
      <button type="submit" class="btn btn-primary">Filtrar</button>
      <button type="button" class="btn btn-ghost-secondary ms-2">Limpiar</button>
    </div>
  </form>
</div>
```

## R6: URL Persistence

**Decision**: Use `URLSearchParams` + `history.replaceState` with `f_` prefix for filter params to avoid conflicts.

**Example URL**: `/Administration/Users?f_accountStatus=Active&f_email=john`

**Flow**:
1. On "Filtrar" click: read form values → update URLSearchParams → replaceState
2. On page load: read URLSearchParams → populate form → open panel → trigger table load
3. On "Limpiar": clear form → remove `f_*` params → replaceState → reload table

**Prefix rationale**: `f_` avoids collision with DataTables' own query params or any future page-level params. Short enough to keep URLs readable.

## R7: Render Function Identification Challenge

**Decision**: Match render functions by identity comparison, not by name string.

**Challenge**: JavaScript column definitions use inline `function(data) { return renderAccountStatus(data); }` wrappers. We can't directly compare the column's `render` to `renderAccountStatus`.

**Solution**: Inspect the render function's `.toString()` output for known function names:
```javascript
var renderStr = col.render ? col.render.toString() : '';
if (renderStr.includes('renderAccountStatus')) → account status filter
if (renderStr.includes('renderStatus')) → generic status (text input unless overridden)
```

**Alternative considered**: Require views to annotate columns with filter metadata — rejected (violates NFR-001 zero-config goal).
