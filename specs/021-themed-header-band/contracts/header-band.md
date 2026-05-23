# Contract: Header Band (UI / asset / resolution)

**Feature**: 021-themed-header-band

This is a UI feature; its "contracts" are the rendered DOM shape, the CSS class↔asset binding (the swap contract), and the route→theme resolution. Downstream code, tests, and any future designer rely on these staying stable.

---

## 1. DOM contract

The band is a single decorative child injected as the **first child** of the existing single `.page-header` in `Views/Shared/_Layout.cshtml`:

```html
<div class="page-header d-print-none">
    <div class="page-header-band header-band--{slug}" aria-hidden="true"></div>
    <div class="container-xl">
        <!-- existing: breadcrumb (page-pretitle), h2.page-title, @PageActions, _TopBar -->
    </div>
</div>
```

Guarantees:
- **Exactly one** `.page-header` exists per rendered page (FR-010). The band does not introduce another header.
- `.page-header-band` carries `aria-hidden="true"`, has no text content, is not a link/button, and is never in the tab order (FR-008).
- `{slug}` is the resolver output (section 3); always present and valid.
- The band is inside the header that already has `d-print-none`, so it is print-suppressed (FR-011).

CSS responsibilities (in `wwwroot/css/mentoory.css`):
- `.page-header { position: relative; overflow: hidden; }`
- `.page-header .container-xl { position: relative; z-index: 1; }`
- `.page-header-band { position: absolute; inset: 0; z-index: 0; pointer-events: none; }`

## 2. Asset + class binding (swap contract — FR-005 / SC-003)

Each theme slug binds, in CSS, to one SVG at a fixed path:

```css
.header-band--dashboard   { background-image: url('/img/headers/dashboard.svg'),   linear-gradient(90deg, transparent 0 45%, <tint> 100%); }
.header-band--proyectos   { background-image: url('/img/headers/proyectos.svg'),   linear-gradient(90deg, transparent 0 45%, <tint> 100%); }
.header-band--conocimiento{ background-image: url('/img/headers/conocimiento.svg'),linear-gradient(90deg, transparent 0 45%, <tint> 100%); }
.header-band--diagnostico { background-image: url('/img/headers/diagnostico.svg'), linear-gradient(90deg, transparent 0 45%, <tint> 100%); }
.header-band--personas    { background-image: url('/img/headers/personas.svg'),    linear-gradient(90deg, transparent 0 45%, <tint> 100%); }
.header-band--incubadoras { background-image: url('/img/headers/incubadoras.svg'), linear-gradient(90deg, transparent 0 45%, <tint> 100%); }
.header-band--auditoria   { background-image: url('/img/headers/auditoria.svg'),   linear-gradient(90deg, transparent 0 45%, <tint> 100%); }
.header-band--default     { background-image: url('/img/headers/default.svg'),     linear-gradient(90deg, transparent 0 45%, <tint> 100%); }
```

Background layering: SVG first (`background-position: right center; background-repeat: no-repeat; background-size: auto 100%`), gradient tint behind it. `<tint>` is a per-theme low-alpha brand color.

**Swap rule**: replacing the file at `/img/headers/{slug}.svg` changes that section's art with **zero code change**. Removing a file degrades to the gradient tint only; the title is never broken (FR-012).

**Asset spec**: SVG, `viewBox="0 0 1200 240"`, abstract/minimal, brand palette (coral `#E07850`, gold `#F5B731`, magenta `#D946A8`, plus neutrals), composed to read when clipped to header height and anchored right. A few KB each.

## 3. Route → theme resolution contract

`Mentoory.Web.Infrastructure.HeaderTheme.Resolve(string? area, string? controller) → string`

- Pure, static, side-effect free; evaluated in `_Layout.cshtml` using the `area`/`controller` already pulled from `RouteData`.
- Returns a slug from the section-2 set; `default` for anything unmapped or null.
- Mapping table: see [data-model.md](../data-model.md#thememapping-resolution-rule).

## 4. Responsive contract (FR-009)

Below the Bootstrap `md` breakpoint, the SVG art layer is suppressed (gradient tint may remain); header text reflows unchanged. The band never overlaps a wrapped title illegibly.

## 5. Test hooks

- `.page-header-band` and `.header-band--{slug}` are the stable selectors integration/E2E tests assert on.
- `HeaderTheme.Resolve` is unit-tested directly (Integration project references `Mentoory.Web`).
