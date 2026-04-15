# Research: Notification Database Configuration

**Branch**: `013-notification-db-config` | **Date**: 2026-04-13

## R1: Access Domain Configuration Pattern Analysis

**Decision**: Replicate the Access domain's database configuration pattern for the Notification domain.

**Rationale**: The Access domain has a proven, production-ready pattern (`SystemConfiguration` entity + `ISystemConfigurationRepository` + `ISystemConfigurationReader`) that is DDD-compliant, fail-fast, and well-tested. Replicating the same structure ensures architectural consistency across domains.

**Pattern Components**:
1. **Domain Entity** (`SystemConfiguration`): Aggregate root with ExternalId, Key (unique), Value (string), Description, DataType, timestamps. Factory method `Create()` + mutation `Update()`.
2. **Domain Repository** (`ISystemConfigurationRepository`): `GetByKeyAsync(key)`, `GetAllAsync()`, `Update()`.
3. **Application Interface** (`ISystemConfigurationReader`): Typed getters (`GetIntAsync`, `GetBoolAsync`). Fail-fast on missing/invalid keys.
4. **Infrastructure Reader** (`SystemConfigurationReader`): Bridge from repository to typed reader. Throws `InvalidOperationException`.
5. **SSDT Table** (`access.SystemConfigurations`): Key/value store with unique constraints on ExternalId and Key.
6. **PostDeployment Seed** (`017.SeedSystemConfiguration.sql`): Idempotent `IF NOT EXISTS` inserts.

**Alternatives Considered**:
- Shared configuration table across domains: Rejected — violates Clean Architecture (each domain owns its data).
- Reuse `ISystemConfigurationReader` from Access directly: Rejected — creates cross-domain dependency.

## R2: Notification Configuration Keys Inventory

**Decision**: The following configuration keys will be defined in the `NotificationConfigurationKey` enum:

| Key | Current Source | Current Default | DataType |
|-----|---------------|----------------|----------|
| SmtpHost | `appsettings.json` Smtp:Host | (none) | String |
| SmtpPort | `appsettings.json` Smtp:Port | (none) | Integer |
| SmtpUsername | `appsettings.json` Smtp:Username | (none) | String |
| SmtpPassword | `appsettings.json` Smtp:Password | (none) | String |
| SmtpFromAddress | `appsettings.json` Smtp:FromAddress | (none) | String |
| SmtpFromName | `appsettings.json` Smtp:FromName | (none) | String |
| SmtpUseSsl | `appsettings.json` Smtp:UseSsl | (none) | Boolean |
| PollingIntervalSeconds | `appsettings.json` Notification:PollingIntervalSeconds | 15 | Integer |
| MaxRetryAttempts | `appsettings.json` Notification:MaxRetryAttempts | 5 | Integer |
| BaseUrl | `appsettings.json` Notification:BaseUrl | (none) | String |

**Rationale**: These 10 keys cover every setting currently read from `appsettings.json` by the Notification domain. No new keys are invented — this is purely a migration.

## R3: Reader Interface Extension — GetStringAsync

**Decision**: The Notification domain's reader interface (`INotificationConfigurationReader`) will include `GetStringAsync` in addition to `GetIntAsync` and `GetBoolAsync`.

**Rationale**: The Access domain only needs integer and boolean configs. The Notification domain has 6 string-typed settings (SMTP host, username, password, from address, from name, base URL). Adding `GetStringAsync` avoids callers doing raw repository lookups.

**Alternatives Considered**:
- Expose only `GetIntAsync`/`GetBoolAsync` and use repository directly for strings: Rejected — breaks the abstraction and forces infrastructure knowledge into Application layer.

## R4: Consumers Requiring Refactoring

**Decision**: Five classes consume `IOptions<SmtpSettings>` or `IOptions<NotificationSettings>` and must be refactored:

| Class | Layer | Current Dependency | Keys Used |
|-------|-------|--------------------|-----------|
| `SmtpEmailService` | Infrastructure | `IOptions<SmtpSettings>` | Host, Port, Username, Password, FromAddress, FromName, UseSsl |
| `NotificationProcessorService` | Infrastructure | `IOptions<NotificationSettings>` | PollingIntervalSeconds |
| `NotificationQueueService` | Infrastructure | `IOptions<NotificationSettings>` | MaxRetryAttempts |
| `UserRegisteredNotificationHandler` | Application | `IOptions<NotificationSettings>` | BaseUrl |
| `InvitationReissuedNotificationHandler` | Application | `IOptions<NotificationSettings>` | BaseUrl |

**Rationale**: Complete migration requires refactoring all consumers. The Application-layer handlers (`UserRegisteredNotificationHandler`, `InvitationReissuedNotificationHandler`) currently depend on `Microsoft.Extensions.Options` which is an infrastructure concern — replacing with `INotificationConfigurationReader` is architecturally cleaner.

## R5: NotificationProcessorService Design Consideration

**Decision**: The `NotificationProcessorService` (BackgroundService) will read `PollingIntervalSeconds` once at startup and use that value for the `PeriodicTimer`. Dynamic polling interval changes require a restart.

**Rationale**: The current implementation already captures `PollingIntervalSeconds` at constructor time via `IOptions<T>.Value`. The `PeriodicTimer` is created once and cannot be reconfigured. Since `NotificationProcessorService` is a singleton `BackgroundService`, it cannot inject scoped `INotificationConfigurationReader` directly. Instead, it will create a scope at startup to read the value.

**Alternatives Considered**:
- Read config every loop iteration: Rejected — over-engineered for a polling interval that rarely changes, and adds scope creation overhead on every tick.
- Make the timer recreatable on config change: Rejected — significantly increases complexity without proportional value.
