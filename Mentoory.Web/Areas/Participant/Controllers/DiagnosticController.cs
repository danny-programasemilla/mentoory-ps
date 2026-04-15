using Mentoory.Diagnostic.Application.Commands.SubmitDiagnosticResponse;
using Mentoory.Diagnostic.Application.Queries.GetEntrepreneurDiagnosticStatus;
using Mentoory.Diagnostic.Application.Queries.GetStageFormAssignment;
using Mentoory.Web.Infrastructure;
using Mentoory.Web.Areas.Participant.Models;
using Mentoory.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mentoory.Web.Areas.Participant.Controllers;

[Area("Participant")]
[Route("[area]/[controller]")]
[Authorize(Roles = "Entrepreneur,Mentor,ProjectCoordinator,IncubatorAdmin,GlobalAdmin")]
public class DiagnosticController : Controller
{
    private readonly MediatRExecutor _executor;

    public DiagnosticController(MediatRExecutor executor)
    {
        _executor = executor;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var projectId = User.GetActiveProjectId();
        var userId = User.GetUserId();

        if (!projectId.HasValue || !userId.HasValue)
        {
            TempData["WarningMessage"] = "Debe seleccionar un proyecto antes de continuar.";
            return RedirectToAction("Select", "Context", new { area = string.Empty, returnUrl = Request.Path.Value });
        }

        var statuses = await _executor.SendOrThrowAsync(
            new GetEntrepreneurDiagnosticStatusQuery(projectId.Value, userId.Value), ct);

        var viewModel = new DiagnosticLandingViewModel
        {
            Assignments = statuses.Select(s => new DiagnosticStageFormViewModel
            {
                AssignmentExternalId = s.AssignmentExternalId,
                ProjectStageId = s.ProjectStageId,
                FormName = s.FormName,
                QuestionCount = s.QuestionCount,
                IsCompleted = s.IsCompleted,
                CompletedAtUtc = s.CompletedAtUtc,
            }).ToList(),
        };

        return View(viewModel);
    }

    [HttpGet("Fill/{assignmentExternalId:guid}")]
    public async Task<IActionResult> Fill(Guid assignmentExternalId, CancellationToken ct)
    {
        var assignment = await _executor.SendOrThrowAsync(
            new GetStageFormAssignmentQuery(assignmentExternalId), ct);

        var model = new DiagnosticFormViewModel
        {
            FormExternalId = assignment.FormExternalId,
            FormName = assignment.FormName,
            StageFormAssignmentExternalId = assignment.ExternalId,
            Questions = assignment.AssignedQuestions.Select(q => new QuestionViewModel
            {
                QuestionId = q.QuestionId,
                QuestionText = q.QuestionText,
                QuestionType = q.QuestionType,
            }).ToList(),
        };

        return View(model);
    }

    [HttpPost("[action]")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(SubmitDiagnosticViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToAction(nameof(Fill), new { assignmentExternalId = model.StageFormAssignmentExternalId });
        }

        var incubatorId = User.GetActiveIncubatorId();
        var projectId = User.GetActiveProjectId();
        var userId = User.GetUserId();

        if (incubatorId == 0 || !projectId.HasValue || !userId.HasValue)
        {
            TempData["ErrorMessage"] = "No se pudo determinar el contexto activo.";
            return RedirectToAction(nameof(Fill), new { assignmentExternalId = model.StageFormAssignmentExternalId });
        }

        var responses = model.Responses.Select(r =>
            new ResponseItem(r.QuestionId, r.TextValue, r.NumericValue, r.SelectedOptionIds)).ToList();

        var result = await _executor.SendAndLogIfFailureAsync(
            new SubmitDiagnosticResponseCommand(
                model.StageFormAssignmentExternalId,
                projectId.Value,
                incubatorId,
                userId.Value,
                responses),
            ct);

        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = "Diagnóstico enviado exitosamente.";
            return RedirectToAction(nameof(Confirmation));
        }

        TempData["ErrorMessage"] = "Error al enviar el diagnóstico.";
        return RedirectToAction(nameof(Fill), new { assignmentExternalId = model.StageFormAssignmentExternalId });
    }

    [HttpGet("[action]")]
    public IActionResult Confirmation()
    {
        return View();
    }
}
