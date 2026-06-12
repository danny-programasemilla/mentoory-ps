# Quickstart / Validation Guide: List/Edit UX Consistency

## Prerequisites

- Build clean: `dotnet build` (must succeed with zero warnings — `TreatWarningsAsErrors`).
- Run the app: `dotnet run --project Mentoory.Aspire.AppHost` (or `dotnet run --project
  Mentoory.Web`).
- Sign in with a role that can reach the affected pages (e.g. GlobalAdmin for Platform
  Incubators/Templates and Administration Projects; a coordination role for Coordination
  Projects/Diagnostics).

## Validate US1 — Dropdown status filters

For each of the 5 tables below, open the list, click **Filtros**, and confirm the status
field is a **dropdown** (not a text box) with a "Todos" default:

| URL | Status field | Expected options |
|---|---|---|
| `/Platform/Incubators` | Estado | Todos / Activa / Inactiva |
| `/Platform/Templates/Diagnostics` | Estado | Todos / Activa / Inactiva |
| `/Administration/Projects` | Estado | Todos / Activo / Inactivo |
| `/Coordination/Projects` | Estado | Todos / Activo / Inactivo |
| `/Coordination/Diagnostics` | Sincronización (syncMode) | Todos / Desconectado / Sincronización parcial |

For at least Incubators and Coordination Diagnostics:
1. Select a specific value → **Filtrar** → only matching rows show.
2. Select **Todos** (or **Limpiar**) → all rows return.

**Regression check (must NOT change):** `/Administration/Users` and `/Platform/Users` — the
status filter dropdown still works exactly as before.

## Validate US2 — Cancel → list

1. Go to `/Platform/Incubators`, click the **edit** (pencil) icon on any row.
2. On the Edit page, click **Cancelar**.
3. Expected: you land on `/Platform/Incubators` (the **list**), not the detail page.

Spot-check one other edit/create page (e.g. `/Administration/Projects` → Nueva) and confirm
**Cancelar** still returns to its list (unchanged).

## Validate US3 — Estado editing for incubators

1. Go to `/Platform/Incubators`, edit an **Activa** incubator.
2. Confirm the form shows an **Estado** toggle, currently on (Activa).
3. Flip it to Inactiva, change nothing else, **Guardar Cambios**.
4. Expected: success message; on **Details** the Estado badge reads **Inactiva**.
5. Back on the list, open **Filtros**, select **Inactiva**, **Filtrar** → the incubator
   appears in the filtered results.
6. Edit again, flip back to Activa, save, confirm Details shows **Activa**.
7. Cancel test: edit, flip the toggle, click **Cancelar** → reopen Edit and confirm the
   estado is unchanged (the cancelled change was discarded).

## Automated tests

- **Integration**: `UpdateIncubatorCommand` with `IsActive=false` deactivates an active
  incubator (and `IsActive=true` reactivates), persisting `IsActive` and updating
  `UpdatedAtUtc`, while leaving Name/Description as provided.
- **(Optional) E2E**: Playwright — edit an incubator, toggle estado, save, assert the
  Details badge; and assert Cancel on the Edit page lands on the list.

Run: `dotnet test`
