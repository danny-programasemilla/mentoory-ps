# Brainstorm: Page Content Banner Redesign

**Date:** 2026-06-11
**Status:** active

## Problem Framing

Feature 023 shipped the page content banner strip, but the visual result fell short of
intent. As implemented it is a ~60px strip whose entire treatment comes from **one
parametric CSS formula**: `linear-gradient(90deg, transparent 0 55%, rgba(accent, 0.14)
100%)` plus a per-action icon at **18% opacity** on the right. The effect is nearly
invisible — a faint color wash, not a designed banner. The constraint that produced this
(feature 023 FR-015 / SC-007: "no new image asset files") is precisely what kept it flat:
a single shared rule recolored by one variable cannot express the bold geometry the user
expects.

The per-action **icon is fine and stays**. What needs to change is the banner *design*:
move from a single generated gradient to a **curated set of pre-defined, bold banner
designs** — one per platform area — in the spirit of the reference sheet
(`brainstorm/page_header_banner.jpg`): solid color fields with geometric accents
(chevrons, diagonal cuts, pixel mosaics), strong enough to give each section a recognisable
identity. The banner may be **taller than 60px**. The set is **pre-authored, not generated
at runtime**, selected by the **area being visited**.

This is a revisit/redesign of feature 023, not a new concept. It reuses 023's plumbing —
section-slug resolution (shared with feature 021's `HeaderTheme`), per-action icon mapping,
title relocation into the strip, and the auth/error-layout exclusion — and replaces only
the visual treatment and the asset strategy.

## Approaches Considered

The design was settled across five decisions rather than three monolithic approaches:

### Selection axis — how many designs, chosen how
- **CHOSEN: One design per area (~7–8).** A distinct color + geometric composition per
  section: `dashboard`, `proyectos`, `conocimiento`, `diagnostico`, `personas`,
  `incubadoras`, `auditoria`, plus a `default` fallback. Selected by the area being
  visited, reusing 021/023's existing section-slug resolution. The per-action icon stays
  as the right-side overlay.
- *Rejected — shared style pool recolored per area:* fewer designs to author, but areas
  sharing a style with only a color change drifts back toward the "same shape recolored"
  flatness we are trying to escape.
- *Rejected — per area × action (~28):* too many designs to author and maintain for the
  payoff; the action signal is already carried by the icon.

### Build medium — what the designs are made of
- **CHOSEN: One lightweight SVG asset per area** (~8 files, e.g.
  `wwwroot/img/banners/{slug}.svg`). Most faithful to the bold geometric reference, easy to
  make each area distinct, crisp at any size, recolorable via `currentColor` / CSS vars
  where useful. **This overturns feature 023's FR-015 / SC-007 "no image assets" rule** —
  intentionally, since that rule is the root cause of the flat look. Vector, not raster;
  small (~2–4 KB each); version-controlled.
- *Rejected — pure CSS geometric:* keeps zero files but `clip-path` / repeating-gradient
  compositions are verbose and can't match the reference's richer shapes (pixel mosaics,
  multi-shape) cleanly.
- *Rejected — CSS now, SVG later:* lowest risk but likely never reaches the target
  boldness; defers the real decision.

### Height & content — vertical presence
- **CHOSEN: Compact band, ~88–100px, title only.** A clear step up from 60px so the SVG
  geometry reads and the title feels anchored, while staying a slim strip above content.
  **No subtitle** — avoids authoring per-page description copy and keeps a single source of
  truth for the page title.
- *Rejected — hero band (~120–150px) with subtitle:* strongest identity but more vertical
  cost and requires per-page subtitle copy.
- *Rejected — medium (~100–120px) title only:* viable middle ground, but the compact band
  is enough presence for the slim-strip role.

### Color & title relationship — look + legibility
- **CHOSEN: Color/geometry on one side, dark title on a clean area.** Bold color + SVG art
  occupy roughly the right ~40–50%; the title sits on a clean light area in dark text, like
  the white reference templates with a colored accent panel. Safest contrast — the title is
  always dark-on-light — which preserves 023's WCAG-AA title requirement with the least
  per-design risk.
- *Rejected — bold full-color field with white title:* strongest identity but every
  area's color must independently clear AA contrast for white text.
- *Rejected — solid color block behind the title (white text) bridging right:* bold but
  shifts contrast burden onto every per-area color.

## Decision

Redesign the feature-023 page banner as a **curated set of one pre-authored SVG banner per
platform area**, selected by the area being visited. Each banner is a **compact ~88–100px**
strip with bold per-area color + geometry occupying the right ~40–50%, the **page title on
the left in dark text over a clean light area** (preserving WCAG-AA), and the existing
**per-action Tabler icon** retained on the right. The reused 023/021 plumbing stays:
section-slug resolution, action→icon mapping, title relocation out of the 021 header band,
and the auth/error-layout exclusion. The only substantive changes vs. 023 are: (1) the
visual treatment becomes a designed per-area SVG instead of one parametric gradient, (2)
the "no image assets" constraint is lifted to allow ~8 small per-area SVGs, and (3) the
strip grows from ~60px to ~88–100px.

## Key Requirements

- A pre-defined, version-controlled set of **one banner design per platform area**
  (`dashboard`, `proyectos`, `conocimiento`, `diagnostico`, `personas`, `incubadoras`,
  `auditoria`, `default`), each a distinct color + geometric composition — **not** a single
  shape recolored.
- Each area's design is delivered as **one lightweight SVG asset** (vector, ~2–4 KB),
  authored ahead of time, **not generated at runtime**.
- Banner is selected by the **area being visited**, reusing the existing 021/023
  section-slug resolution; an unrecognised area falls back to the `default` banner.
- Banner height **~88–100px** (taller than the old 60px), **title only** (no subtitle).
- The **page title sits on the left over a clean light area in dark text** and MUST meet
  WCAG-AA contrast; bold color + geometry occupy the right ~40–50%.
- The **per-action icon is retained** on the right (list/add/edit/view + default), unchanged
  in behavior from 023.
- Reuses 023 plumbing unchanged: title relocation out of the 021 header band, the 021 band
  keeping breadcrumb + actions + topbar, and the **auth/error-layout exclusion**.
- All user-facing text in **Spanish**; code/docs in English.
- This **supersedes feature 023's FR-015 / SC-007** (no image assets) — the spec must record
  this as an intentional reversal, not an oversight.

## Open Questions

- Exact final height within the ~88–100px band, and how the SVG art scales to fill it.
- Per-area palette — reuse the exact 021/023 section colors (current `--banner-accent`
  triples) or refresh them for the bolder treatment?
- The specific geometric style assigned to each area (chevron / diagonal / mosaic / slash)
  and whether each is bespoke or drawn from a small shared visual vocabulary.
- SVG delivery mechanism: inline `<svg>`, `<img>`, or CSS `background-image` — and whether
  per-area recoloring is done in the SVG file or via CSS variables.
- Responsive behavior below Bootstrap `md` (<768px): 023 hides the icon and keeps the strip;
  for the richer SVG art, hide the art / simplify to a color band / keep full art?
- Where the SVG art meets the title column — guarantee the title's dark-on-light region
  stays clean so AA contrast holds across every area design.
- Whether any area needs a per-view banner override, or area-derived selection is sufficient
  (carried over from 023 open thread #19).
- Print and reduced-motion behavior for the SVG art (023 hides the strip in print).

## Reference

- `brainstorm/page_header_banner.jpg` — visual reference sheet (12 bold banner templates).
- `brainstorm/15-page-content-banner.md` — original 023 brainstorm (historical).
- Feature 023 artifacts: `specs/023-page-content-banner/` (spec, plan, data-model, tasks).
- Current implementation: `Mentoory.Web/Views/Shared/_PageBanner.cshtml`,
  `.page-banner` block in `Mentoory.Web/wwwroot/css/mentoory.css` (lines ~394–465).
