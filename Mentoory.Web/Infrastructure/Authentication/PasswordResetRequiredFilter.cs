using Mentoory.Access.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Mentoory.Web.Infrastructure.Authentication;

public class PasswordResetRequiredFilter : IAsyncActionFilter
{
    private static readonly HashSet<string> ExcludedPaths =
    [
        "/Access/ChangePassword",
        "/Access/Logout",
    ];

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            await next();
            return;
        }

        var requestPath = context.HttpContext.Request.Path.Value ?? string.Empty;

        // Exclude static resources
        if (requestPath.StartsWith("/lib/", StringComparison.OrdinalIgnoreCase)
            || requestPath.StartsWith("/css/", StringComparison.OrdinalIgnoreCase)
            || requestPath.StartsWith("/js/", StringComparison.OrdinalIgnoreCase)
            || requestPath.StartsWith("/images/", StringComparison.OrdinalIgnoreCase)
            || requestPath.StartsWith("/_", StringComparison.OrdinalIgnoreCase))
        {
            await next();
            return;
        }

        // Exclude allowed paths
        foreach (var excluded in ExcludedPaths)
        {
            if (requestPath.StartsWith(excluded, StringComparison.OrdinalIgnoreCase))
            {
                await next();
                return;
            }
        }

        var accountStatus = context.HttpContext.User.FindFirst("AccountStatus")?.Value;
        if (accountStatus == AccountStatus.PasswordResetRequired.ToString())
        {
            context.Result = new RedirectToActionResult("Index", "ChangePassword", new { area = "Access" });
            return;
        }

        await next();
    }
}
