# Quickstart: Page Content Banner Redesign

## What changed

Feature 023's faint ~60px banner is replaced by a bold ~96px per-area banner. Each platform area
gets its own pre-authored SVG (`wwwroot/img/banners/{slug}.svg`) bound via CSS, with the page
title on a clean light left area (dark text) and the per-action icon retained on the right.

## Run it locally

```bash
dotnet run --project Mentoory.Aspire.AppHost   # or: dotnet run --project Mentoory.Web
```

Then sign in and visit pages across different areas to see each banner:

| Area         | Example route                          |
|--------------|----------------------------------------|
| dashboard    | `/Administration/Dashboard`            |
| proyectos    | `/Administration/Projects`             |
| conocimiento | `/Administration/Knowledge`            |
| diagnostico  | `/Administration/Diagnostics`          |
| personas     | `/Administration/Users`                |
| incubadoras  | `/Administration/Incubators`           |
| auditoria    | `/Administration/AuditLog`             |
| default      | any unmapped controller (e.g. Home)    |

## Verify (acceptance)

- **Bold & distinct (FR-003/006)**: each area shows a solid colour field + geometry on the right;
  two areas differ in colour and/or motif. Compare `dashboard` vs `personas` (same hue, different
  motif) and `conocimiento` vs `incubadoras` (same hue, different motif).
- **Height (FR-012/SC-006)**: the banner is ~96px tall (taller than the old 60px).
- **Title legible (FR-009/SC-005)**: the page title is dark text on the clean left area, readable
  on every area. Quick check: browser devtools contrast on `.page-banner__title`.
- **Icon retained (FR-011)**: list pages show `ti-list`, create `ti-plus`, edit `ti-edit`,
  details `ti-eye`, anything else `ti-layout-2`, faint on the right, `aria-hidden`.
- **Single title (FR-014/SC-008)**: the title appears once (in the banner), not in the header band.
- **Excluded pages (FR-018/SC-009)**: login and error pages show no banner.
- **Responsive (FR-020)**: below 768px the SVG art and icon drop; the colour band + title remain.
- **Missing-asset fallback (FR-021)**: rename one SVG temporarily → the band still shows its colour
  and the title/icon, no broken-image box.

## Re-author an area's banner (no code change)

Overwrite `wwwroot/img/banners/{slug}.svg`. Keep `viewBox="0 0 1200 240"`, `aria-hidden`, and
weight the geometry to the right ~40–50% so the left title region stays clean. The accent colour
and CSS binding live in the `.page-banner--{slug}` rule in `wwwroot/css/mentoory.css`.

## Run the tests

```bash
dotnet test tests/Mentoory.Tests.Integration   # PageBannerRenderTests (contract) + BannerAssetsServedTests (new)
dotnet test tests/Mentoory.Tests.E2E           # PageBannerTests (height band, single title)
```
