# Contract: `AuditingBehavior<TRequest, TResponse>`

**Layer:** Application (`Mentoory.Shared.Application.Behaviors`)
**Visibility:** Public (registered via `AddOpenBehavior`)
**File:** `Mentoory.Shared.Application/Behaviors/AuditingBehavior.cs`

## API

```csharp
namespace Mentoory.Shared.Application.Behaviors;

public sealed class AuditingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IBaseRequest
{
    public AuditingBehavior(
        IAuditService auditService,
        ITenantContext tenantContext,
        ICorrelationContext correlationContext,
        ITimeProvider timeProvider,
        IOptions<AuditOptions> options,
        ILogger<AuditingBehavior<TRequest, TResponse>> logger);

    public Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken);
}
```

## Registration

`Mentoory.Web/Program.cs` (inside `AddMediatR`):

```csharp
cfg.AddOpenBehavior(typeof(ValidatorBehavior<,>));
cfg.AddOpenBehavior(typeof(AuditingBehavior<,>));    // NEW — must be BETWEEN Validator and Transaction
cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
```

## Behavior contract

1. On entry, inspect `TRequest` for `[Audited]`. If absent, call `await next(cancellationToken)` and return directly (no-op).
2. If present with `Mode = Manual`, call `await next(cancellationToken)` and return directly (no-op — the handler owns the audit write).
3. If present with `Mode = Automatic`:
   - Start a try/catch around `await next(cancellationToken)`.
   - On `TResponse` received:
     - Extract outcome: if `response is Result { IsSuccess: true }` OR `response is Result<T> { IsSuccess: true }` → `"Success"`. Else `"Failure"`.
     - Build an `AuditEntry` per [data-model.md § 2] and call `_auditService.LogAsync(entry, cancellationToken)` (awaited, but failures are swallowed by the service).
     - Return the original `TResponse`.
   - On exception caught:
     - Build an `AuditEntry` with `Outcome = "Failure"`, `ExceptionType = ex.GetType().FullName`.
     - Call `_auditService.LogAsync(entry, cancellationToken)`.
     - Rethrow the original exception (`throw;`) preserving the stack trace.

## Payload construction (Automatic mode)

| Field | Source |
|-------|--------|
| `EventType` | `[Audited].EventType` |
| `EntityType` | `[Audited].EntityType` |
| `EntityId` | `null` in v1 (Manual-mode handlers populate if needed) |
| `Action` | `typeof(TRequest).Name` (e.g., `AssignRoleCommand`) |
| `UserId` | `_tenantContext.UserId` (null if anonymous — see FR-014 for override) |
| `UserEmail` | `_tenantContext.UserEmail` (null if anonymous — command-payload override applies for specific commands) |
| `IncubatorId` | `_tenantContext.IncubatorId` |
| `ProjectId` | `_tenantContext.ProjectId` |
| `RoleContext` | `_tenantContext.Role` |
| `CorrelationId` | `_correlationContext.CorrelationId` |
| `IpAddress` | `_correlationContext.ClientIpAddress` |
| `Outcome` | `"Success"` or `"Failure"` |
| `ExceptionType` | `ex?.GetType().FullName` |
| `OccurredAtUtc` | `_timeProvider.UtcNow` (captured AFTER `next()` returns/throws) |
| `Details` | Redacted JSON payload (see "Redaction" below) |

## Anonymous-command override (FR-014)

When `TRequest` is `RegisterUserCommand` or `LoginUserCommand`, the tenant context is empty at the time this behavior runs (pre-authentication). To populate `UserId` / `UserEmail`, the behavior MUST consult a small interface registered per anonymous command type:

```csharp
// New contract:
internal interface IAuditAnonymousResolver<in TRequest>
{
    (long? UserId, string? UserEmail) Resolve(TRequest request);
}
```

Two implementations, registered in `Mentoory.Access.Application.ServiceCollectionExtensions`:

- `RegisterUserAuditResolver : IAuditAnonymousResolver<RegisterUserCommand>` — returns `(null, command.Email)` (no user id yet at registration time).
- `LoginUserAuditResolver : IAuditAnonymousResolver<LoginUserCommand>` — returns `(null, command.Email)` (user id known only after successful login; see note below).

**Note on login success:** for a *successful* login we could augment with the user id post-authentication, but doing so would require the handler to notify the behavior — crossing a clean boundary. v1 accepts `UserId = null` for login entries; the `UserEmail` + `Outcome` is sufficient for compliance.

## Redaction

Before serialization, the behavior:
1. Calls `JsonSerializer.SerializeToNode(request, options)` → `JsonObject`.
2. Iterates top-level property names (case-insensitive). For each match in `AuditOptions.RedactedFields`, replaces the node with a `JsonValue.Create(options.RedactionSentinel)`.
3. Calls `node.ToJsonString(options)`.
4. Truncates to `AuditOptions.DetailsMaxCharacters` with `AuditOptions.TruncationSuffix` if exceeded.
5. On `JsonException` during serialization, substitutes the sentinel string `"<serialization-failed: {ExceptionType}>"`.

## Testability

- Behavior is pure except for `IAuditService.LogAsync` side effects — unit testable with Moq.
- Unit test matrix:
  - No attribute → next called, no audit write.
  - `Mode = Manual` → next called, no audit write.
  - `Mode = Automatic`, success → audit write with `Outcome = "Success"`.
  - `Mode = Automatic`, handler returns failure `Result` → audit write with `Outcome = "Failure"`, `ExceptionType = null`.
  - `Mode = Automatic`, handler throws → audit write with `Outcome = "Failure"`, `ExceptionType = typeof(ex).FullName`, exception rethrown.
  - Redaction: command with `Password` property → `Details` contains sentinel, not original value.
  - Truncation: command with >8KB serialized payload → `Details` ends with `...<truncated>`.
  - Anonymous resolver: `RegisterUserCommand` → `UserEmail` pulled from payload.
