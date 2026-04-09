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
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var incubatorId = context.User.GetActiveIncubatorId();
            if (incubatorId > 0 && tenantContext is TenantContextService tenantService)
            {
                tenantService.CurrentIncubatorId = incubatorId;
            }
        }

        await _next(context);
    }
}
