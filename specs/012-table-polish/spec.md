# Feature Specification: Table Polish System-Wide

**Feature Branch**: `012-table-polish`
**Created**: 2026-04-13
**Status**: Draft
**Input**: User description: "Polish all tables across the system — add zebra striping, hover effects, header icons, status dots, and fix info line padding. Rewrite the shared DataTable component to make the right patterns automatic."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Visually Polished Table Rows (Priority: P1)

An administrator navigates to any table view (Users, Projects, Diagnostics, etc.) and sees alternating row backgrounds and a subtle highlight when hovering over a row. This makes the table easier to scan and gives immediate visual feedback that rows are interactive.

**Why this priority**: Zebra striping and hover are the highest-impact, lowest-risk improvements — they affect every table and dramatically improve readability.

**Independent Test**: Navigate to any DataTable view (e.g., Administration > Users). Verify alternating row colors and hover highlight are visible. Verify the effect is consistent across all table views.

**Acceptance Scenarios**:

1. **Given** a table with multiple rows, **When** the page loads, **Then** even-numbered rows have a subtly tinted background distinct from odd rows
2. **Given** a table row, **When** the user hovers over it, **Then** the row background shifts to a highlight color and reverts on mouse-out
3. **Given** a table with an empty state (no data), **When** the empty state message is displayed, **Then** no zebra or hover artifacts interfere with the empty state presentation

---

### User Story 2 - Meaningful Header Icons (Priority: P2)

An administrator viewing any table sees a small, semantically meaningful icon next to each column header text (e.g., a mail icon for email columns, a calendar for dates, a user icon for names). Icons are consistent across all tables — the same column type always gets the same icon.

**Why this priority**: Header icons add visual hierarchy and help users scan columns faster. They require a shared icon registry, which is the foundation of the component rewrite.

**Independent Test**: Navigate to Administration > Users table. Verify each header (Correo Electronico, Nombre, Apellido, Estado, Fecha de Registro) has a meaningful icon. Navigate to Projects and verify the same column types use the same icons.

**Acceptance Scenarios**:

1. **Given** a table with column headers, **When** the page loads, **Then** each header displays a Tabler icon to the left of the header text
2. **Given** two tables that share a column type (e.g., "Estado"), **When** both are viewed, **Then** both display the same icon for that column type
3. **Given** a column with no icon mapping (e.g., a custom or rare column), **When** the header renders, **Then** no icon is shown and the header text displays normally without broken layout

---

### User Story 3 - Status Dots Replace Badges (Priority: P2)

An administrator viewing a table with a status column (Users, Projects) sees Tabler status-dot indicators instead of solid-color badges. Active statuses show a green dot, locked shows red, pending verification shows an animated warning dot. The text next to the dot is clearly readable without contrast issues.

**Why this priority**: The current green badges have poor contrast (white text on green background). Status dots solve the contrast issue and align with Tabler's design language already used in the topbar.

**Independent Test**: Navigate to Administration > Users. Verify the "Estado" column shows colored dots with text instead of solid-color badges. Verify PendingVerification shows an animated dot.

**Acceptance Scenarios**:

1. **Given** a user with Active status, **When** the table renders, **Then** the status column shows a green dot with "Active" text
2. **Given** a user with PendingVerification status, **When** the table renders, **Then** the status column shows an animated warning-colored dot
3. **Given** a project with Inactive status, **When** the table renders, **Then** the status column shows a secondary-colored dot with "Inactivo" text

---

### User Story 4 - Info Line Alignment Fix (Priority: P3)

An administrator viewing any table sees the "Mostrando X a Y de Z registros" info line properly aligned with left padding that matches the table content area.

**Why this priority**: A small visual fix but noticeable — the current lack of padding makes the info line look disconnected from the table.

**Independent Test**: Navigate to any table view. Verify the info line below the table has left padding consistent with the table cell content.

**Acceptance Scenarios**:

1. **Given** a table with data, **When** the info line renders below, **Then** the info text has left padding that aligns with the table's first column content
2. **Given** a table with pagination, **When** viewing the footer area, **Then** both the info line and pagination controls have consistent spacing

---

### User Story 5 - Shared Component Adoption (Priority: P1)

All table views across the system use the shared DataTable component instead of inlining their own `<table>` markup. The component automatically applies zebra, hover, and header icons, so new tables get the polished look without extra work.

**Why this priority**: Without component consolidation, each view must independently apply the polish — leading to inconsistency and maintenance burden. This is the structural enabler for all other stories.

**Independent Test**: Open any table view source and verify it invokes the shared DataTable component rather than containing inline `<table>` markup. Verify all DataTable features (sorting, pagination, empty states) still work.

**Acceptance Scenarios**:

1. **Given** a table view that previously inlined its own `<table>` markup, **When** it is updated, **Then** it uses the shared DataTable component
2. **Given** the shared DataTable component, **When** it renders, **Then** it automatically includes zebra striping, hover classes, and header icons
3. **Given** a view with custom column render functions (e.g., action buttons, description truncation), **When** migrated to the shared component, **Then** custom render functions continue to work unchanged

---

### Edge Cases

- Static HTML tables (BatchUpload Results, AnswerCorrection) receive zebra/hover via CSS only — no header icons or status dots since they are not rendered via JavaScript
- Tables with columns that have no icon mapping display header text without an icon, with no layout disruption
- Empty tables display the existing empty-state component without zebra or hover artifacts
- Skeleton loading placeholders during server-side processing remain visually correct with zebra striping

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST apply alternating row background colors (zebra striping) to all DataTable instances
- **FR-002**: System MUST highlight table rows on hover with a subtle background color shift
- **FR-003**: System MUST display a semantically meaningful Tabler icon in each table column header, mapped by column type through a shared registry
- **FR-004**: The header icon registry MUST map column types to icons consistently: Email to ti-mail, Name to ti-user, Last name to ti-users, Status to ti-circle-check, Date to ti-calendar, Description to ti-file-text, Actions to ti-settings, Stage to ti-list-check, Project to ti-briefcase
- **FR-005**: System MUST render all status indicators using Tabler status-dot components instead of solid-color badges
- **FR-006**: Status-to-color mapping MUST follow: Active/Activo to success, Inactive/Inactivo to secondary, Locked to danger, PendingVerification to warning with animated dot
- **FR-007**: The "Mostrando X a Y de Z registros" info line MUST have left padding aligned with table content
- **FR-008**: All DataTable views MUST use the shared DataTable component rather than inlining their own table markup
- **FR-009**: The shared DataTable component MUST automatically apply zebra striping, hover effects, and header icons
- **FR-010**: Static HTML tables (non-DataTable) MUST receive zebra and hover styling via CSS classes only
- **FR-011**: Custom column render functions defined in individual views MUST continue to work after migration to the shared component

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All DataTable-based views (approximately 14) render with zebra striping, hover effects, and header icons without any per-view CSS overrides
- **SC-002**: No table view contains raw badge markup for status indicators — all use the status-dot rendering pattern
- **SC-003**: The info line below every table has consistent left padding matching the first column content alignment
- **SC-004**: All table views use the shared DataTable component — no inline table markup remains in individual views
- **SC-005**: All existing table features (sorting, pagination, filtering, empty states, skeleton loading) continue to function unchanged after the component rewrite
- **SC-006**: The same column type displays the same icon across every table in the system

## Assumptions

- Tabler Icons webfont is already installed and available via `ti-*` CSS classes
- The existing `datatable-helper.js` helper functions (renderStatus, renderAvatar, renderActions, formatRelativeDate) provide a stable foundation and will be extended, not replaced
- The existing `mentoory.css` brand color tokens provide the palette for zebra and hover tints
- Static HTML tables are few in number and can receive styling through global CSS rules without JavaScript
- The DataTable shared component ViewBag-based API can be extended to accept icon configuration alongside column names
