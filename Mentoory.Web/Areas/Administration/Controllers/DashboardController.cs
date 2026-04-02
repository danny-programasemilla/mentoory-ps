using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mentoory.Web.Areas.Administration.Controllers;

[Area("Administration")]
[Authorize(Roles = "IncubatorAdmin,GlobalAdmin")]
public class DashboardController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        var incubatorIdClaim = User.FindFirst("ActiveIncubatorId")?.Value;
        if (!long.TryParse(incubatorIdClaim, out var incubatorId) || incubatorId <= 0)
        {
            TempData["WarningMessage"] = "Debe seleccionar una incubadora antes de continuar.";
            return RedirectToAction("Select", "Context", new { area = string.Empty, returnUrl = Request.Path.Value });
        }

        return View();
    }
}
