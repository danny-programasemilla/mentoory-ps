using Mentoory.Access.Application.StageActions;
using Mentoory.Diagnostic.Application.Commands.CloneFormTemplate;
using Mentoory.Diagnostic.Application.Queries.GetProjectForm;
using Mentoory.Diagnostic.Application.Queries.ListFormTemplates;
using Mentoory.Diagnostic.Application.Queries.ListProjectForms;
using Mentoory.Shared.Application;
using Mentoory.Shared.Application.DataTables;
using Mentoory.Web.Areas.Coordination.Models;
using Mentoory.Web.Infrastructure;
using Mentoory.Web.Infrastructure.Filters;
using Mentoory.Web.Models;
using Mentoory.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mentoory.Web.Areas.Coordination.Controllers;

[Area("Coordination")]
[Route("[area]/[controller]")]
[Authorize(Roles = "ProjectCoordinator,Mentor,IncubatorAdmin,GlobalAdmin")]
public class DiagnosticsController : Controller
{
    private readonly MediatRExecutor _executor;

    public DiagnosticsController(MediatRExecutor executor)
    {
        _executor = executor;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        ViewBag.HasProjectContext = User.GetActiveProjectId().HasValue;
        return View();
    }

    [HttpPost("[action]")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Data([FromForm] DataTableServerRequest request, CancellationToken ct)
    {
        var projectId = User.GetActiveProjectId();
        if (!projectId.HasValue)
        {
            return Json(new { draw = 0, recordsTotal = 0, recordsFiltered = 0, data = Array.Empty<object>() });
        }

        var query = new ListProjectFormsQuery(request.ToDataTableRequest(), projectId.Value);
        var result = await _executor.SendOrThrowAsync(query, ct);

        return Json(new
        {
            draw = result.Draw,
            recordsTotal = result.RecordsTotal,
            recordsFiltered = result.RecordsFiltered,
            data = result.Data
        });
    }

    [HttpGet("[action]")]
    [RequiresStage(StageGatedAction.DiagnosticForms)]
    public async Task<IActionResult> Clone(CancellationToken ct)
    {
        if (!User.GetActiveProjectId().HasValue)
        {
            TempData["WarningMessage"] = "Debe seleccionar un proyecto antes de clonar un formulario.";
            return RedirectToAction("Select", "Context", new { area = string.Empty, returnUrl = Request.Path.Value });
        }

        await PopulateTemplatesViewBag(ct);
        return View(new CloneDiagnosticFormViewModel());
    }

    [HttpPost("[action]")]
    [ValidateAntiForgeryToken]
    [RequiresStage(StageGatedAction.DiagnosticForms)]
    public async Task<IActionResult> Clone(CloneDiagnosticFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            await PopulateTemplatesViewBag(ct);
            return View(model);
        }

        var incubatorId = User.GetActiveIncubatorId();
        var cloneProjectId = User.GetActiveProjectId();

        if (incubatorId == 0 || !cloneProjectId.HasValue)
        {
            ModelState.AddModelError(string.Empty, "No se pudo determinar el contexto activo.");
            await PopulateTemplatesViewBag(ct);
            return View(model);
        }

        var result = await _executor.SendAndLogIfFailureAsync(
            new CloneFormTemplateCommand(model.SourceTemplateExternalId, cloneProjectId.Value, incubatorId), ct);

        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = "Formulario diagnóstico clonado exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError(
            string.Empty,
            FirstErrorMessage(result) ?? "Error al clonar el formulario diagnóstico.");
        await PopulateTemplatesViewBag(ct);
        return View(model);
    }

    [HttpGet("{externalId:guid}")]
    public async Task<IActionResult> Details(Guid externalId, CancellationToken ct)
    {
        var detailsProjectId = User.GetActiveProjectId();
        if (!detailsProjectId.HasValue)
        {
            TempData["WarningMessage"] = "Debe seleccionar un proyecto antes de continuar.";
            return RedirectToAction("Select", "Context", new { area = string.Empty, returnUrl = Request.Path.Value });
        }

        var form = await _executor.SendOrThrowAsync(
            new GetProjectFormQuery(externalId, detailsProjectId.Value), ct);

        return View(form);
    }

    private static string? FirstErrorMessage(Result result) =>
        result.ErrorMessages is { Length: > 0 } messages ? messages[0].Message : null;

    private async Task PopulateTemplatesViewBag(CancellationToken ct)
    {
        var query = new ListFormTemplatesQuery(
            new DataTableRequest(1, 0, 100, null, "asc", null, null), null);
        var result = await _executor.SendOrThrowAsync(query, ct);

        ViewBag.Templates = result.Data.Select(t => new FormTemplateOptionViewModel
        {
            ExternalId = t.ExternalId,
            Name = t.Name,
            Description = t.Description,
            Version = t.Version
        }).ToList();
    }
}
