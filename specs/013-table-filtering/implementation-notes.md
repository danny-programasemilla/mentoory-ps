# Implementation Notes: Table Filtering

## Design Decisions

### Decision: Fully JS-Generated Filter Form (Approach A)
- Three approaches were evaluated:
  - **A) Pure JS generation** — auto-generate filter HTML from column definitions (selected)
  - **B) Server-rendered ViewComponent** — Razor-rendered filter form with C# model
  - **C) Hybrid** — JS-generated with server-provided hints
- Chose A because: aligns with zero-config goal, builds on existing `getActiveFilters()`/`filterId` plumbing, and status values are already defined client-side in render functions
- Rejected B: contradicts auto-detection requirement, forces per-view C# declarations
- Rejected C: introduces two patterns (auto vs explicit) with unclear boundaries

### Decision: Collapsible Panel Outside Card (Option A)
- Three placements were evaluated:
  - **A) Between page header and card** (selected)
  - **B) Inside the card, between header and table**
  - **C) Horizontal strip inside table-responsive wrapper**
- Chose A for clean separation of concerns — filtering is a page-level concern, not a table-internal one

### Decision: Explicit Submit Over Live Filtering (Option B)
- Three behaviors were evaluated:
  - **A) Immediate** — each change triggers reload
  - **B) Explicit "Filtrar" button** (selected)
  - **C) Hybrid** — dropdowns immediate, text on Enter
- Chose B to avoid unnecessary server calls and give users explicit control

### Decision: Auto-Detection with Override Capability (Option B)
- Three approaches for filter type definition:
  - **A) Only opt-out** — exclude columns but no type overrides
  - **B) Override and opt-out** (selected)
  - **C) Minimal global exclude list**
- Chose B for maximum flexibility when auto-detection falls short

### Decision: Badge Counter for Active Filter Feedback (Option A)
- Three feedback styles:
  - **A) Badge counter on toggle link** (selected) — "Filtros (3)"
  - **B) Just a "Limpiar" button, no counter**
  - **C) Color change on toggle link**
- Chose A for clear, quantitative feedback

### Decision: URL Persistence (Option A)
- Three approaches:
  - **A) Full URL sync** (selected) — bookmarkable/shareable filtered views
  - **B) No persistence** — filters reset on reload
  - **C) Deferred** — design for it but implement later
- Chose A for collaboration value and bookmark support

## Existing Infrastructure

The following plumbing already exists and should be reused:

- `getActiveFilters(filterId)` in `datatable-helper.js` — collects form input/select values
- `initDataTable()` config accepts `filterId` — sends filters as POST data
- `DataTableServerRequest.Filters` (Dictionary<string, string>) — server receives filter key-value pairs
- `renderAccountStatus()` — maps status strings to display text and colors (source for dropdown options)
- `COLUMN_ICON_MAP` — existing registry pattern for column-to-icon mapping (model for filter registry)
