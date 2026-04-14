# Review Brief: Table Polish System-Wide

**Spec:** specs/012-table-polish/spec.md
**Generated:** 2026-04-13

> Reviewer's guide to scope and key decisions. See full spec for details.

---

## Feature Overview

Polish all tables across Mentoory — both DataTable (server-side JS) and static HTML tables — with zebra striping, hover effects, header icons, Tabler status-dot indicators, and info line padding fixes. The shared DataTable ViewComponent is rewritten with a strongly-typed model so polished defaults are automatic for current and future tables. Scope is 7 DataTable views and 4 static tables across 5 areas.

## Scope Boundaries

- **In scope:** Zebra rows, hover, header icons (via icon registry), status dots replacing badges, info line padding, ViewComponent rewrite, migration of all 7 DataTable views to shared component, CSS-only polish for 4 static tables
- **Out of scope:** Table filtering/search UI, column resizing/reordering, responsive breakpoints, pagination style changes, non-table UI components, date formatting standardization
- **Why these boundaries:** This is a visual polish pass — structural table functionality (sorting, pagination, filtering) is not changing. Date formatting consistency is a potential follow-up but out of scope here to keep the feature focused.

## Critical Decisions

### Header Icons: JS-based auto-matching vs. server-side rendering
- **Choice:** JavaScript keyword-to-icon map in `datatable-helper.js` that auto-matches column header text
- **Trade-off:** Slightly less explicit than server-side icon configuration, but requires zero changes in individual views — icons are automatic by column name
- **Feedback:** Is auto-matching by header text reliable enough, or should views explicitly declare icons?

### Status Rendering: Adopt existing `renderStatus()` helper
- **Choice:** Replace all raw `<span class="badge bg-*">` with calls to the existing but unused `renderStatus()` helper
- **Trade-off:** None significant — the helper already exists and produces correct Tabler status-dot HTML
- **Feedback:** Straightforward adoption. The helper was built in spec 010 but never used.

### ViewComponent: Strongly-typed model replacing ViewBag
- **Choice:** New `DataTableViewComponent.cs` with `TableId`, `Columns` (string[]), optional `Title`
- **Trade-off:** Breaking change for any views currently using the shared component (none currently do, so zero impact)
- **Feedback:** Is `string[]` for columns sufficient, or should the model support richer column metadata?

## Areas of Potential Disagreement

### Auto-matching icons by header text
- **Decision:** Icon registry matches column header keywords (e.g., "correo" → mail icon, "fecha" → calendar)
- **Why this might be controversial:** If a column header doesn't contain a recognized keyword, it silently gets no icon — could be confusing for future developers adding new tables
- **Alternative view:** Explicit icon configuration per view gives full control
- **Seeking input on:** Is the convenience of zero-config icons worth the implicit behavior?

### Scope limited to visual polish only
- **Decision:** No changes to date formatting, filtering UI, or pagination style
- **Why this might be controversial:** While we're touching every table view, it could be efficient to also standardize date formatting (views use `toLocaleDateString` instead of the existing `formatRelativeDate()` helper)
- **Alternative view:** Bundle date formatting into this spec to avoid a second pass
- **Seeking input on:** Should date formatting standardization be added or kept as a follow-up?

## Naming Decisions

| Item | Name | Context |
|------|------|---------|
| ViewComponent model | `DataTableViewComponent` | Replaces ViewBag-based approach |
| Icon registry | `COLUMN_ICON_MAP` | JS object in datatable-helper.js |
| Icon applicator | `applyHeaderIcons(tableId)` | Called from initDataTable callback |

## Open Questions

- [ ] Should the icon registry be extensible by individual views (e.g., passing extra mappings)?

## Risk Areas

| Risk | Impact | Mitigation |
|------|--------|------------|
| Zebra/hover interferes with empty state or skeleton loading | Medium | Explicit edge case in spec + visual QA phase |
| Auto-icon matching fails on unexpected column header text | Low | Graceful fallback — no icon shown, no broken layout |
| ViewComponent migration breaks custom render functions | Medium | Custom renders stay in view JS; only table markup moves to component |

---
*Share with reviewers before implementation.*
