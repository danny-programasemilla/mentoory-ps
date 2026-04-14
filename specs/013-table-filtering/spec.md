# Feature Specification: Table Filtering

**Feature Branch**: `013-table-filtering`  
**Created**: 2026-04-13  
**Status**: Draft  
**Input**: User description: "Smart, convention-based table filtering for all DataTable views. Collapsible filter panel auto-generated from column definitions with per-column search fields (text/dropdown), filter type registry, per-view overrides, active filter badge counter, and URL persistence."

## User Scenarios & Testing

### User Story 1 - Toggle and Apply Filters (Priority: P1)

A platform administrator navigates to the Users list (`/Administration/Users`). They see a "Filtros" link in the top-right corner above the table. They click it, and a filter panel slides down between the page header and the table card. The panel shows one field per filterable column: text inputs for "Correo Electrónico", "Nombre", "Apellido", and a dropdown for "Estado" pre-populated with all known status options (Activo, Inactivo, Bloqueado, Verificación Pendiente). They select "Activo" from the Estado dropdown and click "Filtrar". The table reloads showing only active users. The toggle link now reads "Filtros (1)".

**Why this priority**: Core value proposition — without this, the feature has no purpose. Delivers immediate filtering capability with smart field detection.

**Independent Test**: Can be fully tested on the Users table by toggling the panel, selecting a status, clicking "Filtrar", and verifying filtered results.

**Acceptance Scenarios**:

1. **Given** a DataTable view with no view-level changes, **When** the page loads, **Then** a "Filtros" toggle link appears in the top-right corner above the table card
2. **Given** the filter panel is hidden, **When** the user clicks the toggle link, **Then** the panel slides down with a smooth animation revealing filter fields
3. **Given** a column uses `renderAccountStatus`, **When** the filter panel renders, **Then** that column's filter is a dropdown with all known status options
4. **Given** a column with no recognized renderer, **When** the filter panel renders, **Then** that column's filter is a text input
5. **Given** the user fills in one or more filter fields and clicks "Filtrar", **When** the table reloads, **Then** only matching records are shown and the toggle link displays the active filter count as a badge (e.g., "Filtros (2)")

---

### User Story 2 - Clear Filters and Reset (Priority: P1)

After applying filters, the user clicks "Limpiar". All filter fields reset to their default (empty/all) values. The table reloads with unfiltered data. The toggle link reverts to "Filtros" with no badge. The URL query parameters for filters are removed.

**Why this priority**: Equally critical as applying filters — users must be able to undo filtering without reloading the page.

**Independent Test**: Apply any filter, click "Limpiar", verify fields are empty, table is unfiltered, badge is gone, and URL has no filter params.

**Acceptance Scenarios**:

1. **Given** one or more filters are active, **When** the user clicks "Limpiar", **Then** all filter fields reset to empty/default values
2. **Given** filters were active and "Limpiar" is clicked, **When** the table reloads, **Then** all records are shown (unfiltered)
3. **Given** the URL contains filter query params, **When** "Limpiar" is clicked, **Then** filter-related query params are removed from the URL

---

### User Story 3 - URL Persistence and Sharing (Priority: P2)

A coordinator applies filters on the Diagnostics table and copies the URL. They share it with a colleague. When the colleague opens the link, the filter panel opens automatically with the same filter values pre-filled, and the table shows the filtered results.

**Why this priority**: Adds significant value for collaboration and bookmarking, but the feature is usable without it.

**Independent Test**: Apply filters, copy URL, open in a new tab, verify filters are restored and table shows filtered results.

**Acceptance Scenarios**:

1. **Given** the user clicks "Filtrar" with active filter values, **When** the request completes, **Then** the filter values are written to URL query parameters via `history.replaceState`
2. **Given** a URL with filter query params, **When** the page loads, **Then** the filter panel opens with those values pre-filled and the table loads filtered data
3. **Given** a URL with a query param that doesn't match any filter field, **When** the page loads, **Then** the unrecognized param is silently ignored

---

### User Story 4 - Per-View Overrides (Priority: P2)

A developer adds a new table view. One column should not be filterable (e.g., "Acciones"). Another column needs a custom dropdown instead of the auto-detected text input. The developer passes an overrides array in the `initDataTable()` config to exclude one column and provide custom options for another. The filter panel respects both overrides.

**Why this priority**: Provides flexibility for edge cases without requiring changes to the core helper.

**Independent Test**: Create a test page with `initDataTable()` passing `filters` overrides, verify the excluded column has no filter field and the custom dropdown renders correctly.

**Acceptance Scenarios**:

1. **Given** a column override with `filterable: false`, **When** the filter panel renders, **Then** that column has no filter field
2. **Given** a column override with `type: 'select'` and custom `options`, **When** the filter panel renders, **Then** a dropdown with those options appears instead of the auto-detected field
3. **Given** overrides for some columns and auto-detection for others, **When** the filter panel renders, **Then** overridden columns use the override config and remaining columns use auto-detected types

---

### Edge Cases

- All filter fields empty + click "Filtrar" = table loads unfiltered, badge shows no count
- A column with no renderer and no override = text input (safe default)
- Multiple tables on the same page = each gets its own filter panel scoped by `tableId`
- Override references a column that doesn't exist = silently ignored
- Browser back/forward after `replaceState` = filter state preserved in URL
- Columns named "Acciones" are excluded from filtering by default (global exclude list)

## Requirements

### Functional Requirements

**Filter Panel**

- **FR-001**: System MUST render a collapsible filter panel between the page header and the table `.card` container, outside the card
- **FR-002**: The panel MUST be hidden by default; a toggle link in the top-right corner above the table shows/hides it with a smooth slide animation
- **FR-003**: The panel MUST contain one form field per filterable column, laid out horizontally in a responsive grid that wraps on smaller screens
- **FR-004**: Each filter field MUST use the column's Spanish header text as its label
- **FR-005**: The panel MUST include a "Filtrar" button to submit all fields and a "Limpiar" button to reset all fields and reload the table without filters

**Filter Type Auto-Detection**

- **FR-006**: System MUST provide a filter type registry that maps known render functions to filter descriptors (type + options)
- **FR-007**: Columns with a recognized renderer MUST receive a smart filter (e.g., dropdown with known options); columns without MUST receive a text input
- **FR-008**: Columns named "Acciones" (and any column matching a configurable global exclude list) MUST be excluded from filtering automatically
- **FR-009**: The registry MUST be extensible — adding a new render-to-filter mapping in one place enables smart filtering for all tables using that renderer

**Per-View Overrides**

- **FR-010**: `initDataTable()` MUST accept an optional `filters` config array where views can override auto-detected filter types or exclude specific columns (`filterable: false`)
- **FR-011**: Overrides MUST support supplying custom dropdown options (e.g., `{ column: 'status', type: 'select', options: [...] }`)
- **FR-012**: Overrides MUST merge with auto-detected defaults — only the specified properties are replaced

**Active Filter Feedback**

- **FR-013**: When filters are active, the toggle link MUST display a badge with the count of non-empty filters (e.g., "Filtros (3)")
- **FR-014**: When no filters are active, the link MUST show "Filtros" with no badge

**URL Persistence**

- **FR-015**: On "Filtrar" click, active filter values MUST be written to URL query parameters via `history.replaceState` (no page reload)
- **FR-016**: On page load, if query params matching filter names exist, the filter panel MUST open with those values pre-filled and the table MUST load with them applied
- **FR-017**: "Limpiar" MUST remove filter query parameters from the URL

**Non-Functional Requirements**

- **NFR-001**: Zero markup changes in existing views — filtering activates automatically for all `initDataTable()` calls
- **NFR-002**: All filter UI text MUST be in Spanish (labels, buttons, placeholders)
- **NFR-003**: Filter panel MUST follow Tabler/Bootstrap 5 form styling conventions
- **NFR-004**: No new JavaScript dependencies — built with vanilla JS and existing DataTables API

## Success Criteria

### Measurable Outcomes

- **SC-001**: All existing DataTable views display the filter toggle link and functional filter panel without any per-view markup changes
- **SC-002**: On the Users table, the "Estado" column auto-detects as a dropdown with all known status values — users can filter by status in under 5 seconds
- **SC-003**: Sharing a filtered URL with another user restores the exact same filtered view on page load
- **SC-004**: "Limpiar" resets all fields, reloads unfiltered data, clears URL params, and removes the badge — all within a single click
- **SC-005**: Per-view overrides (exclude column, custom dropdown) work without breaking auto-detection on other columns in the same table

## Assumptions

- Server-side filter logic in controllers/queries already supports the `Filters` dictionary in `DataTableServerRequest` — no backend changes needed for this feature
- The existing `getActiveFilters()` function and `filterId` config option in `datatable-helper.js` will be reused/extended, not replaced
- Status values displayed in dropdowns (Activo, Inactivo, Bloqueado, Verificación Pendiente) are already defined client-side in render functions
- Static HTML tables (BatchUpload Results, AnswerCorrection) are out of scope — only DataTable-initialized tables receive filtering
- Date range pickers, multi-select filters, and saved filter presets are out of scope for this feature
