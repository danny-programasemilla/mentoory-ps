# Review Brief: Configurable Stage Pipeline & Flexible Diagnosis Module

**Spec:** specs/014-diagnosis-stage-pipeline/spec.md
**Generated:** 2026-04-14

> Reviewer's guide to scope and key decisions. See full spec for details.

---

## Feature Overview

Replaces the hardcoded 7-stage project pipeline and binary Initial/Final diagnosis model with a configurable stage system supporting 4 stage types (Registration, Diagnosis, Mentorship, Closure) that can be duplicated and reordered per project. Introduces `StageFormAssignment` — a new aggregate that links diagnostic forms to specific stages with explicit per-question selection. Enables multi-point diagnosis comparison across any number of evaluation moments, with both topic-level score deltas and question-level answer drill-down.

## Scope Boundaries

- **In scope:** Stage pipeline redesign (Tenant domain), StageFormAssignment aggregate (Diagnostic domain), diagnosis execution refactoring, results comparison UI, answer correction adaptation, full admin/coordinator/entrepreneur UI, E2E tests, SSDT schema changes, 3 new permissions in Access domain
- **Out of scope:** Mentorship Plan Generation, Knowledge Structure integration, pipeline templates (presets), export/PDF, notifications, bulk assignment
- **Why these boundaries:** This spec focuses on the foundation — configurable stages and flexible diagnosis. Downstream consumers (mentorship plans, notifications) depend on this model but are separate features with their own specs.

## Critical Decisions

### Stage Type Simplification (7 → 4)
- **Choice:** Registration, Diagnosis, Mentorship, Closure — with duplicates allowed per project
- **Trade-off:** Lost granularity (Forms vs FinalEvaluation, Analysis vs LearningAssignment) in exchange for maximum flexibility through pipeline composition
- **Feedback:** Does the simplified set cover all your anticipated project configurations?

### StageFormAssignment as Independent Aggregate
- **Choice:** New aggregate root in Diagnostic domain referencing ProjectStageId by ID
- **Trade-off:** More granular entities (one aggregate per form-stage link) vs. simpler but larger aggregates
- **Feedback:** Is the aggregate boundary clean enough, or should stage configuration be consolidated?

### Explicit Per-Assignment Question Selection
- **Choice:** Admin cherry-picks individual questions per stage assignment (no tags, no groups)
- **Trade-off:** Maximum flexibility but more admin clicks vs. tag-based filtering which is faster but less precise
- **Feedback:** Will the checkbox-based question selection scale for forms with 50+ questions?

## Areas of Potential Disagreement

> Decisions or approaches where reasonable reviewers might push back.

### No Hard Blocking on Stage State
- **Decision:** Stage state (NotStarted/InProgress/Completed) is purely informational — never blocks actions
- **Why this might be controversial:** Some stakeholders may expect that entrepreneurs can only fill diagnoses when the stage is "active"
- **Alternative view:** Add optional "strict mode" per project that enforces stage-based access
- **Seeking input on:** Is the fully permissive model correct, or should there be an opt-in restriction?

### Breaking Refactor (No Backward Compatibility)
- **Decision:** Delete EvaluationStage and StageApplicability enums entirely, restructure DB schema
- **Why this might be controversial:** If any test data or demo environments exist, they will break
- **Alternative view:** Keep old enums as deprecated, migrate gradually
- **Seeking input on:** Confirm no existing environments depend on current schema

### Pipeline Constraints (Must Start with Registration, End with Closure)
- **Decision:** Enforced at domain level — cannot create a pipeline without these bookends
- **Why this might be controversial:** Some project types might not need Registration or Closure stages
- **Alternative view:** Make constraints configurable or remove them entirely
- **Seeking input on:** Are there known project types that don't follow this pattern?

## Naming Decisions

| Item | Name | Context |
|------|------|---------|
| New aggregate | StageFormAssignment | Links a ProjectForm to a ProjectStage with question selection |
| Child entity | AssignedQuestion | Records which questions are active for an assignment |
| Value object | ScoreDelta | Comparison metric between two diagnosis executions |
| Stage types | Registration, Diagnosis, Mentorship, Closure | Simplified from 7 to 4 types |
| New permissions | ManageProjectPipeline, AssignDiagnosticForms, ViewDiagnosticComparison | 3 new Access domain permissions |

## Open Questions

- [ ] Should the pipeline editor support drag-and-drop, or are up/down arrows sufficient for v1?
- [ ] When advancing past a Diagnosis stage with incomplete submissions, should the warning show per-entrepreneur completion stats or just a count?

## Risk Areas

| Risk | Impact | Mitigation |
|------|--------|------------|
| StageType enum value change (7→4) breaks existing seed data | High | PostDeployment migration script 019 handles value mapping |
| Removing StageApplicability from 2 entities + all consumers | High | Pre-production system, full test rewrite covers all paths |
| Cross-domain reference (Diagnostic → Tenant by ID) creates implicit coupling | Med | Integration events for stage add/remove keep Diagnostic domain informed |
| Question selection UI complexity for large forms (50+ questions) | Med | Topic-based grouping with select-all toggles mitigates; monitor in QA |
| Comparison queries with multiple joins (assignments → responses → questions) | Med | Indexes on StageFormAssignmentId, QuestionId ensure query performance |

---
*Share with reviewers before implementation.*
