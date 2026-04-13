# Tasks: Tabler Admin Template Migration

**Input**: Design documents from `/specs/009-tabler-template-migration/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, quickstart.md

**Tests**: No automated test tasks — this is a visual/frontend migration. Verification is via `dotnet build` (zero warnings) and manual visual smoke testing per user story checkpoint.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Web views**: `Mentoory.Web/Views/`
- **Shared partials**: `Mentoory.Web/Views/Shared/`
- **Area views**: `Mentoory.Web/Areas/{AreaName}/Views/`
- **Static assets**: `Mentoory.Web/wwwroot/`
- **Infrastructure**: `Mentoory.Web/Infrastructure/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Install Tabler assets and remove standalone Bootstrap

- [X] T001 Download `@tabler/core` dist files and place CSS in `Mentoory.Web/wwwroot/lib/tabler/css/tabler.min.css` and JS in `Mentoory.Web/wwwroot/lib/tabler/js/tabler.min.js`
- [X] T002 Download `@tabler/icons-webfont` dist files and place CSS in `Mentoory.Web/wwwroot/lib/tabler-icons-webfont/tabler-icons.min.css` and font files in `Mentoory.Web/wwwroot/lib/tabler-icons-webfont/fonts/`
- [X] T003 Remove `Mentoory.Web/wwwroot/lib/bootstrap/` directory entirely

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Rewrite the main layout — ALL user stories depend on this being complete

**CRITICAL**: No user story work can begin until this phase is complete

- [X] T004 Rewrite `Mentoory.Web/Views/Shared/_Layout.cshtml` to Tabler's `.page` > `.navbar-vertical` + `.page-wrapper` structure. Update CSS references to load `tabler.min.css`, `tabler-icons.min.css`, `dataTables.bootstrap5.min.css`, `mentoory.css`. Update JS references to load `jquery.min.js`, `tabler.min.js`, `dataTables.min.js`, `dataTables.bootstrap5.min.js`, `site.js`. Preserve `@RenderSectionAsync("Styles")` and `@RenderSectionAsync("Scripts")` hooks. Preserve conditional rendering for authenticated vs unauthenticated users.

**Checkpoint**: Layout compiles and renders — page shows Tabler structure skeleton (content may be unstyled until partials are updated)

---

## Phase 3: User Story 1 - Authenticated Dashboard Navigation (Priority: P1)

**Goal**: Authenticated users see a Tabler-styled dark sidebar with working navigation, footer, and breadcrumbs.

**Independent Test**: Log in as any role. Verify sidebar renders with correct menu items, Tabler Icons, section separators. Navigate between pages — confirm active state. Resize browser to mobile width — verify hamburger collapse. Verify footer and breadcrumbs render.

### Implementation for User Story 1

- [X] T005 [US1] Rewrite `Mentoory.Web/Views/Shared/_Navigation.cshtml` to Tabler's `<aside class="navbar navbar-vertical navbar-expand-sm" data-bs-theme="dark">` pattern. Preserve `IMenuService.GetVisibleMenuItems()` integration. Keep section separators ("Gestion de Plataforma", "Administracion del Contexto"). Add `.active` class logic for current page using `ViewContext.RouteData`. Add hamburger toggle button (`navbar-toggler`) for mobile collapse.
- [X] T006 [US1] Update `Mentoory.Web/Infrastructure/Menu/MenuConfiguration.cs` — replace all Font Awesome icon strings with Tabler Icons webfont equivalents: `"fas fa-home"` to `"ti ti-home"`, `"fas fa-cog"` to `"ti ti-settings"`, `"fas fa-building"` to `"ti ti-building"`, `"fas fa-users"` to `"ti ti-users"`, `"fas fa-file-alt"` to `"ti ti-file-text"`, `"fas fa-users-cog"` to `"ti ti-users-group"`, `"fas fa-tachometer-alt"` to `"ti ti-dashboard"`, `"fas fa-project-diagram"` to `"ti ti-sitemap"`, `"fas fa-file-upload"` to `"ti ti-file-upload"`, `"fas fa-tasks"` to `"ti ti-list-check"`, `"fas fa-clipboard-check"` to `"ti ti-clipboard-check"`, `"fas fa-user-graduate"` to `"ti ti-school"`, `"fas fa-poll"` to `"ti ti-chart-bar"`. Verify icon names at https://tabler.io/icons.
- [X] T007 [P] [US1] Rewrite `Mentoory.Web/Views/Shared/_Footer.cshtml` to Tabler's `.footer.footer-transparent.d-print-none` pattern inside a `.container-xl` wrapper.
- [X] T008 [P] [US1] Update `Mentoory.Web/Views/Shared/_Breadcrumbs.cshtml` to render inside Tabler's `.page-header` structure. Keep the same route-based auto-generation logic.

**Checkpoint**: Sidebar renders with Tabler Icons, section separators, active state highlighting, and hamburger collapse. Footer and breadcrumbs render in Tabler style. `dotnet build` passes with zero warnings.

---

## Phase 4: User Story 2 - Context Display and Switching (Priority: P1)

**Goal**: Context display (role badge, incubator, project) and context-switcher modal work in the Tabler topbar.

**Independent Test**: Log in as a user with multiple roles. Verify context badge, incubator name, and project name display in the Tabler page header. Click "Cambiar contexto" — verify modal opens with cascading dropdowns. Switch context — verify page reloads with new context.

### Implementation for User Story 2

- [X] T009 [US2] Rewrite `Mentoory.Web/Views/Shared/_TopBar.cshtml` to fit Tabler's page header area inside `.page-wrapper`. Preserve context display: active role badge (`<span class="badge">`), incubator name (with `ti ti-building` icon), project name (with `ti ti-sitemap` icon). Preserve "Cambiar contexto" button triggering `#contextSwitcherModal`. Preserve user name display and "Cerrar sesion" logout form. Keep the context-switcher modal markup (no changes to `_ContextSelector.cshtml` logic).
- [X] T010 [US2] Verify `Mentoory.Web/Views/Shared/_ContextSelector.cshtml` renders correctly within the Tabler modal. Check that `form-select`, `form-label`, `btn btn-primary`, `spinner-border` classes work with Tabler's CSS. Update classes only if visual issues are found.

**Checkpoint**: Context display shows role/incubator/project correctly. Context-switcher modal opens and cascade dropdowns work. Context switches successfully.

---

## Phase 5: User Story 3 - Auth Pages with Split-Panel Layout (Priority: P2)

**Goal**: Unauthenticated users see a split-panel auth layout (branded left panel + form right panel).

**Independent Test**: Visit `/Access/Login` without authentication. Verify split-panel layout: left panel with brand color and "Mentoory" text, right panel with login form. Test on mobile — verify panels stack vertically. Visit Register, ForgotPassword, ResetPassword, VerifyEmail, ChangePassword — all show same layout.

### Implementation for User Story 3

- [X] T011 [US3] Create `Mentoory.Web/Views/Shared/_AuthLayout.cshtml` — split-panel layout. Left panel: `col-lg-6` with solid brand-color background (use `--tblr-primary` or custom Mentoory color), centered "Mentoory" text, `min-height: 100vh`, structured for easy background-image swap later. Right panel: `col-lg-6` with centered form area and padding. Load Tabler CSS/JS. Include `@RenderSectionAsync("Styles")` and `@RenderSectionAsync("Scripts")` hooks. No sidebar or topbar.
- [X] T012 [US3] Update `Mentoory.Web/Areas/Access/Views/_ViewStart.cshtml` to point to `_AuthLayout` instead of `_Layout`.
- [X] T013 [US3] Update `Mentoory.Web/Areas/Access/Views/Login/Index.cshtml` — remove `login-container`/`login-card` wrapper classes (now handled by _AuthLayout). Keep form content, validation, and scripts section.
- [X] T014 [P] [US3] Update `Mentoory.Web/Areas/Access/Views/Register/Index.cshtml` — remove wrapper classes, adapt to _AuthLayout right panel.
- [X] T015 [P] [US3] Update `Mentoory.Web/Areas/Access/Views/ForgotPassword/Index.cshtml` and `Mentoory.Web/Areas/Access/Views/ForgotPassword/Confirmation.cshtml` — remove wrapper classes, adapt to _AuthLayout.
- [X] T016 [P] [US3] Update `Mentoory.Web/Areas/Access/Views/ResetPassword/Index.cshtml` and `Mentoory.Web/Areas/Access/Views/ResetPassword/Success.cshtml` — remove wrapper classes, adapt to _AuthLayout.
- [X] T017 [P] [US3] Update `Mentoory.Web/Areas/Access/Views/VerifyEmail/Index.cshtml` — remove wrapper classes, adapt to _AuthLayout.
- [X] T018 [P] [US3] Update `Mentoory.Web/Areas/Access/Views/ChangePassword/Index.cshtml` — remove wrapper classes, adapt to _AuthLayout.
- [X] T019 [US3] Verify `Mentoory.Web/Views/Shared/Error.cshtml` renders correctly in both _Layout (authenticated) and _AuthLayout (unauthenticated) states.

**Checkpoint**: All auth pages render with split-panel layout. Forms submit correctly. Mobile stacks vertically. Error page works in both states.

---

## Phase 6: User Story 4 - Icon Migration (Priority: P2)

**Goal**: All Font Awesome icons replaced with Tabler Icons across all area views.

**Independent Test**: Run `grep -r "fas fa-\|far fa-\|fab fa-" Mentoory.Web/` — must return zero results. Visually verify icons on key pages.

### Implementation for User Story 4

- [X] T020 [P] [US4] Replace Font Awesome icons in `Mentoory.Web/Areas/Administration/Views/Dashboard/Index.cshtml` (4 occurrences)
- [X] T021 [P] [US4] Replace Font Awesome icons in `Mentoory.Web/Areas/Administration/Views/Projects/Index.cshtml` and `Mentoory.Web/Areas/Administration/Views/Projects/Create.cshtml` and `Mentoory.Web/Areas/Administration/Views/Projects/Details.cshtml`
- [X] T022 [P] [US4] Replace Font Awesome icons in `Mentoory.Web/Areas/Administration/Views/Users/Index.cshtml` and `Mentoory.Web/Areas/Administration/Views/Users/Enroll.cshtml`
- [X] T023 [P] [US4] Replace Font Awesome icons in `Mentoory.Web/Areas/Coordination/Views/Diagnostics/Index.cshtml`, `Mentoory.Web/Areas/Coordination/Views/Diagnostics/Details.cshtml`, `Mentoory.Web/Areas/Coordination/Views/Diagnostics/Clone.cshtml`, and `Mentoory.Web/Areas/Coordination/Views/AnswerCorrection/Index.cshtml`
- [X] T024 [P] [US4] Replace Font Awesome icons in `Mentoory.Web/Areas/Participant/Views/Diagnostic/Index.cshtml`, `Mentoory.Web/Areas/Participant/Views/Diagnostic/List.cshtml`, and `Mentoory.Web/Areas/Participant/Views/Diagnostic/Confirmation.cshtml`
- [X] T025 [P] [US4] Replace Font Awesome icons in `Mentoory.Web/Areas/Platform/Views/Incubators/Index.cshtml`, `Mentoory.Web/Areas/Platform/Views/Incubators/Create.cshtml`, `Mentoory.Web/Areas/Platform/Views/Incubators/Edit.cshtml`, `Mentoory.Web/Areas/Platform/Views/Incubators/Details.cshtml`
- [X] T026 [P] [US4] Replace Font Awesome icons in `Mentoory.Web/Areas/Platform/Views/Templates/Knowledge.cshtml`, `Mentoory.Web/Areas/Platform/Views/Configuration/Index.cshtml`, `Mentoory.Web/Areas/Platform/Views/Sponsor/Index.cshtml`, `Mentoory.Web/Areas/Platform/Views/Users/Index.cshtml`
- [X] T027 [P] [US4] Replace Font Awesome icons in `Mentoory.Web/Views/AvailableProjects/Index.cshtml`
- [X] T028 [US4] Run `grep -r "fas fa-\|far fa-\|fab fa-" Mentoory.Web/` to verify zero Font Awesome references remain

**Checkpoint**: Zero Font Awesome references in codebase. All icons render as Tabler Icons across all areas.

---

## Phase 7: User Story 5 - DataTables, Toasts, and Modals (Priority: P2)

**Goal**: All interactive components work correctly with Tabler styling.

**Independent Test**: Navigate to Projects Index — verify DataTable loads, paginates, sorts. Trigger a success action — verify toast appears. Trigger a delete — verify confirm modal works.

### Implementation for User Story 5

- [X] T029 [P] [US5] Verify and update `Mentoory.Web/Views/Shared/Components/DataTable/Default.cshtml` — ensure table classes (`table`, `table-striped`, `table-hover`) work with Tabler. Wrap in Tabler card if needed.
- [X] T030 [P] [US5] Verify and update `Mentoory.Web/Views/Shared/Components/Toast/Default.cshtml` — ensure toast container positioning works with Tabler's page structure. Verify `showToast()` JS function renders correctly.
- [X] T031 [P] [US5] Verify and update `Mentoory.Web/Views/Shared/Components/ConfirmModal/Default.cshtml` — ensure modal classes work with Tabler. Verify confirm/cancel buttons function.
- [X] T032 [US5] Add DataTables-Tabler CSS compatibility overrides in `Mentoory.Web/wwwroot/css/mentoory.css` — fix `.dt-paging .pagination` spacing, `.dt-info` font styling, and any other chrome element misalignment using `--tblr-*` CSS variables (~20-40 lines per research.md findings).

**Checkpoint**: DataTables load and paginate. Toasts appear correctly. Modals open and function.

---

## Phase 8: User Story 6 - Asset Cleanup (Priority: P3)

**Goal**: CSS cleaned up, constitution updated, build passes with zero warnings.

**Independent Test**: `dotnet build` — zero warnings. Check `wwwroot/lib/` — no `bootstrap/` directory. Check `mentoory.css` — no duplicate rules. Constitution references Tabler.

### Implementation for User Story 6

- [X] T033 [US6] Update `Mentoory.Web/wwwroot/css/mentoory.css` — remove `#sidebar` rules (replaced by Tabler sidebar), remove `main` background rule, remove `.page-header` custom rules (conflicts with Tabler's `.page-header`), update `.login-container` and `.login-card` if still needed or remove if _AuthLayout handles it, use `--tblr-*` CSS variables for custom properties.
- [X] T034 [P] [US6] Verify `Mentoory.Web/wwwroot/css/site.css` for Bootstrap-specific rules that conflict with Tabler. Update or remove conflicting rules.
- [X] T035 [US6] Update `.specify/memory/constitution.md` — change "Bootstrap 5 with Phoenix Admin Template" to "Tabler Admin Template (built on Bootstrap 5)" in the UI Framework section.
- [X] T036 [P] [US6] Update `CLAUDE.md` Active Technologies section to reference Tabler instead of Phoenix Admin Template.
- [X] T037 [US6] Run `dotnet build` and verify zero warnings. Fix any warnings introduced by the migration.
- [X] T038 [US6] Final visual smoke test: navigate all major pages across all areas (Access, Administration, Coordination, Participant, Platform), verify DataTables, modals, toasts, context switching, breadcrumbs, sidebar navigation all work correctly.

**Checkpoint**: Build passes with zero warnings. All pages render correctly. No Bootstrap or Font Awesome remnants. Constitution updated.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup (T001-T003) — BLOCKS all user stories
- **US1 (Phase 3)**: Depends on Foundational (T004)
- **US2 (Phase 4)**: Depends on Foundational (T004); pairs naturally with US1 but is independently testable
- **US3 (Phase 5)**: Depends on Foundational (T004); independent of US1/US2
- **US4 (Phase 6)**: Depends on US1 (T006 establishes icon mapping pattern); all area view tasks are parallel
- **US5 (Phase 7)**: Depends on Foundational (T004); independent of other stories
- **US6 (Phase 8)**: Depends on ALL previous phases — cleanup and verification is the final step
- **Polish**: Embedded in US6

### User Story Dependencies

```
Setup (T001-T003)
  └── Foundational: _Layout.cshtml (T004) ← BLOCKS ALL
        ├── US1: Sidebar + Footer + Breadcrumbs (T005-T008)
        ├── US2: TopBar + Context (T009-T010) ← can parallel with US1
        ├── US3: Auth Layout (T011-T019) ← can parallel with US1/US2
        ├── US5: View Components (T029-T032) ← can parallel with US1/US2/US3
        │
        └── US4: Icon Sweep (T020-T028) ← depends on US1 (icon mapping established)
              └── US6: Cleanup + Verification (T033-T038) ← depends on ALL
```

### Within Each User Story

- Shared partials before area views
- Core structure before details
- Verification step at end of each story

### Parallel Opportunities

- T001, T002 can run in parallel (downloading different packages)
- T007, T008 can run in parallel (different files, both US1)
- T014-T018 can ALL run in parallel (different auth views, all US3)
- T020-T027 can ALL run in parallel (different area views, all US4)
- T029-T031 can ALL run in parallel (different view components, all US5)
- T033, T034 can run in parallel (different CSS files, both US6)
- T035, T036 can run in parallel (different config files, both US6)
- US1, US2, US3, and US5 can ALL proceed in parallel after Foundational phase

---

## Parallel Example: User Story 4 (Icon Sweep)

```bash
# Launch all area icon replacement tasks in parallel:
Task: "T020 Replace FA icons in Administration/Dashboard"
Task: "T021 Replace FA icons in Administration/Projects"
Task: "T022 Replace FA icons in Administration/Users"
Task: "T023 Replace FA icons in Coordination views"
Task: "T024 Replace FA icons in Participant views"
Task: "T025 Replace FA icons in Platform/Incubators"
Task: "T026 Replace FA icons in Platform other views"
Task: "T027 Replace FA icons in AvailableProjects"

# Then verify:
Task: "T028 Verify zero FA references"
```

---

## Implementation Strategy

### MVP First (User Stories 1 + 2 Only)

1. Complete Phase 1: Setup (T001-T003)
2. Complete Phase 2: Foundational — _Layout.cshtml (T004)
3. Complete Phase 3: US1 — Sidebar + Navigation (T005-T008)
4. Complete Phase 4: US2 — Context Display (T009-T010)
5. **STOP and VALIDATE**: Log in, navigate, switch context — core dashboard works
6. Deploy/demo if ready

### Incremental Delivery

1. Setup + Foundational + US1 + US2 → Core dashboard works (MVP)
2. Add US3 → Auth pages polished
3. Add US4 → All icons migrated
4. Add US5 → Components verified
5. Add US6 → Cleanup complete, ready to ship

### Single Developer Strategy (Recommended)

Execute sequentially in priority order:
1. T001-T003 (Setup)
2. T004 (Layout)
3. T005-T008 (Sidebar/Footer/Breadcrumbs)
4. T009-T010 (TopBar/Context)
5. T011-T019 (Auth pages)
6. T020-T028 (Icon sweep — batch all parallel tasks)
7. T029-T032 (View components)
8. T033-T038 (Cleanup)

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- No automated tests — verification is visual + `dotnet build` zero warnings
- Commit after each phase checkpoint
- Icon mapping reference in `specs/009-tabler-template-migration/research.md`
- CSS load order documented in `specs/009-tabler-template-migration/quickstart.md`
