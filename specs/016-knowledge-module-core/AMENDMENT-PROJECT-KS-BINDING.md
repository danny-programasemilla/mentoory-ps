# Amendment: Project-owned Knowledge Structure binding

**Date**: 2026-04-19
**Status**: Approved (brainstorm session [11-knowledge-module-binding-redesign](../../brainstorm/11-knowledge-module-binding-redesign.md))
**Affects**: spec 016 — FR-K20..FR-K23, data-model, contracts/diagnostic-cascade, tasks.md

---

## Problem

The shipped-in-progress design put `DefaultKnowledgeStructureTemplateExternalId` on
`FormTemplate`. Because a project can have multiple diagnostic forms (initial,
mid-term, final, impact…), each form could reference a different KS template —
leading to a project holding **N KnowledgeStructures**, one per distinct form
binding. That violates the business reality: a project has **one** learning
path / knowledge tree; multiple diagnostic forms evaluate against the same
topics in that one tree.

## Decision

The binding moves to `Project`. Forms keep `DefaultKnowledgeStructureTemplateExternalId`
but only as *compatibility metadata*. Project has exactly one `KnowledgeStructure`,
set at creation and immutable.

## Invariants (post-amendment)

1. `Project` ⇄ `KnowledgeStructure` is 1-to-1 (UNIQUE constraint on `KnowledgeStructures.ProjectId`).
2. `Project.KnowledgeStructureTemplateExternalId` — **required** at creation, **immutable** after.
3. `Project.KnowledgeStructureExternalId` — populated in the project-creation transaction, **NOT NULL** once the project exists.
4. `FormTemplate.DefaultKnowledgeStructureTemplateExternalId` — *compatibility metadata only*.
5. `CloneFormTemplateHandler` — never provisions KS; only rewrites topic ids against the project's existing KS; rejects incompatible forms.
6. Changing a project's KS template after creation is out of scope for v1.

## Authorization

Project creation (which now carries the KS template choice) is open to:
`ProjectCoordinator, IncubatorAdmin, GlobalAdmin`.

## Data-model delta

### `tenant.Projects` — ALTER

Add two columns:

| Column | Type | Nullable | Notes |
|---|---|---|---|
| `KnowledgeStructureTemplateExternalId` | `UNIQUEIDENTIFIER` | **NOT NULL** | FK → `knowledge.KnowledgeStructureTemplates(ExternalId)`. Immutable after INSERT (enforced at application layer — no trigger). |
| `KnowledgeStructureExternalId` | `UNIQUEIDENTIFIER` | **NOT NULL** | FK → `knowledge.KnowledgeStructures(ExternalId)`. Populated in the project-creation transaction. Immutable after INSERT. |

Deferred: no trigger-based immutability; handler-level guard + test coverage.

### `knowledge.KnowledgeStructures` — ALTER

- Add `CONSTRAINT [UQ_KnowledgeStructures_ProjectId] UNIQUE ([ProjectId])`.
- Drop the composite index `IX_KnowledgeStructures_ProjectId_SourceTemplateId` (superseded by the unique constraint on `ProjectId`).
- `SourceTemplateId` stays `NOT NULL` now (since every project KS is sourced from a template; no more "create-from-scratch" path in v1 — the `Create` factory already throws `NotSupportedException`).

### `diagnostic.FormTemplates` — no schema change

Column `DefaultKnowledgeStructureTemplateExternalId` stays as-is; semantics change from "drives cascade clone" to "advertises compatibility".

## Behavior changes

### Project creation flow

- `CreateProjectCommand` gains a required `KnowledgeStructureTemplateExternalId` parameter.
- Handler (in `Mentoory.Tenant.Application`):
  1. Validate KS template exists and is not archived.
  2. Create `Project` aggregate (existing logic).
  3. Call into Knowledge module (via a cross-module interface, mirroring `ITopicUsageQuery`) to `CloneKnowledgeStructureTemplate(templateExternalId, projectId, incubatorId)` and get back the new structure's ExternalId.
  4. Stamp `project.SetKnowledgeStructure(templateExternalId, structureExternalId)`.
  5. Save both contexts (Tenant + Knowledge) in the project-creation transaction.
- Validation fails if the template is missing or archived.

### Form-template clone (US3 simplified)

- `CloneFormTemplateHandler` flow:
  1. Load `FormTemplate`.
  2. Load project's `KnowledgeStructure` via new `IKnowledgeStructureRepository.GetByProjectIdAsync(projectId, ct)`.
  3. Compatibility check: if `template.DefaultKnowledgeStructureTemplateExternalId` is non-null AND differs from `project.KnowledgeStructureTemplateExternalId`, return `Failure` with the Spanish message.
  4. Build topic-id rewrite map from the project KS (no more loading the KS template's full tree for this; project KS already has `SourceTemplateTopicExternalId` stamps).
  5. Call `ProjectForm.CloneFromTemplate(..., topicIdRewriteMap)` (existing contract).
  6. Save `ProjectForm` — only one context involved (Diagnostic); knowledge has no writes here.

The per-project KS is no longer created lazily by the cascade; it already exists.

### US2 UI simplification

- **Remove** `CloneFromTemplate.cshtml` (coordinator's template picker).
- **Remove** `CloneKnowledgeStructureTemplateCommand` action from `KnowledgeController` (it still exists in Application but is only callable from Tenant.Application's project-creation path).
- **Remove** the "Clonar desde plantilla" button in `Projects.cshtml`.
- **Update** `Projects.cshtml` empty state: instead of "Clone a template to begin", show "This project has no knowledge structure — contact an administrator" (defensive; shouldn't happen post-amendment because KS is NOT NULL).

### Admin UI addition

- Project creation form (`Administration/Projects/Create` — or wherever it lives today) gets a **required** KS template dropdown, populated from `ListKnowledgeStructureTemplatesQuery(includeArchived=false)`.
- The form blocks submission if no template is picked.

## Repository contract changes

`IKnowledgeStructureRepository`:

```csharp
// REPLACE:
//   Task<KnowledgeStructure?> GetByProjectAndSourceTemplateIdAsync(long projectId, long sourceTemplateId, CancellationToken);
//   Task<int> CountClonesBySourceTemplateIdAsync(long sourceTemplateId, CancellationToken);
// WITH:
Task<KnowledgeStructure?> GetByProjectIdAsync(long projectId, CancellationToken cancellationToken);
Task<bool> ExistsByTemplateExternalIdAsync(Guid templateExternalId, CancellationToken cancellationToken);
```

`DeleteKnowledgeStructureTemplateHandler` (EC-01): count-via-`ExistsByTemplateExternalIdAsync`-style clones-exist check still works — `KnowledgeStructures.SourceTemplateId` FK + `ExternalId` uniqueness means a single `ANY` query resolves it.

## Seed data (clean-state rebuild)

No backward compatibility needed (nothing in production). Rebuild seeds:

- `004.SeedTestData.sql`:
  - **Add**: `tenant.Projects` INSERTs now set `KnowledgeStructureTemplateExternalId` to the `"Emprendimiento Básico"` template ExternalId (`11111111-1111-1111-1111-111111111111`).
  - **Keep**: § 3.5 KS seed (project-side `KnowledgeStructures` + `Modules` + `Topics` 1-5), but now with `SourceTemplateId = <Emprendimiento Básico Id>` instead of `NULL`. `Project.KnowledgeStructureExternalId` points at this structure's ExternalId.
  - **Remove**: ad-hoc `SourceTemplateId = NULL` placeholder on the seeded KS.
- `005.SeedKnowledgeData.sql`:
  - § 1 (sample template) unchanged.
  - § 2 (FormTemplate wiring) unchanged — diagnostic FormTemplates still declare compatibility with `"Emprendimiento Básico"`.
- `IntegrationTestBase.SeedKnowledgeTopicsAsync`: unchanged behavior, but the seeded KS now references a seeded template (added at that point) — or alternatively, each integration test seeds its own Project + KS via `CreateProjectCommand` to keep the test path realistic. Decide during implementation.

## Tests impact

- **Domain tests (Knowledge.Tests.Domain)** — unaffected.
- **Handler tests (Knowledge.Tests.Handlers)** —
  - `DeleteTopicHandlerTests` unchanged (uses `ITopicUsageQuery`).
  - Remove `CloneKnowledgeStructureTemplateHandlerTests`' coordinator-path tests; the command now only runs from Tenant-side. Keep unit tests that cover the factory behavior.
  - Rename/adjust `ProjectStructureCrudHandlerTests` to stop assuming "clone happens from UI."
- **Handler tests (Tenant.Tests.Handlers)** — add `CreateProjectHandlerTests` coverage for the new KS-template param: success, missing template, archived template.
- **Diagnostic.Tests.Handlers** — `CloneFormTemplateHandlerCascadeTests` becomes "form-clone compatibility": project has KS A, form bound to A → success; form bound to B → rejection; form unbound → legacy pass-through (no rewrite, no KS touched).
- **Integration (Mentoory.Tests.Integration)** — `DiagnosticCascadeRoundTripTests` adjusted: preseed project with KS template via `CreateProjectCommand`, then clone form and verify rewrite. "Reuses existing KS" test becomes trivially true — retire or repurpose as a regression check that a second form clone doesn't DUPLICATE the KS.
- **E2E** — project-creation Playwright tests need the KS template dropdown covered. Existing flows should still pass if the test projects are created via the seed (which sets the KS).

## Out of scope

- Changing a project's KS template after creation.
- Multi-KS-per-project (domain partitioning / per-stage KS / etc.).
- Migration from the v1-as-merged design — N/A, we're still pre-release.
- Trigger-level immutability enforcement on the Project columns (handler-level only).

## Open threads

- Whether `CreateProjectCommand` should stream the KS-clone work through a cross-module interface (new `IKnowledgeStructureProvisioner` in `Mentoory.Tenant.Application.Abstractions`) or directly inject `IKnowledgeStructureRepository`. Leaning toward the interface for the same module-boundary reasons as `ITopicUsageQuery`.
- Whether `Project.KnowledgeStructureExternalId` being NOT NULL at the schema level is achievable with the existing `Project` creation ordering. The create-project flow must insert Project + KS + UPDATE Project.KSExternalId in a single transaction. Simplest order: (1) clone the KS template into Knowledge schema (gets ExternalId), (2) insert Project with that ExternalId in hand. This requires Tenant.Application to orchestrate both writes before SaveChangesAsync fires on either context. Implementation choice: either share a DbContextTransaction or accept the window where Project.KSExternalId is briefly NULL during creation (NULLABLE column in DB + NOT-NULL-in-domain + CHECK-during-commit). Decide in `/speckit-plan`.
