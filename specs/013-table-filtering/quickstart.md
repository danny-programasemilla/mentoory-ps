# Quickstart: Table Filtering

**Feature**: 013-table-filtering
**Date**: 2026-04-13

## What This Feature Does

Adds a smart, auto-generated filter panel to every DataTable view. The panel appears above the table card and provides per-column filter fields (text inputs or dropdowns) with a toggle link, active filter counter badge, and URL persistence.

## How It Works

1. `initDataTable()` scans column definitions after table initialization
2. A filter type registry maps known render functions to filter descriptors
3. For each filterable column, a form field is generated (text or dropdown)
4. The panel is injected before the table card, hidden by default
5. A "Filtros" toggle link with badge counter appears above the table
6. "Filtrar" submits all fields to the server via the existing `Filters` dictionary
7. "Limpiar" resets fields, table, URL params, and badge
8. Filter values sync to URL query params for bookmarking/sharing

## Files Modified

| File | Change |
|------|--------|
| `Mentoory.Web/wwwroot/js/datatable-helper.js` | Filter type registry, panel generation, toggle, URL sync, override API |
| `Mentoory.Web/wwwroot/css/mentoory.css` | Filter panel styling, toggle link, badge, collapse animation |
| 7 query handlers (Application layer) | Add `Filters` consumption with LINQ Where clauses |

## Zero View Changes Required

No `.cshtml` files need modification. The filter panel auto-generates from existing `initDataTable()` column definitions.

## Per-View Overrides (Optional)

When auto-detection isn't enough, pass a `filters` array:

```javascript
initDataTable('myTable', {
    apiUrl: '...',
    columns: [...],
    filters: [
        { column: 'actions', filterable: false },
        { column: 'customField', type: 'select', options: [
            { value: '', label: 'Todos' },
            { value: 'option1', label: 'Opción 1' }
        ]}
    ]
});
```

## Testing

1. Navigate to `/Administration/Users`
2. Click "Filtros" link → panel slides down
3. Select "Activo" in Estado dropdown → click "Filtrar"
4. Table shows only active users; badge shows "Filtros (1)"
5. Copy URL → open in new tab → same filtered view loads
6. Click "Limpiar" → all fields clear, full table loads, URL cleans up
