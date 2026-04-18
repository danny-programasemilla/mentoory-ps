# Brainstorm Seed: Knowledge Module (US3)

**Date:** 2026-04-18
**Status:** spec-created
**Spec:** [specs/016-knowledge-module-core/](../specs/016-knowledge-module-core/)
**Parent roadmap:** [06-platform-roadmap-gap-analysis.md](./06-platform-roadmap-gap-analysis.md) (Phase A, hot stream)

> This is a **seed document** produced during the roadmap brainstorm. It captures everything known about the Knowledge module before a focused `/spex:brainstorm` session is opened. When you start the Knowledge stream, read this, then invoke `/spex:brainstorm` with this file as context.

---

## Problem Framing

The Knowledge module is the **critical-path prerequisite** for the entire mentoring value chain:
- Diagnostic questions link to topics (FR-017)
- Mentoring plans aggregate scores per topic and map to priority ranges defined on the topic (FR-025, FR-025a)
- Mentoring sessions cover subjects within topics
- Assignments are linked to subjects (FR-031)

Today the module is an empty scaffold: three `.csproj` projects with no code, and `knowledge/Schema.sql` containing only `CREATE SCHEMA [knowledge]`. Diagnostic questions already carry a `TopicId` column but it references a non-existent table.

## Current State

- `Mentoory.Knowledge.Domain` / `.Application` / `.Infrastructure` projects exist, empty
- `Mentoory.Knowledge.Tests` exists, empty
- Database schema is stub only
- `Questions.TopicId` is `BIGINT NOT NULL` with no FK target — data-integrity hazard
- `GetTopicScoreAggregationHandler` in Diagnostic already computes per-topic cumulative scores, but there is no Topic entity on the receiving side

## Key FRs involved

- **FR-020** — Hierarchy: Knowledge Structure > Modules > Topics > Subjects > Resources
- **FR-021** — Resource types: video, link, file
- **FR-022** — Topics are the linkage unit between diagnostics and learning
- **FR-023** — Same clone/customize pattern as Diagnostic forms
- **FR-024** — Selecting a diagnostic form implicitly selects the associated knowledge structure
- **FR-017 fixup** — Add FK from `diagnostic.Questions.TopicId` to `knowledge.Topics.Id` once the table exists
- **FR-025a support** — Topics carry configurable priority score ranges (High/Medium/Low min/max), consumed by Mentoring Plan

## Dependencies & Integration Points

**Upstream (blockers):** None. Knowledge can be built standalone.

**Downstream (unblocks):**
- Diagnostic `Questions.TopicId` FK migration
- `Diagnostic form template → associated knowledge structure` binding (FR-024)
- Mentoring Plan priority mapping (FR-025a)
- Mentoring Session `TopicsCovered` / Subject duration
- Assignments linked to subjects

**Integration touch points:**
1. `FormTemplate` needs a `DefaultKnowledgeStructureId` column (FR-024)
2. `ProjectForm.CloneFromTemplate` must also clone the associated knowledge structure (or link by reference — design choice, see open questions)
3. Mentoring Plan's priority-mapping query reads from `knowledge.Topics.HighPriorityMinScore/MaxScore/...`

## Scope for the first spec

**In scope:**
- Domain aggregates: `KnowledgeStructureTemplate`, `KnowledgeStructure` (project-level clone), `Module`, `Topic`, `Subject`, `Resource`
- Clone pattern mirroring `ProjectForm.CloneFromTemplate` (Disconnected / PartialSync modes)
- Topic priority-range fields (High/Medium/Low min+max scores, NotApplicable range implied by gaps)
- CRUD commands + queries
- Coordinator UI in `Mentoory.Web/Areas/Coordination/Controllers/KnowledgeController.cs`
- DB schema + post-deployment seeds for sample global templates
- `FormTemplate.DefaultKnowledgeStructureId` column + migration path
- Integration event when a topic's priority ranges change (informational for Mentoring Plan)

**Out of scope (first spec):**
- Resource file storage backend (use a simple URL / blob reference for now)
- Topic versioning / history
- Recommendation algorithm beyond topic-linkage

## Known Architecture Constraints (from CLAUDE.md / constitution)

- Clean Architecture: Domain (no deps), Application (MediatR + Mapperly), Infrastructure (EF Core 10.x)
- All external entities need `ExternalId` (Guid); routes use ExternalId
- Commands: `IBaseRequest` / `IBaseRequest<TResult>`; handlers: `BaseCommandHandler<T>`; FluentValidation for input
- Forbidden: AutoMapper, Dapper for primary access
- Test coverage expectation: unit tests for domain logic + handler tests + ≥1 integration test
- UI in Spanish; code + docs in English

## Decided During Roadmap Review

- **Topic identity in Question:** Diagnostic questions on a `ProjectForm` reference the **cloned Topic** (per-project); questions on a `FormTemplate` reference the **template Topic**. This means `Question.TopicId` resolution depends on whether the Question lives under a template or a project clone, and the clone operation must rewrite `TopicId` pointers to the newly-cloned topics.

## Open Design Questions (for the focused brainstorm)

1. **Clone depth:** When cloning a knowledge structure template, do we copy the full tree (Modules → Topics → Subjects → Resources) or reference-and-override? Diagnostic chose deep copy with `SourceTemplateId` for partial sync. Consistency argues for deep copy here too.
2. **Partial sync semantics:** For Knowledge, what does "partial sync" mean? Add new modules/topics/subjects/resources but don't touch local modifications? At which hierarchy level does the sync operate?
3. **Priority ranges at clone time:** Do cloned topics inherit the template's ranges, or should the coordinator be forced to review/adjust per project? Spec implies configurable per topic; keep them editable post-clone.
4. **Resource URL vs blob:** For the first spec, are all resources URLs only, or do we allow uploaded files? Uploaded files imply blob storage decisions.
5. **`FormTemplate.DefaultKnowledgeStructureId` cardinality:** one form template binds to one knowledge structure template (1:1), or can it bind to many? Spec is silent; simplest is 1:1. **Note:** the FK addition to `diagnostic.FormTemplates` lands in the **same SSDT PR** as the Knowledge schema tables — cross-schema changes are atomic per SSDT convention.
6. **Topic reordering within a module:** needed? Likely yes, for UX.
7. **Module "learning route" metaphor:** spec calls modules "learning routes" — does this imply sequencing between modules or are they unordered buckets?

## Success Criteria (from spec 001, scoped to Knowledge)

- SC-001: An Incubator Admin can onboard a project with knowledge structures in under 30 minutes (knowledge component)
- Hierarchical CRUD works correctly (Structure → Module → Topic → Subject → Resource)
- Cloning a global template produces an independent project-level copy
- Modifying the project-level clone does not affect the template

## Suggested Next Steps

1. Open `/spex:brainstorm` with this file as context
2. Resolve open questions 1–6 above
3. Move to `/speckit-specify` for the first Knowledge spec
4. Plan + implement via normal pipeline

## Open Threads

- Clone depth decision (deep vs reference) — **resolved in revisit 2026-04-18**
- Partial sync semantics at module/topic/subject/resource levels — **resolved in revisit 2026-04-18**
- Resource file storage approach (URL only vs blob) — **resolved in revisit 2026-04-18**

---

## Revisit: 2026-04-18

Focused brainstorm opened against this seed. Produced spec [016-knowledge-module-core](../specs/016-knowledge-module-core/).

### Resolved Design Questions

| Seed Q | Decision | Rationale |
|---|---|---|
| Q1 Clone depth | **Deep copy** | Mirrors `ProjectForm.CloneFromTemplate` precedent; deep copy keeps clones independent at creation and simplifies tenancy |
| Q2 PartialSync semantics | **All four levels, append-only** | Matches how curriculum evolves in practice; templates grow through additions at every depth |
| Q2b PartialSync match key | **Stamped source template item ExternalId** | Rename-safe and reorder-safe. New precedent (Diagnostic uses parent+text) but the four-level tree makes the robustness worth the extra column |
| Q3 Priority ranges at clone | Inherited, editable per project | Spec requires per-project configurability |
| Q4 Resource storage v1 | **URL/reference only** | Keeps scope tight; `ResourceType.File` is semantically "URL pointing at a file"; blob storage deferred |
| Q5 `FormTemplate.DefaultKnowledgeStructureTemplateId` cardinality | **1:1 (nullable)** | Simplest; no concrete use case for 1:N |
| Q6 Topic reordering | Yes | Standard UX pattern; mirrors `ProjectForm.ReorderQuestions` |
| Q7 Module "learning route" | **Ordered via `SortOrder`**, no gated sequencing | Display order only; completion/gating is out of scope for v1 |
| Clone mutability | **Full CRUD on clone** | Items added on the clone carry no `SourceTemplateXExternalId`; they are clone-only. Matches the `ProjectForm` precedent |
| Form-clone orchestration | **Auto-cascade with reuse-on-existing** | Coordinator does one thing; if the project already has a clone of the same knowledge template, it is reused (avoids split topic scoring when a project has multiple forms sharing one knowledge tree) |

### Updated Open Questions (captured in spec, deferred to plan phase)

- Topic score normalization — the spec assumes 0–100; verify against `GetTopicScoreAggregationHandler` during `/speckit-plan`.
- Event delivery mechanism — v1 uses in-process MediatR `INotification`; upgrade to outbox (per cross-cutting hardening #10) deferred to when the Mentoring Plan consumer lands.
- PartialSync UX — v1 is apply-and-summarize; per-item diff preview is a future UX polish.
- Template archival visibility — hidden from the clone picker by default with a "Show archived" toggle.

### New Design Decisions Made During Revisit

- **Single transaction for cascade and sync** — the form-clone auto-cascade (FR-K21) and `SyncFromTemplate` (FR-K15) each run inside a single transaction; no partial commit. Made explicit in the spec after review.
- **Authorization role sets** — global-template CRUD requires `"GlobalAdmin"`; project-clone CRUD requires `"ProjectCoordinator,IncubatorAdmin,GlobalAdmin"` (per constitution Principle X, which was initially under-surfaced in NFR-K05 and was corrected in the spec review loop).
- **Menu entries and seed data** — `MenuConfiguration.cs` must carry Knowledge entries with GlobalAdmin inclusion (NFR-K10); PostDeployment seed for one sample global template (FR-K50) was added after the review to ensure smoke testability in fresh environments.

### Spec Review Outcome

One pass through `spex:review-spec`. Initial status was ⚠ NEEDS WORK against the constitution (Principles IV, X, XI). All Important-severity issues were fixed inline; final status ✅ SOUND. See [REVIEW-SPEC.md](../specs/016-knowledge-module-core/REVIEW-SPEC.md) for the full audit trail.

### Open Threads (remaining after revisit)

- **Outbox upgrade for `TopicPriorityRangesChanged`** — deferred, not a blocker until Mentoring Plan consumer is built.
- **Topic score range (0–100 vs raw)** — verification task for `/speckit-plan`.
- **Concurrent-edit semantics on clones** — not addressed in v1; flag if it becomes a real user problem.
