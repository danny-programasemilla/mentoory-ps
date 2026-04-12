using System.Security.Claims;
using Mentoory.Access.Application.Commands.SetActiveContext;
using Mentoory.Access.Application.Queries.GetUserContexts;
using Mentoory.Access.Application.Queries.ListContextIncubators;
using Mentoory.Access.Application.Queries.ListContextProjects;
using Mentoory.Access.Application.Queries.ListContextRoles;
using Mentoory.Access.Domain.ReadModels;
using Mentoory.Shared.Domain.Constants;
using Mentoory.Tenant.Application.Queries.ListIncubatorContextOptions;
using Mentoory.Web.Infrastructure;
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
        var userId = User.GetUserId();
        if (userId is null)
        {
            return RedirectToAction("Login", "Login", new { area = "Access" });
        }

        var contexts = await _executor.SendOrThrowAsync(new GetUserContextsQuery(userId.Value), ct);

        if (contexts.Count == 0)
        {
            return RedirectToAction("Index", "AvailableProjects");
        }

        // Auto-skip: single non-GlobalAdmin context
        if (contexts.Count == 1 && contexts[0].Role != Roles.GlobalAdmin)
        {
            return await SetContext(contexts[0].RoleAssignmentExternalId, returnUrl, ct);
        }

        // Auto-skip: single GlobalAdmin with no incubators to browse
        if (contexts.Count == 1 && contexts[0].Role == Roles.GlobalAdmin)
        {
            var options = await _executor.SendOrThrowAsync(
                new ListIncubatorContextOptionsQuery(), ct);

            if (options.Count == 0)
            {
                return await SetContext(contexts[0].RoleAssignmentExternalId, returnUrl, ct);
            }
        }

        ViewBag.ReturnUrl = returnUrl;
        return View();
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

    [HttpGet]
    [Route("api/context/roles")]
    public async Task<IActionResult> GetRoles(CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized(new { message = "Sesión inválida." });
        }

        var roles = await _executor.SendOrThrowAsync(
            new ListContextRolesQuery(userId.Value), ct);

        return Ok(roles);
    }

    [HttpGet]
    [Route("api/context/incubators")]
    public async Task<IActionResult> GetIncubators([FromQuery] string role, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized(new { message = "Sesión inválida." });
        }

        if (string.IsNullOrWhiteSpace(role) || !Roles.All.Contains(role))
        {
            return BadRequest(new { message = "Rol inválido." });
        }

        var (contexts, options) = await LoadCascadeDataAsync(userId.Value, ct);
        var incubatorNames = options.ToDictionary(o => o.IncubatorId, o => o.IncubatorName);

        if (role == Roles.GlobalAdmin)
        {
            var ga = contexts.FirstOrDefault(c => c.Role == Roles.GlobalAdmin);
            if (ga is null)
            {
                return Ok(Array.Empty<ContextIncubatorDto>());
            }

            return Ok(options
                .Select(o => new ContextIncubatorDto(o.IncubatorId, o.IncubatorName, ga.RoleAssignmentExternalId))
                .ToList());
        }

        var result = contexts
            .Where(c => c.Role == role)
            .GroupBy(a => a.IncubatorId)
            .Select(g => new ContextIncubatorDto(
                g.Key,
                incubatorNames.GetValueOrDefault(g.Key, $"Incubadora #{g.Key}"),
                g.First().RoleAssignmentExternalId))
            .OrderBy(i => i.Name)
            .ToList();

        return Ok(result);
    }

    [HttpGet]
    [Route("api/context/projects")]
    public async Task<IActionResult> GetProjects(
        [FromQuery] string role,
        [FromQuery] long incubatorId,
        CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized(new { message = "Sesión inválida." });
        }

        if (string.IsNullOrWhiteSpace(role) || !Roles.All.Contains(role))
        {
            return BadRequest(new { message = "Rol inválido." });
        }

        var (contexts, options) = await LoadCascadeDataAsync(userId.Value, ct);
        var incubatorOption = options.FirstOrDefault(o => o.IncubatorId == incubatorId);

        if (role == Roles.GlobalAdmin)
        {
            var ga = contexts.FirstOrDefault(c => c.Role == Roles.GlobalAdmin);
            if (ga is null || incubatorOption is null)
            {
                return Ok(Array.Empty<ContextProjectDto>());
            }

            return Ok(incubatorOption.Projects
                .Select(p => new ContextProjectDto(p.ProjectId, p.ProjectName, ga.RoleAssignmentExternalId))
                .ToList());
        }

        var projectNames = incubatorOption?.Projects
            .ToDictionary(p => p.ProjectId, p => p.ProjectName)
            ?? [];

        var result = contexts
            .Where(c => c.Role == role && c.IncubatorId == incubatorId && c.ProjectId.HasValue)
            .Select(c => new ContextProjectDto(
                c.ProjectId!.Value,
                projectNames.GetValueOrDefault(c.ProjectId.Value, $"Proyecto #{c.ProjectId.Value}"),
                c.RoleAssignmentExternalId))
            .OrderBy(p => p.Name)
            .ToList();

        return Ok(result);
    }

    [HttpPost]
    [Route("api/context/switch")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Switch([FromBody] ContextSwitchRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();
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

        if (context.Role == Roles.GlobalAdmin
            && request.IncubatorId.HasValue)
        {
            context = context with
            {
                IncubatorId = request.IncubatorId.Value,
                IncubatorName = request.IncubatorName,
                ProjectId = request.ProjectId,
                ProjectName = request.ProjectName,
            };
        }

        await UpdateAuthCookie(context);

        return Ok(new { message = "Contexto actualizado exitosamente." });
    }

    private static bool IsValidLocalUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        if (url.StartsWith('/') && !url.StartsWith("//") && !url.StartsWith("/\\"))
        {
            return true;
        }

        return false;
    }

    private async Task<(List<UserContext> Contexts, List<IncubatorContextOptionDto> Options)> LoadCascadeDataAsync(
        long userId, CancellationToken ct)
    {
        var contextsTask = _executor.SendOrThrowAsync(new GetUserContextsQuery(userId), ct);
        var optionsTask = _executor.SendOrThrowAsync(new ListIncubatorContextOptionsQuery(), ct);
        await Task.WhenAll(contextsTask, optionsTask);
        return (contextsTask.Result, optionsTask.Result);
    }

    private async Task<IActionResult> SetContext(Guid roleAssignmentExternalId, string? returnUrl, CancellationToken ct)
    {
        var userId = User.GetUserId();
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

    private async Task<IActionResult> SetGlobalAdminContext(
        Guid roleAssignmentExternalId,
        long incubatorId,
        string? incubatorName,
        long? projectId,
        string? projectName,
        string? returnUrl,
        CancellationToken ct)
    {
        var userId = User.GetUserId();
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
}

public sealed record ContextSwitchRequest(
    Guid RoleAssignmentExternalId,
    long? IncubatorId = null,
    string? IncubatorName = null,
    long? ProjectId = null,
    string? ProjectName = null);
