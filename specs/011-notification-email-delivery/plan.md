# Implementation Plan: Notification Domain - Email Delivery System

**Branch**: `011-notification-email-delivery` | **Date**: 2026-04-13 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/011-notification-email-delivery/spec.md`

## Summary

Implement the Notification bounded context to provide centralized, auditable email delivery. The system consumes integration events from Access and Tenant domains, checks user notification preferences, renders branded Razor templates (RazorLight engine) in Spanish, and delivers emails via a database-backed queue processed by a background service with exponential backoff retry. Two SMTP providers are supported: Mailtrap (development) and Mailgun (production) -- both via MailKit, registered as separate `IEmailService` implementations through DI.

## Technical Context

**Language/Version**: C# / .NET 10.0 (SDK 10.0.0)
**Primary Dependencies**: ASP.NET Core MVC, MediatR 14.1, FluentValidation 12.1, MailKit 4.15.1, RazorLight (TBD version -- needs addition to Directory.Packages.props), UAParser (TBD version -- needs addition to Directory.Packages.props), EF Core 10.x
**Storage**: SQL Server with SSDT/DACPAC schema management, EF Core 10.x ORM
**Testing**: xUnit, Moq, FluentAssertions, Testcontainers.MsSql, Respawn
**Target Platform**: Linux server (Aspire 13.2.0 orchestration)
**Project Type**: Modular monolith -- bounded context modules
**Performance Goals**: Email delivery within 2 minutes of triggering event; 15-second polling interval
**Constraints**: Zero compiler warnings; Spanish UI; no web dependencies in Domain/Application layers
**Scale/Scope**: 3 notification types (login alert, registration, invitation) + preference management + 2 SMTP providers

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| # | Principle | Status | Notes |
|---|-----------|--------|-------|
| I | Clean Architecture Layer Boundaries | PASS | Domain: pure entities/VOs, no framework deps. Application: handlers + events. Infrastructure: EF, MailKit, RazorLight. Web: no direct involvement (events flow via MediatR) |
| II | CQRS Pattern Requirements | PASS | Commands use `IBaseRequest`/`IBaseRequest<TResult>`, handlers inherit `BaseCommandHandler<T>`. FluentValidation for preference updates. Queries side-effect-free |
| III | DDD Constraints | PASS | Notification + NotificationPreference as aggregate roots. LoginContext, DeliveryAttempt as value objects. Cross-aggregate references by ID only. ExternalId on external-facing entities |
| IV | Integration Events (ADR-001) | PASS | Consumes events from Access/Tenant Application layers. Handlers implement `INotificationHandler<T>`. No business logic sharing across domains |
| V | Zero-Warnings Policy | PASS | All code must compile with zero warnings. Nullable annotations enforced |
| VI | DateTime Handling | PASS | Domain: DateTime as method parameters. Application: ITimeProvider injection. Events: DateTime as constructor params |
| VII | Naming Conventions | PASS | Commands: `UpdateNotificationPreferenceCommand`, handlers: `...Handler`, DTOs: `...Dto`, repositories: `INotificationRepository` |
| VIII | File Organization | PASS | One class per file. Templates in Infrastructure layer. No JS in Views |
| IX | Spanish-First UI | PASS | All email template text in Spanish. Validation messages in Spanish |
| X | Role Hierarchy & Session Context | N/A | Notification domain is system-generated, not role-gated. Preference management will need controller with proper `[Authorize]` in future UI spec |
| XI | SSDT/DACPAC Database Strategy | PASS | Tables defined in `Mentoory.Db/notification/`. PostDeployment scripts in `/Mentoory.Db.PostDeployment/`. No EF migrations |

**All gates pass. Proceeding to Phase 0.**

## Project Structure

### Documentation (this feature)

```text
specs/011-notification-email-delivery/
├── spec.md                  # Feature specification
├── plan.md                  # This file
├── research.md              # Phase 0: research findings
├── data-model.md            # Phase 1: entity/table definitions
├── quickstart.md            # Phase 1: developer setup guide
├── implementation-notes.md  # Technical decisions from brainstorm
├── review_brief.md          # Reviewer guide
├── REVIEW-SPEC.md           # Spec review results
├── contracts/
│   └── integration-events.md # Event contracts
└── checklists/
    └── requirements.md      # Quality checklist
```

### Source Code (repository root)

```text
Mentoory.Notification.Domain/
├── Aggregates/
│   ├── Notification/
│   │   ├── Notification.cs                # Aggregate root
│   │   ├── NotificationRecipient.cs       # Child entity
│   │   ├── DeliveryAttempt.cs             # Value object
│   │   └── LoginContext.cs                # Value object
│   └── NotificationPreference/
│       └── NotificationPreference.cs      # Aggregate root
├── Enums/
│   ├── NotificationType.cs
│   ├── DeliveryChannel.cs
│   └── DeliveryStatus.cs
└── Repositories/
    ├── INotificationRepository.cs
    └── INotificationPreferenceRepository.cs

Mentoory.Notification.Application/
├── Commands/
│   └── UpdateNotificationPreference/
│       ├── UpdateNotificationPreferenceCommand.cs
│       ├── UpdateNotificationPreferenceHandler.cs
│       └── UpdateNotificationPreferenceValidator.cs
├── IntegrationEvents/
│   ├── UserRegisteredNotificationHandler.cs
│   ├── LoginAttemptNotificationHandler.cs
│   └── InvitationReissuedNotificationHandler.cs
├── Queries/
│   └── GetNotificationPreferences/
│       ├── GetNotificationPreferencesQuery.cs
│       ├── GetNotificationPreferencesHandler.cs
│       └── NotificationPreferenceDto.cs
├── Services/
│   ├── INotificationQueueService.cs       # Queue abstraction
│   └── ILoginContextParser.cs             # UA parsing abstraction
└── DependencyInjection.cs

Mentoory.Notification.Infrastructure/
├── Persistence/
│   ├── NotificationDbContext.cs
│   ├── NotificationRepository.cs
│   └── NotificationPreferenceRepository.cs
├── Services/
│   ├── IEmailService.cs                   # Email provider abstraction
│   ├── ITemplateRenderer.cs               # Template rendering abstraction
│   ├── MailtrapEmailService.cs
│   ├── MailgunEmailService.cs
│   ├── RazorLightTemplateRenderer.cs
│   ├── NotificationProcessorService.cs    # IHostedService background processor
│   └── UaParserLoginContextParser.cs
├── Templates/
│   ├── _EmailLayout.cshtml                # Shared branded layout
│   ├── LoginAlert.cshtml
│   ├── SuspiciousLoginAlert.cshtml
│   ├── UserRegistration.cshtml
│   └── ProjectInvitation.cshtml
├── Configuration/
│   ├── SmtpSettings.cs                    # Options class
│   └── NotificationSettings.cs            # Polling interval, retry config
└── DependencyInjection.cs

Mentoory.Db/
└── notification/
    ├── Schema.sql                         # (exists, empty)
    └── Tables/
        ├── Notifications.sql
        ├── NotificationRecipients.sql
        ├── DeliveryAttempts.sql
        └── NotificationPreferences.sql

Mentoory.Db.PostDeployment/
└── 015-Seed-NotificationTypes.sql         # Seed notification type reference data (if needed)
```

**Structure Decision**: Follows the established modular monolith pattern with Domain/Application/Infrastructure separation per bounded context. The three existing empty Notification projects provide the landing zones. RazorLight templates live in Infrastructure as embedded resources (compiled into the assembly, no runtime file path dependency).

## Complexity Tracking

> No constitution violations. No complexity justifications needed.

---

## Phase 0: Research

### R-001: RazorLight Engine for Email Template Rendering

**Decision**: Use RazorLight as the Razor template engine for rendering emails outside the MVC pipeline.

**Rationale**: RazorLight supports file-system and embedded-resource projects, strongly-typed models, shared layouts (`@{ Layout = "_EmailLayout.cshtml"; }`), and `@RenderBody()`. It compiles templates to IL with memory caching, so subsequent renders are fast. It is a mature library (4k+ GitHub stars) compatible with .NET 6+.

**Configuration**:
```csharp
var engine = new RazorLightEngineBuilder()
    .UseEmbeddedResourcesProject(typeof(RazorLightTemplateRenderer).Assembly, "Mentoory.Notification.Infrastructure.Templates")
    .UseMemoryCachingProvider()
    .Build();
```

**Alternatives considered**:
- ASP.NET Core Razor runtime compilation: Requires MVC dependencies in Infrastructure layer (constitution violation)
- Scriban/Fluid (Liquid templates): No type safety, different syntax from team's existing Razor knowledge
- Raw string interpolation: No layout support, unmaintainable for complex HTML

**Action**: Add `RazorLight` to `Directory.Packages.props`. Reference in `Mentoory.Notification.Infrastructure.csproj`.

### R-002: UAParser .NET Library for User-Agent Parsing

**Decision**: Use `UAParser` NuGet package (C# port of ua-parser, GitHub: ua-parser/uap-csharp).

**Rationale**: Well-maintained C# port of the universal ua-parser project. Uses community-maintained regex patterns. Provides `Parser.GetDefault().Parse(uaString)` returning `ClientInfo` with `UA.Family` (browser), `OS.Family` (OS name). Simple, no external API calls.

**Usage pattern**:
```csharp
var parser = Parser.GetDefault();
var clientInfo = parser.Parse(userAgentString);
// clientInfo.UA.Family -> "Chrome"
// clientInfo.OS.Family -> "Windows"
```

**Graceful degradation**: If parsing fails or returns null/unknown, use "Navegador desconocido" / "Sistema operativo desconocido".

**Alternatives considered**:
- DeviceDetector.NET: Heavier, more features than needed
- Manual regex: Fragile, hard to maintain
- External API: Adds latency and external dependency

**Action**: Add `UAParser` to `Directory.Packages.props`. Reference in `Mentoory.Notification.Infrastructure.csproj`.

### R-003: LoginAttemptEvent Enrichment

**Decision**: Add `UserAgentString` field to the existing `LoginAttemptEvent` and add `FailedAttemptCount` to support suspicious login detection.

**Current state**: `LoginAttemptEvent` already carries `IpAddress` but not `UserAgent`. The event is defined but not yet published anywhere in the codebase.

**Change**:
```csharp
public sealed record LoginAttemptEvent(
    long? UserId,
    string Email,
    bool Success,
    string IpAddress,
    string? UserAgentString,     // NEW: raw User-Agent header
    int FailedAttemptCount,      // NEW: consecutive failed attempts before this login
    DateTime OccurredOnUtc) : IntegrationEvent(OccurredOnUtc);
```

**Impact**: Since this event is not yet published, adding fields is non-breaking. The web layer (LoginController) will populate these from `HttpContext`.

### R-004: InvitationReissuedEvent Creation

**Decision**: Create a new `InvitationReissuedEvent` in the Tenant domain's Application layer, published by `ReissueInvitationHandler`.

**Current state**: `ReissueInvitationHandler` completes successfully but does not publish any integration event. The event does not exist yet.

**New event**:
```csharp
// Mentoory.Tenant.Application/IntegrationEvents/InvitationReissuedEvent.cs
public sealed record InvitationReissuedEvent(
    long UserId,
    Guid UserExternalId,
    string Email,
    string FirstName,
    string LastName,
    Guid ProjectExternalId,
    string ProjectName,
    string IncubatorName,
    int InvitationExpiryHours,
    DateTime OccurredOnUtc) : IntegrationEvent(OccurredOnUtc);
```

**Action**: Create the event, publish it from `ReissueInvitationHandler` after successful save, enrich with project/incubator names for the template.

### R-005: IHostedService Pattern for Background Processing

**Decision**: Use `BackgroundService` (inherits from `IHostedService`) with a `PeriodicTimer` for polling pending notifications every 15 seconds.

**Current state**: No `IHostedService` exists in the project. This will be the first.

**Pattern**:
```csharp
public class NotificationProcessorService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ProcessPendingNotificationsAsync(stoppingToken);
        }
    }
}
```

**Registration**: In `AddNotificationInfrastructure()` extension method via `builder.Services.AddHostedService<NotificationProcessorService>()`.

**Scoped service access**: Since `IHostedService` is singleton, use `IServiceScopeFactory` to create scopes for DbContext and repository access per polling cycle.

### R-006: SMTP Configuration Structure

**Decision**: Use the standard ASP.NET Core options pattern with `IOptions<SmtpSettings>`.

**Mailtrap (Development)**:
```json
{
  "Smtp": {
    "Host": "sandbox.smtp.mailtrap.io",
    "Port": 2525,
    "Username": "<mailtrap-username>",
    "Password": "<mailtrap-password>",
    "FromAddress": "noreply@mentoory.com",
    "FromName": "Mentoory",
    "UseSsl": false
  }
}
```

**Mailgun (Production)**:
```json
{
  "Smtp": {
    "Host": "smtp.mailgun.org",
    "Port": 587,
    "Username": "postmaster@mail.mentoory.com",
    "Password": "<mailgun-password>",
    "FromAddress": "noreply@mail.mentoory.com",
    "FromName": "Mentoory",
    "UseSsl": true
  }
}
```

### R-007: SourceEventId Deduplication Strategy

**Decision**: Use the `IntegrationEvent.EventId` (Guid, auto-generated) as the `SourceEventId` on the `Notification` entity, scoped by `NotificationType`.

**Rationale**: The base `IntegrationEvent` already generates a unique `EventId` per event instance. Combined with `NotificationType`, this forms a composite uniqueness key: one event can produce multiple notification types (e.g., `UserRegisteredEvent` -> registration email + invitation email) without conflict, while duplicate event replay is caught.

**Database**: Unique filtered index on `[SourceEventId, NotificationType]` with filter `WHERE SourceEventId IS NOT NULL`.

---

## Phase 1: Design

### Data Model

See [data-model.md](data-model.md) for full entity definitions, table schemas, and relationships.

### Integration Event Contracts

See [contracts/integration-events.md](contracts/integration-events.md) for event payload definitions.

### Developer Quickstart

See [quickstart.md](quickstart.md) for setup instructions (Mailtrap account, configuration, running the notification processor).

---

## Implementation Phases

### Phase 1: Domain & Database Foundation
**Goal**: Establish the Notification domain model and database schema.

1. Create domain entities: `Notification` aggregate root, `NotificationRecipient`, `DeliveryAttempt` (VO), `LoginContext` (VO), `NotificationPreference` aggregate root
2. Create enums: `NotificationType`, `DeliveryChannel`, `DeliveryStatus`
3. Create repository interfaces: `INotificationRepository`, `INotificationPreferenceRepository`
4. Create SSDT table definitions in `Mentoory.Db/notification/Tables/`
5. Create `NotificationDbContext` with EF Core entity configuration
6. Create repository implementations

### Phase 2: Email Infrastructure
**Goal**: Build the email sending pipeline.

1. Add RazorLight and UAParser to `Directory.Packages.props` and Infrastructure .csproj
2. Create `SmtpSettings` options class and `NotificationSettings`
3. Create `IEmailService` interface
4. Implement `MailtrapEmailService` and `MailgunEmailService`
5. Create `ITemplateRenderer` interface and `RazorLightTemplateRenderer`
6. Create `ILoginContextParser` and `UaParserLoginContextParser`
7. Create branded Razor email templates (layout + 4 templates)
8. Add SMTP configuration sections to `appsettings.json` / `appsettings.Development.json`

### Phase 3: Application Layer & Event Handlers
**Goal**: Wire up event consumption and notification queuing.

1. Enrich `LoginAttemptEvent` with `UserAgentString` and `FailedAttemptCount`
2. Create `InvitationReissuedEvent` in Tenant domain, publish from `ReissueInvitationHandler`
3. Create `INotificationQueueService` and implementation
4. Create integration event handlers: `UserRegisteredNotificationHandler`, `LoginAttemptNotificationHandler`, `InvitationReissuedNotificationHandler`
5. Create `UpdateNotificationPreferenceCommand` with handler and validator
6. Create `GetNotificationPreferencesQuery` with handler
7. Register all services via `AddNotificationApplication()` and `AddNotificationInfrastructure()`
8. Register in `Program.cs`

### Phase 4: Background Processor & Retry
**Goal**: Process the notification queue with retry logic.

1. Create `NotificationProcessorService` (BackgroundService) with 15s polling
2. Implement deduplication check via `SourceEventId`
3. Implement exponential backoff retry logic (1m, 5m, 15m, 1h, 4h)
4. Implement delivery attempt recording
5. Handle error cases: template rendering failure, invalid recipient, SMTP failure

### Phase 5: Integration & Verification
**Goal**: End-to-end testing and validation.

1. Publish `LoginAttemptEvent` from `LoginController` with IP + User-Agent
2. Verify registration email flow end-to-end via Mailtrap
3. Verify invitation email flow (create + reissue)
4. Verify login alert flow (standard + suspicious)
5. Verify preference toggle suppresses login alerts
6. Verify retry behavior on simulated SMTP failure
7. Verify deduplication on event replay
8. Build verification (zero warnings)
