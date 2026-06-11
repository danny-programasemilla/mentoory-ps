# Data Model: Page Content Banner Strip

This feature introduces **no persistent data** — no database tables, EF entities, or domain
objects. The only "model" is a transient, render-time presentation projection derived from the
current request's route values and declared title. Documented here for completeness and to fix
the resolver contracts the tests assert.

## Presentation inputs (read-only, per render)

| Input | Source | Used for |
|-------|--------|----------|
| `area` | `ViewContext.RouteData.Values["area"]` | (passed through to `HeaderTheme.Resolve`; not currently disambiguating) |
| `controller` | `ViewContext.RouteData.Values["controller"]` | section slug via `HeaderTheme.Resolve` |
| `action` | `ViewContext.RouteData.Values["action"]` | action icon via `PageBannerIcon.Resolve` |
| `pageTitle` | `ViewData["Title"] ?? controller` | the strip title (omitted if blank) |

## Derived values

| Derived value | Producer | Type | Invariant |
|---------------|----------|------|-----------|
| `slug` | `HeaderTheme.Resolve(area, controller)` | `string` | non-empty; one of the 021 slug set or `default` |
| `icon` | `PageBannerIcon.Resolve(action)` | `string` | non-empty Tabler icon slug (no `ti ti-` prefix) |
| `title` | `pageTitle` | `string?` | may be null/blank → title node omitted |

These three are computed in `_Layout.cshtml` and passed to `_PageBanner.cshtml` (e.g. via a
small anonymous/tuple model or `ViewData`), keeping the partial free of resolution logic.

## Resolver contract: `PageBannerIcon.Resolve(string? action)`

Pure, static, side-effect free, case-insensitive. Returns a Tabler icon slug; never null or
whitespace.

| Action (case-insensitive) | Returns |
|---------------------------|---------|
| `Index` | `list` |
| `Create` | `plus` |
| `Edit` | `edit` |
| `Details` | `eye` |
| `Delete` | `trash` |
| null / empty / whitespace / any other value | `layout-2` (default) |

## Resolver contract: `HeaderTheme.Resolve(string? area, string? controller)` (reused, unchanged)

Existing feature-021 resolver — see `specs/021-themed-header-band/data-model.md`. Returns the
section slug; `default` for null/blank/unmapped. The strip reuses it verbatim so there is one
section→theme mapping authority (FR-006).

## CSS section-accent map (per slug)

Each `.page-banner--{slug}` sets `--banner-accent` to the section RGB triple (identical to the
colours feature 021 uses for that section). The shared `.page-banner` rule builds the gradient
and faint-icon tint from this single property.

| Slug | `--banner-accent` (R, G, B) |
|------|------------------------------|
| dashboard | 224, 120, 80 |
| proyectos | 245, 183, 49 |
| conocimiento | 217, 70, 168 |
| diagnostico | 66, 153, 225 |
| personas | 224, 120, 80 |
| incubadoras | 217, 70, 168 |
| auditoria | 27, 36, 52 |
| default | 224, 120, 80 |

## State & lifecycle

None. The projection is recomputed on every render from request route values; nothing is
stored, cached, or mutated.
