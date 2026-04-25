# Phase 0 Research: Project Lifecycle Application + UI Completion

**Feature**: 016-project-lifecycle-finish
**Date**: 2026-04-18

This document records the technical decisions made before Phase 1 design. Each entry follows the Decision / Rationale / Alternatives format.

---

## R1. Concurrency protection for stage advancement (FR-006)

**Decision**: Add a SQL Server `ROWVERSION` column (named `RowVersion`) to `tenant.Projects` and map it as EF Core's `IsRowVersion()` concurrency token on the `Project` entity configuration. The `AdvanceProjectStageHandler` catches `DbUpdateConcurrencyException` and returns a dedicated conflict failure code that the controller surfaces as a Spanish "otra operación modificó este proyecto — recargue la página" toast.

**Rationale**:
- Two coordinators clicking "advance" concurrently must not both succeed; the spec (FR-006) and edge case in `spec.md` require this explicitly.
- The `Projects` table currently has no concurrency token — the `tenant/Tables/Projects.sql` source (see plan.md for the path) defines only business columns and timestamps. This is the root cause of the gap.
- `ROWVERSION` is SQL Server's native, zero-code token — maintenance-free (the server updates it on every row write).
- EF Core 10 maps `byte[]` + `IsRowVersion()` to `ROWVERSION` naturally; no EF migration (per constitution XI — SSDT only).
- Scope: only `Projects` needs the token. Child `ProjectStages` updates go through the `Project` aggregate root, whose `UpdatedAtUtc` and `RowVersion` both update in the same `SaveChangesAsync` call, so the aggregate-level token protects all nested changes.

**Alternatives considered**:
- **Application-level lock table or semaphore**: rejected — adds infrastructure, does not survive process restarts, and SQL-side protection is simpler.
- **Serializable transaction / `SELECT ... WITH (UPDLOCK)` in the repository**: rejected — couples business logic to explicit locking, risks deadlocks, and is harder to reason about than optimistic concurrency.
- **Check `CurrentStageType` + `UpdatedAtUtc` as composite token**: rejected — `UpdatedAtUtc` has millisecond precision at best and is set by application code, so two writes within the same tick could collide undetected. `ROWVERSION` is monotonic per row and server-managed.
- **No concurrency protection, accept occasional double-advance**: rejected — violates FR-006 and creates incorrect stage history (the second advance would skip a stage).

**Implications**:
- `Mentoory.Db/tenant/Tables/Projects.sql` gains one column: `[RowVersion] ROWVERSION NOT NULL`.
- `Mentoory.Tenant.Infrastructure/Persistence/Configurations/ProjectConfiguration.cs` (existing, per the repo layout) gets `.Property(p => p.RowVersion).IsRowVersion();`.
- `Project` entity gains a `public byte[] RowVersion { get; private set; } = null!;` property; EF Core populates it on read.
- Integration tests using `EF Core InMemory` must fall back to relational provider (SQLite in-memory or real SQL Server) **only for the handler tests that assert concurrency behavior**. Other handler tests keep using InMemory.

---

## R2. Placement of the stage → actions mapping (FR-020)

**Decision**: Create a small static registry `Mentoory.Access.Application.StageActions.StageActionRegistry` that maps each `StageType` (from `Mentoory.Tenant.Domain.Enums.StageType`) to the set of `StageGatedAction` enum values (also defined in `Mentoory.Access.Application.StageActions`) valid in that stage. Consumed by:
1. `GetProjectLifecycleHandler` — projects the action availability into `ProjectLifecycleDto`.
2. `RequiresStageAttribute` (Web action filter) — server-side guard on stage-gated endpoints.

**Rationale**:
- Both the view model (what to render as available/locked/past) and the server-side guard need the identical mapping. Duplicating it creates drift (spec FR-020 makes "single source of truth" an explicit requirement).
- `Mentoory.Access.Application` is the established home for authorization policy data (it already contains `CheckPermissionHandler` and `Permission` enum). Stage gating is a policy concern, not a tenant-state concern.
- Referencing `Mentoory.Access.*` from `Mentoory.Web` is already established (web uses Access queries elsewhere).
- The mapping is small and immutable (stage count ≤ 7, action count likely < 20), so a compile-time `static readonly Dictionary<StageType, ImmutableHashSet<StageGatedAction>>` is sufficient. No database table is needed.

**Alternatives considered**:
- **Place the registry in `Mentoory.Web.Infrastructure`**: rejected. The registry must be callable from both the web filter (fine) and from `GetProjectLifecycleHandler` in `Mentoory.Tenant.Application` (not fine — Application cannot reference Web per constitution I). The Access module is already referenced by both Web and other Application modules, making it the correct shared location.
- **Place it in `Mentoory.Tenant.Domain`**: rejected. The set of actions is a cross-domain policy artifact (diagnostics, mentoring, corrections, learning — all cross-module). It does not belong to the Tenant domain model.
- **Database-backed registry with a `StageActionPolicies` table**: rejected for this release. The mapping is static, changes via code review, and benefits from compile-time safety. A DB-backed version could be introduced later if the business wants runtime configurability — this is noted as a future option but explicitly deferred.
- **Embed action → stage relation in each action's own module (e.g., put "AnswerCorrection → Analysis" in the Diagnostic module)**: rejected. Creates N registries instead of one; filter code and UI would need N lookups; spec FR-020 is explicit about a single place.

**Implications**:
- `StageGatedAction` enum enumerates coordination-area actions that have stage gates. Initial members (derived from existing Coordination controllers and planned ones): `DiagnosticForms` (Forms stage), `AnswerCorrection` (Analysis stage), `LearningAssignment` (LearningAssignment stage), `MentoringCoordination` (Mentoring stage), `FinalEvaluation` (FinalEvaluation stage), `Closure` (Closure stage). The exact enum list is finalized in `data-model.md`.
- Future actions register themselves by adding an enum member and updating the registry in one commit.

---

## R3. UI affordance for locked / available / past actions (FR-017, FR-018)

**Decision**: Render each action as a Tabler card in a responsive grid on the Lifecycle view, with three visual states:
1. **Available** — primary-colored card, clickable link to the action, icon in primary color, subtitle "Disponible ahora".
2. **Locked** — muted/disabled card styling (`opacity: 0.6`), non-clickable, lock icon (`ti-lock`), subtitle "Disponible desde la etapa <Stage>". `aria-disabled="true"` and `tabindex="-1"`; Bootstrap tooltip reveals the same text on hover/focus.
3. **Past** — subdued card with a checkmark badge, clickable (if the action remains useful for historical review — e.g., viewing past diagnostic submissions), subtitle "Etapa completada".

**Rationale**:
- Tabler already ships card components, status colors, and the icon font (`ti-lock`, `ti-circle-check`, `ti-clock`), so implementation is CSS-only on top of standard components — constitution IX (Spanish) and the existing UI framework (constitution Technology Standards) are respected automatically.
- Three states align with FR-017 exactly.
- The spec requires accessibility (FR-018 "accessible helper text"). `aria-disabled` + tooltip + non-focusable locked cards satisfy screen-reader and keyboard-only users.
- The subtitle text (Spanish) names the unlocking stage, satisfying FR-018 and Acceptance Scenario 4 of Story 3.

**Alternatives considered**:
- **Show only available actions, hide locked**: rejected. Users lose context on what exists and what is coming. Spec explicitly says "locked or hidden with clear indication of which stage unlocks them" — but hiding leaves no affordance for discoverability.
- **Disable via `pointer-events: none` only (no aria-disabled)**: rejected — fails accessibility.
- **Tabs per stage with actions inside each tab**: rejected — the overview is better at answering the coordinator's core question ("where is this project and what should I do next?") than a tab grid.

---

## R4. Spanish display names for the seven stages

**Decision**: Centralize stage display names in a single `StageTypeDisplay` static class (placed in `Mentoory.Web/Infrastructure/Display/StageTypeDisplay.cs`, matching patterns used elsewhere in Web). The mapping:

| `StageType` (enum) | Spanish display name     |
|--------------------|--------------------------|
| Registration       | Registro                 |
| Forms              | Formularios              |
| Analysis           | Análisis                 |
| LearningAssignment | Asignación de Aprendizaje |
| Mentoring          | Mentoría                 |
| FinalEvaluation    | Evaluación Final         |
| Closure            | Cierre                   |

**Rationale**:
- Constitution IX mandates Spanish UI. The existing `Details.cshtml` in `Administration/Views/Projects` renders the enum literal (`@Model.CurrentStageType`) — this is already a bug waiting to be fixed. This feature fixes it for the Coordination view and documents the central helper for future features to adopt.
- A single extension method `this.StageType.ToDisplayName()` replaces every `@stage` rendering; a grep for `CurrentStageType` will find all future offenders.
- Keeping the helper in `Mentoory.Web` avoids polluting the Domain layer with presentation concerns (constitution I).

**Alternatives considered**:
- **Resource files (`.resx`)**: rejected for scope. The platform is single-language; adding localization infrastructure for seven strings is over-engineering. If the platform ever supports additional languages, this helper is one of the first candidates to migrate.
- **Attribute-based display names on the enum via `[Display]`**: rejected — `Display` attribute values live with the enum in the Domain layer, violating constitution I's layer boundaries.

---

## R5. Stage advancement confirmation UX (FR-022, FR-023)

**Decision**: A single Bootstrap modal confirmation dialog triggered by the "Avanzar Etapa" button. Modal body names the current stage and the next stage ("¿Confirma avanzar de *Registro* a *Formularios*?"). Submit is a `POST` to `/Coordination/Projects/AdvanceStage/{externalId}` with antiforgery token. Success/failure surfaces via the established `showToast(message, type)` pattern (constitution Technology Standards — Web Layer). The page then reloads to reflect the new stage state.

**Rationale**:
- Stage advancement is irreversible (per spec Assumptions). A confirmation step is standard UX for irreversible actions.
- Matches existing platform patterns (Tabler modal + toasts).
- Full page reload after success is acceptable because the page is small (single project, 7 stages + actions grid). An AJAX partial update would add complexity for marginal user-visible benefit.

**Alternatives considered**:
- **Inline confirmation (click twice)**: rejected — less discoverable and easier to trigger accidentally.
- **Full-page confirmation screen**: rejected — disruptive for a simple yes/no.

---

## R6. Scope of stage-gated action filter

**Decision**: Ship the filter attribute `[RequiresStage(StageGatedAction.AnswerCorrection)]` as infrastructure and apply it to a small set of **currently existing** coordination-area controllers/actions that are documented stage-gated in the registry. Do not attempt to retroactively gate all coordination endpoints in this release; only the ones whose spec owners have confirmed the stage mapping.

**Concrete initial application**:
- `AnswerCorrectionController` actions → `[RequiresStage(StageGatedAction.AnswerCorrection)]`, gated to `Analysis`.
- `DiagnosticsController` form-management actions (Clone, Configure) → `[RequiresStage(StageGatedAction.DiagnosticForms)]`, gated to `Forms`.

Other coordination-area actions that do not yet have stage mapping are not newly gated in this release. They appear in the Lifecycle overview only if their mapping is declared in `StageActionRegistry`; undeclared actions do not appear in the action grid. This keeps the scope bounded while still delivering the full filter mechanism.

**Rationale**:
- Spec FR-016–021 defines the mechanism; the spec does not enumerate every action. Enumerating actions in one release forces cross-feature coordination that is out of scope.
- The registry is the contract: once an action is added to the registry, its existing controller gains the `[RequiresStage]` attribute, and the Lifecycle view starts showing it. New actions ship with their registry entry from the start.

**Alternatives considered**:
- **Gate every coordination action in this release**: rejected — balloons scope and requires touching feature code owned by other teams.
- **Ship the registry empty and let future PRs populate it**: rejected — this release must deliver demonstrable UI stage-gating for the acceptance scenarios. Populating the two already-existing gated actions (answer correction, diagnostic forms) gives a visible demo.

---

## R7. Permission model — using `ManageLifecycle` vs. Role-based check

**Decision**: Keep the feature on the **role-based `[Authorize]`** model used by the rest of the Coordination area (`ProjectCoordinator,IncubatorAdmin,GlobalAdmin`). Do not introduce `CheckPermissionQuery(Permission.ManageLifecycle)` checks in this feature.

**Rationale**:
- The entire platform currently authorizes coordination-area endpoints via `[Authorize(Roles = ...)]` with the hierarchical role set. `ManageLifecycle` exists in the `Permission` enum but is unused anywhere in the codebase (grep confirmed). Introducing `CheckPermissionQuery` usage here creates a one-off inconsistency.
- Constitution X treats role-based authorization as the primary mechanism. Permission-based authorization is a future extension documented in `access-security-constitution.md` but not yet wired in.
- If/when the platform moves to permission-based checks, it should be a cross-cutting migration, not introduced feature-by-feature.

**Alternatives considered**:
- **Use `CheckPermissionQuery(Permission.ManageLifecycle)` in addition to `[Authorize]`**: rejected — duplicative, inconsistent with every other coordination controller, and adds a query per request with no current benefit.
- **Require the caller be a `ProjectCoordinator` of this specific project (not just any coordinator in the incubator)**: deferred. Project-scoped role assignments exist (`RoleAssignment` aggregate), but the incubator-level scope (via `[Authorize]` + tenant isolation) is consistent with how other coordination actions work today. Tightening this is a separate hardening concern.

---

## R8. Query shape for `GetProjectLifecycleQuery`

**Decision**: The query takes a single `Guid ProjectExternalId` parameter and returns `ProjectLifecycleDto` with:
- Project identity (ExternalId, Name, Description, IncubatorName, IsActive)
- Current stage (StageType + Spanish display + state)
- List of 7 `ProjectLifecycleStageDto` in canonical order: `StageType`, `DisplayName`, `State`, `StartedAtUtc`, `CompletedAtUtc`, `AdvancedByDisplay` (resolved user name when available, else null)
- `AvailableActions`, `LockedActions`, `PastActions` — each a list of `StageActionDto { Action, DisplayName, GatingStageType, GatingStageDisplayName, LinkUrl }`.
- `CanAdvance: bool` — precomputed server-side using the same rules as the command (current stage In Progress, not Closure, project active, user has permission). UI uses this to render/hide the advance button.

Query is side-effect-free (constitution II). Implemented with `AsNoTracking()` on the `Project` aggregate with `Include("_stages")`, then projection to DTOs via Mapperly.

**Rationale**:
- One round-trip, one projection — simple and cacheable if needed later.
- Keeping `CanAdvance` server-side avoids the client reconstructing the rule (duplication bug risk).
- Mapperly is the approved mapper (constitution — Dependency Governance).

**Alternatives considered**:
- **Return raw entity and let the Razor view compute states**: rejected — pushes business logic into views, violates CQRS (constitution II).
- **Split into two queries (identity + stages)**: rejected — unnecessary chattiness for a page that always needs both.

---

## Summary — `NEEDS CLARIFICATION` resolution

The Technical Context in `plan.md` contains zero `NEEDS CLARIFICATION` markers. All open questions surfaced during planning were resolvable from the spec, the existing codebase, and the two constitutions. No subagent research runs were required.
