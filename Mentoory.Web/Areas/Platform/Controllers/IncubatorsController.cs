using Mentoory.Tenant.Application.Commands.CreateIncubator;
using Mentoory.Tenant.Application.Commands.UpdateIncubator;
using Mentoory.Tenant.Application.Queries.GetIncubatorByExternalId;
using Mentoory.Tenant.Application.Queries.ListIncubators;
using Mentoory.Web.Areas.Platform.Models;
using Mentoory.Web.Models;
using Mentoory.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mentoory.Web.Areas.Platform.Controllers;

[Area("Platform")]
[Route("[area]/[controller]")]
[Authorize(Roles = "GlobalAdmin")]
public class IncubatorsController : Controller
{
    private readonly MediatRExecutor _executor;

    public IncubatorsController(MediatRExecutor executor)
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
        var query = new ListIncubatorsQuery(request.ToDataTableRequest());
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
        return View(new CreateIncubatorViewModel());
    }

    [HttpPost("[action]")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateIncubatorViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _executor.SendAndLogIfFailureAsync(
            new CreateIncubatorCommand(model.Name, model.Description), ct);

        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = "Incubadora creada exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError(string.Empty, "Error al crear la incubadora.");
        return View(model);
    }

    [HttpGet("{externalId:guid}")]
    public async Task<IActionResult> Details(Guid externalId, CancellationToken ct)
    {
        var incubator = await _executor.SendOrThrowAsync(
            new GetIncubatorByExternalIdQuery(externalId), ct);

        return View(incubator);
    }

    [HttpGet("{externalId:guid}/Edit")]
    public async Task<IActionResult> Edit(Guid externalId, CancellationToken ct)
    {
        var incubator = await _executor.SendOrThrowAsync(
            new GetIncubatorByExternalIdQuery(externalId), ct);

        var model = new EditIncubatorViewModel
        {
            ExternalId = incubator.ExternalId,
            Name = incubator.Name,
            Description = incubator.Description
        };

        return View(model);
    }

    [HttpPost("{externalId:guid}/Edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid externalId, EditIncubatorViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _executor.SendAndLogIfFailureAsync(
            new UpdateIncubatorCommand(externalId, model.Name, model.Description), ct);

        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = "Incubadora actualizada exitosamente.";
            return RedirectToAction(nameof(Details), new { externalId });
        }

        ModelState.AddModelError(string.Empty, "Error al actualizar la incubadora.");
        return View(model);
    }
}
