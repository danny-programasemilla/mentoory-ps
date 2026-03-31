using System.Security.Claims;
using Mentoory.Identity.Application.Commands.LogoutUser;
using Mentoory.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mentoory.Web.Areas.Identity.Controllers;

[Area("Identity")]
[Authorize]
public class LogoutController : Controller
{
    private readonly MediatRExecutor _executor;

    public LogoutController(MediatRExecutor executor)
    {
        _executor = executor;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var sessionToken = User.FindFirstValue("SessionToken");

        if (!string.IsNullOrEmpty(sessionToken))
        {
            var command = new LogoutUserCommand(sessionToken);
            await _executor.SendAndLogIfFailureAsync(command, ct);
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction("Index", "Login", new { area = "Identity" });
    }
}
