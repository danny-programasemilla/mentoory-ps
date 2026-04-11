# Implementation Plan: Fix Batch Upload Project Scope Authorization Bug

**Branch**: `006-fix-batch-upload-scope` | **Date**: 2026-04-08 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/006-fix-batch-upload-scope/spec.md`

## Summary

The batch upload project dropdown exposes all registration-stage projects in the incubator regardless of the user's role-scoped assignment. The `ListRegistrationProjectsHandler` queries projects by incubator without filtering by the user's `RoleAssignment.ProjectId`, and the `BatchUploadController` does not include `ProjectCoordinator` in its `[Authorize]` attribute nor validate project ownership on submission. The fix introduces role-aware project filtering at the query handler layer, adds server-side scope validation on batch upload submission, updates the controller to include `ProjectCoordinator`, and amends the Access & Security Constitution to grant `SCOPED` batch upload access to ProjectCoordinator. A platform-wide audit of all scoped endpoints is included.

## Technical Context

**Language/Version**: C# / .NET 10.0 (SDK 10.0.0)
**Primary Dependencies**: ASP.NET Core MVC, MediatR 14.1, FluentValidation 12.1, Mapperly 4.x, EF Core 10.x, CsvHelper
**Storage**: SQL Server with SSDT/DACPAC schema management, EF Core 10.x ORM
**Testing**: xUnit, Moq, FluentAssertions, Respawn, EF Core InMemory, Testcontainers
**Target Platform**: Linux server (ASP.NET Core 10, .NET Aspire 13.2.0 orchestration)
**Project Type**: Web application (modular monolith)
**Performance Goals**: N/A — security fix, no new latency-sensitive paths
**Constraints**: Zero compiler warnings, Spanish UI text, Clean Architecture layer boundaries
**Scale/Scope**: 19 controllers audited, 2 confirmed issues, 3 risky endpoints

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Pre-Design Gate

| # | Rule | Status | Notes |
|---|------|--------|-------|
| I | Clean Architecture Layer Boundaries | PASS | Query changes stay in Application layer; controller changes in Web layer |
| II | CQRS Pattern Requirements | PASS | Modified query uses `IBaseRequest<TResult>`, handler uses `BaseCommandHandler` |
| III | DDD Constraints | PASS | No new aggregates; RoleAssignment queried read-only |
| V | Zero-Warnings Policy | PASS | No new warnings introduced |
| VI | DateTime Handling | PASS | No DateTime operations added |
| VII | Naming Conventions | PASS | New query/DTO names follow `{Verb}{Entity}` pattern |
| IX | Spanish-First UI | PASS | All user-facing messages in Spanish |
| X | Role Hierarchy & Session Context | **REQUIRES AMENDMENT** | Controller must add `ProjectCoordinator` to `[Authorize]` and include all higher roles. Constitution permission matrix must change from NONE to SCOPED for PC. |
| XI | SSDT/DACPAC Database Strategy | PASS | No schema changes needed |

**Amendment required**: Access & Security Constitution §6.2 permission matrix — Batch Upload Users row must change ProjectCoordinator from `NONE` to `SCOPED` with constraint "PC can upload for assigned projects only".

### Post-Design Gate

| # | Rule | Status |
|---|------|--------|
| X | `[Authorize]` includes target + higher roles | PASS — `[Authorize(Roles = "ProjectCoordinator,IncubatorAdmin,GlobalAdmin")]` |
| X | Menu groups include GlobalAdmin | PASS — no menu changes needed (batch upload is within Administration area) |
| Constitution Rule 5 | Backend enforcement, not UI-only | PASS — scope validation in handler + controller |
| Constitution Rule 11 | Query-layer scope filter | PASS — `ListRegistrationProjectsHandler` will filter by user's project assignments |
| Constitution Rule 12 | Deny by default | PASS — PC sees nothing unless explicitly assigned |

## Project Structure

### Documentation (this feature)

```text
specs/006-fix-batch-upload-scope/
├── plan.md              # This file
├── research.md          # Phase 0 output — platform-wide audit + design decisions
├── data-model.md        # Phase 1 output — affected entities and query contracts
├── contracts/           # Phase 1 output — modified query/handler interfaces
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
# Files to modify
Mentoory.Web/Areas/Administration/Controllers/BatchUploadController.cs
Mentoory.Tenant.Application/Queries/ListRegistrationProjects/ListRegistrationProjectsQuery.cs
Mentoory.Tenant.Application/Queries/ListRegistrationProjects/ListRegistrationProjectsHandler.cs
Mentoory.Tenant.Application/Queries/ListRegistrationProjects/RegistrationProjectsResult.cs
Mentoory.Tenant.Application/Queries/GetProjectByExternalId/GetProjectByExternalIdHandler.cs
Mentoory.Tenant.Application/Queries/GetIncubatorByExternalId/GetIncubatorByExternalIdHandler.cs
Mentoory.Web/Areas/Coordination/Controllers/DiagnosticsController.cs  # Clone template scoping

# Files to create
(none — all changes modify existing files)

# Test files to modify/create
tests/Mentoory.Tenant.Tests/Handlers/ListRegistrationProjectsHandlerTests.cs
tests/Mentoory.Access.Tests/Handlers/CheckPermissionHandlerTests.cs  # add batch upload permission tests

# Governance files to amend
.specify/memory/access-security-constitution.md  # permission matrix update
```

**Structure Decision**: All changes fit within the existing modular monolith structure. No new projects or directories needed.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Constitution amendment (PC: NONE→SCOPED) | Business requirement: ProjectCoordinators need batch upload for their projects | Keeping NONE would remove functionality the business needs |
| Cross-module query (ListRegistrationProjectsQuery needs user/role context) | Handler must filter projects by user's RoleAssignment.ProjectId | Filtering at controller level only violates Constitution Rule 5 (backend enforcement) |
