# Brainstorm: Diagnosis Module Redesign with Configurable Stage Pipeline

**Date:** 2026-04-14
**Status:** spec-created
**Spec:** specs/014-diagnosis-stage-pipeline/

## Problem Framing

The existing Diagnosis module hardcodes a binary evaluation model (Initial/Final via `EvaluationStage` enum) and a fixed 7-stage project pipeline. This prevents:
- Multiple diagnosis executions per project (mid-project evaluations)
- Configurable stage pipelines per project
- Flexible form-to-stage assignment with per-question selection
- Meaningful comparison across more than two evaluation points

The platform needs a flexible diagnosis system where projects can have any number of Diagnosis stages, each with independently assigned forms and question selections.

## Approaches Considered

### A: StageFormAssignment Aggregate (Chosen)
- New aggregate root in Diagnostic domain linking ProjectForm to ProjectStage with explicit question selection
- Clean aggregate boundaries — form content and stage assignment are separate concerns
- Each assignment is independent with its own lifecycle
- Pros: Clean DDD boundaries, independent lifecycles, straightforward comparison queries
- Cons: More granular entities, admin configuring a stage touches multiple aggregates

### B: StageDiagnosticConfig Aggregate
- One aggregate per Diagnosis stage encapsulating all form assignments
- Stage-centric configuration
- Pros: Single point of configuration per stage, natural open/close lifecycle
- Cons: Larger aggregate boundary, complex response references (config + sub-index)

### C: Extend ProjectForm with Stage Mappings
- Add stage mappings as children of ProjectForm
- Form-centric view
- Pros: No new aggregates, simpler model
- Cons: Mixes form content with assignment concerns, harder to query "what forms does stage X use?"

## Decision

**Approach A: StageFormAssignment Aggregate** — chosen for clean DDD aggregate boundaries. Form content and stage assignment have independent lifecycles and should be separate aggregates. The admin UX abstracts the granularity by presenting a stage-centric configuration view.

### Key Design Decisions

1. **Stage types simplified to 4**: Registration, Diagnosis, Mentorship, Closure (duplicates allowed)
2. **Default pipeline auto-created**: Reg → Diag → Mentorship → Diag → Closure
3. **Explicit per-assignment question selection**: Admin cherry-picks questions per stage (not tag-based)
4. **Both comparison levels**: Topic-level aggregation + question-level drill-down for shared questions
5. **Breaking refactor**: Pre-production, no backward compatibility needed
6. **Full UI scope**: Stage config, form assignment, diagnosis execution, results comparison
7. **No hard blocking on stage state**: Stage state guides UX, never blocks actions

## Open Threads

- Should the pipeline editor support drag-and-drop, or are up/down arrows sufficient for v1?
- When advancing past a Diagnosis stage with incomplete submissions, should the warning show per-entrepreneur completion stats or just a count?
