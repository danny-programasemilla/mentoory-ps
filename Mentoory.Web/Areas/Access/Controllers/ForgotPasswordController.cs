using Mentoory.Access.Application.Commands.RequestPasswordReset;
using Mentoory.Web.Areas.Access.Models;
using Mentoory.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Mentoory.Web.Areas.Access.Controllers;

[Area("Access")]
[AllowAnonymous]
public class ForgotPasswordController : Controller
{
    private readonly MediatRExecutor _executor;

    public ForgotPasswordController(MediatRExecutor executor)
    {
        _executor = executor;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(new ForgotPasswordViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("password-reset")]
    public async Task<IActionResult> Index(ForgotPasswordViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var command = new RequestPasswordResetCommand(model.Email);

        // Always redirect to confirmation to avoid email enumeration
        await _executor.SendAndLogIfFailureAsync(command, ct);

        return RedirectToAction("Confirmation");
    }

    [HttpGet]
    public IActionResult Confirmation()
    {
        return View();
    }
}
