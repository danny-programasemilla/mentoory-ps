# Mentoory Web Layer Patterns

## Controller Patterns

### Area and Routing Convention

Controllers use attribute routing with Area-based organization by user role.

```csharp
[Area("Platform")]
[Route("[area]/[controller]")]
[Authorize(Roles = "GlobalAdmin")]
public class IncubatorsController : Controller
{
    private readonly MediatRExecutor _executor;

    public IncubatorsController(MediatRExecutor executor)
    {
        _executor = executor;
    }

    [HttpGet("")]                                    // /Platform/Incubators
    public IActionResult Index() { }

    [HttpPost("[action]")]                           // /Platform/Incubators/Data
    public Task<IActionResult> Data() { }

    [HttpGet("[action]")]                            // /Platform/Incubators/Create
    public IActionResult Create() { }

    [HttpPost("[action]")]                           // /Platform/Incubators/Create (POST)
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Create(CreateIncubatorViewModel model) { }

    [HttpGet("{externalId:guid}")]                   // /Platform/Incubators/{guid}
    public Task<IActionResult> Details(Guid externalId) { }

    [HttpGet("{externalId:guid}/[action]")]          // /Platform/Incubators/{guid}/Edit
    public Task<IActionResult> Edit(Guid externalId) { }

    [HttpPost("{externalId:guid}/[action]")]         // /Platform/Incubators/{guid}/Edit (POST)
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Edit(Guid externalId, EditIncubatorViewModel model) { }
}
```

**Rules:**
- Every action MUST have an explicit route template (`""`, `"[action]"`, or custom)
- Never use `[HttpGet]`/`[HttpPost]` without a template on an attribute-routed controller
- All POST actions require `[ValidateAntiForgeryToken]`
- Controllers inherit from `Controller` directly
- Inject `MediatRExecutor` — never inject repositories or `IMediator` directly

### Areas by Role

| Area | Role | Purpose |
|------|------|---------|
| Identity | (unauthenticated) | Login, register, password reset, email verification |
| Platform | GlobalAdmin | Incubators, users, subscriptions, templates |
| Administration | IncubatorAdmin | Projects, users, enrollment |
| Coordination | ProjectCoordinator | Diagnostics, knowledge, lifecycle, answer correction |
| Participant | Entrepreneur | Diagnostic form filling, submissions |
| Mentoring | Mentor | Plans, sessions, assignments |
| Sponsor | Sponsor | Read-only dashboards |

### MediatRExecutor Usage

```csharp
// Queries — throws on failure, returns typed result
var incubator = await _executor.SendOrThrowAsync(
    new GetIncubatorByExternalIdQuery(externalId), ct);

// Commands — returns Result, caller handles success/failure
var result = await _executor.SendAndLogIfFailureAsync(
    new CreateIncubatorCommand(model.Name, model.Description), ct);

if (result.IsSuccess)
{
    TempData["SuccessMessage"] = "Incubadora creada exitosamente.";
    return RedirectToAction(nameof(Index));
}

// Map errors to ModelState
if (result.ErrorMessages is not null)
{
    foreach (var (context, message) in result.ErrorMessages)
        ModelState.AddModelError(context, message);
}
return View(model);
```

### DataTable Server-Side Pattern

Controller action:
```csharp
[HttpPost("[action]")]
public async Task<IActionResult> Data([FromForm] DataTableServerRequest request, CancellationToken ct)
{
    var query = new ListIncubatorsQuery(request.ToDataTableRequest());
    var result = await _executor.SendOrThrowAsync(query, ct);

    return Json(new
    {
        draw = result.Draw,
        recordsTotal = result.RecordsTotal,
        recordsFiltered = result.RecordsFiltered,
        data = result.Data
    });
}
```

View (uses `datatable-helper.js`):
```html
<table id="incubatorsTable" class="table table-striped table-hover w-100">
    <thead>
        <tr>
            <th>Nombre</th>
            <th>Estado</th>
            <th>Acciones</th>
        </tr>
    </thead>
</table>

<form id="antiForgeryForm">@Html.AntiForgeryToken()</form>

@section Scripts {
    <script src="~/js/datatable-helper.js"></script>
    <script>
        document.addEventListener('DOMContentLoaded', function () {
            initDataTable('incubatorsTable', {
                apiUrl: '@Url.Action("Data", "Incubators", new { area = "Platform" })',
                columns: [
                    { data: 'name' },
                    {
                        data: 'isActive',
                        render: function (data) {
                            return data
                                ? '<span class="badge bg-success">Activa</span>'
                                : '<span class="badge bg-secondary">Inactiva</span>';
                        }
                    },
                    {
                        data: 'externalId',
                        orderable: false,
                        render: function (data) {
                            var placeholder = '00000000-0000-0000-0000-000000000000';
                            var url = '@Url.Action("Details", "Incubators", new { area = "Platform", externalId = System.Guid.Empty })'.replace(placeholder, data);
                            return '<a href="' + url + '" class="btn btn-sm btn-outline-primary"><i class="fas fa-eye"></i></a>';
                        }
                    }
                ],
                defaultOrder: [[1, 'desc']]
            });
        });
    </script>
}
```

**DataTable URL placeholder rule**: Always use `System.Guid.Empty` as placeholder — string placeholders like `"__ID__"` fail the `:guid` route constraint during URL generation.

## View Patterns

### Area View Setup

Each Area needs `_ViewImports.cshtml` and `_ViewStart.cshtml` in its `Views/` folder:

```html
<!-- Views/_ViewImports.cshtml -->
@using Mentoory.Web
@using Mentoory.Web.Models
@using Mentoory.Web.Areas.Platform.Models
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers

<!-- Views/_ViewStart.cshtml -->
@{ Layout = "~/Views/Shared/_Layout.cshtml"; }
```

### Page Layout Convention

```html
@model CreateIncubatorViewModel
@{
    ViewData["Title"] = "Nueva Incubadora";
}

<div class="d-flex justify-content-between align-items-center mb-4">
    <h2>Nueva Incubadora</h2>
    <a asp-action="Index" class="btn btn-outline-secondary">
        <i class="fas fa-arrow-left me-1"></i>Volver
    </a>
</div>
```

### Success/Error Messages via TempData

```csharp
// Set in controller
TempData["SuccessMessage"] = "Incubadora creada exitosamente.";
```

```html
<!-- Display in view -->
@if (TempData["SuccessMessage"] is string successMessage)
{
    <div class="alert alert-success alert-dismissible fade show" role="alert">
        @successMessage
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Cerrar"></button>
    </div>
}
```

### Form Pattern

```html
<div class="card shadow-sm">
    <div class="card-body">
        <form asp-action="Create" method="post">
            @Html.AntiForgeryToken()
            <div asp-validation-summary="All" class="text-danger mb-3"></div>

            <div class="mb-3">
                <label asp-for="Name" class="form-label"></label>
                <input asp-for="Name" class="form-control" placeholder="Nombre..." />
                <span asp-validation-for="Name" class="text-danger"></span>
            </div>

            <div class="d-flex justify-content-end gap-2">
                <a asp-action="Index" class="btn btn-secondary">Cancelar</a>
                <button type="submit" class="btn btn-primary">
                    <i class="fas fa-save me-1"></i>Crear
                </button>
            </div>
        </form>
    </div>
</div>

@section Scripts {
    <partial name="_ValidationScriptsPartial" />
}
```

## ViewModel Patterns

```csharp
public sealed class CreateIncubatorViewModel
{
    [Required(ErrorMessage = "El nombre es requerido.")]
    [StringLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres.")]
    [Display(Name = "Nombre")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres.")]
    [Display(Name = "Descripción")]
    public string? Description { get; set; }
}
```

**Rules:**
- ViewModels are `sealed class` with DataAnnotation validation
- Error messages in Spanish
- `string.Empty` default for required strings (avoids nullable warnings)
- `Display` attribute for Spanish labels
- ViewModels live in `Areas/{Area}/Models/`

## JavaScript Patterns

All JS files live in `/wwwroot/js/` (never inside Views folders).

### Global Utilities (`site.js`)
- `showToast(message, type)` — Bootstrap toast notification (success, danger, warning, info)
- `getAntiForgeryToken()` — reads `__RequestVerificationToken` from form/meta tag

### DataTable Helper (`datatable-helper.js`)
- `initDataTable(tableId, config)` — server-side DataTable with Spanish locale
- `getActiveFilters(filterId)` — collects filter form values

### Form Helper (`form-helper.js`)
- `submitForm(formElement, options)` — AJAX form submission with toast feedback

### Context Switcher (`context-switcher.js`)
- Top-bar AJAX context switching

### Per-Page JS
- Named as `{area}-{feature}.js` when needed
- Loaded via `@section Scripts { }` in views

## Security Patterns

### CSRF Protection
- Forms: `@Html.AntiForgeryToken()` inside `<form>` tag
- AJAX: `getAntiForgeryToken()` sends token via `RequestVerificationToken` header
- Controller: `[ValidateAntiForgeryToken]` on all POST actions

### Rate Limiting (public endpoints)
- `login`: 5 requests / 15 min per IP
- `registration`: 3 requests / 15 min per IP
- `password-reset`: 3 requests / 60 min per IP

### Session Context
Claims available in authenticated controllers:
- `User.FindFirst("UserId")` — internal user ID
- `User.FindFirst("ActiveIncubatorId")` — current incubator context
- `User.FindFirst("ActiveProjectId")` — current project context
- `User.FindFirst("ActiveRole")` — current role

## Phoenix Admin Template

### Policy
Use only Phoenix Admin Template built-in components. Never add external UI libraries.

### Components Used
- Bootstrap 5 card layout (`.card.shadow-sm`)
- DataTables with server-side processing
- Bootstrap modals for confirmations and corrections
- Bootstrap toasts for notifications
- FontAwesome icons (`fas fa-*`)
- Responsive tables (`.table-responsive`)
- Accordion for expandable content (`.accordion`)
- Badges for status indicators (`.badge.bg-success`, `.badge.bg-secondary`)

### Forbidden
- SweetAlert2 or other notification libraries
- External wizard libraries
- Any UI dependency not included in Phoenix
