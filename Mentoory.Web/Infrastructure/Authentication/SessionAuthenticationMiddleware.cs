using System.Security.Claims;
using Mentoory.Access.Domain.Enums;
using Mentoory.Access.Domain.Repositories;
using Microsoft.AspNetCore.Authentication;

namespace Mentoory.Web.Infrastructure.Authentication;

public class SessionAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SessionAuthenticationMiddleware> _logger;

    public SessionAuthenticationMiddleware(RequestDelegate next, ILogger<SessionAuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var sessionToken = context.User.FindFirstValue("SessionToken");
        if (string.IsNullOrEmpty(sessionToken))
        {
            await _next(context);
            return;
        }

        var sessionRepository = context.RequestServices.GetRequiredService<IAuthSessionRepository>();
        var userRepository = context.RequestServices.GetRequiredService<IUserRepository>();

        var session = await sessionRepository.GetByTokenAsync(sessionToken, context.RequestAborted);
        if (session is null || !session.IsValid(DateTime.UtcNow))
        {
            _logger.LogWarning("Invalid or expired session token for user {UserId}", context.User.FindFirstValue(ClaimTypes.NameIdentifier));
            await SignOutAndRedirect(context);
            return;
        }

        // Load current user to check AccountStatus
        var user = await userRepository.GetByIdAsync(session.UserId, context.RequestAborted);
        if (user is null || user.AccountStatus == AccountStatus.Disabled)
        {
            _logger.LogWarning("User {UserId} is disabled or not found, invalidating session", session.UserId);
            session.Deactivate();
            sessionRepository.Update(session);
            await sessionRepository.UnitOfWork.SaveEntitiesAsync(context.RequestAborted);
            await SignOutAndRedirect(context);
            return;
        }

        // Refresh AccountStatus claim if it changed (e.g., admin set PasswordResetRequired mid-session)
        var currentStatusClaim = context.User.FindFirstValue("AccountStatus");
        var actualStatus = user.AccountStatus.ToString();
        if (currentStatusClaim != actualStatus)
        {
            var identity = context.User.Identity as ClaimsIdentity;
            if (identity is not null)
            {
                var existing = identity.FindFirst("AccountStatus");
                if (existing is not null)
                {
                    identity.RemoveClaim(existing);
                }

                identity.AddClaim(new Claim("AccountStatus", actualStatus));
            }
        }

        await _next(context);
    }

    private static async Task SignOutAndRedirect(HttpContext context)
    {
        await context.SignOutAsync();
        context.Response.Redirect("/Access/Login");
    }
}
