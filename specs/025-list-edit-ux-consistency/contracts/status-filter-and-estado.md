# Contracts: Status Filter & Estado Editing

These are UI/command contracts (this is a web application; no public API/RPC surface).

## C1: Status filter override (client → existing server contract)

### Contract: per-view `filters` override

Each affected `initDataTable` config declares:

```js
filters: [
  { column: '<dataField>', type: 'select', options: <SHARED_OPTIONS_CONSTANT> }
]
```

- `column` MUST equal the column's `data` field (it becomes the filter form field `name`,
  and thus the `Filters` dictionary key sent to the server).
- `type` MUST be `'select'`.
- `options` MUST be one of the three shared constants.

### Contract: filter value → server (UNCHANGED, must be honored exactly)

The server already expects these exact `Filters[key]` string values:

| Column (`data`) | Server key | Valid non-empty values | Server interpretation |
|---|---|---|---|
| `isActive` | `isActive` | `"true"`, `"false"` | `bool.TryParse` → `WHERE IsActive == value` |
| `syncMode` | `syncMode` | `"0"`, `"1"` | `int.Parse` → `WHERE (int)SyncMode == value` |

- Empty string `""` (the "Todos" option) → key sent empty or omitted → **no filter applied**.
- Any value not in the valid set → server applies no filter (defensive parse). The dropdown
  guarantees only valid values, so this path is not user-reachable.

### Acceptance (maps to FR-001..FR-006, SC-001..SC-003)

- On each of the 5 tables, opening the filter panel shows the status field as a `<select>`
  with the "Todos" default plus the table's valid values (Spanish).
- Selecting a value + "Filtrar" reloads the table filtered to that value.
- Selecting "Todos" (or "Limpiar") shows all rows.
- Administration/Users and Platform/Users status filters are unchanged (registry path
  untouched).

## C2: Cancel navigation (UI contract)

| Page | Cancel target before | Cancel target after |
|---|---|---|
| `Platform/Incubators/Edit` | `Details` (view page) | `Index` (list) |
| All other Create/Edit pages | `Index` / list action | unchanged |

### Acceptance (maps to FR-007, FR-008, SC-004)

- Clicking "Cancelar" on the Incubators Edit page navigates to the Incubators list.
- No other Create/Edit page changes behavior.

## C3: Estado editing (command contract)

### Request (Edit POST form → controller → command)

`EditIncubatorViewModel { ExternalId, Name, Description, IsActive }` →
`UpdateIncubatorCommand(ExternalId, Name, Description, IsActive)`.

### Behavior (handler)

1. Load incubator by `ExternalId`; if not found → failure (existing behavior).
2. `incubator.Update(Name, Description, timeProvider.UtcNow)`.
3. `if (IsActive) incubator.Activate(timeProvider.UtcNow); else incubator.Deactivate(timeProvider.UtcNow);`
4. Persist; return success → redirect to `Details` with success TempData (existing redirect
   semantics for a successful save are retained; only the **Cancel** path changes per C2).

### Acceptance (maps to FR-009..FR-013, SC-005)

- The Edit form shows an "Estado" toggle reflecting the current `IsActive`.
- Saving with the toggle flipped persists the new estado; `Details` and the list reflect it;
  the list status filter finds the incubator under the new estado.
- Saving without touching the toggle leaves estado unchanged.
- Changing only the estado leaves Name/Description unchanged.
- Cancelling discards any unsaved toggle change (nothing persisted).
