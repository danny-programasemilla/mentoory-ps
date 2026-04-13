# Implementation Plan: Tabler Admin Template Migration

**Branch**: `009-tabler-template-migration` | **Date**: 2026-04-13 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/009-tabler-template-migration/spec.md`

## Summary

Replace Mentoory's custom Bootstrap 5 layout with the Tabler admin template (`@tabler/core`). Tabler extends Bootstrap 5 with a polished page skeleton, sidebar navigation, and CSS variable system. The migration covers: layout shell, sidebar, topbar, footer, breadcrumbs, auth pages (split-panel), icons (Font Awesome to Tabler Icons webfont), view components, and asset cleanup across ~45 Razor views. No backend, domain, or database changes.

## Technical Context

**Language/Version**: C# / .NET 10.0 (SDK 10.0.0) + ASP.NET Core MVC
**Primary Dependencies**: Tabler Admin Template (`@tabler/core`), Tabler Icons Webfont (`@tabler/icons-webfont`), jQuery, DataTables, jQuery Validation
**Storage**: N/A (no database changes)
**Testing**: Visual verification + `dotnet build` (zero warnings gate)
**Target Platform**: Web (ASP.NET Core MVC, Razor Views)
**Project Type**: Web application (modular monolith)
**Performance Goals**: No increase in page load time beyond what Tabler itself adds (offset by removing standalone Bootstrap)
**Constraints**: Keep jQuery/DataTables; no Node.js build pipeline; local static files only
**Scale/Scope**: ~45 Razor views, 7 shared partials, 3 view components, 1 C# config file, 2 CSS files

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Clean Architecture Layer Boundaries | PASS | Only Web layer modified (views, static assets, MenuConfiguration) |
| II. CQRS Pattern Requirements | N/A | No commands/queries/handlers affected |
| III. Domain-Driven Design Constraints | N/A | No domain entities modified |
| IV. Integration Events | N/A | No events involved |
| V. Zero-Warnings Policy | PASS | SC-009 requires zero warnings after migration |
| VI. DateTime Handling | N/A | No DateTime usage introduced |
| VII. Naming Conventions | PASS | No new artifacts requiring naming convention compliance |
| VIII. File Organization | PASS | JS stays in `wwwroot/js/`; new assets in `wwwroot/lib/`; one file per class maintained |
| IX. Spanish-First UI | PASS | All user-facing text remains in Spanish; no new UI strings introduced |
| X. Role Hierarchy & Session Context | PASS | Context display, [Authorize] attributes, and menu configuration preserved |
| XI. SSDT/DACPAC Database Strategy | N/A | No database changes |

**Constitution Update Required**: Section "UI Framework" references "Bootstrap 5 with Phoenix Admin Template" — must be updated to "Tabler Admin Template (built on Bootstrap 5)" after migration completes.

**All gates pass. No violations.**

## Project Structure

### Documentation (this feature)

```text
specs/009-tabler-template-migration/
├── spec.md              # Feature specification
├── plan.md              # This file
├── research.md          # Phase 0: research findings
├── data-model.md        # Phase 1: data model (minimal — no new entities)
├── quickstart.md        # Phase 1: setup and verification guide
├── implementation-notes.md  # Design decisions from brainstorming
├── review_brief.md      # Reviewer guide
├── REVIEW-SPEC.md       # Formal spec review
└── tasks.md             # Phase 2 output (created by /speckit-tasks)
```

### Source Code (repository root)

```text
Mentoory.Web/
├── Views/Shared/
│   ├── _Layout.cshtml              # REWRITE: Tabler .page structure
│   ├── _AuthLayout.cshtml          # NEW: split-panel auth layout
│   ├── _Navigation.cshtml          # REWRITE: Tabler .navbar-vertical
│   ├── _TopBar.cshtml              # REWRITE: Tabler page header + context display
│   ├── _Footer.cshtml              # REWRITE: Tabler .footer pattern
│   ├── _Breadcrumbs.cshtml         # UPDATE: Tabler breadcrumb styling
│   ├── _ContextSelector.cshtml     # VERIFY: may need minor class updates
│   ├── _ValidationScriptsPartial.cshtml  # VERIFY: jQuery Validation still loads
│   ├── Error.cshtml                # UPDATE: dual-layout compatibility
│   └── Components/
│       ├── DataTable/Default.cshtml    # UPDATE: Tabler table classes
│       ├── Toast/Default.cshtml        # UPDATE: Tabler notification pattern
│       └── ConfirmModal/Default.cshtml # UPDATE: Tabler modal classes
├── Areas/
│   ├── Access/Views/               # UPDATE: 9 auth views → use _AuthLayout + icon swap
│   ├── Administration/Views/       # UPDATE: 10 views → icon swap
│   ├── Coordination/Views/         # UPDATE: 6 views → icon swap
│   ├── Participant/Views/          # UPDATE: 6 views → icon swap
│   └── Platform/Views/             # UPDATE: 9 views → icon swap
├── Infrastructure/Menu/
│   └── MenuConfiguration.cs        # UPDATE: icon strings fas fa-* → ti ti-*
├── wwwroot/
│   ├── lib/
│   │   ├── tabler/                 # NEW: Tabler CSS + JS
│   │   │   ├── css/tabler.min.css
│   │   │   └── js/tabler.min.js
│   │   ├── tabler-icons-webfont/   # NEW: Tabler Icons webfont
│   │   │   ├── tabler-icons.min.css
│   │   │   └── fonts/*
│   │   ├── bootstrap/              # REMOVE: entire directory
│   │   ├── datatables/             # KEEP: unchanged
│   │   ├── jquery/                 # KEEP: unchanged
│   │   ├── jquery-validation/      # KEEP: unchanged
│   │   └── jquery-validation-unobtrusive/  # KEEP: unchanged
│   ├── css/
│   │   ├── mentoory.css            # UPDATE: remove duplicates, add Tabler overrides
│   │   └── site.css                # VERIFY: check for conflicts
│   └── js/                         # KEEP: all 7 custom JS files unchanged
└── Views/
    └── AvailableProjects/Index.cshtml  # UPDATE: icon swap
```

**Structure Decision**: Existing ASP.NET Core MVC area-based structure is unchanged. Only view files and static assets are modified. No new C# projects, no new directories beyond `wwwroot/lib/tabler/` and `wwwroot/lib/tabler-icons-webfont/`.

## Implementation Phases

### Phase 1: Asset Installation and Layout Shell (P1 — Foundation)

**Goal:** Get Tabler rendering on authenticated pages with the sidebar and page structure.

**Steps:**

1. **Install Tabler assets**
   - Download `@tabler/core` and `@tabler/icons-webfont` dist files
   - Place in `wwwroot/lib/tabler/` and `wwwroot/lib/tabler-icons-webfont/`
   - Remove `wwwroot/lib/bootstrap/` directory

2. **Rewrite `_Layout.cshtml`**
   - Replace `d-flex` sidebar + `<main>` with Tabler's `.page` > `.navbar-vertical` + `.page-wrapper` structure
   - Update CSS references: `tabler.min.css`, `tabler-icons.min.css`, `dataTables.bootstrap5.min.css`, `mentoory.css`
   - Update JS references: `jquery.min.js`, `tabler.min.js`, `dataTables.min.js`, `dataTables.bootstrap5.min.js`, `site.js`
   - Preserve `@RenderSectionAsync("Styles")` and `@RenderSectionAsync("Scripts")` hooks
   - Preserve conditional rendering (authenticated vs. unauthenticated)

3. **Rewrite `_Navigation.cshtml`**
   - Convert to Tabler's `<aside class="navbar navbar-vertical navbar-expand-sm" data-bs-theme="dark">` pattern
   - Preserve `IMenuService.GetVisibleMenuItems()` integration
   - Keep section separators ("Gestión de Plataforma", "Administración del Contexto")
   - Add `.active` class logic for current page
   - Add hamburger toggle button for mobile

4. **Rewrite `_TopBar.cshtml`**
   - Move context display (role badge, incubator, project) into Tabler's page header area
   - Preserve "Cambiar contexto" button and context-switcher modal
   - Preserve user name and "Cerrar sesión" form
   - Move breadcrumbs integration into page header

5. **Rewrite `_Footer.cshtml`**
   - Convert to Tabler's `.footer.footer-transparent.d-print-none` pattern

6. **Update `_Breadcrumbs.cshtml`**
   - Adapt styling to render inside Tabler's `.page-header` container
   - Keep route-based auto-generation logic

7. **Update `MenuConfiguration.cs`**
   - Replace all Font Awesome icon strings with Tabler Icons webfont equivalents
   - `"fas fa-home"` → `"ti ti-home"`, etc. (see research.md for full mapping)

8. **Update `mentoory.css`**
   - Remove `#sidebar` rules (replaced by Tabler sidebar)
   - Remove `main` background rule (Tabler handles this)
   - Remove `.page-header` custom rules (conflicts with Tabler's `.page-header` class)
   - Update `.login-container` and `.login-card` for auth layout
   - Add DataTables-Tabler compatibility overrides (~20-40 lines)
   - Use `--tblr-*` CSS variables where appropriate

**Verification:** Log in, verify sidebar + page header + footer render correctly. Navigate between pages. Test mobile hamburger collapse.

### Phase 2: Auth Pages and View Components (P2 — Polish)

**Goal:** Implement split-panel auth layout and update shared view components.

**Steps:**

9. **Create `_AuthLayout.cshtml`**
   - Split-panel layout: `row` > `col-lg-6` (brand panel) + `col-lg-6` (form)
   - Left panel: solid brand color background, centered "Mentoory" text, `min-height: 100vh`
   - Structured for easy image/illustration swap later (background-image CSS property)
   - Right panel: centered form card with padding
   - Responsive: stack vertically on mobile (brand panel hidden or above)
   - Load Tabler CSS/JS, no sidebar, no topbar

10. **Update auth area `_ViewStart.cshtml`**
    - Point Access area views to `_AuthLayout` instead of `_Layout`

11. **Update auth views (6 pages)**
    - Login, Register, ForgotPassword/Index, ForgotPassword/Confirmation
    - ResetPassword/Index, ResetPassword/Success, VerifyEmail, ChangePassword
    - Remove `login-container`/`login-card` wrapper classes (handled by _AuthLayout)
    - Verify forms render correctly in the right panel

12. **Update view components**
    - `DataTable/Default.cshtml`: verify table classes work with Tabler
    - `Toast/Default.cshtml`: verify toast positioning with Tabler layout
    - `ConfirmModal/Default.cshtml`: verify modal renders correctly

13. **Update `Error.cshtml`**
    - Ensure it renders correctly in both authenticated (_Layout) and unauthenticated (_AuthLayout) states

**Verification:** Visit login page — verify split-panel layout. Submit forms — verify validation and redirect work. Trigger toasts and modals — verify they render.

### Phase 3: Icon Sweep and Cleanup (P2/P3 — Completion)

**Goal:** Replace all remaining Font Awesome icons and clean up assets.

**Steps:**

14. **Sweep all area views for Font Awesome icons**
    - Administration area (10 views): Dashboard, Projects (Index/Create/Details), Users (Index/Enroll/RegisterInternal), BatchUpload (Index/Results)
    - Coordination area (6 views): Diagnostics (Index/Details/Clone), AnswerCorrection/Index
    - Participant area (6 views): Diagnostic (Index/List/Confirmation)
    - Platform area (9 views): Incubators (Index/Create/Edit/Details), Users/Index, Templates (Diagnostics/Knowledge), Configuration/Index, Sponsor/Index
    - Other: AvailableProjects/Index

15. **Verify zero Font Awesome references**
    - `grep -r "fas fa-\|far fa-\|fab fa-" Mentoory.Web/` — must return zero results

16. **Verify `site.css`**
    - Check for any Bootstrap-specific rules that conflict with Tabler
    - Update or remove if needed

17. **Final build verification**
    - `dotnet build` — zero warnings
    - Visual smoke test: navigate all major pages, verify DataTables, modals, toasts, context switching

18. **Update constitution**
    - Change "Bootstrap 5 with Phoenix Admin Template" to "Tabler Admin Template (built on Bootstrap 5)" in `.specify/memory/constitution.md`
    - Update CLAUDE.md Active Technologies section

**Verification:** Full visual smoke test across all areas. grep confirms zero FA references. Build passes with zero warnings.

## Complexity Tracking

> No constitution violations to justify. All gates passed.

| Item | Complexity | Notes |
|------|-----------|-------|
| Layout shell rewrite | Medium | 5 partial views to rewrite to Tabler patterns |
| Auth layout (new) | Low | Standard Tabler split-panel pattern |
| Icon sweep | Low (mechanical) | 22 files, string replacement + visual verify |
| DataTables CSS fixes | Low | ~20-40 lines of targeted overrides |
| Area view updates | Low (mechanical) | Most views need only icon swaps; utility classes remain valid |

## Risk Mitigation

| Risk | Mitigation |
|------|-----------|
| DataTables styling mismatch | Research confirmed compatibility with ~20-40 lines of CSS overrides; custom `dom` layout minimizes chrome elements |
| Tabler Icon missing equivalent | Full icon mapping prepared in research.md; fallback to closest semantic match |
| Custom JS conflicts with Tabler JS | Tabler JS is minimal; custom scripts use standard Bootstrap APIs (`data-bs-toggle`, modals) |
| Large changeset | Organized by phase and priority; commits per phase for easier review |

## Dependencies

```
Phase 1 (Layout Shell) → Phase 2 (Auth + Components) → Phase 3 (Icon Sweep + Cleanup)
                                                        ↑
                                                 Can be parallelized
                                                 with Phase 2 partially
```

Phase 1 is blocking — everything depends on the layout shell being in place.
Phase 2 and Phase 3 have minimal coupling and can overlap.
