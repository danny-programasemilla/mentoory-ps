using Mentoory.Tenant.Application.Queries.GetDashboardMetrics;
using Mentoory.Web.Infrastructure;
using Mentoory.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mentoory.Web.Areas.Administration.Controllers;

[Area("Administration")]
[Authorize(Roles = "IncubatorAdmin,GlobalAdmin")]
public class DashboardController(MediatRExecutor executor) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!User.HasValidIncubatorContext())
        {
            TempData["WarningMessage"] = "Debe seleccionar una incubadora antes de continuar.";
            return RedirectToAction("Select", "Context", new { area = string.Empty, returnUrl = Request.Path.Value });
        }

        var incubatorId = User.GetActiveIncubatorId();
        var result = await executor.SendAndLogIfFailureAsync(new GetDashboardMetricsQuery(incubatorId));

        if (result.IsSuccess)
        {
            return View(result.Value);
        }

        return View(new DashboardMetricsDto(0, 0));
    }
}
