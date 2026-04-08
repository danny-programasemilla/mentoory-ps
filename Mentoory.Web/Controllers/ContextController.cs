using System.Security.Claims;
using Mentoory.Access.Application.Commands.SetActiveContext;
using Mentoory.Access.Application.Queries.GetUserContexts;
using Mentoory.Access.Domain.ReadModels;
using Mentoory.Shared.Domain.Constants;
using Mentoory.Tenant.Application.Queries.ListIncubatorContextOptions;
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
            return RedirectToAction("Login", "Login", new { area = "Access" });
        }

        var contexts = await _executor.SendOrThrowAsync(new GetUserContextsQuery(userId.Value), ct);

        if (contexts.Count == 0)
        {
            return RedirectToAction("Index", "AvailableProjects");
        }

        if (contexts.Count == 1 && contexts[0].Role == Roles.GlobalAdmin)
        {
            var globalAdminContexts = await BuildGlobalAdminContextsAsync(contexts[0], ct);

            if (globalAdminContexts.Count == 0)
            {
                return await SetContext(contexts[0].RoleAssignmentExternalId, returnUrl, ct);
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(globalAdminContexts);
        }

        if (contexts.Count == 1)
        {
            return await SetContext(contexts[0].RoleAssignmentExternalId, returnUrl, ct);
        }

        ViewBag.ReturnUrl = returnUrl;
        return View(contexts);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Select(
        Guid roleAssignmentExternalId,
        string? returnUrl,
        long? selectedIncubatorId,
        string? selectedIncubatorName,
        long? selectedProjectId,
        string? selectedProjectName,
        CancellationToken ct)
    {
        if (selectedIncubatorId.HasValue)
        {
            return await SetGlobalAdminContext(
                roleAssignmentExternalId,
                selectedIncubatorId.Value,
                selectedIncubatorName,
                selectedProjectId,
                selectedProjectName,
                returnUrl,
                ct);
        }

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
            return RedirectToAction("Login", "Login", new { area = "Access" });
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

    private async Task<List<UserContext>> BuildGlobalAdminContextsAsync(
        UserContext globalContext, CancellationToken ct)
    {
        var options = await _executor.SendOrThrowAsync(
            new ListIncubatorContextOptionsQuery(), ct);

        var contexts = new List<UserContext>();

        foreach (var incubator in options)
        {
            contexts.Add(globalContext with
            {
                IncubatorId = incubator.IncubatorId,
                IncubatorName = incubator.IncubatorName,
                ProjectId = null,
                ProjectName = null,
            });

            foreach (var project in incubator.Projects)
            {
                contexts.Add(globalContext with
                {
                    IncubatorId = incubator.IncubatorId,
                    IncubatorName = incubator.IncubatorName,
                    ProjectId = project.ProjectId,
                    ProjectName = project.ProjectName,
                });
            }
        }

        return contexts;
    }

    private async Task<IActionResult> SetGlobalAdminContext(
        Guid roleAssignmentExternalId,
        long incubatorId,
        string? incubatorName,
        long? projectId,
        string? projectName,
        string? returnUrl,
        CancellationToken ct)
    {
        var userId = TryGetUserId();
        if (userId is null)
        {
            return RedirectToAction("Login", "Login", new { area = "Access" });
        }

        var baseContext = await _executor.SendOrThrowAsync(
            new SetActiveContextCommand(userId.Value, roleAssignmentExternalId), ct);

        if (baseContext.Role != Roles.GlobalAdmin)
        {
            return RedirectToAction("Login", "Login", new { area = "Access" });
        }

        var context = baseContext with
        {
            IncubatorId = incubatorId,
            IncubatorName = incubatorName,
            ProjectId = projectId,
            ProjectName = projectName,
        };

        await UpdateAuthCookie(context);

        if (IsValidLocalUrl(returnUrl))
        {
            return Redirect(returnUrl!);
        }

        return RedirectToAction("Index", "Home");
    }

    private async Task UpdateAuthCookie(UserContext context)
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
