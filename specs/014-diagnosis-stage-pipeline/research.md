# Research: Configurable Stage Pipeline & Flexible Diagnosis Module

**Date**: 2026-04-14
**Branch**: `014-diagnosis-stage-pipeline`

## Research Summary

All technical decisions were resolved during the brainstorming phase. No NEEDS CLARIFICATION items remain. This document records the decisions and rationale.

## Decisions

### D1: Stage Type Simplification (7 → 4)

**Decision**: Replace the 7-value StageType enum (Registration, Forms, Analysis, LearningAssignment, Mentoring, FinalEvaluation, Closure) with 4 values (Registration, Diagnosis, Mentorship, Closure).

**Rationale**: With configurable pipelines that allow duplicate stage types, a simplified set covers all use cases. A "standard" project uses Registration → Diagnosis → Mentorship → Diagnosis → Closure. Custom projects can add multiple Diagnosis or Mentorship stages. The old granularity (Forms vs FinalEvaluation, Analysis vs LearningAssignment) is unnecessary when stages are position-based rather than type-based.

**Alternatives Considered**:
- Keep all 7 types: Rejected — duplicate types make the fine-grained distinction meaningless
- Merge to 6 (user's original list): Rejected — user chose simplified set for maximum flexibility with minimum types

### D2: StageFormAssignment as Independent Aggregate

**Decision**: Create `StageFormAssignment` as a new aggregate root in the Diagnostic domain, linking a ProjectForm to a ProjectStage with explicit question selection.

**Rationale**: Form content (ProjectForm) and stage assignment are separate concerns with independent lifecycles. A coordinator may modify question selection without changing the form, and may modify the form without changing which questions are assigned. The aggregate boundary prevents accidental coupling.

**Alternatives Considered**:
- StageDiagnosticConfig (one aggregate per stage): Rejected — larger aggregate boundary, complex response references (config + sub-index)
- Extend ProjectForm with stage mappings: Rejected — mixes form content with assignment concerns, harder to query "what forms does stage X use?"

### D3: Explicit Per-Assignment Question Selection

**Decision**: When assigning a form to a stage, the admin explicitly selects which questions are active. All questions selected by default. No tag-based or group-based filtering.

**Rationale**: Maximum flexibility — the admin has full control over which questions appear at each stage. This replaces the rigid StageApplicability enum (Initial/Final/Both) with a fully dynamic model. The same form can be assigned to two stages with completely different question selections.

**Alternatives Considered**:
- Question tags + stage filter: Rejected by user — less control than explicit selection
- Question groups within forms: Rejected by user — middle ground not needed given explicit selection

### D4: Breaking Refactor Strategy

**Decision**: Pre-production breaking refactor with no backward compatibility. Delete EvaluationStage and StageApplicability enums entirely, restructure DB schema, rewrite affected tests.

**Rationale**: System is pre-production with no live data to migrate. Clean-slate approach avoids technical debt from compatibility shims.

**Alternatives Considered**:
- Preserve existing + extend: Rejected — creates model compromises and conceptual mismatch
- Parallel implementation: Rejected — unnecessary complexity for pre-production system

### D5: Comparison Model (Topic + Question Level)

**Decision**: Support both topic-level score aggregation comparison and question-level answer comparison across diagnosis executions. Shared questions identified by QuestionId.

**Rationale**: Topic-level gives quick overview (dashboard-friendly), question-level gives detailed insight. When different forms are used at different stages, only shared questions (matching QuestionId from the same ProjectForm) are comparable. The ScoreDelta value object captures the change.

**Alternatives Considered**:
- Topic-level only: Rejected by user — insufficient granularity
- Question-level only: Rejected — too granular for quick overview

### D6: Default Pipeline Auto-Creation

**Decision**: When a project is created, auto-generate a 5-stage pipeline: Registration → Diagnosis → Mentorship → Diagnosis → Closure. Admins can then customize.

**Rationale**: Provides sensible defaults without requiring manual setup. Most projects follow this standard flow. Admins who need different configurations can add/remove/reorder after creation.

**Alternatives Considered**:
- Pipeline templates (named presets): Rejected — deferred to future enhancement
- Start blank: Rejected by user — too much manual work for common case

### D7: Cross-Domain Reference Pattern for StageFormAssignment

**Decision**: StageFormAssignment lives in Diagnostic domain and references ProjectStageId (from Tenant domain) by ID only. Integration events notify Diagnostic domain of stage additions/removals.

**Rationale**: Follows constitution principle III (cross-aggregate references use ID only) and principle IV (integration events for cross-domain communication). StageFormAssignment is a Diagnostic domain concept — it configures diagnosis behavior for a stage.

**Alternatives Considered**:
- Shared domain: Rejected — violates modular monolith boundaries
- Tenant domain ownership: Rejected — diagnosis configuration is not a tenancy concern

### D8: No Hard Blocking on Stage State

**Decision**: Stage state (NotStarted/InProgress/Completed) guides UX (visual indicators, warnings) but never blocks system actions. Admins can advance stages, create assignments, and entrepreneurs can submit responses regardless of stage state.

**Rationale**: Rigid stage-based blocking was identified as a product limitation. Informational dates and manual advancement give admins full control without system-imposed constraints.

## Open Items

None — all decisions resolved.
