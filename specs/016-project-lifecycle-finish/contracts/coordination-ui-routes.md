# Contract: Coordination Area UI Routes

**Layer**: Web (`Mentoory.Web.Areas.Coordination`)
**Controller**: `ProjectsController` (NEW)
**Base attributes**:

```csharp
[Area("Coordination")]
[Route("[area]/[controller]")]
[Authorize(Roles = "ProjectCoordinator,IncubatorAdmin,GlobalAdmin")]
```

## Routes

| Method | Path                                                  | Action       | Purpose |
|--------|-------------------------------------------------------|--------------|---------|
| GET    | `/Coordination/Projects`                              | `Index`      | Lists projects in the active incubator context. Uses DataTables server-side pattern consistent with `Administration/Projects`. |
| POST   | `/Coordination/Projects/Data`                         | `Data`       | DataTables AJAX endpoint for the index list. Antiforgery-protected. |
| GET    | `/Coordination/Projects/Lifecycle/{externalId:guid}`  | `Lifecycle`  | Lifecycle overview page. Renders `LifecycleProjectViewModel`. Returns `NotFound` on `ProjectNotFound`; redirects to context selector on missing incubator context. |
| POST   | `/Coordination/Projects/AdvanceStage/{externalId:guid}` | `AdvanceStage` | Executes `AdvanceProjectStageCommand`. Antiforgery-protected. Redirects back to `Lifecycle` with a success or error toast in `TempData`. |

## Context & scope requirements

- Every action begins with `User.HasValidIncubatorContext()` check. If false, redirect to `/Context/Select` with `TempData["WarningMessage"] = "Debe seleccionar una incubadora antes de continuar."` — pattern from existing `AdministrationProjectsController`.
- `GlobalAdmin` operating in global scope (`ActiveIncubatorId == 0`) may still view any project's lifecycle (read). Advancement requires the user to be operating within the project's incubator context, or to be `GlobalAdmin` in which case the backend check short-circuits (command field `ActingUserIsGlobalAdmin = true`).

## `Index` action

```csharp
[HttpGet("")]
public IActionResult Index()
{
    if (!User.HasValidIncubatorContext())
    {
        TempData["WarningMessage"] =
            "Debe seleccionar una incubadora antes de continuar.";
        return RedirectToAction("Select", "Context",
            new { area = string.Empty, returnUrl = Request.Path.Value });
    }
    return View();  // DataTables-driven Index.cshtml
}
```

View: `Areas/Coordination/Views/Projects/Index.cshtml`. Columns: Nombre, Descripción (truncada), Etapa Actual (Spanish), Estado, Acciones (link → Lifecycle).

## `Data` action

Reuses `ListProjectsQuery` from `Mentoory.Tenant.Application.Queries.ListProjects` with the incubator context filter. Returns DataTables JSON:

```csharp
[HttpPost("[action]")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Data([FromForm] DataTableServerRequest request, CancellationToken ct);
```

## `Lifecycle` action

```csharp
[HttpGet("Lifecycle/{externalId:guid}")]
public async Task<IActionResult> Lifecycle(Guid externalId, CancellationToken ct)
{
    if (!User.HasValidIncubatorContext() && !User.IsInRole("GlobalAdmin"))
    {
        TempData["WarningMessage"] =
            "Debe seleccionar una incubadora antes de continuar.";
        return RedirectToAction("Select", "Context",
            new { area = string.Empty, returnUrl = Request.Path.Value });
    }

    var query = new GetProjectLifecycleQuery(
        externalId,
        User.GetActiveIncubatorIdOrZero(),
        User.IsInRole("GlobalAdmin"));

    var result = await _executor.SendAndLogIfFailureAsync(query, ct);

    if (result.IsFailure)
    {
        return result.ErrorCode switch
        {
            "ProjectNotFound"    => NotFound(),
            "ProjectOutOfScope"  => Forbid(),
            _                    => StatusCode(500)
        };
    }

    var viewModel = _mapper.ToViewModel(result.Value);  // Mapperly
    return View(viewModel);
}
```

View: `Areas/Coordination/Views/Projects/Lifecycle.cshtml`. Structure:

1. Page header: project name + Spanish current-stage badge.
2. 7-stage timeline (horizontal on desktop, vertical on mobile) with color-coded state, timestamps in `dd/MM/yyyy HH:mm`.
3. **Advance Stage** button — shown iff `Model.CanAdvance`; opens confirmation modal; form POSTs to `AdvanceStage/{externalId}`. Button hidden when `!CanAdvance`; the reason (Spanish) is rendered as a muted help text beneath the timeline.
4. Actions grid — cards with `Available` / `Locked` / `Past` states per research R3. Locked cards include `aria-disabled="true"` + Bootstrap tooltip.

## `AdvanceStage` action

```csharp
[HttpPost("AdvanceStage/{externalId:guid}")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> AdvanceStage(Guid externalId, CancellationToken ct)
{
    if (!User.HasValidIncubatorContext() && !User.IsInRole("GlobalAdmin"))
    {
        TempData["WarningMessage"] =
            "Debe seleccionar una incubadora antes de continuar.";
        return RedirectToAction("Select", "Context",
            new { area = string.Empty, returnUrl = Url.Action(nameof(Lifecycle), new { externalId }) });
    }

    var userId = User.GetActiveUserId();
    var cmd = new AdvanceProjectStageCommand(
        externalId,
        userId,
        User.GetActiveIncubatorIdOrZero(),
        User.IsInRole("GlobalAdmin"));

    var result = await _executor.SendAndLogIfFailureAsync(cmd, ct);

    if (result.IsSuccess)
    {
        var newStageDisplay = StageTypeDisplay.ToSpanish(result.Value.NewCurrentStageType);
        TempData["SuccessMessage"] =
            $"Proyecto avanzado a {newStageDisplay}.";
    }
    else
    {
        TempData["ErrorMessage"] = ResolveSpanishMessage(result.ErrorCode);
    }

    return RedirectToAction(nameof(Lifecycle), new { externalId });
}
```

`ResolveSpanishMessage` maps the failure codes from `advance-project-stage-command.md` to Spanish toast text.

## Menu integration

`Mentoory.Web/Infrastructure/Menu/MenuConfiguration.cs` — add to the existing Coordinación group:

```csharp
new MenuGroup("Coordinación", "ti ti-list-check",
    new[] { "ProjectCoordinator", "Mentor", "IncubatorAdmin", "GlobalAdmin" },
    new MenuItem[]
    {
        new("Proyectos", "/Coordination/Projects", "ti ti-sitemap"),   // NEW — first item
        new("Diagnósticos", "/Coordination/Diagnostics", "ti ti-clipboard-check"),
    }),
```

## `RequiresStageAttribute` (action filter)

Applied to stage-gated coordination action methods. Declarative API:

```csharp
[RequiresStage(StageGatedAction.AnswerCorrection)]
public async Task<IActionResult> Correct(...) { ... }
```

### Filter behavior

1. Resolves the target project id from the request. Supported sources, checked in order:
   - Route value `projectExternalId` (preferred; new stage-gated endpoints should adopt this naming).
   - `User.GetActiveProjectId()` claim (for controllers that operate on the session-active project, e.g., `DiagnosticsController`).
2. If no project id can be resolved → returns `RedirectToAction` to `/Coordination/Projects` with a Spanish error toast `"No se pudo determinar el proyecto para validar la etapa."`.
3. Loads the project's `CurrentStageType` via a lightweight `GetProjectCurrentStageQuery` (or caches it per request if already loaded by a prior filter).
4. Looks up `StageActionRegistry.GetState(currentStage, attribute.Action)`.
5. If state is not `Available` → short-circuits with redirect to `Lifecycle/{externalId}` and a Spanish toast: `"Esta acción estará disponible desde la etapa {gatingStageDisplay}."`.
6. Otherwise → invokes the next filter / action normally.

### Initial application (per research R6)

- `Mentoory.Web.Areas.Coordination.Controllers.AnswerCorrectionController` — class-level or action-level `[RequiresStage(StageGatedAction.AnswerCorrection)]`.
- `Mentoory.Web.Areas.Coordination.Controllers.DiagnosticsController.Clone` and `.Configure` actions — `[RequiresStage(StageGatedAction.DiagnosticForms)]`.

## Antiforgery

Every `POST` includes `@Html.AntiForgeryToken()` in the rendering form, and the action has `[ValidateAntiForgeryToken]`. Non-negotiable (platform default).

## JavaScript

`wwwroot/js/coordination-lifecycle.js`:

- Wires the "Avanzar Etapa" button to the Bootstrap modal.
- Enhances locked action cards with tooltips (Tabler ships tooltips out of the box; this file initializes `bootstrap.Tooltip` on `[data-bs-toggle="tooltip"]` selectors for this view).

No jQuery-only code; vanilla ES-compatible with the existing wwwroot conventions.
