# Quickstart: Notification Database Configuration

**Branch**: `013-notification-db-config` | **Date**: 2026-04-13

## What This Feature Does

Moves all Notification domain settings (SMTP connection, polling interval, retry limits, base URL) from `appsettings.json` to a dedicated database table (`notification.NotificationConfigurations`). Follows the same pattern as the Access domain's `SystemConfiguration` entity.

## Files Changed (by layer)

### Domain Layer (`Mentoory.Notification.Domain/`)
- **NEW**: `Aggregates/NotificationConfiguration/NotificationConfiguration.cs` — aggregate root entity
- **NEW**: `Enums/NotificationConfigurationKey.cs` — typed enum for all config keys
- **NEW**: `Repositories/INotificationConfigurationRepository.cs` — repository interface

### Application Layer (`Mentoory.Notification.Application/`)
- **NEW**: `Configuration/INotificationConfigurationReader.cs` — typed reader interface (GetStringAsync, GetIntAsync, GetBoolAsync)
- **MODIFIED**: `Configuration/NotificationSettings.cs` — **DELETED**
- **MODIFIED**: `IntegrationEvents/UserRegisteredNotificationHandler.cs` — replace `IOptions<NotificationSettings>` with `INotificationConfigurationReader`
- **MODIFIED**: `IntegrationEvents/InvitationReissuedNotificationHandler.cs` — replace `IOptions<NotificationSettings>` with `INotificationConfigurationReader`

### Infrastructure Layer (`Mentoory.Notification.Infrastructure/`)
- **NEW**: `Persistence/Repositories/NotificationConfigurationRepository.cs` — repository implementation
- **NEW**: `Services/NotificationConfigurationReader.cs` — reader implementation
- **MODIFIED**: `Configuration/SmtpSettings.cs` — **DELETED**
- **MODIFIED**: `DependencyInjection.cs` — remove `IOptions<>` registrations, add repository + reader registrations
- **MODIFIED**: `Persistence/NotificationDbContext.cs` — add DbSet and EF configuration
- **MODIFIED**: `Services/SmtpEmailService.cs` — replace `IOptions<SmtpSettings>` with `INotificationConfigurationReader`
- **MODIFIED**: `Services/NotificationProcessorService.cs` — replace `IOptions<NotificationSettings>` with scoped reader
- **MODIFIED**: `Services/NotificationQueueService.cs` — replace `IOptions<NotificationSettings>` with `INotificationConfigurationReader`

### Database (`Mentoory.Db/`)
- **NEW**: `notification/Tables/NotificationConfigurations.sql` — SSDT table definition

### Seed Data (`Mentoory.Db.PostDeployment/`)
- **NEW**: `019.SeedNotificationConfiguration.sql` — idempotent seed for all 10 config keys
- **MODIFIED**: `Script.PostDeployment.sql` — add `:r` reference to new seed script

## How to Verify

1. Build: `dotnet build` (zero warnings)
2. Publish DB: `cd Mentoory.Db && publish-mentoorydb.sh`
3. Run: `dotnet run --project Mentoory.Aspire.AppHost`
4. Verify: Check that `notification.NotificationConfigurations` table is seeded with 10 rows
5. Test: Trigger a notification (e.g., user registration) and confirm email sends using DB config values
