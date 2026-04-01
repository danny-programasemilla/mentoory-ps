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
        // SECURITY RISK: Session tokens are created during login but NOT validated server-side
        // on subsequent requests. A stolen or expired session token remains valid until the
        // authentication cookie expires (currently 8 hours).
        // This is a known limitation — server-side session validation will be implemented
        // when IdentityDbContext and AuthSessionRepository are available (deferred work item).
        await _next(context);
    }
}
