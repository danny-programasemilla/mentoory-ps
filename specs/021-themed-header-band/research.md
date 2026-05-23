# Phase 0 Research: Themed Header Band

**Feature**: 021-themed-header-band
**Date**: 2026-05-23

Resolves the open decisions deferred from the spec (band dimensions, tint model, route→theme table, light-only confirmation) plus the rendering/contrast/test technique choices.

---

## R1 — Rendering technique: how the band attaches to the existing header

**Decision**: Add a single real, decorative child element as the first child of the existing `.page-header` in `_Layout.cshtml`:

```html
<div class="page-header d-print-none">
    <div class="page-header-band header-band--@theme" aria-hidden="true"></div>
    <div class="container-xl"> … existing breadcrumb / title / actions / topbar … </div>
</div>
```

`.page-header` gets `position: relative; overflow: hidden;`. `.page-header-band` is `position: absolute; inset: 0; z-index: 0; pointer-events: none;`. The `.container-xl` content is raised with `position: relative; z-index: 1`.

**Rationale**:
- A real `aria-hidden="true"` element is the same approach already proven by `.auth-decorations` in `mentoory.css` (consistency, FR-008 decorative-only).
- Keeping it inside the existing single `.page-header` guarantees FR-010 (one header) and FR-011 (the header already carries `d-print-none`, so the band inherits print suppression for free).
- No new DOM at page level, no JS — satisfies "no JS to render" / FR-006 static.

**Alternatives considered**:
- CSS `::before` pseudo-element: cannot be a focusable/AT concern anyway, but a real element matches the existing pattern and lets the class drive the `background-image`. Rejected for inconsistency with `.auth-*`.
- Wrapping the whole header in a new banner component: rejected — risks reintroducing structural duplication (FR-010) and is more change than needed.

---

## R2 — Contrast strategy (FR-007 / SC-002): title legible over a full-width band

**Decision**: Concentrate tint + artwork on the **right** of the band and fade to transparent (revealing body background) under the **left** column where the breadcrumb and title live. Two layered backgrounds on `.page-header-band`:

1. A horizontal gradient wash: `linear-gradient(90deg, transparent 0%, transparent 45%, <faint theme tint> 100%)`.
2. The themed SVG art, `background-position: right center`, `background-repeat: no-repeat`, `background-size: auto 100%` (or `cover` clipped right), at low opacity baked into the SVG itself.

**Rationale**:
- The page title (`h2.page-title`, dark heading color) and breadcrumb pretitle sit in the left `col`, over near-body-bg (`#F9FAFB`), so contrast stays well above 4.5:1 regardless of the theme's accent — the "full-width band" reads as full-width but never competes with text. This neutralizes the main risk flagged in the review.
- Art on the right is where the `_TopBar` (avatar/bell) sits; those are icons/avatars, not fine text, so faint art behind them is acceptable.

**Verification**: contrast measured in manual QA (quickstart) across all themes; a representative computed-contrast assertion may be added in E2E.

**Alternatives considered**:
- Uniform low-opacity wash across the whole width: simpler, but every theme's tint must then be tuned per-theme to keep text ≥ 4.5:1, and dark accents (magenta) get risky. Rejected — the right-weighted fade is robust by construction.

---

## R3 — Band dimensions / SVG canvas

**Decision**: The band fills the **natural height of the existing header** — no added height, so FR-013 (no layout shift) holds by construction. SVGs are authored on a wide canvas `viewBox="0 0 1200 240"` (5:1-ish), designed to read correctly when clipped to the header's ~96–130px height and anchored right. Files are vector SVG (crisp at any DPI, a few KB each).

**Rationale**: Reusing the header's own box means zero reserved-space math and zero CLS. A wide canvas anchored right degrades gracefully as the viewport narrows.

**Alternatives considered**: a fixed taller hero band (e.g. 160px) — rejected as "overloaded" and it would add layout height/CLS.

---

## R4 — Tint model: per-theme vs neutral

**Decision**: **Per-theme** faint tint, drawn from each theme's brand accent, applied only in the right-weighted gradient (R2). Defaults and most themes lean on the existing palette (`--mentory-primary` coral, `--mentory-accent-gold`, `--mentory-accent-magenta`, `--mentory-info`).

**Rationale**: Per-theme tint is what makes sections feel distinct (SC-001) at a glance even peripherally; the right-weighting (R2) removes the contrast cost that normally argues for a single neutral wash.

---

## R5 — Route → theme resolution

**Decision**: A pure static resolver `HeaderTheme.Resolve(string? area, string? controller)` in `Mentoory.Web/Infrastructure/`, keyed primarily on **controller name** (case-insensitive), returning a theme slug. Mapping table:

| Controller(s) | Theme slug |
|---|---|
| `Dashboard` | `dashboard` |
| `Projects` (Administration + Coordination) | `proyectos` |
| `Knowledge`, `Templates` | `conocimiento` |
| `Diagnostics`, `Diagnostic`, `AnswerCorrection` | `diagnostico` |
| `Users`, `Sponsor`, `BatchUpload` | `personas` |
| `Incubators` | `incubadoras` |
| `AuditLog` | `auditoria` |
| anything else (`Configuration`, `Home`, …) | `default` |

The resolver always returns a valid slug (never null/empty) → FR-003 + FR-012 (unmapped → `default`).

**Rationale**:
- Controller name is unique and stable enough; area alone is too coarse (Administration spans Dashboard/Projects/Users/Audit). Controller-keyed keeps the "~7 semantic themes" granularity chosen in brainstorm.
- `AnswerCorrection` → `diagnostico` (it corrects diagnostic answers); `BatchUpload` → `personas` (bulk user enrollment) — both grouped by concept, not route.
- A single static table is the one place to edit when a new section wants an identity (quickstart documents this).

**Alternatives considered**: area-keyed (too coarse, rejected in brainstorm); attribute on each controller (more invasive, spreads the mapping across the codebase). Rejected.

---

## R6 — Swap contract (FR-005 / SC-003)

**Decision**: CSS binds each theme class to a fixed file path:

```css
.header-band--dashboard    { background-image: url('/img/headers/dashboard.svg'),    linear-gradient(...); }
.header-band--proyectos    { background-image: url('/img/headers/proyectos.svg'),    linear-gradient(...); }
/* … one rule per theme + default */
```

Files live at `Mentoory.Web/wwwroot/img/headers/{theme}.svg`. Replacing a file changes that section's art with no code change. If a file is absent, the `url()` simply fails to paint and the gradient tint remains — the title is never broken (FR-012).

**Rationale**: The class→URL indirection in CSS is the entire swap contract; the Razor side only emits the class.

---

## R7 — Responsive behaviour (FR-009)

**Decision**: Below the `md` breakpoint, hide the SVG art layer (keep only the faint gradient tint) via media query, so the band never crowds a wrapped title on narrow screens. The header text reflows normally (unchanged).

**Rationale**: On mobile the right zone collapses and the title may wrap; dropping the art removes any overlap risk while keeping a touch of color.

---

## R8 — Light-theme-only confirmation

**Decision**: Confirmed. `_Layout.cshtml` `<body>` carries no `data-bs-theme="dark"`, the app uses the light Tabler build and a light `--tblr-body-bg: #F9FAFB`. Bands are tuned for the light background; dark-mode variants are explicitly out of scope (spec).

---

## R9 — Test strategy

**Decision**: No new test project. Verification spans:

1. **Integration** (`tests/Mentoory.Tests.Integration`, already references `Mentoory.Web`):
   - Pure unit tests of `HeaderTheme.Resolve` covering every mapping-table row + the `default` fallback for an unmapped controller + null/empty inputs.
   - Rendered-HTML assertions (via the existing `WebApplicationFactory` fixture) on a few authenticated routes: exactly one `.page-header`; a `.page-header-band` child carrying the expected `header-band--{theme}` class and `aria-hidden="true"`; default route → `header-band--default`.
2. **E2E** (`tests/Mentoory.Tests.E2E`, Playwright — light touch): one representative authenticated page asserts the band is present, the title is visible, and the band element is not in the keyboard tab order.
3. **Manual QA** (quickstart.md): visual contrast sweep across all themes (≥ 4.5:1 on title/breadcrumb), mobile-viewport crowding check, print-preview suppression, and a file-swap test (SC-003).

**Rationale**: The resolver is the only pure logic → cheap unit tests. Rendering/structure → integration HTML assertions. Contrast/visual/responsive → manual QA + one Playwright check, since pixel-contrast is impractical to assert cheaply in unit/integration. This spec is **not** opted into the prefixed-identifier coverage convention (only 016/018 are), so coverage-check strict traits are not required.

**Alternatives considered**: a dedicated `Mentoory.Web.Tests` project — rejected as scope creep; Integration already reaches the Web assembly.

---

## Constitution touchpoints (resolved)

- **I. Clean Architecture**: feature is presentation-only (Web). No Domain/Application/Infrastructure-persistence changes. The resolver is a view helper, not business logic.
- **VIII. File Organization**: resolver in `Mentoory.Web/Infrastructure/`; CSS in `wwwroot/css/mentoory.css`; assets in `wwwroot/img/headers/`. No JS added.
- **IX. Spanish-First UI**: no user-facing text introduced; theme slugs are internal identifiers.
- **V. Zero-Warnings**: applies at build; nothing in the design forces a warning.
- **Tabler-only components**: SVG background assets + CSS are not an external UI library; compliant with "built-in components only".
