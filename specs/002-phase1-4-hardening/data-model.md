# Data Model Reference: Phase 1-4 Hardening

**Branch**: `002-phase1-4-hardening` | **Date**: 2026-04-01

> No new entities are introduced. This document references existing entities affected by hardening corrections.

## Entities Affected by Corrections

### User (Identity Module)

| Field | Type | Purpose in Hardening |
|-------|------|---------------------|
| Id | long | Internal identity; mapped to ClaimTypes.NameIdentifier |
| ExternalId | Guid | External-facing identity for URLs and APIs |
| Email | string | Login credential |
| IsActive | bool | Account status; must be checked during session validation |

**Hardening Notes**:
- Controllers currently read "UserId" custom claim which is never set. Must read `ClaimTypes.NameIdentifier` instead (UF-02).

### RoleAssignment (Authorization Module)

| Field | Type | Purpose in Hardening |
|-------|------|---------------------|
| Id | long | Internal identity |
| ExternalId | Guid | Used in context selection flow |
| UserId | long | FK to User |
| IncubatorId | long | FK to Incubator (0 = global scope for GlobalAdmin) |
| ProjectId | long? | FK to Project (nullable for incubator-scoped roles) |
| Role | string | Role name (GlobalAdmin, IncubatorAdmin, etc.) |
| IsActive | bool | Active assignment flag |

**Hardening Notes**:
- Context selection queries by UserId to find active assignments (GetUserContextsQuery)
- IncubatorId=0 is the sentinel value for GlobalAdmin global scope
- [Authorize(Roles)] must match these role strings exactly

### Session Context (Claims — Runtime Only)

| Claim | Type | Set By | Read By |
|-------|------|--------|---------|
| ClaimTypes.NameIdentifier | long (as string) | LoginController | All controllers needing user identity |
| SessionToken | string | LoginController | SessionAuthenticationMiddleware (stubbed) |
| ActiveRole | string | ContextController | MenuService, all role-based logic |
| ActiveIncubatorId | long (as string) | ContextController | TenantContextMiddleware, controllers |
| ActiveProjectId | long (as string) | ContextController | Controllers in Coordination, Participant areas |
| ClaimTypes.Role | string | ContextController | ASP.NET [Authorize(Roles)] evaluation |

**Hardening Notes**:
- "UserId" claim does NOT exist — code reading it will get null (UF-02)
- ActiveProjectId is optional (not set for incubator-scoped roles or global scope)
- TenantContextMiddleware reads ActiveIncubatorId but is not registered (UF-03)

### FormTemplate (Diagnostic Module)

| Field | Type | Purpose in Hardening |
|-------|------|---------------------|
| Id | long | Internal identity |
| ExternalId | Guid | External-facing; used in URLs |
| Name | string | Template name |
| IncubatorId | long | Owning incubator |

**Hardening Notes**:
- Platform-level entity. Clone operation copies template into a project-scoped ProjectForm.

### ProjectForm (Diagnostic Module)

| Field | Type | Purpose in Hardening |
|-------|------|---------------------|
| Id | long | Internal identity |
| ExternalId | Guid | External-facing; used in URLs |
| ProjectId | long | FK to Project — **must be validated in all queries** |
| IncubatorId | long | FK to Incubator |
| FormTemplateId | long | Source template |

**Hardening Notes**:
- **CRITICAL**: GetProjectFormHandler loads by ExternalId only — no ProjectId filter (UF-01)
- Repository method `GetByExternalIdWithQuestionsAsync` must accept projectId parameter
- All controller access must validate that the form's ProjectId matches ActiveProjectId claim

### DiagnosticResponse (Diagnostic Module)

| Field | Type | Purpose in Hardening |
|-------|------|---------------------|
| Id | long | Internal identity |
| ExternalId | Guid | External-facing; used in URLs |
| ProjectFormId | long | FK to ProjectForm |
| ProjectId | long | FK to Project — **must be validated in all queries** |
| IncubatorId | long | FK to Incubator |
| EntrepreneurUserId | long | FK to User (submitter) |
| EvaluationStage | string | Evaluation stage identifier |

**Unique Constraint**: `(ProjectFormId, EntrepreneurUserId, EvaluationStage)` — one submission per user per form per stage.

**Hardening Notes**:
- **CRITICAL**: GetDiagnosticResponseHandler loads by ExternalId only — no ProjectId filter (UF-01)
- SubmitDiagnosticResponseHandler validates form exists but not project ownership (UF-01)
- CorrectAnswerHandler loads response without project validation (UF-01)

## State Transitions

No state transition changes. Existing states remain:
- **DiagnosticResponse**: Created (on submit) → Corrected (when answers are corrected by mentor/coordinator)
- **FormTemplate / ProjectForm**: No explicit lifecycle states currently implemented

## Seed Data Entities (New — UF-06)

The following deterministic test data must be created in `004.SeedTestData.sql`:

| Entity | Count | Purpose |
|--------|-------|---------|
| Users | 8+ | GlobalAdmin, IncubatorAdmin×2, ProjectCoordinator×2, Mentor, Entrepreneur×2, Sponsor, multi-role user |
| Incubators | 2 | Test cross-incubator isolation |
| Projects | 3+ | 2 in Incubator 1, 1 in Incubator 2 |
| RoleAssignments | 12+ | Map users to roles/incubators/projects; include multi-role assignments |
| FormTemplates | 2+ | Platform-level diagnostic templates |
| ProjectForms | 3+ | Cloned to different projects |
| Questions | 5+ per form | Mix of Text, Numeric, SingleSelect, MultiSelect types |
| AnswerOptionTemplates | 3+ per select question | Predefined answer options |
| DiagnosticResponses | 2+ | Sample submissions for testing |
