using System.Security.Claims;
using Mentoory.Diagnostic.Application.Commands.SubmitDiagnosticResponse;
using Mentoory.Diagnostic.Application.Queries.GetProjectForm;
using Mentoory.Diagnostic.Application.Queries.ListProjectForms;
using Mentoory.Shared.Application.DataTables;
using Mentoory.Web.Areas.Participant.Models;
using Mentoory.Web.Models;
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
    public async Task<IActionResult> Index(Guid? formExternalId, int evaluationStage = 0, CancellationToken ct = default)
    {
        var projectIdClaim = User.FindFirst("ActiveProjectId")?.Value;
        if (!long.TryParse(projectIdClaim, out var projectId))
        {
            TempData["WarningMessage"] = "Debe seleccionar un proyecto antes de continuar.";
            return RedirectToAction("Select", "Context", new { area = string.Empty });
        }

        if (!formExternalId.HasValue || formExternalId.Value == Guid.Empty)
        {
            return View("List");
        }

        if (!Enum.IsDefined(typeof(Mentoory.Diagnostic.Domain.Enums.EvaluationStage), evaluationStage))
        {
            evaluationStage = 0;
        }

        var form = await _executor.SendOrThrowAsync(
            new GetProjectFormQuery(formExternalId.Value, projectId), ct);

        if (form is null)
        {
            return NotFound();
        }

        var stageName = ((Mentoory.Diagnostic.Domain.Enums.EvaluationStage)evaluationStage).ToString();

        var model = new DiagnosticFormViewModel
        {
            FormExternalId = form.ExternalId,
            FormName = form.Name,
            EvaluationStage = stageName,
            Questions = form.Questions.Select(q => new QuestionViewModel
            {
                QuestionId = q.Id,
                QuestionExternalId = q.ExternalId,
                QuestionText = q.QuestionText,
                QuestionType = q.QuestionType.ToString(),
                IsOptional = q.IsOptional,
                BlockGroup = q.BlockGroup,
                AnswerOptions = q.AnswerOptions.Select(ao => new AnswerOptionViewModel
                {
                    AnswerOptionId = ao.Id,
                    OptionText = ao.OptionText,
                    SortOrder = ao.SortOrder
                }).ToList(),
                FollowUpQuestions = q.FollowUpQuestions.Select(fu => new FollowUpQuestionViewModel
                {
                    FollowUpQuestionId = fu.Id,
                    QuestionText = fu.QuestionText
                }).ToList()
            }).ToList()
        };

        return View(model);
    }

    [HttpPost("[action]")]
    public async Task<IActionResult> Data([FromForm] DataTableServerRequest request, CancellationToken ct)
    {
        var projectIdClaim = User.FindFirst("ActiveProjectId")?.Value;
        if (!long.TryParse(projectIdClaim, out var projectId))
        {
            return Json(new { draw = 0, recordsTotal = 0, recordsFiltered = 0, data = Array.Empty<object>() });
        }

        var query = new ListProjectFormsQuery(request.ToDataTableRequest(), projectId);
        var result = await _executor.SendOrThrowAsync(query, ct);

        return Json(new
        {
            draw = result.Draw,
            recordsTotal = result.RecordsTotal,
            recordsFiltered = result.RecordsFiltered,
            data = result.Data
        });
    }

    [HttpPost("[action]")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(SubmitDiagnosticViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToAction(nameof(Index), new { formExternalId = model.FormExternalId });
        }

        var incubatorIdClaim = User.FindFirst("ActiveIncubatorId")?.Value;
        var projectIdClaim = User.FindFirst("ActiveProjectId")?.Value;
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!long.TryParse(incubatorIdClaim, out var incubatorId) ||
            !long.TryParse(projectIdClaim, out var projectId) ||
            !long.TryParse(userIdClaim, out var userId))
        {
            TempData["ErrorMessage"] = "No se pudo determinar el contexto activo.";
            return RedirectToAction(nameof(Index), new { formExternalId = model.FormExternalId });
        }

        var responses = model.Responses.Select(r =>
            new ResponseItem(r.QuestionId, r.TextValue, r.NumericValue, r.SelectedOptionIds)).ToList();

        var result = await _executor.SendAndLogIfFailureAsync(
            new SubmitDiagnosticResponseCommand(
                model.FormExternalId,
                projectId,
                incubatorId,
                userId,
                (Mentoory.Diagnostic.Domain.Enums.EvaluationStage)model.EvaluationStage,
                responses),
            ct);

        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = "Diagnóstico enviado exitosamente.";
            return RedirectToAction(nameof(Confirmation));
        }

        TempData["ErrorMessage"] = "Error al enviar el diagnóstico.";
        return RedirectToAction(nameof(Index), new { formExternalId = model.FormExternalId });
    }

    [HttpGet("[action]")]
    public IActionResult Confirmation()
    {
        return View();
    }
}
