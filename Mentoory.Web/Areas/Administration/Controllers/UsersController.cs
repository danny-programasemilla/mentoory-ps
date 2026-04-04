using Mentoory.Access.Application.Queries.ListIncubatorMembers;
using Mentoory.Access.Application.Commands.RegisterUser;
using Mentoory.Web.Areas.Administration.Models;
using Mentoory.Web.Models;
using Mentoory.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mentoory.Web.Areas.Administration.Controllers;

[Area("Administration")]
[Authorize(Roles = "IncubatorAdmin,GlobalAdmin")]
public class UsersController : Controller
{
    private readonly MediatRExecutor _executor;

    public UsersController(MediatRExecutor executor)
    {
        _executor = executor;
    }

    [HttpGet]
    public IActionResult Index()
    {
        if (!HasValidIncubatorContext())
        {
            TempData["WarningMessage"] = "Debe seleccionar una incubadora antes de continuar.";
            return RedirectToAction("Select", "Context", new { area = string.Empty, returnUrl = Request.Path.Value });
        }

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Data([FromForm] DataTableServerRequest request, CancellationToken ct)
    {
        var incubatorId = GetActiveIncubatorId();
        var query = new ListIncubatorMembersQuery(request.ToDataTableRequest(), incubatorId);
        var result = await _executor.SendOrThrowAsync(query, ct);

        return Json(new
        {
            draw = result.Draw,
            recordsTotal = result.RecordsTotal,
            recordsFiltered = result.RecordsFiltered,
            data = result.Data
        });
    }

    [HttpGet]
    public IActionResult Enroll()
    {
        return View(new EnrollUserViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enroll(EnrollUserViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _executor.SendAndLogIfFailureAsync(
            new RegisterUserCommand(
                model.Email,
                model.Country,
                model.NationalId,
                model.FirstName,
                model.LastName,
                model.Password),
            ct);

        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = "Usuario inscrito exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        if (result.ErrorMessages is not null)
        {
            foreach (var (context, message) in result.ErrorMessages)
            {
                ModelState.AddModelError(context, message);
            }
        }
        else
        {
            ModelState.AddModelError(string.Empty, "Error al inscribir el usuario.");
        }

        return View(model);
    }

    private bool HasValidIncubatorContext()
    {
        return long.TryParse(User.FindFirst("ActiveIncubatorId")?.Value, out var id) && id > 0;
    }

    private long GetActiveIncubatorId()
    {
        var claim = User.FindFirst("ActiveIncubatorId")?.Value;
        return long.TryParse(claim, out var id) ? id : 0;
    }
}
