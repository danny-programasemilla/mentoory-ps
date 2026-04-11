<!--
  ============================================================
  SYNC IMPACT REPORT
  ============================================================
  Version change: 1.1.0 -> 1.1.1

  Patches:
    - Added Related Governance Documents section linking to
      Access & Security Constitution (access-security-constitution.md)
    - Updated version footer to 1.1.1

  Previous version: 1.0.1 -> 1.1.0

  Minor:
    - Added Principle X: Role Hierarchy & Session Context
    - Renumbered SSDT/DACPAC Database Strategy to XI
    - Establishes: GlobalAdmin has full access, IncubatorAdmin
      inherits project-scope access, [Authorize] must include
      higher roles, MenuConfiguration must include GlobalAdmin
      in all groups, controllers must handle missing context

  Previous version: 1.0.0 -> 1.0.1

  Patches:
    - Updated Aspire version from 13.0.2 to 13.2.0 to match
      actual project dependency (plan.md, CLAUDE.md, AppHost)

  Previous version: (none) -> 1.0.0 (initial ratification)

  Added principles:
    - I. Clean Architecture Layer Boundaries
    - II. CQRS Pattern Requirements
    - III. Domain-Driven Design Constraints
    - IV. Integration Events (ADR-001)
    - V. Zero-Warnings Policy
    - VI. DateTime Handling
    - VII. Naming Conventions
    - VIII. File Organization
    - IX. Spanish-First UI
    - X. SSDT/DACPAC Database Strategy

  Added sections:
    - Mission & Scope
    - Core Principles (10 principles)
    - Technology Standards (Web Layer, Testing, Dependencies)
    - Specification Validation & Enforcement
    - Governance

  Removed sections: (none — first version)

  Templates requiring updates:
    - .specify/templates/plan-template.md        — ⚠ pending
      (Constitution Check section is generic; future /speckit.plan
       invocations will dynamically populate gates from this file)
    - .specify/templates/spec-template.md         — ✅ compatible
      (No constitution-specific sections; spec workflow reads
       constitution at runtime)
    - .specify/templates/tasks-template.md        — ✅ compatible
      (Task phases are feature-driven; constitution validation
       occurs at generation time)
    - .specify/templates/checklist-template.md    — ✅ compatible
    - .specify/templates/agent-file-template.md   — ✅ compatible

  Follow-up TODOs: (none)
  ============================================================
-->

# Mentoory Constitution

## Mission & Scope

Mentoory is an enterprise-grade ASP.NET Core 10 business incubator
management system targeting Spanish-speaking markets. The platform
manages incubators, projects, participants, knowledge structures,
diagnostics, mentoring relationships, subscriptions, and
notifications. The architecture is a modular monolith designed for
eventual microservice extraction.

All specifications MUST assume:

- Spanish-language UI (all user-facing strings, validation messages,
  labels)
- .NET 10.0 target framework with Aspire 13.2.0 orchestration
- SQL Server database with SSDT project (no EF migrations)
- Clean Architecture with mandatory layer separation

## Core Principles

### I. Clean Architecture Layer Boundaries

Layer separation is enforced at build time via project references.
Every specification and implementation MUST respect the following
boundaries:

1. **Domain Layer**: Pure business logic, aggregates, value objects,
   domain events. NO framework dependencies (no EF attributes, no
   ASP.NET types).
2. **Application Layer**: Commands, queries, handlers, DTOs,
   integration events. References Domain only.
3. **Infrastructure Layer**: EF Core DbContext, repository
   implementations, external services. References Domain and
   Application.
4. **Web Layer**: Controllers, views, view models, filters.
   References all layers but MUST NEVER bypass Application for
   data access.

**Enforcement**: Any specification that violates layer boundaries
MUST be rejected.

### II. CQRS Pattern Requirements

All command/query operations MUST follow these patterns:

- Commands use `IBaseRequest` or `IBaseRequest<TResult>`
  (NOT `IRequest<Result<T>>`)
- Handlers inherit from `BaseCommandHandler<TRequest>` or
  `BaseCommandHandler<TRequest, TResult>`
- Handlers return `Success()` or `Success(data)` directly;
  nested Results are forbidden
- FluentValidation validators are REQUIRED for all commands
  that accept user input
- Queries MUST be side-effect-free

### III. Domain-Driven Design Constraints

Domain modeling MUST follow these non-negotiable rules:

- Aggregate roots control all modifications to child entities
- Collections use private backing fields (`_items = new()`)
  with `.AsReadOnly()` public access
- Navigation properties are marked `internal` for EF Core
- Cross-aggregate references use ID only, never object
  references
- Value objects are self-validating via factory methods
- External-facing entities MUST have `ExternalId` (Guid);
  routes use ExternalId, NEVER internal IDs

### IV. Integration Events (ADR-001)

Cross-domain communication via integration events is permitted
under these constraints:

- Events are placed in the originating domain's
  `Application/IntegrationEvents/` directory
- Handlers implement `INotificationHandler<TEvent>`
- Only event contracts and constants may be referenced across
  domains — no business logic sharing

### V. Zero-Warnings Policy

The build configuration enforces
`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`. All
StyleCop violations, nullable warnings, and compiler warnings
MUST be resolved. Specifications that would introduce warnings
are invalid.

**Rationale**: Zero-warnings ensures consistent code quality and
prevents technical debt accumulation at the compiler level.

### VI. DateTime Handling

NEVER use `DateTime.UtcNow` or `DateTime.Now` directly in Domain
or Application layers. The following rules are CRITICAL:

- **Domain**: Accept DateTime as method/constructor parameters
- **Application**: Inject `ITimeProvider` and use
  `timeProvider.UtcNow`
- **Integration Events**: DateTime as constructor parameter,
  never as a computed property

**Rationale**: Testability and deterministic behavior require
explicit time injection.

### VII. Naming Conventions

All code artifacts MUST follow these naming patterns:

| Artifact    | Pattern                              | Example                     |
|-------------|--------------------------------------|-----------------------------|
| Command     | `{Verb}{Entity}Command`              | `CreateProjectCommand`      |
| Query       | `{Get\|List}{Entity}Query`           | `GetProjectByIdQuery`       |
| Handler     | `{CommandName}Handler`               | `CreateProjectCommandHandler` |
| DTO         | `{Entity}Dto`, `{Entity}DetailsDto`  | `ProjectDetailsDto`         |
| ViewModel   | `{Action}{Entity}ViewModel`          | `CreateProjectViewModel`    |
| Repository  | `I{Aggregate}Repository` / `{Aggregate}Repository` | `IProjectRepository` |

### VIII. File Organization

- One class per file; filename MUST match class name
- JavaScript MUST reside in `/wwwroot/js/` (not in Views folders)
- SQL files use UTF-8 encoding without BOM
- PostDeployment scripts in `/PostDeployment/` (outside `Db/`
  folder to avoid MSBuild.Sdk.SqlProj validation)

### IX. Spanish-First UI

ALL user-facing text MUST be in Spanish:

- Validation messages: `"El campo es requerido"`
- Toast notifications: `"Operacion exitosa"`, `"Error al guardar"`
- Labels, buttons, headings, error pages
- Email templates

Code comments, variable names, and documentation MUST remain in
English.

### X. Role Hierarchy & Session Context

The platform uses a **hierarchical role model** with session-scoped context
selection. This principle MUST be followed in ALL specifications and
implementations:

#### Role Hierarchy (higher roles inherit lower-role access)

1. **GlobalAdmin**: Full access to ALL incubators, ALL projects,
   ALL features. Must be included in `[Authorize(Roles = "...")]`
   on every controller.
2. **IncubatorAdmin**: Full access within their assigned incubator
   and all its projects. Must be included alongside ProjectCoordinator
   on project-scoped controllers.
3. **ProjectCoordinator**: Manages assigned project's diagnostic
   forms, knowledge structures, mentoring plans.
4. **Mentor**: Manages mentoring sessions and assignments within
   assigned projects.
5. **Entrepreneur**: Submits diagnostics, completes learning,
   attends sessions within their project.
6. **Sponsor**: Read-only dashboards.

#### Authorization Rules

- `[Authorize]` attributes MUST include the target role AND all
  higher roles. Example: a ProjectCoordinator-scoped controller
  uses `[Authorize(Roles = "ProjectCoordinator,IncubatorAdmin,GlobalAdmin")]`.
- Menu items (`MenuConfiguration.cs`) MUST include higher roles
  in the roles array. GlobalAdmin MUST appear in every menu group.

#### Session Context

- Features are context-scoped: they require an active incubator
  and/or project selected via `/Context/Select`.
- Controllers that read `ActiveProjectId` or `ActiveIncubatorId`
  claims MUST handle missing context gracefully — redirect to
  context selection with a message, never crash.
- GlobalAdmin users operating in global scope (IncubatorId=0)
  should still be able to access listing/read-only views.

**Enforcement**: Any specification that restricts a controller to
a single role without including higher-privilege roles MUST be
rejected. Any menu group that excludes GlobalAdmin MUST be rejected.

### XI. SSDT/DACPAC Database Strategy

Database schema is managed via SQL Server Database Project
(`Mentoory.Db/MentooryDb.sqlproj`). Entity Framework migrations are forbidden.

- Seed data via numbered PostDeployment scripts (000-014+)
- Scripts MUST be idempotent (safe for re-execution)
- Index syntax: INCLUDE clause BEFORE WHERE clause
- Computed columns in filtered indexes MUST be PERSISTED
- No DBCC commands in post-deployment scripts
- EF Core: Use string-based Include for private collections:
  `.Include("_projectStages")`
- Repository pattern with interfaces in Domain, implementations
  in Infrastructure
- Unit of Work for `SaveChangesAsync`

## Technology Standards

### Web Layer Patterns

#### Controller Patterns

- Inherit from `BaseController` with `MediatorExecutor` injection
- NEVER inject repositories into controllers
- Use extension methods:
  `this.SetSuccessToast()`,
  `this.MapErrorsToModelStateAndSetErrorToast<T>()`
- File uploads: Convert `IFormFile` to `Stream` at controller
  boundary before passing to commands

#### UI Framework

- Bootstrap 5 with Phoenix Admin Template
- Toast notifications via `showToast(message, type)` JavaScript
  function
- DataTables with server-side processing
- jsTree for hierarchical data visualization
- Form wizards with auto-save (30s interval, 5s debounce)

### Testing Requirements

- **Framework**: xUnit for unit tests
- **Mocking**: Moq
- **Assertions**: FluentAssertions
- **Database cleanup**: Respawn for integration tests
- **In-memory provider**: EF Core InMemory for unit testing
- **Project naming**: `{Domain}.Tests`

### Dependency Governance

#### Approved Technologies

| Library               | Purpose              |
|-----------------------|----------------------|
| MediatR 14.x         | CQRS                 |
| FluentValidation 12.x| Validation           |
| Riok.Mapperly 4.x    | Object mapping (source gen) |
| Entity Framework Core 10.x | ORM             |
| MailKit / MimeKit     | Email                |
| EPPlus                | Excel generation     |
| CsvHelper             | CSV processing       |

#### Forbidden Patterns

- AutoMapper (use Mapperly instead)
- Dapper for primary data access (use EF Core repositories)
- Global exception handling that swallows errors
- Service locator anti-pattern
- Static classes for business logic

## Specification Validation & Enforcement

### Pre-Finalization Validation Rules

Before any `/speckit.specify`, `/speckit.plan`, or `/speckit.tasks`
output is finalized, the following checks MUST pass:

1. Does the specification respect Clean Architecture layers?
   (Principle I)
2. Are all DateTime values passed via parameters or
   `ITimeProvider`? (Principle VI)
3. Is all user-facing text in Spanish? (Principle IX)
4. Do new entities include `ExternalId` for external exposure?
   (Principle III)
5. Are CQRS patterns followed (correct base classes, Result
   handling)? (Principle II)
6. Would implementation introduce any compiler warnings?
   (Principle V)
7. Are integration events correctly placed and minimal in scope?
   (Principle IV)
8. Does database work follow SSDT/PostDeployment conventions?
   (Principle XI)
9. Do `[Authorize]` attributes include all higher-privilege
   roles? Do menu groups include GlobalAdmin? (Principle X)

If any validation fails, the specification MUST be revised before
proceeding.

### Enforcement Protocol

This constitution is binding. Agents executing `/speckit` workflows
MUST:

1. Load this constitution at the start of each workflow phase
2. Validate all outputs against these principles
3. Reject or revise any output that violates these mandates
4. Document any ambiguity or edge cases encountered for future
   constitution updates

## Governance

### Amendment Procedure

1. Propose amendment with rationale and affected principles
2. Document the change in the Sync Impact Report (HTML comment
   at top of this file)
3. Update version per semantic versioning rules
4. Propagate changes to dependent templates if affected

### Versioning Policy

- **MAJOR**: Backward-incompatible governance/principle removals
  or redefinitions
- **MINOR**: New principle/section added or materially expanded
  guidance
- **PATCH**: Clarifications, wording, typo fixes, non-semantic
  refinements

### Related Governance Documents

- [Access & Security Constitution](access-security-constitution.md) —
  Foundational governance for role hierarchy, scope boundaries,
  permission matrix, threat model, and secure feature delivery
  workflow. Independently versioned.

### Compliance Review

All specifications and implementations produced by `/speckit`
workflows are subject to validation against this constitution.
Non-compliant outputs MUST be revised before approval.

**Version**: 1.1.1 | **Ratified**: 2026-01-27 | **Last Amended**: 2026-04-08
