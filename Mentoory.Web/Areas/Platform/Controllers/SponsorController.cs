using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mentoory.Web.Areas.Platform.Controllers;

[Area("Platform")]
[Route("[area]/[controller]")]
[Authorize(Roles = "Sponsor,IncubatorAdmin,GlobalAdmin")]
public class SponsorController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        return View();
    }
}
