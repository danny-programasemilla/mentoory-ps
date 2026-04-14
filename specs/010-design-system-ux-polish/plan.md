# Implementation Plan: Design System & UX Polish

**Branch**: `010-design-system-ux-polish` | **Date**: 2026-04-13 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/010-design-system-ux-polish/spec.md`

## Summary

Transform Mentoory's UI from a functional Tabler adoption to a polished, brand-aligned product by establishing a design system with the Mentoory logo-derived color palette (coral primary, golden/magenta accents), redesigning the navigation shell, establishing rich component patterns (tables, forms, cards), and applying them consistently across all ~45 views. The approach is phased: CSS foundation first, then shell, components, view application, and auth pages.

## Technical Context

**Language/Version**: C# / .NET 10.0, Razor Views, CSS, JavaScript  
**Primary Dependencies**: Tabler v1.4.0 (Bootstrap 5), jQuery, DataTables 2.3.4, MediatR 14.1  
**Storage**: SQL Server (read-only count queries for dashboard metrics — no schema changes)  
**Testing**: xUnit, Playwright E2E  
**Target Platform**: ASP.NET Core MVC web application  
**Project Type**: Web application (modular monolith)  
**Performance Goals**: Dashboard stat cards load within 2 seconds  
**Constraints**: Zero warnings (TreatWarningsAsErrors), Spanish UI, no new JS dependencies  
**Scale/Scope**: ~45 views across 5 areas, 6 shared partials, 3 view components, 7 JS files, 2 CSS files

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| # | Gate | Status | Notes |
|---|------|--------|-------|
| I | Clean Architecture Layer Boundaries | PASS | CSS/views are Web layer. Dashboard query in Application layer. No cross-layer violations. |
| II | CQRS Pattern Requirements | PASS | New `GetDashboardMetricsQuery` follows `IBaseRequest<TResult>` pattern with `BaseCommandHandler`. |
| III | Domain-Driven Design Constraints | PASS | No domain entity changes. DashboardMetricsDto is an Application-layer DTO. |
| IV | Integration Events | N/A | No cross-domain events. |
| V | Zero-Warnings Policy | PASS | Explicitly in NFRs (SC-011). All code must compile clean. |
| VI | DateTime Handling | PASS | Relative dates rendered client-side in JS. No server-side DateTime.UtcNow usage. |
| VII | Naming Conventions | PASS | `GetDashboardMetricsQuery`, `GetDashboardMetricsQueryHandler`, `DashboardMetricsDto` follow naming patterns. |
| VIII | File Organization | PASS | JS in `wwwroot/js/`, CSS in `wwwroot/css/`, SVG in `wwwroot/img/`. |
| IX | Spanish-First UI | PASS | All UI text remains Spanish. New text (tagline, empty states, relative dates) in Spanish. |
| X | Role Hierarchy & Session Context | PASS | Dashboard controller already checks `HasValidIncubatorContext()`. Menu badge counts respect role-based visibility via existing IMenuService filtering. |
| XI | SSDT/DACPAC Database Strategy | PASS | No database changes. Count queries use existing EF Core repositories. |

**All gates pass. No violations.**

## Project Structure

### Documentation (this feature)

```text
specs/010-design-system-ux-polish/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0 research findings
├── data-model.md        # Entity and query changes
├── quickstart.md        # Build and verification guide
├── review_brief.md      # Reviewer guide
├── REVIEW-SPEC.md       # Spec review assessment
└── checklists/
    └── requirements.md  # Quality checklist
```

### Source Code (repository root)

```text
Mentoory.Web/
├── wwwroot/
│   ├── css/
│   │   └── mentoory.css            # Phase 1: brand palette + component overrides
│   ├── img/
│   │   ├── logo-white.svg          # Phase 2: white logo for sidebar
│   │   └── logo-gradient.svg       # Phase 5: gradient logo for auth
│   └── js/
│       └── datatable-helper.js     # Phase 3: render helpers, empty states, skeleton
├── Infrastructure/Menu/
│   ├── MenuItem.cs                 # Phase 2: add BadgeCount property
│   └── MenuService.cs             # Phase 2: inject badge counts
├── Views/Shared/
│   ├── _Layout.cshtml              # Phase 1: verify body-bg applied
│   ├── _Navigation.cshtml          # Phase 2: logo, section styling, badges, hover/active
│   ├── _TopBar.cshtml              # Phase 2: avatar dropdown, bell, status indicators
│   ├── _Breadcrumbs.cshtml         # Phase 2: merge into page-header pattern
│   ├── _Footer.cshtml              # Phase 2: left/right layout
│   ├── _AuthLayout.cshtml          # Phase 5: gradient panel, logo, illustration
│   └── Components/
│       ├── DataTable/Default.cshtml # Phase 3: table-vcenter, card wrapper
│       └── ConfirmModal/Default.cshtml # Phase 3: modal-status strip
├── Areas/
│   ├── Access/Views/               # Phase 5: auth form styling (~8 views)
│   ├── Administration/
│   │   ├── Controllers/
│   │   │   └── DashboardController.cs  # Phase 4: dispatch metrics query
│   │   └── Views/                  # Phase 4: ~7 content views
│   ├── Coordination/Views/         # Phase 4: ~4 content views
│   ├── Participant/Views/          # Phase 4: ~3 content views
│   └── Platform/Views/             # Phase 4: ~7 content views
└── ViewComponents/
    └── DataTableViewComponent.cs   # Phase 3: update parameters

Mentoory.Application/
└── Administration/Queries/
    └── GetDashboardMetrics/
        ├── GetDashboardMetricsQuery.cs        # Phase 4: query record
        ├── GetDashboardMetricsQueryHandler.cs  # Phase 4: count handler
        └── DashboardMetricsDto.cs             # Phase 4: result DTO
```

**Structure Decision**: No new projects or directories beyond `wwwroot/img/` and `Application/Administration/Queries/GetDashboardMetrics/`. All changes fit within existing Clean Architecture layout.

## Implementation Phases

### Phase 1: Foundation (Brand Theme) — FR-001, FR-002

**Goal**: Replace the default Bootstrap blue with Mentoory's brand palette globally.

**Files**:
- `Mentoory.Web/wwwroot/css/mentoory.css`

**Steps**:
1. Replace existing `:root` block in `mentoory.css` with the full Mentoory design token palette (all `--mentory-*` variables + `--tblr-*` overrides as specified in FR-001)
2. Remove the old `--mentoory-primary: #0d6efd` reference
3. Add component-specific overrides for sidebar (hover, active states), avatar backgrounds, and any elements where Tabler doesn't automatically pick up `--tblr-primary`
4. Verify all button variants, badge colors, link colors, form focus rings render in coral

**Verification**: Load any page, confirm no #0d6efd blue visible. Check buttons, badges, links, form focus states.

---

### Phase 2: Shell Redesign — FR-003 through FR-011

**Goal**: Transform sidebar, topbar, breadcrumbs, and footer into a polished navigation shell.

**Files**:
- `Mentoory.Web/wwwroot/img/logo-white.svg` (new)
- `Mentoory.Web/wwwroot/img/logo-gradient.svg` (new)
- `Mentoory.Web/Infrastructure/Menu/MenuItem.cs`
- `Mentoory.Web/Infrastructure/Menu/MenuService.cs`
- `Mentoory.Web/Views/Shared/_Navigation.cshtml`
- `Mentoory.Web/Views/Shared/_TopBar.cshtml`
- `Mentoory.Web/Views/Shared/_Breadcrumbs.cshtml`
- `Mentoory.Web/Views/Shared/_Footer.cshtml`
- `Mentoory.Web/Views/Shared/_Layout.cshtml` (minor: merge breadcrumbs into page-header)
- `Mentoory.Web/wwwroot/css/mentoory.css` (sidebar/topbar styles)

**Steps**:

*Sidebar (FR-003 to FR-007)*:
1. Create `logo-white.svg` — white monochrome Mentoory lettermark, ~120px wide
2. Replace text brand in `_Navigation.cshtml` with `<img src="~/img/logo-white.svg" class="navbar-brand-image" alt="Mentoory" height="32">`
3. Style section headers with CSS: `text-transform: uppercase; letter-spacing: 0.04em; font-size: 0.7rem; color: var(--tblr-muted)`
4. Add CSS for active state: `.nav-link.active { border-left: 3px solid var(--mentory-primary); background: var(--mentory-sidebar-active); }`
5. Add CSS for hover state: `.navbar-vertical .nav-link:hover { background: var(--mentory-sidebar-hover); }`
6. Extend `MenuItem` with optional `int? BadgeCount` constructor parameter
7. Update `MenuService` to populate badge counts for relevant items (inject `IMediator`, dispatch lightweight count queries)
8. Render badge counts in `_Navigation.cshtml`: `@if (child.BadgeCount.HasValue) { <span class="badge bg-primary ms-auto">@child.BadgeCount</span> }`

*TopBar (FR-008 to FR-010)*:
9. Restructure `_TopBar.cshtml` into Tabler's `page-header` pattern with three zones: left (page-pretitle + page-title), center (context status), right (avatar + bell + dropdown)
10. Merge breadcrumb content into the page-pretitle area and remove standalone `_Breadcrumbs.cshtml` render from `_Layout.cshtml`
11. Add initials-based avatar: `<span class="avatar avatar-sm" style="background-color: var(--mentory-primary); color: white;">@initials</span>`
12. Add avatar dropdown with: profile placeholder, change context, logout
13. Add notification bell: `<a class="nav-link" href="#"><i class="ti ti-bell"></i></a>` (non-functional placeholder)
14. Style context display with Tabler status indicators: `<span class="status status-primary"><span class="status-dot"></span> @activeRole</span>`

*Footer (FR-011)*:
15. Update `_Footer.cshtml` to two-column layout: left ("Mentoory" + year), right (version text)

**Verification**: Login, verify logo in sidebar, section headers styled, active/hover states work, topbar shows avatar with dropdown, bell placeholder, context as status indicators, footer two-column.

---

### Phase 3: Component Patterns — FR-012 through FR-022, FR-029

**Goal**: Establish reusable table, form, card, and feedback patterns.

**Files**:
- `Mentoory.Web/wwwroot/js/datatable-helper.js`
- `Mentoory.Web/Views/Shared/Components/DataTable/Default.cshtml`
- `Mentoory.Web/ViewComponents/DataTableViewComponent.cs`
- `Mentoory.Web/Views/Shared/Components/ConfirmModal/Default.cshtml`
- `Mentoory.Web/wwwroot/css/mentoory.css` (component styles)

**Steps**:

*Tables (FR-012 to FR-019)*:
1. Update `DataTable/Default.cshtml`: change `table-striped table-hover` to `table table-vcenter`, wrap in card with card-header (title + count badge slot)
2. Add `formatRelativeDate(isoString)` function to `datatable-helper.js` — returns Spanish relative time with `<span title="exact date">` for tooltip
3. Add `renderAvatar(name, email)` function — generates initials-based avatar HTML with compound cell (avatar + name + email)
4. Add `renderStatus(status, colorMap)` function — generates Tabler status dot HTML: `<span class="status status-{color}"><span class="status-dot status-dot-animated"></span> {text}</span>`
5. Add `renderActions(actions)` function — generates `btn-actions` HTML with icon-only buttons
6. Update `initDataTable` to accept `emptyState` config (icon, title, message, actionUrl, actionText) and inject Tabler `.empty` component HTML via `language.emptyTable`
7. Add skeleton placeholder HTML for loading state in DataTable's `processing` override

*Forms (FR-020 to FR-022)*:
8. No shared partial needed — form patterns are applied per-view via Razor markup. Document the pattern in a CSS comment block in `mentoory.css` for reference:
   - Section dividers: `<div class="hr-text">Section Title</div>`
   - Card structure: `card > card-header + card-body + card-footer`
   - Button bar: `<div class="card-footer"><div class="btn-list justify-content-end">...</div></div>`

*Feedback (FR-029)*:
9. Update `ConfirmModal/Default.cshtml` to include `modal-status` colored strip at top

**Verification**: Create a test table view that uses all new render functions. Verify empty state, skeleton loading, status dots, avatars, relative dates, action buttons all render correctly.

---

### Phase 4: View Application — FR-023 to FR-025, FR-030

**Goal**: Apply component patterns to all existing views across 5 areas.

**Files**: All area views + new dashboard query.

**Steps**:

*Dashboard Metrics (FR-023 to FR-025)*:
1. Create `GetDashboardMetricsQuery.cs`, `GetDashboardMetricsQueryHandler.cs`, `DashboardMetricsDto.cs` in `Mentoory.Application/Administration/Queries/GetDashboardMetrics/`
2. Handler queries `IUserRepository`, `IProjectRepository`, `IDiagnosticFormRepository` for counts filtered by incubator ID
3. Update `DashboardController.Index` to dispatch query and pass `DashboardMetricsDto` to view via ViewModel
4. Rewrite `Dashboard/Index.cshtml` with stat cards (card-sm, row-deck row-cards), quick action buttons, card-status-start strips

*Administration Views (~7 content views)*:
5. `Users/Index.cshtml` — rich table with avatar compound cell, status dots, relative dates, action buttons, empty state
6. `Users/Enroll.cshtml` — card structure, hr-text dividers, btn-list footer, form-hint descriptions
7. `Users/RegisterInternal.cshtml` — same form pattern as Enroll
8. `Projects/Index.cshtml` — rich table with status indicators, action buttons, empty state
9. `Projects/Create.cshtml` — card structure, hr-text dividers
10. `Projects/Details.cshtml` — card layout with datagrid for project info
11. `BatchUpload/Index.cshtml` — styled upload area, progress indicators
12. `BatchUpload/Results.cshtml` — result summary cards with status strips

*Coordination Views (~4 content views)*:
13. `Diagnostics/Index.cshtml` — rich table with progress indicators, user avatar
14. `Diagnostics/Details.cshtml` — card-based detail layout
15. `Diagnostics/Clone.cshtml` — styled form
16. `AnswerCorrection/Index.cshtml` — styled form/table

*Participant Views (~3 content views)*:
17. `Diagnostic/List.cshtml` — rich table or card list with progress bars
18. `Diagnostic/Index.cshtml` — step indicator (Tabler `steps` component)
19. `Diagnostic/Confirmation.cshtml` — success card with status strip

*Platform Views (~7 content views)*:
20. `Incubators/Index.cshtml` — rich table with status, member count
21. `Incubators/Create.cshtml` — card form with hr-text dividers
22. `Incubators/Edit.cshtml` — card form with hr-text dividers
23. `Incubators/Details.cshtml` — datagrid for incubator info
24. `Configuration/Index.cshtml` — datagrid for settings display
25. `Templates/Knowledge.cshtml` — styled view
26. `Templates/Diagnostics.cshtml` — styled view
27. `Sponsor/Index.cshtml` — dashboard-style stat cards
28. `Users/Index.cshtml` (Platform) — rich table

*Shared/Root Views*:
29. `Home/Index.cshtml` — styled landing/dashboard
30. `Home/Privacy.cshtml` — card layout
31. `Context/Select.cshtml` — styled context selection page
32. `AvailableProjects/Index.cshtml` — card list or rich table
33. `Shared/Error.cshtml` — styled error page with Tabler empty component

**Verification**: Navigate through every area. Verify consistent table, form, and card patterns. Run E2E tests. Build with zero warnings.

---

### Phase 5: Auth Pages — FR-026 to FR-028

**Goal**: Transform auth pages into a branded experience with gradient panel, logo, and illustration.

**Files**:
- `Mentoory.Web/Views/Shared/_AuthLayout.cshtml`
- `Mentoory.Web/wwwroot/css/mentoory.css` (auth-specific styles)
- All `Areas/Access/Views/` content views (~8 views)

**Steps**:
1. Rewrite `_AuthLayout.cshtml` left panel: brand gradient background, centered white logo SVG, tagline "Impulsa tu emprendimiento", CSS decorative geometric shapes (floating circles, connecting lines at various opacities)
2. Style right panel: max-width 420px form area, clean white background, consistent spacing
3. Add auth-specific CSS to `mentoory.css`: gradient background, geometric shapes positioning, responsive adjustments
4. Update `Login/Index.cshtml` — clean form with brand focus color, forgot password link
5. Update `Register/Index.cshtml` — styled multi-field form
6. Update `Register/Success.cshtml` — success card with icon
7. Update `ForgotPassword/Index.cshtml` — single email field, clear instructions
8. Update `ForgotPassword/Confirmation.cshtml` — confirmation card
9. Update `ResetPassword/Index.cshtml` — password reset form
10. Update `ResetPassword/Success.cshtml` — success card
11. Update `VerifyEmail/Index.cshtml` — verification status card
12. Update `ChangePassword/Index.cshtml` — password change form

**Verification**: Open login page in incognito. Verify gradient, logo, tagline, illustration on left. Verify clean form on right. Test all auth flows.

## Complexity Tracking

No constitution violations to justify. All gates pass cleanly.

## View Inventory

Complete list of content views to update (excluding _ViewImports, _ViewStart):

| # | Area | View | Pattern | Phase |
|---|------|------|---------|-------|
| 1 | Shared | _Layout.cshtml | Layout shell | 2 |
| 2 | Shared | _Navigation.cshtml | Sidebar | 2 |
| 3 | Shared | _TopBar.cshtml | TopBar | 2 |
| 4 | Shared | _Breadcrumbs.cshtml | Merge into TopBar | 2 |
| 5 | Shared | _Footer.cshtml | Footer | 2 |
| 6 | Shared | _AuthLayout.cshtml | Auth layout | 5 |
| 7 | Shared | Components/DataTable/Default.cshtml | Table component | 3 |
| 8 | Shared | Components/ConfirmModal/Default.cshtml | Modal component | 3 |
| 9 | Shared | Components/Toast/Default.cshtml | Toast (verify only) | 3 |
| 10 | Shared | _ContextSelector.cshtml | Context modal (verify only) | 2 |
| 11 | Root | Home/Index.cshtml | Dashboard/landing | 4 |
| 12 | Root | Home/Privacy.cshtml | Content card | 4 |
| 13 | Root | Context/Select.cshtml | Context form | 4 |
| 14 | Root | AvailableProjects/Index.cshtml | Table/card list | 4 |
| 15 | Root | Shared/Error.cshtml | Error page | 4 |
| 16 | Admin | Dashboard/Index.cshtml | Dashboard stats | 4 |
| 17 | Admin | Users/Index.cshtml | Rich table | 4 |
| 18 | Admin | Users/Enroll.cshtml | Form | 4 |
| 19 | Admin | Users/RegisterInternal.cshtml | Form | 4 |
| 20 | Admin | Projects/Index.cshtml | Rich table | 4 |
| 21 | Admin | Projects/Create.cshtml | Form | 4 |
| 22 | Admin | Projects/Details.cshtml | Detail card | 4 |
| 23 | Admin | BatchUpload/Index.cshtml | Upload form | 4 |
| 24 | Admin | BatchUpload/Results.cshtml | Result cards | 4 |
| 25 | Coord | Diagnostics/Index.cshtml | Rich table | 4 |
| 26 | Coord | Diagnostics/Details.cshtml | Detail card | 4 |
| 27 | Coord | Diagnostics/Clone.cshtml | Form | 4 |
| 28 | Coord | AnswerCorrection/Index.cshtml | Table/form | 4 |
| 29 | Part | Diagnostic/List.cshtml | Table/cards | 4 |
| 30 | Part | Diagnostic/Index.cshtml | Step wizard | 4 |
| 31 | Part | Diagnostic/Confirmation.cshtml | Success card | 4 |
| 32 | Plat | Incubators/Index.cshtml | Rich table | 4 |
| 33 | Plat | Incubators/Create.cshtml | Form | 4 |
| 34 | Plat | Incubators/Edit.cshtml | Form | 4 |
| 35 | Plat | Incubators/Details.cshtml | Detail card | 4 |
| 36 | Plat | Configuration/Index.cshtml | Datagrid | 4 |
| 37 | Plat | Templates/Knowledge.cshtml | Template view | 4 |
| 38 | Plat | Templates/Diagnostics.cshtml | Template view | 4 |
| 39 | Plat | Sponsor/Index.cshtml | Dashboard cards | 4 |
| 40 | Plat | Users/Index.cshtml | Rich table | 4 |
| 41 | Access | Login/Index.cshtml | Auth form | 5 |
| 42 | Access | Register/Index.cshtml | Auth form | 5 |
| 43 | Access | Register/Success.cshtml | Success card | 5 |
| 44 | Access | ForgotPassword/Index.cshtml | Auth form | 5 |
| 45 | Access | ForgotPassword/Confirmation.cshtml | Confirmation card | 5 |
| 46 | Access | ResetPassword/Index.cshtml | Auth form | 5 |
| 47 | Access | ResetPassword/Success.cshtml | Success card | 5 |
| 48 | Access | VerifyEmail/Index.cshtml | Status card | 5 |
| 49 | Access | ChangePassword/Index.cshtml | Auth form | 5 |
