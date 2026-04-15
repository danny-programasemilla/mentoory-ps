# Data Model: Notification Database Configuration

**Branch**: `013-notification-db-config` | **Date**: 2026-04-13

## New Entity: NotificationConfiguration

**Schema**: `notification` | **Table**: `NotificationConfigurations`

### Fields

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | BIGINT IDENTITY | PK, clustered | Internal identifier |
| ExternalId | UNIQUEIDENTIFIER | NOT NULL, UNIQUE, DEFAULT NEWID() | External identifier for API exposure |
| Key | NVARCHAR(100) | NOT NULL, UNIQUE | Configuration key name (matches enum) |
| Value | NVARCHAR(500) | NOT NULL | Stored as string, parsed by reader |
| Description | NVARCHAR(500) | NULL | Human-readable description (Spanish) |
| DataType | NVARCHAR(50) | NOT NULL | Type hint: "String", "Integer", "Boolean" |
| CreatedAtUtc | DATETIME2 | NOT NULL | Creation timestamp |
| UpdatedAtUtc | DATETIME2 | NOT NULL | Last modification timestamp |

### Indexes

| Name | Columns | Type |
|------|---------|------|
| PK_notification_NotificationConfigurations | Id | Clustered PK |
| UQ_NotificationConfigurations_ExternalId | ExternalId | Unique |
| UQ_NotificationConfigurations_Key | Key | Unique |

### Relationships

None. This is a standalone aggregate root with no foreign keys.

## Configuration Key Enum: NotificationConfigurationKey

| Enum Value | Ordinal | DataType | Default Value | Description |
|------------|---------|----------|---------------|-------------|
| SmtpHost | 0 | String | (environment-specific) | SMTP server hostname |
| SmtpPort | 1 | Integer | 587 | SMTP server port |
| SmtpUsername | 2 | String | (environment-specific) | SMTP authentication username |
| SmtpPassword | 3 | String | (environment-specific) | SMTP authentication password |
| SmtpFromAddress | 4 | String | (environment-specific) | Sender email address |
| SmtpFromName | 5 | String | Mentoory | Sender display name |
| SmtpUseSsl | 6 | Boolean | true | Whether to use SSL/TLS |
| PollingIntervalSeconds | 7 | Integer | 15 | Background processor polling interval |
| MaxRetryAttempts | 8 | Integer | 5 | Maximum delivery retry attempts per recipient |
| BaseUrl | 9 | String | (environment-specific) | Base URL for generating email action links |

## State Diagram

```
NotificationConfiguration is stateless — it is a key-value record.
Only mutation: Update(value, utcNow) changes Value and UpdatedAtUtc.
```

## Validation Rules

- Key: required, non-whitespace, max 100 chars
- Value: required (non-null), max 500 chars
- DataType: required, non-whitespace, max 50 chars
- ExternalId: auto-generated, immutable after creation
- CreatedAtUtc: set at creation, immutable
- UpdatedAtUtc: updated on every mutation
