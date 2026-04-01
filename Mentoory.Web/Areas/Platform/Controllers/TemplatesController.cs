using Mentoory.Diagnostic.Application.Queries.ListFormTemplates;
using Mentoory.Web.Models;
using Mentoory.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mentoory.Web.Areas.Platform.Controllers;

[Area("Platform")]
[Route("[area]/[controller]")]
[Authorize(Roles = "GlobalAdmin")]
public class TemplatesController : Controller
{
    private readonly MediatRExecutor _executor;

    public TemplatesController(MediatRExecutor executor)
    {
        _executor = executor;
    }

    [HttpGet("Diagnostics")]
    public IActionResult Diagnostics()
    {
        return View();
    }

    [HttpPost("Diagnostics/Data")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DiagnosticsData([FromForm] DataTableServerRequest request, CancellationToken ct)
    {
        var query = new ListFormTemplatesQuery(request.ToDataTableRequest(), null);
        var result = await _executor.SendOrThrowAsync(query, ct);

        return Json(new
        {
            draw = result.Draw,
            recordsTotal = result.RecordsTotal,
            recordsFiltered = result.RecordsFiltered,
            data = result.Data
        });
    }

    [HttpGet("Knowledge")]
    public IActionResult Knowledge()
    {
        // Placeholder for US3
        return View();
    }
}
