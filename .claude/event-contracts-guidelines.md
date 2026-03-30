# Event Contracts Guidelines

## Overview
This document defines guidelines for creating and managing integration events in the Mentoory modular monolith architecture. Events are the primary mechanism for cross-domain communication while maintaining loose coupling between bounded contexts.

## Event Types

### 1. Domain Events (Internal)
Events that occur within a single bounded context and don't cross domain boundaries.

**Location**: `{Domain}.Domain.Events`
**Naming**: `{Entity}{Action}Event`
**Example**: `ProjectStatusChangedEvent`

```csharp
namespace Mentoory.BusinessIncubator.Domain.Events;

public record ProjectStatusChangedEvent(
    long ProjectId,
    ProjectStatus OldStatus,
    ProjectStatus NewStatus,
    DateTime ChangedAt);
```

### 2. Integration Events (Cross-Domain)
Events that cross bounded context boundaries and represent public contracts between domains.

**Location**: `{Domain}.Application.IntegrationEvents`
**Naming**: `{Entity}{Action}IntegrationEvent` or just `{Entity}{Action}`
**Example**: `ProtectedResourceCreated`

```csharp
namespace Mentoory.Permissions.Application.IntegrationEvents;

public record ProtectedResourceCreated(
    Guid ResourceExternalId,
    string ResourceType,
    string ResourceName,
    string CreatedBy) : IIntegrationEvent;
```

## Guidelines

### 1. Event Design Principles

#### Immutability
All events MUST be immutable. Use `record` types or readonly properties.

```csharp
// ✅ Good - Immutable record
public record UserRegistered(string UserId, string Email, DateTime RegisteredAt);

// ❌ Bad - Mutable class
public class UserRegistered
{
    public string UserId { get; set; }  // Mutable!
}
```

#### Self-Contained
Events should contain all necessary data. Avoid requiring additional queries.

```csharp
// ✅ Good - Self-contained
public record OrderPlaced(
    Guid OrderId,
    string CustomerName,    // Include relevant data
    decimal TotalAmount,
    List<OrderItemDto> Items);

// ❌ Bad - Requires lookup
public record OrderPlaced(
    Guid OrderId,
    Guid CustomerId);  // Requires querying customer service
```

#### Primitive Types
Use primitive types and simple DTOs. Avoid domain entities.

```csharp
// ✅ Good - Primitive types
public record ProjectCreated(
    Guid ProjectId,
    string Name,
    int Status);  // Use int, not enum

// ❌ Bad - Domain entity reference
public record ProjectCreated(
    Project Project);  // Never pass entities!
```

### 2. Naming Conventions

#### Integration Events
- **Creation**: `{Entity}Created`, `{Entity}Registered`
- **Update**: `{Entity}Updated`, `{Entity}Modified`
- **Deletion**: `{Entity}Deleted`, `{Entity}Removed`
- **State Change**: `{Entity}{State}`, e.g., `OrderShipped`, `ProjectApproved`
- **Action**: `{Entity}{Action}`, e.g., `UserInvited`, `EmailSent`

#### Event Properties
- Use PascalCase for property names
- Use past tense for timestamps: `CreatedAt`, `UpdatedAt`, `DeletedAt`
- Include actor when relevant: `CreatedBy`, `ApprovedBy`

### 3. Versioning Strategy

When events need to change:

#### Non-Breaking Changes (OK)
- Adding new optional properties
- Adding new events

```csharp
// Version 1
public record UserRegistered(string UserId, string Email);

// Version 1.1 - OK, backward compatible
public record UserRegistered(string UserId, string Email, string? PhoneNumber = null);
```

#### Breaking Changes (Requires New Version)
- Removing properties
- Renaming properties
- Changing property types

```csharp
// Version 1 (keep for backward compatibility)
[Obsolete("Use UserRegisteredV2")]
public record UserRegistered(string UserId, string Email);

// Version 2 (new event)
public record UserRegisteredV2(Guid UserId, string Email, string DisplayName);
```

### 4. Cross-Domain References

#### Allowed References
Only these types of cross-domain references are permitted:

1. **Integration Events**: Located in `{Domain}.Application.IntegrationEvents`
2. **Shared Constants**: Located in `{Domain}.Domain.Constants` when they represent public contracts
3. **Shared Value Objects**: Only if in `Mentoory.Shared.Domain`

```csharp
// ✅ Allowed - Integration event reference
using Mentoory.Permissions.Application.IntegrationEvents;

// ✅ Allowed - Public constants
using Mentoory.Permissions.Domain.Constants; // For ResourceTypes

// ❌ NOT Allowed - Domain entity reference
using Mentoory.Permissions.Domain.Aggregates; // Never!
```

### 5. Event Publishing Patterns

#### Synchronous (Development/Testing)
```csharp
#if DEBUG
await integrationEventService.PublishAsync(eventData, cancellationToken);
#endif
```

#### Asynchronous (Production)
```csharp
#if !DEBUG
_ = Task.Run(async () => 
    await integrationEventService.PublishAsync(eventData, cancellationToken), 
    cancellationToken);
#endif
```

#### Transactional Outbox Pattern (Future)
```csharp
// Save event to outbox table in same transaction
await outboxService.AddAsync(eventData, cancellationToken);
await unitOfWork.SaveChangesAsync(cancellationToken);
// Background service publishes from outbox
```

### 6. Event Handling

#### Idempotency
All event handlers MUST be idempotent.

```csharp
public class ProjectCreatedHandler : IIntegrationEventHandler<ProjectCreated>
{
    public async Task Handle(ProjectCreated @event, CancellationToken cancellationToken)
    {
        // Check if already processed
        var exists = await repository.ExistsAsync(@event.ProjectId);
        if (exists) return; // Idempotent
        
        // Process event
        await CreateProtectedResource(@event);
    }
}
```

#### Error Handling
Use retry policies and dead letter queues for failures.

```csharp
[RetryPolicy(MaxRetries = 3, BackoffMultiplier = 2)]
public class CriticalEventHandler : IIntegrationEventHandler<CriticalEvent>
{
    // Implementation
}
```

## Examples from Mentoory

### Good Example: ProtectedResourceCreated
```csharp
namespace Mentoory.Permissions.Application.IntegrationEvents;

/// <summary>
/// Integration event raised when a protected resource is created.
/// Used by other domains to trigger permission setup.
/// </summary>
public record ProtectedResourceCreated(
    Guid ResourceExternalId,    // Primitive type
    string ResourceType,        // Simple string, not enum
    string ResourceName,        // Self-contained data
    string CreatedBy           // Actor information
) : IIntegrationEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
```

### Usage Pattern
```csharp
// In BusinessIncubator.Application
public class CreateBusinessIncubatorHandler
{
    public async Task<Result> Handle(CreateBusinessIncubatorCommand request)
    {
        // Create domain entity
        var incubator = new BusinessIncubator(request.Name, request.Key);
        repository.Add(incubator);
        await unitOfWork.SaveChangesAsync();
        
        // Publish integration event
        var @event = new ProtectedResourceCreated(
            incubator.ExternalId,
            ResourceTypes.BusinessIncubator,  // Constant from Permissions.Domain.Constants
            incubator.Name,
            currentUser.Id
        );
        
        await integrationEventService.PublishAsync(@event);
        
        return Success(incubator.Id);
    }
}
```

## Validation Checklist

Before creating or modifying an integration event:

- [ ] Is the event immutable (record type or readonly properties)?
- [ ] Does it use only primitive types and simple DTOs?
- [ ] Is it self-contained (no need for additional queries)?
- [ ] Does it follow naming conventions?
- [ ] Is it in the correct namespace (Application.IntegrationEvents)?
- [ ] Is the handler idempotent?
- [ ] Is it documented with XML comments?
- [ ] Are cross-domain references limited to events and constants only?

## Anti-Patterns to Avoid

### ❌ God Event
```csharp
// Bad - Too many responsibilities
public record EverythingChanged(
    Guid UserId, Guid ProjectId, Guid IncubatorId, 
    string Action, Dictionary<string, object> Changes);
```

### ❌ Chatty Events
```csharp
// Bad - Too granular
public record UserFirstNameChanged(string UserId, string FirstName);
public record UserLastNameChanged(string UserId, string LastName);
public record UserEmailChanged(string UserId, string Email);
// Better: UserProfileUpdated with all changes
```

### ❌ Domain Logic in Events
```csharp
// Bad - Contains business logic
public record OrderPlaced(Order order)
{
    public decimal CalculateDiscount() => // Business logic!
}
```

### ❌ Anemic Events
```csharp
// Bad - Not enough information
public record SomethingHappened(Guid Id); // What happened? What type?
```

## Domain Read Models Pattern

### Key Principles
1. **Synchronous Updates**: Access control read models are updated synchronously for security
2. **Idempotent Handlers**: Check for existing records before creating
3. **Self-Contained Events**: Include all data needed to update read models
4. **No Cross-Domain Queries**: Auth services only query their own read models

## References
- [Domain-Driven Design by Eric Evans](https://www.domainlanguage.com/ddd/)
- [Enterprise Integration Patterns](https://www.enterpriseintegrationpatterns.com/)