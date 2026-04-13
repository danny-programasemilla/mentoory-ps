# Implementation Plan: Invitation Domain Decoupling

**Branch**: `010-invitation-domain-decoupling` | **Date**: 2026-04-12 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/010-invitation-domain-decoupling/spec.md`

## Summary

Remove the synchronous cross-domain dependency between Access and Tenant modules by eliminating the `IInvitationTokenValidator` bridge and the `TokenType.Invitation` path. All onboarding authentication unifies on Access-domain `EmailVerificationToken`s. Invitation acceptance remains a Tenant-domain reaction to `UserEmailVerifiedEvent`. A new `InvitationReissuedEvent` enables token regeneration without cross-domain calls.

## Technical Context

**Language/Version**: C# / .NET 10.0 (SDK 10.0.0)
**Primary Dependencies**: ASP.NET Core MVC, MediatR 14.1, FluentValidation 12.1, EF Core 10.x
**Storage**: SQL Server with SSDT/DACPAC schema management
**Testing**: xUnit, Moq, FluentAssertions
**Target Platform**: Linux server (Aspire 13.2.0 orchestration)
**Project Type**: Modular monolith web application
**Constraints**: Zero compiler warnings, Spanish-first UI, Clean Architecture layer boundaries
**Scale/Scope**: Internal refactoring — 3 files deleted, ~10 modified, 2 created, 1 DB column dropped

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Clean Architecture Layer Boundaries | PASS | **Fixes existing violation.** Removes Access→Tenant synchronous dependency. New event follows correct integration event placement. |
| II. CQRS Pattern Requirements | PASS | `SetInitialPasswordCommand` simplified, handler still inherits `BaseCommandHandler`. No new commands. |
| III. Domain-Driven Design Constraints | PASS | **Fixes existing violation.** Removes cross-aggregate dependency. `ProjectInvitation` simplified. Cross-domain uses IDs only via events. |
| IV. Integration Events (ADR-001) | PASS | `InvitationReissuedEvent` placed in `Tenant.Application/IntegrationEvents/` (originating domain). Handler in `Access.Application/IntegrationEvents/`. Only event contracts cross domains. |
| V. Zero-Warnings Policy | PASS | Success criterion SC-005 requires zero warnings. |
| VI. DateTime Handling | PASS | `ITimeProvider` used in new handler. Event carries `OccurredOnUtc` as constructor parameter. |
| VII. Naming Conventions | PASS | `InvitationReissuedEvent`, `InvitationReissuedEventHandler` follow `{Verb}{Entity}Event` / `{Event}Handler` patterns. |
| VIII. File Organization | PASS | One class per file. New files follow existing directory conventions. |
| IX. Spanish-First UI | N/A | No new user-facing text. Internal refactoring only. |
| X. Role Hierarchy & Session Context | N/A | No authorization changes. |
| XI. SSDT/DACPAC Database Strategy | PASS | `TokenHash` column dropped via SSDT table definition edit. No EF migrations. |

**Result: ALL GATES PASS. No violations.**

## Project Structure

### Documentation (this feature)

```text
specs/010-invitation-domain-decoupling/
├── spec.md              # Feature specification
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output (entity changes)
├── contracts/
│   └── internal-contracts.md  # Integration event contracts
├── checklists/
│   └── requirements.md  # Spec quality checklist
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
# Files to DELETE
Mentoory.Access.Application/Commands/SetInitialPassword/TokenType.cs
Mentoory.Shared.Application/Interfaces/IInvitationTokenValidator.cs
Mentoory.Tenant.Infrastructure/Services/InvitationTokenValidator.cs

# Files to MODIFY
Mentoory.Access.Application/Commands/SetInitialPassword/SetInitialPasswordCommand.cs
Mentoory.Access.Application/Commands/SetInitialPassword/SetInitialPasswordCommandHandler.cs
Mentoory.Tenant.Domain/Aggregates/ProjectInvitation/ProjectInvitation.cs
Mentoory.Tenant.Application/Invitations/Commands/CreateInvitation/CreateInvitationHandler.cs
Mentoory.Tenant.Application/Invitations/Commands/ReissueInvitation/ReissueInvitationHandler.cs
Mentoory.Tenant.Infrastructure/DependencyInjection.cs
Mentoory.Tenant.Infrastructure/Persistence/TenantDbContext.cs
Mentoory.Web/Areas/Access/Controllers/OnboardingController.cs
Mentoory.Web/Areas/Access/Views/Onboarding/AcceptInvitation.cshtml
Mentoory.Db/tenant/Tables/ProjectInvitations.sql

# Files to CREATE
Mentoory.Tenant.Application/IntegrationEvents/InvitationReissuedEvent.cs
Mentoory.Access.Application/IntegrationEvents/InvitationReissuedEventHandler.cs
```

**Structure Decision**: Existing modular monolith structure. All changes follow established directory conventions per module (Access, Tenant, Shared, Web, Db).

## Complexity Tracking

> No violations found. Table intentionally left empty.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| (none)    | —          | —                                   |
