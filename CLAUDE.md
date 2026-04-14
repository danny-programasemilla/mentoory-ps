# Mentoory Project Memory

## Project Overview
- **Platform**: ASP.NET Core 10 business incubator management system
- **Database**: SQL Server with SSDT/DACPAC schema management and Entity Framework Core 10.x ORM
- **Architecture**: Clean Architecture with Domain-Driven Design (modular monolith)
- **Cloud Native**: .NET Aspire 13.2.0 for orchestration and observability
- **Frontend**: Razor Views with Tabler Admin Template (built on Bootstrap 5)
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

## Active Technologies
- C# / .NET 10.0 (SDK 10.0.0 with pre-release) + ASP.NET Core MVC, MediatR 14.1, FluentValidation 12.1, Mapperly 4.x, EF Core 10.x, MailKit/MimeKit (001-mentory-platform-core)
- SQL Server with SSDT/DACPAC schema management, EF Core 10.x ORM (001-mentory-platform-core)
- C# / .NET 10.0 (SDK 10.0.0) + ASP.NET Core MVC + MediatR 14.1, FluentValidation 12.1, Mapperly 4.x, EF Core 10.x, MailKit/MimeKit (002-phase1-4-hardening)
- C# / .NET 10.0 (SDK 10.0.0) + ASP.NET Core MVC, MediatR 14.1, FluentValidation 12.1, Mapperly 4.x, EF Core 10.x, MailKit/MimeKit (003-merge-identity-auth-domains)
- C# / .NET 10.0 (SDK 10.0.0) + ASP.NET Core MVC, MediatR 14.1, FluentValidation 12.1, Mapperly 4.x, EF Core 10.x, CsvHelper (004-phase1-3-hardening)
- Markdown governance documentation (no application code) + Existing codebase analysis (PlatformRole enum, Permission enum, CheckPermissionHandler, RoleAssignment aggregate, TenantContextMiddleware, ITenantContext, Authorize attributes) (005-access-security-constitution)
- `.specify/memory/access-security-constitution.md` — version-controlled governance artifact (005-access-security-constitution)
- C# / .NET 10.0 (SDK 10.0.0) + ASP.NET Core MVC, CsvHelper (007-csv-sample-download)
- N/A (no database changes) (007-csv-sample-download)
- C# / .NET 10.0 (SDK 10.0.0) + ASP.NET Core MVC + MediatR 14.1, FluentValidation 12.1, EF Core 10.x, Tabler Admin Template (built on Bootstrap 5) (008-context-selector-ux)
- SQL Server (existing RoleAssignments table — no schema changes) (008-context-selector-ux)
- C# / .NET 10.0, Razor Views, CSS, JavaScript + Tabler v1.4.0 (Bootstrap 5), jQuery, DataTables 2.3.4, MediatR 14.1 (010-design-system-ux-polish)
- SQL Server (read-only count queries for dashboard metrics — no schema changes) (010-design-system-ux-polish)
- C# / .NET 10.0 + Razor Views + CSS + Tabler v1.4.0 (Bootstrap 5), Tabler Icons Webfont, jQuery, DataTables 2.3.4 (011-dashboard-ui-polish)
- SQL Server (read-only — existing `GetDashboardMetricsQuery`, no changes) (011-dashboard-ui-polish)

## Code Review Standards
After completing any implementation, review the code for:
- Methods longer than 30 lines (likely doing too much — extract private helpers)
- Logic duplicated more than twice (extract to shared utility or base class)
- Domain entities accepting invalid state at construction (guard clauses in factory methods)
- Commands/handlers with 7+ parameters (group into value objects or nested records)
- Magic strings where enums or constants exist in the codebase
- Dead code: unused value objects, unreachable switch branches, methods never called
- `AsNoTracking()` missing on read-only query paths (repositories, query handlers)
- EF Include() chains loading more data than the caller needs

Run /simplify before presenting code to the user.

## Recent Changes
- 011-dashboard-ui-polish: Added C# / .NET 10.0 + Razor Views + CSS + Tabler v1.4.0 (Bootstrap 5), Tabler Icons Webfont, jQuery, DataTables 2.3.4
- 010-design-system-ux-polish: Added C# / .NET 10.0, Razor Views, CSS, JavaScript + Tabler v1.4.0 (Bootstrap 5), jQuery, DataTables 2.3.4, MediatR 14.1
- 009-tabler-template-migration: Added Tabler Admin Template (@tabler/core), Tabler Icons Webfont (@tabler/icons-webfont); removed standalone Bootstrap 5 CSS/JS and Font Awesome
