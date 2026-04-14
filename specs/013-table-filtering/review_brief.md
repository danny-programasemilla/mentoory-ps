# Review Brief: Table Filtering

**Spec:** specs/013-table-filtering/spec.md
**Generated:** 2026-04-13

> Reviewer's guide to scope and key decisions. See full spec for details.

---

## Feature Overview

Adds smart, convention-based filtering to all DataTable views across Mentoory. A collapsible filter panel is auto-generated from each table's column definitions — columns with known render functions (like status) get dropdown filters, others get text inputs. Filter state persists in the URL for bookmarkable/shareable filtered views. Zero per-view markup changes required.

## Scope Boundaries

- **In scope:** Filter panel UI, toggle link, filter type auto-detection registry, per-view overrides, active filter badge counter, URL query param persistence, "Filtrar"/"Limpiar" buttons
- **Out of scope:** Server-side controller/query changes (already supported), static HTML tables, date range pickers, multi-select filters, saved filter presets
- **Why these boundaries:** The server-side `Filters` dictionary in `DataTableServerRequest` and `getActiveFilters()` JS function already exist but are unused. This feature builds the missing UI layer on top of that foundation. Advanced filter types (date ranges, multi-select) can be added to the registry later without rework.

## Critical Decisions

### Auto-detection over explicit declaration
- **Choice:** Filter types are inferred from column render functions via a registry, not declared per-view
- **Trade-off:** Less control per-view, but zero config for the common case. Override mechanism exists for edge cases.
- **Feedback:** Is the convention-based approach clear enough for future developers to extend?

### Panel outside the card
- **Choice:** Filter panel renders between page header and table card, not inside the card
- **Trade-off:** Cleaner separation of concerns, but the panel is visually disconnected from the table
- **Feedback:** Does this placement work for all table layouts in the app?

### Explicit submit over live filtering
- **Choice:** Filters apply only when clicking "Filtrar", not on each keystroke/selection
- **Trade-off:** Extra click required, but avoids unnecessary server calls and gives users control
- **Feedback:** Is this the right UX for tables with small datasets?

## Areas of Potential Disagreement

### Zero-config activation for all tables
- **Decision:** Every `initDataTable()` call automatically gets the filter panel
- **Why this might be controversial:** Some tables may not benefit from filtering (e.g., tables with very few rows or only 2-3 columns)
- **Alternative view:** Opt-in via a `filtering: true` config flag
- **Seeking input on:** Should filtering be opt-out (current) or opt-in?

### URL persistence via replaceState
- **Decision:** Filter values are written to URL query params, making filtered views bookmarkable
- **Why this might be controversial:** Adds complexity; shared URLs with filters may confuse users who expect an unfiltered view
- **Alternative view:** Session-only filters that reset on page reload
- **Seeking input on:** Is URL persistence worth the added complexity for your user base?

## Naming Decisions

| Item | Name | Context |
|------|------|---------|
| Toggle link text | "Filtros" | Spanish UI, shown above table |
| Submit button | "Filtrar" | Applies all filter fields |
| Reset button | "Limpiar" | Clears all fields + URL params |
| Badge format | "Filtros (N)" | N = count of non-empty filters |
| Config property | `filters` | Optional override array in `initDataTable()` config |

## Open Questions

- [ ] Should the filter panel animate with CSS transitions or Bootstrap collapse? (UX detail, deferred to implementation)

## Risk Areas

| Risk | Impact | Mitigation |
|------|--------|------------|
| Auto-detection misidentifies a column's filter type | Med | Per-view override mechanism (FR-010 to FR-012) |
| URL params conflict with existing query params on a page | Low | Use namespaced param keys (e.g., `filter_status`) or scope by tableId |
| Filter panel layout breaks on tables with many columns | Med | Responsive grid wrapping (FR-003) + visual QA on widest table |

---
*Share with reviewers before implementation.*
