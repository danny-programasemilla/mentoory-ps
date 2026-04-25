# Contract: `IAuditService`

**Layer:** Application (`Mentoory.Shared.Application.Audit`)
**Visibility:** Public
**File:** `Mentoory.Shared.Application/Audit/IAuditService.cs`
**Implementation:** `Mentoory.Shared.Infrastructure/Audit/AuditService.cs`

## API (unchanged shape, extended payload)

```csharp
namespace Mentoory.Shared.Application.Audit;

public interface IAuditService
{
    Task LogAsync(AuditEntry entry, CancellationToken cancellationToken = default);
}
```

## Input contract — `AuditEntry`

See [data-model.md](../data-model.md) § 2.

## Behavior contract

- **Best-effort.** Exceptions MUST be caught by the implementation, logged via `ILogger<AuditService>` at `LogLevel.Error`, and not rethrown. Callers (MediatR pipeline behavior, Manual-mode handlers) MUST NOT wrap `LogAsync` in try/catch blocks — that responsibility is the service's.
- **Out of transaction.** The implementation MUST NOT participate in any `DbContext`'s transaction. Current implementation opens a fresh `SqlConnection` per call; preserved.
- **Idempotency.** Not guaranteed. Callers should expect exactly-once semantics on success but MAY see zero writes on failure.
- **Write target.** `[audit].[AuditLog]`. The implementation is free to change the table or persistence mechanism as long as the contract is preserved.

## Guarantees

- `LogAsync` returns a `Task`. It completes successfully even if the write failed (failure is logged, not thrown).
- `CancellationToken` is observed for connection acquisition and command execution.
- Thread-safe — may be called concurrently from multiple handlers within one HTTP request.

## Consumers

- `AuditingBehavior<TRequest, TResponse>` — automatic path.
- `CorrectAnswerHandler` — Manual-mode path (after the aggregate `CorrectAnswer()` call, before `Success()`).
- Future Manual-mode handlers for approvals, stage advances, etc.
