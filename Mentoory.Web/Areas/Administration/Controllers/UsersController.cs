using Mentoory.Access.Application.Commands.AdminVerifyEmail;
using Mentoory.Access.Application.Commands.CreateUser;
using Mentoory.Access.Application.Commands.RegenerateVerificationToken;
using Mentoory.Access.Application.Countries.Queries.ListCountries;
using Mentoory.Access.Application.Queries.ListIncubatorMembers;
using Mentoory.Tenant.Application.Queries.ResolveProjectExternalId;
using Mentoory.Web.Areas.Administration.Models;
using Mentoory.Web.Infrastructure;
using Mentoory.Web.Models;
using Mentoory.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mentoory.Web.Areas.Administration.Controllers;

[Area("Administration")]
[Authorize(Roles = "ProjectCoordinator,IncubatorAdmin,GlobalAdmin")]
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
        if (!User.HasValidIncubatorContext())
        {
            TempData["WarningMessage"] = "Debe seleccionar una incubadora antes de continuar.";
            return RedirectToAction("Select", "Context", new { area = string.Empty, returnUrl = Request.Path.Value });
        }

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Data([FromForm] DataTableServerRequest request, CancellationToken ct)
    {
        var incubatorId = User.GetActiveIncubatorId();
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
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        var model = new CreateUserViewModel();
        var countriesResult = await _executor.SendAndLogIfFailureAsync(new ListCountriesQuery(), ct);
        model.Countries = countriesResult.IsSuccess ? countriesResult.Value! : [];

        if (User.HasValidProjectContext())
        {
            model.HasActiveProject = true;
            model.ActiveProjectName = User.GetActiveProjectName();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCreateViewModel(model, ct);
            return View(model);
        }

        if (!User.HasValidProjectContext())
        {
            TempData["ErrorMessage"] = "Debe seleccionar un proyecto antes de crear un usuario.";
            await PopulateCreateViewModel(model, ct);
            return View(model);
        }

        var projectId = User.GetActiveProjectId()!.Value;
        var resolveResult = await _executor.SendAndLogIfFailureAsync(
            new ResolveProjectExternalIdQuery(projectId), ct);

        if (resolveResult.IsFailure)
        {
            TempData["ErrorMessage"] = "No se pudo resolver el proyecto seleccionado.";
            await PopulateCreateViewModel(model, ct);
            return View(model);
        }

        var command = new CreateUserCommand(
            model.Email,
            model.Country,
            model.Identification,
            model.FirstName,
            model.LastName,
            model.SkipEmailVerification,
            model.SkipInvitationAcceptance,
            resolveResult.Value!);

        var result = await _executor.SendAndLogIfFailureAsync(command, ct);

        if (result.IsSuccess)
        {
            var value = result.Value!;
            switch (value.Outcome)
            {
                case CreateUserOutcome.Created:
                    TempData["SuccessMessage"] = "Usuario creado exitosamente.";
                    break;
                case CreateUserOutcome.Enrolled:
                    TempData["SuccessMessage"] = "Usuario existente inscrito al proyecto.";
                    break;
                case CreateUserOutcome.AlreadyEnrolled:
                    TempData["SuccessMessage"] = "El usuario ya está inscrito en este proyecto.";
                    break;
            }

            if (value.TemporaryPassword is not null)
            {
                TempData["TemporaryPassword"] = value.TemporaryPassword;
            }

            if (value.Warnings.Count > 0)
            {
                TempData["WarningMessages"] = string.Join("|", value.Warnings);
            }

            return RedirectToAction(nameof(Index));
        }

        foreach (var error in result.ErrorMessages ?? [])
        {
            ModelState.AddModelError(error.Context, error.Message);
        }

        await PopulateCreateViewModel(model, ct);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdminVerifyEmail(Guid userExternalId, CancellationToken ct)
    {
        var result = await _executor.SendAndLogIfFailureAsync(
            new AdminVerifyEmailCommand(userExternalId), ct);

        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = "Correo electrónico verificado exitosamente.";
        }
        else
        {
            TempData["ErrorMessage"] = result.ErrorMessages?.FirstOrDefault().Message ?? "Error al verificar el correo.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegenerateVerificationToken(Guid userExternalId, CancellationToken ct)
    {
        var result = await _executor.SendAndLogIfFailureAsync(
            new RegenerateVerificationTokenCommand(userExternalId), ct);

        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = "Token de verificación regenerado exitosamente.";
        }
        else
        {
            TempData["ErrorMessage"] = result.ErrorMessages?.FirstOrDefault().Message ?? "Error al regenerar el token.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateCreateViewModel(CreateUserViewModel model, CancellationToken ct)
    {
        var countriesResult = await _executor.SendAndLogIfFailureAsync(new ListCountriesQuery(), ct);
        model.Countries = countriesResult.IsSuccess ? countriesResult.Value! : [];

        if (User.HasValidProjectContext())
        {
            model.HasActiveProject = true;
            model.ActiveProjectName = User.GetActiveProjectName();
        }
    }
}
