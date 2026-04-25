using Mentoory.Shared.Application.Queries.Audit;
using Mentoory.Web.Models;
using Mentoory.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mentoory.Web.Areas.Administration.Controllers;

[Area("Administration")]
[Authorize(Roles = "GlobalAdmin")]
public class AuditLogController : Controller
{
    private readonly MediatRExecutor _executor;

    public AuditLogController(MediatRExecutor executor)
    {
        _executor = executor;
    }

    [HttpGet]
    public IActionResult Index() => View();

    [HttpPost]
    public async Task<IActionResult> Data([FromForm] DataTableServerRequest request, CancellationToken ct)
    {
        var query = new GetAuditLogPagedQuery(request.ToDataTableRequest());
        var result = await _executor.SendOrThrowAsync(query, ct);

        return Json(new
        {
            draw = result.Draw,
            recordsTotal = result.RecordsTotal,
            recordsFiltered = result.RecordsFiltered,
            data = result.Data
        });
    }
}
