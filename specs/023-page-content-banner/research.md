# Research: Page Content Banner Strip

Phase 0 design decisions. All spec requirements have reasonable, low-risk implementations
within the existing `Mentoory.Web` patterns; there are no open NEEDS CLARIFICATION items.

## R1 — Placement & markup home

**Decision**: Render the strip via a new shared partial `_PageBanner.cshtml`, invoked at the
top of `.page-body > .container-xl`, immediately before `@RenderBody()` in `_Layout.cshtml`.

**Rationale**: `.page-body > .container-xl` is the existing content gutter; placing the strip
as its first child makes it "full content width" and "directly above the page content" (FR-001)
without disturbing per-page views. A partial keeps the layout readable and lets the integration
test assert a stable DOM contract. The strip is a sibling *above* `@RenderBody()`, so it
precedes every page's table/filters/forms.

**Alternatives considered**: (a) Inline markup directly in `_Layout` — works but clutters the
layout and is harder to evolve; (b) a ViewComponent — heavier than needed for a stateless,
route-derived fragment; the partial + pure resolver is lighter and matches 021's style.

## R2 — Title relocation & heading semantics

**Decision**: Remove `<h2 class="page-title">@pageTitle</h2>` from the header band block in
`_Layout.cshtml`. Render the title inside the strip as `<h2 class="page-title">` (same element
and class, relocated). The header band retains the breadcrumb (`page-pretitle`), the
`PageActions` section, and `_TopBar`.

**Rationale**: Reusing the existing `h2.page-title` element preserves current typography and
keeps the page's primary heading at the same outline level it has today — satisfying FR-008
(single title) and FR-010 (title is the page's heading) with the smallest possible change and
zero risk of introducing a competing `<h1>`. The `pageTitle` value (`ViewData["Title"] ??
controller`) is computed once in the layout and passed to the partial.

**Alternatives considered**: Promote to `<h1>` — semantically defensible but risks colliding
with content pages that already render their own headings and changes the established outline;
rejected for v1 to keep blast radius minimal (recorded as a possible later refinement).

## R3 — Section colour: reuse the 021 resolver, add strip-scoped accent

**Decision**: Reuse `HeaderTheme.Resolve(area, controller)` for the section slug. The strip
element gets `class="page-banner page-banner--{slug}"`. In CSS, each `.page-banner--{slug}`
rule sets a single custom property `--banner-accent: <r>, <g>, <b>` (the section's RGB triple,
identical to the colours feature 021 already uses for that section). The shared `.page-banner`
rule builds the gradient once from `rgba(var(--banner-accent), …)`.

**Rationale**: Satisfies FR-006/FR-007 — the section→theme *mapping authority* is the existing
resolver (no parallel mapping). The 021 `.header-band--{slug}` rules are left **unchanged** to
avoid any visual regression on the existing band; the strip defines only the bare RGB triple
per slug (not a duplicated gradient), so gradient logic lives in exactly one place
(`.page-banner`). Unmapped controllers resolve to `default`, which has its own accent.

**Alternatives considered**: (a) Refactor the section colour into one shared custom property
consumed by *both* the band and the strip — most DRY, but requires re-deriving 021's exact
per-slug gradients and re-verifying no band regression; deferred to avoid touching working 021
CSS. (b) Inline the gradient per slug in the strip — duplicates gradient logic 8× and would be
flagged by the simplify gate; rejected.

**Section accent palette** (RGB triples, matching 021's tint colours):

| Slug | `--banner-accent` |
|------|-------------------|
| dashboard | 224, 120, 80 |
| proyectos | 245, 183, 49 |
| conocimiento | 217, 70, 168 |
| diagnostico | 66, 153, 225 |
| personas | 224, 120, 80 |
| incubadoras | 217, 70, 168 |
| auditoria | 27, 36, 52 |
| default | 224, 120, 80 |

## R4 — Action → icon resolver

**Decision**: New pure static class `Mentoory.Web.Infrastructure.PageBannerIcon` with
`Resolve(string? action)` returning a Tabler icon **slug** (without the `ti ti-` prefix),
case-insensitive, never null/empty:

| Action (MVC) | Icon slug | Glyph intent |
|--------------|-----------|--------------|
| Index | `list` | browse a list |
| Create | `plus` | add a record |
| Edit | `edit` | modify a record |
| Details | `eye` | view a record |
| Delete | `trash` | remove a record |
| (anything else / null / blank) | `layout-2` | neutral default (FR-005) |

The partial renders `<i class="ti ti-@icon page-banner__icon" aria-hidden="true"></i>`.

**Rationale**: Mirrors `HeaderTheme`'s pure-resolver shape exactly, so it is unit-testable
without a host (the `[Trait("Category","Unit")]` pattern from `HeaderThemeResolveTests`).
Returning the bare slug keeps the `ti ti-` prefix in the view, consistent with every existing
icon usage. `layout-2` is a neutral, non-CRUD-specific Tabler glyph that exists in the bundled
webfont — a safe default that never renders a missing glyph (FR-005). Delete is included
because several controllers expose it; everything outside the set falls through to default.

**Alternatives considered**: Extend `HeaderTheme` with the action map — rejected to keep
section-theme and action-icon concerns in separate single-responsibility resolvers (each with
its own focused unit test). A view-side `switch` in the partial — rejected because it is not
unit-testable in isolation.

## R5 — Faint icon styling & accessibility

**Decision**: The icon is right-anchored, large (~2rem), and faint (low opacity, e.g.
`opacity: 0.18` tinted with the section accent), `aria-hidden="true"`, and `pointer-events:
none`. The title sits on the left over the transparent/low-tint portion of the gradient.

**Rationale**: Satisfies FR-003 (faint watermark), FR-011 (hidden from assistive tech),
FR-016 (the title stays over a near-transparent region for ≥ 4.5:1 contrast — same technique as
021, whose gradient is transparent 0–45% under the left text column). Decorative-only, so no
focus/tab order (mirrors the band's contract).

## R6 — Long title & small-screen behaviour

**Decision**: The title uses single-line truncation (`text-overflow: ellipsis; overflow:
hidden; white-space: nowrap`) within a flex row where the icon has a fixed slot, so the title
never overlaps the icon (FR-017). Below the Bootstrap `md` breakpoint (`max-width: 767.98px`),
`.page-banner__icon { display: none; }` — identical breakpoint and rationale to the 021 band's
responsive rule (Clarification 2026-06-10). The ~60px height band is preserved at all widths.

**Rationale**: Matches the established 021 responsive contract and the spec clarifications;
keeps the strip legible and uncluttered on phones (SC-008).

## R7 — Title-less pages

**Decision**: When `pageTitle` is null/blank, the partial omits the `<h2>` entirely (renders no
title node) but still renders the strip with its section gradient and action/default icon
(FR-012). The strip is never suppressed on an in-scope page for a missing title.

**Rationale**: The layout already falls back `ViewData["Title"] ?? controller`, so a truly
blank title is rare; FR-012 still requires defined, coherent behaviour — render the chrome,
skip the text node (no empty `<h2>`).

## R8 — Scope gating

**Decision**: The strip lives inside the `if (User.Identity?.IsAuthenticated == true)` branch
of `_Layout` (the same branch that renders the header band and page-body). The auth/error
layouts use the `else` branch / different layouts and therefore never render the strip
(FR-013, SC-004) with no extra conditional.

**Rationale**: The existing layout already gates all main-layout chrome on authentication;
placing the strip in that branch makes exclusion automatic — no per-page opt-in/opt-out.

## R9 — Testing strategy (mirror 021)

**Decision**: Three test files mirroring the 021 trio:
- `PageBannerIconResolveTests` — `[Trait("Category","Unit")]`, no host: every action→slug row,
  case-insensitivity, default fallback, never-null/empty.
- `PageBannerRenderTests` — `[Collection(IntegrationTestCollection.Name)]`,
  `[Trait("Category","Integration")]`: authenticated routes render exactly one strip inside
  `.page-body`, with the resolved `page-banner--{slug}` class, the action icon, the title shown
  once (and NOT in the header band), and the icon `aria-hidden`.
- `PageBannerTests` (E2E) — strip visible above content, ~60px tall, title present once.

**Rationale**: Reuses the proven `MentooryWebApplicationFactory` + IncubatorAdmin fixture and
the same route set (`/Administration/Dashboard|Users|Projects`, `/AvailableProjects` for the
default slug) already exercised by `HeaderBandRenderTests`, minimising new fixture work.
