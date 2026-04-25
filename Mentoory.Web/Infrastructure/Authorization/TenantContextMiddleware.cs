using System.Security.Claims;
using Mentoory.Shared.Application.Interfaces;
using Mentoory.Shared.Infrastructure.Services;

namespace Mentoory.Web.Infrastructure.Authorization;

public class TenantContextMiddleware
{
    private readonly RequestDelegate _next;

    public TenantContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        if (context.User.Identity?.IsAuthenticated == true
            && tenantContext is TenantContextService tenantService)
        {
            var incubatorId = context.User.GetActiveIncubatorId();
            if (incubatorId > 0)
            {
                tenantService.CurrentIncubatorId = incubatorId;
            }

            tenantService.UserId = context.User.GetUserId();
            tenantService.UserEmail = context.User.FindFirstValue(ClaimTypes.Email);
            tenantService.ProjectId = context.User.GetActiveProjectId();
            tenantService.Role = context.User.GetActiveRole();
        }

        await _next(context);
    }
}
