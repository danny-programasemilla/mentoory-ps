# Review Brief: Knowledge Module Core

**Spec:** specs/016-knowledge-module-core/spec.md
**Generated:** 2026-04-18

> Reviewer's guide to scope and key decisions. See full spec for details.

---

## Feature Overview

The Knowledge module is today an empty scaffold; `knowledge/Schema.sql` contains only a `CREATE SCHEMA` statement, and Diagnostic `Questions.TopicId` has no FK target. This spec delivers the minimum-viable Knowledge domain — a four-level hierarchy (KnowledgeStructure → Module → Topic → Subject → Resource) with a template-and-project-clone pattern mirroring the shipped `ProjectForm.CloneFromTemplate`. It also closes the dangling-FK hazard by cascading `Question.TopicId` rewrites during form-template cloning. This unblocks every downstream mentoring capability (Plan priority mapping, Session coverage, Assignments) that depends on `Topic` being a first-class persisted entity.

## Scope Boundaries

- **In scope:** Full domain model for templates and project clones; topic priority ranges (High/Medium/Low on a 0–100 scale); deep-copy clone with all four-level `SourceTemplateXExternalId` stamps; `PartialSync` that appends new template-added items at all levels; auto-cascading form clone (new `FormTemplate.DefaultKnowledgeStructureTemplateId` + `Questions.TopicId` FK); coordinator UI; single SSDT PR for cross-schema DB changes; PostDeployment seed for one sample template; `TopicPriorityRangesChanged` in-process `INotification`.
- **Out of scope:** Resource file upload / blob storage (URLs only); topic versioning; recommendation algorithms; gated module sequencing; knowledge-structure import/export; the consumer of `TopicPriorityRangesChanged` (lands with Mentoring Plan).
- **Why these boundaries:** Scope is sized to unblock Mentoring Plan without taking on adjacent concerns. File upload and versioning each deserve their own specs. Holding the event emit in scope (and its consumer out) lets Mentoring Plan land without reshipping this module.

## Critical Decisions

### Clone pattern mirrors Diagnostic's `ProjectForm`
- **Choice:** Deep copy with `SourceTemplateId`/`SourceTemplateVersion` on the root, typed `SourceTemplateXExternalId` stamps on every cloned node, `SyncMode` enum (`Disconnected` | `PartialSync`).
- **Trade-off:** Duplicated storage vs. independence-at-clone-time and simple reasoning about "what is mine vs. what came from the template".
- **Feedback:** Confirm this is the right precedent to mirror for a hierarchical (not flat) structure.

### PartialSync matches by stamped template ExternalId, not by (parent + name)
- **Choice:** Each cloned item stamps its source template item's `ExternalId`; sync decides "already here?" by checking that stamp.
- **Trade-off:** New precedent for the codebase (Diagnostic uses TopicId+text match). Robust under rename; stable under reordering. Cost: one new nullable Guid column per cloneable entity.
- **Feedback:** Are we OK diverging from the Diagnostic match precedent here?

### Auto-cascade on form clone, with reuse-on-existing
- **Choice:** `CloneFormTemplateHandler` auto-clones the bound knowledge template when needed, or reuses the project's existing clone of the same source. Inside a single transaction.
- **Trade-off:** One coordinator action does more work; guarantees `Question.TopicId` consistency. Rejected explicit-two-step because it creates an ordering foot-gun.
- **Feedback:** Comfortable with coordinator actions that span two modules behind one command?

### `TopicPriorityRangesChanged` as in-process MediatR `INotification` (v1)
- **Choice:** Event emitted via MediatR in-process; NOT via the outbox pattern ratified during cross-cutting hardening (#10).
- **Trade-off:** Simpler v1; the Mentoring Plan consumer doesn't exist yet. Can upgrade to outbox later without domain changes.
- **Feedback:** Push back if this should already go through the outbox.

## Areas of Potential Disagreement

### Full CRUD on project clones (not "overrides only")
- **Decision:** Coordinator can add brand-new items on the clone that never existed in the template; such items carry no `SourceTemplateXExternalId`.
- **Why this might be controversial:** Some teams prefer clones as pure overrides, arguing that project-specific additions should be in separate "extension" structures for cleaner template lineage.
- **Alternative view:** Restrict clones to edit-existing + delete; no net-new items.
- **Seeking input on:** Whether the added flexibility will blur "template coverage" metrics downstream (e.g., a dashboard that wants to report "% of template completed per project" gets fuzzier).

### Hard-delete of templates is blocked; archival only
- **Decision:** Delete of a `KnowledgeStructureTemplate` with any clone is blocked; `IsArchived` is the only removal path. Archived templates still resolve `SourceTemplateId` but disappear from the picker by default.
- **Why this might be controversial:** A strict-cleanup school prefers hard delete with CASCADE or orphan-null behavior for clones.
- **Alternative view:** Hard delete with `SourceTemplateId → NULL` on clones; treat clones as fully independent after origin disappears.
- **Seeking input on:** Whether losing the historical lineage is a bigger cost than the operational tidiness of hard delete.

### PartialSync is append-only across all four levels
- **Decision:** Sync never modifies or removes existing clone items, only appends missing ones. Applies equally at Module/Topic/Subject/Resource levels.
- **Why this might be controversial:** Some reviewers may prefer more granular control: "update metadata but not structure," or level-scoped sync ("sync only new modules, not subjects/resources").
- **Alternative view:** Configurable per-operation level toggle, or a "soft-update" mode that reconciles name changes on non-renamed cloned items.
- **Seeking input on:** Whether append-only is enough for the long-term evolution of templates or if we'll regret the simplicity.

## Naming Decisions

| Item | Name | Context |
|---|---|---|
| Root template aggregate | `KnowledgeStructureTemplate` | Mirrors `FormTemplate` naming |
| Project clone root | `KnowledgeStructure` | Drops the "Template" suffix for clones (mirrors `ProjectForm`) |
| Hierarchy levels | `Module` → `Topic` → `Subject` → `Resource` | Taken directly from FR-020 in spec 001 |
| Sync mode enum | `Disconnected` / `PartialSync` | Exact match to Diagnostic's enum |
| Stamped source field | `SourceTemplate{Level}ExternalId` (nullable) | Typed per level; null = clone-only item |
| Cross-module binding | `FormTemplate.DefaultKnowledgeStructureTemplateExternalId` | 1:1, nullable |
| Domain event | `TopicPriorityRangesChanged` | In `Mentoory.Knowledge.Application/IntegrationEvents/` |
| Resource type enum | `ResourceType { Video, Link, File }` | `File` = URL to externally-hosted file in v1 |
| Priority bands | `High` / `Medium` / `Low` / `NotApplicable` | `NotApplicable` implied by gap, not a stored enum value |

## Open Questions

- [ ] Is the 0–100 score normalization assumption actually true? Verify against the existing `GetTopicScoreAggregationHandler` during `/speckit-plan` (captured as an Assumption in the spec, flagged for plan-phase verification).
- [ ] Concurrent-edit semantics — two coordinators editing the same clone at once — are not addressed in v1. If this is a real risk for the audience (it usually is not for coordinator-tier users), raise it before implementation.

## Risk Areas

| Risk | Impact | Mitigation |
|---|---|---|
| Existing seed data has `Questions.TopicId` values referencing non-existent topics → adding the FK breaks publish | High | PostDeployment seed (FR-K50) and test-data audit during `/speckit-plan`; FK and seed must land atomically |
| Auto-cascade inside `CloneFormTemplateHandler` expands its scope of responsibility and adds a cross-module repository dependency | Medium | Dependency is interface-shaped (`IKnowledgeStructureRepository`); limit to this single orchestration |
| In-process event delivery (no outbox) could be insufficient when Mentoring Plan consumer arrives if that consumer needs cross-process durability | Medium | Publisher can be upgraded without changing this module's domain; flagged in Assumptions |
| Deep-clone at onboarding time could be slow for very large templates | Low | v1 templates are modest; revisit only if a real performance issue appears |

---

*Share with reviewers before implementation.*
