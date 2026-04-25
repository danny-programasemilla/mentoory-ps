# Contract: Admin Viewer — `/Admin/AuditLog`

**Layer:** Web (`Mentoory.Web.Areas.Administration`)
**Visibility:** GlobalAdmin only
**Files:**
- `Mentoory.Web/Areas/Administration/Controllers/AuditLogController.cs`
- `Mentoory.Web/Areas/Administration/Views/AuditLog/Index.cshtml`
- `Mentoory.Web/wwwroot/js/audit-log.js`
- `Mentoory.Shared.Application/Queries/Audit/GetAuditLogPagedQuery.cs`
- `Mentoory.Shared.Infrastructure/Persistence/Audit/AuditReadRepository.cs`

## Route

`GET /Administration/AuditLog` — renders the DataTable shell.
`GET /Administration/AuditLog/Data` — server-side DataTable data endpoint (JSON).

## Controller surface

```csharp
namespace Mentoory.Web.Areas.Administration.Controllers;

[Area("Administration")]
[Authorize(Roles = "GlobalAdmin")]
public sealed class AuditLogController : BaseController
{
    public AuditLogController(MediatRExecutor executor) : base(executor) { }

    [HttpGet]
    public IActionResult Index();    // renders view

    [HttpGet]
    public Task<IActionResult> Data(/* DataTable server-side params */);    // JSON
}
```

## Query contract

```csharp
public sealed record GetAuditLogPagedQuery(
    int PageNumber,
    int PageSize,
    string? EventType,
    string? UserEmail,
    string? Outcome,
    DateTime? FromUtc,
    DateTime? ToUtc,
    string SortColumn = "OccurredAtUtc",
    bool SortDescending = true) : IBaseRequest<PagedResult<AuditLogDto>>;
```

## UI layout (Tabler + DataTables)

Columns (Spanish labels per Principle IX):
- `Fecha (UTC)` — `OccurredAtUtc` — sortable default DESC
- `Evento` — `EventType` — filterable via dropdown
- `Usuario` — `UserEmail` — filterable via typeahead
- `Acción` — `Action`
- `Resultado` — `Outcome` — filterable via Success/Failure dropdown
- `Rol` — `RoleContext`
- `(expand)` — expandable child row showing `Details` pretty-printed JSON plus secondary fields (`EntityType`, `EntityId`, `CorrelationId`, `ExceptionType`, `IpAddress`, `IncubatorId`, `ProjectId`)

Filter bar (feature-013 pattern): date range (from/to), EventType dropdown, Outcome dropdown, user-email typeahead. No edit/delete actions.

## Spanish strings

All user-facing text in Spanish (Principle IX). Examples:
- Page title: `"Registro de auditoría"`
- Filter button: `"Aplicar filtros"`
- Empty state: `"No hay registros para los filtros seleccionados."`
- Error toast: `"Error al cargar el registro de auditoría."`

## Permissions

- `GlobalAdmin` only (`[Authorize(Roles = "GlobalAdmin")]`).
- No per-incubator scoping — audit is platform-wide and visible only to the global role (Principle X: higher role inherits lower access, but this view is intentionally restricted to GlobalAdmin because it surfaces cross-tenant data).

## Menu

`MenuConfiguration.cs` gains a new item under the existing `"Administration"` group:

```
{ Name = "Registro de auditoría", Url = "/Administration/AuditLog", Icon = "history", Roles = new[] { "GlobalAdmin" } }
```

Per Principle X, GlobalAdmin is the only role in the menu array — this is an exception to the usual rule because the feature itself is restricted to GlobalAdmin; the menu matches the controller's authorization.

## Performance contract (SC-007)

- Initial render for the default filter (last 7 days, sorted by `OccurredAtUtc DESC`) returns 1,000 rows in under 1 second on a modern browser over broadband.
- The query uses `AsNoTracking()` and relies on the existing `IX_AuditLog_*` indexes plus the new `IX_AuditLog_CorrelationId`.
- Server-side pagination caps each round-trip at the DataTable's `pageLength` (default 25; configurable per Principle VII's DataTable conventions).
