using Mentoory.Access.Application.Commands.SetInitialPassword;
using Mentoory.Access.Application.Queries.GetUserByExternalId;
using Mentoory.Access.Application.Queries.GetUserById;
using Mentoory.Access.Domain.Enums;
using Mentoory.Shared.Application.TimeProvider;
using Mentoory.Tenant.Application.Invitations.Commands.AcceptInvitation;
using Mentoory.Tenant.Application.Invitations.Queries.GetInvitationDetails;
using Mentoory.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mentoory.Web.Areas.Access.Controllers;

[Area("Access")]
[AllowAnonymous]
public class OnboardingController : Controller
{
    private readonly MediatRExecutor _executor;
    private readonly ITimeProvider _timeProvider;

    public OnboardingController(MediatRExecutor executor, ITimeProvider timeProvider)
    {
        _executor = executor;
        _timeProvider = timeProvider;
    }

    [HttpGet]
    public async Task<IActionResult> VerifyEmail(string? token, Guid? userExternalId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token) || !userExternalId.HasValue)
        {
            return RedirectToAction(nameof(Expired));
        }

        // Validate user and token via query
        var userResult = await _executor.SendAndLogIfFailureAsync(
            new GetUserByExternalIdQuery(userExternalId.Value), ct);

        if (userResult.IsFailure || userResult.Value is null)
        {
            return RedirectToAction(nameof(Expired));
        }

        var user = userResult.Value;
        var verificationToken = user.EmailVerificationTokens
            .FirstOrDefault(t => t.TokenHash == token);

        if (verificationToken is null || verificationToken.IsUsed)
        {
            return RedirectToAction(nameof(Expired));
        }

        if (_timeProvider.UtcNow >= verificationToken.ExpiresAtUtc)
        {
            return RedirectToAction(nameof(Expired));
        }

        ViewBag.Token = token;
        ViewBag.UserExternalId = user.ExternalId;
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

        var result = await _executor.SendAndLogIfFailureAsync(command, ct);

        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = "Su correo ha sido verificado y su contrasena configurada exitosamente.";
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

        var detailsResult = await _executor.SendAndLogIfFailureAsync(
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

        if (_timeProvider.UtcNow >= details.ExpiresAtUtc)
        {
            return RedirectToAction(nameof(Expired));
        }

        // Resolve user via query instead of direct repository access
        var userResult = await _executor.SendAndLogIfFailureAsync(
            new GetUserByIdQuery(details.UserId), ct);

        if (userResult.IsFailure || userResult.Value is null)
        {
            return RedirectToAction(nameof(Expired));
        }

        var user = userResult.Value;
        ViewBag.InvitationExternalId = invitationExternalId;
        ViewBag.UserExternalId = user.ExternalId;
        ViewBag.NeedsPassword = user.GetActiveCredential() is null
                                || user.AccountStatus == AccountStatus.PasswordResetRequired;

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AcceptInvitation(
        Guid invitationExternalId,
        Guid userExternalId,
        string? newPassword,
        string? confirmPassword,
        bool needsPassword,
        CancellationToken ct)
    {
        // Set password if needed
        if (needsPassword && !string.IsNullOrWhiteSpace(newPassword))
        {
            var passwordCommand = new SetInitialPasswordCommand(
                userExternalId,
                invitationExternalId.ToString(),
                TokenType.Invitation,
                newPassword,
                confirmPassword ?? string.Empty);

            var passwordResult = await _executor.SendAndLogIfFailureAsync(passwordCommand, ct);

            if (passwordResult.IsFailure)
            {
                return ReturnAcceptInvitationView(invitationExternalId, userExternalId, true, passwordResult.ErrorMessages);
            }
        }

        // Resolve userId from userExternalId server-side (never trust client-provided internal IDs)
        var userResult = await _executor.SendAndLogIfFailureAsync(
            new GetUserByExternalIdQuery(userExternalId), ct);

        if (userResult.IsFailure || userResult.Value is null)
        {
            return ReturnAcceptInvitationView(invitationExternalId, userExternalId, needsPassword, null);
        }

        var acceptResult = await _executor.SendAndLogIfFailureAsync(
            new AcceptInvitationCommand(invitationExternalId, userResult.Value.Id), ct);

        if (acceptResult.IsSuccess)
        {
            TempData["SuccessMessage"] = "Invitacion aceptada exitosamente. Ya puede iniciar sesion.";
            return RedirectToAction("Index", "Login", new { area = "Access" });
        }

        return ReturnAcceptInvitationView(invitationExternalId, userExternalId, needsPassword, acceptResult.ErrorMessages);
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
