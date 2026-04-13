# Brainstorm: Tabler Admin Template Migration

**Date:** 2026-04-13
**Status:** spec-created
**Spec:** specs/009-tabler-template-migration/

## Problem Framing

Mentoory uses a custom Bootstrap 5 layout (sidebar + topbar + footer) with Font Awesome icons and hand-written CSS. The user wants to adopt the Tabler admin template (https://tabler.io/admin-template) for a polished, professional admin UI with less custom CSS to maintain. Tabler extends Bootstrap 5, so most existing utility classes remain compatible — the migration focuses on the page skeleton, navigation, icons, and auth pages.

## Approaches Considered

### A: Layout-First Full Replacement (Selected)
- Replace the entire layout shell, then icons, then area views in one spec
- Tabler CSS/JS replaces standalone Bootstrap (Tabler bundles it)
- Pros: Clean cut, no dual-CSS bloat, manageable scope (~45 views)
- Cons: Larger single changeset

### B: Dual-Layout Coexistence
- Keep old layout while building new one in parallel, migrate areas one at a time
- Pros: Safer rollback per area
- Cons: CSS bloat during transition, two layouts to maintain, overkill for this project size

### C: Component-First Migration
- Start with view components, then layout, then views
- Pros: Bottom-up testing
- Cons: Components depend on layout context — testing in isolation is confusing

## Decision

**Approach A selected.** Full replacement, layout-first. Key decisions:

- **Icons**: Tabler Icons only (remove Font Awesome entirely). 44 occurrences across 22 views + MenuConfiguration.cs
- **Auth pages**: Split-panel layout (branded left panel placeholder + form right). New `_AuthLayout.cshtml`
- **DataTables**: Keep jQuery + DataTables, restyle for Tabler compatibility
- **Dark mode**: Not included (light theme, dark sidebar only). Can be added later
- **Installation**: Local files in `wwwroot/lib/tabler/`, not CDN
- **Branding panel**: Solid color + logo text placeholder, structured for easy image swap later

## Open Threads

- Tabler Icons rendering approach (inline SVG vs webfont) deferred to implementation — affects how `MenuConfiguration.cs` stores icon identifiers
