# Research: Tabler Admin Template Migration

**Date:** 2026-04-13
**Feature:** 009-tabler-template-migration

## Research Questions

### 1. Tabler Icons Rendering Approach

**Decision:** Use the **webfont approach** via `@tabler/icons-webfont`.

**Rationale:**
- Drop-in replacement for Font Awesome: same `<i class="...">` pattern in Razor views
- `MenuConfiguration.cs` stays as simple string icons: `"fas fa-home"` becomes `"ti ti-home"`
- Zero structural changes to `IMenuService`, `MenuItem`, or `_Navigation.cshtml` rendering logic
- ~400KB font file is acceptable for an admin dashboard (cached after first load)

**Alternatives Considered:**

| Approach | Pros | Cons | Why Rejected |
|----------|------|------|-------------|
| Inline SVG | Pixel-perfect, per-icon styling, no font download | Requires Tag Helper or partial to map name to SVG; reworks `MenuItem.Icon` type; touches 23+ views for rendering change | Too much infrastructure change for a template migration |
| SVG Sprite | Single file, string identifiers | Requires `<svg><use>` wrappers in Razor; limited styling via shadow DOM | Medium effort, no clear win over webfont for this use case |

**Package Details:**
- `@tabler/core` does NOT bundle icons — they're separate packages
- Install `@tabler/icons-webfont` as local files in `wwwroot/lib/tabler-icons-webfont/`
- Add `<link>` tag for `tabler-icons.min.css` in layout
- Icon syntax: `<i class="ti ti-home"></i>`

**Icon Mapping (Font Awesome -> Tabler):**

| Current (FA) | Tabler Webfont | Used In |
|---|---|---|
| `fas fa-home` | `ti ti-home` | MenuConfiguration.cs |
| `fas fa-cog` | `ti ti-settings` | MenuConfiguration.cs |
| `fas fa-building` | `ti ti-building` | MenuConfiguration.cs, _TopBar.cshtml |
| `fas fa-users` | `ti ti-users` | MenuConfiguration.cs |
| `fas fa-file-alt` | `ti ti-file-text` | MenuConfiguration.cs |
| `fas fa-users-cog` | `ti ti-users-group` | MenuConfiguration.cs |
| `fas fa-tachometer-alt` | `ti ti-dashboard` | MenuConfiguration.cs |
| `fas fa-project-diagram` | `ti ti-sitemap` | MenuConfiguration.cs, _TopBar.cshtml, area views |
| `fas fa-file-upload` | `ti ti-file-upload` | MenuConfiguration.cs |
| `fas fa-tasks` | `ti ti-list-check` | MenuConfiguration.cs |
| `fas fa-clipboard-check` | `ti ti-clipboard-check` | MenuConfiguration.cs |
| `fas fa-user-graduate` | `ti ti-school` | MenuConfiguration.cs |
| `fas fa-poll` | `ti ti-chart-bar` | MenuConfiguration.cs |
| `fas fa-plus` | `ti ti-plus` | Various area views |
| `fas fa-eye` | `ti ti-eye` | Various area views |
| `fas fa-exchange-alt` | `ti ti-switch-horizontal` | _TopBar.cshtml |
| `fas fa-edit` | `ti ti-edit` | Various area views |
| `fas fa-trash` | `ti ti-trash` | Various area views |
| `fas fa-download` | `ti ti-download` | Various area views |
| `fas fa-check` | `ti ti-check` | Various area views |
| `fas fa-times` | `ti ti-x` | Various area views |
| `fas fa-search` | `ti ti-search` | Various area views |

*Note: Verify exact Tabler icon names at https://tabler.io/icons during implementation. Some mappings may need adjustment.*

---

### 2. DataTables Compatibility with Tabler

**Decision:** Keep `dataTables.bootstrap5` styling plugin. Add ~20-40 lines of Tabler-specific CSS overrides.

**Rationale:**
- `dataTables.bootstrap5` targets Bootstrap 5 class names (`.table`, `.pagination`, `.form-control`), which Tabler preserves
- 90% compatibility out of the box; only DataTables "chrome" (pagination buttons, info text) needs minor spacing fixes
- The project's `datatable-helper.js` uses a custom `dom` layout that strips search box and length selector from DataTables chrome, minimizing conflict points

**Alternatives Considered:**

| Approach | Pros | Cons | Why Rejected |
|----------|------|------|-------------|
| Plain DataTables + full custom CSS | Complete visual control | Far more CSS work; must rewrite all chrome HTML generation | Overkill when bootstrap5 plugin gives 90% for free |
| Replace with List.js (what Tabler uses internally) | Native Tabler look | Requires rebuilding all server-side integration; List.js lacks server-side processing | Different library, different scope — not a template migration concern |

**Known CSS Conflicts (from GitHub issues):**

| Element | Issue | Fix Strategy |
|---------|-------|-------------|
| `.dt-paging .pagination` | Button spacing/border-radius slightly off | Align with Tabler's `.pagination` overrides using `--tblr-*` variables |
| `.dt-info` | Font-size/color mismatch | Match `--tblr-body-font-size` and `--tblr-secondary-color` |
| `.dt-search input` | Sizing/padding mismatch (if rendered) | Override height/padding to match Tabler form-control |
| `.dt-length select` | Dropdown sizing inconsistent (if rendered) | Match Tabler's form-select sizing |

**Load Order:**
1. `tabler.min.css` (base)
2. `tabler-icons.min.css` (icons)
3. `dataTables.bootstrap5.min.css` (DataTables Bootstrap 5 integration)
4. `mentoory.css` (custom overrides including DataTables fixes)

---

### 3. Tabler Local Installation

**Decision:** Install `@tabler/core` and `@tabler/icons-webfont` as local files.

**Method:** Download dist files from npm/GitHub releases, place in `wwwroot/lib/`:
- `wwwroot/lib/tabler/css/tabler.min.css`
- `wwwroot/lib/tabler/js/tabler.min.js`
- `wwwroot/lib/tabler-icons-webfont/tabler-icons.min.css`
- `wwwroot/lib/tabler-icons-webfont/fonts/` (webfont files)

**Rationale:** Consistent with existing asset management pattern (Bootstrap is in `wwwroot/lib/bootstrap/`). No Node.js build pipeline required. No CDN dependency.
