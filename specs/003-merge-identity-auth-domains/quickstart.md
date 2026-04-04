# Quickstart: Merge Identity and Authorization Domains

**Branch**: `003-merge-identity-auth-domains` | **Date**: 2026-04-03

## Overview

This feature merges 6 projects into 3 under a unified "Access" domain. No logic changes — pure namespace/project/schema rename.

## Execution Order

The merge must follow this strict order to maintain a compilable solution at each step:

### Step 1: Create New Access Projects

Create the three new `.csproj` files with correct references:
- `Mentoory.Access.Domain` → references `Mentoory.Shared.Domain`
- `Mentoory.Access.Application` → references `Access.Domain`, `Mentoory.Shared.Application`
- `Mentoory.Access.Infrastructure` → references `Access.Domain`, `Mentoory.Shared.Infrastructure`

### Step 2: Move and Rename Source Files

Copy all source files from Identity and Authorization projects into the new Access projects:
- Identity.Domain + Authorization.Domain → Access.Domain
- Identity.Application + Authorization.Application → Access.Application
- Identity.Infrastructure + Authorization.Infrastructure → Access.Infrastructure

Update all namespaces: `Mentoory.Identity.*` → `Mentoory.Access.*`, `Mentoory.Authorization.*` → `Mentoory.Access.*`

### Step 3: Consolidate DbContexts

Merge IdentityDbContext + AuthorizationDbContext into AccessDbContext with all DbSets and EF configuration, using `[access]` schema.

### Step 4: Simplify Integration Event Pattern

Remove `UserRegisteredEventHandler` from Application layer. Add direct UserProfile creation to `RegisterUserHandler`.

### Step 5: Update Web Layer

- Rename `Areas/Identity/` → `Areas/Access/`
- Update controller namespaces and area attributes
- Update `Program.cs` cookie paths: `/Identity/Login` → `/Access/Login`
- Update `DbContextFactory` mapping: "Access" → AccessDbContext

### Step 6: Update DI Registrations

Replace `AddIdentityApplication()`, `AddIdentityInfrastructure()`, `AddAuthorizationApplication()`, `AddAuthorizationInfrastructure()` with `AddAccessApplication()` and `AddAccessInfrastructure()` in `Program.cs`.

### Step 7: Update Database Schema

- Create `Mentoory.Db/access/` with Schema.sql and all table definitions
- Remove `Mentoory.Db/identity/` and `Mentoory.Db/authorization/`
- Update PostDeployment scripts (002, 004, 005) to use `[access]` schema
- Add migration script for existing data

### Step 8: Merge Test Projects

Create `Mentoory.Access.Tests`, move all tests from Identity.Tests and Authorization.Tests, update namespaces.

### Step 9: Clean Up Solution

- Remove old project directories (6 source + 2 test)
- Update `Mentoory.sln` to reference new projects, remove old ones
- Update solution folders

### Step 10: Verify

- `dotnet build` — zero errors, zero warnings
- `dotnet test` — all tests pass
- Grep for `Mentoory.Identity` and `Mentoory.Authorization` — zero matches

## Key Files to Modify

| File | Change |
|------|--------|
| `Mentoory.sln` | Remove 8 old projects + 2 solution folders, add 4 new projects + 1 folder |
| `Mentoory.Web/Program.cs` | DI calls, cookie paths |
| `Mentoory.Web/Infrastructure/Persistence/DbContextFactory.cs` | Mapping dictionary |
| `Mentoory.Web/Areas/Access/*` | All controllers, models, views |
| `Mentoory.Db/access/*` | All schema and table definitions |
| `Mentoory.Db.PostDeployment/002,004,005` | Schema references |
| `tests/Mentoory.Tests.Integration/*` | Namespace references |
| `tests/Mentoory.Tests.E2E/*` | URL path references |

## Verification Commands

```bash
# Build
dotnet build

# Run all tests
dotnet test

# Verify no orphan references (all relevant file types)
grep -r "Mentoory\.Identity" --include="*.cs" --include="*.csproj" --include="*.sln" --include="*.sql" --include="*.cshtml" --include="*.json" .
grep -r "Mentoory\.Authorization" --include="*.cs" --include="*.csproj" --include="*.sln" --include="*.sql" --include="*.cshtml" --include="*.json" .
```
