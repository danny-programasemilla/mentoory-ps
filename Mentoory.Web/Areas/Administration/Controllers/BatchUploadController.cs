using System.Globalization;
using CsvHelper;
using Mentoory.Access.Application.Commands.BatchRegisterUsers;
using Mentoory.Tenant.Application.Queries.ListRegistrationProjects;
using Mentoory.Web.Areas.Administration.Infrastructure;
using Mentoory.Web.Areas.Administration.Models;
using Mentoory.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mentoory.Web.Areas.Administration.Controllers;

[Area("Administration")]
[Authorize(Roles = "IncubatorAdmin,GlobalAdmin")]
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
        if (!HasValidIncubatorContext())
        {
            TempData["WarningMessage"] = "Debe seleccionar una incubadora antes de continuar.";
            return RedirectToAction("Select", "Context", new { area = string.Empty, returnUrl = Request.Path.Value });
        }

        var model = new BatchUploadViewModel();
        await PopulateProjectsAsync(model, ct);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(BatchUploadViewModel model, CancellationToken ct)
    {
        if (!HasValidIncubatorContext())
        {
            TempData["WarningMessage"] = "Debe seleccionar una incubadora antes de continuar.";
            return RedirectToAction("Select", "Context", new { area = string.Empty, returnUrl = Request.Path.Value });
        }

        var incubatorId = GetActiveIncubatorId();
        var projectsResult = await _executor.SendAndLogIfFailureAsync(
            new ListRegistrationProjectsQuery(incubatorId), ct);

        if (!ModelState.IsValid || model.CsvFile is null)
        {
            if (projectsResult.IsSuccess)
            {
                model.Projects = projectsResult.Value!.Projects;
            }

            return View(model);
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
            if (projectsResult.IsSuccess)
            {
                model.Projects = projectsResult.Value!.Projects;
            }

            return View(model);
        }
        catch (Exception)
        {
            ModelState.AddModelError(string.Empty, "Error al leer el archivo CSV.");
            if (projectsResult.IsSuccess)
            {
                model.Projects = projectsResult.Value!.Projects;
            }

            return View(model);
        }

        var command = new BatchRegisterUsersCommand(rows, model.ProjectExternalId, incubatorExternalId);
        var result = await _executor.SendAndLogIfFailureAsync(command, ct);

        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessages?.FirstOrDefault().Message ?? "Error al procesar el archivo.");
            if (projectsResult.IsSuccess)
            {
                model.Projects = projectsResult.Value!.Projects;
            }

            return View(model);
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

    private async Task PopulateProjectsAsync(BatchUploadViewModel model, CancellationToken ct)
    {
        var incubatorId = GetActiveIncubatorId();
        var result = await _executor.SendAndLogIfFailureAsync(
            new ListRegistrationProjectsQuery(incubatorId), ct);

        if (result.IsSuccess)
        {
            model.Projects = result.Value!.Projects;
        }
    }

    private bool HasValidIncubatorContext()
    {
        return long.TryParse(User.FindFirst("ActiveIncubatorId")?.Value, out var id) && id > 0;
    }

    private long GetActiveIncubatorId()
    {
        var claim = User.FindFirst("ActiveIncubatorId")?.Value;
        return long.TryParse(claim, out var id) ? id : 0;
    }
}
