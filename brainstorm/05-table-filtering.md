# Brainstorm: Table Filtering

**Date:** 2026-04-13
**Status:** spec-created
**Spec:** specs/013-table-filtering/

## Problem Framing

After the table polish work (spec 012) improved visual consistency across DataTable views, the next UX gap is filtering. The existing `datatable-helper.js` has unused plumbing for filtering (`getActiveFilters()`, `filterId` config, `DataTableServerRequest.Filters` on the server), but no view uses it and there's no filter UI. Users need to find specific records in growing tables (e.g., filtering users by status on `/Administration/Users`), but currently must scan through paginated results manually.

The user wants a modern, clean approach — not inline header filters, but a dedicated filter section above the table with smart field detection (dropdowns for enums, text for free-form fields).

## Approaches Considered

### A: Fully JS-Generated Filter Form (Selected)
- `initDataTable()` auto-generates the entire filter panel from column definitions
- Filter type registry maps render functions to filter descriptors
- Zero markup changes in views
- Pros: Zero config, consistent UX, single source of truth
- Cons: Dropdown options must be defined in JS (not from DB)

### B: Server-Rendered Filter Form via ViewComponent
- Extend DataTable ViewComponent to accept filter metadata from C#
- Server renders filter HTML with Razor
- Pros: Options from C# enums/DB, strongly typed
- Cons: Contradicts auto-detection goal, every view must declare filters

### C: Hybrid — JS-Generated with Server Hints
- Column definitions include optional filter metadata
- JS generates form, views can pass server-rendered option lists
- Pros: Best of both worlds
- Cons: Two patterns for developers to understand

## Decision

**Approach A selected.** Key decisions:

- **Panel placement**: Collapsible panel between page header and table card (outside card), hidden by default
- **Toggle**: "Filtros" link in top-right corner above table, with badge counter when filters active
- **Apply behavior**: Explicit "Filtrar" button (not live filtering) to avoid unnecessary server calls
- **Auto-detection**: Convention-based registry maps render functions to filter types (e.g., `renderAccountStatus` → status dropdown)
- **Overrides**: Per-view `filters` config array can exclude columns or provide custom dropdown options
- **URL persistence**: Filter values synced to URL query params via `replaceState` for bookmarkable/shareable filtered views
- **Reset**: "Limpiar" button clears all fields, reloads unfiltered table, removes URL params

## Open Threads

- Should the filter panel animate with CSS transitions or Bootstrap collapse?
- URL param namespacing strategy to avoid conflicts with existing query params
- Filter panel layout behavior on tables with 7+ filterable columns (responsive wrapping)
