# Contracts: Integration Events

**Scope**: FR-K30 — the single integration event this spec emits.

---

## `TopicPriorityRangesChanged`

**Location**: `Mentoory.Knowledge.Application/IntegrationEvents/TopicPriorityRangesChanged.cs`
**Delivery**: In-process MediatR `INotification`. Published by `MediatR.IMediator.Publish` after `SaveChangesAsync` completes successfully.

```
public sealed record TopicPriorityRangesChanged(
    Guid TopicExternalId,
    long ProjectId,
    PriorityRangeDto? HighRange,
    PriorityRangeDto? MediumRange,
    PriorityRangeDto? LowRange) : INotification;
```

### Publication Mechanics
- `UpdateTopicPriorityRangesHandler` (project-clone side) applies the range change on the aggregate.
- The Knowledge `DbContext` collects the event via a `DomainEventDispatcher` that flushes domain-event collections on `SaveChanges` — mirrors the Diagnostic precedent (e.g., `DiagnosticResponseCreated`) if one exists, otherwise introduce the dispatcher here.
- On successful save, events are published through `IMediator.Publish(@event)`; failures during publication are logged but do not roll back the save (default MediatR behavior).

### Event Semantics
- Emitted **only** from project-clone topic range edits. Template-level range edits are explicitly silent (FR-K30 final sentence; US4 Acceptance Scenario 5).
- Payload carries **new** range state (post-edit). Consumers are not expected to reconcile previous state; Mentoring Plan will re-compute its priority mapping from the new ranges on next read.
- `PriorityRangeDto` fields follow the decimal scale decided in research R1 (no 0–100 bound).

### Consumer
- **v1**: no consumer in this repo. An empty `INotificationHandler<TopicPriorityRangesChanged>` MUST NOT be registered (would silence the event).
- **Future**: Mentoring Plan (separate spec) will register a handler to invalidate/rebuild its priority cache.

### Not an outbox event (yet)
Per spec Assumptions (and research R1/R2/R3 reaffirmation): this is NOT wired through the outbox pattern decided in cross-cutting hardening #10. If Mentoring Plan consumer later requires durability, the publisher is upgraded to `IOutboxEventPublisher` without changing the `INotification` contract.

---

## Domain Events (internal)

None exposed in this spec's public contracts. Implementation may use internal `DomainEvent` types inside the aggregates (for dispatcher purposes) but those are not part of the cross-module surface.
