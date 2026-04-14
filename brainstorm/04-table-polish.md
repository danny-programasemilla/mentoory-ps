# Brainstorm: Table Polish

**Date:** 2026-04-13
**Status:** spec-created
**Spec:** specs/012-table-polish/

## Problem Framing

After the Tabler migration (spec 009) and design system polish (spec 010), tables across Mentoory are functional but visually plain. Specific issues identified from the Users table screenshot:

1. No zebra striping — all rows are the same white background, making it hard to scan
2. No hover effect — no visual feedback when hovering over rows
3. No header icons — plain text headers without semantic visual cues
4. Poor status badge contrast — green `bg-success` badges have white text on green background, hard to read
5. Info line padding — "Mostrando 1 a 7 de 7 registros" has no left padding, looks disconnected

Additionally, the existing `renderStatus()` helper in `datatable-helper.js` (built in spec 010) was never adopted by any view — they all use raw badge HTML. The shared DataTable ViewComponent exists but is also unused.

## Approaches Considered

### A: CSS-Only Polish
- Add zebra/hover via CSS classes, fix info padding, update status renders in each view
- Pros: Minimal blast radius, quick
- Cons: Doesn't address the structural issue of unused shared component

### B: DataTable Component Rewrite (Selected)
- Rewrite the shared ViewComponent with strongly-typed model, add icon registry to JS, migrate all views to use it
- Pros: Makes polished defaults automatic, reduces per-view boilerplate, single source of truth
- Cons: Larger scope — touching all 7 DataTable views

### C: View-by-View Updates
- Update each view individually with all improvements
- Pros: Fine-grained control
- Cons: Repetitive, inconsistent, high maintenance

## Decision

**Approach B selected.** Key decisions:

- **Zebra/hover**: Use Tabler/Bootstrap 5 built-in `table-striped` and `table-hover` classes applied via the shared component
- **Header icons**: JavaScript keyword-to-icon registry in `datatable-helper.js` that auto-matches column header text — zero config needed per view
- **Status rendering**: Adopt the existing `renderStatus()` helper (Tabler status dots) across all views, replacing raw badge HTML
- **ViewComponent**: Strongly-typed model (`DataTableViewComponent.cs`) replacing the ViewBag-based approach
- **Info padding**: CSS fix in mentoory.css for `.dt-info` padding
- **Static tables**: CSS-only polish (4 static HTML tables get `table-striped table-hover` classes)
- **Scope**: All tables system-wide — 7 DataTable views + 4 static tables across 5 areas

## Open Threads

- Should the icon registry be extensible by individual views (passing extra mappings)?
- Consider standardizing date formatting across tables in a follow-up (views use `toLocaleDateString` instead of the existing `formatRelativeDate()` helper)
