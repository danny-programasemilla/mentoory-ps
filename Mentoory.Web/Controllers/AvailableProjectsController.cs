using Mentoory.Tenant.Application.Invitations.Commands.RequestSelfEnrollment;
using Mentoory.Web.Infrastructure;
using Mentoory.Tenant.Application.Projects.Queries.ListPublicProjects;
using Mentoory.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mentoory.Web.Controllers;

/// <summary>
/// Controller for listing public projects and handling self-enrollment requests
/// for authenticated users who do not yet have a project context.
/// </summary>
[Authorize]
public class AvailableProjectsController : Controller
{
    private readonly MediatRExecutor _executor;

    public AvailableProjectsController(MediatRExecutor executor)
    {
        _executor = executor;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var projects = await _executor.SendOrThrowAsync(new ListPublicProjectsQuery(), ct);
        return View(projects);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enroll(Guid projectExternalId, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return RedirectToAction("Login", "Login", new { area = "Access" });
        }

        var result = await _executor.SendAndLogIfFailureAsync(
            new RequestSelfEnrollmentCommand(projectExternalId, userId.Value), ct);

        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = "Solicitud de inscripción enviada exitosamente.";
        }
        else
        {
            var message = result.ErrorMessages?.FirstOrDefault().Message
                          ?? "Error al procesar la solicitud de inscripción.";
            TempData["ErrorMessage"] = message;
        }

        return RedirectToAction(nameof(Index));
    }
}
