# Review Brief: Tabler Admin Template Migration

**Spec:** specs/009-tabler-template-migration/spec.md
**Generated:** 2026-04-13

> Reviewer's guide to scope and key decisions. See full spec for details.

---

## Feature Overview

Migrate Mentoory's entire frontend template from a custom Bootstrap 5 layout to the Tabler admin template (`@tabler/core`). Tabler extends Bootstrap 5 with a polished page skeleton, sidebar, and icon library. This is a pure Web layer change — no backend, domain, or database modifications. The migration covers the layout shell, sidebar navigation, topbar, footer, auth pages (split-panel), all icons (Font Awesome to Tabler Icons), view components, and asset cleanup across ~45 views.

## Scope Boundaries

- **In scope:** Layout skeleton, sidebar, topbar, footer, breadcrumbs, auth page layout, all icons, view components (DataTable/Toast/ConfirmModal), CSS/JS asset swap, `mentoory.css` cleanup, constitution UI Framework reference update
- **Out of scope:** Dark mode, jQuery/DataTables replacement, backend changes, new features, branding assets (placeholder only), `IMenuService` interface changes beyond icon type
- **Why these boundaries:** Tabler is a Bootstrap 5 superset, so most view content (utility classes, cards, forms, modals) is inherently compatible. The migration focuses on what Tabler actually changes: page structure, navigation pattern, and icon library. Everything else (DataTables, jQuery, backend) is orthogonal.

## Critical Decisions

### Full Replacement (Not Progressive)
- **Choice:** Replace the entire layout, icons, and assets in one spec/branch rather than phased migration
- **Trade-off:** Larger changeset but avoids dual-CSS bloat and the complexity of maintaining two layout systems simultaneously
- **Feedback:** Is the ~45 view scope acceptable for a single branch, or would you prefer phased delivery?

### Tabler Icons Only (Remove Font Awesome)
- **Choice:** Clean cut — zero Font Awesome references after migration
- **Trade-off:** Requires touching 22 views + `MenuConfiguration.cs` for icon swaps, but eliminates dual icon library confusion
- **Feedback:** Any Font Awesome icons you consider essential that might not have a Tabler equivalent?

### Split-Panel Auth Pages
- **Choice:** Left branded panel (placeholder) + right form panel for all 6 auth pages
- **Trade-off:** More visual polish than centered card, but requires a new `_AuthLayout.cshtml` and restructuring auth views
- **Feedback:** Is the placeholder approach (solid color + text, swappable to image later) sufficient for launch?

## Areas of Potential Disagreement

> Decisions or approaches where reasonable reviewers might push back.

### DataTables Bootstrap 5 Compatibility Assumption
- **Decision:** Assumed `dataTables.bootstrap5` styling works with Tabler's Bootstrap 5 base without changes
- **Why this might be controversial:** Tabler overrides some Bootstrap variables and adds `tblr-` prefixed custom properties. DataTables styling might look slightly off.
- **Alternative view:** Could require a custom DataTables stylesheet for Tabler
- **Seeking input on:** Has anyone tested DataTables with Tabler specifically?

### Icon Rendering Approach Left Open
- **Decision:** Deferred the inline SVG vs webfont choice for Tabler Icons to implementation
- **Why this might be controversial:** This affects `MenuConfiguration.cs` — if inline SVG, icons can't be stored as simple class strings anymore
- **Alternative view:** Could mandate webfont approach to keep `MenuConfiguration.cs` simple (icon as string class name)
- **Seeking input on:** Does the team prefer simpler C# code (webfont) or better rendering quality (inline SVG)?

## Naming Decisions

| Item | Name | Context |
|------|------|---------|
| New layout file | `_AuthLayout.cshtml` | Split-panel auth layout, lives in `Views/Shared/` |
| Asset directory | `wwwroot/lib/tabler/` | Local Tabler CSS/JS files, replaces `wwwroot/lib/bootstrap/` |
| Feature branch | `009-tabler-template-migration` | Spec-kit convention: `NNN-feature-name` |

## Open Questions

- [ ] Tabler Icons rendering: inline SVG vs webfont? (Affects `MenuConfiguration.cs` icon storage pattern)

## Risk Areas

| Risk | Impact | Mitigation |
|------|--------|------------|
| DataTables styling mismatch with Tabler | Med | Test early; custom CSS overrides if needed |
| Tabler Icon equivalent missing for an FA icon | Low | Use closest semantic match, document in implementation notes |
| Custom JS conflicts with Tabler JS | Low | Tabler JS is minimal; custom scripts use standard Bootstrap APIs |
| Large changeset makes review harder | Med | Organize commits by user story priority (P1 layout, P2 icons/components, P3 cleanup) |

---
*Share with reviewers before implementation.*
