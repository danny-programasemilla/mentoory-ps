# Feature Specification: Notification Database Configuration

**Feature Branch**: `013-notification-db-config`  
**Created**: 2026-04-13  
**Status**: Draft  
**Input**: User description: "The notification domain should have its own configuration database and don't use any appsettings.json just like ISystemConfigurationReader"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - System Administrator Manages Notification Settings (Priority: P1)

A system administrator needs to change notification behavior (e.g., retry limits, polling intervals, SMTP connection details) without redeploying the application. They update values in the database, and the notification system picks up the changes on the next read cycle.

**Why this priority**: Core value of the feature. Without database-driven configuration, every setting change requires a code deployment. This is the foundational capability that all other stories build upon.

**Independent Test**: Can be fully tested by inserting/updating configuration records in the database and verifying that the notification system reads and applies the new values when processing the next notification cycle.

**Acceptance Scenarios**:

1. **Given** a notification configuration record exists with key "MaxRetryAttempts" and value "5", **When** the notification processor retrieves the max retry setting, **Then** it receives the integer value 5.
2. **Given** an administrator updates the "MaxRetryAttempts" value to "3" in the database, **When** the notification processor retrieves the setting on the next read, **Then** it receives the updated value 3.
3. **Given** a required configuration key does not exist in the database, **When** the system attempts to read it, **Then** the system raises a clear error indicating the missing key.
4. **Given** a configuration record has a value that cannot be parsed to the expected type (e.g., "abc" for an integer field), **When** the system reads it, **Then** it raises a clear error indicating the invalid value.

---

### User Story 2 - Notification System Uses Database SMTP Settings (Priority: P1)

The notification email delivery service reads all SMTP connection settings (host, port, credentials, sender details, encryption flag) from the database instead of appsettings.json. This removes the dependency on configuration files for sensitive credentials and operational parameters.

**Why this priority**: Equal priority with Story 1 because SMTP settings are the most critical notification configuration. Without this, the core email delivery pipeline still depends on appsettings.json.

**Independent Test**: Can be tested by inserting SMTP configuration records in the database and sending a test email, verifying the system connects using the database values rather than any appsettings.json section.

**Acceptance Scenarios**:

1. **Given** SMTP configuration records exist in the database (host, port, username, password, sender address, sender name, SSL flag), **When** the email service sends a notification, **Then** it connects to the SMTP server using those database values.
2. **Given** the SMTP host configuration is changed in the database, **When** the next email is sent, **Then** the system uses the updated SMTP host.
3. **Given** a required SMTP configuration key is missing, **When** the email service attempts to send, **Then** it raises a clear error identifying which SMTP setting is absent.

---

### User Story 3 - Notification Processor Uses Database Operational Settings (Priority: P2)

The background notification processor reads its operational parameters (polling interval, base URL for email links) from the database. This allows fine-tuning notification delivery behavior without redeployment.

**Why this priority**: Important for operational flexibility but secondary to the core config infrastructure and SMTP settings. The system can function with sensible defaults while this is implemented.

**Independent Test**: Can be tested by setting the polling interval to a specific value in the database and observing the processor loop timing, or by changing the base URL and verifying generated email links reflect the update.

**Acceptance Scenarios**:

1. **Given** a "PollingIntervalSeconds" configuration exists with value "30", **When** the notification processor starts, **Then** it polls for pending notifications every 30 seconds.
2. **Given** a "BaseUrl" configuration exists with value "https://app.mentoory.com", **When** a registration email is generated, **Then** the verification link uses that base URL.

---

### Edge Cases

- What happens when the database is temporarily unreachable while reading configuration? The system should propagate the error (fail-fast) rather than silently falling back to defaults.
- What happens when a configuration key is defined with an empty string value? The system should treat it as an invalid value and raise an error.
- What happens when the same configuration key is queried multiple times within a single request? Each call reads from the database through the repository (consistent with the Access domain pattern).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The Notification domain MUST store all its operational configuration in a dedicated database table within the `notification` schema.
- **FR-002**: The system MUST NOT read any notification-specific settings from appsettings.json (SMTP settings, polling intervals, retry limits, base URL).
- **FR-003**: The system MUST provide a typed configuration reader interface in the Notification Application layer (following the same contract pattern as the Access domain's `ISystemConfigurationReader`).
- **FR-004**: The configuration reader MUST support reading string, integer, and boolean values from the database.
- **FR-005**: The system MUST fail fast with a clear error when a required configuration key is missing from the database.
- **FR-006**: The system MUST fail fast with a clear error when a stored value cannot be parsed to the expected type.
- **FR-007**: The Notification domain MUST define an enumeration of all known configuration keys to avoid magic strings.
- **FR-008**: Configuration values MUST be changeable at runtime by updating the database record, without requiring application restart.
- **FR-009**: The configuration database table MUST store key, value, description, and data type metadata for each setting.
- **FR-010**: The system MUST provide seed data for all required notification configuration keys with sensible default values.

### Key Entities

- **NotificationConfiguration**: A system setting specific to the notification domain. Identified by a unique key. Stores the value as a string with a data type hint. Includes a human-readable description for administrative context. Tracks creation and modification timestamps.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All notification SMTP and operational settings are read from the database; zero notification-related keys remain in appsettings.json.
- **SC-002**: Administrators can change any notification setting by updating the database and the change takes effect without application restart.
- **SC-003**: The system reports a clear, actionable error within 1 second when a required notification configuration key is missing or invalid.
- **SC-004**: The notification configuration pattern is structurally consistent with the Access domain's configuration pattern (same interface contract shape, same entity structure, same fail-fast behavior).

## Assumptions

- The existing Access domain configuration pattern (`ISystemConfigurationReader`, `SystemConfiguration` entity, `SystemConfigurations` table) is the proven reference architecture and should be replicated for the Notification domain.
- The configuration reader interface for the Notification domain will live in `Mentoory.Notification.Application` and the implementation in `Mentoory.Notification.Infrastructure`, consistent with Clean Architecture boundaries.
- The database table will live in the `notification` schema (e.g., `notification.NotificationConfigurations`), separate from the Access domain's `access.SystemConfigurations` table.
- The existing `SmtpSettings` and `NotificationSettings` classes (currently populated from appsettings.json) will be replaced by database reads through the new configuration reader.
- Seed data will be provided via PostDeployment scripts in `/Mentoory.Db.PostDeployment/` following the existing project convention.
- The configuration reader will be scoped per request, matching the Access domain's lifecycle choice.
- No caching layer is needed for v1; configuration reads go directly to the database per call (consistent with the Access domain pattern).
