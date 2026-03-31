using System.Security.Claims;
using Mentoory.Identity.Application.Commands.LoginUser;
using Mentoory.Web.Areas.Identity.Models;
using Mentoory.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Mentoory.Web.Areas.Identity.Controllers;

[Area("Identity")]
[AllowAnonymous]
public class LoginController : Controller
{
    private readonly MediatRExecutor _executor;

    public LoginController(MediatRExecutor executor)
    {
        _executor = executor;
    }

    [HttpGet]
    public IActionResult Index(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Index(LoginViewModel model, string? returnUrl, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var command = new LoginUserCommand(
            model.Email,
            model.Password,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            HttpContext.Request.Headers.UserAgent.ToString());

        var result = await _executor.SendAndLogIfFailureAsync(command, ct);

        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessages?.FirstOrDefault().Message ?? "Credenciales inválidas.");
            return View(model);
        }

        var session = result.Value!;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, session.UserId.ToString()),
            new("SessionToken", session.SessionToken),
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return RedirectToAction("Select", "Context", new { area = string.Empty });
    }
}
