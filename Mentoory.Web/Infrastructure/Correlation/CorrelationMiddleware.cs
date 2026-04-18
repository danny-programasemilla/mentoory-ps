using Microsoft.AspNetCore.Http;

namespace Mentoory.Web.Infrastructure.Correlation;

/// <summary>
/// Stamps every request with a correlation id, accepting the incoming
/// <c>X-Correlation-Id</c> header when it is a valid GUID and falling back to a freshly
/// generated one otherwise. The id is attached to <see cref="HttpContext.Items"/> for
/// downstream <see cref="WebCorrelationContext"/> reads and echoed back in the response.
/// Must run BEFORE authentication so authentication failures are still correlated.
/// </summary>
public sealed class CorrelationMiddleware
{
    public const string HeaderName = "X-Correlation-Id";

    private readonly RequestDelegate _next;

    public CorrelationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        var id = TryParseIncoming(context.Request.Headers) ?? Guid.NewGuid();
        context.Items[WebCorrelationContext.CorrelationItemKey] = id;
        context.Response.Headers[HeaderName] = id.ToString();
        return _next(context);
    }

    private static Guid? TryParseIncoming(IHeaderDictionary headers)
    {
        if (!headers.TryGetValue(HeaderName, out var values))
        {
            return null;
        }

        var raw = values.ToString();
        return Guid.TryParse(raw, out var parsed) ? parsed : null;
    }
}
