using System.Diagnostics;
using Mentoory.Web.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mentoory.Web.Models;

namespace Mentoory.Web.Controllers;

public class HomeController : Controller
{
    [Authorize]
    public IActionResult Index()
    {
        var activeRole = User.GetActiveRole();

        if (string.IsNullOrEmpty(activeRole))
        {
            return RedirectToAction("Index", "AvailableProjects");
        }

        return activeRole switch
        {
            "GlobalAdmin" => RedirectToAction("Index", "Incubators", new { area = "Platform" }),
            "IncubatorAdmin" => RedirectToAction("Index", "Dashboard", new { area = "Administration" }),
            "ProjectCoordinator" => RedirectToAction("Index", "Diagnostics", new { area = "Coordination" }),
            "Mentor" => RedirectToAction("Index", "Diagnostics", new { area = "Coordination" }),
            "Entrepreneur" => RedirectToAction("Index", "Diagnostic", new { area = "Participant" }),
            "Sponsor" => RedirectToAction("Index", "Sponsor", new { area = "Platform" }),
            _ => RedirectToAction("Select", "Context")
        };
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
