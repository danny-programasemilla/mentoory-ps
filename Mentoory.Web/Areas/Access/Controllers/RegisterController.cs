using Mentoory.Access.Application.Commands.RegisterUser;
using Mentoory.Web.Areas.Access.Models;
using Mentoory.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Mentoory.Web.Areas.Access.Controllers;

[Area("Access")]
[AllowAnonymous]
public class RegisterController : Controller
{
    private readonly MediatRExecutor _executor;

    public RegisterController(MediatRExecutor executor)
    {
        _executor = executor;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("registration")]
    public async Task<IActionResult> Index(RegisterViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var command = new RegisterUserCommand(
            model.Email,
            model.Country,
            model.NationalId,
            model.FirstName,
            model.LastName,
            model.Password);

        var result = await _executor.SendAndLogIfFailureAsync(command, ct);

        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessages?.FirstOrDefault().Message ?? "No se pudo completar el registro.");
            return View(model);
        }

        return RedirectToAction("Success");
    }

    [HttpGet]
    public IActionResult Success()
    {
        return View();
    }
}
