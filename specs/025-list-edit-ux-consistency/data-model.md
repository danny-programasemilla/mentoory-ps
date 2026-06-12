# Data Model: List/Edit UX Consistency

**No database schema changes.** This feature touches view models, a command DTO, and
client-side filter option data only. The persistent model is unchanged.

## Persistent entities (unchanged)

### Incubator (`Mentoory.Tenant.Domain/Aggregates/Incubator/Incubator.cs`)

Already has everything needed:

| Field | Type | Notes |
|---|---|---|
| `ExternalId` | `Guid` | Route key (unchanged). |
| `Name` | `string` | Editable (existing). |
| `Description` | `string?` | Editable (existing). |
| `IsActive` | `bool` | **Now editable via the Edit form.** Already persisted; `Create` defaults it to `true`. |
| `UpdatedAtUtc` | `DateTime` | Set via `ITimeProvider` on any mutation. |

Domain methods reused (no change): `Update(name, description, utcNow)`,
`Activate(utcNow)`, `Deactivate(utcNow)`.

## Application DTOs

### UpdateIncubatorCommand (modified)

```
record UpdateIncubatorCommand(Guid ExternalId, string Name, string? Description, bool IsActive)
```

- Add `bool IsActive`.
- `UpdateIncubatorValidator` unchanged (no new validation rule needed — a bool is always
  valid; existing `ExternalId`/`Name` rules remain).

### IncubatorDto (unchanged)

Already exposes `IsActive`; used by the Edit GET to pre-populate the toggle and by the list.

## View models

### EditIncubatorViewModel (modified — `Areas/Platform/Models/IncubatorViewModels.cs`)

| Field | Type | Annotation |
|---|---|---|
| `ExternalId` | `Guid` | (existing) |
| `Name` | `string` | (existing) `[Required]`, `[StringLength(200)]`, `[Display(Name="Nombre")]` |
| `Description` | `string?` | (existing) `[StringLength(500)]`, `[Display(Name="Descripción")]` |
| `IsActive` | `bool` | **new** — `[Display(Name="Estado")]`. Bound by a Tabler form-switch. |

## Client-side filter option model (`datatable-helper.js`)

Three module-level constants (arrays of `{ value, label }`), used as `options` in per-view
`filters` overrides:

| Constant | Options (`value` → `label`) |
|---|---|
| `FILTER_OPTIONS_ACTIVE_FEM` | `'' → Todos`, `'true' → Activa`, `'false' → Inactiva` |
| `FILTER_OPTIONS_ACTIVE_MASC` | `'' → Todos`, `'true' → Activo`, `'false' → Inactivo` |
| `FILTER_OPTIONS_SYNC_MODE` | `'' → Todos`, `'0' → Desconectado`, `'1' → Sincronización parcial` |

`value` strings are exactly what the corresponding server handler parses. `''` is the
"all"/no-filter default.

## State transitions

Incubator estado: `Activa (IsActive=true) ⇄ Inactiva (IsActive=false)`, toggled freely from
the Edit form. No guarded transitions, no cascade — a pure status flag.
