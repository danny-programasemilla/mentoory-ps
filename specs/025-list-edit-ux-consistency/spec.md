# Feature Specification: List/Edit UX Consistency

**Feature Branch**: `025-list-edit-ux-consistency`

**Created**: 2026-06-11

**Status**: Draft

**Input**: User description: "List/Edit UX consistency (systemic): dropdown filters for fixed-value status columns across list tables, a consistent Cancel→list navigation convention on Create/Edit pages, and the ability to change an incubator's estado from its Edit form."

## Clarifications

### Session 2026-06-11

- Q: What form should the incubator estado control on the Edit form take? →
  A: A labeled on/off toggle (switch) reading Activa / Inactiva, presented alongside the
  existing name and description fields (consistent with the brainstorm decision). The
  exact widget styling is a UI detail for planning, but the control is binary
  (Activa/Inactiva), not a multi-value selector.
- Q: Does the Cancel→list convention (FR-008) require changing every Create/Edit page? →
  A: No. Only the Incubators Edit page currently violates the convention and requires a
  change. All other Create/Edit pages already return to their list and MUST remain
  unchanged; for them this is a verify-only confirmation, not a modification.
- Q: Are status filter dropdowns limited to the two-value Activa/Inactiva case? →
  A: No. A status filter dropdown MUST offer the full set of values that the column can
  display (e.g. the diagnostic synchronization states, which are more than two), not just a
  boolean Activa/Inactiva pair. The precise option set per table is determined during
  planning from each column's existing displayed values.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Filter list tables by a fixed-value status using a dropdown (Priority: P1)

A user viewing a list/table page (for example, the Incubators list) wants to narrow the
results by a status column whose values come from a fixed, known set (e.g.
"Estado" = Activa / Inactiva). When they open the table's filter panel, the status
field offers a dropdown of the valid values plus an "all" default, so they can pick a
value instead of having to type free text and guess the exact wording.

**Why this priority**: This is the originally reported pain point and the
highest-leverage fix — a single root-cause correction restores correct dropdown
filtering across every affected table, immediately improving the find-a-record
experience platform-wide.

**Independent Test**: Open the Incubators list, open the filter panel, confirm the
"Estado" filter is a dropdown containing Activa, Inactiva, and an "all" default; select
"Inactiva" and confirm the table shows only inactive incubators. Repeat for each of the
other affected tables.

**Acceptance Scenarios**:

1. **Given** a list table with a fixed-value status column, **When** the user opens the
   filter panel, **Then** the status field is rendered as a dropdown whose options are
   the valid status values plus an "all"/no-filter default, not a free-text input.
2. **Given** the status filter dropdown, **When** the user selects a specific value and
   applies the filter, **Then** only rows matching that status are shown.
3. **Given** the status filter dropdown set to a specific value, **When** the user
   clears the filter (or selects the "all" default), **Then** the table shows rows of
   all statuses again.
4. **Given** a table that already filters its status column correctly (Administration
   Users, Platform Users), **When** this feature ships, **Then** that table's status
   filtering continues to work unchanged (no regression).

---

### User Story 2 - Cancel on an Edit/Create page returns to the entity list (Priority: P2)

A user reaches the Edit page for an entity by clicking the edit icon in a list table.
After deciding not to make changes, they click "Cancelar" and expect to be returned to
the list they came from, consistent with every other Create/Edit page in the
application. Today the Incubators Edit page is the one exception that returns them to the
detail (view) page instead.

**Why this priority**: It is a smaller, localized correction than US1, but it removes a
jarring inconsistency. Most pages already follow the convention; this aligns the lone
outlier and documents the rule so future pages stay consistent.

**Independent Test**: From the Incubators list, click the edit icon for a row, then click
"Cancelar" on the Edit page; confirm the user lands on the Incubators list (not the
detail page). Confirm all other Create/Edit pages already behave this way.

**Acceptance Scenarios**:

1. **Given** the Incubators Edit page, **When** the user clicks "Cancelar", **Then** they
   are returned to the Incubators list view.
2. **Given** any other Create/Edit page in the application that has an associated list,
   **When** the user clicks "Cancelar", **Then** they are returned to that entity's list
   view (the convention holds uniformly).

---

### User Story 3 - Change an incubator's estado from the Edit form (Priority: P3)

A user editing an incubator wants to change its estado (Activa / Inactiva) in addition to
its name and description. The Edit form presents an estado control alongside the existing
fields, and saving the form persists the chosen estado together with the other changes.

**Why this priority**: It adds a new capability rather than fixing an existing defect, and
it is scoped to a single entity (incubators). It depends on nothing in US1/US2 and can
ship independently, but it is the lowest-urgency of the three.

**Independent Test**: Open the Edit form for an active incubator, change its estado to
Inactiva, save, and confirm the incubator's estado is now Inactiva on the detail page and
in the list (and that the list's status filter can then find it under Inactiva).

**Acceptance Scenarios**:

1. **Given** the Incubators Edit form, **When** the user views it, **Then** an estado
   control (Activa / Inactiva) is shown alongside name and description.
2. **Given** the Edit form with the estado control set to a value different from the
   current one, **When** the user saves, **Then** the incubator's estado is updated to the
   chosen value and persisted.
3. **Given** the Edit form, **When** the user changes only the estado (leaving name and
   description unchanged) and saves, **Then** the estado change is persisted and the other
   fields are unaffected.
4. **Given** the estado was changed to Inactiva and saved, **When** the user returns to
   the incubator's detail page and the list, **Then** both reflect the Inactiva estado.

---

### Edge Cases

- **No matching rows**: When a status filter value matches zero rows, the table shows its
  standard empty state rather than an error.
- **All-default behaves as no filter**: Selecting the "all" default for a status filter
  must be equivalent to not filtering on that column at all.
- **Cancel after partial edits**: Clicking "Cancelar" on the Edit form discards any
  unsaved changes (including an unsaved estado change) and returns to the list; nothing is
  persisted.
- **Saving estado unchanged**: Saving the Edit form without changing the estado leaves the
  estado value as-is.
- **Single-value / empty status sets**: A table whose status column currently shows only
  one value still presents the dropdown with the full set of valid values plus the "all"
  default (the dropdown reflects the valid domain, not just values currently present).
- **Spanish wording**: All filter labels, options, the estado control, and the Cancel
  action use Spanish, consistent with the rest of the UI.

## Requirements *(mandatory)*

### Functional Requirements

#### Fixed-value status filters (US1)

- **FR-001**: List/table pages MUST present the filter for a fixed-value/enumerated status
  column as a dropdown of the valid status values, not a free-text input.
- **FR-002**: Each status filter dropdown MUST include an "all"/no-filter default option
  that, when selected, applies no filtering on that column.
- **FR-003**: Selecting a specific value in a status filter dropdown and applying the
  filter MUST restrict the displayed rows to those matching the selected status.
- **FR-004**: The dropdown-filter behavior MUST apply to all currently-affected list
  tables — Platform Incubators, Administration Projects, Coordination Projects,
  Coordination Diagnostics, and Platform Templates Diagnostics — via a single shared
  mechanism rather than table-by-table duplication.
- **FR-005**: The change MUST NOT regress list tables whose status filtering already works
  correctly today (Administration Users, Platform Users).
- **FR-006**: All status filter labels and option values MUST be presented in Spanish.

#### Cancel → list navigation (US2)

- **FR-007**: The Cancel action on the Incubators Edit page MUST return the user to the
  Incubators list view (not the detail/view page).
- **FR-008**: The Cancel action on Create/Edit pages across the application MUST return the
  user to the corresponding entity's list view whenever such a list exists; this is the
  standard convention and MUST hold uniformly. Apart from the Incubators Edit page
  (FR-007), all existing Create/Edit pages already satisfy this and MUST remain unchanged
  (verify-only).

#### Estado editing for incubators (US3)

- **FR-009**: The Incubators Edit form MUST present a control allowing the user to set the
  incubator's estado to Activa or Inactiva, alongside the existing name and description
  fields.
- **FR-010**: Saving the Incubators Edit form MUST persist the selected estado together
  with the name and description in a single save action.
- **FR-011**: After an estado change is saved, the incubator's estado MUST be reflected
  consistently on its detail page and in the list view (including being findable via the
  list's status filter).
- **FR-012**: Cancelling the Incubators Edit form MUST discard any unsaved estado change
  (no persistence), consistent with FR-007.
- **FR-013**: The estado control and its option labels MUST be presented in Spanish.

### Key Entities *(include if feature involves data)*

- **Incubator**: A business incubator managed in the platform. Relevant attributes for
  this feature: name, description, and estado (active/inactive status). This feature adds
  the ability to change estado after creation; name and description editing already exists.
- **List status column**: A conceptual column on list/table pages whose displayed value is
  drawn from a fixed, enumerated set (e.g. Activa/Inactiva, Activo/Inactivo, and the
  diagnostic sync states). This feature governs how such columns are filtered.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: On all 5 affected list tables, the status column's filter is presented as a
  dropdown (0 of the 5 still uses a free-text input for the status column).
- **SC-002**: A user can filter any affected table to a single status value in one
  interaction (select value + apply) without typing the value manually.
- **SC-003**: The 2 already-correct tables (Administration Users, Platform Users) retain
  their working status filtering with zero behavioral regressions.
- **SC-004**: Cancelling from the Incubators Edit page lands the user on the Incubators
  list 100% of the time, matching the behavior of every other Create/Edit page.
- **SC-005**: A user can change an incubator's estado and save it from the Edit form in a
  single save action, and the new estado is visible on the detail page and list and is
  findable via the status filter.

## Assumptions

- The existing table-filtering capability (the filter panel, apply/clear behavior, and URL
  persistence introduced previously) remains in place; this feature corrects which filter
  control type is used for fixed-value status columns and does not redesign the filter
  panel itself.
- "Estado" for an incubator is a two-value set (Activa / Inactiva) reflecting an
  active/inactive flag; no additional status values are introduced by this feature.
- Deactivating an incubator is treated as a pure status change with no cascading side
  effects on related entities unless the planning phase surfaces an existing domain rule
  that requires otherwise (flagged as an open question for planning).
- The Cancel→list convention applies only where an entity list view exists; pages without
  a list (if any) are unaffected.
- All affected list tables and edit pages already exist; this feature modifies their
  behavior rather than introducing new pages.
- The set of "currently-affected tables" (5) and "already-correct tables" (2) is treated as
  the known inventory at specification time; if planning discovers additional fixed-value
  status columns, the same shared mechanism should cover them, but enumerating new tables
  is not a goal of this feature.

## Out of Scope

- Activar/Desactivar quick-action buttons on the incubator detail page (estado is changed
  via the Edit form only).
- Estado/status editing for entities other than incubators (e.g. Projects, Diagnostics).
- Referer-based "smart back" navigation that returns the user to wherever they came from;
  the convention is the simpler, uniform "Cancel returns to the list".
- Redesigning the filter panel, adding new filterable columns, or changing filtering for
  non-status (free-text) columns.
