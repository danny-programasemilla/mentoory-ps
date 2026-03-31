using Mentoory.Tenant.Application.Commands.CreateProject;
using Mentoory.Tenant.Application.Queries.GetProjectByExternalId;
using Mentoory.Tenant.Application.Queries.ListIncubators;
using Mentoory.Tenant.Application.Queries.ListProjects;
using Mentoory.Web.Areas.Administration.Models;
using Mentoory.Web.Models;
using Mentoory.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mentoory.Web.Areas.Administration.Controllers;

[Area("Administration")]
[Route("[area]/[controller]")]
[Authorize(Roles = "IncubatorAdmin")]
public class ProjectsController : Controller
{
    private readonly MediatRExecutor _executor;

    public ProjectsController(MediatRExecutor executor)
    {
        _executor = executor;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost("[action]")]
    public async Task<IActionResult> Data([FromForm] DataTableServerRequest request, CancellationToken ct)
    {
        var incubatorId = GetActiveIncubatorId();
        var query = new ListProjectsQuery(request.ToDataTableRequest(), incubatorId);
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
    public IActionResult Create()
    {
        return View(new CreateProjectViewModel());
    }

    [HttpPost("[action]")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateProjectViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var incubatorExternalId = await GetIncubatorExternalIdAsync(ct);

        var result = await _executor.SendAndLogIfFailureAsync(
            new CreateProjectCommand(incubatorExternalId, model.Name, model.Description), ct);

        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = "Proyecto creado exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError(string.Empty, "Error al crear el proyecto.");
        return View(model);
    }

    [HttpGet("{externalId:guid}")]
    public async Task<IActionResult> Details(Guid externalId, CancellationToken ct)
    {
        var project = await _executor.SendOrThrowAsync(
            new GetProjectByExternalIdQuery(externalId), ct);

        return View(project);
    }

    private long GetActiveIncubatorId()
    {
        var claim = User.FindFirst("ActiveIncubatorId")?.Value;
        return long.TryParse(claim, out var id) ? id : 0;
    }

    private async Task<Guid> GetIncubatorExternalIdAsync(CancellationToken ct)
    {
        // The incubator ID from context is the internal ID; we need the ExternalId for the command.
        // We use ListIncubators with a minimal request to get the incubator info.
        var incubatorId = GetActiveIncubatorId();
        var request = new Mentoory.Shared.Application.DataTables.DataTableRequest(1, 0, 1, null, "asc", null, null);
        var result = await _executor.SendOrThrowAsync(new ListIncubatorsQuery(request), ct);

        var incubator = result.Data.FirstOrDefault();
        return incubator?.ExternalId ?? Guid.Empty;
    }
}
