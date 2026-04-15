# Research: Notification Domain - Email Delivery System

**Date**: 2026-04-13
**Branch**: `011-notification-email-delivery`

## R-001: RazorLight Engine for Email Template Rendering

**Decision**: Use RazorLight with embedded resources project.

**Rationale**: RazorLight supports strongly-typed models, shared layouts (`@{ Layout = "..." }`), `@RenderBody()`, and compiles templates to IL with memory caching. It runs outside the MVC pipeline, so no ASP.NET MVC dependency leaks into Infrastructure layer.

**Configuration**:
```csharp
var engine = new RazorLightEngineBuilder()
    .UseEmbeddedResourcesProject(
        typeof(RazorLightTemplateRenderer).Assembly,
        "Mentoory.Notification.Infrastructure.Templates")
    .UseMemoryCachingProvider()
    .Build();

string html = await engine.CompileRenderAsync("UserRegistration.cshtml", model);
```

**Layout support**: Templates reference a shared layout via `@{ Layout = "_EmailLayout.cshtml"; }`. The layout contains the branded header, card wrapper, and footer. Child templates provide content via `@RenderBody()`.

**Alternatives rejected**:
- ASP.NET Razor runtime compilation: Would pull MVC dependencies into Infrastructure (constitution violation of Principle I)
- Scriban/Fluid: No type safety, unfamiliar syntax for the team
- Raw string interpolation: Unmaintainable for complex HTML with conditionals

**Action**: Add RazorLight to `Directory.Packages.props`. Embed `.cshtml` templates as resources in Infrastructure .csproj.

---

## R-002: UAParser .NET Library

**Decision**: Use `UAParser` NuGet package (ua-parser/uap-csharp).

**Rationale**: C# port of the universal ua-parser project. Community-maintained regex patterns for browser/OS detection. Simple API: `Parser.GetDefault().Parse(uaString)` returns `ClientInfo` with `UA.Family` and `OS.Family`.

**Usage**:
```csharp
var parser = Parser.GetDefault();
var clientInfo = parser.Parse(userAgentString);
var browser = clientInfo.UA.Family;  // "Chrome"
var os = clientInfo.OS.Family;       // "Windows"
```

**Graceful degradation**: Wrap in try-catch. On failure, return `LoginContext` with "Navegador desconocido" / "Sistema operativo desconocido".

**Alternatives rejected**:
- DeviceDetector.NET: Heavier than needed for browser/OS extraction
- Manual regex: Fragile, hard to maintain against evolving User-Agent strings
- External API (ip-api, ipinfo): Adds latency, rate limits, external dependency

**Action**: Add `UAParser` to `Directory.Packages.props`. Reference in Infrastructure .csproj.

---

## R-003: LoginAttemptEvent Enrichment

**Decision**: Add `UserAgentString` and `FailedAttemptCount` fields.

**Current state**: `LoginAttemptEvent(UserId, Email, Success, IpAddress, OccurredOnUtc)` -- defined but not yet published anywhere.

**Updated record**:
```csharp
public sealed record LoginAttemptEvent(
    long? UserId,
    string Email,
    bool Success,
    string IpAddress,
    string? UserAgentString,
    int FailedAttemptCount,
    DateTime OccurredOnUtc) : IntegrationEvent(OccurredOnUtc);
```

**Suspicious detection logic**: `FailedAttemptCount >= 3` OR login after lockout release (detectable when `Success == true` AND `FailedAttemptCount >= 5`, since lockout triggers at 5).

**Impact**: Non-breaking -- event is not yet published. LoginController will populate from `HttpContext.Request.Headers["User-Agent"]` and `HttpContext.Connection.RemoteIpAddress`.

---

## R-004: InvitationReissuedEvent Creation

**Decision**: Create new integration event in Tenant domain.

**Current state**: `ReissueInvitationHandler` completes but publishes no event. The handler has access to `ProjectInvitation` (which has `ProjectId`, `UserId`).

**New event**:
```csharp
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

**Publishing**: After `SaveEntitiesAsync()` in `ReissueInvitationHandler`, publish via `IIntegrationEventService`. The handler needs to be enriched to load user and project/incubator names for the event payload.

**Alternative considered**: Publish a minimal event (just IDs) and let the Notification handler query for names. Rejected because it couples Notification to Tenant/Access queries, violating bounded context isolation.

---

## R-005: Background Service Pattern

**Decision**: `BackgroundService` with `PeriodicTimer(TimeSpan.FromSeconds(15))`.

**Current state**: No `IHostedService` exists in the project. This is the first.

**Pattern**:
```csharp
public class NotificationProcessorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationProcessorService> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            using var scope = _scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<INotificationQueueService>();
            await processor.ProcessPendingAsync(stoppingToken);
        }
    }
}
```

**Key considerations**:
- `IServiceScopeFactory` for scoped DbContext access (BackgroundService is singleton)
- Try-catch around processing to prevent single failures from stopping the service
- Logging for each processing cycle (count processed, failures)

---

## R-006: SMTP Configuration

**Decision**: `IOptions<SmtpSettings>` with environment-specific `appsettings.{Environment}.json`.

**Mailtrap (Development)**: Host `sandbox.smtp.mailtrap.io`, Port `2525`, no SSL
**Mailgun (Production)**: Host `smtp.mailgun.org`, Port `587`, STARTTLS, domain `mail.mentoory.com`

**Provider selection**: DI registration based on `IHostEnvironment.IsDevelopment()`:
```csharp
if (builder.Environment.IsDevelopment())
    builder.Services.AddScoped<IEmailService, MailtrapEmailService>();
else
    builder.Services.AddScoped<IEmailService, MailgunEmailService>();
```

---

## R-007: SourceEventId Deduplication

**Decision**: Use `IntegrationEvent.EventId` (Guid) as `SourceEventId`, scoped by `NotificationType`.

**Rationale**: Base `IntegrationEvent` auto-generates `EventId = Guid.NewGuid()`. Combined with `NotificationType`, this allows one event to trigger multiple notification types without conflict.

**Database index**: Unique filtered index on `(SourceEventId, NotificationType)` where `SourceEventId IS NOT NULL`.

**Check**: Before creating a `Notification`, query for existing record with same `SourceEventId` + `NotificationType`. If found, skip silently.
