using System.Globalization;
using CsvHelper;
using Mentoory.Access.Application.Commands.BatchCreateUsers;
using Mentoory.Access.Application.Commands.BatchRegisterUsers;
using Mentoory.Tenant.Application.Queries.GetProjectContextInfo;
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
    public IActionResult Index()
    {
        if (!User.HasValidIncubatorContext())
        {
            TempData["WarningMessage"] = "Debe seleccionar una incubadora antes de continuar.";
            return RedirectToAction("Select", "Context", new { area = string.Empty, returnUrl = Request.Path.Value });
        }

        var projectId = User.GetActiveProjectId();
        var model = new BatchUploadViewModel
        {
            HasActiveProject = projectId.HasValue,
            ActiveProjectName = User.FindFirst("ActiveProjectName")?.Value,
        };

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

        var projectId = User.GetActiveProjectId();
        if (!projectId.HasValue)
        {
            TempData["ErrorMessage"] = "Seleccione un proyecto desde el selector de contexto para continuar.";
            return ReturnBatchView(model);
        }

        if (!ModelState.IsValid || model.CsvFile is null)
        {
            return ReturnBatchView(model);
        }

        var contextInfo = await _executor.SendAndLogIfFailureAsync(
            new GetProjectContextInfoQuery(projectId.Value), ct);

        if (contextInfo.IsFailure)
        {
            TempData["ErrorMessage"] = "Error al resolver el contexto del proyecto.";
            return ReturnBatchView(model);
        }

        List<BatchUserRow> rows;
        try
        {
            rows = ParseCsvFile(model.CsvFile);
        }
        catch (HeaderValidationException)
        {
            ModelState.AddModelError(string.Empty, "El archivo CSV no contiene las columnas esperadas.");
            return ReturnBatchView(model);
        }
        catch (Exception)
        {
            ModelState.AddModelError(string.Empty, "Error al leer el archivo CSV.");
            return ReturnBatchView(model);
        }

        var userId = User.GetUserId();
        var command = new BatchCreateUsersCommand(
            rows,
            model.SkipEmailVerification,
            model.SkipInvitationAcceptance,
            contextInfo.Value!.ProjectExternalId,
            userId ?? 0);

        var result = await _executor.SendAndLogIfFailureAsync(command, ct);

        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessages?.FirstOrDefault().Message ?? "Error al procesar el archivo.");
            return ReturnBatchView(model);
        }

        return View("Results", result.Value);
    }

    [HttpGet]
    public IActionResult DownloadSample()
    {
        using var memoryStream = new MemoryStream();
        using (var writer = new StreamWriter(memoryStream, leaveOpen: true))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            csv.Context.RegisterClassMap<CsvUserMap>();
            csv.WriteHeader<CsvUserRecord>();
            csv.NextRecord();

            csv.WriteRecord(new CsvUserRecord
            {
                Country = "CRI",
                Identification = "101110111",
                Email = "juan.ejemplo@correo.com",
                FirstName = "Juan",
                LastName = "Ejemplo"
            });
            csv.NextRecord();

            csv.WriteRecord(new CsvUserRecord
            {
                Country = "CRI",
                Identification = "202220222",
                Email = "maria.muestra@correo.com",
                FirstName = "Maria",
                LastName = "Muestra"
            });
            csv.NextRecord();
        }

        return File(memoryStream.ToArray(), "text/csv", "plantilla-carga-masiva.csv");
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

    private ViewResult ReturnBatchView(BatchUploadViewModel model)
    {
        model.HasActiveProject = User.GetActiveProjectId().HasValue;
        model.ActiveProjectName = User.FindFirst("ActiveProjectName")?.Value;
        return View(model);
    }
}
