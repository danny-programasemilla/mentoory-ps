# Review Brief: Dashboard Rewrite & UI Polish

**Spec:** specs/011-dashboard-ui-polish/spec.md
**Generated:** 2026-04-13

> Reviewer's guide to scope and key decisions. See full spec for details.

---

## Feature Overview

Corrective follow-up to spec 010 (Design System & UX Polish). The Administration Dashboard has visual bugs caused by incorrect usage of Tabler's `card-status-start` pattern — it was applied as a CSS class on the card element instead of as a child `<div>`. This produces full-height colored lines, vertically stacked text, and overlapping content. This spec rewrites the dashboard view with correct Tabler patterns, adds CSS polish (shadows, hover effects, brand warmth) to `mentoory.css`, and verifies no regressions on 5 other key pages plus the login page.

## Scope Boundaries

- **In scope:** Dashboard view rewrite (correct Tabler markup), shared CSS polish (card shadows, hover transitions, typography hierarchy, brand color presence), visual QA on 6 pages
- **Out of scope:** New metrics/visualizations, dashboard layout restructuring, redesigning other pages, mobile improvements beyond bug fixes, dark mode
- **Why these boundaries:** The goal is to fix what's broken and elevate what's flat, not to add new features or change the dashboard's conceptual structure

## Critical Decisions

### Fix approach: Rewrite dashboard vs. patch markup
- **Choice:** Full view rewrite using correct Tabler patterns (Approach B from brainstorm)
- **Trade-off:** More work than a patch, but cleaner result and eliminates root cause rather than working around it
- **Feedback:** Does a full rewrite feel proportionate for a ~130-line Razor view?

### CSS changes in shared stylesheet
- **Choice:** All polish goes in existing `mentoory.css` — card shadows, hover effects apply globally to all `.card` elements
- **Trade-off:** Other pages with cards will also get shadows/hover effects (expected positive side effect, but could surface unexpected issues)
- **Feedback:** Is global card polish acceptable, or should it be scoped to dashboard-only classes?

## Areas of Potential Disagreement

### Tabler class names in a "what not how" spec
- **Decision:** Spec references specific Tabler classes (`card-status-start`, `bg-primary`, `row-deck`)
- **Why this might be controversial:** Specs should focus on WHAT, not HOW — CSS classes are implementation details
- **Alternative view:** Describe only the visual outcome ("colored left strip") and let implementer choose the pattern
- **Seeking input on:** Is referencing Tabler patterns acceptable here since the root cause IS incorrect pattern usage?

## Naming Decisions

| Item | Name | Context |
|------|------|---------|
| Spec directory | `011-dashboard-ui-polish` | Sequential after 010-design-system-ux-polish |
| Feature branch | `011-dashboard-ui-polish` | Matches spec directory |

## Open Questions

(None — scope is well-defined)

## Risk Areas

| Risk | Impact | Mitigation |
|------|--------|------------|
| Shared CSS changes regress other pages | Medium | FR-011/FR-012 require visual QA on 6 specific pages |
| Card shadow/hover styles conflict with Tabler defaults | Low | mentoory.css loads after tabler.min.css, overrides are intentional |
| `row-deck` behavior differs across viewport sizes | Low | Acceptance scenario specifies desktop width (1200px+); mobile is out of scope |

---
*Share with reviewers before implementation.*
