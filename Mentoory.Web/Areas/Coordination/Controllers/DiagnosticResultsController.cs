using Mentoory.Diagnostic.Application.Queries.CompareDiagnosticResults;
using Mentoory.Diagnostic.Application.Queries.GetDiagnosticResponse;
using Mentoory.Diagnostic.Application.Queries.GetDiagnosticTimeline;
using Mentoory.Web.Areas.Coordination.Models;
using Mentoory.Web.Infrastructure;
using Mentoory.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mentoory.Web.Areas.Coordination.Controllers;

[Area("Coordination")]
[Route("[area]/[controller]")]
[Authorize(Roles = "ProjectCoordinator,Mentor,IncubatorAdmin,GlobalAdmin")]
public class DiagnosticResultsController : Controller
{
    private readonly MediatRExecutor _executor;

    public DiagnosticResultsController(MediatRExecutor executor)
    {
        _executor = executor;
    }

    [HttpGet("Timeline/{entrepreneurUserId:long}")]
    public async Task<IActionResult> Timeline(long entrepreneurUserId, CancellationToken ct)
    {
        var projectId = User.GetActiveProjectId();
        if (!projectId.HasValue)
        {
            TempData["WarningMessage"] = "Debe seleccionar un proyecto antes de continuar.";
            return RedirectToAction("Select", "Context", new { area = string.Empty, returnUrl = Request.Path.Value });
        }

        var entries = await _executor.SendOrThrowAsync(
            new GetDiagnosticTimelineQuery(projectId.Value, entrepreneurUserId), ct);

        var viewModel = new TimelineViewModel
        {
            EntrepreneurUserId = entrepreneurUserId,
            Entries = entries.Select(e => new TimelineEntryViewModel
            {
                ResponseExternalId = e.ResponseExternalId,
                AssignmentExternalId = e.AssignmentExternalId,
                ProjectStageId = e.ProjectStageId,
                FormName = e.FormName,
                CompletedAtUtc = e.CompletedAtUtc,
                ResponseCount = e.ResponseCount,
            }).ToList(),
        };

        return View(viewModel);
    }

    [HttpGet("Detail/{responseExternalId:guid}")]
    public async Task<IActionResult> Detail(Guid responseExternalId, CancellationToken ct)
    {
        var projectId = User.GetActiveProjectId();
        if (!projectId.HasValue)
        {
            TempData["WarningMessage"] = "Debe seleccionar un proyecto antes de continuar.";
            return RedirectToAction("Select", "Context", new { area = string.Empty, returnUrl = Request.Path.Value });
        }

        var response = await _executor.SendOrThrowAsync(
            new GetDiagnosticResponseQuery(responseExternalId, projectId), ct);

        if (response is null)
        {
            return NotFound();
        }

        return View(response);
    }

    [HttpGet("Compare")]
    public async Task<IActionResult> Compare(
        [FromQuery] Guid responseId1,
        [FromQuery] Guid responseId2,
        CancellationToken ct)
    {
        var comparison = await _executor.SendOrThrowAsync(
            new CompareDiagnosticResultsQuery(responseId1, responseId2), ct);

        var viewModel = new CompareViewModel
        {
            Earlier = new ComparisonSideViewModel
            {
                ResponseExternalId = comparison.Earlier.ResponseExternalId,
                FormName = comparison.Earlier.FormName,
                CompletedAtUtc = comparison.Earlier.CompletedAtUtc,
            },
            Later = new ComparisonSideViewModel
            {
                ResponseExternalId = comparison.Later.ResponseExternalId,
                FormName = comparison.Later.FormName,
                CompletedAtUtc = comparison.Later.CompletedAtUtc,
            },
            TopicComparisons = comparison.TopicComparisons.Select(t => new TopicComparisonViewModel
            {
                TopicId = t.TopicId,
                PreviousScore = t.PreviousScore,
                CurrentScore = t.CurrentScore,
                Delta = t.Delta,
                PercentageChange = t.PercentageChange,
            }).ToList(),
            SharedQuestions = comparison.SharedQuestions.Select(q => new QuestionComparisonViewModel
            {
                QuestionId = q.QuestionId,
                QuestionText = q.QuestionText,
                EarlierAnswer = q.EarlierAnswer,
                LaterAnswer = q.LaterAnswer,
                EarlierNumeric = q.EarlierNumeric,
                LaterNumeric = q.LaterNumeric,
            }).ToList(),
        };

        return View(viewModel);
    }
}
