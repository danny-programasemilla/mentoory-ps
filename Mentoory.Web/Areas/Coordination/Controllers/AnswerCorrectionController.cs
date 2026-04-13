using Mentoory.Diagnostic.Application.Commands.CorrectAnswer;
using Mentoory.Web.Infrastructure;
using Mentoory.Diagnostic.Application.Queries.GetDiagnosticResponse;
using Mentoory.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mentoory.Web.Areas.Coordination.Controllers;

[Area("Coordination")]
[Route("[area]/AnswerCorrection")]
[Authorize(Roles = "ProjectCoordinator,IncubatorAdmin,GlobalAdmin,Mentor")]
public class AnswerCorrectionController : Controller
{
    private readonly MediatRExecutor _executor;

    public AnswerCorrectionController(MediatRExecutor executor)
    {
        _executor = executor;
    }

    [HttpGet("{diagnosticExternalId:guid}")]
    public async Task<IActionResult> Index(Guid diagnosticExternalId, CancellationToken ct)
    {
        var projectId = User.GetActiveProjectId();
        if (!projectId.HasValue)
        {
            TempData["WarningMessage"] = "Debe seleccionar un proyecto antes de continuar.";
            return RedirectToAction("Select", "Context", new { area = string.Empty });
        }

        var response = await _executor.SendOrThrowAsync(
            new GetDiagnosticResponseQuery(diagnosticExternalId, projectId.Value), ct);

        return View(response);
    }

    [HttpPost("Correct")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Correct(
        Guid diagnosticExternalId,
        long questionResponseId,
        string? newTextValue,
        decimal? newNumericValue,
        List<long>? newSelectedOptionIds,
        string? reason,
        CancellationToken ct)
    {
        var correctProjectId = User.GetActiveProjectId();

        var result = await _executor.SendAndLogIfFailureAsync(
            new CorrectAnswerCommand(
                diagnosticExternalId,
                questionResponseId,
                newTextValue,
                newNumericValue,
                newSelectedOptionIds,
                User.GetUserId(),
                reason,
                correctProjectId),
            ct);

        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = "Respuesta corregida exitosamente.";
        }
        else
        {
            TempData["ErrorMessage"] = "Error al corregir la respuesta.";
        }

        return RedirectToAction(nameof(Index), new { diagnosticExternalId });
    }
}
