# Phase 1 Data Model: Themed Header Band

**Feature**: 021-themed-header-band

This feature persists nothing. The "data model" is a small in-memory/static registry plus a resolution rule. No database, no EF, no schema.

## Entities (conceptual)

### HeaderTheme

A named visual identity for a group of sections.

| Field | Type | Notes |
|---|---|---|
| `Slug` | string | Stable internal identifier, e.g. `proyectos`. Lowercase, no spaces. Becomes the `header-band--{slug}` CSS modifier. |
| `ImagePath` | string (convention) | `/img/headers/{slug}.svg` — bound in CSS, not stored in code. |

The known slugs: `dashboard`, `proyectos`, `conocimiento`, `diagnostico`, `personas`, `incubadoras`, `auditoria`, `default`.

Invariant: a theme's `Slug` is never null/empty; `default` always exists and is the fallback.

### ThemeMapping (resolution rule)

The function that maps a request's section to a `HeaderTheme.Slug`.

- **Input**: `area` (string?), `controller` (string?) — both from `RouteData`, may be null.
- **Output**: a theme slug (always valid; never null/empty).
- **Rule**: case-insensitive match on `controller` against the table below; no match → `default`.

| Controller(s) | → Slug |
|---|---|
| `Dashboard` | `dashboard` |
| `Projects` | `proyectos` |
| `Knowledge`, `Templates` | `conocimiento` |
| `Diagnostics`, `Diagnostic`, `AnswerCorrection` | `diagnostico` |
| `Users`, `Sponsor`, `BatchUpload` | `personas` |
| `Incubators` | `incubadoras` |
| `AuditLog` | `auditoria` |
| (any other / null) | `default` |

State transitions: none (stateless pure function, evaluated per request render).

## Relationships

- One `HeaderTheme` ⟷ many controllers (a theme represents a group of related sections).
- Exactly one `default` theme catches everything unmapped.

## Validation rules

- Resolver MUST return a non-empty slug for any input, including `(null, null)`.
- Every slug emitted by the resolver MUST have a corresponding `.header-band--{slug}` CSS rule and a `{slug}.svg` asset (enforced by review + the contract; a missing asset degrades to tint-only per FR-012, never a broken header).
