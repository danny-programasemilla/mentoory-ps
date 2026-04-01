using System.Security.Claims;
using Mentoory.Authorization.Application.Commands.SetActiveContext;
using Mentoory.Authorization.Application.Queries.GetUserContexts;
using Mentoory.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mentoory.Web.Controllers;

[Authorize]
public class ContextController : Controller
{
    private readonly MediatRExecutor _executor;

    public ContextController(MediatRExecutor executor)
    {
        _executor = executor;
    }

    [HttpGet]
    public async Task<IActionResult> Select(string? returnUrl, CancellationToken ct)
    {
        var userId = TryGetUserId();
        if (userId is null)
        {
            return RedirectToAction("Login", "Login", new { area = "Identity" });
        }

        var contexts = await _executor.SendOrThrowAsync(new GetUserContextsQuery(userId.Value), ct);

        if (contexts.Count == 1)
        {
            return await SetContext(contexts[0].RoleAssignmentExternalId, returnUrl, ct);
        }

        ViewBag.ReturnUrl = returnUrl;
        return View(contexts);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Select(Guid roleAssignmentExternalId, string? returnUrl, CancellationToken ct)
    {
        return await SetContext(roleAssignmentExternalId, returnUrl, ct);
    }

    [HttpPost]
    [Route("api/context/switch")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Switch([FromBody] ContextSwitchRequest request, CancellationToken ct)
    {
        var userId = TryGetUserId();
        if (userId is null)
        {
            return Unauthorized(new { message = "Sesión inválida." });
        }

        var result = await _executor.SendAndLogIfFailureAsync(
            new SetActiveContextCommand(userId.Value, request.RoleAssignmentExternalId), ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = "No se pudo cambiar el contexto." });
        }

        var context = result.Value!;
        await UpdateAuthCookie(context);

        return Ok(new { message = "Contexto actualizado exitosamente." });
    }

    private static bool IsValidLocalUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        // Only allow relative paths (local URLs)
        // Reject absolute URIs, protocol-relative URLs (//), and backslash tricks
        if (url.StartsWith('/') && !url.StartsWith("//") && !url.StartsWith("/\\"))
        {
            return true;
        }

        return false;
    }

    private async Task<IActionResult> SetContext(Guid roleAssignmentExternalId, string? returnUrl, CancellationToken ct)
    {
        var userId = TryGetUserId();
        if (userId is null)
        {
            return RedirectToAction("Login", "Login", new { area = "Identity" });
        }

        var context = await _executor.SendOrThrowAsync(
            new SetActiveContextCommand(userId.Value, roleAssignmentExternalId), ct);

        await UpdateAuthCookie(context);

        if (IsValidLocalUrl(returnUrl))
        {
            return Redirect(returnUrl!);
        }

        return RedirectToAction("Index", "Home");
    }

    private async Task UpdateAuthCookie(Authorization.Domain.ReadModels.UserContext context)
    {
        var existingClaims = User.Claims
            .Where(c => c.Type != "ActiveRole"
                        && c.Type != "ActiveIncubatorId"
                        && c.Type != "ActiveProjectId"
                        && c.Type != "ActiveIncubatorName"
                        && c.Type != "ActiveProjectName")
            .ToList();

        var claims = new List<Claim>(existingClaims)
        {
            new("ActiveRole", context.Role),
            new("ActiveIncubatorId", context.IncubatorId.ToString()),
        };

        if (!string.IsNullOrEmpty(context.IncubatorName))
        {
            claims.Add(new Claim("ActiveIncubatorName", context.IncubatorName));
        }

        if (context.ProjectId.HasValue)
        {
            claims.Add(new Claim("ActiveProjectId", context.ProjectId.Value.ToString()));
        }

        if (!string.IsNullOrEmpty(context.ProjectName))
        {
            claims.Add(new Claim("ActiveProjectName", context.ProjectName));
        }

        // Ensure the role claim is present for [Authorize(Roles = "...")] checks
        if (!claims.Any(c => c.Type == ClaimTypes.Role && c.Value == context.Role))
        {
            claims.Add(new Claim(ClaimTypes.Role, context.Role));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal);
    }

    private long? TryGetUserId()
    {
        return long.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id)
            ? id
            : null;
    }
}

public sealed record ContextSwitchRequest(Guid RoleAssignmentExternalId);
