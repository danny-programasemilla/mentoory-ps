# Implementation Plan: Phase 1–3 Hardening

**Branch**: `004-phase1-3-hardening` | **Date**: 2026-04-03 | **Spec**: [spec.md](spec.md)  
**Input**: Feature specification from `/specs/004-phase1-3-hardening/spec.md`

## Summary

This is a correction and stabilization effort — not new feature development. The hardening addresses critical gaps in the existing registration, authentication, verification, and project enrollment flows to bring them to production-ready quality. Key deliverables: (1) server-side session validation on every request, (2) forced password change enforcement for PasswordResetRequired status, (3) email verification token generation at registration, (4) project invitation lifecycle with explicit states, (5) batch user registration with CSV upload, (6) database-driven configuration replacing all hardcoded values, (7) public project self-enrollment for users without project context, (8) administrative tools for manual user state progression.

## Technical Context

**Language/Version**: C# / .NET 10.0 (SDK 10.0.0)  
**Primary Dependencies**: ASP.NET Core MVC, MediatR 14.1, FluentValidation 12.1, Mapperly 4.x, EF Core 10.x, CsvHelper  
**Storage**: SQL Server with SSDT/DACPAC schema management, EF Core 10.x ORM  
**Testing**: xUnit, Moq, FluentAssertions, Respawn, Testcontainers (SQL Server 2022), Playwright (Chromium headless)  
**Target Platform**: Linux/Windows server with .NET Aspire 13.2.0 orchestration  
**Project Type**: Web application (modular monolith, Clean Architecture)  
**Performance Goals**: Batch upload of 500 CSV rows completes within HTTP request timeout; all page loads under 2 seconds  
**Constraints**: Zero-warnings build, Spanish UI, no EF migrations, SSDT/DACPAC only, no email delivery in this phase  
**Scale/Scope**: Hundreds of users per batch upload, dozens of concurrent users, ~15 existing controllers across 5 areas

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| # | Gate | Status | Notes |
|---|------|--------|-------|
| 1 | Clean Architecture Layer Boundaries (Principle I) | PASS | All changes follow existing layer structure: Domain aggregates, Application commands/queries, Infrastructure persistence, Web controllers |
| 2 | CQRS Pattern Requirements (Principle II) | PASS | New commands use IBaseRequest/BaseCommandHandler, FluentValidation validators for all user input |
| 3 | DDD Constraints (Principle III) | PASS | New entities (ProjectInvitation, Country, SystemConfiguration) include ExternalId; cross-aggregate references by ID only |
| 4 | Integration Events (Principle IV) | PASS | Cross-domain communication (Access→Tenant for enrollment) via integration events with handlers in target domain |
| 5 | Zero-Warnings Policy (Principle V) | PASS | All code must compile with zero warnings |
| 6 | DateTime Handling (Principle VI) | PASS | ITimeProvider injected in Application handlers; DateTime passed as parameter in Domain methods |
| 7 | Naming Conventions (Principle VII) | PASS | Follows {Verb}{Entity}Command, {Get/List}{Entity}Query patterns |
| 8 | File Organization (Principle VIII) | PASS | One class per file, JS in /wwwroot/js/, PostDeployment in /Mentoory.Db.PostDeployment/ |
| 9 | Spanish-First UI (Principle IX) | PASS | All validation messages, labels, toasts in Spanish |
| 10 | Role Hierarchy & Session Context (Principle X) | PASS | [Authorize] includes higher roles; GlobalAdmin in all menu groups; controllers handle missing context |
| 11 | SSDT/DACPAC Database Strategy (Principle XI) | PASS | Schema changes via SSDT SQL files; seed data via numbered idempotent PostDeployment scripts |

No violations. No Complexity Tracking entries needed.

## Project Structure

### Documentation (this feature)

```text
specs/004-phase1-3-hardening/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (internal contracts only)
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
# Access Domain (User, Auth, Verification, Configuration)
Mentoory.Access.Domain/
├── Aggregates/
│   ├── User/                    # Modified: token generation at registration, password rules
│   │   ├── User.cs
│   │   ├── Credential.cs
│   │   ├── EmailVerificationToken.cs
│   │   └── PasswordResetToken.cs
│   ├── AuthSession/             # Existing: session lifecycle
│   │   └── AuthSession.cs
│   ├── RoleAssignment/          # Existing: role management
│   │   └── RoleAssignment.cs
│   └── SystemConfiguration/     # NEW: database-driven configuration
│       └── SystemConfiguration.cs
├── Enums/
│   ├── AccountStatus.cs         # Existing: PendingVerification, Active, Locked, Disabled, PasswordResetRequired
│   └── ConfigurationKey.cs      # NEW: enum of known configuration keys
├── ReadModels/
│   └── UserProfile.cs           # Modified: keep in sync with User status changes
└── ValueObjects/
    ├── EmailAddress.cs
    └── NationalIdentity.cs

Mentoory.Access.Application/
├── Users/
│   ├── Commands/
│   │   ├── RegisterUser/        # Modified: generate verification token, specific error messages
│   │   ├── RegisterInternalUser/ # NEW: internal registration with verification toggle
│   │   ├── BatchRegisterUsers/  # NEW: CSV batch registration
│   │   ├── VerifyEmail/         # Modified: admin manual verify support
│   │   ├── AdminVerifyEmail/    # NEW: admin-initiated email verification
│   │   ├── ChangePassword/     # Existing: password history from config
│   │   ├── ForcedPasswordChange/ # NEW: forced change for PasswordResetRequired
│   │   └── RegenerateVerificationToken/ # NEW: regenerate token
│   └── Queries/
│       ├── ListUsersByStatus/   # NEW: filter users by AccountStatus
│       ├── GetUserDetails/      # NEW: detailed user state view
│       └── ListIncubatorMembers/ # Existing
├── Auth/
│   ├── Commands/
│   │   ├── LoginUser/           # Modified: PasswordResetRequired detection, config-driven values
│   │   └── LogoutUser/          # Existing
│   └── Queries/
│       └── ValidateSession/     # Modified: actual server-side validation
├── Configuration/
│   ├── Commands/
│   │   └── UpdateConfiguration/ # NEW: update config values
│   └── Queries/
│       └── GetConfiguration/    # NEW: read config by key
└── IntegrationEvents/
    └── UserRegisteredEvent.cs   # NEW: for cross-domain enrollment coordination

# Tenant Domain (Project, Invitation, Enrollment)
Mentoory.Tenant.Domain/
├── Aggregates/
│   ├── Project/
│   │   ├── Project.cs           # Modified: IsPublic flag, EnrollmentVariant
│   │   ├── ProjectParticipant.cs # Existing
│   │   └── ProjectStage.cs     # Existing
│   └── ProjectInvitation/       # NEW: invitation lifecycle
│       └── ProjectInvitation.cs
├── Enums/
│   ├── InvitationStatus.cs      # NEW: Pending, Accepted, Expired
│   └── EnrollmentVariant.cs     # NEW: FullFlow, Bypass
└── ValueObjects/
    └── InvitationToken.cs       # NEW: token hash + expiry

Mentoory.Tenant.Application/
├── Projects/
│   └── Queries/
│       └── ListPublicProjects/  # NEW: public projects in Registration stage
├── Invitations/
│   ├── Commands/
│   │   ├── CreateInvitation/    # NEW
│   │   ├── AcceptInvitation/    # NEW
│   │   ├── AdminAcceptInvitation/ # NEW
│   │   ├── ReissueInvitation/   # NEW
│   │   └── RequestSelfEnrollment/ # NEW
│   └── Queries/
│       ├── ListPendingInvitations/ # NEW
│       └── GetInvitationDetails/   # NEW
├── Enrollment/
│   ├── Commands/
│   │   └── EnrollParticipant/   # Existing: orchestrated with invitation
│   └── IntegrationEventHandlers/
│       └── UserRegisteredEventHandler.cs # NEW: handle cross-domain enrollment
└── BatchEnrollment/
    ├── Commands/
    │   └── ProcessBatchUpload/  # NEW: CSV processing
    └── Models/
        └── BatchRowResult.cs    # NEW: per-row result DTO

# Shared Domain
Mentoory.Shared.Domain/
├── Aggregates/
│   └── Country/                 # NEW: supported countries with ID masks
│       └── Country.cs
└── Constants/
    └── Roles.cs                 # Existing

# Web Layer
Mentoory.Web/
├── Areas/
│   ├── Access/
│   │   ├── Controllers/
│   │   │   ├── RegisterController.cs     # Modified: country dropdown, specific errors
│   │   │   ├── LoginController.cs        # Modified: PasswordResetRequired redirect
│   │   │   ├── VerifyEmailController.cs  # Existing
│   │   │   ├── ChangePasswordController.cs # NEW: forced password change page
│   │   │   └── ForgotPasswordController.cs # Existing
│   │   └── Views/
│   │       ├── Register/                 # Modified: country dropdown, ID mask
│   │       ├── Login/                    # Existing
│   │       └── ChangePassword/           # NEW: forced change UI
│   └── Administration/
│       ├── Controllers/
│       │   ├── UsersController.cs        # Modified: internal registration, user management
│       │   └── BatchUploadController.cs  # NEW: CSV upload and results
│       └── Views/
│           ├── Users/                    # Modified: user details, state management
│           └── BatchUpload/              # NEW: upload form, results table
├── Controllers/
│   ├── HomeController.cs                 # Modified: redirect users without context to public projects
│   └── ContextController.cs             # Existing
├── Infrastructure/
│   └── Authentication/
│       ├── SessionAuthenticationMiddleware.cs  # Modified: actual server-side validation
│       └── PasswordResetRequiredFilter.cs      # NEW: redirect filter
└── wwwroot/js/
    ├── registration.js                   # NEW: country-based ID mask
    └── batch-upload.js                   # NEW: CSV upload handling

# Database
Mentoory.Db/
├── access/Tables/
│   ├── Users.sql                # Existing
│   ├── SystemConfigurations.sql # NEW
│   └── ...
├── tenant/Tables/
│   ├── Projects.sql             # Modified: IsPublic, EnrollmentVariant columns
│   ├── ProjectInvitations.sql   # NEW
│   └── ...
└── shared/Tables/
    └── Countries.sql            # NEW

Mentoory.Db.PostDeployment/
├── 016.SeedCountries.sql        # NEW: Costa Rica + supported countries
├── 017.SeedSystemConfiguration.sql # NEW: default config values
└── 018.AddProjectPublicFlag.sql # NEW: seed IsPublic for existing projects

# Tests
tests/
├── Mentoory.Access.Tests/       # Modified: new handler/domain unit tests
├── Mentoory.Tenant.Tests/       # Modified: invitation/enrollment tests
├── Mentoory.Tests.Integration/  # Modified: new integration test suites
└── Mentoory.Tests.E2E/          # Modified: new E2E scenarios
```

**Structure Decision**: Uses the existing modular monolith layout. Changes span the Access and Tenant bounded contexts with new entities in each. A new SystemConfiguration aggregate in Access holds database-driven settings. Country reference data goes in Shared domain. No new projects are created — all work fits within existing project boundaries.
