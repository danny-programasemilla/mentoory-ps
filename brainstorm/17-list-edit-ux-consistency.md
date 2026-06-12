# Brainstorm: List/Edit UX Consistency

**Date:** 2026-06-11
**Status:** active

## Problem Framing

On `/Platform/Incubators` (and other list pages) three related UX inconsistencies surfaced:

1. **Free-text filters on fixed-value columns.** The "Estado" column is a fixed set
   (Activa/Inactiva) but its filter renders as a free-text input instead of a dropdown.
   This is not Incubators-specific: the same generic boolean status render appears in
   5 tables, none of which get a dropdown filter.
2. **Inconsistent Cancel navigation.** On the Incubators Edit page, "Cancelar" returns
   to the Details (view) page. Users typically arrive at Edit via the edit icon in the
   list table, so Cancel should return them to the list. Every other Create/Edit page in
   the app already returns to the list — Incubators Edit is the lone outlier.
3. **No way to change estado.** The Incubators Edit form only exposes Name and
   Description; there is no UI to change an incubator's estado after creation, and no
   Activate/Deactivate action exists.

### Root causes (from code exploration)
- **Filters:** The 013 table-filtering auto-detection registry (`datatable-helper.js`)
  only recognizes `renderAccountStatus`. Fixed-value columns rendered with the generic
  `renderStatus('Activa'/'Inactiva', ...)` based on a boolean are not matched, so they
  fall through to free-text. Affected tables: Platform/Incubators,
  Administration/Projects, Coordination/Projects, Coordination/Diagnostics,
  Platform/Templates Diagnostics.
- **Cancel:** `Incubators/Edit.cshtml` hardcodes `asp-action="Details"`; all other
  Create/Edit views use `asp-action="Index"` (or the equivalent list action).
- **Estado editing:** `EditIncubatorViewModel` / Edit form carry only Name + Description;
  the edit command does not include IsActive.

## Approaches Considered

### Scope: Systemic vs. Incubators-only (Selected: Systemic)
- **Systemic** — fix the filter auto-detection root cause once (all 5 tables benefit),
  standardize the cancel→list convention, add estado editing for incubators.
  - Pros: Eliminates the class of bug, not just the instance; consistent UX everywhere.
  - Cons: Slightly wider blast radius (filter behavior changes on 5 tables).
- **Incubators-only** — touch just the reported page.
  - Pros: Minimal change.
  - Cons: Leaves the same defect on 4 other tables; contradicts the user's stated
    general principles.

### Estado editing UX (Selected: Toggle on Edit form)
- **Toggle on Edit form** — add an Activa/Inactiva control saved with Name/Description.
  - Pros: One save action; simplest; matches existing edit flow.
  - Cons: No quick state change without entering edit mode.
- **Activar/Desactivar buttons on Details** — separate state-change action.
- **Both** — toggle on Edit + quick buttons on Details.

### Cancel navigation rule (Selected: always → list)
- **Always → list** — Cancel/back on Create/Edit returns to the entity list.
  - Pros: Consistent, simple, no referer tracking; matches the app's existing convention.
  - Cons: If the user reached Edit from Details, they land on the list rather than
    back on Details (accepted trade-off).
- **Smart referer-based back** — track origin and return there. Rejected as over-engineered.

## Decision

**Systemic fix** across three threads:

1. **Dropdown filters for fixed-value status columns.** Make boolean/fixed-set status
   columns auto-render as dropdown filters (Activa/Inactiva, etc.) instead of free-text.
   One root-cause fix benefits all 5 affected tables.
2. **Cancel → list convention.** Standardize Cancel on Create/Edit to return to the
   entity's list; concretely fix the `Incubators/Edit` outlier (Details → Index).
3. **Estado editing for incubators.** Add an Activa/Inactiva toggle to the Incubators
   Edit form, persisted alongside Name/Description on save.

## Key Requirements

- Fixed-value/enumerated status columns in list tables present their filter as a
  dropdown of the valid values (plus an "all" default), not a free-text input.
- The dropdown-filter behavior applies to the 5 currently-affected tables via the same
  mechanism (no per-table copy-paste of the same fix).
- Cancel (and equivalent back actions) on Create/Edit pages return the user to the
  entity's list view when a list exists. Incubators Edit must return to the list.
- The Incubators Edit form includes an Activa/Inactiva control; saving updates the
  incubator's estado together with Name and Description.
- All UI text in Spanish; no regression to the existing 013 filtering behavior on tables
  that already work (e.g., Users with `renderAccountStatus`).

## Out of Scope

- Activar/Desactivar quick buttons on the Details page.
- Estado/status editing for entities other than incubators (Projects, Diagnostics, etc.).
- Referer-based "smart back" navigation.

## Open Questions

- Filter-detection mechanism: extend the JS registry to recognize the generic
  active/inactive render, vs. per-view dropdown option overrides — a HOW detail for the
  plan phase.
- Are dropdown option labels derived automatically from the rendered values or declared
  per table?
- Domain: does deactivating an incubator have downstream/cascading effects, or is it a
  pure status flag with no side effects?
