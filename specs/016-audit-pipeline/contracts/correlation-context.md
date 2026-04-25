# Contract: `ICorrelationContext` + `CorrelationMiddleware`

**Layer:** Application (interface) + Web Infrastructure (implementation)
**Visibility:** Public
**Files:**
- `Mentoory.Shared.Application/Interfaces/ICorrelationContext.cs`
- `Mentoory.Web/Infrastructure/Correlation/CorrelationMiddleware.cs`
- `Mentoory.Web/Infrastructure/Correlation/WebCorrelationContext.cs`
- `Mentoory.Shared.Infrastructure/Correlation/AmbientCorrelationContext.cs`

## Interface

```csharp
namespace Mentoory.Shared.Application.Interfaces;

public interface ICorrelationContext
{
    Guid CorrelationId { get; }
    string? ClientIpAddress { get; }
}
```

## Web implementation — `WebCorrelationContext`

```csharp
namespace Mentoory.Web.Infrastructure.Correlation;

public sealed class WebCorrelationContext : ICorrelationContext
{
    internal const string CorrelationItemKey = "__Mentoory.CorrelationId";
    private readonly IHttpContextAccessor _accessor;

    public WebCorrelationContext(IHttpContextAccessor accessor) { _accessor = accessor; }

    public Guid CorrelationId
    {
        get
        {
            if (_accessor.HttpContext?.Items.TryGetValue(CorrelationItemKey, out var v) == true
                && v is Guid guid)
            {
                return guid;
            }
            return Guid.Empty; // should not happen if middleware ran; defensive
        }
    }

    public string? ClientIpAddress => _accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}
```

## Middleware — `CorrelationMiddleware`

```csharp
namespace Mentoory.Web.Infrastructure.Correlation;

public sealed class CorrelationMiddleware
{
    public const string HeaderName = "X-Correlation-Id";
    private readonly RequestDelegate _next;

    public CorrelationMiddleware(RequestDelegate next) { _next = next; }

    public async Task InvokeAsync(HttpContext context)
    {
        var id = TryParseIncoming(context.Request.Headers) ?? Guid.NewGuid();
        context.Items[WebCorrelationContext.CorrelationItemKey] = id;
        context.Response.Headers[HeaderName] = id.ToString();
        await _next(context);
    }

    private static Guid? TryParseIncoming(IHeaderDictionary headers)
    {
        if (!headers.TryGetValue(HeaderName, out var values)) return null;
        var s = values.ToString();
        return Guid.TryParse(s, out var g) ? g : null;
    }
}
```

## Non-web implementation — `AmbientCorrelationContext`

Used in background services and tests.

```csharp
namespace Mentoory.Shared.Infrastructure.Correlation;

public sealed class AmbientCorrelationContext : ICorrelationContext
{
    public Guid CorrelationId
    {
        get
        {
            var id = Activity.Current?.RootId;
            return Guid.TryParse(id, out var g) ? g : Guid.NewGuid();
        }
    }

    public string? ClientIpAddress => null;
}
```

## Registration

**Web app** (`Mentoory.Web/Program.cs`):

```csharp
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICorrelationContext, WebCorrelationContext>();

// …

app.UseMiddleware<CorrelationMiddleware>();   // FIRST — before UseAuthentication
app.UseAuthentication();
// … rest of pipeline unchanged
```

**Background service host** (future): register `AmbientCorrelationContext` as `Scoped` or `Transient` instead of `WebCorrelationContext`.

**Tests**: `IntegrationTestBase` registers `AmbientCorrelationContext` for non-HTTP command dispatch; HTTP-driven integration tests use `WebCorrelationContext` via `WebApplicationFactory`.

## Guarantees

- Middleware attaches the correlation id to `HttpContext.Items` BEFORE `UseAuthentication()`, so failed authentication still produces auditable entries (once authentication itself becomes audited in a future feature) with a valid correlation id.
- The same `CorrelationId` returned by `ICorrelationContext.CorrelationId` within one HTTP request → two commands dispatched during one request share that id.
- Invalid `X-Correlation-Id` header values (not a GUID) → ignored; a fresh GUID is generated.
- Response always includes `X-Correlation-Id` (even for failing requests) — debugging affordance.

## Consumers

- `AuditingBehavior<,>` — reads `CorrelationId` and `ClientIpAddress` for every audit entry.
- Any future telemetry / structured-logging enrichment can read the same abstraction.
