# Implementation Plan: Notification Database Configuration

**Branch**: `013-notification-db-config` | **Date**: 2026-04-13 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/012-notification-db-config/spec.md`

## Summary

Migrate all Notification domain settings (SMTP, polling, retries, base URL) from `appsettings.json` to a dedicated `notification.NotificationConfigurations` database table. Replicates the Access domain's proven `SystemConfiguration` pattern — domain entity, repository, typed reader interface — adapted with an additional `GetStringAsync` method for the 6 string-typed SMTP settings.

## Technical Context

**Language/Version**: C# / .NET 10.0 (SDK 10.0.0)
**Primary Dependencies**: ASP.NET Core MVC, MediatR 14.1, FluentValidation 12.1, EF Core 10.x, MailKit 4.15.1
**Storage**: SQL Server with SSDT/DACPAC schema management, EF Core 10.x ORM
**Testing**: xUnit, Moq, FluentAssertions
**Target Platform**: Linux server (via .NET Aspire 13.2.0 orchestration)
**Project Type**: Modular monolith web application
**Performance Goals**: Configuration reads must not degrade notification processing (<1ms per read)
**Constraints**: Zero compiler warnings (`TreatWarningsAsErrors`), no `appsettings.json` for notification settings
**Scale/Scope**: 10 configuration keys, 5 consumer classes to refactor, 1 new DB table

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| # | Principle | Status | Notes |
|---|-----------|--------|-------|
| I | Clean Architecture Layer Boundaries | PASS | Interface in Application, implementation in Infrastructure, entity in Domain |
| II | CQRS Pattern Requirements | N/A | No commands/queries in this feature |
| III | DDD Constraints | PASS | Aggregate root with ExternalId, factory method, guard clauses |
| IV | Integration Events | N/A | No cross-domain events |
| V | Zero-Warnings Policy | PASS | Must verify with `dotnet build` |
| VI | DateTime Handling | PASS | DateTime passed as parameter to `Create()`/`Update()`, no `DateTime.UtcNow` |
| VII | Naming Conventions | PASS | `NotificationConfiguration`, `INotificationConfigurationRepository`, `NotificationConfigurationReader` |
| VIII | File Organization | PASS | One class per file, SQL in `Mentoory.Db/notification/`, PostDeployment in `/Mentoory.Db.PostDeployment/` |
| IX | Spanish-First UI | PASS | Seed data descriptions in Spanish |
| X | Role Hierarchy & Session Context | N/A | No controllers or UI in this feature |
| XI | SSDT/DACPAC Database Strategy | PASS | Table via SSDT, seed via numbered PostDeployment script, idempotent |

**Post-Design Re-Check**: All principles still PASS after Phase 1 design. No violations.

## Project Structure

### Documentation (this feature)

```text
specs/012-notification-db-config/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── checklists/
│   └── requirements.md  # Spec quality checklist
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
Mentoory.Notification.Domain/
├── Aggregates/
│   └── NotificationConfiguration/
│       └── NotificationConfiguration.cs        # NEW: aggregate root
├── Enums/
│   └── NotificationConfigurationKey.cs         # NEW: typed config keys
└── Repositories/
    └── INotificationConfigurationRepository.cs # NEW: repository interface

Mentoory.Notification.Application/
├── Configuration/
│   ├── INotificationConfigurationReader.cs     # NEW: typed reader interface
│   └── NotificationSettings.cs                 # DELETE
└── IntegrationEvents/
    ├── UserRegisteredNotificationHandler.cs     # MODIFY: replace IOptions
    └── InvitationReissuedNotificationHandler.cs # MODIFY: replace IOptions

Mentoory.Notification.Infrastructure/
├── Configuration/
│   └── SmtpSettings.cs                         # DELETE
├── DependencyInjection.cs                      # MODIFY: remove IOptions, add reader
├── Persistence/
│   ├── NotificationDbContext.cs                 # MODIFY: add DbSet + config
│   └── Repositories/
│       └── NotificationConfigurationRepository.cs # NEW: repository impl
└── Services/
    ├── NotificationConfigurationReader.cs      # NEW: reader impl
    ├── SmtpEmailService.cs                     # MODIFY: replace IOptions
    ├── NotificationProcessorService.cs         # MODIFY: replace IOptions
    └── NotificationQueueService.cs             # MODIFY: replace IOptions

Mentoory.Db/
└── notification/
    └── Tables/
        └── NotificationConfigurations.sql      # NEW: SSDT table

Mentoory.Db.PostDeployment/
├── 019.SeedNotificationConfiguration.sql       # NEW: seed data
└── Script.PostDeployment.sql                   # MODIFY: add :r reference
```

**Structure Decision**: Follows existing Clean Architecture layout. New files mirror the Access domain's `SystemConfiguration` structure within the Notification domain boundary. No new projects required.

## Complexity Tracking

No constitution violations to justify — all principles pass cleanly.
