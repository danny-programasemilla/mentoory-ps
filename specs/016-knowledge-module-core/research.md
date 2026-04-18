# Phase 0 Research: Knowledge Module Core

**Feature**: 016-knowledge-module-core
**Date**: 2026-04-18
**Purpose**: Resolve the deferred items flagged as Assumptions / Open Questions in `spec.md` and any technical unknowns surfaced by the plan's Technical Context.

---

## R1 — Topic score scale and priority-range bounds

### Question
The spec assumed topic scores are normalized 0–100 and therefore FR-K05 bounded priority ranges to `[0, 100]`. What is the actual score scale?

### Findings
- `Mentoory.Db/diagnostic/Tables/AnswerOptions.sql` and `AnswerOptionTemplates.sql` both declare `[Score] DECIMAL(10, 2) NOT NULL`. No min/max constraint; any positive or negative decimal value with two decimal places is legal.
- `Mentoory.Diagnostic.Application.Queries.GetTopicScoreAggregation.GetTopicScoreAggregationHandler` sums selected options' raw scores and returns `TopicScoreDto { long TopicId, decimal TotalScore, int QuestionCount, decimal AverageScore }`. No normalization step exists anywhere in the aggregation path.
- Nowhere in the codebase is there a "max possible score per topic" concept or a utility that would normalize the aggregation output.

### Decision
**Drop the absolute 0–100 bound. Priority ranges are arbitrary decimal values on the same scale as `AnswerOption.Score`.** Validation enforces only intra-topic consistency: `min <= max` within each configured band, non-overlapping bands, and the `NotApplicable` fallback for any score outside configured bands.

### Rationale
- Smallest blast radius: only `spec.md` FR-K04/K05 + Assumptions change; no other module is touched.
- Matches reality: coordinators already configure option scores on an arbitrary decimal scale when designing diagnostic form templates; they will intuitively configure topic bands on the same scale.
- Leaves Mentoring Plan free to decide whether `TotalScore` or `AverageScore` is passed to `Topic.ResolvePriority` at consumption time. Keeps this module's domain decoupled from aggregation semantics.

### Alternatives Considered
- **Introduce `Topic.MaxPossibleScore` + percent normalization.** Rejected: Topic would need to know which questions belong to it (a Diagnostic-side concept); introduces a leaky dependency from Knowledge → Diagnostic.
- **Normalize in `GetTopicScoreAggregationHandler`.** Rejected: modifies a handler outside this spec's scope; affects any existing (or untested) consumer of the raw aggregation result; widens the change surface unnecessarily.

### Spec Amendments Applied
- FR-K04 reworded: "decimal pair ... same decimal scale as `diagnostic.AnswerOption.Score` (DECIMAL(10,2)); no absolute 0–100 bound is enforced."
- FR-K05 reworded: only `min <= max` and non-overlap; removed absolute-range constraint.
- Assumptions section updated with "amended during plan-phase research" note pointing here.

### Downstream Implications (tracked into data-model.md and contracts/)
- `TopicTemplate` / `Topic` range columns: `DECIMAL(10, 2) NULL` (matches `AnswerOption.Score` precision).
- `PriorityRange` value object: `(decimal Min, decimal Max)`; factory enforces `Min <= Max`.
- Validator across bands enforces non-overlap on successful range edits.
- Acceptance scenarios in US1 (score 65 → Medium) and US4 (score 77 → High) remain valid — they just aren't implicitly asserting 0–100.

---

## R2 — Concurrent edits on project clones

### Question
Spec Open Question: do we address two coordinators editing the same project clone simultaneously?

### Findings
- Existing Diagnostic module has no optimistic-concurrency versioning on `ProjectForm` or its children. Last-write-wins via EF Core's default behavior; simultaneous edits produce whichever `SaveChanges` lands last.
- Coordinator-tier scale is modest; users are typically assigned one per project. Historical incidents from concurrent edits: zero reported (inferred from issue tracker scan and absence of row-version columns).

### Decision
**No explicit concurrency handling in v1.** Defer optimistic-concurrency tokens to a cross-cutting hardening spec if a real incident emerges.

### Rationale
- Consistent with existing Diagnostic precedent; adding `RowVersion`/`ConcurrencyToken` here alone would create an inconsistent pattern across two nearly-identical modules.
- YAGNI: no user reports, no stakeholder request, negligible real-world exposure at the coordinator tier.

### Alternatives Considered
- **Add `RowVersion` (`ROWVERSION` SQL type → `byte[]` EF concurrency token) on every aggregate root.** Rejected: new infra pattern applied inconsistently; defer until a concurrency incident motivates a module-wide adoption.

### Risk Acceptance
- Two coordinators editing the same clone at the same second: last save wins; earlier user sees their change vanish on refresh. Acceptable for v1. Tracked in `brainstorm/07-knowledge-module.md` Open Threads.

---

## R3 — Transactional boundaries for cascade and sync

### Question
FR-K15 (SyncFromTemplate) and FR-K21 (CloneFormTemplateHandler auto-cascade) both require "single transaction; no partial commit." How is that enforced in the existing handler pattern?

### Findings
- Every existing command handler in `Mentoory.Diagnostic.Application/Commands/*` ends with a single `await _repository.UnitOfWork.SaveEntitiesAsync(cancellationToken);` call.
- `SaveEntitiesAsync` wraps EF Core `SaveChangesAsync` inside an implicit transaction by default. Cross-aggregate changes accumulated in the same `DbContext` are committed atomically.
- For the form-clone cascade (FR-K21), the handler must share a `DbContext` across Knowledge and Diagnostic writes OR use explicit `TransactionScope` / `IDbContextTransaction`. Current Diagnostic handlers do NOT span multiple module `DbContext`s; the codebase uses a separate `DbContext` per module (`DiagnosticDbContext`, pending `KnowledgeDbContext`).

### Decision
**Use an explicit `IDbContextTransaction` obtained from a shared connection in the modified `CloneFormTemplateHandler`.** For `SyncFromTemplate` (single-module, Knowledge only), the default `SaveEntitiesAsync` transaction is sufficient.

### Rationale
- Cross-module atomicity requires deliberate coordination. EF Core per-module `DbContext`s will not share an implicit transaction.
- The pragmatic path: `CloneFormTemplateHandler` acquires a `TransactionScope` (or explicit `BeginTransactionAsync` on a shared connection) wrapping both Knowledge and Diagnostic saves. SQL Server's MSDTC is not required when both contexts point at the same connection string.
- `SyncFromTemplate` stays within `Mentoory.Knowledge` module; no coordination complexity needed.

### Alternatives Considered
- **Merge `DiagnosticDbContext` + `KnowledgeDbContext` into one context** for this cascade. Rejected: breaks module boundaries and complicates future extraction.
- **Use an integration event / outbox to decouple form-clone and knowledge-clone.** Rejected: introduces eventual-consistency between two steps of a single user action; fails FR-K23's "no partial commit" requirement.
- **Accept partial commit (Knowledge saves, then Diagnostic fails → orphan knowledge clone).** Rejected: violates FR-K23 and would require cleanup logic.

### Downstream Implications
- `CloneFormTemplateHandler` gains an explicit transaction. Will be documented in contracts/.
- Test coverage: an integration test that intentionally induces a Diagnostic-save failure mid-cascade and asserts the Knowledge clone is rolled back.

---

## R4 — EF Core 10.x owning-collection patterns for the four-level tree

### Question
How should the Module → Topic → Subject → Resource hierarchy be modelled with EF Core 10.x for clean aggregate loading?

### Findings
- Existing Diagnostic module uses:
  - Private backing fields (`private readonly List<Question> _questions = new();`) exposed via `IReadOnlyCollection<T>` + `.AsReadOnly()`.
  - String-based `Include("_questions")` in repositories to hydrate private collections (constitution Principle XI explicit guidance).
  - `internal` navigation properties for EF-only access.
  - `IHaveExternalId` or equivalent shared interface to expose the Guid lookup.
- No existing precedent for four-level nested Include in the repo; Diagnostic maxes out at two levels (FormTemplate → QuestionTemplate → AnswerOptionTemplate).

### Decision
**Replicate the Diagnostic pattern at each of the four levels:** private backing field + `.AsReadOnly()`, internal navigation, string-based nested Include in repository `GetByExternalIdWithFullTreeAsync`.

### Rationale
- Consistency with Diagnostic; zero new patterns for maintainers to learn.
- EF Core 10.x handles four-level `.Include().ThenInclude().ThenInclude().ThenInclude()` chains correctly. Cartesian explosion is bounded by expected scale (~20 modules × ~5 topics × ~5 subjects × ~5 resources = ~2500 rows worst case).
- Can augment with `AsSplitQuery()` if profiling reveals Cartesian cost; start simple.

### Alternatives Considered
- **Document-style storage (JSON columns for children).** Rejected: breaks EF Core query composition, complicates partial loading, diverges from Diagnostic.
- **Separate queries per level (1 load per aggregate level).** Rejected: N+1 hazard; violates aggregate-loading principles.

### Downstream Implications
- `IKnowledgeStructureTemplateRepository.GetByExternalIdWithFullTreeAsync` / `GetByIdWithFullTreeAsync`.
- Equivalent on `IKnowledgeStructureRepository`.
- Persistence tests verify full-tree hydration.

---

## R5 — Versioning on template changes

### Question
`KnowledgeStructureTemplate.Version` is referenced throughout the spec (stamped on clones at FR-K11; drift indicator at EC-03). How is `Version` bumped?

### Findings
- `FormTemplate.Version` in Diagnostic is bumped inside aggregate mutation methods (e.g., `AddQuestion`, `RemoveQuestion`) by `Version++`. The field is initialized to `1` at creation.
- No separate event or publisher is involved; the bump is a pure domain concern.

### Decision
**Mirror Diagnostic: `KnowledgeStructureTemplate.Version` is bumped inside every aggregate root mutation method (add/update/delete/reorder at any descendant level).** Mutations routed through the root aggregate; leaf entities (Resource, Subject, Topic, Module) cannot bump version directly.

### Rationale
- Preserves the aggregate invariant that only the root can coordinate version changes.
- Keeps the drift-detection logic simple: `SourceTemplateVersion` stamped on clone at clone time; compare on sync.

### Downstream Implications
- Every mutation command that targets descendants (e.g., `AddTopicTemplate`) routes through `KnowledgeStructureTemplate.AddTopic(...)` which calls into the Module child and bumps its own `Version++`.
- Data-model.md will enumerate the mutation methods.

---

## R6 — Menu configuration and route structure

### Question
Where exactly do the Knowledge entries belong in `MenuConfiguration.cs`, and what's the route shape?

### Findings
- `MenuConfiguration.cs` (read) groups menu items by area/feature with `roles` arrays. Existing groups for Coordination include Projects, Diagnostic Forms, etc. Per constitution Principle X, every group MUST include `GlobalAdmin`.
- Current coordinator area URL pattern is `/Coordination/{Controller}/{Action}/{externalId?}`. The `KnowledgeController` will follow this pattern.

### Decision
- **Coordination > Knowledge (single top-level entry)** containing:
  - "Plantillas de conocimiento" (visible only to GlobalAdmin role), route: `/Coordination/Knowledge/Templates`
  - "Estructuras del proyecto" (visible to ProjectCoordinator, IncubatorAdmin, GlobalAdmin), route: `/Coordination/Knowledge/Projects`
- Menu group `roles`: `["ProjectCoordinator", "IncubatorAdmin", "GlobalAdmin"]` (per Principle X — higher roles always included).

### Rationale
- Mirrors the Diagnostic Forms menu entry placement and role arrays.
- Two sub-entries keep global-admin template CRUD visible separately from coordinator clone workflows, making tenancy boundaries obvious in the UI.

### Alternatives Considered
- **Two top-level menu groups.** Rejected: adds UI clutter; templates and clones are the same conceptual domain.
- **Single entry with runtime tab switching.** Rejected: breaks URL bookmarking and deep-linking.

---

## R7 — PostDeployment seed numbering

### Question
What is the next available PostDeployment script number?

### Findings
- `Mentoory.Db.PostDeployment/` contains scripts 000–004 currently (from file scan during setup). Next available: `005.SeedKnowledgeSampleTemplate.sql`.
- Actually re-checking: `000.Main.sql`, `001.SeedTenantData.sql`, `002.SeedDiagnosticData.sql`, `003.SeedAccessData.sql`, `004.SeedTestData.sql`. The spec's FR-K50 suggested `015.*` but the real next number is `005`.

### Decision
**Name the seed `005.SeedKnowledgeData.sql`** — matches the `{NNN}.Seed{Domain}Data.sql` convention and uses the next available slot.

### Rationale
- Consistent with existing seed naming.
- `015.*` in the spec was a placeholder; the real number comes from the filesystem.

### Downstream Implications
- Tasks.md (future) will reference `005.SeedKnowledgeData.sql`.
- The seed MUST also add a sample `FormTemplate.DefaultKnowledgeStructureTemplateId` binding on the existing Diagnostic seed template if any, so smoke tests can exercise the cascade end-to-end. Add a coordinated line to `002.SeedDiagnosticData.sql` OR include the binding in `005.*` via UPDATE — implementation detail for plan/tasks phase.

---

## Open items passed to `/speckit-tasks`

None. All plan-phase unknowns resolved; no NEEDS CLARIFICATION remain.

## Items deferred past this spec

- Optimistic concurrency tokens on Knowledge aggregates (R2). Revisit only on incident.
- Outbox upgrade for `TopicPriorityRangesChanged` (spec Assumptions). Revisit when Mentoring Plan consumer lands.
- Per-item diff preview for PartialSync (spec Assumptions). Revisit as UX polish spec.
- Resource file upload / blob storage (spec Out of Scope). Separate future spec.
