# Implementation Plan: Notification Generic Delivery

**Branch**: `014-notification-generic-delivery` | **Date**: 2026-04-14 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/014-notification-generic-delivery/spec.md`

## Summary

Refactor the Notification domain from a content-aware delivery system into a pure delivery service. Template rendering moves to originating domains (Access, Tenant), a new `NotificationRequestedEvent` contract in `Mentoory.Notification.Contracts` becomes the single cross-domain integration point, and the `LoginContext` value object and its database columns are removed. A shared email layout wrapper in `Mentoory.Shared` ensures consistent branding across all domains.

## Technical Context

**Language/Version**: C# / .NET 10.0 (SDK 10.0.0)  
**Primary Dependencies**: ASP.NET Core MVC, MediatR 14.1, FluentValidation 12.1, EF Core 10.x, MailKit/MimeKit, RazorLight 2.3.1  
**Storage**: SQL Server with SSDT/DACPAC schema management  
**Testing**: xUnit, Moq, FluentAssertions  
**Target Platform**: Linux/Windows server (modular monolith)  
**Project Type**: web-service (ASP.NET Core MVC with Aspire 13.2.0 orchestration)  
**Performance Goals**: N/A (no new endpoints or user-facing performance changes)  
**Constraints**: Zero warnings build, Spanish-first UI, Clean Architecture layer boundaries  
**Scale/Scope**: 3 notification types migrated, 1 new project created, ~12 files modified

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Clean Architecture Layer Boundaries | PASS | Template rendering interfaces in Application layers, implementations in Infrastructure. Notification domain does not reference other domains. |
| II. CQRS Pattern Requirements | N/A | No new commands or queries — only integration event handlers. |
| III. Domain-Driven Design Constraints | PASS | Notification aggregate simplified (fewer concerns). LoginContext value object removed. No new cross-aggregate object references. |
| IV. Integration Events | PASS | `NotificationRequestedEvent` placed in `Mentoory.Notification.Contracts` following established `*.Contracts` pattern (Access.Contracts, Tenant.Contracts). Only event contracts cross domain boundaries. |
| V. Zero-Warnings Policy | PASS | All removed references, unused imports, and dead code will be cleaned up. |
| VI. DateTime Handling | PASS | Handlers use `ITimeProvider` for `ScheduledForUtc` and `CreatedAtUtc`. Integration events receive DateTime as constructor parameters. |
| VII. Naming Conventions | PASS | `NotificationRequestedEvent` follows `{Noun}{Action}Event` pattern consistent with existing events. Handler follows `{EventName}Handler` pattern. |
| VIII. File Organization | PASS | One class per file. Templates as embedded resources in Infrastructure projects. |
| IX. Spanish-First UI | PASS | Email templates remain in Spanish. Shared layout footer text in Spanish. |
| X. Role Hierarchy & Session Context | N/A | No controllers or authorization changes. |
| XI. SSDT/DACPAC Database Strategy | PASS | Column removal via SSDT schema change. No EF migrations. |

## Project Structure

### Documentation (this feature)

```text
specs/014-notification-generic-delivery/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── contracts/           # Phase 1 output
└── tasks.md             # Phase 2 output (via /speckit.tasks)
```

### Source Code (repository root)

```text
# New project
Mentoory.Notification.Contracts/
├── Mentoory.Notification.Contracts.csproj    # References Mentoory.Shared.Application
└── IntegrationEvents/
    └── NotificationRequestedEvent.cs         # Generic notification delivery event

# Modified: Shared layer (new layout wrapper)
Mentoory.Shared.Application/
└── Notifications/
    └── IEmailLayoutWrapper.cs                # Interface for brand layout wrapping

Mentoory.Shared.Infrastructure/
└── Notifications/
    └── EmailLayoutWrapper.cs                 # Implementation: wraps inner HTML in Mentoory layout

# Modified: Access domain (gains template rendering responsibility)
Mentoory.Access.Application/
└── IntegrationEvents/
    ├── UserRegisteredNotificationHandler.cs  # Renders welcome email + publishes NotificationRequestedEvent
    └── LoginAttemptNotificationHandler.cs    # Renders login alert + publishes NotificationRequestedEvent

Mentoory.Access.Infrastructure/
├── Templates/
│   ├── UserRegistration.cshtml               # Moved from Notification.Infrastructure
│   ├── LoginAlert.cshtml                     # Moved from Notification.Infrastructure
│   └── SuspiciousLoginAlert.cshtml           # Moved from Notification.Infrastructure
├── Services/
│   └── AccessTemplateRenderer.cs             # RazorLight renderer for Access templates
└── DependencyInjection.cs                    # Register template renderer

# Modified: Tenant domain (gains template rendering responsibility)
Mentoory.Tenant.Application/
└── IntegrationEvents/
    └── InvitationReissuedNotificationHandler.cs  # Renders invitation + publishes NotificationRequestedEvent

Mentoory.Tenant.Infrastructure/
├── Templates/
│   └── ProjectInvitation.cshtml              # Moved from Notification.Infrastructure
├── Services/
│   └── TenantTemplateRenderer.cs             # RazorLight renderer for Tenant templates
└── DependencyInjection.cs                    # Register template renderer

# Modified: Notification domain (simplified to pure delivery)
Mentoory.Notification.Domain/
├── Aggregates/Notification/
│   └── Notification.cs                       # Remove LoginContext property, simplify Create()
└── ValueObjects/
    └── LoginContext.cs                       # DELETED

Mentoory.Notification.Application/
└── IntegrationEvents/
    ├── NotificationRequestedHandler.cs       # NEW: single generic handler
    ├── LoginAttemptNotificationHandler.cs     # DELETED
    ├── UserRegisteredNotificationHandler.cs   # DELETED
    └── InvitationReissuedNotificationHandler.cs  # DELETED

Mentoory.Notification.Infrastructure/
├── Persistence/
│   └── NotificationDbContext.cs              # Remove OwnsOne(LoginContext) mapping
├── Services/
│   ├── NotificationQueueService.cs           # Replace 3 type-specific methods with 1 generic QueueAsync()
│   ├── RazorLightTemplateRenderer.cs         # DELETED (or kept if still needed by Notification)
│   └── ITemplateRenderer.cs                  # Moved to Shared.Application
└── Templates/                                # DELETED (all templates moved to originating domains)
    ├── _EmailLayout.cshtml                   # Content extracted into EmailLayoutWrapper
    ├── UserRegistration.cshtml               # Moved to Access.Infrastructure
    ├── ProjectInvitation.cshtml              # Moved to Tenant.Infrastructure
    ├── LoginAlert.cshtml                     # Moved to Access.Infrastructure
    └── SuspiciousLoginAlert.cshtml           # Moved to Access.Infrastructure

# Database schema change
Mentoory.Db/notification/Tables/
└── Notifications.sql                         # Remove LoginContext_* columns
```

**Structure Decision**: Follows existing modular monolith structure. Each domain gains its own `Templates/` directory as embedded resources in its Infrastructure project. The `ITemplateRenderer` interface moves to `Mentoory.Shared.Application` since multiple domains need it. The shared layout becomes a simple HTML wrapper service (not a Razor template) to avoid cross-assembly template dependencies.

## Complexity Tracking

> No constitution violations requiring justification.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| (none)    | —          | —                                   |
