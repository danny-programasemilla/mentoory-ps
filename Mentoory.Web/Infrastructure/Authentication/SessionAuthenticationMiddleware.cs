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
        // Session validation will be implemented in T032/T034 when IdentityDbContext and AuthSessionRepository are available.
        // For now, the standard cookie authentication middleware handles the auth cookie.
        await _next(context);
    }
}
