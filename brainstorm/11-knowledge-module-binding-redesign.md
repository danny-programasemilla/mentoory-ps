# Brainstorm: Knowledge Module — Project-owned KS binding redesign

**Date:** 2026-04-19
**Status:** spec-created
**Spec:** [specs/016-knowledge-module-core/](../specs/016-knowledge-module-core/) (amended in-place; see [AMENDMENT-PROJECT-KS-BINDING.md](../specs/016-knowledge-module-core/AMENDMENT-PROJECT-KS-BINDING.md))
**Related:** [07-knowledge-module](./07-knowledge-module.md) (original design) · [07-knowledge-module-feedback](./07-knowledge-module-feedback.md) (PR-14 review)

## Problem Framing

Mid-PR review surfaced a modeling mistake in spec 016 as shipped on PR-14: the column
`FormTemplate.DefaultKnowledgeStructureTemplateExternalId` implies that every diagnostic
form "owns" a knowledge structure. Because a project can use multiple forms (initial
diagnosis, mid-term, final, impact), the current design could produce **N KnowledgeStructures
per project** — one per distinct form binding. Business reality: a project has **exactly
one** learning path / knowledge tree; multiple forms evaluate against topics in that one
tree. The binding belongs on the project, not on the form.

The user also raised: *"or is this overkill for the feature of when a diagnosis is complete
then define the mentoring path?"* — a useful prompt to pressure-test whether the per-project
clone concept is justified at all, or whether topics + priority ranges could live globally.

## Approaches Considered

### A: Project-owned binding, form binding becomes compatibility metadata (chosen)
- `Project.KnowledgeStructureTemplateExternalId` (required, immutable) + `Project.KnowledgeStructureExternalId` (NOT NULL after creation, immutable).
- `FormTemplate.DefaultKnowledgeStructureTemplateExternalId` kept, semantics shift from "drives cascade" to "advertises compatibility".
- `CloneFormTemplateHandler` simplifies: compatibility check + topic-id rewrite against the project's pre-materialized KS; never creates a KS.
- UNIQUE on `KnowledgeStructures.ProjectId` enforces the 1:1 invariant at the schema level.
- **Pros**: single source of truth; invariant enforced in the DB; handler simpler; clear admin UX (pick the template at project creation); lines up with how projects are already set up.
- **Cons**: project creation gains a required input; admins have to configure at least one KS template before any project can exist.

### B: Keep form binding, enforce single KS per project via DB constraint
- Minimum change: UNIQUE on `KnowledgeStructures.ProjectId` + reject a second form clone bound to a different KS template.
- Project's KS materializes lazily on first form clone.
- **Pros**: smallest code change, no new Project fields.
- **Cons**: implicit "first form wins" is a sneaky side effect; coordinators can't see/choose their KS before doing a diagnostic; creates strange lifecycle (project exists but has no KS until someone triggers a form).

### C: Bind at Mentoring Plan generation time
- Drop form binding; projects don't pre-commit; coordinator picks the KS at plan-generation time.
- **Pros**: zero upfront configuration.
- **Cons**: coordinator does everything at once (pick template, customize thresholds, customize resources, generate plan) — poor UX; priority-range customization can't happen until after a diagnosis is scored.

### D: Hybrid — project binding + auto-derive from form on first use
- `Project.KnowledgeStructureTemplateExternalId` optional; first form clone sets it if null.
- **Pros**: flexibility.
- **Cons**: two code paths (pre-configured vs. auto-derived), twice the tests; doesn't win over A.

### Also explicitly considered and rejected
- **"Drop KS entirely; topics are global"** — would work if priority ranges + resources were the same across all projects. They are not (per user: both are per-project customized). So the clone model stays.

## Decision

Approach **A**. Full design captured in the [amendment doc](../specs/016-knowledge-module-core/AMENDMENT-PROJECT-KS-BINDING.md). Key invariants:

1. 1 `KnowledgeStructure` per project (UNIQUE constraint).
2. `Project.KnowledgeStructureTemplateExternalId` required at creation, immutable after.
3. `Project.KnowledgeStructureExternalId` NOT NULL after creation, populated in the creation transaction.
4. `FormTemplate.DefaultKnowledgeStructureTemplateExternalId` = compatibility metadata.
5. `CloneFormTemplateHandler` does rewrite-only — never provisions KS.

Scope fits within PR #14 (spec 016 is pre-release, no production data, no migration needed).
Refactor work added as **Phase 9** in `tasks.md` (T110-T132).

Project-creation role list broadens to include `ProjectCoordinator` (previously
implicit that only `IncubatorAdmin` / `GlobalAdmin` could create; coordinator now
explicitly included).

## Open Threads

- Whether `CreateProjectCommand` should orchestrate the cross-module clone via a shared
  `DbContextTransaction` across Tenant + Knowledge contexts, or accept a brief window
  where `Project.KnowledgeStructureExternalId` is transiently unset (NULLABLE at the
  schema level, NOT NULL enforced by handler + test). Deferred to `/speckit-plan`.
- Whether the existing `CloneKnowledgeStructureTemplateCommand` (MediatR command used
  from the coordinator UI, now obsolete from that surface) should be kept around for
  the Tenant-side `IKnowledgeStructureProvisioner` implementation, or replaced with a
  direct call into the domain factory. Deferred to implementation.
