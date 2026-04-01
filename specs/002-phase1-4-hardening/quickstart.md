# Quickstart: Phase 1-4 Hardening Verification

**Branch**: `002-phase1-4-hardening` | **Date**: 2026-04-01

## Prerequisites

- .NET 10.0 SDK installed
- SQL Server instance running (local or Docker)
- Node.js (for Playwright)
- Database published with DACPAC + PostDeployment scripts

## Setup Steps

### 1. Build and Verify Zero Warnings

```bash
dotnet build
```

The solution enforces `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`. If any warnings exist after corrections, the build fails.

### 2. Publish Database with Seed Data

```bash
cd Mentoory.Db && ./publish-mentoorydb.sh
```

This runs all PostDeployment scripts (000-004+) which create:
- Roles, GlobalAdmin user, subscription plan (existing)
- Test users, incubators, projects, diagnostic data (new — script 004)

### 3. Run the Application

```bash
dotnet run --project Mentoory.Aspire.AppHost
```

Or for web-only:

```bash
dotnet run --project Mentoory.Web
```

### 4. Run Unit Tests

```bash
dotnet test
```

All existing and new unit tests must pass.

### 5. Run E2E Tests

```bash
cd tests/Mentoory.Tests.E2E
dotnet test
```

Playwright tests run against a live instance with seed data.

## Seed User Credentials

| User | Email | Role(s) | Context |
|------|-------|---------|---------|
| Global Admin | admin@mentoory.com | GlobalAdmin | Global scope (IncubatorId=0) |
| Incubator Admin 1 | incadmin1@test.mentoory.com | IncubatorAdmin | Incubator 1 |
| Incubator Admin 2 | incadmin2@test.mentoory.com | IncubatorAdmin | Incubator 2 |
| Project Coordinator 1 | coord1@test.mentoory.com | ProjectCoordinator | Incubator 1, Project 1 |
| Project Coordinator 2 | coord2@test.mentoory.com | ProjectCoordinator | Incubator 1, Project 2 |
| Mentor | mentor1@test.mentoory.com | Mentor | Incubator 1, Project 1 |
| Entrepreneur 1 | entrepreneur1@test.mentoory.com | Entrepreneur | Incubator 1, Project 1 |
| Entrepreneur 2 | entrepreneur2@test.mentoory.com | Entrepreneur | Incubator 2, Project 3 |
| Sponsor | sponsor1@test.mentoory.com | Sponsor | Incubator 1 |
| Multi-Role User | multirole@test.mentoory.com | IncubatorAdmin + ProjectCoordinator | Incubator 1 (admin) + Project 2 (coordinator) |

Default password for all test users: `Test123!@#` (must be changed after first login in production).

## Verification Checklist

After setup, verify:

- [ ] Build succeeds with zero warnings
- [ ] Database publishes without errors
- [ ] GlobalAdmin can log in and sees Platform dashboard
- [ ] IncubatorAdmin logs in and sees Administration dashboard
- [ ] Multi-role user sees role selection screen
- [ ] Context switching works from top bar
- [ ] Diagnostic form can be cloned, viewed, and submitted
- [ ] All `dotnet test` pass
- [ ] All Playwright tests pass

## Troubleshooting

| Issue | Solution |
|-------|---------|
| Build warnings | Fix the warning — TreatWarningsAsErrors blocks the build |
| Database publish fails | Verify SQL Server is running and connection string is correct |
| Login fails | Run PostDeployment scripts to ensure seed data exists |
| Context selection shows no options | Verify RoleAssignment seed data for the user |
| Playwright tests fail to launch | Ensure `npx playwright install` has been run |
