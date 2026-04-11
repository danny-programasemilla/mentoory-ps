# Implementation Plan: Merge Identity and Authorization Domains

**Branch**: `003-merge-identity-auth-domains` | **Date**: 2026-04-03 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/003-merge-identity-auth-domains/spec.md`

## Summary

Merge the separate Identity (6 files: Domain/Application/Infrastructure) and Authorization (6 files: Domain/Application/Infrastructure) bounded contexts into a single "Access" domain (`Mentoory.Access.*`). This is a pure structural refactor — no behavioral changes. All entities, commands, queries, handlers, validators, repositories, DbContexts, database schemas, test projects, DI registrations, Web Areas, and middleware must be consolidated under the new namespace. The cross-domain integration event pattern (UserRegisteredEvent handler) becomes internal. All tests must pass, zero warnings, zero orphan references.

## Technical Context

**Language/Version**: C# / .NET 10.0 (SDK 10.0.0)
**Primary Dependencies**: ASP.NET Core MVC, MediatR 14.1, FluentValidation 12.1, Mapperly 4.x, EF Core 10.x, MailKit/MimeKit
**Storage**: SQL Server with SSDT/DACPAC schema management, EF Core 10.x ORM
**Testing**: xUnit, Moq, FluentAssertions, Respawn, Playwright (E2E)
**Target Platform**: Linux/Windows server, .NET Aspire 13.2.0 orchestration
**Project Type**: Web application (modular monolith)
**Performance Goals**: N/A (structural refactor, no behavior change)
**Constraints**: Zero compiler warnings (TreatWarningsAsErrors), zero orphan references, all tests pass
**Scale/Scope**: 6 projects → 3 projects, 2 DB schemas → 1 schema, 2 DbContexts → 1, 2 test projects → 1, 1 Web Area rename

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Clean Architecture Layer Boundaries | PASS | New Access projects maintain Domain/Application/Infrastructure separation |
| II. CQRS Pattern Requirements | PASS | All commands/queries move unchanged — base classes, validators preserved |
| III. DDD Constraints | PASS | Aggregates, value objects, ExternalId patterns preserved as-is |
| IV. Integration Events (ADR-001) | PASS | Cross-domain event becomes internal; integration event contracts remain for other domains if needed |
| V. Zero-Warnings Policy | PASS | Must verify after merge — namespace changes could surface unused usings |
| VI. DateTime Handling | PASS | No changes to time handling |
| VII. Naming Conventions | PASS | All artifact names follow existing patterns, just namespace prefix changes |
| VIII. File Organization | PASS | One class per file preserved; JS stays in wwwroot |
| IX. Spanish-First UI | PASS | No UI text changes |
| X. Role Hierarchy & Session Context | PASS | Role model, claims, and session context logic unchanged |
| XI. SSDT/DACPAC Database Strategy | PASS | Schema rename follows SSDT conventions; PostDeployment scripts updated |

No violations. No complexity tracking needed.

## Project Structure

### Documentation (this feature)

```text
specs/003-merge-identity-auth-domains/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

**BEFORE (current):**

```text
Mentoory.Identity.Domain/
├── Aggregates/User/          # User, Credential, EmailVerificationToken, PasswordResetToken
├── Aggregates/AuthSession/   # AuthSession
├── ValueObjects/             # EmailAddress, NationalIdentity, HashedPassword
├── Enums/                    # AccountStatus
├── Repositories/             # IUserRepository, IAuthSessionRepository
└── Services/                 # IPasswordHasher

Mentoory.Identity.Application/
├── Commands/                 # RegisterUser, LoginUser, LogoutUser, VerifyEmail, etc.
├── Queries/                  # GetUserByEmail, GetUserByExternalId, ListUsers, ValidateSession
├── IntegrationEvents/        # UserRegisteredEvent, UserEmailVerifiedEvent, etc.
└── DependencyInjection.cs

Mentoory.Identity.Infrastructure/
├── Persistence/              # IdentityDbContext, UserRepository, AuthSessionRepository
├── Services/                 # Pbkdf2PasswordHasher
└── DependencyInjection.cs

Mentoory.Authorization.Domain/
├── Aggregates/RoleAssignment/ # RoleAssignment
├── ReadModels/               # UserProfile, UserContext
├── Enums/                    # PlatformRole, Permission
└── Repositories/             # IRoleAssignmentRepository, IUserProfileRepository

Mentoory.Authorization.Application/
├── Commands/                 # AssignRole, RevokeRole, SetActiveContext
├── Queries/                  # GetActiveContext, GetUserContexts, CheckPermission, ListIncubatorMembers
├── IntegrationEvents/Handlers/ # UserRegisteredEventHandler
└── DependencyInjection.cs

Mentoory.Authorization.Infrastructure/
├── Persistence/              # AuthorizationDbContext, RoleAssignmentRepository, UserProfileRepository
└── DependencyInjection.cs

Mentoory.Db/
├── identity/Schema.sql
├── identity/Tables/          # Users, Credentials, AuthSessions, EmailVerificationTokens, PasswordResetTokens
├── authorization/Schema.sql
└── authorization/Tables/     # RoleAssignments, UserProfiles

tests/Mentoory.Identity.Tests/
tests/Mentoory.Authorization.Tests/
```

**AFTER (target):**

```text
Mentoory.Access.Domain/
├── Aggregates/User/          # User, Credential, EmailVerificationToken, PasswordResetToken
├── Aggregates/AuthSession/   # AuthSession
├── Aggregates/RoleAssignment/ # RoleAssignment
├── ReadModels/               # UserProfile, UserContext
├── ValueObjects/             # EmailAddress, NationalIdentity, HashedPassword
├── Enums/                    # AccountStatus, PlatformRole, Permission
├── Repositories/             # IUserRepository, IAuthSessionRepository, IRoleAssignmentRepository, IUserProfileRepository
└── Services/                 # IPasswordHasher

Mentoory.Access.Application/
├── Commands/                 # RegisterUser, LoginUser, LogoutUser, VerifyEmail, AssignRole, RevokeRole, SetActiveContext, etc.
├── Queries/                  # GetUserByEmail, GetUserByExternalId, ListUsers, ValidateSession, GetActiveContext, GetUserContexts, CheckPermission, ListIncubatorMembers
├── IntegrationEvents/        # UserRegisteredEvent, UserEmailVerifiedEvent, etc. (kept for other domains)
└── DependencyInjection.cs    # Unified AddAccessApplication()

Mentoory.Access.Infrastructure/
├── Persistence/              # AccessDbContext (single), all repositories
├── Services/                 # Pbkdf2PasswordHasher
└── DependencyInjection.cs    # Unified AddAccessInfrastructure()

Mentoory.Db/
├── access/Schema.sql
└── access/Tables/            # Users, Credentials, AuthSessions, EmailVerificationTokens, PasswordResetTokens, RoleAssignments, UserProfiles

Mentoory.Web/Areas/Access/    # Renamed from Areas/Identity/

tests/Mentoory.Access.Tests/  # Merged from Identity.Tests + Authorization.Tests
```

**Structure Decision**: The six Identity/Authorization projects collapse into three Access projects following the existing Clean Architecture pattern. The two DbContexts (IdentityDbContext + AuthorizationDbContext) merge into a single AccessDbContext. Two DB schemas merge into one `[access]` schema. Two test projects merge into one.

## Complexity Tracking

> No constitution violations detected — section intentionally left empty.
