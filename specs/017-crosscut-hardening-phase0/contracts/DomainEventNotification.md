# Contract: `DomainEventNotification<TEvent>` adapter

**File**: `Mentoory.Shared.Application/DomainEvents/DomainEventNotification.cs`
**Introduced in**: CP-6 (QW-2)
**Consumers**: `SharedAbstractDbContext.DispatchDomainEventsAsync` (producer); every `INotificationHandler<DomainEventNotification<TEvent>>` (consumer).

## Shape

```csharp
using MediatR;
using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Shared.Application.DomainEvents;

public sealed record DomainEventNotification<TEvent>(TEvent DomainEvent) : INotification
    where TEvent : IDomainEvent;
```

## Producer contract

The dispatcher at `Mentoory.Shared.Infrastructure/Persistence/SharedAbstractDbContext.cs` wraps every `IDomainEvent` collected from tracked entities into a closed `DomainEventNotification<TConcrete>` and publishes it via `IPublisher`. The wrapping uses `MakeGenericType` + `Activator.CreateInstance` — this is the single acceptable reflection point in the entire Domain-event path.

## Consumer contract

Handlers subscribe to the wrapped shape:

```csharp
public sealed class UserRegisteredEventHandler
    : INotificationHandler<DomainEventNotification<UserRegisteredEvent>>
{
    public Task Handle(DomainEventNotification<UserRegisteredEvent> notification, CancellationToken ct)
    {
        var domainEvent = notification.DomainEvent;
        // ...
    }
}
```

Handlers MUST NOT subscribe to raw `IDomainEvent` — they subscribe to the closed generic wrapper.

## No-subscribers invariant (Edge Case E-2)

If a domain event has zero registered handlers, publishing the wrapper MUST complete without error. MediatR's default behavior satisfies this; no custom handling required.

## Performance

`MakeGenericType` is called once per (concrete event type) per request. If profiling ever shows this as measurable overhead, cache the closed generic type per event-type on the `SharedAbstractDbContext` instance. This optimization is NOT part of the initial CP-6; only add it when evidence justifies it.
