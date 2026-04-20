# Contract: `ITenantContextWriter`

**File**: `Mentoory.Shared.Application/Interfaces/ITenantContextWriter.cs`
**Introduced in**: CP-3 (QW-3)
**Consumers**: `TenantContextMiddleware` (sole caller of the setter).

## Shape

```csharp
namespace Mentoory.Shared.Application.Interfaces;

public interface ITenantContextWriter
{
    void SetCurrentIncubatorId(long? incubatorId);
}
```

## Semantics

- Accepts `null`. A null incubator id is a valid state for GlobalAdmin operating in global scope (edge case E-1).
- Idempotent per request: calling with the same value twice has no observable effect.
- Scope: the same instance MUST back `ITenantContext` (reader) and `ITenantContextWriter` (writer) within one request.

## Registration

```csharp
services.AddScoped<TenantContextService>();
services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContextService>());
services.AddScoped<ITenantContextWriter>(sp => sp.GetRequiredService<TenantContextService>());
```

The `TenantContextService` concrete class MUST remain registered as itself so the same instance is shared across both interface registrations.

## Middleware rule

`TenantContextMiddleware` MUST inject `ITenantContextWriter` directly and MUST NOT cast `ITenantContext` to any concrete type. This is enforced by review; a future architecture test may formalize "no concrete-type casts of request-scoped interfaces inside middleware".

## Failure mode

If `ITenantContextWriter` is not registered, DI MUST fail at application startup, not silently no-op at request time. `InvokeAsync(HttpContext, ITenantContextWriter)` parameter injection achieves this via ASP.NET's middleware constructor-injection policy.
