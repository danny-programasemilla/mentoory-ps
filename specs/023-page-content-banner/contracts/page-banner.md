# Contract: Page Content Banner Strip (rendered DOM + CSS)

This is the UI contract the integration and E2E tests assert. It defines the rendered DOM shape,
the CSS class contract, and the behavioural guarantees. No HTTP/API contract exists (the feature
adds no endpoints).

## DOM contract

On every authenticated page rendered by `_Layout`, inside `.page-body > .container-xl`, as the
first child and before page content:

```html
<div class="page-banner page-banner--{slug}">
    <h2 class="page-title page-banner__title">{title}</h2>   <!-- omitted entirely if title blank -->
    <i class="ti ti-{icon} page-banner__icon" aria-hidden="true"></i>
</div>
```

Where:
- `{slug}` ∈ { dashboard, proyectos, conocimiento, diagnostico, personas, incubadoras,
  auditoria, default } — from `HeaderTheme.Resolve(area, controller)`.
- `{icon}` ∈ { list, plus, edit, eye, trash, layout-2 } — from `PageBannerIcon.Resolve(action)`.
- `{title}` = `ViewData["Title"] ?? controller`; if blank, the `<h2>` node is not rendered.

### Guarantees (asserted by tests)

1. **C-01 (presence)**: Exactly one `.page-banner` element renders on an authenticated
   main-layout page, and it is inside `.page-body` (not inside `.page-header`).
2. **C-02 (section class)**: The element carries `page-banner--{slug}` matching the section
   resolver for the route (same slug the header band uses).
3. **C-03 (action icon)**: The element contains exactly one `i.page-banner__icon` whose class
   includes `ti-{icon}` for the route's action, and it is `aria-hidden="true"`.
4. **C-04 (single title)**: The page title text appears in the strip; the `.page-header` band
   no longer contains an `h2.page-title`. The title appears exactly once in the document.
5. **C-05 (header band preserved)**: The `.page-header` still renders the breadcrumb
   (`.page-pretitle` / `.breadcrumb`), any `PageActions`, and the `_TopBar`.
6. **C-06 (exclusion)**: Login and error pages (non-main layouts / unauthenticated `else`
   branch) render no `.page-banner` element.
7. **C-07 (decorative icon)**: The icon contains no text, no link, no `tabindex`; it is
   non-interactive (`pointer-events: none`).

## CSS contract

- **height**: `.page-banner` presents a ~60px height band (e.g. `min-height: 60px`) at all
  viewport widths.
- **gradient**: built once in `.page-banner` from `rgba(var(--banner-accent), …)`; each
  `.page-banner--{slug}` only sets `--banner-accent` (the RGB triple). The left region under
  the title stays light enough for ≥ 4.5:1 title contrast (WCAG-AA, FR-016/SC-005).
- **icon**: right-anchored, ~2rem, faint (low opacity), `pointer-events: none`.
- **title**: truncates (`ellipsis`, `nowrap`) and never overlaps the icon (FR-017).
- **responsive**: below `767.98px`, `.page-banner__icon { display: none; }` (icon only); title
  and gradient remain; height band preserved.

## Non-goals (explicitly NOT in the contract)

- No new image assets (`.svg`/`.png`) — gradient + webfont icon only (SC-007).
- No JavaScript.
- No per-view subtitle or per-view icon override (deferred; v1 is route-derived only).
