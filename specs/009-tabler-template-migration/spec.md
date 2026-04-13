# Feature Specification: Tabler Admin Template Migration

**Feature Branch**: `009-tabler-template-migration`
**Created**: 2026-04-13
**Status**: Draft
**Input**: User description: "Change the UX/UI and integrate the Tabler template (https://tabler.io/admin-template). Changing, removing, adding as necessary to make Mentoory use Tabler instead of the current HTML implementation, still using ASP.NET MVC."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Authenticated Dashboard Navigation (Priority: P1)

An authenticated user logs in and sees the Mentoory dashboard with a Tabler-styled dark vertical sidebar, page header with context display, and page body. The sidebar shows menu groups driven by `MenuConfiguration.cs` with Tabler Icons. The user can navigate between areas, see active state highlighting, and collapse the sidebar on mobile.

**Why this priority**: The layout shell (sidebar + page wrapper) is the foundation every other page depends on. Nothing else works visually until this is in place.

**Independent Test**: Log in as any role, verify the sidebar renders with correct menu items, icons, section separators. Navigate between pages and confirm active state. Resize browser to mobile width and verify hamburger collapse.

**Acceptance Scenarios**:

1. **Given** an authenticated user, **When** they access any page, **Then** they see a dark vertical sidebar (Tabler `.navbar-vertical` pattern) with Tabler Icons, section separators ("Gestión de Plataforma", "Administración del Contexto"), and the page body in Tabler's `.page-wrapper` structure.
2. **Given** an authenticated user on mobile, **When** the viewport is below 576px, **Then** the sidebar collapses to a hamburger menu that can be toggled open.
3. **Given** an authenticated user navigating to a page, **When** that page's menu item exists in the sidebar, **Then** the corresponding nav-link has the `.active` class.

---

### User Story 2 - Context Display and Switching (Priority: P1)

An authenticated user sees their active role, incubator, and project in the top bar area. They can click "Cambiar contexto" to open the context-switcher modal, which works identically to the current implementation.

**Why this priority**: Context switching is critical platform functionality. It must survive the template migration without regression.

**Independent Test**: Log in as a user with multiple roles. Verify context badge, incubator name, and project name display correctly. Click "Cambiar contexto", verify modal opens, cascade dropdowns work, and context switches successfully.

**Acceptance Scenarios**:

1. **Given** an authenticated user with an active context, **When** they view any page, **Then** they see their role badge, incubator name, and project name in the top bar area styled with Tabler classes.
2. **Given** an authenticated user, **When** they click "Cambiar contexto", **Then** the context-switcher modal opens with cascading dropdowns (Role -> Incubator -> Project) fully functional.
3. **Given** an authenticated user in the context-switcher modal, **When** they select a new context and confirm, **Then** the context switches and the page reloads with the new context displayed.

---

### User Story 3 - Auth Pages with Split-Panel Layout (Priority: P2)

An unauthenticated user visiting Login, Register, ForgotPassword, ResetPassword, VerifyEmail, or ChangePassword sees a split-panel layout: left side shows a branded placeholder panel (solid color + Mentoory text), right side shows the form. No sidebar or topbar is visible.

**Why this priority**: Auth pages are the first touchpoint for users. The split-panel layout gives a professional impression, but the app is functional without it (current centered card still works).

**Independent Test**: Visit the login page without being authenticated. Verify the split-panel layout renders with branded left panel and form on right. Verify no sidebar or topbar appears. Test on mobile to confirm panels stack vertically.

**Acceptance Scenarios**:

1. **Given** an unauthenticated user, **When** they visit `/Access/Login`, **Then** they see a split-panel page: left panel with brand color and "Mentoory" text, right panel with the login form.
2. **Given** an unauthenticated user on mobile, **When** the viewport is small, **Then** the split panels stack vertically (brand panel on top or hidden, form below).
3. **Given** an unauthenticated user, **When** they visit any auth page (Register, ForgotPassword, ResetPassword, VerifyEmail, ChangePassword), **Then** they see the same split-panel layout with the corresponding form.

---

### User Story 4 - Icon Migration (Priority: P2)

All pages display Tabler Icons instead of Font Awesome icons. The sidebar menu items, action buttons, status indicators, and all other icon usages across the 22 affected views and `MenuConfiguration.cs` use Tabler Icons.

**Why this priority**: Icons are a visual consistency concern. The app works with mixed icons, but it looks unprofessional. Grouped as P2 because it's mechanical work that doesn't affect functionality.

**Independent Test**: Search the entire codebase for `fas fa-`, `far fa-`, `fab fa-` — zero matches expected. Visually verify icons render correctly on key pages (sidebar, Projects Index, Users Index, Dashboard).

**Acceptance Scenarios**:

1. **Given** the migrated codebase, **When** searching for Font Awesome class patterns (`fas fa-`, `far fa-`, `fab fa-`), **Then** zero matches are found in any `.cshtml` or `.cs` file.
2. **Given** an authenticated user, **When** they view the sidebar, **Then** all menu icons render as Tabler Icons (SVG-based).
3. **Given** an authenticated user, **When** they view any page with action buttons or status indicators, **Then** all icons are Tabler Icons, visually consistent with the template.

---

### User Story 5 - DataTables, Toasts, and Modals (Priority: P2)

All existing interactive components (DataTables with server-side processing, toast notifications, confirm modals) work correctly with Tabler's styling. Tables render inside Tabler-styled cards, toasts appear in the correct position, and modals use Tabler's modal classes.

**Why this priority**: These components are already functional. The migration only needs to ensure visual compatibility, not rebuild functionality.

**Independent Test**: Navigate to Projects Index — verify DataTable loads, paginates, sorts. Trigger a success action — verify toast appears. Trigger a delete action — verify confirm modal appears and functions.

**Acceptance Scenarios**:

1. **Given** an authenticated user on Projects Index, **When** the page loads, **Then** the DataTable renders with Tabler-compatible table styling inside a card, with working pagination and sorting.
2. **Given** an authenticated user performing an action that triggers a toast, **When** the action completes, **Then** the toast notification appears correctly positioned and styled.
3. **Given** an authenticated user triggering a confirm modal, **When** the modal opens, **Then** it renders with Tabler modal styling and both Cancel/Confirm buttons work.

---

### User Story 6 - Asset Cleanup (Priority: P3)

The standalone Bootstrap CSS and JS files are removed from `wwwroot/lib/bootstrap/`. Tabler's CSS and JS (`@tabler/core`) are installed as local files in `wwwroot/lib/tabler/`. The `mentoory.css` file contains only Mentoory-specific overrides using `--tblr-*` CSS variables where appropriate, with no rules that duplicate Tabler defaults.

**Why this priority**: Cleanup is important for maintainability but doesn't affect user-facing behavior. Can be verified independently.

**Independent Test**: Check `wwwroot/lib/` — `bootstrap/` directory should be removed, `tabler/` directory should exist with CSS and JS. Check `_Layout.cshtml` references only Tabler assets. Check `mentoory.css` for duplicate rules.

**Acceptance Scenarios**:

1. **Given** the migrated codebase, **When** checking `wwwroot/lib/`, **Then** the `bootstrap/` directory no longer exists and `tabler/` contains `tabler.min.css` and `tabler.min.js`.
2. **Given** the migrated `_Layout.cshtml`, **When** checking stylesheet and script references, **Then** only Tabler CSS/JS, jQuery, DataTables, and custom scripts are loaded — no standalone Bootstrap references.
3. **Given** `mentoory.css`, **When** reviewing its contents, **Then** it contains only Mentoory-specific overrides (login-container, toast positioning, custom variables) with no rules that duplicate Tabler defaults.

---

### Edge Cases

- Views using `@section Styles` or `@section Scripts` must continue rendering correctly with the new layout.
- `Error.cshtml` must render correctly in both authenticated (with sidebar) and unauthenticated (without sidebar) states.
- `MenuConfiguration.cs` icon identifiers must support the SVG rendering pattern chosen for Tabler Icons (inline SVG vs webfont class).
- If a Tabler Icon equivalent doesn't exist for a current Font Awesome icon, the closest semantic match is used and documented.
- Breadcrumbs must render inside Tabler's `.page-header` section with the same route-based auto-generation logic.
- The left branding panel on auth pages must be structured so a background image or illustration can replace the solid color later without layout changes.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST replace `_Layout.cshtml` with Tabler's `.page` > `.navbar-vertical` + `.page-wrapper` structure.
- **FR-002**: System MUST rewrite `_Navigation.cshtml` to Tabler's `.navbar.navbar-vertical.navbar-expand-sm` pattern with `data-bs-theme="dark"`.
- **FR-003**: System MUST preserve `IMenuService` integration for sidebar menu rendering.
- **FR-004**: System MUST replace `_TopBar.cshtml` with Tabler's topbar pattern, preserving context display (role badge, incubator, project) and context-switcher modal trigger.
- **FR-005**: System MUST adapt `_Breadcrumbs.cshtml` to Tabler's breadcrumb styling within `.page-header`.
- **FR-006**: System MUST use Tabler's `.footer.footer-transparent` pattern for `_Footer.cshtml`.
- **FR-007**: System MUST create `_AuthLayout.cshtml` as a split-panel layout (branded left panel + form right panel) for all auth pages.
- **FR-008**: System MUST replace all Font Awesome icon references with Tabler Icons across all `.cshtml` files and `MenuConfiguration.cs`.
- **FR-009**: System MUST install `@tabler/core` as local files in `wwwroot/lib/tabler/`.
- **FR-010**: System MUST remove standalone Bootstrap CSS/JS from `wwwroot/lib/bootstrap/` and all layout references.
- **FR-011**: System MUST keep jQuery and DataTables dependencies, ensuring DataTables renders correctly with Tabler's Bootstrap 5 base.
- **FR-012**: System MUST update view components (DataTable, Toast, ConfirmModal) for Tabler visual compatibility.
- **FR-013**: System MUST update `mentoory.css` to remove Tabler-duplicate rules and use `--tblr-*` CSS variables where appropriate.
- **FR-014**: System MUST preserve all existing `@RenderSectionAsync("Styles")` and `@RenderSectionAsync("Scripts")` hooks.
- **FR-015**: System MUST keep all custom JS files functional (`context-selector.js`, `context-switcher.js`, `datatable-helper.js`, `batch-upload.js`, `form-helper.js`, `registration.js`, `site.js`).

### Key Entities

No new domain entities. This feature modifies only Web layer view files, static assets, and the `MenuConfiguration.cs` icon references.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All pages render with Tabler's visual style — dark vertical sidebar, Tabler page header, Tabler footer.
- **SC-002**: Auth pages display as split-panel (branded left panel + form right panel) on desktop, stacked on mobile.
- **SC-003**: Sidebar navigation is fully functional: menu groups, section separators, active state, hamburger collapse on mobile.
- **SC-004**: Context display and context-switcher modal work identically to pre-migration behavior.
- **SC-005**: All DataTables load and paginate correctly with Tabler-compatible table styling.
- **SC-006**: Toast notifications and confirm modals function unchanged.
- **SC-007**: Zero Font Awesome references remain in the codebase (`fas fa-`, `far fa-`, `fab fa-` — 0 matches).
- **SC-008**: Zero standalone Bootstrap CSS/JS files loaded (only `@tabler/core`).
- **SC-009**: Build passes with zero warnings (`TreatWarningsAsErrors`).
- **SC-010**: All existing JS functionality works without regressions.

## Assumptions

- Tabler's Bootstrap 5 base ensures most existing utility classes (`btn-*`, `card`, `form-control`, `modal`, `badge`, `d-flex`, `mb-3`, etc.) remain valid without changes.
- `@tabler/core` CSS and JS can be installed as local static files without a Node.js build pipeline.
- DataTables' `dataTables.bootstrap5` styling plugin is compatible with Tabler's Bootstrap 5 base (same class names).
- The Tabler Icons library provides semantic equivalents for all Font Awesome icons currently used in the project.
- No backend or domain layer changes are required for this migration.

## Out of Scope

- Dark mode toggle or full dark theme support.
- Replacing jQuery or DataTables with alternative libraries.
- Backend changes (controllers, commands, queries, domain logic).
- Adding new pages or features.
- Changing the `IMenuService` interface contract beyond what's needed for the icon type change.
- Branding assets (illustrations, photos) for the auth panel — placeholder only.

## Open Questions

- **Tabler Icons rendering**: Inline SVG (copy-paste per icon) vs `<i>` tag with Tabler Icons webfont? Both are supported. Inline SVG is Tabler's recommended approach but changes how `MenuConfiguration.cs` stores icon data. To be decided during implementation.
