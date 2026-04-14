# Research: Dashboard Rewrite & UI Polish

**Date**: 2026-04-13

## Root Cause Analysis

### Bug: Full-height colored lines, vertical text, overlapping content

**Decision**: The dashboard view uses Tabler's `card-status-start` incorrectly — as a CSS class on the card element instead of as a child `<div>`.

**Rationale**: Tabler's CSS defines `.card-status-start` as an absolutely-positioned element (`position: absolute; width: 2px; height: 100%`). When applied as a class on the card element itself, the card becomes the positioned element, and combined with `row-deck` (which uses `align-items: stretch` + `flex: 1 1 auto`), the card stretches to fill the viewport height. Additionally, `border-start border-3` adds a separate 3px border on top of the 2px status bar, creating doubled borders and squeezing the card content.

**Correct pattern** (from Tabler v1.4.0 docs via Context7):
```html
<div class="card">
  <div class="card-status-start bg-green"></div>
  <div class="card-body">...</div>
</div>
```

**Incorrect pattern** (current code, lines 68/90/112 of Index.cshtml):
```html
<div class="card card-status-start border-start border-primary border-3">
  <div class="card-body">...</div>
</div>
```

**Alternatives considered**: Patching with CSS overrides (rejected — masks root cause, fragile).

## Scope of Incorrect Pattern

The incorrect `card-status-start` pattern exists **only** in `Mentoory.Web/Areas/Administration/Views/Dashboard/Index.cshtml` (3 occurrences). No other views in the project use this pattern.

## Tabler Card Shadow Patterns

**Decision**: Add custom card shadow styles in `mentoory.css` rather than relying on Tabler utility classes.

**Rationale**: Tabler v1.4.0 does not include default card shadows — cards use `border` only. Adding `box-shadow` via custom CSS provides the visual depth requested. The shadow values should be subtle to complement rather than compete with Tabler's clean aesthetic.

**Reference values** (industry standard for card elevation):
- Rest: `box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08)`
- Hover: `box-shadow: 0 4px 12px rgba(0, 0, 0, 0.12)`
- Transition: `0.2s ease`

## E2E Test Strategy

**Decision**: Enhance existing `DashboardRenderingTests.cs` with structural assertions using `data-testid` attributes.

**Rationale**: Current tests only verify the page loads without errors. Adding `data-testid` attributes to dashboard cards enables stable selectors that won't break with CSS class changes. This follows the project's existing pattern (e.g., `data-testid='diagnostic-list'`, `data-testid='incubator-list'`).

**Alternatives considered**: Page Object Model (rejected — overkill for 2-3 new test methods in an existing file).
