# Brainstorm: Page Content Banner Strip

**Date:** 2026-06-10
**Status:** active

## Problem Framing

Authenticated pages currently open with the feature-021 themed header band (breadcrumb +
H2 title + action buttons + topbar, with a decorative per-section SVG anchored right),
then drop straight into page content (DataTable + filter controls, forms, detail cards).
The transition from header to content is abrupt and the page content itself has no
on-brand visual anchor that signals *what kind of page* the user is on.

The goal is a **slim, sober content banner strip** (~60px) that sits inside `.page-body`,
directly above the page content and below the 021 header band. It reuses the platform's
existing abstract design language (brand gradient like the login panel, Tabler icon as a
faint watermark on the right) and varies by the **section** (gradient) and the **action**
(icon) so it subtly reflects what the user is doing on the page. It is decorative-but-
purposeful: it also becomes the home of the page title.

This is a **second, intentional element** distinct from the 021 header band — not a
replacement for it. The 021 band remains the top accent + breadcrumb/actions/topbar zone.

## Approaches Considered

### A: CSS-rendered strip — section gradient + per-action webfont icon (CHOSEN)
- A `~60px` full-width strip rendered purely in CSS: a subtle **section gradient**
  (resolved the same way feature 021 resolves its per-section theme) plus a single
  **Tabler Icons webfont** glyph floated right as a faint watermark.
- The **page title moves into the strip** (left-aligned); the 021 header band drops its
  big H2 and keeps breadcrumb + action buttons + topbar.
- Icon is chosen **per action**: Index → `ti-list`, Create → `ti-plus`, Edit → `ti-edit`,
  Details → `ti-eye`, with a default fallback for unmapped actions.
- Pros: zero image files to author or keep in sync; instantly themeable via CSS variables;
  crisp at any DPI; tiny footprint; mirrors how the login decorations are built; reuses
  021's section-theme resolution; single source of truth for the page title.
- Cons: art is constrained to gradient + a glyph (no bespoke per-page illustration);
  moving the title touches the 021 header markup; heading-hierarchy semantics must be
  re-confirmed when the title relocates.

### B: Static SVG asset per page/action
- Author one abstract SVG per page × action under `/img/banners/`.
- Pros: bespoke art control per page.
- Cons: many files to create and maintain as pages are added; heavier; duplicates the
  asset-management burden 021 already carries; overkill for a "sober, ~60px" strip.

### C: Reuse the 021 section SVGs, restyled into the strip
- Scale/crop the 8 existing section SVGs into the 60px strip.
- Pros: fewest new assets.
- Cons: the strip would look near-identical to the header band directly above it —
  two stacked, near-duplicate art blocks; visually redundant; no per-action signal.

## Decision

**Approach A.** A pure-CSS ~60px content banner strip living in `.page-body` above the page
content. Gradient is **per-section** (continuity with 021's brand themes); icon is
**per-action** (faint Tabler webfont glyph, right-aligned). The **page title moves into the
strip** as its primary title block; the 021 header band keeps breadcrumb + actions +
topbar but drops its H2. Applies to **all authenticated pages on the main `_Layout`**;
auth (login) and error pages use a different layout and are excluded. Tone: sober and
professional.

## Key Requirements

- **R1 — Placement:** Strip renders inside `.page-body`, full content width, directly above
  `@RenderBody()` content (DataTable + filters / forms / detail cards) and below the 021
  header band. Implemented once in the shared layout so it appears on every qualifying page.
- **R2 — Height & tone:** Approximately 60px tall; sober/professional; subtle gradient, not
  loud. Must read as part of the brand system (login panel + 021 band lineage).
- **R3 — Section gradient:** Background gradient varies by section, resolved consistently
  with feature 021's per-section theme (coral / gold / magenta family). Reuse 021's
  section resolution rather than introduce a parallel mapping.
- **R4 — Per-action icon:** A single Tabler Icons webfont glyph, floated right as a faint
  watermark, chosen by the route action — Index → `ti-list`, Create → `ti-plus`,
  Edit → `ti-edit`, Details → `ti-eye`, plus a sensible default for unmapped actions.
- **R5 — Title relocation:** The page title (currently the 021 header H2, sourced from
  `ViewData["Title"]`) becomes the strip's left-aligned title. The 021 header band no longer
  renders the big H2; it retains breadcrumb, `PageActions`, and the topbar. No visible title
  duplication.
- **R6 — Scope:** All authenticated pages using the main `_Layout`. Auth/login pages and
  error pages (separate layouts) are excluded automatically.
- **R7 — Zero new image assets:** Rendered entirely with CSS + the existing Tabler webfont.
- **R8 — Fallbacks:** Pages with an unmapped action get a default icon + section gradient;
  define behavior for pages with no `ViewData["Title"]`.
- **R9 — Accessibility:** The relocated title must remain a proper page heading in the
  document outline; the decorative icon is `aria-hidden`; gradient + text must meet
  WCAG-AA contrast.

## Open Questions

- Where should the icon + gradient keys be computed — extend feature 021's `HeaderTheme`
  resolver (add an action→icon facet) or introduce a sibling helper? (defer to `/speckit-plan`)
- Exact, complete action→icon map and the default glyph for unmapped actions.
- Heading-hierarchy decision: when the title moves into the strip, is the strip title the
  page `h1`/`h2`, and does the 021 band need any residual heading for screen readers?
- Small-screen behavior: keep the gradient + title and drop the watermark icon (as 021 does
  below `md`), or keep the full strip?
- Should individual views be able to override the strip's icon or supply a custom subtitle
  later (extensibility), or is the route-derived mapping sufficient for v1?
- Does the dashboard/home page (and any non-CRUD pages) need bespoke icon entries, or do
  they fall through to the default?
