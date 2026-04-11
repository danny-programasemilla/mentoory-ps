# Mentoory Architecture Guide

## Clean Architecture Overview
- **Strict layer separation**: Domain → Application → Infrastructure → Web
- **Dependency rule**: Dependencies only point inward (Web → Infrastructure → Application → Domain)
- **No web concerns in inner layers**: Convert IFormFile to Stream at controller boundary
- **Modular monolith**: 7 bounded contexts, each with Domain/Application/Infrastructure projects
- **Schema-based isolation**: Each bounded context owns its SQL schema (e.g., `[access]`, `[diagnostic]`)

## Bounded Contexts

| Context | Schema | Status | Purpose |
|---------|--------|--------|---------|
| Access | `[access]` | Implemented (US1) | Users, credentials, sessions, email verification, password reset, role assignments, context selection, permissions |
| Tenant | `[tenant]` | Implemented (US1) | Incubators, projects, stages, participants, mentor assignments |
| Diagnostic | `[diagnostic]` | Implemented (US2) | Form templates, project forms, responses, answer corrections |
| Knowledge | `[knowledge]` | Scaffolded | Knowledge structures, modules, topics, subjects, resources |
| Mentoring | `[mentoring]` | Scaffolded | Plans, sessions, assignments, submissions |
| Subscription | `[subscription]` | Scaffolded | Plans, features, overrides |
| Notification | `[notification]` | Scaffolded | Email delivery, preferences |

Cross-domain communication is via **integration events** only (MediatR `INotification`).

## Project Structure

### Per Bounded Context (example: Diagnostic)
```
Mentoory.Diagnostic.Domain/
├── Aggregates/
│   ├── FormTemplate/          # Aggregate root + child entities
│   ├── ProjectForm/           # Aggregate root + child entities
│   └── DiagnosticResponse/    # Aggregate root + child entities
├── Enums/
├── ValueObjects/
└── Repositories/              # Interfaces only

Mentoory.Diagnostic.Application/
├── Commands/
│   ├── CloneFormTemplate/     # Command + Handler + Validator
│   ├── CustomizeProjectForm/
│   ├── SubmitDiagnosticResponse/
│   ├── CorrectAnswer/
│   └── SyncFromTemplate/
├── Queries/
│   ├── GetProjectForm/        # Query + Handler + DTO
│   ├── GetDiagnosticResponse/
│   ├── ListFormTemplates/
│   └── GetTopicScoreAggregation/
├── IntegrationEvents/
└── DependencyInjection.cs

Mentoory.Diagnostic.Infrastructure/
├── Persistence/
│   ├── DiagnosticDbContext.cs
│   └── Repositories/
└── DependencyInjection.cs
```

### Shared Kernel
```
Mentoory.Shared.Domain/
├── SeedWork/                  # Entity, ValueObject, IAggregateRoot, IRepository, IUnitOfWork
└── Constants/                 # Roles

Mentoory.Shared.Application/
├── MediatR/                   # IBaseRequest, BaseCommandHandler
├── DataTables/                # DataTableRequest, DataTableResponse<T>
├── IntegrationEvents/         # IntegrationEvent, IIntegrationEventService
├── TimeProvider/              # ITimeProvider
├── Behaviors/                 # ValidatorBehavior, TransactionBehavior
├── Result.cs                  # Result pattern
├── Result{T}.cs
└── ResultErrorCodes.cs

Mentoory.Shared.Infrastructure/
├── Persistence/
│   ├── SharedAbstractDbContext.cs   # Base DbContext with IUnitOfWork + domain event dispatch
│   └── Repositories/
│       └── AbstractRepository.cs    # Base repository
└── Services/                        # DefaultSystemTimeProvider, TenantContextService
```

### Web Layer (organized by user role)
```
Mentoory.Web/
├── Areas/
│   ├── Identity/              # Login, register, password reset (unauthenticated)
│   ├── Platform/              # GlobalAdmin: incubators, users, subscriptions, templates
│   ├── Administration/        # IncubatorAdmin: projects, users, settings
│   ├── Coordination/          # ProjectCoordinator: diagnostics, knowledge, lifecycle
│   ├── Mentoring/             # Mentor: plans, sessions, assignments
│   ├── Participant/           # Entrepreneur: diagnostics, learning, submissions
│   └── Sponsor/               # Sponsor: read-only dashboards
├── Controllers/               # Root: Home, Error, Context
├── Infrastructure/
│   ├── Authentication/        # Session cookie middleware
│   ├── Authorization/         # Tenant context middleware
│   └── Menu/                  # Role-based navigation
├── Services/
│   └── MediatRExecutor.cs     # Centralized MediatR dispatch with error handling
├── Models/
│   └── DataTableServerRequest.cs
├── Views/Shared/              # Phoenix Admin layout, components
├── wwwroot/js/                # All JS files (never in Views)
└── Program.cs                 # Service registration + middleware pipeline
```

### Database
```
Mentoory.Db/                   # SSDT SQL project
├── access/
│   ├── Schema.sql
│   └── Tables/                # Users, Credentials, AuthSessions, Roles, etc.
├── tenant/Tables/
├── diagnostic/Tables/         # FormTemplates, ProjectForms, Questions, etc.
├── knowledge/Tables/
├── mentoring/Tables/
├── subscription/Tables/
├── notification/Tables/
└── audit/Tables/

Mentoory.Db.PostDeployment/    # Seed scripts (outside Mentoory.Db/)
├── 001.SeedRoles.sql
├── 002.SeedGlobalAdmin.sql
├── 003.SeedDefaultSubscriptionPlan.sql
└── Script.PostDeployment.sql
```

### Tests
```
tests/
├── Mentoory.Access.Tests/          # Domain unit tests
├── Mentoory.Tenant.Tests/
├── Mentoory.Diagnostic.Tests/
├── Mentoory.Tests.Integration/     # Integration tests (WebApplicationFactory + Testcontainers)
└── Mentoory.Tests.E2E/             # End-to-end (Playwright)
```

## Architecture Patterns

- **CQRS with MediatR**: Commands for writes, Queries for reads
- **Repository pattern**: Interfaces in Domain, implementations in Infrastructure
- **Result pattern**: `Result` / `Result<T>` for error handling at command/query boundaries
- **Integration events**: Cross-domain communication via `INotificationHandler<T>`
- **Value objects**: Self-validating domain concepts without identity
- **Multi-tenancy**: EF Core global query filters on `IncubatorId`

## Layer Responsibilities

### Domain Layer
- Aggregate roots with private constructors and static factory methods
- Child entities with `internal static` factory methods
- Value objects extending `ValueObject` with `GetEqualityComponents()`
- Enums with explicit numeric values for DB storage
- Repository interfaces (`IRepository<T>` constraint: `IAggregateRoot`)
- No framework dependencies

### Application Layer
- Commands: sealed records implementing `IBaseRequest` / `IBaseRequest<T>`
- Handlers: partial classes extending `BaseCommandHandler<T>` with `[LoggerMessage]`
- Validators: `AbstractValidator<T>` with Spanish messages
- Queries: sealed records implementing `IBaseRequest<TResult>`
- DTOs: sealed records for data transfer
- Integration events: sealed records extending `IntegrationEvent`
- `DependencyInjection.cs`: registers MediatR + FluentValidation from assembly

### Infrastructure Layer
- DbContext extending `SharedAbstractDbContext` (provides IUnitOfWork + domain event dispatch)
- Entity configurations via Fluent API in `OnModelCreating`
- Repository implementations extending `AbstractRepository<T>`
- `DependencyInjection.cs`: registers DbContext (with Aspire enrichment), repositories, services
- Connection string from `IHostApplicationBuilder.Configuration`

### Web Layer
- Controllers: `[Area]` + `[Route("[area]/[controller]")]` + `[Authorize(Roles = "...")]`
- Inject `MediatRExecutor` (not repositories, not IMediator directly)
- `SendOrThrowAsync` for queries, `SendAndLogIfFailureAsync` for commands
- DataTable endpoints: `[HttpPost("[action]")]` returning JSON
- `TempData["SuccessMessage"]` for success toasts
- `ValidateAntiForgeryToken` on all POST actions
- Spanish UI text, English code

## Key Integration Points

### Adding a New Bounded Context
1. Create `{BC}.Domain`, `{BC}.Application`, `{BC}.Infrastructure` projects
2. Add project references (Domain → Shared.Domain; App → Domain + Shared.App; Infra → Domain + Shared.Infra)
3. Create DbContext extending `SharedAbstractDbContext`
4. Register in `Program.cs`: `builder.Services.Add{BC}Application()` + `builder.Add{BC}Infrastructure()`
5. Create SSDT schema + tables in `Mentoory.Db/{schema}/`

### Adding a New Feature to an Existing Context
1. Domain: Add/modify aggregates, entities, value objects
2. Application: Add Command + Handler + Validator (or Query + Handler + DTO)
3. Infrastructure: Update DbContext entity configuration if needed
4. Web: Add controller action + view in the appropriate Area
5. SSDT: Add/modify table definitions
6. Tests: Domain unit tests + integration tests

### Dependency Injection Pattern
```csharp
// Application layer (IServiceCollection extension)
public static IServiceCollection AddDiagnosticApplication(this IServiceCollection services)
{
    services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(Assembly.GetExecutingAssembly()));
    FluentValidation.AssemblyScanner
        .FindValidatorsInAssembly(Assembly.GetExecutingAssembly())
        .ForEach(item => services.AddScoped(item.InterfaceType, item.ValidatorType));
    return services;
}

// Infrastructure layer (IHostApplicationBuilder extension)
public static IHostApplicationBuilder AddDiagnosticInfrastructure(
    this IHostApplicationBuilder builder, string connectionName = "DefaultConnection")
{
    var cs = builder.Configuration.GetConnectionString(connectionName)
        ?? throw new InvalidOperationException($"Connection string '{connectionName}' not found.");
    builder.Services.AddDbContext<DiagnosticDbContext>(opts => opts.UseSqlServer(cs));
    builder.EnrichSqlServerDbContext<DiagnosticDbContext>(s => s.CommandTimeout = 30);
    builder.Services.AddScoped<IFormTemplateRepository, FormTemplateRepository>();
    return builder;
}
```

### .NET Aspire Integration
- `Mentoory.Aspire.AppHost` orchestrates Web + SQL Server resources
- Each DbContext uses `EnrichSqlServerDbContext<T>()` for connection resilience and health checks
- Single SQL Server database with schema-based isolation per bounded context
