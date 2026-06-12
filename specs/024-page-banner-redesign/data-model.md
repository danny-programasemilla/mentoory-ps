# Data Model: Page Content Banner Redesign

This feature introduces **no domain data, no persistence, and no schema changes**. The only
"model" is the static, curated mapping from a platform area to its banner asset, accent colour,
and geometric motif. The mapping is realised as CSS rules + SVG files, not as runtime data.

## Area → Banner mapping (the curated set)

The area slug is produced by the **existing** `HeaderTheme.Resolve(area, controller)` resolver
(reused unchanged). Each slug binds to one SVG asset and one accent colour. The geometric motif
is chosen so the two shared-hue pairs remain distinguishable (FR-006).

| Area slug      | Asset                          | Accent (RGB / Hex)        | Geometric motif | Notes |
|----------------|--------------------------------|---------------------------|-----------------|-------|
| `dashboard`    | `/img/banners/dashboard.svg`   | 224,120,80 / #E07850      | mosaic          | shares hue with `personas` → distinct motif |
| `proyectos`    | `/img/banners/proyectos.svg`   | 245,183,49 / #F5B731      | chevron         | |
| `conocimiento` | `/img/banners/conocimiento.svg`| 168,82,52 / #A85234       | mosaic          | terracotta (`--mentory-primary-700`); recoloured from magenta 2026-06-11 |
| `diagnostico`  | `/img/banners/diagnostico.svg` | 66,153,225 / #4299E1      | diagonal        | blue (`--mentory-info`) |
| `personas`     | `/img/banners/personas.svg`    | 224,120,80 / #E07850      | chevron         | shares hue with `dashboard` → distinct motif |
| `incubadoras`  | `/img/banners/incubadoras.svg` | 47,179,68 / #2FB344       | diagonal        | green (`--mentory-success`); recoloured from magenta 2026-06-11 |
| `auditoria`    | `/img/banners/auditoria.svg`   | 27,36,52 / #1B2434        | mosaic          | dark/slate identity |
| `default`      | `/img/banners/default.svg`     | 224,120,80 / #E07850      | diagonal        | fallback for any unmapped area |

**Invariants:**
- Exactly 8 assets (7 recognised areas + `default`) — bounded set (SC-011).
- Every slug `HeaderTheme` can return has a matching `.page-banner--{slug}` CSS rule and SVG.
- The colour field is a CSS gradient (always paints); the SVG adds geometry on top, so a missing
  SVG degrades to a coherent coloured band (FR-021).

## Action → icon mapping (UNCHANGED, reused from 023)

Produced by the existing `PageBannerIcon.Resolve(action)` resolver. Listed here for completeness;
**not modified** by this feature.

| Action (case-insensitive) | Icon slug (`ti ti-*`) |
|---------------------------|------------------------|
| Index                     | `list`                 |
| Create                    | `plus`                 |
| Edit                      | `edit`                 |
| Details                   | `eye`                  |
| Delete                    | `trash`                |
| (any other / unmapped)    | `layout-2` (default)   |

## Rendered DOM contract (UNCHANGED, reused from 023)

```html
<div class="page-banner page-banner--{slug}">
  <h2 class="page-title page-banner__title">{title}</h2>   <!-- omitted entirely if title blank -->
  <i class="ti ti-{icon} page-banner__icon" aria-hidden="true"></i>
</div>
```

The redesign changes only the **CSS** bound to these classes and the **SVG** referenced by the
`--banner-art` custom property. No attribute, element, or class changes — preserving the
`PageBannerRenderTests` contract.
