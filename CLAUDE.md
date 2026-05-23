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
- C# / .NET 10.0 + Razor Views + CSS + JavaScript + Tabler v1.4.0 (Bootstrap 5), DataTables 2.3.4, jQuery, Tabler Icons Webfont (012-table-polish)
- C# / .NET 10.0 + JavaScript (vanilla, ES5-compatible) + DataTables 2.3.4, jQuery, Tabler v1.4.0 (Bootstrap 5), Tabler Icons Webfont (013-table-filtering)
- N/A (no schema changes) (013-table-filtering)
- C# / .NET 10.0 (SDK 10.0.0) + ASP.NET Core MVC, MediatR 14.1, FluentValidation 12.1, Mapperly 4.x, EF Core 10.x, Microsoft.Extensions.Logging (source-generated `[LoggerMessage]`) (016-registration-access-hardening)
- SQL Server (no schema changes — all changes are behavioural at the Application and Web layers) (016-registration-access-hardening)
- C# / .NET 10.0 (SDK 10.0.0) — tool and all test projects + xUnit 2.x (`TraitAttribute`), `System.Reflection.MetadataLoadContext`, `System.CommandLine` (CLI parsing), MSBuild custom `.targets`, `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory` — existing), `Testcontainers.MsSql` (existing), `Microsoft.Playwright` (existing) (018-access-security-delivery-quality-gate)
- N/A for the tool. Integration tests use the existing Testcontainers SQL Server fixture; no new tables, no new seed data. (018-access-security-delivery-quality-gate)
- C# / .NET 10.0 (SDK 10.0.0) + ASP.NET Core MVC, MediatR 14.1, FluentValidation 12.1, Riok.Mapperly 4.x, EF Core 10.x, Tabler Admin Template (Bootstrap 5), DataTables 2.3.4, jQuery, Tabler Icons Webfont (016-project-lifecycle-finish)
- SQL Server with SSDT/DACPAC schema (`Mentoory.Db`). Existing tables `tenant.Projects` and `tenant.ProjectStages` are reused. One additive schema change: add `RowVersion` optimistic-concurrency column to `tenant.Projects`. (016-project-lifecycle-finish)
- C# / .NET 10.0 (SDK 10.0.0) + ASP.NET Core MVC, MediatR 14.1, FluentValidation 12.1, Mapperly 4.x, EF Core 10.x, System.Text.Json (BCL), Microsoft.Data.SqlClient, Tabler v1.4.0 + DataTables 2.3.4 + jQuery (016-audit-pipeline)
- SQL Server via SSDT/DACPAC — `[audit].[AuditLog]` extended with CorrelationId, Outcome, ExceptionType, UserEmail, RoleContext + `IX_AuditLog_CorrelationId` filtered index (016-audit-pipeline)
- C# / .NET 10.0 (SDK 10.0.0) + Microsoft.Playwright (already referenced by `Mentoory.Tests.E2E`), xUnit, FluentAssertions, Microsoft.AspNetCore.Mvc.Testing, existing `PlaywrightFixture` + Testcontainers (MsSql) (017-audit-e2e)
- Read-only access to `[audit].[AuditLog]` via the existing admin viewer endpoints; no schema changes, no writes outside the feature 016 pipeline (017-audit-e2e)
- C# / .NET 10.0 (SDK 10.0.0 pre-release) + ASP.NET Core MVC 10.x, MediatR 14.1, FluentValidation 12.1, Riok.Mapperly 4.x, Entity Framework Core 10.x, MailKit/MimeKit (not used by this spec), Tabler v1.4.0 + Bootstrap 5 (UI) (016-knowledge-module-core)
- SQL Server (SSDT/DACPAC schema management; no EF migrations). New `knowledge` schema; cross-schema FK additions to `diagnostic.FormTemplates` and `diagnostic.Questions` (016-knowledge-module-core)
- C# / .NET 10.0 (SDK 10.0.0 pre-release) — test projects only + Microsoft.Playwright (Chromium headless), xUnit, FluentAssertions, Microsoft.AspNetCore.Mvc.Testing, Testcontainers.MsSql, Microsoft.SqlServer.DacFx, coverlet.collector. All already declared in `tests/Mentoory.Tests.E2E/Mentoory.Tests.E2E.csproj` and `tests/Mentoory.Tests.Integration/Mentoory.Tests.Integration.csproj`; no new packages required. (016-knowledge-module-core)
- Ephemeral `mcr.microsoft.com/mssql/server:2022-latest` container per test collection; schema deployed from `Mentoory.Db/bin/Debug/MentooryDb.dacpac` with PostDeployment seeds `001–005`. Respawn resets DB state between integration tests (not between E2E tests — E2E tests rely on fresh-per-collection + unique-per-test identifiers). (016-knowledge-module-core)
- C# / .NET 10.0 (SDK 10.0.0) + ASP.NET Core MVC (Razor views), Tabler v1.4.0 (Bootstrap 5), Tabler Icons Webfont; existing `context-switcher.js` / `context-selector.js` (reused, unchanged) (020-sidebar-context-footer)
- N/A — no schema changes. Reads existing cookie claims (`ActiveRole`, `ActiveIncubatorName`, `ActiveProjectName`) + one new claim (`CanSwitchContext`) (020-sidebar-context-footer)

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
- 020-sidebar-context-footer: Added C# / .NET 10.0 (SDK 10.0.0) + ASP.NET Core MVC (Razor views), Tabler v1.4.0 (Bootstrap 5), Tabler Icons Webfont; existing `context-switcher.js` / `context-selector.js` (reused, unchanged)
- 018-access-security-delivery-quality-gate: Shipped the access-security delivery quality gate — coverage-enforcement tool at `tools/Mentoory.Specs.CoverageCheck/` (parses `spec.md`, reflects xUnit `[Trait]` attributes via `MetadataLoadContext`, reports Unclaimed/Dangling/Duplicate/MissingFloor/MalformedExclusion/Reflection violations); MSBuild integration via `Directory.Build.targets` (gate fires after solution `Build`); GitHub Actions workflow `.github/workflows/coverage-check.yml`; constitution amendment v1.1.0 adding floor-category Sections 11.9-11.14 and Section 13 (Delivery Quality Gate); feature-016 retrofit (`[Trait("Spec","FR-016-NN")]`, `[Trait("Sc","SC-016-NN")]`, `[Trait("Floor","...")]`); seven new feature-016 scenarios closing the PR #13 coverage gap (admin dup-NID + fresh + unauth, 50-probe response sweep with `AntiforgeryStripper`, defense-in-depth, password-identifying-data admin half + form-state preservation E2E). Adopted prefixed identifier convention `(FR|SC)-DDD-DD` for opted-in specs (016, 018) to avoid global ID collision.
- 016-registration-access-hardening: Closed the public-registration enumeration oracle (`RegisterUserHandler` now always returns `Success()` and logs the real outcome); split admin enrollment into a dedicated `AdminEnrollUserCommand` that keeps field-attributed duplicate errors; extracted a shared `IUserProvisioningService`, `MustBeStrongPassword()` and `MustNotContainIdentifyingData()` FluentValidation extensions under `Mentoory.Access.Application/Validation/`.

<!-- SPECKIT START -->
For additional context about technologies to be used, project structure,
shell commands, and other important information, read the current plan
<!-- SPECKIT END -->
