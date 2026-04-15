# Quickstart: Configurable Stage Pipeline & Flexible Diagnosis Module

**Date**: 2026-04-14
**Branch**: `014-diagnosis-stage-pipeline`

## Prerequisites

- .NET 10.0 SDK installed
- SQL Server instance running (via Aspire or local)
- Repository cloned and on branch `014-diagnosis-stage-pipeline`

## Build & Run

```bash
# Build entire solution
dotnet build

# Run with Aspire (includes SQL Server, observability)
dotnet run --project Mentoory.Aspire.AppHost

# Run web only (requires external SQL Server)
dotnet run --project Mentoory.Web
```

## Run Tests

```bash
# All tests
dotnet test

# Tenant domain tests only
dotnet test --filter "FullyQualifiedName~Mentoory.Tenant.Tests"

# Diagnostic domain tests only
dotnet test --filter "FullyQualifiedName~Mentoory.Diagnostic.Tests"

# E2E integration tests only
dotnet test --filter "FullyQualifiedName~Integration"
```

## Database

```bash
# Build DACPAC and publish to SQL Server
cd Mentoory.Db && ./publish-mentoorydb.sh
```

PostDeployment scripts run automatically during publish:
- `019.MigrateStageTypes.sql` — migrates stage types and adds positions
- `020.SeedNewPermissions.sql` — seeds new permission values

## Key Verification Steps

### 1. Pipeline Configuration
1. Login as GlobalAdmin or ProjectCoordinator
2. Navigate to a project → "Etapas" tab
3. Verify default 5-stage pipeline appears
4. Add a Diagnosis stage, reorder, rename
5. Advance through stages

### 2. Form Assignment
1. From pipeline editor, click "Configurar diagnóstico" on a Diagnosis stage
2. Click "Agregar formulario" and select a ProjectForm
3. Open question selection, deselect some questions, save
4. Assign same form to another Diagnosis stage with different selections

### 3. Diagnosis Execution
1. Login as Entrepreneur
2. Navigate to "Diagnósticos"
3. Open a pending form, fill out questions, submit
4. Verify confirmation page and status update

### 4. Results Comparison
1. Login as Coordinator
2. Navigate to "Resultados diagnósticos"
3. Select an entrepreneur → view timeline
4. Select 2 executions → compare
5. Check topic-level deltas and question-level drill-down

## Implementation Order

The recommended implementation order follows dependency chain:

1. **Tenant domain refactoring** — StageType enum, ProjectStage entity, Project aggregate
2. **Diagnostic domain cleanup** — Remove EvaluationStage, StageApplicability
3. **StageFormAssignment aggregate** — New aggregate with AssignedQuestion
4. **DiagnosticResponse refactoring** — Link to StageFormAssignment
5. **SSDT schema changes** — New tables, altered columns, PostDeployment scripts
6. **Application layer commands/queries** — All new and modified handlers
7. **Web layer — Pipeline config UI** — Controllers, views, JS
8. **Web layer — Stage form assignment UI** — Config and question selection
9. **Web layer — Entrepreneur diagnosis execution** — Modified questionnaire flow
10. **Web layer — Results and comparison** — Timeline, detail, compare views
11. **E2E integration tests** — Full flow coverage
