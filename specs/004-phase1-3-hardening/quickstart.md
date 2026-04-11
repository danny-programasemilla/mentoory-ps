# Quickstart: Phase 1–3 Hardening

**Date**: 2026-04-03

## Prerequisites

- .NET 10.0 SDK
- Docker (for SQL Server via Testcontainers)
- Node.js (for Playwright E2E tests)

## Build & Run

```bash
# Build all projects
dotnet build

# Run with Aspire orchestration
dotnet run --project Mentoory.Aspire.AppHost

# Run web only
dotnet run --project Mentoory.Web
```

## Database

Schema is managed via SSDT/DACPAC. After building `Mentoory.Db`, deploy using:

```bash
cd Mentoory.Db && ./publish-mentoorydb.sh
```

PostDeployment scripts run automatically during DACPAC deployment. New scripts for this feature:
- `016.SeedCountries.sql` — Costa Rica and supported countries
- `017.SeedSystemConfiguration.sql` — Default configuration values
- `018.AddProjectPublicFlag.sql` — Set IsPublic defaults for existing projects

## Testing

```bash
# All tests
dotnet test

# Unit tests only
dotnet test tests/Mentoory.Access.Tests
dotnet test tests/Mentoory.Tenant.Tests

# Integration tests (requires Docker for Testcontainers)
dotnet test tests/Mentoory.Tests.Integration

# E2E tests (requires Docker + Playwright)
dotnet test tests/Mentoory.Tests.E2E
```

## Key Test Accounts (seed data)

| Email | Password | Role |
|-------|----------|------|
| admin@mentoory.com | 123abc987 | GlobalAdmin |
| incadmin1@mentoory.com | Test123!@# | IncubatorAdmin |
| coord1@mentoory.com | Test123!@# | ProjectCoordinator |

## Feature-Specific Verification

### Manual verification of hardening changes:

1. **Server-side session validation**: Log in, then manually deactivate the session in the DB. Next request should redirect to login.
2. **Forced password change**: Set a user's AccountStatus to 4 (PasswordResetRequired) in DB. Login should redirect to password change.
3. **Registration with verification token**: Register a new user and verify that `EmailVerificationTokens` has a new row.
4. **Country-based ID mask**: Select Costa Rica on registration, verify ID field applies mask.
5. **Configuration from DB**: Change `MaxFailedLoginAttempts` in `SystemConfigurations` table and verify behavior changes.

## Architecture Notes

- **Access domain**: User identity, authentication, verification, configuration
- **Tenant domain**: Projects, invitations, enrollment, participation
- **Cross-domain**: Integration events (`UserRegisteredEvent`, `UserEmailVerifiedEvent`)
- **No email delivery**: All flows model states/tokens but do not send emails. Admin manual progression available.
