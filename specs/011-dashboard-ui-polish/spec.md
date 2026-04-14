# Feature Specification: Dashboard Rewrite & UI Polish

**Feature Branch**: `011-dashboard-ui-polish`
**Created**: 2026-04-13
**Status**: Draft
**Input**: Corrective follow-up to spec 010. Fix layout bugs caused by incorrect Tabler card-status-start usage, rewrite dashboard with correct patterns, add CSS polish, and QA key pages.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Bug-Free Dashboard Layout (Priority: P1)

An administrator navigates to the Administration Dashboard and sees a clean, well-structured page with stat cards and navigation cards rendered correctly — no vertical text, no full-height colored lines, no overlapping content.

**Why this priority**: The current dashboard is visually broken due to incorrect Tabler markup patterns. This is the core bug fix that must land before any polish work matters.

**Independent Test**: Navigate to `/Administration/Dashboard` as an IncubatorAdmin with at least one user and one project. The page renders with horizontal card layouts, properly contained colored status strips, and readable link text.

**Acceptance Scenarios**:

1. **Given** an admin with an active incubator containing users and projects, **When** they navigate to the Dashboard, **Then** stat cards display user and project counts in a horizontal row with prominent metric values and icon avatars.
2. **Given** the same dashboard, **When** rendered at desktop width (1200px+), **Then** navigation cards display in a 3-column grid with colored left strips contained within each card's boundaries (not stretching to page height).
3. **Given** the same dashboard, **When** looking at navigation card footers, **Then** link text ("Ver proyectos", "Ver usuarios", "Ir a carga masiva") renders horizontally with an arrow icon, never vertically stacked character-by-character.
4. **Given** a new incubator with zero users and zero projects, **When** the admin visits the Dashboard, **Then** the empty state displays with the rocket icon, welcome message, and action buttons — no stat cards or navigation cards shown.
5. **Given** an incubator with 0 projects but 3 users, **When** the admin visits the Dashboard, **Then** the populated state renders (stat cards + navigation cards), not the empty state.

---

### User Story 2 - Visual Polish & Brand Warmth (Priority: P2)

An administrator using the dashboard perceives a polished, professional product — cards have depth via shadows, interactive elements respond to hover with smooth transitions, and the coral brand identity is visually present in accent elements.

**Why this priority**: Once the layout bugs are fixed, visual polish is what transforms the UI from "functional but generic" to "professional product." This is the "wow factor" the user is seeking.

**Independent Test**: Navigate to the Dashboard and interact with cards. Cards have visible shadows at rest, and hovering over navigation cards produces a lift effect with elevated shadow. The coral color appears in avatar backgrounds, status borders, and link hover states.

**Acceptance Scenarios**:

1. **Given** the dashboard is loaded, **When** the admin views stat cards, **Then** each card has a subtle box-shadow creating visual depth against the page background.
2. **Given** the dashboard is loaded, **When** the admin hovers over a navigation card, **Then** the card smoothly lifts (translateY) and its shadow deepens, with the transition completing in approximately 0.2 seconds.
3. **Given** the dashboard is loaded, **When** viewing stat card avatars, **Then** avatar backgrounds use the brand coral color (primary), success green, or other semantic colors — not generic gray.
4. **Given** the dashboard is loaded, **When** viewing stat card metrics, **Then** the count number (e.g., "7") is visually prominent with larger, semi-bold typography, and the description text is smaller and secondary-colored.

---

### User Story 3 - No Regressions on Key Pages (Priority: P3)

After shared CSS changes to `mentoory.css`, other key pages in the application continue to render correctly — no broken layouts, missing styles, or visual regressions.

**Why this priority**: CSS changes to shared styles can ripple across the application. Verification prevents shipping new bugs while fixing old ones.

**Independent Test**: After all CSS changes, navigate to each page listed below and verify layout integrity: sidebar navigation, topbar, card layouts, tables, forms, and auth pages.

**Acceptance Scenarios**:

1. **Given** updated `mentoory.css`, **When** navigating to Users Index (`/Administration/Users`), **Then** the page renders with correct table layout, sidebar navigation, and topbar.
2. **Given** updated `mentoory.css`, **When** navigating to Projects Index (`/Administration/Projects`), **Then** the page renders with correct card/table layout and no styling artifacts.
3. **Given** updated `mentoory.css`, **When** navigating to Diagnostics Index (`/Coordination/Diagnostics`), **Then** the page renders correctly.
4. **Given** updated `mentoory.css`, **When** navigating to Batch Upload Index (`/Administration/BatchUpload`), **Then** the page renders correctly.
5. **Given** updated `mentoory.css`, **When** viewing the sidebar navigation on any page, **Then** active states, hover effects, and section headers render as expected (coral left border on active item, dark hover background).
6. **Given** updated `mentoory.css`, **When** viewing the Login page (`/Access/Login`), **Then** the auth gradient panel and decorative elements render correctly.

---

### Edge Cases

- Single stat card scenario (e.g., if only UserCount metric exists): the card must not stretch to full row width — column classes constrain it.
- Very long incubator names in topbar context display: must not push the avatar/bell off-screen.
- Zero-count metrics in populated state (e.g., 0 projects but 5 users): "0 Proyectos" displays normally, does not trigger empty state.

## Requirements *(mandatory)*

### Functional Requirements

**Dashboard View Rewrite**

- **FR-001**: Dashboard view MUST be rewritten to use correct Tabler `card-status-start` pattern — a child `<div>` with a `bg-*` color class inside the card, never as a class on the card element itself.
- **FR-002**: Stat cards (users, projects) MUST use Tabler's `card-sm` pattern with an avatar-icon, metric value displayed as visually prominent text (large, semi-bold), and a secondary description line.
- **FR-003**: Navigation cards (Proyectos, Usuarios, Carga Masiva) MUST use the correct child-div `card-status-start` pattern with `bg-primary`, `bg-success`, `bg-info` respectively for their colored left strips.
- **FR-004**: Empty state (both UserCount and ProjectCount equal zero) MUST continue using Tabler's `.empty` component with the rocket icon and action buttons unchanged.
- **FR-005**: All card footer links MUST render text horizontally with the trailing arrow icon. No vertical text stacking permitted.

**Shared CSS Polish**

- **FR-006**: Cards MUST have a subtle `box-shadow` at rest and an elevated shadow on hover, with a smooth CSS transition (~0.2s ease).
- **FR-007**: Interactive cards (navigation cards with links) MUST have a hover state that includes a slight vertical lift (`translateY(-2px)` or similar) combined with shadow elevation.
- **FR-008**: Stat metric values MUST use larger font size and semi-bold weight for visual prominence. Secondary descriptions MUST use `text-secondary` styling with smaller font size.
- **FR-009**: Dashboard card rows MUST use Tabler's `row-deck row-cards` pattern for consistent vertical spacing. No custom margin overrides.
- **FR-010**: The coral brand primary color MUST be visually present in accent elements — avatar backgrounds, card-status-start strips, link hover states — not merely defined as a CSS variable.

**Visual QA**

- **FR-011**: After CSS changes, the following pages MUST render correctly without regressions: Administration Dashboard, Users Index, Projects Index, Diagnostics Index, Batch Upload Index.
- **FR-012**: No regression permitted in sidebar navigation styling, topbar layout, or auth page gradient/decorative elements.

### Non-Functional Requirements

- **NFR-001**: All CSS changes go in the existing `mentoory.css` — zero new CSS files.
- **NFR-002**: No JavaScript changes for the dashboard — this is purely markup and CSS.
- **NFR-003**: Build MUST compile with zero warnings (`TreatWarningsAsErrors` is enabled).
- **NFR-004**: All user-facing text MUST remain in Spanish (constitution Principle IX).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Dashboard page loads with zero layout artifacts — no vertical text, no full-height colored lines, no overlapping content. Visual inspection passes on first load.
- **SC-002**: Stat cards display metric counts with clear visual hierarchy — the number is the most prominent element, readable at a glance.
- **SC-003**: Navigation cards show contained colored left strips that do not extend beyond the card boundaries.
- **SC-004**: All cards have visible shadow depth at rest, distinguishing them from the page background.
- **SC-005**: Hovering over navigation cards produces a perceptible lift and shadow change within 0.2 seconds.
- **SC-006**: The coral brand identity (#E07850) is visible in at least 3 UI elements on the dashboard (avatar backgrounds, status strips, link colors).
- **SC-007**: All 5 QA pages (Dashboard, Users, Projects, Diagnostics, Batch Upload) plus the Login page render without visual regressions after CSS changes.

## Out of Scope

- Adding new metrics or data visualizations (charts, trend indicators, recent activity feeds)
- Changing the dashboard layout structure (it stays as stat cards + navigation cards)
- Redesigning pages beyond the dashboard (only QA for regressions, no redesign)
- Mobile responsiveness improvements beyond fixing the current layout bugs
- Dark mode support

## Assumptions

- Tabler v1.4.0 CSS is already installed and loaded via `_Layout.cshtml` — no upgrade needed.
- Tabler Icons Webfont is already available for icon classes (`ti ti-*`).
- `GetDashboardMetricsQuery`, `GetDashboardMetricsQueryHandler`, and `DashboardMetricsDto` are already implemented from spec 010 — no backend changes required.
- The `DashboardController` correctly passes the `DashboardMetricsDto` model to the view — no controller changes needed.
- The existing `mentoory.css` design tokens (`:root` variables) are correct and complete — only component-level styles need additions.
- Other views in the application that use cards may also benefit from the shared CSS polish (shadows, hover effects) — this is an expected positive side effect, not a regression.
