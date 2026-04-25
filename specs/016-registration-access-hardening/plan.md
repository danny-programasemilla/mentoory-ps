# Implementation Plan: Registration & Access Hardening

**Branch**: `016-registration-access-hardening` | **Date**: 2026-04-18 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/016-registration-access-hardening/spec.md`

## Summary

Close the public-registration enumeration oracle, split admin enrollment onto its own specific-feedback code path, and add a password-identifying-data validator rule. All three items touch the same account-creation surface and ship together.

**Technical approach**: Split the current shared `RegisterUserCommand` into two distinct Application-layer commands (`RegisterUserCommand` for the public path and `AdminEnrollUserCommand` for the admin path) that delegate shared provisioning logic to an internal `IUserProvisioningService`. The public handler masks uniqueness conflicts into a success-shaped response and logs the real outcome; the admin handler surfaces field-attributed errors to the authenticated admin. Introduce a reusable `PasswordIdentifyingDataRule` validator helper in `Mentoory.Access.Application` and wire it into both validators so every password-setting surface (present and future) picks it up automatically. No database schema changes; no new domain entities.

## Technical Context

**Language/Version**: C# / .NET 10.0 (SDK 10.0.0)
**Primary Dependencies**: ASP.NET Core MVC, MediatR 14.1, FluentValidation 12.1, Mapperly 4.x, EF Core 10.x, Microsoft.Extensions.Logging (source-generated `[LoggerMessage]`)
**Storage**: SQL Server (no schema changes — all changes are behavioural at the Application and Web layers)
**Testing**: xUnit, Moq, FluentAssertions, EF Core InMemory for unit tests; Respawn + SQL Server for integration tests
**Target Platform**: Linux/Windows server hosting ASP.NET Core 10
**Project Type**: Web application (modular monolith; Clean Architecture with Access domain module)
**Performance Goals**: No p95 regression on public registration (current ~200 ms); masked-conflict path MUST visibly match the fresh-creation path (HTTP status, headers, body); timing-channel parity is best-effort and not a gating target
**Constraints**:
- Public endpoint response indistinguishability is measured at the visible HTTP layer (status, body, rendering-relevant headers), not at TLS-packet timing
- `registration` rate-limit policy stays attached (defence-in-depth)
- Anti-forgery token stays in force on both endpoints
- All user-facing messages in Spanish; code and docs in English
**Scale/Scope**: Two controller actions modified (one split), one new command/handler/validator triad, one shared validator helper, shared provisioning service, ~4 new unit test files and 2 integration test files. ~15 files touched total.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| # | Principle | Gate | Verdict |
|---|-----------|------|---------|
| I | Clean Architecture Layer Boundaries | Changes sit in Web (controllers) and Application (commands, handlers, validators, service). No framework deps leak into Domain. No direct EF/HttpContext access in Application — client IP and correlation ID pass through as command metadata. | ✅ Pass |
| II | CQRS Pattern Requirements | Both commands are `IBaseRequest`; both handlers inherit `BaseCommandHandler<T>`; FluentValidation validators cover every command. No nested `Result<T>`. | ✅ Pass |
| III | Domain-Driven Design Constraints | No new aggregates or value objects; the shared provisioning service orchestrates existing `User.Register(...)` factory and repository. `ExternalId` is unchanged (not exposed on these endpoints). | ✅ Pass |
| IV | Integration Events (ADR-001) | No new integration events introduced. (Future `UserRegisteredIntegrationEvent` is orthogonal and out of scope.) | ✅ Pass |
| V | Zero-Warnings Policy | Changes compile with `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` in CI; nullable annotations applied. | ✅ Pass |
| VI | DateTime Handling | `ITimeProvider.UtcNow` already injected in the public handler; new admin handler and provisioning service use the same abstraction. | ✅ Pass |
| VII | Naming Conventions | `AdminEnrollUserCommand`, `AdminEnrollUserHandler`, `AdminEnrollUserValidator`, `IUserProvisioningService`, `PasswordIdentifyingDataRule` follow the convention table. | ✅ Pass |
| VIII | File Organization | One class per file; no JS added; no SSDT changes; no files relocated. | ✅ Pass |
| IX | Spanish-First UI | Generic failure (`No fue posible completar el registro. Revise los datos e intente nuevamente.`), masked confirmation (existing `Revise su correo electrónico...`), admin-specific messages (existing national-ID / email copy), and the identifying-data password rejection (`La contraseña no puede contener su correo electrónico ni su número de identificación.`) are Spanish. Code/docs English. | ✅ Pass |
| X | Role Hierarchy & Session Context | Admin endpoint retains `[Authorize(Roles = "IncubatorAdmin,GlobalAdmin")]`; no menu changes; no new roles. | ✅ Pass |
| XI | SSDT/DACPAC Database Strategy | No schema changes; no PostDeployment scripts. | ✅ Pass |

**Result**: All gates pass. No Complexity Tracking entries required.

## Project Structure

### Documentation (this feature)

```text
specs/016-registration-access-hardening/
├── plan.md              # This file
├── spec.md              # Feature specification (existing)
├── research.md          # Phase 0 output — design decisions
├── data-model.md        # Phase 1 output — command/validator/service contracts
├── quickstart.md        # Phase 1 output — manual verification script
└── contracts/           # Phase 1 output — HTTP endpoint contracts
    ├── public-register.http.md
    └── admin-enroll.http.md
```

### Source Code (repository root)

```text
# Web layer — controller changes (two existing actions modified)
Mentoory.Web/
└── Areas/
    ├── Access/
    │   ├── Controllers/
    │   │   └── RegisterController.cs                    # MODIFY — dispatch public command; mask conflict
    │   ├── Models/
    │   │   └── RegisterViewModel.cs                     # unchanged
    │   └── Views/Register/
    │       ├── Index.cshtml                             # MODIFY — single generic failure banner
    │       └── Success.cshtml                           # MODIFY — add "already have an account? reset password" hint
    └── Administration/
        ├── Controllers/
        │   └── UsersController.cs                       # MODIFY — dispatch AdminEnrollUserCommand
        ├── Models/
        │   └── EnrollUserViewModel.cs                   # unchanged (or minor — see data-model)
        └── Views/Users/
            └── Enroll.cshtml                            # unchanged

# Application layer — split commands, shared provisioning service, shared validator helper
Mentoory.Access.Application/
├── Commands/
│   ├── RegisterUser/                                    # MODIFY — public-path command
│   │   ├── RegisterUserCommand.cs                       # MODIFY — add CorrelationId, ClientIpAddress metadata
│   │   ├── RegisterUserHandler.cs                       # MODIFY — mask conflict, structured outcome log
│   │   └── RegisterUserValidator.cs                     # MODIFY — invoke PasswordIdentifyingDataRule
│   └── AdminEnrollUser/                                 # NEW — admin-path command
│       ├── AdminEnrollUserCommand.cs                    # NEW
│       ├── AdminEnrollUserHandler.cs                    # NEW — specific field-attributed failure
│       └── AdminEnrollUserValidator.cs                  # NEW — invokes PasswordIdentifyingDataRule
├── Services/
│   └── IUserProvisioningService.cs                      # NEW — shared uniqueness + create + token logic
├── Infrastructure/                                      # (existing folder for Application-layer infra)
│   └── UserProvisioningService.cs                       # NEW — implementation
└── Validation/
    └── PasswordIdentifyingDataRule.cs                   # NEW — reusable FluentValidation helper

# Domain layer — NO changes
Mentoory.Access.Domain/
└── (unchanged — User aggregate, HashedPassword value object, repositories all unchanged)

# Tests
tests/Mentoory.Access.Application.Tests/
├── Commands/
│   ├── RegisterUser/
│   │   └── RegisterUserHandlerTests.cs                  # MODIFY/EXTEND — conflict-masking cases
│   └── AdminEnrollUser/
│       └── AdminEnrollUserHandlerTests.cs               # NEW — specific failure cases
└── Validation/
    └── PasswordIdentifyingDataRuleTests.cs              # NEW — threshold + normalisation cases

tests/Mentoory.Web.Tests/
└── Areas/
    ├── Access/
    │   └── RegisterControllerTests.cs                   # NEW — visible-response equality harness
    └── Administration/
        └── UsersControllerEnrollTests.cs                # NEW — specific-feedback harness
```

**Structure Decision**: The existing modular monolith layout is preserved. All changes sit inside the Access module (`Mentoory.Access.Application` and the two controllers under `Mentoory.Web`). The split-command design satisfies FR-016-07 ("distinct code path") by giving the admin path its own command, handler, and validator while still sharing the lower-level uniqueness-and-create logic through `IUserProvisioningService`. The shared password rule is placed on `Mentoory.Access.Application/Validation/` so any future password-setting command picks it up by adding a single line to its validator (FR-016-14).

## Complexity Tracking

*Not applicable — no Constitution Check violations to justify.*
