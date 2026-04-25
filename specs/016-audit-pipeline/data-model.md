# Data Model: Audit Pipeline Wiring

**Feature:** 016-audit-pipeline
**Phase:** 1 (Design & Contracts)
**Date:** 2026-04-18

This document captures the concrete shape of every data-bearing type introduced or modified by this feature. It is the single source of truth for `AuditLog` schema, the in-memory record, and the configuration/attribute surface.

---

## 1. Database — `[audit].[AuditLog]`

### Existing columns (preserved)

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| Id | BIGINT IDENTITY(1,1) | NO | PK, clustered |
| EventType | NVARCHAR(100) | NO | e.g., `Role.Assigned` |
| UserId | BIGINT | YES | Acting user; null for anonymous |
| IncubatorId | BIGINT | YES | From tenant context |
| ProjectId | BIGINT | YES | From tenant context |
| EntityType | NVARCHAR(100) | YES | From `[Audited(EntityType = "…")]` |
| EntityId | NVARCHAR(100) | YES | Populated by Manual-mode handlers |
| Action | NVARCHAR(50) | NO | Command type name, e.g., `AssignRoleCommand` |
| Details | NVARCHAR(MAX) | YES | Redacted JSON payload |
| IpAddress | NVARCHAR(45) | YES | IPv4/IPv6; from request-context abstraction |
| OccurredAtUtc | DATETIME2 | NO | `ITimeProvider.UtcNow` at handler completion |

### New columns (added by FR-008)

| Column | Type | Nullable | Default | Notes |
|--------|------|----------|---------|-------|
| CorrelationId | UNIQUEIDENTIFIER | YES | NULL | Shared across audit rows for one logical operation |
| Outcome | NVARCHAR(20) | NO | `'Success'` | `Success` or `Failure` |
| ExceptionType | NVARCHAR(200) | YES | NULL | Fully-qualified CLR type when `Outcome='Failure'` and an exception was thrown |
| UserEmail | NVARCHAR(256) | YES | NULL | Acting user's email (from tenant context or command payload for anonymous commands) |
| RoleContext | NVARCHAR(50) | YES | NULL | Active role name (`GlobalAdmin`, `IncubatorAdmin`, `ProjectCoordinator`, `Mentor`, `Entrepreneur`, `Sponsor`) |

### New index

```sql
CREATE NONCLUSTERED INDEX [IX_AuditLog_CorrelationId]
    ON [audit].[AuditLog] ([CorrelationId], [OccurredAtUtc] DESC)
    WHERE [CorrelationId] IS NOT NULL;
```

Filtered on `CorrelationId IS NOT NULL` — avoids bloat from legacy rows without correlation context. Include `OccurredAtUtc` in key to support ordered lookup inside a correlation group.

### Existing indexes (preserved)

- `IX_AuditLog_EventType_OccurredAtUtc`
- `IX_AuditLog_UserId_OccurredAtUtc`
- `IX_AuditLog_EntityType_EntityId`

### DACPAC migration path

The schema lives in `Mentoory.Db/audit/Tables/AuditLog.sql`. Edit the file in place — SSDT's `sqlpackage` generates `ALTER TABLE` statements for column additions and `CREATE INDEX` for the new index on the next `publish-mentoorydb.sh` run. No post-deployment script required (the default `'Success'` ensures existing rows do not violate NOT NULL).

---

## 2. `AuditEntry` record (Mentoory.Shared.Application.Audit)

### Modified shape

```csharp
namespace Mentoory.Shared.Application.Audit;

public sealed record AuditEntry(
    string EventType,
    long? UserId,
    long? IncubatorId,
    long? ProjectId,
    string? EntityType,
    string? EntityId,
    string Action,
    string? Details,
    string? IpAddress,
    DateTime OccurredAtUtc,
    // New in 016-audit-pipeline:
    Guid? CorrelationId,
    string Outcome,            // "Success" | "Failure"
    string? ExceptionType,
    string? UserEmail,
    string? RoleContext);
```

### Validation rules

- `EventType` required, non-empty.
- `Action` required, non-empty.
- `Outcome` MUST be `"Success"` or `"Failure"` (validated at construction — factory method `AuditEntry.Success(…)` and `AuditEntry.Failure(…)` recommended for clarity; covered by unit tests).
- `OccurredAtUtc` must be UTC (asserted via `DateTimeKind.Utc` check).
- When `Outcome = "Failure"`, `ExceptionType` SHOULD be non-null (soft constraint, log warning if missing).
- All other fields nullable as typed.

### State transitions

None — `AuditEntry` is an immutable record. One write per entry.

---

## 3. `AuditedAttribute` (Mentoory.Shared.Application.Audit)

### Shape

```csharp
namespace Mentoory.Shared.Application.Audit;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class AuditedAttribute : Attribute
{
    public AuditedAttribute(string eventType)
    {
        EventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
    }

    public string EventType { get; }
    public string? EntityType { get; init; }
    public AuditMode Mode { get; init; } = AuditMode.Automatic;
}

public enum AuditMode
{
    Automatic = 0,
    Manual = 1,
}
```

### Semantics

- `EventType`: required; references `AuditEventTypes.*` constants.
- `EntityType`: optional string; copied to the audit row's `EntityType` column.
- `Mode`: defaults to `Automatic`; `Manual` opts out of behavior-driven capture and delegates to the handler.

### Applied to

- Command records/classes implementing `IBaseRequest` or `IBaseRequest<T>`.
- NOT applied to queries (FR-022 — queries not audited in v1).

---

## 4. `AuditEventTypes` constants (Mentoory.Shared.Application.Audit)

```csharp
namespace Mentoory.Shared.Application.Audit;

public static class AuditEventTypes
{
    // v1 retrofit commands:
    public const string ContextActivated = "Context.Activated";
    public const string RoleAssigned     = "Role.Assigned";
    public const string UserRegistered   = "User.Registered";
    public const string UserLoggedIn     = "User.LoggedIn";
    public const string AnswerCorrected  = "Answer.Corrected";

    // Reserved for future features (declared so governance test can reference
    // them; string values are frozen before their handlers ship):
    public const string ProjectStageAdvanced  = "Project.StageAdvanced";
    public const string MentoringPlanApproved = "MentoringPlan.Approved";
}
```

---

## 5. `AuditOptions` configuration (Mentoory.Shared.Application.Audit)

```csharp
namespace Mentoory.Shared.Application.Audit;

public sealed class AuditOptions
{
    public const string SectionName = "Audit";

    /// <summary>
    /// Property names (case-insensitive, top-level only) whose values are replaced
    /// with the redaction sentinel before serialization into Details.
    /// </summary>
    public IReadOnlyList<string> RedactedFields { get; init; } = [
        "Password",
        "PasswordHash",
        "NationalId",
        "VerificationToken",
        "Token",
        "Secret",
        "ApiKey",
    ];

    /// <summary>
    /// Maximum character count for the Details column value; payload serialization
    /// is truncated with a suffix if longer.
    /// </summary>
    public int DetailsMaxCharacters { get; init; } = 8_000;

    /// <summary>
    /// Sentinel string substituted for redacted property values.
    /// </summary>
    public string RedactionSentinel { get; init; } = "***REDACTED***";

    /// <summary>
    /// Suffix appended to Details when the payload exceeds DetailsMaxCharacters.
    /// </summary>
    public string TruncationSuffix { get; init; } = "...<truncated>";
}
```

Bound in `Program.cs`:

```csharp
builder.Services.Configure<AuditOptions>(builder.Configuration.GetSection(AuditOptions.SectionName));
```

Configuration file `appsettings.json` may override `RedactedFields` without redeploying code.

---

## 6. `ICorrelationContext` / `IRequestContext` abstraction (Mentoory.Shared.Application.Interfaces)

Per FR-011a, the audit pipeline reads both the correlation id and the client IP through a single request-context abstraction — never `IHttpContextAccessor` directly.

```csharp
namespace Mentoory.Shared.Application.Interfaces;

public interface ICorrelationContext
{
    Guid CorrelationId { get; }
    string? ClientIpAddress { get; }
}
```

Two concrete implementations:

- `Mentoory.Web.Infrastructure.Correlation.WebCorrelationContext` — reads from `IHttpContextAccessor`; populated by `CorrelationMiddleware` which stashes the correlation id in `HttpContext.Items`. IP comes from `HttpContext.Connection.RemoteIpAddress`.
- `Mentoory.Shared.Infrastructure.Correlation.AmbientCorrelationContext` — fallback for non-HTTP dispatches (hosted services, tests). Reads `Activity.Current?.RootId` (parsed as GUID if possible) or generates a fresh `Guid.NewGuid()`. `ClientIpAddress` is always `null`.

DI registration: `WebCorrelationContext` is scoped and registered in `Mentoory.Web/Program.cs`; tests and background services substitute `AmbientCorrelationContext`.

---

## 7. `ITenantContext` extension

Minimal one-line addition for `UserEmail` (see R-09):

```csharp
public interface ITenantContext
{
    long UserId { get; }
    string? UserEmail { get; }     // NEW
    long IncubatorId { get; }
    long? ProjectId { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
}
```

`TenantContextService` populates `UserEmail` from `HttpContext.User.FindFirstValue(ClaimTypes.Email)` — same claims-reading pattern already used for `UserId` and `Role`.

---

## 8. Admin viewer query — `GetAuditLogPagedQuery`

```csharp
namespace Mentoory.Shared.Application.Queries.Audit;

public sealed record GetAuditLogPagedQuery(
    int PageNumber,
    int PageSize,
    string? EventType,
    string? UserEmail,
    string? Outcome,
    DateTime? FromUtc,
    DateTime? ToUtc,
    string SortColumn = "OccurredAtUtc",
    bool SortDescending = true) : IBaseRequest<PagedResult<AuditLogDto>>;

public sealed record AuditLogDto(
    long Id,
    DateTime OccurredAtUtc,
    string EventType,
    string Action,
    string Outcome,
    string? UserEmail,
    string? RoleContext,
    long? UserId,
    long? IncubatorId,
    long? ProjectId,
    string? EntityType,
    string? EntityId,
    Guid? CorrelationId,
    string? ExceptionType,
    string? IpAddress,
    string? Details);
```

Query handler reads via EF Core `AsNoTracking()` against `[audit].[AuditLog]`. Not part of any module DbContext — a dedicated `AuditReadDbContext` in `Mentoory.Shared.Infrastructure/Persistence/Audit/` OR (preferred for minimalism) scoped to the existing `SharedDbContext` as a keyless view-entity. Final decision deferred to `contracts/` phase.

---

## 9. Entity relationships

```
[audit].[AuditLog]
   │
   ├─ UserId       ─→ (no FK, tenant-independent logging)
   ├─ IncubatorId  ─→ (no FK)
   ├─ ProjectId    ─→ (no FK)
   │
   └─ CorrelationId ─→ logical grouping (no FK; see IX_AuditLog_CorrelationId)
```

No foreign keys by design: the audit log must survive tenant deletions, user removals, and project archival. Entity IDs are soft references; missing rows are acceptable.

---

## 10. Volumes and growth

| Metric | v1 estimate |
|--------|-------------|
| Rows per day (5 retrofitted commands at current traffic) | ~500 |
| Rows per year | ~180K |
| Average row size (with typical Details) | ~1.5 KB |
| Annual storage | ~270 MB |

Storage is modest; retention strategy (OQ-3) can remain deferred until annual growth exceeds ~5 GB.
