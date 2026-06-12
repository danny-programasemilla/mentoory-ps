# Implementation Plan: List/Edit UX Consistency

**Branch**: `025-list-edit-ux-consistency` | **Date**: 2026-06-11 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/025-list-edit-ux-consistency/spec.md`

## Summary

Three UX-consistency fixes, all small and low-risk:

1. **Dropdown status filters (US1)** — Make the status column on 5 list tables render its
   filter as a `<select>` instead of a free-text input. The server already applies these
   filters; the only gap is client-side. Fix via the existing per-view `filters` override
   mechanism in `datatable-helper.js`, sourcing options from three shared constants so the
   change is one mechanism, not five bespoke hacks. No filter-detection/matching logic
   changes → zero regression risk to the already-working Users tables.
2. **Cancel → list (US2)** — One-line change to `Incubators/Edit.cshtml`: Cancel target
   `Details` → `Index`. All other Create/Edit pages already comply (verify-only).
3. **Estado editing (US3)** — Add an `IsActive` toggle to the Incubators Edit form. The
   domain already exposes `Activate()`/`Deactivate()` and `IsActive` is already persisted,
   so this is view-model + command + handler wiring with **no schema/EF change**.

## Technical Context

**Language/Version**: C# / .NET 10.0 (SDK 10.0.0); JavaScript (vanilla, ES5-compatible)

**Primary Dependencies**: ASP.NET Core MVC (Razor views), MediatR 14.1, FluentValidation
12.1, EF Core 10.x, Tabler v1.4.0 (Bootstrap 5), DataTables 2.3.4, jQuery; existing
`datatable-helper.js` filter infrastructure (spec 013)

**Storage**: SQL Server (SSDT/DACPAC). **No schema change** — `Incubator.IsActive` column
already exists and is already mapped.

**Testing**: xUnit (unit/integration), existing Testcontainers MsSql fixture; Playwright
(E2E) available. Integration test for the estado-edit command; targeted E2E/quickstart for
filter dropdowns and cancel navigation.

**Target Platform**: Linux server (ASP.NET Core web app)

**Project Type**: Web application (modular monolith, Areas-based MVC)

**Performance Goals**: No new performance constraints; filtering remains server-side paged
exactly as today.

**Constraints**: `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` (zero warnings); all
user-facing text in Spanish; no `DateTime.UtcNow` (use injected `ITimeProvider`); routes use
`ExternalId`; commands use `IBaseRequest` + FluentValidation.

**Scale/Scope**: 5 list views (filter override), 1 edit view + 1 controller + 1 view-model +
1 command + 1 handler (estado editing), 1 shared JS helper edit (option constants). ~10
files touched.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Constitution rule | Status | Notes |
|---|---|---|
| Spanish UI text | PASS | All new labels/options in Spanish (Todos, Activa/Inactiva, Activo/Inactivo, Desconectado, Sincronización parcial, Estado). |
| No web deps in Domain/Application | PASS | No new domain/app deps; reuse existing `Activate()`/`Deactivate()`. |
| Controllers never inject repositories | PASS | Controller continues to dispatch commands/queries via `MediatRExecutor`. |
| No `DateTime.UtcNow` | PASS | Handler already receives `ITimeProvider`; estado change uses `timeProvider.UtcNow`. |
| ExternalId in routes | PASS | Edit route already keyed on `{externalId:guid}`. |
| Commands: `IBaseRequest`, handler base, FluentValidation | PASS | Extend existing `UpdateIncubatorCommand`/validator; no new pattern. |
| Forbidden (AutoMapper, Dapper-primary, service locator, swallow exceptions) | PASS | None introduced. |
| PostDeployment scripts location / schema management | N/A | No schema change. |
| `[Authorize(Roles=...)]` on controllers | PASS | Existing `IncubatorsController` authorization unchanged. |

No violations. Complexity Tracking not required.

## Project Structure

### Documentation (this feature)

```text
specs/025-list-edit-ux-consistency/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── status-filter-and-estado.md  # UI/command contracts
├── checklists/
│   └── requirements.md  # Spec quality checklist (already created)
└── tasks.md             # Phase 2 output (/speckit-tasks)
```

### Source Code (repository root)

```text
Mentoory.Web/
├── wwwroot/js/
│   └── datatable-helper.js                       # + 3 shared filter-option constants
├── Areas/Platform/Views/Incubators/
│   ├── Index.cshtml                              # + filters override (isActive, fem)
│   └── Edit.cshtml                               # + estado toggle; Cancel → Index
├── Areas/Platform/Views/Templates/
│   └── Diagnostics.cshtml                        # + filters override (isActive, fem)
├── Areas/Administration/Views/Projects/
│   └── Index.cshtml                              # + filters override (isActive, masc)
├── Areas/Coordination/Views/Projects/
│   └── Index.cshtml                              # + filters override (isActive, masc)
├── Areas/Coordination/Views/Diagnostics/
│   └── Index.cshtml                              # + filters override (syncMode)
├── Areas/Platform/Models/
│   └── IncubatorViewModels.cs                    # + IsActive on EditIncubatorViewModel
└── Areas/Platform/Controllers/
    └── IncubatorsController.cs                   # populate + pass IsActive

Mentoory.Tenant.Application/Commands/UpdateIncubator/
├── UpdateIncubatorCommand.cs                     # + bool IsActive
└── UpdateIncubatorHandler.cs                     # apply Activate()/Deactivate()

tests/
└── (integration test for estado change; optional E2E for cancel + filter dropdown)
```

**Structure Decision**: Existing Areas-based MVC modular monolith. The feature edits
existing files only; it adds no new projects, no new endpoints, and no new domain types.

## Implementation Approach (per user story)

### US1 — Dropdown status filters

**Mechanism (shared):** Add three module-level option constants to `datatable-helper.js`:

- `FILTER_OPTIONS_ACTIVE_FEM` → `[{'',Todos},{'true',Activa},{'false',Inactiva}]`
- `FILTER_OPTIONS_ACTIVE_MASC` → `[{'',Todos},{'true',Activo},{'false',Inactivo}]`
- `FILTER_OPTIONS_SYNC_MODE` → `[{'',Todos},{'0',Desconectado},{'1',Sincronización parcial}]`

**Per-view wiring:** each of the 5 `initDataTable(...)` configs gains a `filters` override:

| View | column | options constant | server filter key / parse |
|---|---|---|---|
| Platform/Incubators/Index | `isActive` | FEM | `isActive` → `bool.TryParse` |
| Platform/Templates/Diagnostics | `isActive` | FEM | `isActive` → `bool.TryParse` |
| Administration/Projects/Index | `isActive` | MASC | `isActive` → `bool.TryParse` |
| Coordination/Projects/Index | `isActive` | MASC | `isActive` → `bool.TryParse` |
| Coordination/Diagnostics/Index | `syncMode` | SYNC_MODE | `syncMode` → `(int)SyncMode` |

Option **values** are chosen to match exactly what each handler already parses
(`"true"`/`"false"` for booleans, `"0"`/`"1"` for the SyncMode enum). No server change.

**Why per-view override, not the registry:** The registry matches by render-function name,
but all 5 columns use the generic inline `renderStatus(...)`, and their Spanish labels differ
by gender (Activa vs Activo) and cardinality (SyncMode has its own values). A single
registry entry cannot express these. The per-view `filters` override is the existing,
purpose-built mechanism for exactly this; sourcing options from shared constants keeps it
DRY and satisfies "one mechanism, not table-by-table duplication" (FR-004).

**No regression:** The matching/detection code is untouched, so Administration/Users and
Platform/Users (which use `renderAccountStatus` via the registry) behave identically.

### US2 — Cancel → list

In `Areas/Platform/Views/Incubators/Edit.cshtml`, change the Cancel link from
`asp-action="Details" asp-route-externalId=...` to `asp-action="Index"`. Verify (no change)
that the other Create/Edit pages already point Cancel at their list action.

### US3 — Estado editing for incubators

- `EditIncubatorViewModel`: add `bool IsActive` with `[Display(Name = "Estado")]`.
- `IncubatorsController.Edit` (GET): set `IsActive = incubator.IsActive` (DTO already has it).
- `IncubatorsController.Edit` (POST): pass `model.IsActive` into the command.
- `UpdateIncubatorCommand`: add `bool IsActive`.
- `UpdateIncubatorHandler`: after `incubator.Update(name, description, utcNow)`, apply estado
  via the existing domain methods — `if (request.IsActive) incubator.Activate(utcNow); else
  incubator.Deactivate(utcNow);` (no domain change; methods already exist).
- `Edit.cshtml`: add a Tabler form-switch bound to `IsActive` labeled "Estado" (Activa when
  on, Inactiva when off), placed with the other fields. Cancel discards (already handled by
  not posting).

## Open Questions (resolved during planning)

- **Filter-detection mechanism** → Resolved: per-view `filters` override + shared option
  constants (see US1 rationale).
- **Dropdown labels auto vs declared** → Resolved: declared via shared constants (gender and
  enum differences make auto-derivation incorrect).
- **Deactivation cascade semantics** → Resolved: pure status flag. The domain `Deactivate()`
  only flips `IsActive` + `UpdatedAtUtc`; no cascade exists or is added.

## Complexity Tracking

No constitution violations; section intentionally empty.
