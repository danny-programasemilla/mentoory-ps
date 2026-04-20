# Contract: `IDomainEvent` marker interface

**File**: `Mentoory.Shared.Domain/SeedWork/IDomainEvent.cs`
**Introduced in**: CP-6 (QW-2)
**Consumers**: every aggregate-emitted domain event in the solution.

## Shape

```csharp
namespace Mentoory.Shared.Domain.SeedWork;

public interface IDomainEvent
{
}
```

## Rules

1. MUST live in `Mentoory.Shared.Domain` — no exceptions.
2. MUST be a pure marker (no members). Adding members later is a breaking change for all domain events and requires its own spec.
3. Domain event classes implement `IDomainEvent` DIRECTLY. They MUST NOT also implement `INotification` (that coupling is what this contract eliminates).
4. Integration events (`IIntegrationEvent` in `Shared.Application/IntegrationEvents/`) are OUT OF SCOPE and continue to inherit `INotification` directly. They are cross-module Application contracts, not Domain events.

## Adapter bridge

See [DomainEventNotification.md](./DomainEventNotification.md). Dispatch-time wrapping is the only coupling point between `IDomainEvent` and MediatR.

## Architecture-test enforcement

Rule R-4.2.1 in `Mentoory.Tests.Architecture` asserts that `Mentoory.*.Domain` assemblies do NOT reference the MediatR assembly. A regression where someone re-adds MediatR to a Domain project will fail the build.
