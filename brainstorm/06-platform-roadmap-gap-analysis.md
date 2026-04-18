# Brainstorm: Platform Roadmap — Gap Analysis + 4-Phase Delivery Plan

**Date:** 2026-04-18
**Status:** active
**Spec:** — (this is a roadmap that feeds multiple future specs, not a single spec)

## Problem Framing

Spec 001 (`specs/001-mentory-platform-core/spec.md`) defines the full Mentory enterprise SaaS platform — 8 user stories, 59 functional requirements, 12 success criteria. Since then, 12 follow-up specs (002–013) have been created for hardening, consolidation, and UX polish. The user requested a **deep gap analysis** to determine what remains to reach the spec 001 goal, plus a **feature-by-feature delivery plan** optimized for quality and parallelism.

Context on delivery shape:
- **Capacity:** Solo human + AI pairing. Parallelism achieved via AI subagent worktrees (2–3 warm streams alongside one hot stream).
- **Priority:** Balanced parallel waves (hardening + new features in parallel, respecting dependency gates).
- **Artifact:** This roadmap doc plus per-feature brainstorm seed documents (07–10) that future `/spex:brainstorm` sessions can pick up.

---

## Gap Analysis Summary

### Implementation scorecard by user story (from 5 parallel exploration streams)

| US | Module | Spec FRs | Done | Partial | Missing | Verdict |
|----|--------|:-------:|:----:|:-------:|:-------:|---------|
| US1 | Access + Tenant | FR-001–013, 046–056 | 17 | 5 | 1 | ~80% — solid foundation, authorization-rigor gaps |
| US1/5 | Mentor Assignment | FR-057–059 | 3 | 0 | 0 | 100% |
| US2 | Diagnostic | FR-014–019 | 9 | 1 | 0 | ~90% — Topic FK blocked by US3 |
| US3 | Knowledge | FR-020–024 | 0 | 1 | 4 | **0% — empty scaffold** |
| US4 | Mentoring Plan | FR-025–027 | 0 | 0 | 5 | **0% — not started** |
| US5 | Mentoring Execution | FR-028–031 | 0 | 0 | 4 | **0% — not started** |
| US6 | Subscription | FR-032–035 | 0 | 1 | 3 | ~5% — empty scaffold, domain method on Incubator |
| US7 | Lifecycle | FR-036–039 | 2 | 0 | 2 | ~50% — domain done, no application/UI |
| US8 | Notification | FR-040–044 | 0 | 0 | 5 | **0% — empty scaffold** |
| X | Audit | FR-045 | 0 | 1 | 0 | **Half-built — infra exists, zero callers** |

**Weighted platform completion: ~35%.** The foundation (auth, tenancy, diagnostics, assignments) is real. The value chain (US3 → US4 → US5) is not started. Cross-cutting (US6, US8, audit, lifecycle enforcement) is incomplete.

### Cross-cutting state
- ✅ **Build:** clean, 0 warnings, `TreatWarningsAsErrors` green
- ✅ **Constitution compliance:** no `DateTime.UtcNow` abuse, no AutoMapper, no Dapper as primary access, no swallowed exceptions, no repository-in-controller injection, no TODO/FIXME in code
- ✅ **Tests:** Access (17), Diagnostic (12), Tenant (8), Integration (14), E2E (25)
- ❌ **Tests:** Knowledge / Mentoring / Notification / Subscription have **zero test files**
- ❌ **Tabler migration (spec 009):** library installed, layout templates not migrated
- ⚠️ **Audit wiring:** `IAuditService` + `AuditLog` table exist, but **no handler invokes `LogAsync`** anywhere

### Concrete hardening items on existing "done" work

**Priority SECURITY + CONSTITUTION items (must be Phase A gate, not backlog):**

- **S1** — `RegisterUserHandler` returns distinct field-keyed errors for national-ID vs email conflicts on a public endpoint → unauthenticated enumeration oracle → violates **FR-052** (public-endpoint enumeration prevention). *Shipped security bug.*
- **S2** — `ProjectsController` uses `[Authorize(Roles = "IncubatorAdmin,GlobalAdmin")]`. Constitution Principle X (lines 219-233) explicitly requires `"ProjectCoordinator,IncubatorAdmin,GlobalAdmin"` on project-scoped controllers. Per FR-010 permission matrix, Coordinators have Edit/View but NOT Create → fix is **action-level authorization** (split the blanket controller attribute), not just attribute widening. *Shipped constitution violation.*
- **S3** — `ProjectsController.GetIncubatorExternalIdAsync` (lines 99-109) fetches the user's active `incubatorId` then discards it and fires `ListIncubatorsQuery().FirstOrDefault()`. For users with multiple incubator assignments (including GlobalAdmin), this returns the *wrong* incubator. *Shipped correctness bug.*

**Remaining hardening items:**

4. Admin-enrollment controller flow with specific-field feedback (**FR-053**) not wired
5. `RegisterUserValidator` missing password-must-not-contain-email-or-ID rule (**FR-056**)
6. Controllers use `[Authorize(Roles=...)]` only. Fix per **FR-010** is **additive — layer `CheckPermission` on top of role guards**, not replace them. Role guards stay; an action filter or pipeline behavior adds per-action/per-resource checks.
7. Handlers accept `ProjectExternalId` without validating it belongs to the user's active incubator → **FR-011/012** cross-tenant URL-manipulation risk
8. `CorrectAnswerHandler` doesn't emit audit events → **FR-018b** audit requirement unmet
9. `CreateProjectHandler` doesn't enforce subscription project limit → **FR-034** unenforced
10. `Sponsor` role defined but not exposed via any controller → US1 Sponsor read-only scope unmet
11. `RoleAssignmentRepository.Query()` returns `AsNoTracking()` without a mandatory tenant filter — potential leakage if a caller forgets the filter (see Risk Register for the GlobalAdmin-aware fix)
12. `AsNoTracking` usage on query-handler read paths is unverified — only 2 direct matches found in `Queries/**/*.cs`. Either repositories apply it at `Query()` level consistently, or there is non-compliance. *Phase A verification task.*

---

## Dependency Graph

```
Access/Tenant (US1) ──┬─> Diagnostic (US2) ──┐
                      │                       ├─> Mentoring Plan (US4) ──> Mentoring Execution (US5)
                      └─> Knowledge (US3) ────┘

Parallel-independent streams (no upstream deps on new value-chain work):
  • Subscription (US6)          — gates feature availability; integration points in Tenant + Diagnostic
  • Notification (US8)          — pure side-effect; subscribes to domain events
  • Lifecycle app/UI (US7)      — domain model done; needs command + UI
  • Audit wiring (FR-045)       — horizontal pipeline behavior; module-by-module retrofit
  • Access hardening            — FR-052, FR-053, FR-056, RBAC rigor
  • Tabler migration (spec 009) — UI-only, zero domain risk
```

Key insight: **Knowledge (US3) is the single longest critical-path piece**. Everything else is either parallelizable or small.

---

## 4-Phase Delivery Plan

**Parallelism model:** 1 **hot stream** (human + Claude pairing) + 2–3 **warm AI streams** in isolated git worktrees. Warm streams are bounded to their own module directories so they don't collide with the hot stream's files. Each warm stream merges via PR for human review.

**Total estimated duration:** 8–11 weeks.

### Phase A — Foundation hardening + unlock value chain (~2–3 weeks)

| Stream | Scope | Owner | FRs | Risk |
|--------|-------|-------|-----|------|
| **A-hot** | **Knowledge module (US3)** — full domain (Structure/Module/Topic/Subject/Resource), clone pattern matching Diagnostic, Topic priority-range fields (HighMin/Max, MediumMin/Max, LowMin/Max for FR-025a), Coordinator CRUD UI, template + clone customization | Human + Claude | FR-020–024, Topic FK fixup for FR-017 | High — biggest critical-path piece; unblocks US4 |
| **A-α** | **Access + controller security polish** — (1) generic public-endpoint registration errors closing the enumeration oracle (R-SEC-1 / FR-052); (2) admin-enrollment controller with specific feedback (FR-053); (3) password-contains-email-or-ID validator (FR-056); (4) `ProjectsController` constitution fix + action-level split + `GetIncubatorExternalIdAsync` multi-incubator correctness fix (R-SEC-2); (5) `AsNoTracking` compliance audit across all query paths (R-SEC-3) | AI worktree | FR-052, FR-053, FR-056, FR-010 (constitution Principle X), plus R-SEC-1/2/3 | Low-Medium — localized but touches shipped controllers; regression tests required |
| **A-β** | **Audit pipeline wiring** — MediatR `AuditingBehavior`; attribute-based opt-in; retrofit CorrectAnswer, SetActiveContext, AdvanceStage, plan-approval handlers | AI worktree | FR-045, FR-018b | Medium — contract design for audit payload |
| **A-γ** | **Lifecycle finish (US7)** — `AdvanceProjectStageCommand` + handler, Coordinator UI, stage-gated action filters on UI | AI worktree | FR-037, FR-039 | Low — domain exists |

**Exit criteria for Phase A:**
- Knowledge module CRUD functional end-to-end (template + project clone + customization), **with unit tests for every new domain method and handler tests for every new command/query handler**
- All public registration paths return identical generic errors; admin enrollment returns specific feedback; regression test covers both enumeration modes
- `ProjectsController` authorization conforms to constitution Principle X with action-level splits; multi-incubator regression test passes for `Create` + `Details`
- `AsNoTracking` compliance verified across all query handlers (either via repo-level `Query()` defaults or direct handler usage); offenders listed and fixed
- At least 5 sensitive commands emit audit events via `AuditingBehavior`; audit log queryable in Platform Admin area; architecture test enforces `[Audited]` on sensitive command classes
- Coordinator can advance a project through stages via UI; wrong-stage actions are blocked; handler + UI tests cover state transitions

### Phase B — Platform capability completion in parallel (~2–3 weeks)

| Stream | Scope | Owner | FRs | Risk |
|--------|-------|-------|-----|------|
| **B-hot** | **Mentoring Plan (US4)** — `MentoringPlan` aggregate; `DiagnosticCompletedEvent` subscriber that builds cumulative topic scores → priority mapping → suggested plan; collaborative adjust/approve workflow; mentor + entrepreneur UI | Human + Claude | FR-025, 025a, 025b, 025c, FR-026, FR-027 | High — cross-module integration + core product logic |
| **B-α** | **Subscription module (US6)** — versioned plans; positive-only accumulative overrides; effective-plan query; `CreateProjectHandler` gate; FormTemplate filtering by subscription tier | AI worktree | FR-032–035, FR-034 enforcement, FR-014 subscription-tier filter | Medium — integration surface in Tenant + Diagnostic |
| **B-β** | **Notification engine (US8)** — `NotificationOutbox` pattern with shared transaction; MailKit channel adapter; immediate + scheduled dispatch; dedup by event key; preferences stub | AI worktree | FR-040–044 | Medium — outbox correctness + dedup semantics |
| **B-γ** | **Tabler migration closeout (spec 009)** — `_Layout.cshtml` → navbar-vertical; auth split-panel layout; icon webfont conversion | AI worktree | spec 009 DoD | Low — UI-only |

**Exit criteria for Phase B:**
- Completing a diagnostic triggers suggested plan generation; mentor + entrepreneur can adjust and approve. **Unit tests for score-mapping logic, handler tests for plan generation, integration test for the Diagnostic → Mentoring event flow.**
- Creating a project past subscription limit is blocked with clear UX; **unit tests for effective-plan computation, handler test for limit enforcement.**
- Notification outbox delivers a test email; scheduled notification fires on cron; **interceptor-attachment test + dedup test ship with the module.**
- All layouts use Tabler patterns; no pre-Tabler templates remain.

### Phase C — Execution + rigor (~2–3 weeks)

| Stream | Scope | Owner | FRs | Risk |
|--------|-------|-------|-----|------|
| **C-hot-1** | **Scheduling engine + Sessions (US5.a)** — calendar generator from plan + scheduling params; session scheduling/logging UI; flexible topic coverage; session audit log | Human + Claude | FR-028, 029, 030 | Medium-High — algorithm design |
| **C-hot-2** | **Assignment workflow (US5.b)** — creation/submission/review/feedback; notification events | Human + Claude (after C-hot-1) | FR-031 | Medium — integrates with Notification |
| **C-α** | **Authorization rigor** — controller-level `CheckPermission` enforcement via action filter / attribute; resource-ownership guard via MediatR pipeline behavior; Sponsor role surfaced with read-only dashboards; Coordinator on Projects | AI worktree | FR-010, 011, 012 deep | Medium — security-sensitive; thorough test coverage required |
| **C-β** | **E2E test coverage for US1–5 golden paths** — register → verify → context → diagnostic → plan → session → assignment; cross-tenant isolation tests; subscription-limit tests | AI worktree | SC-005, SC-006 | Low — test-only |

**Exit criteria for Phase C:**
- End-to-end journey works: registration through assignment feedback. **Unit + handler tests for the scheduling engine and assignment workflow ship with the feature.**
- Every controller action validates permission against active context on the server via the **additive** layering — existing `[Authorize]` role guards retained, `CheckPermission` calls or an action filter added on top for per-action/per-resource rigor.
- ≥3 golden-path E2E tests pass reliably; cross-tenant isolation verified (including the GlobalAdmin-bypass scenarios from R-SEC paths).

### Phase D — Production readiness (~1–2 weeks)

**Note:** per-phase exit criteria already require tests to ship with each feature. Phase D is NOT a "catch up on tests" phase — it's production polish. If any earlier phase did not meet its test coverage gate, the gap is surfaced and closed *before* entering Phase D.

| Stream | Scope | Targets |
|--------|-------|---------|
| **D-hot** | Sponsor read-only dashboards; notification preferences full UI; edge-case coverage from spec 001 | FR-003 Sponsor, FR-043 full, spec 001 edge cases |
| **D-α** | Performance hardening — `Include()` bloat review, N+1 detection via EF logging, query plan review on hot paths; aim SC-009 (p95 <200ms @ 50 incubators × 100 projects). (`AsNoTracking` compliance was verified in Phase A.) | SC-009 |
| **D-β** | Additional coverage beyond per-phase minimums — property-based tests for score aggregation + priority mapping, load test scenarios, chaos tests for notification outbox recovery | Hardening & confidence |

---

## Quality & Integration Risk Register

These are the seams where parallel work most likely collides, where correctness is hardest, or where shipped code has a security/compliance gap that must be closed in Phase A.

### Security + correctness (Phase A gate)

- **R-SEC-1 — Registration enumeration oracle (FR-052):** `RegisterUserHandler` leaks whether a national-ID or email is already registered on an unauthenticated endpoint. Phase A-α must normalize to a single generic failure response on public paths (admin enrollment retains specific feedback per FR-053). Acceptance: identical HTTP status + body + timing across all failure modes; property-based test that probes both conflict types.
- **R-SEC-2 — `ProjectsController` constitution violation + multi-incubator bug:** per constitution Principle X, project-scoped controllers require `ProjectCoordinator,IncubatorAdmin,GlobalAdmin`; FR-010 requires action-level split (Coordinator can View/Edit, not Create). `GetIncubatorExternalIdAsync` additionally returns the wrong incubator for multi-assignment users. Phase A-α must fix both with regression tests for multi-incubator users.
- **R-SEC-3 — `AsNoTracking` compliance audit:** only 2 query handlers directly reference `AsNoTracking`. Phase A must verify compliance across all repository `Query()` methods + direct query handlers and list any offenders. No handler ships to Phase B without compliance.

### Integration seams (design risk)

1. **Knowledge Topic ↔ Diagnostic Question FK (FR-017) — coordinated SSDT change:** today `Questions.TopicId` is `BIGINT NOT NULL` with no target table. When A-hot creates `knowledge.Topics`, the FK addition AND any `FormTemplate.DefaultKnowledgeStructureId` column land in the **same SSDT PR** (cross-schema DACPAC change), not split across migrations — otherwise the intermediate publish is invalid. Add a data-seed for existing test fixtures. One PR, one integration test.

2. **`DiagnosticCompletedEvent` → Mentoring Plan generation:** the event exists in Diagnostic but has no subscriber. Design the event contract (`ProjectId`, `UserId`, `EvaluationStage`, `CompletedAtUtc`) explicitly before Phase B.

3. **Subscription limit enforcement in Tenant:** `CreateProjectHandler` needs an interface in `Shared.Application` for "effective features" query; Subscription implements it. Tenant must not depend on Subscription.

4. **AuditingBehavior retrofit:** once the pipeline behavior lands (A-β), every new handler must opt in. An architecture test asserts handlers for sensitive commands carry the `[Audited]` attribute.

5. **Notification outbox transactional consistency:** outbox writes must participate in the triggering handler's transaction. With module-scoped DbContexts, this is achieved via a `SaveChangesInterceptor` (not a shared DbContext and not a dual-write). See seed 10 for the committed v1 pattern.

6. **GlobalAdmin-aware tenant filters (NOT blanket query filters):** `RoleAssignmentRepository.Query()` returns an unscoped `IQueryable` — the canonical smell. A **blanket** EF query filter in `OnModelCreating` would silently break GlobalAdmin cross-tenant operations (constitution Principle X). The fix is either (a) a query filter that bypasses when `ITenantContext.IsGlobalAdminScope` is true, or (b) explicit `QueryForCurrentTenant()` vs `QueryAcrossTenants()` repository methods with the wider one gated by a GlobalAdmin check. Two integration tests required: (i) non-GlobalAdmin queries see only their tenant rows, (ii) GlobalAdmin queries see all tenant rows.

7. **Answer-correction retroactive score re-aggregation:** spec edge case — corrections don't re-aggregate unless triggered. Document this in the Mentoring Plan brainstorm.

---

## What's Out of Scope (explicit)

From spec 001 assumptions + edge cases:
- **Email change re-verification** (flagged out of scope in spec, edge cases line 202)
- **Native mobile apps** (desktop web only)
- **External calendar integration** (no Google Calendar / Outlook sync)
- **Payment gateway** (FR-035 — admin-managed only)
- **SMS / in-app notification delivery** (FR-044 — extensibility hooks yes, channels no)
- **Cross-incubator collaboration** (each tenant is isolated)

---

## Decision

**Proceed with the 4-phase roadmap as defined.**

- Phase A starts with Knowledge as the hot stream; 3 warm AI streams handle hardening (Access + controller security polish), audit wiring, and lifecycle finish in parallel.
- Each phase has explicit exit criteria **including tests shipping with each feature**; do not start the next phase until the hot stream's exit criteria are met.
- Per-feature seed documents for the four biggest upcoming pieces are captured in `07-knowledge-module.md`, `08-mentoring-plan.md`, `09-mentoring-execution.md`, `10-cross-cutting-hardening.md`. Each is parked pending a focused `/spex:brainstorm` when the stream is scheduled.
- Phase A hardening stream (A-α) is small enough to go straight to `/speckit-specify` without a dedicated brainstorm.
- Phase A-β Audit pipeline design is moderately non-trivial; recommend a quick brainstorm before specify (seed 10 commits to v1 scope — see that doc).

### Decisions ratified during the code-review loop

- **Outbox pattern (seed 10):** committed to `SaveChangesInterceptor` attached to each module's DbContext writing to a shared outbox schema in the same transaction as the business operation. Dual-write is explicitly rejected due to atomicity concerns with module-scoped DbContexts.
- **Audit v1 scope (seed 10):** `AuditingBehavior` captures command type + payload (with sensitive-field redaction) + user/tenant/project/role context + correlation ID + outcome. Entity-level before/after diffs are OUT OF SCOPE for v1; aggregate changes are traceable via domain events logged separately if needed.
- **Topic identity in Question (seed 07):** diagnostic questions on a `ProjectForm` reference the cloned `Topic`; questions on a `FormTemplate` reference the template `Topic`. No longer an open question.
- **Authorization layering (this doc):** `CheckPermission` calls are ADDITIVE to `[Authorize]` role guards, never replacements. Existing role attributes stay; granular permission checks layer on top.
- **Tenant query filters (this doc):** the fix for unscoped repository queries is a **GlobalAdmin-aware** filter (bypass when `ITenantContext.IsGlobalAdminScope`), not a blanket `OnModelCreating` filter.
- **Phase A test coverage:** tests ship with each feature, not deferred to Phase D. `AsNoTracking` compliance is a Phase A verification gate, not Phase D polish.

---

## Open Threads

- Should audit events be captured via a MediatR pipeline behavior (automatic via `[Audited]` attribute) or explicit `IAuditService` calls inside handlers? Trade-off: invisible vs explicit. (v1 scope is decided — see Decisions; the attribute-vs-explicit mechanism remains open.)
- Authorization rigor mechanism — `[RequirePermission(Permission.X)]` attribute + action filter, or MediatR pipeline behavior checking against `ICurrentUser`? The attribute is more visible; the pipeline is more testable. (Layering intent is decided — see Decisions; the mechanism remains open.)
- `ITenantContext` currently exposes only `CurrentIncubatorId`. Should it also carry `CurrentProjectId` + `IsGlobalAdminScope` to simplify handler authorization and make the GlobalAdmin bypass in tenant filters first-class? Risk: overloading a simple abstraction.
- Should Subscription enforce FormTemplate tier filtering at the query layer (hides templates) or at the clone handler (blocks clone with a clear error)? Query-layer hides friction but may surprise admins.
- Sponsor role UX — which dashboards render for Sponsor? Spec says "project dashboards and reports" but doesn't enumerate. Needs a brainstorm before building.
- Scheduling engine (FR-028) — rule-based algorithm vs constraint solver? Likely rule-based given SC-004 (<5s response), but needs design session.
- Answer-correction triggers mentoring plan regeneration? Spec says no (plan is a snapshot). But UX should at least surface a "plan may be stale" badge — worth confirming.
- Phase A-α bundling — one spec "Phase A access + controller security polish" covering R-SEC-1/2/3 + FR-053/056 in a single PR, or split per FR? Single spec is cheaper to ship but larger to review.

---

## Pointers to Seed Documents

- [Knowledge module (US3) seed](./07-knowledge-module.md) — hot stream of Phase A
- [Mentoring Plan (US4) seed](./08-mentoring-plan.md) — hot stream of Phase B
- [Mentoring Execution (US5) seed](./09-mentoring-execution.md) — hot streams of Phase C
- [Cross-cutting — Subscription + Notification + Audit seed](./10-cross-cutting-hardening.md) — warm streams across Phases A–B
