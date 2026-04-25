using Microsoft.AspNetCore.Builder;

namespace Mentoory.Web.Infrastructure.Correlation;

public static class CorrelationMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelation(this IApplicationBuilder app) =>
        app.UseMiddleware<CorrelationMiddleware>();
}
