# Domain-Driven Design Patterns in Mentoory

## Overview
This document captures the DDD patterns and principles applied in the Mentoory codebase, particularly focusing on aggregate boundary enforcement and encapsulation.

## ⚠️ CRITICAL: Clean Architecture Layer Separation

### Controllers MUST NEVER directly use domain repositories
```csharp
// ❌ WRONG - Controller using domain repository directly
public class Controller(IBusinessIncubatorRepository repository) 

// ✅ CORRECT - Controller only uses MediatorExecutor
public class Controller(MediatorExecutor mediator)
```

**Why this matters:**
- Enables microservice extraction without breaking web layer
- Keeps business logic in Application layer (CQRS commands/queries)
- Maintains clean architecture: Web → Application → Domain
- Prevents tight coupling between layers

**Authorization belongs in Commands, not separate services:**
- Put authorization logic in GetOrCreate commands
- Use ResultErrorCodes for all denial scenarios
- Keep controllers thin - only orchestration, no business logic

## Separation of Concerns

### Application Layer Returns Pure Domain Data
The Application layer (Commands/Queries) should return domain entities, enums, or DTOs with domain data. It should NOT handle UI concerns like display text or localization:

```csharp
// ✅ CORRECT - Application layer returns domain enum
public class PendingFormDto
{
    public Domain.Enums.QuestionPhase Phase { get; set; }
    public Domain.Enums.ProjectFormSubmissionStatus Status { get; set; }
}

// ❌ WRONG - Application layer handling UI text
public class PendingFormDto  
{
    public string Phase { get; set; }  // "Inicio", "Final", etc.
    public string Status { get; set; } // "Borrador", "Enviado", etc.
}
```

### Web Layer Handles Display Formatting
The Web layer (Controllers/ViewModels) is responsible for converting domain data to display text:

```csharp
// ✅ CORRECT - Controller maps domain enum to display text
private static string GetPhaseDisplayName(QuestionPhase phase)
{
    return phase switch
    {
        QuestionPhase.Start => "Inicio",
        QuestionPhase.Final => "Final",
        _ => phase.ToString()
    };
}
```

## Key DDD Principles

### 1. Aggregate Boundaries
Aggregates are consistency boundaries that encapsulate business invariants. In Mentoory:

- **Aggregate roots** are the only entry points to the aggregate
- **Navigation properties** between aggregates are `internal` to prevent direct access
- **Cross-aggregate references** use IDs instead of object references
- **Domain boundaries are sacred** - Never use raw SQL queries to cross schema boundaries
- **Each domain owns its data completely** - No direct database access between domains

#### Cross-Domain Communication
When domains need to share data:
- Use **Integration Events** (see ADR-001) for eventual consistency
- Create **Read Models** in the consuming domain synchronized via events
- Never query another domain's tables directly, even with raw SQL
- Publish events via MediatR: `await _mediator.Publish(integrationEvent, cancellationToken)`
- Always inject ITimeProvider for event timestamps, never use DateTime.UtcNow

### 3. DateTime Handling in Domain
Domain entities should never generate DateTime values internally:

```csharp
// ❌ WRONG - Domain generating DateTime
public bool IsWithinWindow(ProjectStage? stage)
{
    return stage.IsWithinPeriod(DateTime.UtcNow);  // Bad!
}

// ✅ CORRECT - DateTime passed as parameter
public bool IsWithinSubmissionWindow(ProjectStage? stage, DateTime currentDate)
{
    if (stage is null)
        return false;
    return stage.IsWithinPeriod(currentDate) && stage.IsActive;
}
```

- Always pass DateTime as a parameter to domain methods
- Use ITimeProvider in application/infrastructure layers
- This ensures testability and consistency

##### Integration Event Placement
- **Shared Events**: Place in Mentoory.Shared.Application/IntegrationEvents/ when multiple domains consume
- **Domain-Specific**: Keep in originating domain's Application layer if only one consumer
- **Event Handlers**: ALWAYS in Application layer (INotificationHandler<T>), never in Infrastructure
- **Repository Pattern**: Handlers use IRepository interfaces, not DbContext directly

#### Aggregate Analysis Before Refactoring
Before moving entities between domains, analyze if they're part of an aggregate:
- If an entity has domain methods that the aggregate root calls (e.g., Project.AddUser() calling ProjectUser methods), it's part of that aggregate
- Business logic must stay with the aggregate root
- Example: ProjectUsers is part of Project aggregate, not a separate Auth concept

#### Cross-Domain Access Verification
Domain entities should NOT check cross-domain concerns:
- **Wrong**: Domain entity checking Auth domain's UserProjectAccess table
- **Right**: Application layer verifies access using repository methods
- **Pattern**: Use `repository.IsUserProjectParticipantAsync()` at application layer
- **Note**: Access checks in domain should only use local aggregate data

### 2. Read Models vs Domain Entities

#### Domain Entities (in AggregatesModel/)
- Have business logic (Create, Update, Deactivate methods)
- Maintain invariants and business rules
- Use private setters with public domain methods
- Example: `UserProjectAccess` with `Deactivate()`, `Reactivate()`, `UpdateRole()` methods

#### Pure Read Models (in ReadModels/)
- Immutable - no public methods that change state
- Used only for queries and reporting
- No business logic or validation
- Typically synchronized from other domains via events

## Value Objects

### Trust Value Object Validation
Value objects encapsulate validation logic and should be trusted once created:

```csharp
// ❌ WRONG - Entity duplicating value object validation
public Result UpdateLocation(string? country, string? province, ...)
{
    // Don't duplicate country-specific validation here!
    if (country?.Equals("Costa Rica") == true)
    {
        if (string.IsNullOrWhiteSpace(province))
            return Failure(...);
    }
    
    var locationResult = Location.Create(country, province, ...);
    // More validation after Location already validated...
}

// ✅ CORRECT - Trust the value object
public Result UpdateLocation(string? country, string? province, ...)
{
    // Delegate ALL validation to the value object
    var locationResult = Location.Create(country, province, ...);
    if (!locationResult.IsSuccess)
        return Failure(locationResult.ErrorCode, locationResult.ErrorMessages);
    
    _location = locationResult.Value;
    return Success();
}
```

### Scalable Country-Specific Validation
Use pattern matching for country-specific rules to avoid long switch statements:

```csharp
private static Result ValidateCountryRequirements(string? country, ...)
{
    // Pattern matching for scalability
    return country?.ToUpperInvariant() switch
    {
        "COSTA RICA" => ValidateCostaRicaRequirements(...),
        "PANAMA" => ValidatePanamaRequirements(...),
        _ => Result.Success() // Default for countries without special rules
    };
}
```

**Key Principles:**
- Value objects are immutable and self-validating
- Entities should trust value objects once created
- Don't duplicate validation between entities and value objects
- Use factory methods (Create) for value object creation with validation
- Keep country/region-specific logic in one place for maintainability


### Integration Event Pattern
Include both datasets in events:
```csharp
public record ProjectFormSubmissionApproved(
    string StarterDraftData,
    string CoordinatorDraftData,
    string CoordinatorUserId) : IntegrationEvent;
```

### Target Domain Pattern
Use source discrimination for multiple versions:
```csharp
public class DiagnosisAnswer : Entity
{
    public string AnswerSource { get; private set; } = "Starter";
    public string? CoordinatorUserId { get; private set; }
}
```

## Encapsulation Patterns

### 1. Private Setters for All Properties
Entities should only be modified through domain methods:

```csharp
public class BusinessIncubator : SoftDeletableEntity, IAggregateRoot
{
    // ✅ CORRECT - Private setter
    public string Name { get; private set; }
    
    // Domain method for modification
    public void UpdateDetails(string name, string description, IAuditContext auditContext)
    {
        Name = name;
        SetUpdated(auditContext);
    }
}
```

### 2. Private Backing Fields for Collections
Collections should be encapsulated with controlled access:

```csharp
public class BusinessIncubator : SoftDeletableEntity, IAggregateRoot
{
    // Private backing field
    private readonly List<Project> _projects = new();
    
    // Internal navigation for EF Core (with private setter)
    internal virtual ICollection<Project> Projects { get; private set; }
    
    // Public read-only access
    public IReadOnlyCollection<Project> GetProjects() => _projects.AsReadOnly();
    
    // Controlled modification through domain methods
    public Project AddProject(string name, string description, IAuditContext auditContext)
    {
        var project = new Project(name, description, Id, auditContext);
        _projects.Add(project);
        SetUpdated(auditContext);
        return project;
    }
}
```

### 3. No Direct Cross-Aggregate Navigation
Aggregates should reference each other by ID only:

```csharp
public class Project : SoftDeletableEntity
{
    // ✅ CORRECT - Reference by ID
    public long BusinessIncubatorId { get; private set; }
    
    // ❌ WRONG - Direct navigation to another aggregate root
    // public BusinessIncubator BusinessIncubator { get; set; }
    
    // ✅ OK - Internal navigation for EF Core only
    internal virtual BusinessIncubator BusinessIncubator { get; private set; }
}
```

### 4. Domain Methods for All Business Operations
All state changes should go through domain methods that enforce business rules:

```csharp
public void Deactivate(string deactivatedBy, DateTime deactivatedAt)
{
    if (!IsActive)
    {
        throw new InvalidOperationException("Access is already inactive.");
    }
    
    IsActive = false;
    DeactivatedBy = deactivatedBy;
    DeactivatedAt = deactivatedAt;
}
```

## Repository Patterns

### Repository Interfaces in Domain Layer
Repository interfaces belong in the domain layer as they represent domain contracts:

```csharp
// In Domain/Repositories/
public interface IBusinessIncubatorRepository : IRepository<BusinessIncubator>
{
    Task<BusinessIncubator?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<BusinessIncubator?> GetWithProjectsAsync(long id, CancellationToken cancellationToken = default);
}
```

### Repository Implementation Patterns
Repositories should return fully hydrated aggregates when needed:

```csharp
public async Task<BusinessIncubator?> GetWithProjectsAsync(long id, CancellationToken cancellationToken)
{
    return await _context.BusinessIncubators
        .Include("_projects")  // Use string for private backing field
        .FirstOrDefaultAsync(bi => bi.Id == id, cancellationToken);
}
```

### No Direct DbContext in Application Layer
Application layer should only use repository interfaces:

```csharp
public class CommandHandler
{
    private readonly IBusinessIncubatorRepository _repository;
    
    // ✅ CORRECT - Use repository
    public async Task Handle(Command command, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(command.Id, cancellationToken);
        // ...
    }
}
```

## Navigation Properties in EF Core

### Internal Navigation Configuration
Configure internal navigation properties using strings:

```csharp
public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        // Configure internal navigation
        builder.HasOne<BusinessIncubator>()
            .WithMany("Projects")  // String name for internal property
            .HasForeignKey(p => p.BusinessIncubatorId);
    }
}
```

### Many-to-Many with Internal Navigation
```csharp
builder.HasMany<ProjectQuestion>()
    .WithMany("ProjectAnswerOptions")
    .UsingEntity<ProjectQuestionAnswerOption>(
        "ProjectQuestionAnswerOptions",
        // configuration...
    );
```
h
## Testing with Internal Members

### Making Internals Visible to Tests
Add InternalsVisibleTo attributes for test assemblies:

```csharp
[assembly: InternalsVisibleTo("Mentoory.Tests.UnitTesting")]
[assembly: InternalsVisibleTo("Mentoory.Tests.E2E")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")] // For Moq
```

### Testing Domain Methods
Test through public domain methods, not by setting properties directly:

```csharp
[Fact]
public void UpdateDetails_ShouldModifyNameAndDescription()
{
    // Arrange
    var incubator = new BusinessIncubator("Original", "Description", auditContext);
    
    // Act - Use domain method
    incubator.UpdateDetails("Updated", "New Description", auditContext);
    
    // Assert
    Assert.Equal("Updated", incubator.Name);
}
```

## Read Model Synchronization Patterns

### Seed Data for Read Models
When using read models that are synchronized via events, seed data requires special handling:

```sql
-- Pattern: Mirror source domain data to read models
-- Example: Auth domain mirroring BusinessIncubator relationships

-- 1. Clear existing data (idempotent execution)
DELETE FROM [auth].[UserProjectAccess];

-- 2. Copy from source with necessary JOINs
INSERT INTO [auth].[UserProjectAccess]
    ([UserId], [ProjectId], [IncubatorId], [Role], [IsActive], [LastSyncedAt])
SELECT 
    pu.UserId,
    pu.ProjectId,
    p.BusinessIncubatorId,  -- JOIN to get additional data
    pu.Role,
    pu.IsActive,
    '2024-01-01 00:00:00'   -- Fixed seed date
FROM [businessincubators].[ProjectUsers] pu
INNER JOIN [businessincubators].[Projects] p ON pu.ProjectId = p.Id;

-- 3. Verify data consistency
DECLARE @SourceCount INT = (SELECT COUNT(*) FROM [businessincubators].[ProjectUsers]);
DECLARE @TargetCount INT = (SELECT COUNT(*) FROM [auth].[UserProjectAccess]);
PRINT 'Source: ' + CAST(@SourceCount AS NVARCHAR(10)) + ', Target: ' + CAST(@TargetCount AS NVARCHAR(10));
```

### Key Principles
1. **Mirror, Don't Create**: Seed data should mirror existing relationships
2. **Maintain Referential Integrity**: All foreign keys must reference existing data
3. **Use Fixed Timestamps**: Consistent dates for seed data (e.g., '2024-01-01')
4. **Include Verification**: Always verify counts match between source and target
5. **Idempotent Execution**: DELETE/INSERT pattern for re-runnability

## Orchestration Patterns

### Cross-Domain Commands with Progress Tracking
When implementing bulk operations that span multiple domains, use progress tracking:

```csharp
// In orchestration command handler
const int saveProgressInterval = 10; // Save every N records

foreach (var item in items)
{
    // Process item...
    processedCount++;
    
    if (processedCount % saveProgressInterval == 0)
    {
        batchEntity.UpdateProgress(processedCount, successCount, failedCount);
        repository.Update(aggregate);
        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);
    }
}
```

### Invitation Token Generation Pattern
Always use domain commands for invitation tokens, never generate GUIDs directly:

```csharp
// ✅ CORRECT - Use domain command
var createInvitationCommand = new CreateProjectInvitationCommand(
    projectExternalId,
    email,
    fullName,
    identificationNumber,
    role,
    expirationDays);
var invitationResult = await mediator.Send(createInvitationCommand);
var token = invitationResult.Value!;

// ❌ WRONG - Direct GUID generation
var token = Guid.NewGuid().ToString();
```

### Bulk User Processing Pattern
For batch user operations, check existing users first, then process incrementally:

```csharp
// 1. Batch check for existing users
var getUsersQuery = new GetUsersByEmailsQuery(emails);
var usersResult = await mediator.Send(getUsersQuery);
var existingUsers = usersResult.Value!.ExistingUsers;

// 2. Process each user with proper error handling
foreach (var userData in items)
{
    if (!existingUsers.ContainsKey(userData.Email))
    {
        // Create new user
        var createCommand = new CreateUserWithProfileCommand(...);
        var result = await mediator.Send(createCommand);
        if (result.IsSuccess)
        {
            existingUsers[userData.Email] = result.Value!.User;
        }
    }
    
    // Create invitation for all users (new and existing)
    var invitationCommand = new CreateProjectInvitationCommand(...);
    await mediator.Send(invitationCommand);
}
```

## Guidelines for New Development

1. Always make entity properties have private setters
2. Use private backing fields for collections
3. Expose collections as IReadOnlyCollection
4. Create repository methods for cross-aggregate queries
5. Update entities through their aggregate root when possible
6. Use domain methods for all business operations
7. Configure EF Core with string-based navigation names for internal properties

## Benefits Achieved

1. **Encapsulation**: Business logic is protected within aggregates
2. **Consistency**: All state changes go through domain methods
3. **Testability**: Tests can still access internals when needed
4. **Maintainability**: Clear boundaries make the codebase easier to understand
5. **Flexibility**: Can change internal implementation without affecting consumers