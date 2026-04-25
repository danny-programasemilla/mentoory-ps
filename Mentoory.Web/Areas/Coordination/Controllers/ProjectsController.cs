using Mentoory.Access.Application.StageActions;
using Mentoory.Shared.Application;
using Mentoory.Shared.Domain.Constants;
using Mentoory.Tenant.Application.Commands.AdvanceProjectStage;
using Mentoory.Tenant.Application.Projects.Queries.GetProjectLifecycle;
using Mentoory.Tenant.Application.Queries.ListProjects;
using Mentoory.Web.Areas.Coordination.Models;
using Mentoory.Web.Infrastructure;
using Mentoory.Web.Models;
using Mentoory.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mentoory.Web.Areas.Coordination.Controllers;

[Area("Coordination")]
[Route("[area]/[controller]")]
[Authorize(Roles = Roles.ProjectCoordinator + "," + Roles.IncubatorAdmin + "," + Roles.GlobalAdmin)]
public class ProjectsController : Controller
{
    private readonly MediatRExecutor _executor;
    private readonly LifecycleMapper _mapper;

    public ProjectsController(MediatRExecutor executor, LifecycleMapper mapper)
    {
        _executor = executor;
        _mapper = mapper;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        if (!User.HasValidIncubatorContext())
        {
            return RedirectToContextSelector(Request.Path.Value);
        }

        return View();
    }

    [HttpPost("[action]")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Data([FromForm] DataTableServerRequest request, CancellationToken ct)
    {
        var incubatorId = User.GetActiveIncubatorId();
        var query = new ListProjectsQuery(request.ToDataTableRequest(), incubatorId);
        var result = await _executor.SendOrThrowAsync(query, ct);

        return Json(new
        {
            draw = result.Draw,
            recordsTotal = result.RecordsTotal,
            recordsFiltered = result.RecordsFiltered,
            data = result.Data,
        });
    }

    [HttpGet("Lifecycle/{externalId:guid}")]
    public async Task<IActionResult> Lifecycle(Guid externalId, CancellationToken ct)
    {
        if (!User.HasValidIncubatorContext() && !User.IsInRole(Roles.GlobalAdmin))
        {
            return RedirectToContextSelector(Request.Path.Value);
        }

        var query = new GetProjectLifecycleQuery(
            externalId,
            User.GetActiveIncubatorId(),
            User.IsInRole(Roles.GlobalAdmin));

        var result = await _executor.SendAndLogIfFailureAsync(query, ct);

        if (result.IsFailure)
        {
            return result.ErrorCode switch
            {
                ResultErrorCodes.ProjectNotFound => NotFound(),
                ResultErrorCodes.ProjectOutOfScope => Forbid(),
                _ => StatusCode(500),
            };
        }

        return View(_mapper.ToViewModel(result.Value!));
    }

    [HttpPost("AdvanceStage/{externalId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdvanceStage(Guid externalId, CancellationToken ct)
    {
        if (!User.HasValidIncubatorContext() && !User.IsInRole(Roles.GlobalAdmin))
        {
            return RedirectToContextSelector(Url.Action(nameof(Lifecycle), new { externalId }));
        }

        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            TempData[TempDataKeys.ErrorMessage] = "No se pudo determinar el usuario que realiza la acción.";
            return RedirectToAction(nameof(Lifecycle), new { externalId });
        }

        var cmd = new AdvanceProjectStageCommand(
            externalId,
            userId.Value,
            User.GetActiveIncubatorId(),
            User.IsInRole(Roles.GlobalAdmin));

        var result = await _executor.SendAndLogIfFailureAsync(cmd, ct);

        if (result.IsSuccess)
        {
            TempData[TempDataKeys.SuccessMessage] =
                $"Proyecto avanzado a {StageTypeDisplay.ToSpanish(result.Value!.NewCurrentStageType)}.";
        }
        else
        {
            TempData[TempDataKeys.ErrorMessage] = ResolveSpanishMessage(result.ErrorCode);
        }

        return RedirectToAction(nameof(Lifecycle), new { externalId });
    }

    private static string ResolveSpanishMessage(ResultErrorCodes? errorCode) => errorCode switch
    {
        ResultErrorCodes.ProjectNotFound =>
            "El proyecto no existe o ya no es accesible.",
        ResultErrorCodes.ProjectOutOfScope =>
            "No tiene permisos para gestionar el ciclo de vida de este proyecto.",
        ResultErrorCodes.ProjectInactive =>
            "El proyecto está inactivo. Active el proyecto antes de avanzar de etapa.",
        ResultErrorCodes.StageNotInProgress =>
            "La etapa actual no está en progreso. Actualice la página.",
        ResultErrorCodes.ProjectAlreadyClosed =>
            "El proyecto ya está en la etapa final (Cierre).",
        ResultErrorCodes.LifecycleConcurrencyConflict =>
            "Otra operación modificó este proyecto. Actualice la página e intente de nuevo.",
        _ => "Error al avanzar la etapa del proyecto.",
    };

    private IActionResult RedirectToContextSelector(string? returnUrl)
    {
        TempData[TempDataKeys.WarningMessage] = "Debe seleccionar una incubadora antes de continuar.";
        return RedirectToAction("Select", "Context", new { area = string.Empty, returnUrl });
    }
}
