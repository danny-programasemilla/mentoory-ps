# Tasks: Notification Database Configuration

**Input**: Design documents from `specs/012-notification-db-config/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Exact file paths included in all descriptions

---

## Phase 1: Setup (Database Schema & Seed Data)

**Purpose**: Create the database table and seed data that all subsequent work depends on

- [X] T001 [P] Create SSDT table definition in `Mentoory.Db/notification/Tables/NotificationConfigurations.sql` — mirror `access.SystemConfigurations` structure with BIGINT IDENTITY PK, ExternalId (UNIQUEIDENTIFIER, UQ, DEFAULT NEWID()), Key (NVARCHAR(100), UQ), Value (NVARCHAR(500)), Description (NVARCHAR(500) NULL), DataType (NVARCHAR(50)), CreatedAtUtc (DATETIME2), UpdatedAtUtc (DATETIME2) in `notification` schema
- [X] T002 [P] Create idempotent seed script in `Mentoory.Db.PostDeployment/019.SeedNotificationConfiguration.sql` — seed all 10 keys (SmtpHost, SmtpPort=587, SmtpUsername, SmtpPassword, SmtpFromAddress, SmtpFromName=Mentoory, SmtpUseSsl=true, PollingIntervalSeconds=15, MaxRetryAttempts=5, BaseUrl) with Spanish descriptions and `IF NOT EXISTS` guard on Key column
- [X] T003 Add `:r .\019.SeedNotificationConfiguration.sql` to `Mentoory.Db.PostDeployment/Script.PostDeployment.sql` after existing entries

---

## Phase 2: Foundational — Core Config Infrastructure (US1: Admin Manages Settings, P1) 🎯 MVP

**Goal**: Build the complete database configuration pattern for the Notification domain — entity, repository, reader — so that configuration values can be read from the database with type safety and fail-fast behavior

**Independent Test**: Insert a config row in `notification.NotificationConfigurations`, inject `INotificationConfigurationReader`, call `GetIntAsync`/`GetStringAsync`/`GetBoolAsync` and verify correct typed values are returned. Verify missing/invalid keys throw `InvalidOperationException`.

- [X] T004 [P] Create `NotificationConfiguration` aggregate root entity in `Mentoory.Notification.Domain/Aggregates/NotificationConfiguration/NotificationConfiguration.cs` — replicate `Access.Domain.Aggregates.SystemConfiguration.SystemConfiguration` pattern: private constructor, ExternalId, Key, Value, Description, DataType, CreatedAtUtc, UpdatedAtUtc, static `Create()` factory with key guard clause, `Update()` mutation. Implements `Entity, IAggregateRoot`
- [X] T005 [P] Create `NotificationConfigurationKey` enum in `Mentoory.Notification.Domain/Enums/NotificationConfigurationKey.cs` — values: SmtpHost=0, SmtpPort=1, SmtpUsername=2, SmtpPassword=3, SmtpFromAddress=4, SmtpFromName=5, SmtpUseSsl=6, PollingIntervalSeconds=7, MaxRetryAttempts=8, BaseUrl=9
- [X] T006 [P] Create `INotificationConfigurationRepository` interface in `Mentoory.Notification.Domain/Repositories/INotificationConfigurationRepository.cs` — extends `IRepository<NotificationConfiguration>`, methods: `GetByKeyAsync(string key, CancellationToken)`, `GetAllAsync(CancellationToken)`, `Update(NotificationConfiguration)`
- [X] T007 [P] Create `INotificationConfigurationReader` interface in `Mentoory.Notification.Application/Configuration/INotificationConfigurationReader.cs` — methods: `GetStringAsync(string key, CancellationToken)`, `GetIntAsync(string key, CancellationToken)`, `GetBoolAsync(string key, CancellationToken)`
- [X] T008 Create `NotificationConfigurationRepository` in `Mentoory.Notification.Infrastructure/Persistence/Repositories/NotificationConfigurationRepository.cs` — extends `AbstractRepository<NotificationConfiguration>`, uses `NotificationDbContext.NotificationConfigurations` DbSet, `GetByKeyAsync` via `FirstOrDefaultAsync`, `GetAllAsync` with `AsNoTracking().OrderBy(c => c.Key)`, `Update` sets entity state to Modified
- [X] T009 Create `NotificationConfigurationReader` in `Mentoory.Notification.Infrastructure/Services/NotificationConfigurationReader.cs` — implements `INotificationConfigurationReader`, injects `INotificationConfigurationRepository`. `GetStringAsync`: lookup by key, throw `InvalidOperationException` if not found, return Value. `GetIntAsync`: same + `int.TryParse`, throw if invalid. `GetBoolAsync`: same + `bool.TryParse`, throw if invalid
- [X] T010 Update `Mentoory.Notification.Infrastructure/Persistence/NotificationDbContext.cs` — add `DbSet<NotificationConfiguration> NotificationConfigurations` property, add `ConfigureNotificationConfiguration(ModelBuilder)` private method mapping to table `NotificationConfigurations` in schema `notification` with key, unique index on ExternalId, unique index on Key, max lengths matching SSDT definition
- [X] T011 Update `Mentoory.Notification.Infrastructure/DependencyInjection.cs` — add `AddScoped<INotificationConfigurationRepository, NotificationConfigurationRepository>()` and `AddScoped<INotificationConfigurationReader, NotificationConfigurationReader>()`

**Checkpoint**: `INotificationConfigurationReader` is fully functional. All 10 config keys are seeded in the database and can be read with type safety. US1 acceptance scenarios are met.

---

## Phase 3: User Story 2 — SMTP Settings from Database (Priority: P1)

**Goal**: The email service reads SMTP connection settings from the database instead of `appsettings.json`

**Independent Test**: Change SmtpHost value in `notification.NotificationConfigurations`, trigger a notification email, verify the service connects using the updated DB value

### Implementation for User Story 2

- [X] T012 [US2] Refactor `SmtpEmailService` in `Mentoory.Notification.Infrastructure/Services/SmtpEmailService.cs` — replace constructor parameter `IOptions<SmtpSettings>` with `INotificationConfigurationReader`. In `SendAsync`, read each SMTP setting via reader using `nameof(NotificationConfigurationKey.SmtpHost)` etc. Read host, port, username, password, from address, from name, SSL flag before building and sending the MimeMessage
- [X] T013 [US2] Remove `SmtpSettings` class — delete `Mentoory.Notification.Infrastructure/Configuration/SmtpSettings.cs` and remove `builder.Services.Configure<SmtpSettings>(...)` line from `Mentoory.Notification.Infrastructure/DependencyInjection.cs`

**Checkpoint**: SmtpEmailService sends emails using database configuration. `SmtpSettings` class and its `IOptions<>` registration are gone. US2 acceptance scenarios are met.

---

## Phase 4: User Story 3 — Operational Settings from Database (Priority: P2)

**Goal**: The notification processor, queue service, and integration event handlers read operational parameters from the database

**Independent Test**: Change `PollingIntervalSeconds` in the database, restart the app, verify the processor uses the new interval. Change `BaseUrl`, trigger a registration notification, verify the verification link uses the new URL.

### Implementation for User Story 3

- [X] T014 [P] [US3] Refactor `UserRegisteredNotificationHandler` in `Mentoory.Notification.Application/IntegrationEvents/UserRegisteredNotificationHandler.cs` — replace `IOptions<NotificationSettings>` with `INotificationConfigurationReader`. In `Handle`, read BaseUrl via `await _configReader.GetStringAsync(nameof(NotificationConfigurationKey.BaseUrl), cancellationToken)`
- [X] T015 [P] [US3] Refactor `InvitationReissuedNotificationHandler` in `Mentoory.Notification.Application/IntegrationEvents/InvitationReissuedNotificationHandler.cs` — replace `IOptions<NotificationSettings>` with `INotificationConfigurationReader`. In `Handle`, read BaseUrl via `await _configReader.GetStringAsync(nameof(NotificationConfigurationKey.BaseUrl), cancellationToken)`
- [X] T016 [US3] Refactor `NotificationQueueService` in `Mentoory.Notification.Infrastructure/Services/NotificationQueueService.cs` — replace `IOptions<NotificationSettings>` with `INotificationConfigurationReader`. In `ProcessPendingAsync`, read MaxRetryAttempts via `await _configReader.GetIntAsync(nameof(NotificationConfigurationKey.MaxRetryAttempts), cancellationToken)`
- [X] T017 [US3] Refactor `NotificationProcessorService` in `Mentoory.Notification.Infrastructure/Services/NotificationProcessorService.cs` — replace `IOptions<NotificationSettings>` with scoped `INotificationConfigurationReader`. Since this is a `BackgroundService` (singleton), create a scope via `IServiceScopeFactory` at the beginning of `ExecuteAsync` to read `PollingIntervalSeconds` once, then use that value for the `PeriodicTimer`
- [X] T018 [US3] Remove `NotificationSettings` class — delete `Mentoory.Notification.Application/Configuration/NotificationSettings.cs` and remove `builder.Services.Configure<NotificationSettings>(...)` line from `Mentoory.Notification.Infrastructure/DependencyInjection.cs`

**Checkpoint**: All 5 consumers read settings from the database. `NotificationSettings` class and its `IOptions<>` registration are gone. Zero notification-related keys remain in `appsettings.json`. US3 acceptance scenarios are met.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Final validation and cleanup

- [X] T019 Build verification — run `dotnet build` from repo root and confirm zero warnings (TreatWarningsAsErrors)
- [X] T020 Run quickstart.md validation — verify `notification.NotificationConfigurations` table exists with 10 seeded rows, trigger a test notification to confirm end-to-end email delivery using database configuration

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately. T001 and T002 are parallel.
- **Foundational (Phase 2)**: Depends on Phase 1 (table must exist for EF mapping). T004-T007 are parallel. T008 depends on T004+T006. T009 depends on T006+T007. T010 depends on T004. T011 depends on T008+T009.
- **US2 (Phase 3)**: Depends on Phase 2 (reader must be registered). T012 → T013 sequential.
- **US3 (Phase 4)**: Depends on Phase 2 (reader must be registered). T014+T015 are parallel. T016 depends on Phase 2 only. T017 depends on Phase 2 only. T018 depends on T014+T015+T016+T017.
- **US2 and US3 are independent**: Can be worked in parallel after Phase 2.
- **Polish (Phase 5)**: Depends on Phase 3 + Phase 4 completion.

### Within Each User Story

- Refactor consumers first, then delete old settings classes
- Each consumer refactoring can be done independently (different files)

### Parallel Opportunities

- **Phase 1**: T001 ‖ T002 (then T003)
- **Phase 2**: T004 ‖ T005 ‖ T006 ‖ T007 (then T008 ‖ T009 ‖ T010, then T011)
- **Phase 3 ‖ Phase 4**: US2 and US3 can run in parallel after Phase 2
- **Phase 4**: T014 ‖ T015 ‖ T016 ‖ T017 (then T018)

---

## Parallel Example: Phase 2 Foundational

```bash
# Wave 1 — all independent new files:
Task: "Create NotificationConfiguration entity in Domain/Aggregates/NotificationConfiguration/"
Task: "Create NotificationConfigurationKey enum in Domain/Enums/"
Task: "Create INotificationConfigurationRepository in Domain/Repositories/"
Task: "Create INotificationConfigurationReader in Application/Configuration/"

# Wave 2 — depends on Wave 1:
Task: "Create NotificationConfigurationRepository in Infrastructure/Persistence/Repositories/"
Task: "Create NotificationConfigurationReader in Infrastructure/Services/"
Task: "Add DbSet + EF config to NotificationDbContext"

# Wave 3 — depends on Wave 2:
Task: "Update DependencyInjection.cs with repository + reader registrations"
```

---

## Implementation Strategy

### MVP First (Phase 1 + Phase 2)

1. Complete Phase 1: Database table + seed data
2. Complete Phase 2: Entity, repository, reader — full config infrastructure
3. **STOP and VALIDATE**: Config reader works, 10 keys seeded and readable
4. This alone delivers US1 (admin manages settings)

### Incremental Delivery

1. Phase 1 + Phase 2 → US1 complete (config infrastructure works)
2. Add Phase 3 → US2 complete (SMTP from DB, SmtpSettings deleted)
3. Add Phase 4 → US3 complete (all operational settings from DB, NotificationSettings deleted)
4. Phase 5 → Build clean, end-to-end verified

### Single Developer Flow

Phase 1 → Phase 2 → Phase 3 → Phase 4 → Phase 5 (sequential, ~20 tasks)

---

## Notes

- [P] tasks = different files, no dependencies on incomplete tasks in same wave
- [US2]/[US3] labels map tasks to spec user stories for traceability
- Phase 2 doubles as US1 since the core config infrastructure IS what US1 delivers
- No test tasks generated — not explicitly requested in the spec
- Commit after each phase checkpoint for clean rollback points
