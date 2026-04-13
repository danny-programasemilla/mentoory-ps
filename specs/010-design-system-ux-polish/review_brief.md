# Review Brief: Mentoory Design System & UX Polish

**Spec:** specs/010-design-system-ux-polish/spec.md
**Generated:** 2026-04-13

> Reviewer's guide to scope and key decisions. See full spec for details.

---

## Feature Overview

Transforms Mentoory's UI from a functional Tabler template adoption to a polished, brand-aligned product experience. Defines a complete design system based on the Mentoory logo's yellow-to-magenta gradient palette, then applies it systematically across all ~45 views in 5 areas through a 5-phase approach: Foundation (brand theme CSS) → Shell (sidebar/topbar redesign) → Component Patterns (tables, forms, cards) → View Application → Auth Pages.

## Scope Boundaries

- **In scope:** Brand color palette as CSS tokens, sidebar with logo + styled sections + badge counts, topbar with avatar dropdown + notification placeholder, rich DataTables (avatars, status dots, relative dates, empty states, skeleton loading), polished forms (hr-text dividers, card structure), stat-card dashboards with real metrics, branded auth pages with gradient + illustration, all ~45 views updated
- **Out of scope:** Dark mode, new features/pages, WCAG accessibility audit, i18n, mobile-first redesign, functional notifications, user profile images, JS framework changes
- **Why these boundaries:** The goal is visual polish and brand alignment on existing flows — not new functionality. Keeping backend changes minimal (IMenuService badges, dashboard metric queries) limits risk.

## Critical Decisions

### Decision: Logo-gradient coral (#E07850) as primary color
- **Choice:** Replace Bootstrap's default blue (#0d6efd) globally with coral derived from logo center
- **Trade-off:** Warm coral is distinctive but less "safe" than blue for admin UIs — some users may associate orange/coral with warnings
- **Feedback:** Does coral feel authoritative enough for an admin/university platform?

### Decision: Rich tables over simple polish
- **Choice:** Full table redesign with compound avatar cells, animated status dots, relative dates, skeleton loading, and empty states
- **Trade-off:** More implementation effort per table view, but dramatically more polished result. DataTables stays (no framework change).
- **Feedback:** Is the DataTable enhancement approach (render functions) maintainable long-term?

### Decision: Illustrated auth panel (CSS/SVG-based)
- **Choice:** Brand gradient + white logo + decorative SVG illustration on auth left panel, no external image assets
- **Trade-off:** CSS/SVG illustration will be simpler than a custom graphic but avoids external asset dependency. May look less polished than a professional illustration.
- **Feedback:** Is CSS/SVG sufficient, or should we budget for a professional illustration asset?

## Areas of Potential Disagreement

> Decisions or approaches where reasonable reviewers might push back.

### Full palette override vs. selective branding
- **Decision:** Override Tabler's `--tblr-primary` globally so ALL components change color
- **Why this might be controversial:** Global override affects every button, link, and badge. Some components (info badges, secondary actions) may look odd in coral.
- **Alternative view:** Keep Tabler's blue for non-brand elements, use coral only for primary actions
- **Seeking input on:** Should we preserve Tabler's default colors for non-primary semantic elements (info, secondary)?

### Dashboard metric queries as backend changes
- **Decision:** New MediatR queries for user/project/diagnostic counts (FR-023)
- **Why this might be controversial:** This is a backend change in what's framed as a UI spec. Could be a separate spec.
- **Alternative view:** Dashboard shows static link cards until a separate "dashboard metrics" feature is built
- **Seeking input on:** Are simple count queries acceptable scope here, or should metrics be deferred?

## Naming Decisions

| Item | Name | Context |
|------|------|---------|
| CSS variable prefix | `--mentory-` | Brand design tokens (note: single 'o', not 'mentoory') |
| Spec directory | `010-design-system-ux-polish` | Sequential after 009-tabler-template-migration |
| Feature branch | `010-design-system-ux-polish` | Matches spec directory |

## Open Questions

- [ ] Logo SVG delivery: extract from PDF or create fresh? Committed to `wwwroot/images/` or `wwwroot/img/`?
- [ ] Should the `--mentory-` prefix match the product name exactly (`--mentoory-`) or stay as the shorter form?

## Risk Areas

| Risk | Impact | Mitigation |
|------|--------|------------|
| Global color override breaks subtle Tabler component styling | Medium | Test each component type after applying CSS overrides; keep semantic colors (success, danger, info) separate |
| E2E tests break due to changed CSS classes or DOM structure | Medium | Tests use data-testid attributes; run full E2E suite after each phase |
| DataTable render functions become complex and hard to maintain | Low | Extract shared render functions into datatable-helper.js; keep column renderers simple |
| Auth illustration looks amateur as CSS/SVG | Low | Start with geometric shapes; can swap for professional asset later without spec change |

---
*Share with reviewers before implementation.*
