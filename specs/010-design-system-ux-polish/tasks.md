# Tasks: Mentoory Design System & UX Polish

**Input**: Design documents from `/specs/010-design-system-ux-polish/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md

**Tests**: Not explicitly requested. Test tasks omitted. Verification is visual + E2E + build.

**Organization**: Tasks grouped by user story for independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2)
- Includes exact file paths in descriptions

## Path Conventions

- **Web Layer**: `Mentoory.Web/`
- **Application Layer**: `Mentoory.Application/`
- **Static Assets**: `Mentoory.Web/wwwroot/`
- **Views**: `Mentoory.Web/Views/` and `Mentoory.Web/Areas/*/Views/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create assets and directories needed by multiple user stories

- [ ] T001 Create `Mentoory.Web/wwwroot/img/` directory and add `logo-white.svg` (white monochrome Mentoory lettermark, ~120px wide, based on logo PDF page 2)
- [ ] T002 [P] Create `Mentoory.Web/wwwroot/img/logo-gradient.svg` (full gradient version of Mentoory lettermark for auth pages, based on logo PDF page 1)

**Checkpoint**: Logo SVG assets exist and render correctly at sidebar and auth panel sizes

---

## Phase 2: Foundational — User Story 1: Brand-Consistent Visual Identity (Priority: P1) 🎯 MVP

**Goal**: Replace all Bootstrap blue with Mentoory's brand palette. This is the FOUNDATION that all other stories build upon.

**Independent Test**: Load any page, confirm all primary elements (buttons, links, badges, focus rings) use #E07850 coral. No #0d6efd blue visible anywhere. Page background is #F9FAFB.

**⚠️ CRITICAL**: No other user story work can begin until this phase is complete.

- [ ] T003 [US1] Replace the existing `:root` block in `Mentoory.Web/wwwroot/css/mentoory.css` with the complete Mentoory design token palette: all `--mentory-*` custom properties (primary scale 50-900, accent colors, semantic colors, surface, sidebar, gradient) plus `--tblr-*` overrides (primary, success, danger, info, warning, body-bg, border-color, font-sans-serif) as defined in spec FR-001
- [ ] T004 [US1] Remove the old `--mentoory-primary: #0d6efd` reference and any remaining Bootstrap blue hardcoded values in `Mentoory.Web/wwwroot/css/mentoory.css`
- [ ] T005 [US1] Add component-specific CSS overrides in `Mentoory.Web/wwwroot/css/mentoory.css` for elements where Tabler doesn't automatically pick up `--tblr-primary`: sidebar hover/active backgrounds, avatar backgrounds, link hover states, form focus ring color
- [ ] T006 [US1] Verify the build compiles with zero warnings: run `dotnet build` from repo root

**Checkpoint**: Foundation ready — every page renders in the Mentoory coral palette. All user story work can now begin.

---

## Phase 3: User Story 2 — Professional Navigation Shell (Priority: P1)

**Goal**: Transform sidebar (logo, section styling, badges, hover/active), topbar (avatar dropdown, bell, context status), breadcrumbs (merge into page-header), and footer (two-column).

**Independent Test**: Login and verify: sidebar shows logo image, section headers are visually distinct with uppercase styling, active item has coral left border, topbar shows initials avatar with working dropdown, bell placeholder visible, context uses status indicators, footer shows "Mentoory + year" left and version right.

### Sidebar (FR-003 to FR-007)

- [ ] T007 [US2] Replace text brand in `Mentoory.Web/Views/Shared/_Navigation.cshtml` with `<img>` tag referencing `~/img/logo-white.svg` using `navbar-brand-image` class with `height="32"` and alt text
- [ ] T008 [US2] Style sidebar section headers in `Mentoory.Web/wwwroot/css/mentoory.css`: add rules for `.nav-link-header` with `text-transform: uppercase`, `letter-spacing: 0.04em`, `font-size: 0.7rem`, muted color
- [ ] T009 [US2] Add sidebar active state CSS in `Mentoory.Web/wwwroot/css/mentoory.css`: `.navbar-vertical .nav-link.active` with `border-left: 3px solid var(--mentory-primary)` and `background: var(--mentory-sidebar-active)`
- [ ] T010 [US2] Add sidebar hover state CSS in `Mentoory.Web/wwwroot/css/mentoory.css`: `.navbar-vertical .nav-link:hover` with `background: var(--mentory-sidebar-hover)`
- [ ] T011 [US2] Extend `MenuItem` class in `Mentoory.Web/Infrastructure/Menu/MenuItem.cs` with an optional `int? BadgeCount` property (add constructor overload or property with default null, maintain backward compatibility)
- [ ] T012 [US2] Update `MenuService` in `Mentoory.Web/Infrastructure/Menu/MenuService.cs` to inject `IMediator` and populate `BadgeCount` on relevant menu items by dispatching lightweight count queries filtered by tenant context
- [ ] T013 [US2] Render badge counts in `Mentoory.Web/Views/Shared/_Navigation.cshtml`: for each menu item/child with `BadgeCount.HasValue`, render `<span class="badge bg-primary ms-auto">@BadgeCount</span>`

### TopBar (FR-008 to FR-010)

- [ ] T014 [US2] Restructure `Mentoory.Web/Views/Shared/_TopBar.cshtml` into Tabler's standard page-header layout with three zones: left (breadcrumb as page-pretitle + page-title from ViewData["Title"]), center (context with Tabler status indicators), right (avatar + bell + dropdown)
- [ ] T015 [US2] Add initials-based avatar to topbar right zone in `Mentoory.Web/Views/Shared/_TopBar.cshtml`: `<span class="avatar avatar-sm">` with user initials extracted from `User.Identity.Name`, styled with `--mentory-primary` background
- [ ] T016 [US2] Add avatar dropdown menu in `Mentoory.Web/Views/Shared/_TopBar.cshtml` with items: "Perfil" (placeholder, disabled), "Cambiar contexto" (triggers modal), "Cerrar sesion" (existing logout form)
- [ ] T017 [US2] Add notification bell placeholder in topbar right zone in `Mentoory.Web/Views/Shared/_TopBar.cshtml`: `<a class="nav-link"><i class="ti ti-bell"></i></a>` (non-functional)
- [ ] T018 [US2] Restyle context display in `Mentoory.Web/Views/Shared/_TopBar.cshtml` from plain badge + text to Tabler status indicators: `<span class="status status-primary"><span class="status-dot"></span> @activeRole</span>` for role, similar for incubator/project

### Breadcrumbs & Footer (FR-011)

- [ ] T019 [US2] Merge breadcrumb logic from `Mentoory.Web/Views/Shared/_Breadcrumbs.cshtml` into the page-pretitle area of `_TopBar.cshtml` and remove the standalone `@await Html.PartialAsync("_Breadcrumbs")` call from `Mentoory.Web/Views/Shared/_Layout.cshtml`
- [ ] T020 [US2] Update `Mentoory.Web/Views/Shared/_Footer.cshtml` to two-column layout: left column with "Mentoory" + `@DateTime.UtcNow.Year`, right column with version text (e.g., "v1.0")
- [ ] T021 [US2] Verify build compiles clean and run the app to visually test all shell changes

**Checkpoint**: Navigation shell fully redesigned — sidebar has logo, styled sections, badges; topbar has avatar dropdown, bell, status indicators; footer is two-column.

---

## Phase 4: User Story 3 — Rich Data Tables (Priority: P2)

**Goal**: Establish the rich table pattern: vcenter, card wrapper with header, compound avatar cells, status dots, relative dates, action buttons, empty states, skeleton loading.

**Independent Test**: Navigate to Users Index — table uses vcenter, user rows have initials avatars, status shows animated dots, dates are relative in Spanish, actions are icon buttons, empty state appears when no data.

- [ ] T022 [US3] Update `Mentoory.Web/Views/Shared/Components/DataTable/Default.cshtml`: change `table-striped table-hover` to `table table-vcenter`, wrap in card with `card-header` containing title slot and count badge slot
- [ ] T023 [US3] Update `Mentoory.Web/ViewComponents/DataTableViewComponent.cs` to accept new parameters: `string title`, `string? emptyIcon`, `string? emptyTitle`, `string? emptyMessage`, `string? emptyActionUrl`, `string? emptyActionText`
- [ ] T024 [US3] Add `formatRelativeDate(isoString)` function to `Mentoory.Web/wwwroot/js/datatable-helper.js` returning Spanish relative time strings ("hace 3 dias", "hace 2 horas", etc.) wrapped in `<span title="exact date">` for tooltip
- [ ] T025 [P] [US3] Add `renderAvatar(firstName, lastName, email)` function to `Mentoory.Web/wwwroot/js/datatable-helper.js` generating initials-based avatar HTML with compound cell: avatar circle + name + email as secondary text
- [ ] T026 [P] [US3] Add `renderStatus(statusText, statusColor, animated)` function to `Mentoory.Web/wwwroot/js/datatable-helper.js` generating Tabler status dot HTML: `<span class="status status-{color}"><span class="status-dot status-dot-animated"></span> {text}</span>`
- [ ] T027 [P] [US3] Add `renderActions(actions)` function to `Mentoory.Web/wwwroot/js/datatable-helper.js` generating `<div class="btn-actions">` with icon-only `btn-action` buttons from an array of `{url, icon, title}` objects
- [ ] T028 [US3] Update `initDataTable` in `Mentoory.Web/wwwroot/js/datatable-helper.js` to accept `emptyState` config object and inject Tabler `.empty` component HTML via `language.emptyTable` setting
- [ ] T029 [US3] Add skeleton placeholder HTML for loading state in `Mentoory.Web/wwwroot/js/datatable-helper.js`: override DataTable's `processing` display with `placeholder-glow` skeleton rows
- [ ] T030 [US3] Add table-specific CSS to `Mentoory.Web/wwwroot/css/mentoory.css`: avatar compound cell layout, text truncation for long names, action column width, skeleton placeholder styling

**Checkpoint**: Table pattern established — all render helper functions work. Ready to apply to individual table views.

---

## Phase 5: User Story 4 — Polished Forms (Priority: P2)

**Goal**: Establish the form pattern: card structure (header/body/footer), hr-text section dividers, btn-list action bar, form-hint descriptions.

**Independent Test**: Navigate to Enroll User — form is in a card with header and footer, sections use hr-text dividers, action buttons are in btn-list with cancel as ghost-secondary.

- [ ] T031 [US4] Restyle `Mentoory.Web/Areas/Administration/Views/Users/Enroll.cshtml` as the reference form implementation: wrap in card with `card-header` (title "Inscribir Usuario"), `card-body` for fields, `card-footer` for action buttons; replace `<h5>` section headers with `<div class="hr-text">` dividers; wrap action buttons in `<div class="btn-list justify-content-end">` with cancel as `btn-ghost-secondary`; add `form-hint` text below inputs where helpful
- [ ] T032 [US4] Add any form-specific CSS to `Mentoory.Web/wwwroot/css/mentoory.css`: form-hint spacing adjustments, validation error styling for Tabler's `invalid-feedback` pattern if needed

**Checkpoint**: Form pattern established on reference view (Enroll). Ready to apply to other form views.

---

## Phase 6: User Story 5 — Informative Dashboard (Priority: P2)

**Goal**: Transform admin dashboard from link cards to stat cards with real metrics, quick actions, and card-status-start colored strips.

**Independent Test**: Login as admin — stat cards show real user/project/diagnostic counts from database, cards use card-sm pattern with row-deck row-cards, navigation cards have colored left strips.

- [ ] T033 [P] [US5] Create `Mentoory.Application/Administration/Queries/GetDashboardMetrics/DashboardMetricsDto.cs` with properties: `int UserCount`, `int ProjectCount`, `int DiagnosticFormCount`
- [ ] T034 [P] [US5] Create `Mentoory.Application/Administration/Queries/GetDashboardMetrics/GetDashboardMetricsQuery.cs` as `sealed record GetDashboardMetricsQuery(long IncubatorId) : IBaseRequest<DashboardMetricsDto>`
- [ ] T035 [US5] Create `Mentoory.Application/Administration/Queries/GetDashboardMetrics/GetDashboardMetricsQueryHandler.cs` inheriting from `BaseCommandHandler<GetDashboardMetricsQuery, DashboardMetricsDto>`, injecting repositories for user, project, and diagnostic form counts filtered by incubator ID, using `AsNoTracking()` on all read paths
- [ ] T036 [US5] Update `Mentoory.Web/Areas/Administration/Controllers/DashboardController.cs` to dispatch `GetDashboardMetricsQuery` with active incubator ID and pass `DashboardMetricsDto` to the view
- [ ] T037 [US5] Rewrite `Mentoory.Web/Areas/Administration/Views/Dashboard/Index.cshtml` with: top row of `card-sm` stat cards (user count, project count, diagnostic count) using avatar-icon pattern in `row-deck row-cards` grid; navigation cards below with `card-status-start` colored strips by category; welcoming empty state for new incubators with no data
- [ ] T038 [US5] Verify build compiles clean and dashboard loads with real counts

**Checkpoint**: Dashboard shows real metrics — functional, data-driven admin hub.

---

## Phase 7: User Story 6 — Branded Auth Experience (Priority: P3)

**Goal**: Transform auth pages with brand gradient panel, white logo, tagline, CSS illustration, and polished forms.

**Independent Test**: Open login in incognito — left panel shows yellow-to-magenta gradient with white logo, tagline, and decorative shapes; right panel has clean form with coral-colored elements.

- [ ] T039 [US6] Rewrite `Mentoory.Web/Views/Shared/_AuthLayout.cshtml` left panel: replace flat color with `background: var(--mentory-gradient)`, center white logo SVG, add tagline "Impulsa tu emprendimiento" below logo, add container for CSS decorative elements
- [ ] T040 [US6] Style auth right panel in `Mentoory.Web/Views/Shared/_AuthLayout.cshtml`: `max-width: 420px` form area, clean white background, consistent vertical spacing
- [ ] T041 [US6] Add auth-specific CSS to `Mentoory.Web/wwwroot/css/mentoory.css`: decorative geometric shapes using `::before`/`::after` pseudo-elements and positioned `<div>` elements (floating circles, connecting lines at various opacities), responsive adjustments for the left panel
- [ ] T042 [P] [US6] Update `Mentoory.Web/Areas/Access/Views/Login/Index.cshtml` with brand-styled form: clean inputs, coral primary button, "Olvide mi contrasena" link
- [ ] T043 [P] [US6] Update `Mentoory.Web/Areas/Access/Views/Register/Index.cshtml` with brand-styled multi-field form
- [ ] T044 [P] [US6] Update `Mentoory.Web/Areas/Access/Views/Register/Success.cshtml` with success card and icon
- [ ] T045 [P] [US6] Update `Mentoory.Web/Areas/Access/Views/ForgotPassword/Index.cshtml` with single email field and clear instructions
- [ ] T046 [P] [US6] Update `Mentoory.Web/Areas/Access/Views/ForgotPassword/Confirmation.cshtml` with confirmation card
- [ ] T047 [P] [US6] Update `Mentoory.Web/Areas/Access/Views/ResetPassword/Index.cshtml` with password reset form
- [ ] T048 [P] [US6] Update `Mentoory.Web/Areas/Access/Views/ResetPassword/Success.cshtml` with success card
- [ ] T049 [P] [US6] Update `Mentoory.Web/Areas/Access/Views/VerifyEmail/Index.cshtml` with verification status card
- [ ] T050 [P] [US6] Update `Mentoory.Web/Areas/Access/Views/ChangePassword/Index.cshtml` with password change form

**Checkpoint**: All auth pages use the branded gradient panel with logo and illustration.

---

## Phase 8: User Story 7 — Consistent View Application (Priority: P3)

**Goal**: Apply the established component patterns (rich tables, polished forms, stat cards, empty states) to ALL remaining views across 5 areas so the entire application is visually consistent.

**Independent Test**: Navigate through every area — all table views use rich table pattern, all forms use hr-text/card pattern, no Bootstrap blue visible anywhere, empty states on all list views.

### Administration Views

- [ ] T051 [US7] Apply rich table pattern to `Mentoory.Web/Areas/Administration/Views/Users/Index.cshtml`: use DataTable ViewComponent with avatar compound cell, status dots, relative dates, action buttons, empty state "Inscribir Usuario"
- [ ] T052 [P] [US7] Apply form pattern to `Mentoory.Web/Areas/Administration/Views/Users/RegisterInternal.cshtml`: card structure, hr-text dividers, btn-list footer
- [ ] T053 [P] [US7] Apply rich table pattern to `Mentoory.Web/Areas/Administration/Views/Projects/Index.cshtml`: status indicators, action buttons, empty state "Crear Proyecto"
- [ ] T054 [P] [US7] Apply form pattern to `Mentoory.Web/Areas/Administration/Views/Projects/Create.cshtml`: card structure, hr-text dividers
- [ ] T055 [P] [US7] Apply detail card pattern to `Mentoory.Web/Areas/Administration/Views/Projects/Details.cshtml`: datagrid for project info display
- [ ] T056 [P] [US7] Restyle `Mentoory.Web/Areas/Administration/Views/BatchUpload/Index.cshtml`: styled upload card with progress area
- [ ] T057 [P] [US7] Restyle `Mentoory.Web/Areas/Administration/Views/BatchUpload/Results.cshtml`: result summary cards with status strips

### Coordination Views

- [ ] T058 [P] [US7] Apply rich table pattern to `Mentoory.Web/Areas/Coordination/Views/Diagnostics/Index.cshtml`: progress indicators, user avatar, action buttons, empty state
- [ ] T059 [P] [US7] Apply detail card pattern to `Mentoory.Web/Areas/Coordination/Views/Diagnostics/Details.cshtml`
- [ ] T060 [P] [US7] Apply form pattern to `Mentoory.Web/Areas/Coordination/Views/Diagnostics/Clone.cshtml`
- [ ] T061 [P] [US7] Apply table/form pattern to `Mentoory.Web/Areas/Coordination/Views/AnswerCorrection/Index.cshtml`

### Participant Views

- [ ] T062 [P] [US7] Apply rich table or card list pattern to `Mentoory.Web/Areas/Participant/Views/Diagnostic/List.cshtml` with progress bars and status
- [ ] T063 [P] [US7] Add Tabler `steps` component to `Mentoory.Web/Areas/Participant/Views/Diagnostic/Index.cshtml` for diagnostic step indicator
- [ ] T064 [P] [US7] Restyle `Mentoory.Web/Areas/Participant/Views/Diagnostic/Confirmation.cshtml` as success card with green `modal-status` strip and check icon

### Platform Views

- [ ] T065 [P] [US7] Apply rich table pattern to `Mentoory.Web/Areas/Platform/Views/Incubators/Index.cshtml`: status, member count, action buttons, empty state
- [ ] T066 [P] [US7] Apply form pattern to `Mentoory.Web/Areas/Platform/Views/Incubators/Create.cshtml`: card structure, hr-text dividers
- [ ] T067 [P] [US7] Apply form pattern to `Mentoory.Web/Areas/Platform/Views/Incubators/Edit.cshtml`
- [ ] T068 [P] [US7] Apply detail card/datagrid pattern to `Mentoory.Web/Areas/Platform/Views/Incubators/Details.cshtml`
- [ ] T069 [P] [US7] Apply datagrid pattern to `Mentoory.Web/Areas/Platform/Views/Configuration/Index.cshtml` for key-value settings
- [ ] T070 [P] [US7] Restyle `Mentoory.Web/Areas/Platform/Views/Templates/Knowledge.cshtml` with card layout and consistent styling
- [ ] T071 [P] [US7] Restyle `Mentoory.Web/Areas/Platform/Views/Templates/Diagnostics.cshtml` with card layout and consistent styling
- [ ] T072 [P] [US7] Apply dashboard card pattern to `Mentoory.Web/Areas/Platform/Views/Sponsor/Index.cshtml` with stat cards
- [ ] T073 [P] [US7] Apply rich table pattern to `Mentoory.Web/Areas/Platform/Views/Users/Index.cshtml`

### Shared/Root Views

- [ ] T074 [P] [US7] Restyle `Mentoory.Web/Views/Home/Index.cshtml` as landing/dashboard with card layout
- [ ] T075 [P] [US7] Restyle `Mentoory.Web/Views/Home/Privacy.cshtml` with card layout
- [ ] T076 [P] [US7] Restyle `Mentoory.Web/Views/Context/Select.cshtml` with improved context selection UX
- [ ] T077 [P] [US7] Apply table/card pattern to `Mentoory.Web/Views/AvailableProjects/Index.cshtml`
- [ ] T078 [P] [US7] Restyle `Mentoory.Web/Views/Shared/Error.cshtml` with Tabler `.empty` component and error icon
- [ ] T079 [US7] Update `Mentoory.Web/Views/Shared/Components/ConfirmModal/Default.cshtml` to include `modal-status` colored strip at top per FR-029

**Checkpoint**: All views across all 5 areas use consistent design patterns. No Bootstrap blue visible anywhere.

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Final verification, cleanup, and cross-cutting improvements

- [ ] T080 Verify build compiles with zero warnings: `dotnet build` from repo root
- [ ] T081 Run full E2E test suite: `dotnet test` — fix any selector-based failures from DOM structure changes
- [ ] T082 [P] Audit `Mentoory.Web/wwwroot/css/mentoory.css` for any remaining #0d6efd references or redundant CSS that duplicates Tabler built-in classes
- [ ] T083 [P] Audit `Mentoory.Web/wwwroot/css/site.css` for any conflicts with new design system — consolidate into mentoory.css or remove if redundant
- [ ] T084 Visual walkthrough: navigate every area (Admin, Coordination, Participant, Platform, Access) and verify consistent patterns, no visual regressions
- [ ] T085 Run quickstart.md validation steps from `specs/010-design-system-ux-polish/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational/US1 (Phase 2)**: Depends on Setup for logo SVGs — **BLOCKS all other stories**
- **US2 (Phase 3)**: Depends on US1 (brand palette must be applied before shell restyling)
- **US3 (Phase 4)**: Depends on US1 (brand palette). Independent of US2.
- **US4 (Phase 5)**: Depends on US1. Independent of US2 and US3.
- **US5 (Phase 6)**: Depends on US1 (palette) and US3 (card patterns for stat cards)
- **US6 (Phase 7)**: Depends on US1 (palette) and Setup (logo SVGs). Independent of US2-US5.
- **US7 (Phase 8)**: Depends on US3 (table patterns) and US4 (form patterns). Must wait for component patterns to be established.
- **Polish (Phase 9)**: Depends on all user stories being complete

### User Story Dependencies

```
Setup (T001-T002)
  └─► US1: Brand Palette (T003-T006) ← FOUNDATION
        ├─► US2: Navigation Shell (T007-T021)
        ├─► US3: Rich Tables (T022-T030) ──┐
        ├─► US4: Polished Forms (T031-T032) ├─► US7: All Views (T051-T079)
        ├─► US5: Dashboard (T033-T038) ◄────┘
        └─► US6: Auth Pages (T039-T050)
              └─► Polish (T080-T085)
```

### Within Each User Story

- CSS foundation before component markup
- Helper functions before views that use them
- Reference implementation before applying pattern to remaining views
- Verify build at each checkpoint

### Parallel Opportunities

**After US1 completes, these can run in parallel:**
- US2 (Shell) and US3 (Tables) and US4 (Forms) and US6 (Auth)

**Within US3**: T025, T026, T027 (render helper functions) can run in parallel
**Within US5**: T033, T034 (DTO and Query) can run in parallel
**Within US6**: T042-T050 (individual auth views) can ALL run in parallel
**Within US7**: T051-T079 (individual area views) can ALL run in parallel — this is the highest-parallelism phase

---

## Parallel Example: User Story 3 (Rich Tables)

```
# Launch render helper functions in parallel:
Task: "Add renderAvatar() function to datatable-helper.js"
Task: "Add renderStatus() function to datatable-helper.js"
Task: "Add renderActions() function to datatable-helper.js"

# Then sequentially:
Task: "Update initDataTable with emptyState config"
Task: "Add skeleton placeholder for loading state"
```

## Parallel Example: User Story 7 (All Views)

```
# All area views can be applied in parallel (different files):
Task: "Apply rich table to Admin/Users/Index.cshtml"
Task: "Apply rich table to Admin/Projects/Index.cshtml"
Task: "Apply form pattern to Admin/Projects/Create.cshtml"
Task: "Apply rich table to Coordination/Diagnostics/Index.cshtml"
Task: "Apply rich table to Platform/Incubators/Index.cshtml"
# ... all [P] tasks in Phase 8 can run simultaneously
```

---

## Implementation Strategy

### MVP First (US1 Only)

1. Complete Phase 1: Setup (logo SVGs)
2. Complete Phase 2: US1 Brand Palette
3. **STOP and VALIDATE**: Every page should render in Mentoory coral
4. This alone transforms the product's visual identity

### Incremental Delivery

1. Setup + US1 → Brand identity established (MVP)
2. Add US2 → Professional navigation shell
3. Add US3 + US4 → Component patterns ready
4. Add US5 → Dashboard with real metrics
5. Add US7 → All views polished consistently
6. Add US6 → Auth pages branded
7. Polish → Final verification

### Parallel Strategy (Multiple Agents)

After US1:
- Agent A: US2 (Shell)
- Agent B: US3 (Tables) + US4 (Forms)
- Agent C: US6 (Auth Pages)
- Then: Agent A/B/C all work on US7 (highest parallelism)
- Then: US5 (Dashboard, needs US3 patterns)

---

## Summary

| Phase | Story | Tasks | Parallel |
|-------|-------|-------|----------|
| 1. Setup | — | 2 | 2 |
| 2. Foundation | US1 Brand | 4 | 0 |
| 3. Shell | US2 Nav | 15 | 0 |
| 4. Tables | US3 Tables | 9 | 3 |
| 5. Forms | US4 Forms | 2 | 0 |
| 6. Dashboard | US5 Dashboard | 6 | 2 |
| 7. Auth | US6 Auth | 12 | 9 |
| 8. All Views | US7 Views | 29 | 28 |
| 9. Polish | — | 6 | 2 |
| **Total** | **7 stories** | **85** | **46** |

## Notes

- [P] tasks = different files, no dependencies on incomplete tasks in same phase
- [Story] label maps task to specific user story for traceability
- Each user story is independently completable and visually verifiable
- Commit after each task or logical group
- Stop at any checkpoint to validate the story independently
- Phase 8 (US7) has the highest parallelism — 28 of 29 tasks can run simultaneously
