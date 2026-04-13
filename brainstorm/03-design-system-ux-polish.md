# Brainstorm: Design System & UX Polish

**Date:** 2026-04-13
**Status:** spec-created
**Spec:** specs/010-design-system-ux-polish/

## Problem Framing

After migrating from Bootstrap 5 to the Tabler admin template (brainstorm #02, spec 009), Mentoory's UI is functional but still looks generic — default Bootstrap blue, basic table rendering, plain forms, and a utilitarian navigation shell. The migration preserved existing look & feel rather than leveraging Tabler's full component library. With only ~45 views across 5 areas, this is an ideal moment to establish a proper design system and elevate the UI to match a mature, professional product for entrepreneur incubation.

## Approaches Considered

### A: Phased Design System (Selected)
- Define brand theme (CSS custom properties from logo palette) as foundation
- Redesign shell (sidebar, topbar, footer) for maximum per-page impact
- Establish component patterns (rich tables, polished forms, stat cards)
- Apply patterns to all ~45 views
- Redesign auth pages with brand gradient and illustration
- Pros: Systematic, prevents inconsistency, each phase ships independently
- Cons: Larger overall scope

### B: View-by-View Redesign
- Redesign highest-traffic views first, expand outward
- Pros: Immediate visible impact on key screens
- Cons: Inconsistency between redesigned and untouched views, repeated pattern work

### C: Component Library First
- Build shared component library, then update views to consume it
- Pros: Most reusable long-term
- Cons: Over-engineering for ~45 views, delays visible progress

## Decision

**Approach A selected.** Key decisions:

- **Brand colors**: Coral #E07850 as primary (from logo center), golden yellow and magenta as accents. Full palette scale (50-900) defined. Replaces all Bootstrap blue globally via Tabler CSS variable overrides
- **Tables**: Rich DataTables with compound avatar cells, Tabler status dots, relative dates, icon action buttons, empty states, and skeleton loading
- **Forms**: hr-text section dividers, card structure (header/body/footer), btn-list actions, form-hint descriptions
- **Dashboard**: Stat cards with real database metrics via MediatR queries, replacing link-only cards
- **Navigation**: Logo SVG in sidebar, styled section headers, active left border, user avatar dropdown in topbar, notification bell placeholder
- **Auth pages**: Brand gradient panel with white logo, Spanish tagline, decorative SVG illustration

## Open Threads

- Logo SVG delivery mechanism: extract from PDF or create fresh? Where to store in wwwroot?
- CSS variable prefix: `--mentory-` (current) vs `--mentoory-` (matching product name exactly)
- Whether global `--tblr-primary` override might cause issues with non-primary Tabler components
