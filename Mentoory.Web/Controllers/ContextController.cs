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
    public async Task<IActionResult> Select(CancellationToken ct)
    {
        var userId = GetUserId();
        var contexts = await _executor.SendOrThrowAsync(new GetUserContextsQuery(userId), ct);

        if (contexts.Count == 1)
        {
            return await SetContext(contexts[0].RoleAssignmentExternalId, ct);
        }

        return View(contexts);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Select(Guid roleAssignmentExternalId, CancellationToken ct)
    {
        return await SetContext(roleAssignmentExternalId, ct);
    }

    [HttpPost]
    [Route("api/context/switch")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Switch([FromBody] ContextSwitchRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _executor.SendAndLogIfFailureAsync(
            new SetActiveContextCommand(userId, request.RoleAssignmentExternalId), ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = "No se pudo cambiar el contexto." });
        }

        var context = result.Value!;
        await UpdateAuthCookie(context);

        return Ok(new { message = "Contexto actualizado exitosamente." });
    }

    private async Task<IActionResult> SetContext(Guid roleAssignmentExternalId, CancellationToken ct)
    {
        var userId = GetUserId();
        var context = await _executor.SendOrThrowAsync(
            new SetActiveContextCommand(userId, roleAssignmentExternalId), ct);

        await UpdateAuthCookie(context);

        return RedirectToAction("Index", "Home");
    }

    private async Task UpdateAuthCookie(Authorization.Domain.ReadModels.UserContext context)
    {
        var existingClaims = User.Claims
            .Where(c => c.Type != "ActiveRole"
                        && c.Type != "ActiveIncubatorId"
                        && c.Type != "ActiveProjectId")
            .ToList();

        var claims = new List<Claim>(existingClaims)
        {
            new("ActiveRole", context.Role),
            new("ActiveIncubatorId", context.IncubatorId.ToString()),
        };

        if (context.ProjectId.HasValue)
        {
            claims.Add(new Claim("ActiveProjectId", context.ProjectId.Value.ToString()));
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

    private long GetUserId()
    {
        return long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }
}

public sealed record ContextSwitchRequest(Guid RoleAssignmentExternalId);
