using Mentoory.Diagnostic.Application.Commands.AssignFormToStage;
using Mentoory.Diagnostic.Application.Commands.CloneFormTemplate;
using Mentoory.Diagnostic.Application.Commands.RemoveFormFromStage;
using Mentoory.Diagnostic.Application.Commands.UpdateStageQuestionSelection;
using Mentoory.Diagnostic.Application.Queries.GetProjectForm;
using Mentoory.Diagnostic.Application.Queries.GetStageFormAssignment;
using Mentoory.Diagnostic.Application.Queries.ListFormTemplates;
using Mentoory.Diagnostic.Application.Queries.ListProjectForms;
using Mentoory.Diagnostic.Application.Queries.ListStageFormAssignments;
using Mentoory.Shared.Application.DataTables;
using Mentoory.Web.Areas.Coordination.Models;
using Mentoory.Web.Infrastructure;
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

        ModelState.AddModelError(string.Empty, "Error al clonar el formulario diagnóstico.");
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

    [HttpGet("StageConfig/{stageId:long}")]
    public async Task<IActionResult> StageConfig(long stageId, CancellationToken ct)
    {
        var projectId = User.GetActiveProjectId();
        if (!projectId.HasValue)
        {
            TempData["WarningMessage"] = "Debe seleccionar un proyecto antes de continuar.";
            return RedirectToAction("Select", "Context", new { area = string.Empty, returnUrl = Request.Path.Value });
        }

        var assignments = await _executor.SendOrThrowAsync(
            new ListStageFormAssignmentsQuery(stageId), ct);

        await PopulateFormsViewBag(projectId.Value, ct);

        var viewModel = new StageConfigViewModel
        {
            ProjectStageId = stageId,
            Assignments = assignments.Select(a => new AssignedFormViewModel
            {
                ExternalId = a.ExternalId,
                FormName = a.FormName,
                QuestionCount = a.QuestionCount,
                IsActive = a.IsActive,
                CreatedAtUtc = a.CreatedAtUtc,
            }).ToList(),
        };

        return View(viewModel);
    }

    [HttpPost("StageConfig/{stageId:long}/Assign")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignForm(long stageId, [FromForm] Guid formExternalId, CancellationToken ct)
    {
        var projectId = User.GetActiveProjectId();
        var incubatorId = User.GetActiveIncubatorId();
        if (!projectId.HasValue || incubatorId == 0)
        {
            return BadRequest();
        }

        var result = await _executor.SendAndLogIfFailureAsync(
            new AssignFormToStageCommand(projectId.Value, incubatorId, stageId, formExternalId), ct);

        TempData[result.IsSuccess ? "SuccessMessage" : "ErrorMessage"] =
            result.IsSuccess ? "Formulario asignado exitosamente." : "Error al asignar el formulario.";

        return RedirectToAction(nameof(StageConfig), new { stageId });
    }

    [HttpPost("StageConfig/RemoveAssignment/{assignmentExternalId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveAssignment(Guid assignmentExternalId, [FromForm] long stageId, CancellationToken ct)
    {
        var result = await _executor.SendAndLogIfFailureAsync(
            new RemoveFormFromStageCommand(assignmentExternalId), ct);

        TempData[result.IsSuccess ? "SuccessMessage" : "ErrorMessage"] =
            result.IsSuccess ? "Asignación eliminada exitosamente." : "Error al eliminar la asignación.";

        return RedirectToAction(nameof(StageConfig), new { stageId });
    }

    [HttpGet("QuestionSelection/{assignmentExternalId:guid}")]
    public async Task<IActionResult> QuestionSelection(Guid assignmentExternalId, CancellationToken ct)
    {
        var assignment = await _executor.SendOrThrowAsync(
            new GetStageFormAssignmentQuery(assignmentExternalId), ct);

        var selectedIds = assignment.AssignedQuestions
            .Select(aq => aq.QuestionId)
            .ToHashSet();

        var viewModel = new QuestionSelectionViewModel
        {
            AssignmentExternalId = assignment.ExternalId,
            FormName = assignment.FormName,
            Questions = assignment.AllFormQuestions.Select(q => new SelectableQuestionViewModel
            {
                QuestionId = q.QuestionId,
                QuestionText = q.QuestionText,
                QuestionType = q.QuestionType,
                IsSelected = selectedIds.Contains(q.QuestionId),
            }).ToList(),
        };

        return View(viewModel);
    }

    [HttpPost("QuestionSelection/{assignmentExternalId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuestionSelection(Guid assignmentExternalId, [FromForm] List<long> selectedQuestionIds, CancellationToken ct)
    {
        if (selectedQuestionIds.Count == 0)
        {
            TempData["ErrorMessage"] = "Debe seleccionar al menos una pregunta.";
            return RedirectToAction(nameof(QuestionSelection), new { assignmentExternalId });
        }

        var result = await _executor.SendAndLogIfFailureAsync(
            new UpdateStageQuestionSelectionCommand(assignmentExternalId, selectedQuestionIds), ct);

        TempData[result.IsSuccess ? "SuccessMessage" : "ErrorMessage"] =
            result.IsSuccess ? "Selección de preguntas actualizada." : "Error al actualizar la selección.";

        return RedirectToAction(nameof(QuestionSelection), new { assignmentExternalId });
    }

    private async Task PopulateFormsViewBag(long projectId, CancellationToken ct)
    {
        var query = new ListProjectFormsQuery(
            new DataTableRequest(1, 0, 100, null, "asc", null, null), projectId);
        var result = await _executor.SendOrThrowAsync(query, ct);

        ViewBag.AvailableForms = result.Data.Select(f => new FormOptionViewModel
        {
            ExternalId = f.ExternalId,
            Name = f.Name,
        }).ToList();
    }

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
