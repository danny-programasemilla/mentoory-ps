# Implementation Plan: Phase 1-4 Hardening

**Branch**: `002-phase1-4-hardening` | **Date**: 2026-04-01 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/002-phase1-4-hardening/spec.md`

## Summary

This is a **production-readiness checkpoint** for Phases 1, 3, and 4 of the Mentoory platform. The effort audits, corrects, and hardens existing functionality across authentication routing, context selection/switching, role-based authorization, menu visibility, platform vs administration separation, and the diagnostic module. No new features are introduced. The deliverables include corrected code, deterministic seed data, end-to-end Playwright tests, and manual validation playbooks.

## Technical Context

**Language/Version**: C# / .NET 10.0 (SDK 10.0.0) + ASP.NET Core MVC  
**Primary Dependencies**: MediatR 14.1, FluentValidation 12.1, Mapperly 4.x, EF Core 10.x, MailKit/MimeKit  
**Storage**: SQL Server with SSDT/DACPAC schema management, EF Core 10.x ORM  
**Testing**: xUnit (unit), FluentAssertions, Moq; Playwright (E2E — to be configured)  
**Target Platform**: Linux/Windows server, .NET Aspire 13.2.0 orchestration  
**Project Type**: Web application (modular monolith, Clean Architecture)  
**Performance Goals**: Dashboard load < 3 seconds; diagnostic operations < 2 minutes  
**Constraints**: Zero compiler warnings; Spanish-only UI; claims-based context in auth cookie  
**Scale/Scope**: 6 roles, 5 Areas (Identity, Platform, Administration, Coordination, Participant), ~39 projects in solution

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| # | Gate | Status | Notes |
|---|------|--------|-------|
| I | Clean Architecture Layer Boundaries | PASS | No new cross-layer references introduced; hardening preserves existing structure |
| II | CQRS Pattern Requirements | PASS | Existing commands/queries follow BaseCommandHandler pattern; no new commands introduced |
| III | Domain-Driven Design Constraints | PASS | No domain model changes; existing aggregates/entities remain |
| IV | Integration Events (ADR-001) | PASS | DiagnosticCompletedEvent, AnswerCorrectedEvent already correctly placed |
| V | Zero-Warnings Policy | GATE | Must verify zero warnings after all corrections; build check required before merge |
| VI | DateTime Handling | PASS | No new DateTime usage; existing patterns use ITimeProvider |
| VII | Naming Conventions | PASS | No new artifacts requiring naming; existing names follow conventions |
| VIII | File Organization | PASS | JS in /wwwroot/js/, SQL in correct schemas, one class per file |
| IX | Spanish-First UI | GATE | Must audit all user-facing text in corrected/hardened areas for Spanish compliance |
| X | Role Hierarchy & Session Context | GATE | Primary target of this hardening — audit every [Authorize] and MenuConfiguration |
| XI | SSDT/DACPAC Database Strategy | PASS | Seed data scripts follow PostDeployment conventions; idempotent |

**Gated items (V, IX, X)** are the primary focus of this hardening effort and will be validated as exit criteria.

### Post-Design Re-Check (after Phase 1)

All planned corrections stay within constitution boundaries:
- **Principle I**: Project filtering added to handlers (Application) and repositories (Infrastructure) — proper layer separation preserved.
- **Principle IX**: New UI elements (context switcher, dashboard routing) will use Spanish text.
- **Principle X**: Authorization fixes add missing higher-privilege roles; menu corrections add missing role entries.
- **Principle XI**: New seed script `004.SeedTestData.sql` follows existing idempotent PostDeployment pattern.

No new violations introduced. No complexity justifications needed.

## Project Structure

### Documentation (this feature)

```text
specs/002-phase1-4-hardening/
├── plan.md              # This file
├── research.md          # Phase 0: Code audit findings
├── data-model.md        # Phase 1: Existing entity reference
├── quickstart.md        # Phase 1: Setup and verification guide
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (affected areas)

```text
Mentoory.Web/
├── Areas/
│   ├── Identity/Controllers/LoginController.cs          # Post-login redirect
│   ├── Administration/Controllers/                      # [Authorize] audit
│   │   ├── DashboardController.cs
│   │   ├── ProjectsController.cs
│   │   └── UsersController.cs
│   ├── Coordination/Controllers/                        # [Authorize] + context audit
│   │   ├── DiagnosticsController.cs
│   │   └── AnswerCorrectionController.cs
│   ├── Participant/Controllers/                         # [Authorize] + context audit
│   │   └── DiagnosticController.cs
│   └── Platform/Controllers/                            # GlobalAdmin-only audit
│       ├── IncubatorsController.cs
│       ├── TemplatesController.cs
│       └── UsersController.cs
├── Controllers/
│   ├── HomeController.cs                                # Dashboard routing
│   └── ContextController.cs                             # Context selection/switching
├── Infrastructure/
│   ├── Menu/MenuConfiguration.cs                        # Menu role mapping
│   ├── Menu/MenuService.cs                              # Menu filtering logic
│   ├── Authorization/TenantContextMiddleware.cs         # Context enforcement
│   └── Authentication/SessionAuthenticationMiddleware.cs # Session validation (stubbed)
└── Views/                                               # Spanish UI audit

Mentoory.Authorization.Application/
├── Commands/SetActiveContext/                            # Context selection
└── Queries/GetUserContexts/                             # User contexts

Mentoory.Db.PostDeployment/
├── 002.SeedGlobalAdmin.sql                              # Existing seed
├── 004.SeedTestData.sql                                 # NEW: Deterministic test data
└── Script.PostDeployment.sql                            # Master script

tests/
├── Mentoory.Tests.E2E/                                  # NEW: Playwright tests
└── [existing test projects]                             # Verify/extend coverage
```

**Structure Decision**: No structural changes. All corrections happen within the existing Area-based structure, controller hierarchy, and module boundaries. New files are limited to seed data scripts, E2E test files, and playbook documents.

## Complexity Tracking

> No constitution violations requiring justification. All changes work within existing architecture.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | — | — |
