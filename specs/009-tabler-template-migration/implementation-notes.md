# Implementation Notes: Tabler Admin Template Migration

## Design Decisions

### Decision: Full Replacement vs. Progressive Migration
- Chose full replacement (all layout, icons, assets in one spec)
- Rationale: Tabler bundles Bootstrap 5 — running both simultaneously adds CSS bloat with no benefit. ~45 views is manageable in a single pass.
- Rejected progressive/dual-layout: unnecessary complexity for this project size

### Decision: Tabler Icons Only (No Font Awesome)
- Chose clean cut — remove all Font Awesome, use only Tabler Icons
- Rationale: 44 icon occurrences across 22 views + 1 C# file is small enough for a full sweep. Dual icon libraries are confusing for future development.
- Font Awesome is not explicitly loaded via CSS/JS in the layout — icons may have been rendered via a CDN link that was removed or were part of the original Phoenix template setup

### Decision: Split-Panel Auth Pages with Placeholder Branding
- Chose Tabler's split-panel sign-in pattern (left branded panel + right form)
- Left panel: solid brand-color background with centered "Mentoory" text
- Structured so a background-image or illustration can replace the solid color later without layout changes
- Rejected simple centered card: less visually distinctive
- Rejected illustration panel: no branding assets available yet

### Decision: Keep jQuery + DataTables
- Chose to keep both libraries as-is
- Rationale: DataTables has a working server-side integration. Replacing it is a separate concern from the template migration.
- DataTables' `dataTables.bootstrap5` styling should be compatible with Tabler's Bootstrap 5 base (same class structure)

### Decision: No Dark Mode
- Chose light theme only (dark sidebar is Tabler's default vertical layout pattern)
- Rationale: dark mode is a feature, not a template concern. Tabler's dark mode is baked in and can be added later.

### Decision: Local Files (Not CDN)
- Chose to install `@tabler/core` as local files in `wwwroot/lib/tabler/`
- Rationale: consistent with existing asset management (Bootstrap is also in `wwwroot/lib/`)
- Avoids external CDN dependency and integrity/availability concerns

## Current State Reference

### Files to Modify
- `Mentoory.Web/Views/Shared/_Layout.cshtml` — main layout (rewrite)
- `Mentoory.Web/Views/Shared/_Navigation.cshtml` — sidebar (rewrite)
- `Mentoory.Web/Views/Shared/_TopBar.cshtml` — topbar (rewrite)
- `Mentoory.Web/Views/Shared/_Footer.cshtml` — footer (rewrite)
- `Mentoory.Web/Views/Shared/_Breadcrumbs.cshtml` — breadcrumbs (adapt)
- `Mentoory.Web/Views/Shared/_ContextSelector.cshtml` — class updates if needed
- `Mentoory.Web/Views/Shared/Error.cshtml` — ensure dual-layout compatibility
- `Mentoory.Web/Views/Shared/Components/DataTable/Default.cshtml` — Tabler table classes
- `Mentoory.Web/Views/Shared/Components/Toast/Default.cshtml` — Tabler toast pattern
- `Mentoory.Web/Views/Shared/Components/ConfirmModal/Default.cshtml` — Tabler modal classes
- `Mentoory.Web/Infrastructure/Menu/MenuConfiguration.cs` — icon names
- `Mentoory.Web/wwwroot/css/mentoory.css` — remove duplicates, use `--tblr-*` vars
- 22 area views with Font Awesome icon references

### Files to Create
- `Mentoory.Web/Views/Shared/_AuthLayout.cshtml` — split-panel auth layout

### Files/Directories to Remove
- `Mentoory.Web/wwwroot/lib/bootstrap/` — entire directory (replaced by Tabler)

### Files/Directories to Add
- `Mentoory.Web/wwwroot/lib/tabler/` — Tabler CSS and JS files

### Icon Mapping (Font Awesome -> Tabler Icons)
To be compiled during implementation. Current FA icons in use:
- `fas fa-home`, `fas fa-cog`, `fas fa-building`, `fas fa-users`, `fas fa-file-alt`
- `fas fa-users-cog`, `fas fa-tachometer-alt`, `fas fa-project-diagram`, `fas fa-file-upload`
- `fas fa-tasks`, `fas fa-clipboard-check`, `fas fa-user-graduate`, `fas fa-poll`
- `fas fa-plus`, `fas fa-eye`, `fas fa-exchange-alt`, `fas fa-building` (topbar)
- `fas fa-project-diagram` (topbar)

## Tabler Structure Reference

```html
<div class="page">
  <aside class="navbar navbar-vertical navbar-expand-sm" data-bs-theme="dark">
    <!-- sidebar -->
  </aside>
  <div class="page-wrapper">
    <div class="page-header d-print-none">
      <div class="container-xl"><!-- title, breadcrumbs, context --></div>
    </div>
    <div class="page-body">
      <div class="container-xl"><!-- main content --></div>
    </div>
    <footer class="footer footer-transparent d-print-none">
      <div class="container-xl"><!-- footer --></div>
    </footer>
  </div>
</div>
```

## Constitution Impact

- Constitution section "UI Framework" references "Bootstrap 5 with Phoenix Admin Template" — must be updated to "Tabler Admin Template (built on Bootstrap 5)"
- All other constitution principles (Clean Architecture, CQRS, DDD, Zero Warnings, Spanish UI, etc.) are unaffected
