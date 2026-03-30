# Mentoory Architecture Guide

## Clean Architecture Overview
- **Strict layer separation**: Domain → Application → Infrastructure → Web
- **Dependency rule**: Dependencies only point inward (Web → Infrastructure → Application → Domain)
- **No web concerns in inner layers**: Convert IFormFile to Stream at controller boundary

## Project Structure
- **Mentoory.Web**: Main web application (Controllers, Views, Models)
- **Example.Domain**: Example Core business logic and aggregates
- **Example.Application**: Example CQRS commands/queries with MediatR
- **Example.Infrastructure**: Example Data persistence and external services
- **LindaDb**: SQL Server database project (SSDT)

## Architecture Patterns
- **CQRS with MediatR**: Commands for write operations, Queries for read operations
- **Repository pattern**: Data abstraction with EF Core implementation
- **Result pattern**: Use `Result<T>` for error handling instead of exceptions
- **Domain events**: For cross-aggregate communication
- **Value objects**: For complex domain concepts without identity

## Layer Responsibilities

### Domain Layer
- Business logic and rules
- Domain entities and aggregates
- Domain services
- Value objects
- Domain events
- Repository interfaces (no implementation)

### Application Layer
- CQRS commands and queries  
- DTOs for data transfer
- Validation logic (FluentValidation)
- Mapping between domain and DTOs
- Domain-focused services only (no infrastructure concerns)
- **Result<T> Pattern**: Use ONLY for commands/queries at architectural boundaries

### Infrastructure Layer
- EF Core DbContext and configurations
- Repository implementations
- External service integrations
- Email services
- File system operations

### Orchestration Layer
- Cross-domain coordination
- Multi-step workflows
- Transaction boundaries across domains
- Complex business processes
- Infrastructure services for orchestration needs (e.g., CSV parsing)
- Workflow-specific utilities and helpers
- **Service Pattern**: Infrastructure services can live here when supporting orchestration workflows
- **Pragmatic Exception**: May directly use domain repositories for audit/tracking operations when the alternative adds unnecessary complexity (must be well-documented)
- **User Administration**: Coordinates user creation across Auth (Identity), UserManagement (profiles), and BusinessIncubator (assignments) domains

### Web Layer
- MVC Controllers (inherit from BaseController)
- ViewModels for forms
- Razor Views
- API endpoints
- Authentication/Authorization
- Convert web types (IFormFile) to domain types (Stream)

## Key Principles

### Clean Architecture File Upload Pattern
```csharp
// ❌ WRONG - Web dependency in domain
public record BatchCommand(IFormFile CsvFile);

// ✅ CORRECT - Infrastructure agnostic
public record BatchCommand(Stream CsvStream, string FileName);

// Web layer conversion
var command = new BatchCommand(
    model.CsvFile.OpenReadStream(),
    model.CsvFile.FileName);
```

### .NET Aspire Integration Pattern
```csharp
// ❌ WRONG - Manual service configuration
services.AddSingleton(sp => 
{
    var connectionString = configuration["Storage:ConnectionString"];
    return new BlobServiceClient(connectionString);
});

// ✅ CORRECT - Aspire automatic configuration
// In Program.cs
builder.AddAzureBlobClient("avatars");

// In service constructor - auto-injected
public AvatarService(BlobServiceClient blobServiceClient) 
{
    _blobServiceClient = blobServiceClient; // Configured by Aspire
}
```

### Dependency Injection
- Configure in each layer's DependencyInjection.cs
- Use IServiceCollection extensions
- Scoped lifetime for DbContext and repositories
- Transient for commands/queries

### Database Design
- Entity Framework Code First
- Proper indexes for query performance
- Foreign key relationships
- Soft deletes where appropriate

## Result<T> Pattern Usage (CRITICAL)

### When to Use Result<T>
- **ONLY at architectural boundaries**: Commands, Queries, and their handlers
- **Public APIs**: Methods exposed to other layers or external systems
- **Error propagation**: When multiple error types need to be communicated across layers

### When NOT to Use Result<T>
- **Internal service methods**: Use standard C# patterns instead
- **Private methods**: Use bool with out parameters, exceptions, or nullable types
- **Infrastructure services**: Use appropriate return types for the operation

### Example - Correct Pattern
```csharp
// ❌ WRONG - Abusing Result<T> for internal method
private Result<bool> ValidateCsvStream(Stream stream)
{
    if (stream == null)
        return Result<bool>.Failure(ErrorCode, "Stream is null");
    return Result<bool>.Success(true);
}

// ✅ CORRECT - Standard C# pattern for internal method
private bool ValidateCsvStream(
    [NotNullWhen(true)] Stream? csvStream, 
    string fileName, 
    out (ResultErrorCodes ErrorCode, string ErrorMessage) error)
{
    error = default;
    if (csvStream is not { CanRead: true })
    {
        error = (ResultErrorCodes.InvalidCsv, "Stream is invalid");
        return false;
    }
    return true;
}
```

## Infrastructure Service Placement

### Domain Application Layer
- Should NOT contain infrastructure services
- No file parsing, email sending, or external integrations
- Focus on domain logic and business rules only

### Orchestration Application Layer
- CAN contain infrastructure services that support workflows
- Examples: CSV parsing, file processing, complex data transformations
- These services support the orchestration's coordination role

### Infrastructure Layer
- Primary home for infrastructure services
- Database access, external APIs, file system operations
- Services used across multiple domains

## Architectural Decisions & Trade-offs

### Controllers and DDD Compliance
**Context**: Controllers must not directly use repositories

**Decision**: Controllers should only use queries/commands through MediatR

**Pattern**:
```csharp
// ❌ WRONG - Controller using repository
public class ProjectsController(IBusinessIncubatorRepository repository)
{
    var project = await repository.GetProjectByExternalIdAsync(id);
}

// ✅ CORRECT - Controller using query
public class ProjectsController(MediatorExecutor mediator)
{
    var query = new GetProjectByExternalIdQuery(id);
    var result = await mediator.SendAndLogIfFailureAsync(query);
}
```

**Enhanced Query Pattern for Access Checks**:
```csharp
// Query can optionally verify access
public record GetProjectByExternalIdQuery(
    Guid ExternalId, 
    string? CheckAccessForUserId = null) : IBaseRequest<ProjectByExternalIdDto>;
```
- Multiple domains need to coordinate complex state changes
- Testing or maintenance becomes difficult due to coupling

## Feature Consolidation Pattern

When unifying duplicate features across the codebase:

### 1. Analysis Phase
- Compare both implementations for feature completeness
- Identify the implementation with better UI/UX
- Document missing features in the chosen base
- Check for role-based access requirements

### 2. Enhancement Phase  
- Add missing business logic to chosen implementation
- Integrate proper domain commands (never use placeholder GUIDs)
- Add progress tracking for bulk operations
- Ensure multi-role access support

### 3. Cleanup Phase
**Order of removal is critical to avoid build breaks:**
1. Delete command/query classes from Orchestration/Application layers
2. Remove service interfaces and implementations
3. Delete views (.cshtml) and view models
4. Update controllers (remove actions, fix using statements)
5. Update navigation links in remaining views
6. Remove WebFeatures.sql entries for deleted actions
7. Update DependencyInjection.cs service registrations

### 4. Verification
- Build must be clean (0 errors, 0 warnings)
- All navigation links must point to unified interface
- Database seed scripts must be consistent
- Test with all affected user roles

**Example: Bulk Invitation Consolidation**
- Kept: BulkInviteParticipantsCommand (better UI with Excel support)
- Removed: ProcessBatchUserRegistrationOrchestrationCommand
- Enhanced with: CreateProjectInvitationCommand integration, progress tracking
- Result: Single implementation serving all roles