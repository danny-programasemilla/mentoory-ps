using Mentoory.Access.Application.Commands.AdminVerifyEmail;
using Mentoory.Access.Application.Commands.CreateUser;
using Mentoory.Access.Application.Commands.RegenerateVerificationToken;
using Mentoory.Access.Application.Countries.Queries.ListCountries;
using Mentoory.Access.Application.Queries.ListIncubatorMembers;
using Mentoory.Tenant.Application.Invitations.Commands.ReissueInvitation;
using Mentoory.Tenant.Application.Invitations.Queries.GetBulkInvitationStatus;
using Mentoory.Tenant.Application.Invitations.Queries.GetPendingInvitationForUser;
using Mentoory.Tenant.Application.Queries.GetProjectContextInfo;
using Mentoory.Web.Areas.Administration.Models;
using Mentoory.Web.Infrastructure;
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

        // Enrich with onboarding status if a project is selected
        var projectId = User.GetActiveProjectId();
        if (projectId.HasValue && result.Data.Count > 0)
        {
            var userIds = result.Data.Select(d => d.UserId).ToList();
            var statusResult = await _executor.SendAndLogIfFailureAsync(
                new GetBulkInvitationStatusQuery(userIds, projectId.Value), ct);

            if (statusResult.IsSuccess)
            {
                var invitationStatuses = statusResult.Value!;
                result = result with
                {
                    Data = result.Data.Select(d => d with
                    {
                        OnboardingStatus = ComputeOnboardingStatus(d.AccountStatus, invitationStatuses.GetValueOrDefault(d.UserId))
                    }).ToList()
                };
            }
        }

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

        var projectId = User.GetActiveProjectId();
        model.HasActiveProject = projectId.HasValue;
        model.ActiveProjectName = User.FindFirst("ActiveProjectName")?.Value;

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model, CancellationToken ct)
    {
        var projectId = User.GetActiveProjectId();
        if (!projectId.HasValue)
        {
            TempData["ErrorMessage"] = "Seleccione un proyecto desde el selector de contexto para continuar.";
            return await ReturnCreateViewWithCountries(model, ct);
        }

        if (!ModelState.IsValid)
        {
            return await ReturnCreateViewWithCountries(model, ct);
        }

        var contextInfo = await _executor.SendAndLogIfFailureAsync(
            new GetProjectContextInfoQuery(projectId.Value), ct);

        if (contextInfo.IsFailure)
        {
            TempData["ErrorMessage"] = "Error al resolver el contexto del proyecto.";
            return await ReturnCreateViewWithCountries(model, ct);
        }

        var userId = User.GetUserId();
        var command = new CreateUserCommand(
            model.Email,
            model.Country,
            model.Identification,
            model.FirstName,
            model.LastName,
            model.SkipEmailVerification,
            model.SkipInvitationAcceptance,
            contextInfo.Value!.ProjectExternalId,
            userId ?? 0);

        var result = await _executor.SendAndLogIfFailureAsync(command, ct);

        if (result.IsSuccess)
        {
            var value = result.Value!;
            if (value.TemporaryPassword is not null)
            {
                TempData["SuccessMessage"] = $"Usuario creado exitosamente. Contraseña temporal: {value.TemporaryPassword}";
                TempData["TemporaryPassword"] = value.TemporaryPassword;
            }
            else
            {
                var statusMessage = value.Outcome switch
                {
                    CreateUserOutcome.Created => "Usuario creado exitosamente. Se iniciará el proceso de incorporación.",
                    CreateUserOutcome.Enrolled => "Usuario existente inscrito en el proyecto.",
                    CreateUserOutcome.AlreadyEnrolled => "El usuario ya está inscrito en este proyecto.",
                    _ => "Operación completada."
                };
                TempData["SuccessMessage"] = statusMessage;
            }

            if (value.Warnings.Count > 0)
            {
                TempData["WarningMessage"] = string.Join(" ", value.Warnings);
            }

            return RedirectToAction(nameof(Index));
        }

        foreach (var error in result.ErrorMessages ?? [])
        {
            ModelState.AddModelError(error.Context, error.Message);
        }

        return await ReturnCreateViewWithCountries(model, ct);
    }

    [HttpGet]
    public IActionResult Enroll()
    {
        return RedirectToAction(nameof(Create));
    }

    [HttpGet]
    public IActionResult RegisterInternal()
    {
        return RedirectToAction(nameof(Create));
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReissueInvitation(Guid userExternalId, CancellationToken ct)
    {
        var projectId = User.GetActiveProjectId();
        if (!projectId.HasValue)
        {
            TempData["ErrorMessage"] = "Seleccione un proyecto para reenviar la invitación.";
            return RedirectToAction(nameof(Index));
        }

        var userResult = await _executor.SendAndLogIfFailureAsync(
            new Mentoory.Access.Application.Queries.GetUserByExternalId.GetUserByExternalIdQuery(userExternalId), ct);

        if (userResult.IsFailure || userResult.Value is null)
        {
            TempData["ErrorMessage"] = "Usuario no encontrado.";
            return RedirectToAction(nameof(Index));
        }

        var pendingInvitation = await _executor.SendAndLogIfFailureAsync(
            new GetPendingInvitationForUserQuery(userResult.Value.Id, projectId.Value), ct);

        if (pendingInvitation.IsFailure || pendingInvitation.Value is null)
        {
            TempData["ErrorMessage"] = "No se encontró una invitación pendiente para este usuario.";
            return RedirectToAction(nameof(Index));
        }

        var reissueResult = await _executor.SendAndLogIfFailureAsync(
            new ReissueInvitationCommand(pendingInvitation.Value.Value), ct);

        if (reissueResult.IsSuccess)
        {
            TempData["SuccessMessage"] = "Invitación reenviada exitosamente.";
        }
        else
        {
            TempData["ErrorMessage"] = reissueResult.ErrorMessages?.FirstOrDefault().Message ?? "Error al reenviar la invitación.";
        }

        return RedirectToAction(nameof(Index));
    }

    private static string ComputeOnboardingStatus(string accountStatus, string? invitationStatus)
    {
        var isPendingVerification = accountStatus == "PendingVerification";
        var isPendingInvitation = invitationStatus == "Pending";

        if (isPendingVerification && isPendingInvitation)
        {
            return "Pendiente ambos";
        }

        if (isPendingVerification)
        {
            return "Pendiente verificación";
        }

        if (isPendingInvitation)
        {
            return "Pendiente invitación";
        }

        return "Activo";
    }

    private async Task<IActionResult> ReturnCreateViewWithCountries(CreateUserViewModel model, CancellationToken ct)
    {
        var countriesResult = await _executor.SendAndLogIfFailureAsync(new ListCountriesQuery(), ct);
        model.Countries = countriesResult.IsSuccess ? countriesResult.Value! : [];
        model.HasActiveProject = User.GetActiveProjectId().HasValue;
        model.ActiveProjectName = User.FindFirst("ActiveProjectName")?.Value;
        return View("Create", model);
    }
}
