using Mentoory.Access.Application.Commands.ForcedPasswordChange;
using Mentoory.Web.Areas.Access.Models;
using Mentoory.Web.Infrastructure;
using Mentoory.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mentoory.Web.Areas.Access.Controllers;

[Area("Access")]
[Authorize]
public class ChangePasswordController : Controller
{
    private readonly MediatRExecutor _executor;

    public ChangePasswordController(MediatRExecutor executor)
    {
        _executor = executor;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(new ForcedPasswordChangeViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ForcedPasswordChangeViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var command = new ForcedPasswordChangeCommand(User.GetUserId(), model.CurrentPassword, model.NewPassword);
        var result = await _executor.SendAndLogIfFailureAsync(command, ct);

        if (result.IsFailure)
        {
            foreach (var error in result.ErrorMessages ?? [])
            {
                ModelState.AddModelError(error.Context, error.Message);
            }

            return View(model);
        }

        TempData["SuccessMessage"] = "Contraseña actualizada exitosamente.";
        return RedirectToAction("Select", "Context", new { area = string.Empty });
    }
}
