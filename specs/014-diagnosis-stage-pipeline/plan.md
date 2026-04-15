# Implementation Plan: Configurable Stage Pipeline & Flexible Diagnosis Module

**Branch**: `014-diagnosis-stage-pipeline` | **Date**: 2026-04-14 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/014-diagnosis-stage-pipeline/spec.md`

## Summary

Redesign the project stage pipeline (Tenant domain) and diagnosis module (Diagnostic domain) to support configurable, repeatable stages with flexible form-to-stage assignment and explicit per-assignment question selection. The current hardcoded 7-stage pipeline with binary Initial/Final evaluation is replaced by a 4-type configurable pipeline with a new `StageFormAssignment` aggregate. This is a pre-production breaking refactor across Tenant, Diagnostic, and Access domains with full UI coverage.

## Technical Context

**Language/Version**: C# / .NET 10.0 (SDK 10.0.0-preview)
**Primary Dependencies**: ASP.NET Core MVC, MediatR 14.1, FluentValidation 12.1, Mapperly 4.x, EF Core 10.x
**Storage**: SQL Server with SSDT/DACPAC schema management (no EF migrations)
**Testing**: xUnit, Moq, FluentAssertions, Respawn (EF Core InMemory for unit tests)
**Target Platform**: Linux server via .NET Aspire 13.2.0 orchestration
**Project Type**: Modular monolith web application (ASP.NET Core MVC + Razor Views)
**Performance Goals**: Standard web app expectations — sub-second page loads, no batch processing bottlenecks
**Constraints**: TreatWarningsAsErrors=true, all UI in Spanish, multi-tenant isolation via IncubatorId
**Scale/Scope**: Pre-production, single-digit concurrent users during development. 3 bounded contexts modified (Tenant, Diagnostic, Access)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| # | Principle | Status | Notes |
|---|-----------|--------|-------|
| I | Clean Architecture Layer Boundaries | PASS | Domain/Application/Infrastructure/Web separation maintained. No cross-layer violations in design. |
| II | CQRS Pattern Requirements | PASS | All commands use IBaseRequest/IBaseRequest<TResult>, handlers inherit BaseCommandHandler, FluentValidation on all commands. |
| III | DDD Constraints | PASS | StageFormAssignment is new aggregate root with ExternalId. Collections use private backing fields. Cross-aggregate by ID only. |
| IV | Integration Events (ADR-001) | PASS | New events in originating domain's Application/IntegrationEvents/. Handlers implement INotificationHandler. |
| V | Zero-Warnings Policy | PASS | All removed enums/properties will be cleaned from all references. No orphaned code. |
| VI | DateTime Handling | PASS | All factory methods accept DateTime as parameter. Application handlers use ITimeProvider. |
| VII | Naming Conventions | PASS | Commands: {Verb}{Entity}Command, Queries: {Get|List}{Entity}Query, Handlers: {Command}Handler. |
| VIII | File Organization | PASS | One class per file. JS in /wwwroot/js/. SQL UTF-8 without BOM. |
| IX | Spanish-First UI | PASS | All validation messages, labels, buttons, headings in Spanish. |
| X | Role Hierarchy & Session Context | PASS | All [Authorize] attributes include target role + higher roles. MenuConfiguration includes GlobalAdmin in all groups. |
| XI | SSDT/DACPAC Database Strategy | PASS | New tables via SSDT. Seed data via idempotent PostDeployment scripts. No EF migrations. |

**Gate Result**: ALL PASS — proceed to Phase 0.

## Project Structure

### Documentation (this feature)

```text
specs/014-diagnosis-stage-pipeline/
├── spec.md              # Feature specification
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
└── checklists/
    └── requirements.md  # Spec quality checklist
```

### Source Code (repository root)

```text
# Tenant Domain (modified)
Mentoory.Tenant.Domain/
├── Aggregates/Project/
│   ├── Project.cs                    # MODIFY: configurable pipeline methods
│   └── ProjectStage.cs              # MODIFY: add ExternalId, Position, DisplayName, dates
├── Enums/
│   ├── StageType.cs                 # MODIFY: 7 → 4 values
│   └── StageState.cs               # KEEP: NotStarted, InProgress, Completed

Mentoory.Tenant.Application/
├── Commands/
│   ├── AddProjectStage/             # NEW
│   ├── RemoveProjectStage/          # NEW
│   ├── ReorderProjectStages/        # NEW
│   ├── RenameProjectStage/          # NEW
│   └── AdvanceProjectStage/         # MODIFY (refactor from fixed enum)
├── Queries/
│   ├── GetProjectPipeline/          # NEW
│   └── ListProjectStages/           # NEW
└── IntegrationEvents/
    ├── ProjectStageAddedEvent.cs    # NEW
    └── ProjectStageRemovedEvent.cs  # NEW

Mentoory.Tenant.Infrastructure/
└── Persistence/
    └── TenantDbContext.cs           # MODIFY: update ProjectStage configuration

# Diagnostic Domain (modified + new)
Mentoory.Diagnostic.Domain/
├── Aggregates/
│   ├── FormTemplate/
│   │   └── QuestionTemplate.cs      # MODIFY: remove StageApplicability
│   ├── ProjectForm/
│   │   └── Question.cs              # MODIFY: remove StageApplicability
│   ├── DiagnosticResponse/
│   │   └── DiagnosticResponse.cs    # MODIFY: replace EvaluationStage with StageFormAssignmentId
│   └── StageFormAssignment/         # NEW aggregate
│       ├── StageFormAssignment.cs   # NEW: aggregate root
│       └── AssignedQuestion.cs      # NEW: child entity
├── Enums/
│   ├── EvaluationStage.cs           # DELETE
│   └── StageApplicability.cs        # DELETE
├── ValueObjects/
│   └── ScoreDelta.cs                # NEW
└── Repositories/
    └── IStageFormAssignmentRepository.cs  # NEW

Mentoory.Diagnostic.Application/
├── Commands/
│   ├── AssignFormToStage/           # NEW
│   ├── UpdateStageQuestionSelection/ # NEW
│   ├── RemoveFormFromStage/         # NEW
│   ├── SubmitDiagnosticResponse/    # MODIFY: StageFormAssignmentExternalId
│   ├── CustomizeProjectForm/        # MODIFY: remove StageApplicability
│   ├── CorrectAnswer/              # MINOR: internal ref changes
│   ├── CloneFormTemplate/          # KEEP
│   └── SyncFromTemplate/           # KEEP
├── Queries/
│   ├── GetStageFormAssignment/      # NEW
│   ├── ListStageFormAssignments/    # NEW
│   ├── GetEntrepreneurDiagnosticStatus/ # NEW
│   ├── CompareDiagnosticResults/    # NEW
│   ├── GetDiagnosticTimeline/       # NEW
│   ├── GetDiagnosticResponse/       # MODIFY: include assignment context
│   ├── GetProjectForm/              # MODIFY: remove StageApplicability from DTO
│   ├── GetTopicScoreAggregation/    # KEEP
│   ├── ListProjectForms/           # KEEP
│   └── ListFormTemplates/          # KEEP
└── IntegrationEvents/
    ├── DiagnosticCompletedEvent.cs  # MODIFY: replace EvaluationStage
    └── AnswerCorrectedEvent.cs      # KEEP

Mentoory.Diagnostic.Infrastructure/
└── Persistence/
    ├── DiagnosticDbContext.cs        # MODIFY: add StageFormAssignments, AssignedQuestions DbSets
    └── Repositories/
        └── StageFormAssignmentRepository.cs  # NEW

# Access Domain (minor additions)
Mentoory.Access.Domain/
└── Enums/
    └── Permission.cs                # MODIFY: add 3 new permissions

# Web Layer
Mentoory.Web/
├── Areas/Coordination/
│   ├── Controllers/
│   │   ├── ProjectPipelineController.cs     # NEW
│   │   ├── DiagnosticsController.cs         # MODIFY: add stage config actions
│   │   ├── DiagnosticResultsController.cs   # NEW
│   │   └── AnswerCorrectionController.cs    # MODIFY: remove EvaluationStage refs
│   ├── Models/
│   │   ├── ProjectPipelineViewModels.cs     # NEW
│   │   ├── DiagnosticViewModels.cs          # MODIFY
│   │   └── DiagnosticResultsViewModels.cs   # NEW
│   └── Views/
│       ├── ProjectPipeline/                 # NEW: Index.cshtml
│       ├── Diagnostics/                     # MODIFY + NEW: StageConfig.cshtml, QuestionSelection.cshtml
│       └── DiagnosticResults/               # NEW: Timeline.cshtml, Detail.cshtml, Compare.cshtml
├── Areas/Participant/
│   ├── Controllers/
│   │   └── DiagnosticController.cs          # MODIFY: use StageFormAssignment
│   ├── Models/
│   │   └── DiagnosticViewModels.cs          # MODIFY
│   └── Views/Diagnostic/                    # MODIFY: Index.cshtml, Fill.cshtml, Confirmation.cshtml
└── wwwroot/js/
    ├── pipeline-editor.js                   # NEW
    ├── question-selection.js                # NEW
    └── diagnostic-comparison.js             # NEW

# Database (SSDT)
Mentoory.Db/
├── diagnostic/Tables/
│   ├── StageFormAssignments.sql             # NEW
│   ├── AssignedQuestions.sql                # NEW
│   ├── DiagnosticResponses.sql             # MODIFY: add StageFormAssignmentId, remove EvaluationStage
│   ├── QuestionTemplates.sql               # MODIFY: remove StageApplicability
│   └── Questions.sql                       # MODIFY: remove StageApplicability
└── tenant/Tables/
    └── ProjectStages.sql                   # MODIFY: add ExternalId, Position, DisplayName, dates

Mentoory.Db.PostDeployment/
├── 019.MigrateStageTypes.sql               # NEW: migrate 7→4 stage types
└── 020.SeedNewPermissions.sql              # NEW: seed ManageProjectPipeline, AssignDiagnosticForms, ViewDiagnosticComparison

# Tests
tests/
├── Mentoory.Tenant.Tests/
│   ├── Domain/
│   │   └── ProjectPipelineTests.cs          # NEW
│   └── Handlers/
│       ├── AddProjectStageHandlerTests.cs   # NEW
│       ├── RemoveProjectStageHandlerTests.cs # NEW
│       ├── ReorderProjectStagesHandlerTests.cs # NEW
│       └── AdvanceProjectStageHandlerTests.cs # NEW (refactored)
├── Mentoory.Diagnostic.Tests/
│   ├── Domain/
│   │   ├── StageFormAssignmentTests.cs      # NEW
│   │   ├── DiagnosticResponseTests.cs       # MODIFY
│   │   ├── ProjectFormTests.cs              # MODIFY
│   │   └── FormTemplateTests.cs             # MODIFY
│   ├── Handlers/
│   │   ├── AssignFormToStageHandlerTests.cs # NEW
│   │   ├── UpdateStageQuestionSelectionHandlerTests.cs # NEW
│   │   ├── RemoveFormFromStageHandlerTests.cs # NEW
│   │   ├── SubmitDiagnosticResponseHandlerTests.cs # MODIFY
│   │   ├── CompareDiagnosticResultsHandlerTests.cs # NEW
│   │   └── GetDiagnosticTimelineHandlerTests.cs # NEW
│   └── Integration/
│       ├── DiagnosisFullFlowTests.cs        # NEW: E2E
│       ├── MidProjectDiagnosisTests.cs      # NEW: E2E
│       ├── AnswerCorrectionFlowTests.cs     # NEW: E2E
│       ├── PipelineModificationTests.cs     # NEW: E2E
│       ├── MultiTenantIsolationTests.cs     # NEW: E2E
│       ├── TemplateSyncAssignmentTests.cs   # NEW: E2E
│       └── ConcurrentSubmissionTests.cs     # NEW: E2E
```

**Structure Decision**: Follows existing modular monolith pattern with per-domain project separation (Domain/Application/Infrastructure). New aggregate (StageFormAssignment) lives in Diagnostic domain. Web layer uses existing Area-based structure (Coordination, Participant). All new UI components are Razor views with vanilla JS in wwwroot/js/.

## Complexity Tracking

> No constitution violations detected — this section is empty.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| (none) | — | — |
