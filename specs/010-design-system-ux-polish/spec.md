# Feature Specification: Mentoory Design System & UX Polish

**Feature Branch**: `010-design-system-ux-polish`  
**Created**: 2026-04-13  
**Status**: Draft  
**Input**: User description: "Elevate Mentoory's UI from a functional Tabler adoption to a polished, brand-aligned product experience across all views"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Brand-Consistent Visual Identity (Priority: P1)

As any user of Mentoory, when I navigate the application I see a cohesive visual identity that reflects the Mentoory brand (warm coral/salmon primary, golden yellow and magenta accents) instead of generic Bootstrap blue, making the platform feel like a professional, purpose-built product for entrepreneur incubation.

**Why this priority**: The brand theme is the foundation that all other visual changes build upon. Without the correct color palette applied globally, no other UI improvements will feel cohesive.

**Independent Test**: Can be tested by loading any page in the application and verifying that all interactive elements (buttons, links, badges, focus states) use the Mentoory coral primary (#E07850) instead of Bootstrap blue (#0d6efd). The page background should be #F9FAFB and borders #E5E7EB.

**Acceptance Scenarios**:

1. **Given** a user loads any authenticated page, **When** the page renders, **Then** all primary-colored elements (buttons, links, active states, focus rings) use #E07850 coral and no Bootstrap blue (#0d6efd) is visible anywhere
2. **Given** the `mentoory.css` file, **When** inspected, **Then** it contains the complete Mentoory design token palette as CSS custom properties and the corresponding Tabler variable overrides
3. **Given** a user interacts with form inputs, **When** an input receives focus, **Then** the focus ring uses the Mentoory primary color

---

### User Story 2 - Professional Navigation Shell (Priority: P1)

As an authenticated user, I see a polished navigation shell with the Mentoory logo in the sidebar, a topbar with my avatar and dropdown menu, and clear visual hierarchy in the sidebar sections, making navigation feel intuitive and the product feel mature.

**Why this priority**: The navigation shell is visible on every single page. Improving it has the highest visual impact per unit of effort and sets the tone for the entire application experience.

**Independent Test**: Can be tested by logging in and verifying: sidebar shows the Mentoory logo image (not text), section headers are visually distinct, active menu item has a colored left border, topbar shows an initials-based avatar with a working dropdown menu (profile placeholder, change context, logout).

**Acceptance Scenarios**:

1. **Given** a user is authenticated, **When** they view the sidebar, **Then** the Mentoory logo SVG (white version) is displayed at the top instead of plain text
2. **Given** a user is authenticated, **When** they view the sidebar, **Then** section headers ("Gestion de Plataforma", "Administracion del Contexto") have subtle uppercase and letter-spacing styling
3. **Given** a user navigates to a page, **When** the corresponding menu item is active, **Then** it has a colored left border (#E07850) and an active background (#2E3B55)
4. **Given** a user hovers over a sidebar menu item, **When** the cursor is over the item, **Then** the item background changes to #243047
5. **Given** a user is authenticated, **When** they view the topbar, **Then** they see their initials-based avatar (coral background) with a dropdown containing: profile placeholder, change context, and logout options
6. **Given** a user is authenticated, **When** they view the topbar, **Then** they see a notification bell icon placeholder (non-functional, ready for future implementation)
7. **Given** a user views the topbar, **When** context is active, **Then** the role, incubator, and project are displayed with Tabler status indicators instead of plain text and badge

---

### User Story 3 - Rich Data Tables (Priority: P2)

As an administrator or coordinator browsing entity lists (users, projects, diagnostics, incubators), I see polished data tables with avatars, status indicators, relative dates, and clear action buttons, making it easy to scan and act on data at a glance.

**Why this priority**: Tables are the most frequently used component across the application. They appear in Administration, Coordination, Participant, and Platform areas. Upgrading them has broad visual impact.

**Independent Test**: Can be tested by navigating to any table view (e.g., Users Index) and verifying: table uses vertical centering, user rows show initials-based avatars, status is shown as Tabler status dots (not plain badges), dates show relative time with exact-date tooltips, and action buttons appear as icon-only buttons in a narrow column.

**Acceptance Scenarios**:

1. **Given** a table view loads, **When** the table renders, **Then** it uses `table table-vcenter` classes (not `table-striped`)
2. **Given** the Users table, **When** rows render, **Then** each user row shows an initials-based avatar alongside their name and email in a single compound cell
3. **Given** a table with status data, **When** rows render, **Then** active states use Tabler's `status` component with animated dots instead of plain Bootstrap badges
4. **Given** a table with date columns, **When** rows render, **Then** dates display as relative time in Spanish ("hace 3 dias") with a tooltip showing the exact date
5. **Given** a table with actionable rows, **When** rows render, **Then** action buttons appear as icon-only buttons (view, edit, delete) using Tabler's `btn-actions` pattern in a narrow `w-1` column
6. **Given** a table view, **When** the table is wrapped in a card, **Then** the card header shows the entity name as card-title with a count badge
7. **Given** a table view with no data, **When** the table renders, **Then** a Tabler `.empty` component is displayed with a relevant icon, descriptive message, and a primary action button (e.g., "Inscribir Usuario")
8. **Given** a table view, **When** the DataTable is fetching data via AJAX, **Then** skeleton placeholders (`placeholder-glow`) are displayed instead of a blank table

---

### User Story 4 - Polished Forms (Priority: P2)

As a user filling out forms (enrollment, project creation, configuration), I see well-organized forms with clear section dividers, helpful hints, and a professional card structure that guides me through the input process.

**Why this priority**: Forms are the second most common component and the primary interaction point for data entry. Professional forms reduce errors and improve the user experience.

**Independent Test**: Can be tested by navigating to the Enroll User page and verifying: the form is wrapped in a card with header and footer, section dividers use Tabler `hr-text` component, action buttons are in a `btn-list` wrapper with cancel as ghost-secondary style.

**Acceptance Scenarios**:

1. **Given** a form with multiple sections, **When** the form renders, **Then** section headers use Tabler's `hr-text` dividers instead of plain `<h5>` elements
2. **Given** a form card, **When** rendered, **Then** it has a `card-header` with the form title, `card-body` for fields, and `card-footer` for action buttons
3. **Given** form action buttons, **When** rendered, **Then** they are wrapped in a `btn-list` container with the primary action right-aligned and cancel styled as `btn-ghost-secondary`
4. **Given** a form field that benefits from guidance, **When** rendered, **Then** a hint text appears below the input using Tabler's `form-hint` class
5. **Given** a form with validation errors, **When** validation fails, **Then** error messages are styled consistently using Tabler's validation pattern

---

### User Story 5 - Informative Dashboard (Priority: P2)

As an administrator, when I land on my dashboard, I see real metric counts (total users, projects, diagnostics) in stat cards at a glance, plus recent activity and quick actions, making the dashboard a useful operational hub rather than a page of links.

**Why this priority**: The dashboard is the first thing users see after login. Transforming it from link cards to an informative hub makes the product feel data-driven and valuable.

**Independent Test**: Can be tested by logging in as an administrator and verifying: top row shows stat cards with real counts from the database, cards use `card-sm` with avatar-icon pattern and `row-deck row-cards` for equal heights, and quick action buttons are available.

**Acceptance Scenarios**:

1. **Given** an admin accesses the dashboard, **When** the page loads, **Then** stat cards display real counts for users, projects, and diagnostics fetched from the database
2. **Given** the dashboard layout, **When** rendered, **Then** stat cards use Tabler's `card-sm` pattern with colored avatar-icon, metric value, and secondary description text
3. **Given** multiple stat cards, **When** rendered in a row, **Then** they use `row-deck row-cards` classes for consistent equal heights
4. **Given** the dashboard, **When** link/navigation cards are displayed, **Then** they include `card-status-start` colored strips to differentiate categories
5. **Given** a new incubator with no data, **When** the dashboard loads, **Then** a welcoming empty state is shown with setup action buttons instead of zeros

---

### User Story 6 - Branded Auth Experience (Priority: P3)

As a visitor on the login, registration, or password recovery pages, I see a split-panel layout with the Mentoory brand gradient, white logo, a tagline, and a decorative illustration on the left, alongside a clean form on the right, making authentication feel like entering a professional, modern product.

**Why this priority**: Auth pages are the first impression for new users. While important for brand perception, they are visited less frequently than the main application, making them lower priority than in-app improvements.

**Independent Test**: Can be tested by navigating to the login page in an incognito window and verifying: left panel shows the yellow-to-magenta gradient with white logo SVG, a tagline in Spanish, and a decorative SVG illustration; right panel shows a clean form with brand-colored focus states and primary button.

**Acceptance Scenarios**:

1. **Given** a visitor navigates to the login page, **When** the page renders, **Then** the left panel displays the Mentoory brand gradient (linear-gradient 135deg from #F5B731 through #E07850 to #D946A8)
2. **Given** the auth left panel, **When** rendered, **Then** it shows the white Mentoory logo SVG centered, a tagline ("Impulsa tu emprendimiento") below, and a decorative SVG illustration representing growth/entrepreneurship
3. **Given** the auth right panel, **When** rendered, **Then** the form area has a max-width of 420px, with form title, subtitle, styled inputs, and a primary button in the Mentoory coral color
4. **Given** the auth page illustration, **When** implemented, **Then** it uses CSS-based geometric shapes or inline SVG with no external image dependencies

---

### User Story 7 - Consistent View Application (Priority: P3)

As any user navigating between different areas (Administration, Coordination, Participant, Platform), I experience visual consistency where the same patterns (tables, forms, cards, empty states) are used uniformly, making the entire application feel like a single cohesive product.

**Why this priority**: Consistency across all views prevents the "patchwork" feeling where some pages look polished and others don't. This is the final layer that ties everything together.

**Independent Test**: Can be tested by navigating through representative pages in each area and verifying: all table views use the same rich table pattern, all forms use hr-text dividers and card structure, all empty states use the Tabler `.empty` component, and no view still uses the old Bootstrap blue styling.

**Acceptance Scenarios**:

1. **Given** any table view across any area, **When** rendered, **Then** it follows the rich table pattern (card wrapper, vcenter, status dots, action buttons, empty state)
2. **Given** any form view across any area, **When** rendered, **Then** it follows the form pattern (card structure, hr-text dividers, btn-list actions)
3. **Given** the Participant Diagnostic flow, **When** the user takes a diagnostic, **Then** a step indicator (Tabler `steps` component) appears at the top showing progress
4. **Given** Platform configuration views, **When** displaying key-value settings, **Then** the Tabler `datagrid` component is used
5. **Given** any view across all 5 areas, **When** inspected, **Then** no Bootstrap blue (#0d6efd) is visible in any element

---

### Edge Cases

- Users with very long names: compound avatar cells truncate names with CSS text-overflow ellipsis
- Empty dashboards (new incubator): show a welcoming empty state with setup actions rather than stat cards with zeros
- Sidebar with many menu items: sidebar container is scrollable with overflow-y auto, no content overflow
- Mobile/small screens: sidebar collapses, topbar stacks gracefully using existing Tabler responsive behavior
- DataTables with 0 rows: Tabler `.empty` component replaces the default "No hay datos disponibles" DataTables message
- Logo SVG unavailable: sidebar falls back to text "Mentoory" (graceful degradation)
- Avatar for users without names: show a generic user icon instead of empty initials

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST define the complete Mentoory color palette as CSS custom properties in `mentoory.css`, including the full primary scale (50-900), accent colors, semantic colors, surface colors, sidebar colors, and Tabler variable overrides
- **FR-002**: System MUST override Tabler's default primary color so all components (buttons, badges, alerts, links, form focus states) use #E07850 instead of Bootstrap blue
- **FR-003**: Sidebar MUST display the Mentoory logo as an SVG image (white version) instead of text
- **FR-004**: Sidebar section headers MUST use subtle uppercase and letter-spacing styling for visual separation
- **FR-005**: Sidebar active menu items MUST show a colored left border (#E07850) with an active background (#2E3B55)
- **FR-006**: Sidebar hover states MUST use the #243047 background color
- **FR-007**: Sidebar MUST support badge counts on menu items, rendered server-side via IMenuService
- **FR-008**: TopBar MUST display a user initials-based avatar with dropdown menu (profile placeholder, change context, logout)
- **FR-009**: TopBar MUST include a notification bell icon placeholder (non-functional)
- **FR-010**: TopBar MUST display context (role, incubator, project) using Tabler status indicators
- **FR-011**: Footer MUST show "Mentoory" with current year on the left and version text on the right
- **FR-012**: All tables MUST use `table table-vcenter` classes without `table-striped`
- **FR-013**: Table views MUST be wrapped in cards with a card-header containing the entity title and count badge
- **FR-014**: User table columns MUST render as compound cells with initials-based avatar, name, and email
- **FR-015**: Status table columns MUST use Tabler's `status` component with animated dots for active states
- **FR-016**: Date table columns MUST display relative time in Spanish with exact-date tooltips
- **FR-017**: Action table columns MUST use icon-only buttons in Tabler's `btn-actions` pattern within `w-1` columns
- **FR-018**: All table views MUST display a Tabler `.empty` component when no data exists
- **FR-019**: Tables MUST show skeleton placeholders during AJAX data loading
- **FR-020**: Forms with multiple sections MUST use Tabler `hr-text` dividers instead of `<h5>` headers
- **FR-021**: Form cards MUST use card-header for title, card-body for fields, card-footer for actions
- **FR-022**: Form action buttons MUST use `btn-list` wrapper with primary action right-aligned and cancel as `btn-ghost-secondary`
- **FR-023**: Dashboard stat cards MUST display real metric counts from the database (user, project, diagnostic counts)
- **FR-024**: Dashboard stat cards MUST use Tabler's `card-sm` pattern with `row-deck row-cards` for equal heights
- **FR-025**: Navigation/link cards MUST include `card-status-start` colored strips by category
- **FR-026**: Auth pages left panel MUST display the brand gradient background with white logo SVG, tagline, and decorative illustration
- **FR-027**: Auth pages right panel MUST contain a clean form area (max-width 420px) with brand-colored elements
- **FR-028**: Auth page illustration MUST use CSS-based shapes or inline SVG (no external image dependency)
- **FR-029**: Confirmation modals MUST use Tabler's `modal-status` colored strip pattern
- **FR-030**: All ~45 views across 5 areas (Access, Administration, Coordination, Participant, Platform) MUST be updated to use the new design patterns

### Key Entities

- **MenuItem**: Extended with a `BadgeCount` property (nullable int) for rendering server-side badge counts in the sidebar
- **DashboardMetrics**: Value object containing user count, project count, and diagnostic count for stat card rendering

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: No Bootstrap blue (#0d6efd) is visible on any page of the application — all primary-colored elements use the Mentoory coral (#E07850)
- **SC-002**: 100% of table views (Users, Projects, Diagnostics, Incubators, Templates, Participant Diagnostics) display a meaningful empty state when no data exists
- **SC-003**: 100% of forms with multiple sections use `hr-text` dividers instead of plain HTML headings
- **SC-004**: The admin dashboard displays real database-sourced counts in stat cards within 2 seconds of page load
- **SC-005**: The sidebar displays the Mentoory logo image on every authenticated page
- **SC-006**: The topbar displays a user avatar with a functional dropdown menu on every authenticated page
- **SC-007**: Auth pages (login, register, password recovery) display the brand gradient with logo and illustration
- **SC-008**: Visual consistency across all 5 areas: the same card pattern, table pattern, and form pattern is applied uniformly
- **SC-009**: `mentoory.css` is the single source of truth for all brand color overrides (no inline color styles, no scattered CSS files)
- **SC-010**: All existing E2E tests pass without modification (or with minimal selector updates)
- **SC-011**: The application builds with zero warnings (TreatWarningsAsErrors)

## Assumptions

- The Mentoory logo SVG files (white version for sidebar, colored for auth) can be derived from the existing PDF or created as new SVG assets. If creation is needed, it is within scope of this feature
- The Inter font (Tabler's default) is already available through the Tabler CSS bundle — no additional font installation needed
- Dashboard metric counts will be implemented as new MediatR query handlers that query existing EF Core repositories
- The IMenuService interface can be extended with badge count support without breaking existing implementations
- The decorative illustration for auth pages will be implemented as CSS geometric shapes or inline SVG rather than requiring an external illustration asset
- DataTables library (currently v2.3.4) supports the custom rendering patterns needed for compound cells, status dots, and relative dates via existing column render functions
- Existing E2E test selectors are based on data-testid attributes and standard element types, not on specific CSS classes that will change
- The application currently has ~45 views across 5 areas as counted during exploration — the exact count may vary slightly

## Out of Scope

- Dark mode toggle
- New feature development or new pages/flows
- Backend changes beyond IMenuService badge counts and dashboard metric queries
- Full WCAG accessibility audit
- Internationalization beyond existing Spanish UI text
- Mobile-first responsive redesign (existing Tabler responsive behavior is preserved)
- Animations or transitions beyond Tabler's built-in defaults
- User profile image upload functionality
- Functional notification system (bell icon is placeholder only)
- JavaScript framework changes (keep jQuery + DataTables as-is)
