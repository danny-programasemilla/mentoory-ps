# Contract: `AuditedAttribute` + `AuditMode`

**Layer:** Application (`Mentoory.Shared.Application.Audit`)
**Visibility:** Public
**File:** `Mentoory.Shared.Application/Audit/AuditedAttribute.cs`

## API

```csharp
namespace Mentoory.Shared.Application.Audit;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class AuditedAttribute : Attribute
{
    public AuditedAttribute(string eventType);

    public string EventType { get; }          // required
    public string? EntityType { get; init; }  // optional
    public AuditMode Mode { get; init; }      // default Automatic
}

public enum AuditMode { Automatic, Manual }
```

## Usage

Automatic mode (most commands):

```csharp
[Audited(AuditEventTypes.RoleAssigned, EntityType = "RoleAssignment")]
public sealed record AssignRoleCommand(...) : IBaseRequest;
```

Manual mode (command needs domain-specific detail):

```csharp
[Audited(AuditEventTypes.AnswerCorrected, EntityType = "AnswerCorrection", Mode = AuditMode.Manual)]
public sealed record CorrectAnswerCommand(...) : IBaseRequest;
```

## Guarantees

- Exactly one audit row is written per dispatched command in `Automatic` mode (by `AuditingBehavior`).
- Zero audit rows are written by the behavior in `Manual` mode — the handler owns the write.
- The attribute MUST be applied to command classes only (architecture test asserts this: queries carrying `[Audited]` fail the build).
- `EventType` constructor arg is `not null, not empty` — `ArgumentException` thrown otherwise (verified by unit test).

## Consumers

- `AuditingBehavior<TRequest, TResponse>` — reads the attribute via `typeof(TRequest).GetCustomAttribute<AuditedAttribute>()`.
- Architecture test (`Mentoory.Tests.Architecture.AuditCoverageTests`) — enforces presence on sensitive-named commands.
