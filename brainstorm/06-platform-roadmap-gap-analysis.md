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

1. `RegisterUserHandler` returns distinct messages for national-ID vs email conflicts → violates **FR-052** (public-endpoint enumeration prevention)
2. Admin-enrollment controller flow with specific-field feedback (**FR-053**) not wired
3. `RegisterUserValidator` missing password-must-not-contain-email-or-ID rule (**FR-056**)
4. Controllers use `[Authorize(Roles=...)]` only; none call `CheckPermission` query → **FR-010** granular module/action/resource RBAC not actually enforced at the controller layer
5. Handlers accept `ProjectExternalId` without validating it belongs to the user's active incubator → **FR-011/012** cross-tenant URL-manipulation risk
6. `CorrectAnswerHandler` doesn't emit audit events → **FR-018b** audit requirement unmet
7. `CreateProjectHandler` doesn't enforce subscription project limit → **FR-034** unenforced
8. `ProjectCoordinator` role missing from `ProjectsController` `[Authorize]` attribute
9. `Sponsor` role defined but not exposed via any controller → US1 Sponsor read-only scope unmet
10. `RoleAssignmentRepository.Query()` returns `AsNoTracking()` without a mandatory tenant filter — potential leakage if a caller forgets the filter

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
| **A-α** | **Access security polish** — generic public-endpoint errors; admin-enrollment controller with specific feedback; password-contains-ID validator | AI worktree | FR-052, FR-053, FR-056 | Low — localized to Access module |
| **A-β** | **Audit pipeline wiring** — MediatR `AuditingBehavior`; attribute-based opt-in; retrofit CorrectAnswer, SetActiveContext, AdvanceStage, plan-approval handlers | AI worktree | FR-045, FR-018b | Medium — contract design for audit payload |
| **A-γ** | **Lifecycle finish (US7)** — `AdvanceProjectStageCommand` + handler, Coordinator UI, stage-gated action filters on UI | AI worktree | FR-037, FR-039 | Low — domain exists |

**Exit criteria for Phase A:**
- Knowledge module CRUD functional end-to-end (template + project clone + customization)
- All public registration paths return identical generic errors; admin enrollment returns specific feedback
- At least 5 sensitive commands emit audit events; audit log queryable in Administration area
- Coordinator can advance a project through stages via UI; wrong-stage actions are blocked

### Phase B — Platform capability completion in parallel (~2–3 weeks)

| Stream | Scope | Owner | FRs | Risk |
|--------|-------|-------|-----|------|
| **B-hot** | **Mentoring Plan (US4)** — `MentoringPlan` aggregate; `DiagnosticCompletedEvent` subscriber that builds cumulative topic scores → priority mapping → suggested plan; collaborative adjust/approve workflow; mentor + entrepreneur UI | Human + Claude | FR-025, 025a, 025b, 025c, FR-026, FR-027 | High — cross-module integration + core product logic |
| **B-α** | **Subscription module (US6)** — versioned plans; positive-only accumulative overrides; effective-plan query; `CreateProjectHandler` gate; FormTemplate filtering by subscription tier | AI worktree | FR-032–035, FR-034 enforcement, FR-014 subscription-tier filter | Medium — integration surface in Tenant + Diagnostic |
| **B-β** | **Notification engine (US8)** — `NotificationOutbox` pattern with shared transaction; MailKit channel adapter; immediate + scheduled dispatch; dedup by event key; preferences stub | AI worktree | FR-040–044 | Medium — outbox correctness + dedup semantics |
| **B-γ** | **Tabler migration closeout (spec 009)** — `_Layout.cshtml` → navbar-vertical; auth split-panel layout; icon webfont conversion | AI worktree | spec 009 DoD | Low — UI-only |

**Exit criteria for Phase B:**
- Completing a diagnostic triggers suggested plan generation; mentor + entrepreneur can adjust and approve
- Creating a project past subscription limit is blocked with clear UX
- Notification outbox delivers a test email; scheduled notification fires on cron
- All layouts use Tabler patterns; no pre-Tabler templates remain

### Phase C — Execution + rigor (~2–3 weeks)

| Stream | Scope | Owner | FRs | Risk |
|--------|-------|-------|-----|------|
| **C-hot-1** | **Scheduling engine + Sessions (US5.a)** — calendar generator from plan + scheduling params; session scheduling/logging UI; flexible topic coverage; session audit log | Human + Claude | FR-028, 029, 030 | Medium-High — algorithm design |
| **C-hot-2** | **Assignment workflow (US5.b)** — creation/submission/review/feedback; notification events | Human + Claude (after C-hot-1) | FR-031 | Medium — integrates with Notification |
| **C-α** | **Authorization rigor** — controller-level `CheckPermission` enforcement via action filter / attribute; resource-ownership guard via MediatR pipeline behavior; Sponsor role surfaced with read-only dashboards; Coordinator on Projects | AI worktree | FR-010, 011, 012 deep | Medium — security-sensitive; thorough test coverage required |
| **C-β** | **E2E test coverage for US1–5 golden paths** — register → verify → context → diagnostic → plan → session → assignment; cross-tenant isolation tests; subscription-limit tests | AI worktree | SC-005, SC-006 | Low — test-only |

**Exit criteria for Phase C:**
- End-to-end journey works: registration through assignment feedback
- Every controller action validates permission against active context on the server
- ≥3 golden-path E2E tests pass reliably; cross-tenant isolation verified

### Phase D — Production readiness (~1–2 weeks)

| Stream | Scope | Targets |
|--------|-------|---------|
| **D-hot** | Sponsor read-only dashboards; notification preferences full UI; edge-case coverage from spec 001 | FR-003 Sponsor, FR-043 full, spec 001 edge cases |
| **D-α** | Performance hardening — `AsNoTracking` audit, `Include()` bloat review, query plans; aim SC-009 (p95 <200ms @ 50 incubators × 100 projects) | SC-009 |
| **D-β** | Domain unit test coverage for Knowledge/Mentoring/Notification/Subscription | Close the zero-test gap |

---

## Quality & Integration Risk Register

These are the seams where parallel work most likely collides or where correctness is hardest:

1. **Knowledge Topic ↔ Diagnostic Question FK (FR-017):** today `Questions.TopicId` is a `BIGINT NOT NULL` with no target table. When A-hot creates `knowledge.Topics`, add the FK and data-seed existing test fixtures. One migration, one integration test.

2. **`DiagnosticCompletedEvent` → Mentoring Plan generation:** the event exists in Diagnostic but has no subscriber. Design the event contract (`ProjectId`, `UserId`, `EvaluationStage`, `CompletedAtUtc`) explicitly before Phase B.

3. **Subscription limit enforcement in Tenant:** `CreateProjectHandler` needs an interface in `Shared.Application` for "effective features" query; Subscription implements it. Tenant must not depend on Subscription.

4. **AuditingBehavior retrofit:** once the pipeline behavior lands (A-β), every new handler must opt in. Consider an architecture test that asserts handlers for sensitive commands have the `[Audited]` attribute.

5. **Notification outbox transactional consistency:** outbox writes must share the DbContext transaction with the triggering handler — otherwise dual-write consistency breaks. This needs a `SaveChanges` interceptor or an explicit outbox repository that participates in the UoW.

6. **Cross-tenant leakage risk in repository query methods:** `RoleAssignmentRepository.Query()` is the canonical smell. Add EF Core query filters in `OnModelCreating` that scope all tenant-bound entities to `CurrentIncubatorId`, and an integration test that fails if a query without the filter runs.

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

- Phase A starts with Knowledge as the hot stream; 3 warm AI streams handle hardening, audit wiring, and lifecycle finish in parallel.
- Each phase has explicit exit criteria; do not start the next phase until the hot stream's exit criteria are met.
- Per-feature seed documents for the four biggest upcoming pieces are captured in `07-knowledge-module.md`, `08-mentoring-plan.md`, `09-mentoring-execution.md`, `10-cross-cutting-hardening.md`. Each is parked pending a focused `/spex:brainstorm` when the stream is scheduled.
- Phase A hardening streams (A-α Access polish, A-γ Lifecycle finish) are small enough to go straight to `/speckit-specify` without a dedicated brainstorm.
- Phase A-β Audit pipeline design is moderately non-trivial; recommend a quick brainstorm before specify (captured as an open thread below).

---

## Open Threads

- Should audit events be captured via a MediatR pipeline behavior (automatic via `[Audited]` attribute) or explicit `IAuditService` calls inside handlers? Trade-off: invisible vs explicit.
- Outbox pattern library choice — hand-rolled, or adopt an established library? The constitution prefers minimalism, but dual-write correctness is subtle.
- Authorization rigor approach — `[RequirePermission(Permission.X)]` attribute + action filter, or MediatR pipeline behavior checking against `ICurrentUser`? The attribute is more visible; the pipeline is more testable.
- `ITenantContext` currently exposes only `CurrentIncubatorId`. Should it also carry `CurrentProjectId` to simplify handler authorization? Risk: overloading a simple abstraction.
- Should Subscription enforce FormTemplate tier filtering at the query layer (hides templates) or at the clone handler (blocks clone with a clear error)? Query-layer hides friction but may surprise admins.
- Sponsor role UX — which dashboards render for Sponsor? Spec says "project dashboards and reports" but doesn't enumerate. Needs a brainstorm before building.
- Scheduling engine (FR-028) — rule-based algorithm vs constraint solver? Likely rule-based given SC-004 (<5s response), but needs design session.
- Answer-correction triggers mentoring plan regeneration? Spec says no (plan is a snapshot). But UX should at least surface a "plan may be stale" badge — worth confirming.
- Phase A hardening items bundled vs per-FR specs? Small items (FR-052, FR-056) could share a single spec "Phase A auth polish" — cheaper to ship one PR than three.

---

## Pointers to Seed Documents

- [Knowledge module (US3) seed](./07-knowledge-module.md) — hot stream of Phase A
- [Mentoring Plan (US4) seed](./08-mentoring-plan.md) — hot stream of Phase B
- [Mentoring Execution (US5) seed](./09-mentoring-execution.md) — hot streams of Phase C
- [Cross-cutting — Subscription + Notification + Audit seed](./10-cross-cutting-hardening.md) — warm streams across Phases A–B
