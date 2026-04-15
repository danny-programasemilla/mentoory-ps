using Mentoory.Diagnostic.Application.Queries.GetStageFormNames;
using Mentoory.Tenant.Application.Commands.AddProjectStage;
using Mentoory.Tenant.Application.Commands.AdvanceProjectStage;
using Mentoory.Tenant.Application.Commands.RemoveProjectStage;
using Mentoory.Tenant.Application.Commands.RenameProjectStage;
using Mentoory.Tenant.Application.Commands.ReorderProjectStages;
using Mentoory.Tenant.Application.Queries.GetProjectPipeline;
using Mentoory.Web.Areas.Coordination.Models;
using Mentoory.Web.Infrastructure;
using Mentoory.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mentoory.Web.Areas.Coordination.Controllers;

[Area("Coordination")]
[Route("[area]/[controller]")]
[Authorize(Roles = "ProjectCoordinator,IncubatorAdmin,GlobalAdmin")]
public class ProjectPipelineController : Controller
{
    private readonly MediatRExecutor _executor;

    public ProjectPipelineController(MediatRExecutor executor)
    {
        _executor = executor;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var projectId = User.GetActiveProjectId();
        if (!projectId.HasValue)
        {
            TempData["WarningMessage"] = "Debe seleccionar un proyecto para gestionar las etapas.";
            return RedirectToAction("Select", "Context", new { area = string.Empty, returnUrl = Request.Path.Value });
        }

        var pipelineTask = _executor.SendOrThrowAsync(
            new GetProjectPipelineQuery(projectId.Value), ct);
        var formNamesTask = _executor.SendOrThrowAsync(
            new GetStageFormNamesQuery(projectId.Value), ct);

        await Task.WhenAll(pipelineTask, formNamesTask);

        var pipeline = pipelineTask.Result;
        var formNames = formNamesTask.Result;

        var stageIdToFormNames = new Dictionary<Guid, List<string>>();
        foreach (var stage in pipeline.Stages)
        {
            if (formNames.FormNamesByStageId.TryGetValue(stage.StageId, out var names))
            {
                stageIdToFormNames[stage.ExternalId] = names.ToList();
            }
        }

        var viewModel = new PipelineViewModel
        {
            ProjectExternalId = pipeline.ProjectExternalId,
            ProjectName = pipeline.ProjectName,
            CurrentStageState = pipeline.CurrentStageState,
            Stages = pipeline.Stages.Select(s => new StageViewModel
            {
                ExternalId = s.ExternalId,
                StageType = s.StageType,
                State = s.State,
                Position = s.Position,
                DisplayName = s.DisplayName,
                PlannedStartDate = s.PlannedStartDate,
                PlannedEndDate = s.PlannedEndDate,
                StartedAtUtc = s.StartedAtUtc,
                CompletedAtUtc = s.CompletedAtUtc,
                AssignedFormNames = stageIdToFormNames.GetValueOrDefault(s.ExternalId, []),
            }).ToList(),
        };

        return View(viewModel);
    }

    [HttpPost("[action]")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddStage([FromForm] AddStageViewModel model, CancellationToken ct)
    {
        var projectId = User.GetActiveProjectId();
        if (!projectId.HasValue)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Datos inválidos para agregar la etapa.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _executor.SendAndLogIfFailureAsync(
            new AddProjectStageCommand(projectId.Value, model.StageType, model.Position), ct);

        TempData[result.IsSuccess ? "SuccessMessage" : "ErrorMessage"] =
            result.IsSuccess ? "Etapa agregada exitosamente." : "Error al agregar la etapa.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("[action]/{stageExternalId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveStage(Guid stageExternalId, CancellationToken ct)
    {
        var projectId = User.GetActiveProjectId();
        if (!projectId.HasValue)
        {
            return BadRequest();
        }

        var result = await _executor.SendAndLogIfFailureAsync(
            new RemoveProjectStageCommand(projectId.Value, stageExternalId), ct);

        TempData[result.IsSuccess ? "SuccessMessage" : "ErrorMessage"] =
            result.IsSuccess ? "Etapa eliminada exitosamente." : "Error al eliminar la etapa.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("[action]")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reorder([FromForm] List<Guid> orderedStageIds, CancellationToken ct)
    {
        var projectId = User.GetActiveProjectId();
        if (!projectId.HasValue)
        {
            return BadRequest();
        }

        var result = await _executor.SendAndLogIfFailureAsync(
            new ReorderProjectStagesCommand(projectId.Value, orderedStageIds), ct);

        if (Request.Headers.XRequestedWith == "XMLHttpRequest")
        {
            return result.IsSuccess ? Ok() : BadRequest("Error al reordenar las etapas.");
        }

        TempData[result.IsSuccess ? "SuccessMessage" : "ErrorMessage"] =
            result.IsSuccess ? "Etapas reordenadas exitosamente." : "Error al reordenar las etapas.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("[action]/{stageExternalId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rename(Guid stageExternalId, [FromForm] RenameStageViewModel model, CancellationToken ct)
    {
        var projectId = User.GetActiveProjectId();
        if (!projectId.HasValue)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "El nombre de la etapa es requerido.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _executor.SendAndLogIfFailureAsync(
            new RenameProjectStageCommand(projectId.Value, stageExternalId, model.DisplayName), ct);

        TempData[result.IsSuccess ? "SuccessMessage" : "ErrorMessage"] =
            result.IsSuccess ? "Etapa renombrada exitosamente." : "Error al renombrar la etapa.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("[action]")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Advance(CancellationToken ct)
    {
        var projectId = User.GetActiveProjectId();
        var userId = User.GetUserId();
        if (!projectId.HasValue || !userId.HasValue)
        {
            return BadRequest();
        }

        var result = await _executor.SendAndLogIfFailureAsync(
            new AdvanceProjectStageCommand(projectId.Value, userId.Value), ct);

        TempData[result.IsSuccess ? "SuccessMessage" : "ErrorMessage"] =
            result.IsSuccess ? "Etapa avanzada exitosamente." : "Error al avanzar la etapa.";

        return RedirectToAction(nameof(Index));
    }
}
