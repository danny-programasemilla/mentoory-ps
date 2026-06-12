# Research: List/Edit UX Consistency

All "unknowns" were resolved by reading the existing codebase (no external research needed).

## R1: Why are fixed-value status filters showing as free text?

**Decision**: The defect is entirely client-side.

**Findings**:
- `datatable-helper.js` `buildFilterPanel()` auto-detects filter type by matching each
  column's `render.toString()` against `FILTER_TYPE_REGISTRY`. The only registered key is
  `renderAccountStatus`.
- The 5 affected tables render their status column with an inline anonymous function that
  calls the generic `renderStatus('Activa'/'Activo'/'Desconectado'/…)`. None match the
  registry, so they fall through to `filterType = 'text'`.
- A text input cannot produce a valid filter value: typing "Activa" sends
  `filters['isActive'] = 'Activa'`, which fails `bool.TryParse` server-side, so the filter
  silently does nothing. The current behavior is not just ugly — it is non-functional.

**Rationale**: Fixing the control type (text → select) with correct option values restores
working filtering.

## R2: Does the server already support filtering these columns?

**Decision**: Yes — no server changes required.

**Findings** (filter keys and parse logic already present in handlers):

| Table | Handler | Filter key | Server parse |
|---|---|---|---|
| Incubators | `ListIncubatorsHandler` | `isActive` | `bool.TryParse(value)` |
| Admin + Coordination Projects | `ListProjectsHandler` | `isActive` | `bool.TryParse(value)` |
| Templates Diagnostics | `ListFormTemplatesHandler` | `isActive` | `bool.TryParse(value)` |
| Coordination Diagnostics | `ListProjectFormsHandler` | `syncMode` | `(int)f.SyncMode == int.Parse(value)` |

`DataTableServerRequest.Filters` is a `Dictionary<string,string>`; the filter form field
`name` equals the column's `data` field. So the dropdown option `value` must equal what the
handler parses: `"true"`/`"false"` for booleans, `"0"`/`"1"` for `SyncMode`.

`SyncMode` enum (`Mentoory.Diagnostic.Domain/Enums/SyncMode.cs`): `Disconnected = 0`,
`PartialSync = 1`.

## R3: Mechanism — per-view override vs extending the registry

**Decision**: Per-view `filters` override, options sourced from shared constants.

**Rationale**:
- The registry matches by render-function name. All 5 columns share the generic
  `renderStatus(...)`, so the registry cannot tell them apart.
- Labels differ by gender (incubadora/plantilla → "Activa/Inactiva"; proyecto →
  "Activo/Inactivo") and by domain (SyncMode has its own values). A single registry render
  helper cannot express these without per-table data anyway.
- `initDataTable(tableId, { filters: [...] })` already forwards `config.filters` to
  `buildFilterPanel`, which honors `{ column, type:'select', options }`. This is the
  purpose-built extension point.
- Putting the option arrays in three shared module-level constants gives one source of truth
  for values+labels (DRY) while keeping detection/matching code untouched (no Users-table
  regression).

**Alternatives considered**:
- *Extend FILTER_TYPE_REGISTRY with new named renders* (e.g. `renderActiveFem`,
  `renderActiveMasc`, `renderSyncMode`) and replace inline renders. Rejected: more invasive
  (touches render output, not just filters), and still needs three distinct entries — no net
  simplification, higher regression surface.

## R4: How to change estado without a schema change?

**Decision**: Reuse existing domain methods; extend command/view-model only.

**Findings**:
- `Incubator` aggregate already has `Activate(utcNow)` and `Deactivate(utcNow)` and an
  already-persisted `IsActive` column (set to `true` in `Create`).
- `UpdateIncubatorHandler` already injects `ITimeProvider`.
- `GetIncubatorByExternalIdHandler` returns `IncubatorDto` which already includes `IsActive`,
  so the Edit GET can pre-populate the toggle.

**Rationale**: No EF migration, no DACPAC change, no new domain method. The handler calls
`Update(...)` then `Activate()`/`Deactivate()` based on the posted flag.

## R5: Cancel navigation convention

**Decision**: Change only `Incubators/Edit.cshtml` (Details → Index).

**Findings**: A sweep of all `Cancelar` links shows every Create/Edit page already targets
its list action (`Index`, or `Templates` for knowledge templates) except
`Incubators/Edit.cshtml`, which targets `Details`. The convention already holds everywhere
else, so this is a one-line correction plus a verify-only confirmation of the others.
