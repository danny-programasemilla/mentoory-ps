using Mentoory.Access.Application.Commands.SetInitialPassword;
using Mentoory.Access.Application.Queries.GetUserOnboardingInfo;
using Mentoory.Access.Application.Queries.ValidateVerificationToken;
using Mentoory.Access.Domain.Enums;
using Mentoory.Tenant.Application.Invitations.Commands.AcceptInvitation;
using Mentoory.Tenant.Application.Invitations.Queries.GetInvitationDetails;
using Mentoory.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mentoory.Web.Areas.Access.Controllers;

[Area("Access")]
[AllowAnonymous]
public class OnboardingController(MediatRExecutor executor) : Controller
{
    [HttpGet]
    public async Task<IActionResult> VerifyEmail(string? token, Guid? userExternalId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token) || !userExternalId.HasValue)
        {
            return RedirectToAction(nameof(Expired));
        }

        var result = await executor.SendAndLogIfFailureAsync(
            new ValidateVerificationTokenQuery(userExternalId.Value, token), ct);

        if (result.IsFailure || result.Value is null)
        {
            return RedirectToAction(nameof(Expired));
        }

        ViewBag.Token = token;
        ViewBag.UserExternalId = result.Value.UserExternalId;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyEmail(
        Guid userExternalId,
        string token,
        string newPassword,
        string confirmPassword,
        CancellationToken ct)
    {
        var command = new SetInitialPasswordCommand(
            userExternalId,
            token,
            TokenType.Verification,
            newPassword,
            confirmPassword);

        var result = await executor.SendAndLogIfFailureAsync(command, ct);

        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = "Su correo ha sido verificado y su contraseña configurada exitosamente.";
            return RedirectToAction("Index", "Login", new { area = "Access" });
        }

        ViewBag.Token = token;
        ViewBag.UserExternalId = userExternalId;

        foreach (var error in result.ErrorMessages ?? [])
        {
            ModelState.AddModelError(error.Context, error.Message);
        }

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> AcceptInvitation(string? token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token) || !Guid.TryParse(token, out var invitationExternalId))
        {
            return RedirectToAction(nameof(Expired));
        }

        var detailsResult = await executor.SendAndLogIfFailureAsync(
            new GetInvitationDetailsQuery(invitationExternalId), ct);

        if (detailsResult.IsFailure)
        {
            return RedirectToAction(nameof(Expired));
        }

        var details = detailsResult.Value!;
        if (details.Status != "Pending" || !details.IsActive)
        {
            return RedirectToAction(nameof(Expired));
        }

        var userResult = await executor.SendAndLogIfFailureAsync(
            new GetUserOnboardingInfoByIdQuery(details.UserId), ct);

        if (userResult.IsFailure || userResult.Value is null)
        {
            return RedirectToAction(nameof(Expired));
        }

        var userInfo = userResult.Value;
        ViewBag.InvitationExternalId = invitationExternalId;
        ViewBag.UserExternalId = userInfo.UserExternalId;
        ViewBag.NeedsPassword = userInfo.NeedsPassword;

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AcceptInvitation(
        Guid invitationExternalId,
        Guid userExternalId,
        string? newPassword,
        string? confirmPassword,
        CancellationToken ct)
    {
        // Validate invitation server-side
        var detailsResult = await executor.SendAndLogIfFailureAsync(
            new GetInvitationDetailsQuery(invitationExternalId), ct);

        if (detailsResult.IsFailure || detailsResult.Value is null
            || detailsResult.Value.Status != "Pending" || !detailsResult.Value.IsActive)
        {
            return RedirectToAction(nameof(Expired));
        }

        // Re-derive needsPassword server-side
        var userResult = await executor.SendAndLogIfFailureAsync(
            new GetUserOnboardingInfoByExternalIdQuery(userExternalId), ct);

        if (userResult.IsFailure || userResult.Value is null)
        {
            return ReturnAcceptInvitationView(invitationExternalId, userExternalId, false, null);
        }

        var userInfo = userResult.Value;

        // Set password if needed
        if (userInfo.NeedsPassword && !string.IsNullOrWhiteSpace(newPassword))
        {
            var passwordCommand = new SetInitialPasswordCommand(
                userExternalId,
                invitationExternalId.ToString(),
                TokenType.Invitation,
                newPassword,
                confirmPassword ?? string.Empty);

            var passwordResult = await executor.SendAndLogIfFailureAsync(passwordCommand, ct);

            if (passwordResult.IsFailure)
            {
                return ReturnAcceptInvitationView(
                    invitationExternalId, userExternalId, true, passwordResult.ErrorMessages);
            }
        }

        var acceptResult = await executor.SendAndLogIfFailureAsync(
            new AcceptInvitationCommand(invitationExternalId, userInfo.UserId), ct);

        if (acceptResult.IsSuccess)
        {
            TempData["SuccessMessage"] = "Invitación aceptada exitosamente. Ya puede iniciar sesión.";
            return RedirectToAction("Index", "Login", new { area = "Access" });
        }

        return ReturnAcceptInvitationView(
            invitationExternalId, userExternalId, userInfo.NeedsPassword, acceptResult.ErrorMessages);
    }

    [HttpGet]
    public IActionResult Expired()
    {
        return View();
    }

    private IActionResult ReturnAcceptInvitationView(
        Guid invitationExternalId,
        Guid userExternalId,
        bool needsPassword,
        IReadOnlyList<(string Context, string Message)>? errors)
    {
        ViewBag.InvitationExternalId = invitationExternalId;
        ViewBag.UserExternalId = userExternalId;
        ViewBag.NeedsPassword = needsPassword;

        if (errors is not null)
        {
            foreach (var error in errors)
            {
                ModelState.AddModelError(error.Context, error.Message);
            }
        }

        return View("AcceptInvitation");
    }
}
