# Data Model: Merge Identity and Authorization Domains

**Branch**: `003-merge-identity-auth-domains` | **Date**: 2026-04-03

> No new entities or relationships are introduced. This document captures the unified data model after merging two schemas into one.

## Unified `[access]` Schema

All tables move to the `[access]` schema. No column, index, constraint, or relationship changes.

### Aggregates

#### User Aggregate (from Identity)

**Table**: `[access].[Users]`

| Column | Type | Constraints |
|--------|------|-------------|
| Id | int | PK, IDENTITY |
| ExternalId | uniqueidentifier | UNIQUE, NOT NULL, DEFAULT NEWID() |
| Email | nvarchar(256) | NOT NULL |
| NormalizedEmail | nvarchar(256) | UNIQUE, NOT NULL |
| Country | nvarchar(3) | NOT NULL |
| NationalId | nvarchar(50) | NOT NULL |
| FirstName | nvarchar(100) | NOT NULL |
| LastName | nvarchar(100) | NOT NULL |
| AccountStatus | int | NOT NULL, DEFAULT 0 |
| FailedLoginAttempts | int | NOT NULL, DEFAULT 0 |
| LockoutEndUtc | datetime2 | NULL |
| EmailVerifiedAtUtc | datetime2 | NULL |
| CreatedAtUtc | datetime2 | NOT NULL |
| UpdatedAtUtc | datetime2 | NOT NULL |

**Child: `[access].[Credentials]`**

| Column | Type | Constraints |
|--------|------|-------------|
| Id | int | PK, IDENTITY |
| UserId | int | FK → Users, CASCADE DELETE |
| PasswordHash | nvarchar(500) | NOT NULL |
| IsActive | bit | NOT NULL, DEFAULT 1 |
| CreatedAtUtc | datetime2 | NOT NULL |

**Child: `[access].[EmailVerificationTokens]`**

| Column | Type | Constraints |
|--------|------|-------------|
| Id | int | PK, IDENTITY |
| UserId | int | FK → Users |
| TokenHash | nvarchar(500) | NOT NULL |
| ExpiresAtUtc | datetime2 | NOT NULL |
| IsUsed | bit | NOT NULL, DEFAULT 0 |
| CreatedAtUtc | datetime2 | NOT NULL |

**Child: `[access].[PasswordResetTokens]`**

| Column | Type | Constraints |
|--------|------|-------------|
| Id | int | PK, IDENTITY |
| UserId | int | FK → Users |
| TokenHash | nvarchar(500) | NOT NULL |
| ExpiresAtUtc | datetime2 | NOT NULL |
| IsUsed | bit | NOT NULL, DEFAULT 0 |
| CreatedAtUtc | datetime2 | NOT NULL |

#### AuthSession Aggregate (from Identity)

**Table**: `[access].[AuthSessions]`

| Column | Type | Constraints |
|--------|------|-------------|
| Id | int | PK, IDENTITY |
| SessionToken | nvarchar(500) | UNIQUE (filtered: IsActive=1) |
| UserId | int | FK → Users, CASCADE DELETE |
| IpAddress | nvarchar(45) | NOT NULL |
| UserAgent | nvarchar(500) | NOT NULL |
| CreatedAtUtc | datetime2 | NOT NULL |
| LastActivityUtc | datetime2 | NOT NULL |
| ExpiresAtUtc | datetime2 | NOT NULL |
| IsActive | bit | NOT NULL, DEFAULT 1 |
| ActiveIncubatorId | int | NULL |
| ActiveProjectId | int | NULL |
| ActiveRole | nvarchar(50) | NULL |

#### RoleAssignment Aggregate (from Authorization)

**Table**: `[access].[RoleAssignments]`

| Column | Type | Constraints |
|--------|------|-------------|
| Id | int | PK, IDENTITY |
| ExternalId | uniqueidentifier | UNIQUE, NOT NULL, DEFAULT NEWID() |
| UserId | int | NOT NULL |
| IncubatorId | int | NOT NULL (0 = global scope) |
| ProjectId | int | NULL |
| Role | nvarchar(50) | NOT NULL |
| IsActive | bit | NOT NULL, DEFAULT 1 |
| CreatedAtUtc | datetime2 | NOT NULL |
| UpdatedAtUtc | datetime2 | NOT NULL |

### Read Models

#### UserProfile (from Authorization)

**Table**: `[access].[UserProfiles]`

| Column | Type | Constraints |
|--------|------|-------------|
| Id | int | PK, IDENTITY |
| UserId | int | UNIQUE, NOT NULL |
| UserExternalId | uniqueidentifier | UNIQUE, NOT NULL |
| Email | nvarchar(256) | NOT NULL |
| FirstName | nvarchar(100) | NOT NULL |
| LastName | nvarchar(100) | NOT NULL |
| AccountStatus | int | NOT NULL |
| CreatedAtUtc | datetime2 | NOT NULL |
| LastSyncedAtUtc | datetime2 | NOT NULL |

## Domain Model (C# — namespace changes only)

All entities move to `Mentoory.Access.Domain.*` namespace. No property, method, or behavior changes.

| Entity | Old Namespace | New Namespace |
|--------|---------------|---------------|
| User | Mentoory.Identity.Domain.Aggregates.User | Mentoory.Access.Domain.Aggregates.User |
| Credential | Mentoory.Identity.Domain.Aggregates.User | Mentoory.Access.Domain.Aggregates.User |
| EmailVerificationToken | Mentoory.Identity.Domain.Aggregates.User | Mentoory.Access.Domain.Aggregates.User |
| PasswordResetToken | Mentoory.Identity.Domain.Aggregates.User | Mentoory.Access.Domain.Aggregates.User |
| AuthSession | Mentoory.Identity.Domain.Aggregates.AuthSession | Mentoory.Access.Domain.Aggregates.AuthSession |
| RoleAssignment | Mentoory.Authorization.Domain.Aggregates.RoleAssignment | Mentoory.Access.Domain.Aggregates.RoleAssignment |
| UserProfile | Mentoory.Authorization.Domain.ReadModels | Mentoory.Access.Domain.ReadModels |
| UserContext | Mentoory.Authorization.Domain.ReadModels | Mentoory.Access.Domain.ReadModels |
| EmailAddress | Mentoory.Identity.Domain.ValueObjects | Mentoory.Access.Domain.ValueObjects |
| NationalIdentity | Mentoory.Identity.Domain.ValueObjects | Mentoory.Access.Domain.ValueObjects |
| HashedPassword | Mentoory.Identity.Domain.ValueObjects | Mentoory.Access.Domain.ValueObjects |
| AccountStatus | Mentoory.Identity.Domain.Enums | Mentoory.Access.Domain.Enums |
| PlatformRole | Mentoory.Authorization.Domain.Enums | Mentoory.Access.Domain.Enums |
| Permission | Mentoory.Authorization.Domain.Enums | Mentoory.Access.Domain.Enums |
| IUserRepository | Mentoory.Identity.Domain.Repositories | Mentoory.Access.Domain.Repositories |
| IAuthSessionRepository | Mentoory.Identity.Domain.Repositories | Mentoory.Access.Domain.Repositories |
| IRoleAssignmentRepository | Mentoory.Authorization.Domain.Repositories | Mentoory.Access.Domain.Repositories |
| IUserProfileRepository | Mentoory.Authorization.Domain.Repositories | Mentoory.Access.Domain.Repositories |
| IPasswordHasher | Mentoory.Identity.Domain.Services | Mentoory.Access.Domain.Services |

## DbContext Consolidation

**Before**: IdentityDbContext + AuthorizationDbContext (separate projects, same DB)
**After**: AccessDbContext (single project, same DB)

The new AccessDbContext combines all DbSets and EF configuration from both contexts into one, using the `[access]` schema for all table mappings.
