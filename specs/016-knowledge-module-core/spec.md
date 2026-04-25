# Feature Specification: Knowledge Module Core

**Feature Branch**: `016-knowledge-module-core`
**Created**: 2026-04-18
**Status**: Draft (amended 2026-04-19)
**Input**: User description: Knowledge module core — hierarchical knowledge content domain (KnowledgeStructure > Module > Topic > Subject > Resource) with template-and-project-clone pattern mirroring Diagnostic's ProjectForm.CloneFromTemplate. Delivers the minimum-viable Knowledge domain that unblocks every downstream mentoring module (Mentoring Plan priority mapping, Session coverage, Assignments).

> ## AMENDMENT 2026-04-19 — Project-owned KS binding
>
> Review flagged that the original design let a project end up with **multiple**
> KnowledgeStructures — one per FormTemplate whose `DefaultKnowledgeStructureTemplateExternalId`
> pointed at a different KS template. The business invariant is the opposite: a project has
> **exactly one** KnowledgeStructure. The binding moves from FormTemplate to Project.
>
> **Invariants enforced post-amendment:**
> 1. Each `Project` has exactly one `KnowledgeStructure` (UNIQUE on `KnowledgeStructures.ProjectId`).
> 2. `Project.KnowledgeStructureTemplateExternalId` is **required** at project creation and **immutable** afterwards.
> 3. `Project.KnowledgeStructureExternalId` is **populated in the same transaction** as project creation (NOT NULL once the project row exists).
> 4. `FormTemplate.DefaultKnowledgeStructureTemplateExternalId` becomes **compatibility metadata** — it says "this form's questions reference topics authored against this KS template"; it does **not** drive KS creation.
> 5. `CloneFormTemplateHandler` **never** creates or mutates a `KnowledgeStructure`. It rewrites `Question.TopicId` against the project's existing KS. If the form's `DefaultKnowledgeStructureTemplateExternalId` is set and does not match the project's `KnowledgeStructureTemplateExternalId`, the clone is rejected with `"Este formulario está diseñado para una estructura de conocimiento diferente a la del proyecto."`
>
> **Role changes:** project creation (which already selects a KS template) is now open to `ProjectCoordinator`, `IncubatorAdmin`, `GlobalAdmin`.
>
> **Scope impact:** US1 (global-admin template CRUD) and US3 (diagnostic cascade) stay; US2 loses the coordinator-facing "Clone from template" flow — the KS is auto-materialized at project creation so coordinators always find it pre-populated. US2's per-project tree editor (priority ranges + resources CRUD) is unchanged. US4 + US5 unchanged.
>
> This amendment is pre-release (no production data), so SSDT + domain + handler changes land in the same PR as the original implementation. See `AMENDMENT-PROJECT-KS-BINDING.md` for full delta.

## Overview

Today `Mentoory.Knowledge` is an empty scaffold: three `.csproj` projects contain no code and `knowledge/Schema.sql` contains only `CREATE SCHEMA [knowledge]`. Diagnostic `Questions.TopicId` is declared `BIGINT NOT NULL` but has no FK target — a latent data-integrity hazard. Every downstream mentoring capability (plan priority mapping, session coverage tracking, assignment linkage) depends on `Topic` being a first-class persisted entity with configurable priority-score ranges.

This spec delivers the minimum-viable Knowledge domain (templates + project clones), topic priority ranges consumed later by Mentoring Plan, and the cross-module integration (`FormTemplate.DefaultKnowledgeStructureTemplateId`, `Questions.TopicId` FK, `CloneFormTemplateHandler` auto-cascade with `TopicId` rewriting) that unblocks the next streams of work.

The design mirrors the already-shipped `ProjectForm.CloneFromTemplate` precedent — deep-copy clones, `SourceTemplateId` / `SourceTemplateVersion` stamps, `SyncMode` enum (`Disconnected` | `PartialSync`) — extended to a four-level tree with stamped source ExternalIds on every cloned node.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Global admin curates the knowledge catalog (Priority: P1)

A global administrator creates, edits, archives, and reorganizes `KnowledgeStructureTemplate`s so that incubators have a shared catalog of learning content to clone from. Each template is a hierarchy of Modules → Topics → Subjects → Resources, with topic-level priority bands (High/Medium/Low) pre-populated against a 0–100 score scale.

**Why this priority**: Without templates, no project can clone. This is the foundational catalog layer; every other story depends on at least one template existing.

**Independent Test**: Can be fully tested by a global admin logging in, creating a template, adding two modules each with two topics (each with priority bands), three subjects per topic, and a mix of Video/Link/File resources, then archiving and un-archiving the template. Delivers a reusable learning-content catalog visible only to global admins.

**Acceptance Scenarios**:

1. **Given** a global admin is logged in, **When** they create a new template named "Emprendimiento Básico" with a description, **Then** the template appears in the global template list with `IsArchived = false`.
2. **Given** an existing template, **When** the admin adds a module, then a topic inside it with priority ranges High (80–100), Medium (50–79), Low (0–49), **Then** saving succeeds and the topic resolves a hypothetical score of 65 to Medium priority.
3. **Given** a topic, **When** the admin enters overlapping ranges (High 70–100, Medium 65–80), **Then** validation fails with a field-level error identifying the overlap.
4. **Given** an existing template with two modules, **When** the admin reorders them, **Then** the displayed order reflects the new `SortOrder` values.
5. **Given** a template exists, **When** the admin archives it, **Then** the template disappears from the default clone picker but remains resolvable by existing project clones.
6. **Given** an archived template, **When** a coordinator toggles "Show archived" in the clone picker, **Then** the archived template becomes selectable again.
7. **Given** a template that has been cloned into at least one project, **When** the admin attempts a hard delete, **Then** the delete is blocked with an error instructing them to archive instead.

---

### User Story 2 - Coordinator clones a template and customizes the project structure (Priority: P1)

An incubator coordinator, scoped to a specific project via the cascading context selector, clones a global `KnowledgeStructureTemplate` into a project-scoped `KnowledgeStructure` and then edits it freely — renaming items, adjusting priority ranges, adding project-specific modules or resources, and reordering the tree. Changes to the clone never affect the template.

**Why this priority**: This is the coordinator's day-one workflow. Onboarding a project starts here. SC-K01 (≤10 minutes) is measured against this flow.

**Independent Test**: Can be fully tested by a coordinator selecting a project, cloning a global template, verifying the full tree is copied, editing a topic name and a priority band on the clone, adding a brand-new topic and resource, and confirming the template rows are unchanged. Delivers a per-project learning structure that is safe to customize.

**Acceptance Scenarios**:

1. **Given** a project with no knowledge structure and a global template, **When** the coordinator clones the template, **Then** a new `KnowledgeStructure` is created scoped to the project, with all Modules/Topics/Subjects/Resources copied; root has `SourceTemplateId` and `SourceTemplateVersion` set; `SyncMode = Disconnected`; every copied node has its `SourceTemplateXExternalId` stamped.
2. **Given** a project clone, **When** the coordinator renames a topic and adjusts its Medium-band max score, **Then** saves succeed and the template's topic is unchanged.
3. **Given** a project clone, **When** the coordinator adds a new topic under an existing module, **Then** the new topic has no `SourceTemplateTopicExternalId` (clone-only).
4. **Given** a project clone, **When** the coordinator deletes a topic that no diagnostic question references, **Then** the delete succeeds.
5. **Given** a project clone where topic T1 is referenced by at least one diagnostic `Question.TopicId`, **When** the coordinator attempts to delete T1, **Then** the delete is blocked with a message identifying the referencing question count.
6. **Given** two coordinators scoped to different projects, **When** coordinator A views the knowledge tree, **Then** coordinator B's project clones are not visible (tenant isolation on `ITenantContext.CurrentProjectId`).

---

### User Story 3 - Cloning a diagnostic form cascades into an aligned knowledge structure (Priority: P2)

When a coordinator clones a `FormTemplate` that has been bound (1:1) to a `KnowledgeStructureTemplate`, the system automatically ensures the project has a consistent `KnowledgeStructure` cloned from the bound template — either creating it or reusing an existing one — and rewrites each `Question.TopicId` from the template-topic id to the corresponding per-project topic id so that downstream topic-score aggregation resolves correctly.

**Why this priority**: This closes the dangling-FK hazard that currently blocks adding `Questions.TopicId → knowledge.Topics.Id` as an enforced foreign key. It also unblocks Mentoring Plan (which aggregates scores per per-project topic). P2 because it depends on US1 + US2 being in place.

**Independent Test**: Can be fully tested by creating a form template bound to a knowledge template, cloning the form into a project with no prior knowledge clone, and verifying (a) the project now has a `KnowledgeStructure` cloned from the bound template, (b) each `Question.TopicId` in the new `ProjectForm` points at a `knowledge.Topics` row owned by the project, not by any template.

**Acceptance Scenarios**:

1. **Given** a form template bound to a knowledge template and a project with no knowledge clone, **When** the coordinator clones the form into the project, **Then** the project gains a new `KnowledgeStructure` cloned from the bound template; every `Question.TopicId` in the new `ProjectForm` resolves to a per-project topic row.
2. **Given** a project that already has a `KnowledgeStructure` cloned from template T, **When** the coordinator clones a second form template also bound to T, **Then** the existing project `KnowledgeStructure` is reused; no second clone is created; `Question.TopicId` values are rewritten through the existing clone's map.
3. **Given** a form template bound to a knowledge template, **When** the template contains any question whose `TopicId` references a topic NOT reachable in the bound knowledge structure, **Then** the clone fails with an error that names the offending question(s).
4. **Given** a form template with `DefaultKnowledgeStructureTemplateId = null`, **When** the coordinator clones the form, **Then** no knowledge cascade runs; `Question.TopicId` values retain their template-topic references (legal per the ratified decision for template-side questions).
5. **Given** a project has two form templates bound to two *different* knowledge templates, **When** both are cloned into the same project, **Then** the project has two independent `KnowledgeStructure` rows.

---

### User Story 4 - Coordinator configures topic priority ranges and emits a domain event (Priority: P2)

On a project clone, the coordinator edits a topic's High/Medium/Low score bands to tune how that topic's diagnostic score will later map to a priority bucket in the mentoring plan. Saving range changes emits a `TopicPriorityRangesChanged` in-process notification so downstream modules (not yet built) can react.

**Why this priority**: Required by FR-025a (downstream Mentoring Plan priority mapping). P2 because no consumer ships in v1, but the emit must exist so Mentoring Plan can subscribe without re-shipping this module.

**Independent Test**: Can be fully tested by opening a project topic, adjusting its High band from 80–100 to 75–100, saving, and verifying (a) the saved ranges persist, (b) a `TopicPriorityRangesChanged` notification was published with `{ TopicExternalId, ProjectId, HighRange, MediumRange, LowRange }`, (c) the topic resolves a hypothetical score of 77 to High instead of Medium.

**Acceptance Scenarios**:

1. **Given** a project topic with bands High 80–100 / Medium 50–79 / Low 0–49, **When** the coordinator changes High to 75–100, **Then** the new ranges persist and resolving a score of 77 returns High.
2. **Given** a project topic, **When** the coordinator saves with overlapping bands (High 70–100, Medium 65–80), **Then** the save fails with a field-level validation error.
3. **Given** a project topic with all three bands unset, **When** resolving any score, **Then** the result is `NotApplicable` and the UI shows a "no priority bands configured" warning.
4. **Given** a project topic, **When** the coordinator saves a valid range change, **Then** a `TopicPriorityRangesChanged` notification is published with the topic's `ExternalId`, the project id, and the three band values.
5. **Given** editing ranges on a *template* topic (not project), **When** saving, **Then** no `TopicPriorityRangesChanged` event is emitted (event is scoped to project clones).

---

### User Story 5 - Coordinator pulls newly-added template items via PartialSync (Priority: P3)

A coordinator whose project clone is in `PartialSync` mode triggers a "Sync from template" action to append new modules/topics/subjects/resources that the template has added since the original clone, without disturbing any local modifications on the clone.

**Why this priority**: Useful for long-running projects when the global catalog evolves. Nice-to-have; the MVP ships without requiring this for any downstream module.

**Independent Test**: Can be fully tested by cloning a template, enabling PartialSync, adding a new topic to the template on the admin side, triggering sync on the clone, and verifying (a) the new topic appears in the clone with its `SourceTemplateTopicExternalId` stamped, (b) a pre-existing clone-only topic is untouched, (c) a pre-existing cloned topic whose name the coordinator had renamed retains its renamed value.

**Acceptance Scenarios**:

1. **Given** a project clone in `PartialSync` mode and a template that has added a new topic T_new under an existing module M, **When** the coordinator triggers Sync from template, **Then** T_new is appended under M in the clone at `SortOrder = max+1`, with `SourceTemplateTopicExternalId` stamped.
2. **Given** the same setup, **When** the template has also added a new subject under an existing topic, **Then** the subject is appended under the clone's matching topic.
3. **Given** a clone containing a locally-added topic T_local (no `SourceTemplateTopicExternalId`), **When** sync runs, **Then** T_local is never touched.
4. **Given** a clone whose topic T_cloned was renamed after cloning, **When** sync runs, **Then** T_cloned retains its renamed value (sync never modifies existing cloned items).
5. **Given** a clone in `Disconnected` mode, **When** the coordinator attempts to trigger sync, **Then** the action is disabled/blocked with a message instructing them to enable PartialSync first.
6. **Given** a successful sync that added 1 module, 3 topics, 5 subjects, 2 resources, **When** the operation returns, **Then** the UI shows an apply-and-summarize result "11 items added across 4 modules/topics/subjects/resources" (no per-item diff preview).

---

### Edge Cases

**Clone lifecycle:**

- **EC-01** — Deleting a `KnowledgeStructureTemplate` that has project clones is blocked; archival (`IsArchived` flag) is the only removal path. Archived templates remain resolvable by `SourceTemplateId` but cannot be newly cloned.
- **EC-02** — Deleting a template Topic/Module/Subject/Resource is allowed; the item is removed from future clones; existing clones retain their stamped snapshot (orphaned from source on subsequent sync — no-op).
- **EC-03** — When a template item is renamed or reshaped after clones exist, the clones keep their snapshot; PartialSync never back-propagates edits. The clone UI surfaces a "template has changed" indicator derived from `SourceTemplateVersion` drift (informational only).
- **EC-04** — When the template adds a new item in position 2, PartialSync appends it at `max(SortOrder)+1` within the matching parent, not at position 2 (mirrors `SyncNewQuestionsFromTemplate`). The coordinator reorders manually afterward.

**Priority ranges:**

- **EC-10** — Topic with all three bands unset: every aggregated score resolves to `NotApplicable`; the UI warns "no priority bands configured".
- **EC-11** — Only a subset of bands set (e.g., only High): scores outside configured bands resolve to `NotApplicable`. Valid state; no warning.
- **EC-12** — Min == Max within a band is valid (single-point range).
- **EC-13** — Overlapping bands are rejected at save with a field-level validation error.

**Cross-module clone cascade:**

- **EC-20** — `FormTemplate.DefaultKnowledgeStructureTemplateId = null`: form clone proceeds without knowledge cascade; `Question.TopicId` values retain template-topic references (legal per ratified decision for template-side questions).
- **EC-21** — `FormTemplate` has `DefaultKnowledgeStructureTemplateId` set but some `Question.TopicId` references a topic not reachable in that knowledge structure: clone fails with an error naming the offending question(s).
- **EC-22** — Project already has a clone of the same `KnowledgeStructureTemplate` (same `SourceTemplateId`): the existing clone is reused; a second project-level `KnowledgeStructure` is never created; the rewrite map is built from the existing clone.
- **EC-23** — Two form templates bound to *different* knowledge templates are cloned into the same project: two distinct project-level `KnowledgeStructure` rows co-exist (intentional; different trees).

**Deletion safety on project clones:**

- **EC-30** — Deleting a project-level Topic that has diagnostic `Question.TopicId` references is blocked with the error "Cannot delete topic X: N diagnostic questions reference it. Reassign or delete the questions first." Validation runs before `SaveChanges` so the user sees a clean error instead of a raw FK exception.
- **EC-31** — Deleting a project-level Module/Subject/Resource is allowed when no downstream dependencies exist (v1: no downstream refs exist yet).

**Tenant isolation:**

- **EC-40** — Project-clone queries filter by `ITenantContext.CurrentProjectId`; switching project context via the cascading selector changes which clones are visible.
- **EC-41** — Global template CRUD requires `ITenantContext.IsGlobalAdminScope == true`; coordinators cannot see or edit template rows.

## Requirements *(mandatory)*

### Functional Requirements

**Templates (global catalog, global-admin scope):**

- **FR-K01** — Global admin MUST be able to create, edit, archive (via `IsArchived`), and un-archive `KnowledgeStructureTemplate`s (name, description). Hard delete is blocked when any project clones exist.
- **FR-K02** — Template CRUD MUST support all four hierarchy levels (Module, Topic, Subject, Resource) with a `SortOrder` integer on every collection and explicit reorder commands at each level.
- **FR-K03** — A `Resource` MUST carry a `ResourceType` enum (`Video | Link | File`), a required `Url` string, a `Title`, and a `Description`. In v1, `ResourceType.File` is semantically "URL pointing at a file" (no blob storage).
- **FR-K04** — A `Topic` MUST carry three optional priority bands (High, Medium, Low), each a `(min, max)` decimal pair. Unset band = "not configured at this band". Bounds are on the same decimal scale as `diagnostic.AnswerOption.Score` (DECIMAL(10,2)); no absolute 0–100 bound is enforced.
- **FR-K05** — Topic-range validation MUST reject: overlapping bands within the same topic, and `min > max` within any band. Range bounds are arbitrary decimal values (see FR-K04); validation does NOT enforce an absolute 0–100 range.
- **FR-K06** — Each Topic MUST expose a score-to-priority resolution operation: given a score, return the first matching band in the order High → Medium → Low, or `NotApplicable` if no band matches.

**Project clones (coordinator scope, tenant-isolated to `ITenantContext.CurrentProjectId`):**

- **FR-K10** — Coordinator MUST be able to clone a `KnowledgeStructureTemplate` into a project-scoped `KnowledgeStructure`. The clone is deep (Modules, Topics, Subjects, Resources all copied). Every cloned node stamps the source template item's `ExternalId` in a typed field (`SourceTemplateModuleExternalId`, `SourceTemplateTopicExternalId`, etc.).
- **FR-K11** — The root `KnowledgeStructure` clone MUST stamp `SourceTemplateId` and `SourceTemplateVersion`. `SyncMode` MUST default to `Disconnected` on initial clone.
- **FR-K12** — Coordinator MUST have full CRUD on a project clone at every level: add new items (which have no `SourceTemplateXExternalId`, i.e., clone-only), edit any item, delete any item subject to deletion-safety rules (EC-30/31).
- **FR-K13** — Priority ranges MUST be copied from the template at clone time and MUST be independently editable per project thereafter; edits on the template do not propagate to existing clones.
- **FR-K14** — Coordinator MUST be able to toggle `SyncMode` between `Disconnected` and `PartialSync`; switching to `PartialSync` MUST be rejected if the clone has no `SourceTemplateId` (defensive; all clones have one, but `Create`-from-scratch structures would not).
- **FR-K15** — The `SyncFromTemplate` operation MUST only be invocable when `SyncMode = PartialSync`. It walks the template tree depth-first by ascending `SortOrder` at each level; at every level, for each template item whose `ExternalId` is NOT present as a `SourceTemplateXExternalId` anywhere in the clone, it appends a new cloned item under the parent whose own `SourceTemplateXExternalId` matches the template parent. Items with matching stamps are never modified; clone-only items are never touched. The whole operation MUST run inside a single transaction; no partial commit. Operation returns a summary count `{ modulesAdded, topicsAdded, subjectsAdded, resourcesAdded }`.

**Diagnostic integration (changes landing in this spec's PR):**

- **FR-K20** — `FormTemplate` MUST gain a `DefaultKnowledgeStructureTemplateExternalId` (nullable Guid; 1:1). Global admin MUST be able to set and clear this binding at form-template edit time.
- **FR-K21** — `CloneFormTemplateHandler` MUST auto-cascade when the source `FormTemplate.DefaultKnowledgeStructureTemplateId` is non-null, in this order, all inside a single transaction (no partial commit):
  1. If the target project already has a `KnowledgeStructure` whose `SourceTemplateId` equals the form's bound knowledge template id, reuse it.
  2. Otherwise, clone the bound `KnowledgeStructureTemplate` into a new project-scoped `KnowledgeStructure`.
  3. Build an in-memory lookup from each template-topic `ExternalId` to the corresponding project-topic id in the (new or reused) project structure.
  4. During question cloning, rewrite each `Question.TopicId` from the template-topic id to the project-topic id via this lookup.
- **FR-K22** — If `DefaultKnowledgeStructureTemplateId` is null, form cloning MUST proceed without any knowledge cascade; `Question.TopicId` values retain their template-topic references.
- **FR-K23** — If the cascade's rewrite step encounters a `Question.TopicId` that cannot be resolved through the map (template has a question whose topic is not in the bound `KnowledgeStructureTemplate`), the whole clone operation MUST fail with an error identifying the offending question(s); no partial commit.

**Events:**

- **FR-K30** — Editing a project topic's priority ranges MUST emit a `TopicPriorityRangesChanged` in-process `MediatR.INotification` with payload `{ TopicExternalId, ProjectId, HighRange, MediumRange, LowRange }`. The event contract MUST reside in `Mentoory.Knowledge.Application/IntegrationEvents/` per constitution Principle IV. No consumer ships in v1; the notification is informational for Mentoring Plan (future spec). Edits to *template* topic ranges MUST NOT emit this event (scope is project clones only).

**UI (coordinator area):**

- **FR-K40** — A `KnowledgeController` under `Mentoory.Web/Areas/Coordination/Controllers/` MUST present: (a) a global-admin-scoped tree view of templates; (b) a coordinator-scoped tree view of project clones filtered by current tenant context. Archived templates MUST be hidden from the clone picker by default with a "Show archived" toggle.
- **FR-K41** — Each view MUST offer inline CRUD at Module/Topic/Subject/Resource levels with Tabler-pattern reorder controls.
- **FR-K42** — Topic detail view MUST include a priority-range editor (three rows: High, Medium, Low, each min+max) with live validation feedback mirroring Diagnostic form-template editing.
- **FR-K43** — Project-clone detail view MUST expose a `SyncMode` toggle and a "Sync from template" action (enabled only when `PartialSync`). The post-sync result MUST be an apply-and-summarize notification ("N items added across M modules/topics/subjects/resources"); no per-item diff preview step is required in v1.
- **FR-K44** — All user-facing text MUST be in Spanish. All route parameters MUST use `ExternalId` (Guid). Code and internal docs remain in English.

**Seed data:**

- **FR-K50** — The single SSDT PR MUST include an idempotent PostDeployment script (numbered per the `Mentoory.Db.PostDeployment/` convention) that seeds at least one sample global `KnowledgeStructureTemplate` with at least one Module, one Topic (with a complete set of priority bands), one Subject, and one Resource of each `ResourceType`. The seed MUST be re-runnable without duplicating rows and MUST enable smoke testing the clone flow in fresh environments.

### Non-Functional Requirements

- **NFR-K01** — Clean Architecture: `Mentoory.Knowledge.Domain` has no external dependencies; `Application` uses MediatR + Mapperly; `Infrastructure` uses EF Core 10.x.
- **NFR-K02** — All external entities (`KnowledgeStructureTemplate`, `KnowledgeStructure`, `Module`, `Topic`, `Subject`, `Resource`) have `ExternalId` (Guid); routes use ExternalId exclusively (never internal long IDs).
- **NFR-K03** — Commands implement `IBaseRequest` / `IBaseRequest<TResult>`; handlers derive from `BaseCommandHandler<T>`; FluentValidation covers command input.
- **NFR-K04** — Read-only query handlers MUST use `AsNoTracking()` (Phase A gate per constitution).
- **NFR-K05** — Tenant isolation: project clones filtered by `ITenantContext.CurrentProjectId`; global template CRUD requires `ITenantContext.IsGlobalAdminScope == true`. Authorization uses `[Authorize(Roles=...)]` ADDITIVELY combined with `CheckPermission`, per constitution Principle X (role hierarchy) and the access-security constitution. Concretely:
  - Global `KnowledgeStructureTemplate` CRUD controller/actions: `[Authorize(Roles = "GlobalAdmin")]`.
  - Project-clone CRUD controller/actions: `[Authorize(Roles = "ProjectCoordinator,IncubatorAdmin,GlobalAdmin")]` (higher roles always included).
  - Any query surface coordinators use MUST also include `IncubatorAdmin` and `GlobalAdmin` per the hierarchical-inclusion rule.
- **NFR-K06** — `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` — zero warnings before commit.
- **NFR-K07** — No `DateTime.UtcNow` in Domain; use `ITimeProvider` (Application) or pass `utcNow` as a parameter (Domain).
- **NFR-K08** — All cross-schema DB changes (knowledge.* schema + tables, `diagnostic.FormTemplates.DefaultKnowledgeStructureTemplateId` nullable FK, `diagnostic.Questions.TopicId` FK constraint to `knowledge.Topics.Id`) MUST ship in a single SSDT PR (atomic deploy).
- **NFR-K09** — Test coverage MUST include: unit tests for all domain invariants (range validation, sync semantics, match-by-ExternalId, score-to-priority resolution); handler tests for `CloneFormTemplateHandler` cascade + rewriting and for `SyncFromTemplate`; at least one integration test covering the full round-trip "clone template → diagnostic response → topic aggregation resolves through per-project topics".
- **NFR-K10** — `MenuConfiguration.cs` MUST gain Knowledge entries placed in the Coordination area; every menu group that contains a Knowledge entry MUST include `GlobalAdmin` in its roles array per constitution Principle X. Global-admin-scoped entries use the `GlobalAdmin`-only role; coordinator-scoped entries use `ProjectCoordinator,IncubatorAdmin,GlobalAdmin`.

### Key Entities

- **KnowledgeStructureTemplate** — Global-catalog root. Attributes: `ExternalId`, `Name`, `Description`, `IsArchived`, `Version` (bumped on any descendant change). Owns a collection of **ModuleTemplate** via composition.
- **ModuleTemplate** — Ordered child of `KnowledgeStructureTemplate`. Attributes: `ExternalId`, `Name`, `Description`, `SortOrder`. Owns a collection of **TopicTemplate**.
- **TopicTemplate** — Ordered child of `ModuleTemplate`. Attributes: `ExternalId`, `Name`, `Description`, `SortOrder`, `HighRangeMin`, `HighRangeMax`, `MediumRangeMin`, `MediumRangeMax`, `LowRangeMin`, `LowRangeMax` (all nullable). Owns a collection of **SubjectTemplate**.
- **SubjectTemplate** — Ordered child of `TopicTemplate`. Attributes: `ExternalId`, `Name`, `Description`, `SortOrder`. Owns a collection of **ResourceTemplate**.
- **ResourceTemplate** — Ordered child of `SubjectTemplate`. Attributes: `ExternalId`, `Title`, `Description`, `Url`, `ResourceType` (enum Video/Link/File), `SortOrder`.
- **KnowledgeStructure** — Project-scoped root clone. Attributes: `ExternalId`, `ProjectId`, `IncubatorId`, `Name`, `Description`, `SourceTemplateId`, `SourceTemplateVersion`, `SyncMode` (Disconnected/PartialSync), `CreatedAtUtc`. Owns **Module**s.
- **Module / Topic / Subject / Resource** — Project-scoped mirror of the template hierarchy. Each carries the same intrinsic attributes as its template counterpart PLUS a nullable `SourceTemplateXExternalId` (null when the item was added locally on the clone, not sourced from template).
- **TopicPriorityRangesChanged** *(domain event)* — `INotification` payload `{ TopicExternalId, ProjectId, HighRange: (min, max)?, MediumRange: (min, max)?, LowRange: (min, max)? }`. Published when a project topic's bands change.
- **FormTemplate** *(existing Diagnostic aggregate — extended by this spec)* — Gains a nullable `DefaultKnowledgeStructureTemplateExternalId` (1:1) + internal FK.
- **Question** *(existing Diagnostic entity — FK constraint added by this spec)* — `TopicId` gains an enforced FK to `knowledge.Topics.Id`. No schema-shape change to `Question` itself.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-K01** — An Incubator Admin can onboard a project, including cloning a knowledge structure and reviewing topic priority ranges, in under 10 minutes (measured end-to-end from project selection to "save and exit" on the reviewed clone).
- **SC-K02** — Hierarchical CRUD at all four levels (Structure → Module → Topic → Subject → Resource) round-trips cleanly in the coordinator UI and via command handlers, with automated tests covering each level's create/edit/delete/reorder path.
- **SC-K03** — Cloning a global `KnowledgeStructureTemplate` produces a per-project copy; an automated integration test demonstrates that editing the project clone leaves the template row completely unmodified.
- **SC-K04** — Cloning a `FormTemplate` with a non-null `DefaultKnowledgeStructureTemplateId` produces a self-consistent `ProjectForm` whose `Question.TopicId` values all resolve to `knowledge.Topics` rows owned by the target project; an automated integration test asserts this, and the new `diagnostic.Questions.TopicId` FK passes schema validation without referential integrity errors against existing seeded test data.
- **SC-K05** — Triggering PartialSync against a template that has added at least one new topic AND one new subject appends both under their matching parents in the clone without disturbing local edits; a unit test asserts the sort-order is `max+1` and an integration test asserts a pre-existing renamed topic retains its local name.
- **SC-K06** — A `TopicPriorityRangesChanged` notification is published on every project-topic range edit that persists successfully; a handler-test subscriber asserts one notification is fired per save with the correct payload.

## Assumptions

- **Topic score scale is arbitrary decimal** *(amended during plan-phase research — see `research.md`)* — `GetTopicScoreAggregationHandler` returns raw `DECIMAL(10,2)` sums and averages against `AnswerOption.Score`; no normalization layer exists. FR-K04/K05 therefore validate only band consistency (non-overlapping, `min <= max`), with no absolute numeric bound. Coordinators configure bands on the same decimal scale as their diagnostic option scores. Mentoring Plan later decides whether to pass `TotalScore` or `AverageScore` to `Topic.ResolvePriority`.
- **Single SSDT PR atomicity** — the cross-schema changes (knowledge.* tables + diagnostic FK + `FormTemplates.DefaultKnowledgeStructureTemplateId` column) deploy atomically. This is the standing SSDT/DACPAC convention in the repo.
- **In-process MediatR notifications are sufficient for v1** — `TopicPriorityRangesChanged` is published and handled in-process. The outbox pattern decided under cross-cutting hardening (#10) is NOT adopted for this event in v1; if the Mentoring Plan consumer requires cross-process durability later, the publisher can be upgraded without changing this module's domain.
- **Coordinator identity and project scoping** — an authenticated coordinator session already exposes `ITenantContext.CurrentProjectId` via the cascading context selector (spec 008). No changes to that plumbing are required.
- **ResourceType.File v1 semantics** — `File` is a URL pointing at an externally-hosted file (Drive, CDN, etc.). No upload UX, no blob backend, no MIME validation. The v1 form field for Url is a free-text URL input with standard URL-format validation.
- **Template-side `Question.TopicId`** — continues to reference template-topic ids (ratified during roadmap review). This spec does not change that; it only constrains project-side clones via FR-K21's rewrite.
- **Spanish UI translations** are provided by the coordinator as resource strings; this spec does not depend on any specific translation tooling beyond what the Diagnostic module already uses.
