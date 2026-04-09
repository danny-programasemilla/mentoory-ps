using System.Globalization;
using CsvHelper;
using Mentoory.Access.Application.Commands.BatchRegisterUsers;
using Mentoory.Access.Application.Queries.GetUserContexts;
using Mentoory.Shared.Domain.Constants;
using Mentoory.Tenant.Application.Queries.ListRegistrationProjects;
using Mentoory.Web.Areas.Administration.Infrastructure;
using Mentoory.Web.Areas.Administration.Models;
using Mentoory.Web.Infrastructure;
using Mentoory.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mentoory.Web.Areas.Administration.Controllers;

[Area("Administration")]
[Authorize(Roles = "ProjectCoordinator,IncubatorAdmin,GlobalAdmin")]
public class BatchUploadController : Controller
{
    private readonly MediatRExecutor _executor;

    public BatchUploadController(MediatRExecutor executor)
    {
        _executor = executor;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (!User.HasValidIncubatorContext())
        {
            TempData["WarningMessage"] = "Debe seleccionar una incubadora antes de continuar.";
            return RedirectToAction("Select", "Context", new { area = string.Empty, returnUrl = Request.Path.Value });
        }

        var authorizedProjectIds = await GetAuthorizedProjectIdsAsync(ct);
        if (User.GetActiveRole() == Roles.ProjectCoordinator && authorizedProjectIds is { Count: 0 })
        {
            TempData["WarningMessage"] = "No tiene proyectos asignados para carga masiva.";
            return RedirectToAction("Index", "Home", new { area = "Administration" });
        }

        var model = new BatchUploadViewModel();
        await PopulateProjectsAsync(model, authorizedProjectIds, ct);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(BatchUploadViewModel model, CancellationToken ct)
    {
        if (!User.HasValidIncubatorContext())
        {
            TempData["WarningMessage"] = "Debe seleccionar una incubadora antes de continuar.";
            return RedirectToAction("Select", "Context", new { area = string.Empty, returnUrl = Request.Path.Value });
        }

        var authorizedProjectIds = await GetAuthorizedProjectIdsAsync(ct);
        var incubatorId = User.GetActiveIncubatorId();
        var callerIncubatorId = User.GetActiveIncubatorIdOrNull();
        var projectsResult = await _executor.SendAndLogIfFailureAsync(
            new ListRegistrationProjectsQuery(incubatorId, authorizedProjectIds, callerIncubatorId), ct);

        if (!ModelState.IsValid || model.CsvFile is null)
        {
            return ViewWithProjects(model, projectsResult);
        }

        if (User.GetActiveRole() == Roles.ProjectCoordinator && projectsResult.IsSuccess)
        {
            var authorizedExternalIds = projectsResult.Value!.Projects.Select(p => p.ExternalId).ToHashSet();
            if (!authorizedExternalIds.Contains(model.ProjectExternalId))
            {
                return Forbid();
            }
        }

        var incubatorExternalId = projectsResult.IsSuccess
            ? projectsResult.Value!.IncubatorExternalId
            : Guid.Empty;

        List<BatchUserRow> rows;
        try
        {
            rows = ParseCsvFile(model.CsvFile);
        }
        catch (HeaderValidationException)
        {
            ModelState.AddModelError(string.Empty, "El archivo CSV no contiene las columnas esperadas.");
            return ViewWithProjects(model, projectsResult);
        }
        catch (Exception)
        {
            ModelState.AddModelError(string.Empty, "Error al leer el archivo CSV.");
            return ViewWithProjects(model, projectsResult);
        }

        var command = new BatchRegisterUsersCommand(rows, model.ProjectExternalId, incubatorExternalId);
        var result = await _executor.SendAndLogIfFailureAsync(command, ct);

        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessages?.FirstOrDefault().Message ?? "Error al procesar el archivo.");
            return ViewWithProjects(model, projectsResult);
        }

        return View("Results", result.Value);
    }

    private static List<BatchUserRow> ParseCsvFile(IFormFile csvFile)
    {
        using var stream = csvFile.OpenReadStream();
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<CsvUserMap>();

        return csv.GetRecords<CsvUserRecord>()
            .Select(r => new BatchUserRow(r.Country, r.Identification, r.Email, r.FirstName, r.LastName))
            .ToList();
    }

    private ViewResult ViewWithProjects(
        BatchUploadViewModel model,
        Shared.Application.Result<RegistrationProjectsResult> projectsResult)
    {
        if (projectsResult.IsSuccess)
        {
            model.Projects = projectsResult.Value!.Projects;
        }

        return View(model);
    }

    private async Task PopulateProjectsAsync(
        BatchUploadViewModel model,
        IReadOnlyList<long>? authorizedProjectIds,
        CancellationToken ct)
    {
        var incubatorId = User.GetActiveIncubatorId();
        var result = await _executor.SendAndLogIfFailureAsync(
            new ListRegistrationProjectsQuery(incubatorId, authorizedProjectIds, User.GetActiveIncubatorIdOrNull()), ct);

        if (result.IsSuccess)
        {
            model.Projects = result.Value!.Projects;
        }
    }

    private async Task<IReadOnlyList<long>?> GetAuthorizedProjectIdsAsync(CancellationToken ct)
    {
        if (User.GetActiveRole() != Roles.ProjectCoordinator)
        {
            return null;
        }

        var userId = User.GetUserId();
        if (userId is null)
        {
            return [];
        }

        var incubatorId = User.GetActiveIncubatorId();
        var contextsResult = await _executor.SendAndLogIfFailureAsync(
            new GetUserContextsQuery(userId.Value), ct);

        if (contextsResult.IsFailure)
        {
            return [];
        }

        return contextsResult.Value!
            .Where(c => c.IncubatorId == incubatorId
                        && c.Role == Roles.ProjectCoordinator
                        && c.ProjectId.HasValue)
            .Select(c => c.ProjectId!.Value)
            .ToList();
    }
}
