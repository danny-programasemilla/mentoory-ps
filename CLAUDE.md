# Mentoory Project Memory

## Project Overview
- **Platform**: ASP.NET Core 10 business incubator management system
- **Database**: SQL Server with SSDT/DACPAC schema management and Entity Framework Core 10.x ORM
- **Architecture**: Clean Architecture with Domain-Driven Design (modular monolith)
- **Cloud Native**: .NET Aspire 13.2.0 for orchestration and observability
- **Frontend**: Razor Views with Bootstrap 5 (Phoenix Admin Template)
- **Authentication**: Not defined yet, but should avoid vendor lock
- **Language**: Spanish UI (all user-facing text)

## Key Commands
- **Build**: `dotnet build`d
- **Run with Aspire**: `dotnet run --project Mentoory.Aspire.AppHost`
- **Run Web Only**: `dotnet run --project Mentoory.Web`
- **Test**: `dotnet test`
- **Database Build**: `cd Mentoory.Db && publish-mentoorydb.sh` (generates DACPAC with PostDeployment scripts)

## Knowledge Base
**Quick lookup by scenario:**

| Scenario | Documentation |
|----------|---------------|
| Architectural governance | [constitution.md](.specify/memory/constitution.md) ← **overrides this file** |
| Approved technologies | [constitution.md](.specify/memory/constitution.md) (§ Approved Technologies) |
| SpecKit workflows | [`.specify/templates/`](.specify/templates/) (spec, plan, tasks, checklist) |
| Creating features | [architecture.md](.claude/architecture.md), [web-patterns.md](.claude/web-patterns.md) |
| Code patterns / web conventions | [web-patterns.md](.claude/web-patterns.md) |
| Domain changes | [ddd-patterns.md](.claude/ddd-patterns.md), [domain-reference.md](.claude/domain-reference.md) |
| Build errors | [coding-standards.md](.claude/coding-standards.md), [common-issues.md](.claude/common-issues.md) |

## Feature Workflow

New features follow the SpecKit pipeline:

1. `/speckit.specify` — Create feature spec in `specs/{###-feature-name}/spec.md`
2. `/speckit.plan` — Generate implementation plan + supporting docs
3. `/speckit.tasks` — Generate dependency-ordered task list
4. `/speckit.implement` — Execute tasks with checkpoints per user story

Feature artifacts live in `specs/{###-feature-name}/`. See [`.specify/templates/`](.specify/templates/) for template details.

## Critical Rules

> Full governance: [constitution.md](.specify/memory/constitution.md) (overrides this file)

- `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` — zero warnings before committing
- All UI text in Spanish; code and docs in English
- No web dependencies in Domain/Application; controllers never inject repositories
- Never `DateTime.UtcNow` — use `ITimeProvider` (Application) or pass as parameter (Domain)
- External entities need `ExternalId` (Guid); routes use ExternalId, never internal IDs
- Commands: `IBaseRequest`/`IBaseRequest<TResult>`, handlers: `BaseCommandHandler<T>`, FluentValidation for input
- PostDeployment scripts at `/Mentoory.Db.PostDeployment/` (project root, not inside `Mentoory.Db/`)
- Security: `[Authorize(Roles = "...")]` on controllers; no WebFeatures table
- Forbidden: AutoMapper (use Mapperly), Dapper for primary access (use EF), service locator, static business logic, swallowing exceptions

## Project Layout
```
/Areas/{AreaName}/Controllers|Models|Views  # Area-based structure
/Domain/Aggregates/{Aggregate}/            # Domain entities
/Application/{Feature}/Commands|Queries/   # CQRS operations
/Infrastructure/Persistence/               # EF Core implementations
/wwwroot/js/                               # All JavaScript files (NOT in Views)
```

## Project Context
- **Base Branch**: Always work from `develop`, not `main`
