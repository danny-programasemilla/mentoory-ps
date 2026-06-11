# Research: Page Content Banner Redesign

## R1. How feature 021 binds per-slug SVG art (the pattern to reuse)

**Finding**: `.page-header-band` (021) composes two background layers from CSS custom
properties and swaps art per section via a modifier class:

```css
.page-header-band {
    background-image: var(--band-art, none), var(--band-tint, none);
    background-position: right center;
    background-size: auto 100%;
}
.header-band--proyectos {
    --band-art: url('/img/headers/proyectos.svg');
    --band-tint: linear-gradient(90deg, transparent 0 45%, rgba(245, 183, 49, 0.12) 100%);
}
```

The SVGs live at `/img/headers/{slug}.svg`, sized `viewBox="0 0 1200 240"`, drawn at low
opacity (~0.13) as a faint accent, right-anchored. A missing file simply fails to paint and the
tint remains — the documented graceful-degradation contract.

**Decision**: Feature 024 reuses this exact mechanism for `.page-banner`, with `/img/banners/`
as the directory and a **bold** field instead of a faint tint. The CSS-gradient field (not the
SVG) carries the colour, so a missing SVG still yields a coherent coloured band (satisfies
FR-021 the same way 021 satisfies its FR-012).

## R2. Resolver and markup reuse — what does NOT change

**Finding**: `_Layout.cshtml` already computes `headerTheme = HeaderTheme.Resolve(area,
controller)` and `bannerIcon = PageBannerIcon.Resolve(action)` once per render and passes them
into `_PageBanner.cshtml` as `BannerSlug`/`BannerIcon`/`BannerTitle`. `_PageBanner.cshtml`
renders `<div class="page-banner page-banner--@slug">` + `h2.page-banner__title` +
`i.ti.ti-@icon.page-banner__icon`.

**Decision**: No C# and no Razor changes. The area→slug map (`HeaderTheme`) and action→icon map
(`PageBannerIcon`) are reused verbatim — this is exactly what the spec means by "resolved
consistently with the existing section theming" (FR-004) and "per-action icon unchanged"
(FR-011). The only files that change are `mentoory.css` and the new SVG assets.

## R3. Existing test contract — regression surface

**Finding**:
- `PageBannerRenderTests` (Integration) asserts the **DOM contract**: exactly one `.page-banner`
  inside `.page-body`, the `page-banner--{slug}` modifier, exactly one `aria-hidden`
  non-interactive `page-banner__icon`, and the title rendered once (and absent from the header
  band). It does not assert CSS.
- `PageBannerTests` (E2E) asserts the banner is visible above content, the title appears once,
  and `BoundingBox.Height >= 56`.

**Decision**: The redesign preserves the DOM contract, so `PageBannerRenderTests` stays green
unchanged — it becomes the guard that markup/accessibility didn't regress. The E2E height
assertion `>= 56` still passes at 96px, but is tightened to the new `88–100px` band so SC-006 is
actually enforced rather than merely not-violated.

## R4. Colour & contrast strategy

**Finding**: 023 already guarantees AA title contrast by keeping the gradient transparent under
the left title column. The chosen layout (color/geometry on the right, dark title on a clean
light left area) keeps that guarantee with the least per-design risk — the title never sits over
the bold colour.

**Decision**: Reuse the existing per-area accent triples (021/023 values) for the bold right
field; render the title dark over the transparent left region (unchanged contrast posture). The
retained icon moves from an accent tint (legible over near-white) to a faint **light** watermark
so it reads over the now-bold right field, while staying decorative and `aria-hidden`.

**Per-area accent hues (reused from 021/023):**

| Slug | RGB | Hex |
|------|-----|-----|
| dashboard | 224,120,80 | #E07850 |
| proyectos | 245,183,49 | #F5B731 |
| conocimiento | 217,70,168 | #D946A8 |
| diagnostico | 66,153,225 | #4299E1 |
| personas | 224,120,80 | #E07850 |
| incubadoras | 217,70,168 | #D946A8 |
| auditoria | 27,36,52 | #1B2434 |
| default | 224,120,80 | #E07850 |

Note: `personas`/`dashboard` and `incubadoras`/`conocimiento` share an accent hue in the 021
palette. FR-006 requires two areas be distinguishable in **both** colour and geometry — so the
shared-hue pairs MUST differ by geometric motif (e.g. dashboard=mosaic vs personas=chevron;
conocimiento=mosaic vs incubadoras=diagonal). The geometry carries the distinction where the hue
repeats.

## R5. Geometric vocabulary

**Decision**: A small shared vocabulary keeps authoring bounded (per Clarifications): three
motifs — **chevron** (stacked angled bars), **diagonal** (slashing parallelogram cut), **mosaic**
(stepped pixel blocks). Assign one motif per area such that the two shared-hue pairs above use
different motifs. Each SVG is hand-authored (~2–4 KB), consistent with the 021 header SVGs, but
drawn bolder (higher opacity / solid fills) since the bold colour field is the banner's intent.

## Open items deferred to implementation (non-blocking)

- Exact gradient stop percentages for the right colour field (target: hard edge near 50–56%).
- Exact icon opacity/colour over the bold field (target: white at ~0.22–0.30, tuned for legibility).
- Per-area motif assignment table is finalised in `data-model.md`.
