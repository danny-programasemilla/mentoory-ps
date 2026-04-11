namespace Mentoory.Web.Infrastructure.Authentication;

public static class AuthenticationMiddlewareExtensions
{
    public static IApplicationBuilder UseSessionAuthentication(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SessionAuthenticationMiddleware>();
    }
}
