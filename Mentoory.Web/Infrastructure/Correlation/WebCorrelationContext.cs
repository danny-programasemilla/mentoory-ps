using Mentoory.Shared.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Mentoory.Web.Infrastructure.Correlation;

/// <summary>
/// <see cref="ICorrelationContext"/> implementation backed by <see cref="IHttpContextAccessor"/>.
/// Expects <see cref="CorrelationMiddleware"/> to have already populated
/// <c>HttpContext.Items[CorrelationItemKey]</c> for the current request.
/// </summary>
public sealed class WebCorrelationContext : ICorrelationContext
{
    internal const string CorrelationItemKey = "__Mentoory.CorrelationId";

    private readonly IHttpContextAccessor _accessor;

    public WebCorrelationContext(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public Guid CorrelationId
    {
        get
        {
            var ctx = _accessor.HttpContext;
            if (ctx is not null
                && ctx.Items.TryGetValue(CorrelationItemKey, out var value)
                && value is Guid guid)
            {
                return guid;
            }

            return Guid.NewGuid();
        }
    }

    public string? ClientIpAddress => _accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}
