using Mentoory.Identity.Application.Commands.VerifyEmail;
using Mentoory.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mentoory.Web.Areas.Identity.Controllers;

[Area("Identity")]
[AllowAnonymous]
public class VerifyEmailController : Controller
{
    private readonly MediatRExecutor _executor;

    public VerifyEmailController(MediatRExecutor executor)
    {
        _executor = executor;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            ViewData["Success"] = false;
            ViewData["Message"] = "El enlace de verificación no es válido.";
            return View();
        }

        var command = new VerifyEmailCommand(token);
        var result = await _executor.SendAndLogIfFailureAsync(command, ct);

        if (result.IsFailure)
        {
            ViewData["Success"] = false;
            ViewData["Message"] = result.ErrorMessages?.FirstOrDefault().Message ?? "No se pudo verificar el correo electrónico.";
            return View();
        }

        ViewData["Success"] = true;
        ViewData["Message"] = "Su correo electrónico ha sido verificado exitosamente.";
        return View();
    }
}
