# Brainstorm: Cross-cutting Infrastructure Code Audit (Phase 0 Quick Wins)

**Date:** 2026-04-20
**Status:** spec-created
**Spec:** specs/017-crosscut-hardening-phase0/

## Problem Framing

A structured Staff-Engineer-level audit was requested on the Mentoory codebase. Scope was narrowed to **cross-cutting infrastructure** (Shared.Application, Shared.Domain, Shared.Infrastructure, Web cross-cutting, and one consumer module — Access — as case study). Brainstorm #06 had already covered feature-completeness gaps; this audit ran the orthogonal lens of **code/design quality**.

The platform is a disciplined codebase. The constitution is largely followed, Clean Architecture boundaries hold, and test/DI patterns are consistent across modules. However, the audit surfaced a set of **high-leverage, low-risk quick wins** — cross-cutting debt that is named but not scheduled, and that will compound as the next four modules (Knowledge, Mentoring, Notification, Subscription) land on top of it.

## Approaches Considered

### A: Full platform sweep

- Pros: Widest coverage.
- Cons: Generic findings; each finding less concrete; diffuses effort.

### B: Cross-cutting infrastructure (selected)

- Pros: Problems here replicate across every module; fixing them pays compounding interest. Next four modules consume this surface directly.
- Cons: Requires parallel exploration of multiple projects; evidence pack is larger.

### C: Single module deep dive

- Pros: Concrete per-finding; catches module-specific debt.
- Cons: Only applies to one module; misses systemic issues.

### D: Web/frontend layer

- Pros: Recent heavy investment (specs 010–013); likely where patterns have ossified.
- Cons: Much of the pain here is already being worked; audit ROI lower than B.

**Choice:** B — cross-cutting.

## Bundling Decision

The audit produced 13 concrete findings. Decision: bundle the **seven quick wins** (QW-1 through QW-7) into a single spec + single PR, and defer the three strategic items (Outbox, Audit pipeline, Permission layer) to their own dedicated specs that were already seeded in brainstorm #10.

Rationale for bundling:

- Each quick win is too small for a standalone spec-kit cycle.
- All seven share a single PR blast radius (code quality + constitution enforcement), single rollback boundary, single reviewer mental model.
- Landing architecture tests (QW-4) first acts as a gate for the subsequent strategic specs.
- Avoids 7× the spec-kit + PR overhead.

## Audit Findings Summary

### Strengths (validates project health)

- Clean Architecture boundaries hold across all seven modules.
- `Result<T>` + `BaseCommandHandler<T>` + `ValidatorBehavior` enforce a single error-handling contract.
- DDD primitives (`Entity`, `ValueObject`, `SoftDeletableEntity`) correctly modelled with proper equality semantics.
- Multi-tenancy via EF query filters + `ITenantContext` is the right trade-off for this domain.
- Module-level DI extensions (`AddAccessApplication()`, `AddAccessInfrastructure()`) are consistent and discoverable.
- Test frameworks (xUnit, Moq, FluentAssertions) are consistent across all test projects that exist.
- Integration test infrastructure uses Testcontainers.MsSql + Respawn; E2E uses Playwright — appropriate tooling choices.

### Critical Issues (addressed by spec 017)

- **CI-A Audit infrastructure exists but has zero callers.** `IAuditService` is wired but no handler calls `LogAsync`. *(Deferred to dedicated Audit spec — brainstorm #10. This audit bundle does not attempt to retrofit.)*
- **CI-B No outbox despite committed design.** `SharedAbstractDbContext.SaveEntitiesAsync` publishes events inline post-commit; failures silently lose events. *(Deferred to dedicated Outbox spec.)*
- **CI-C Domain depends on MediatR.** `Entity.cs` references `INotification`. Constitutional §I violation. → **QW-2 in spec 017.**
- **CI-D Role strings duplicated between menu config and `[Authorize]`.** No central constants. → **QW-1 in spec 017.**
- **CI-E `TenantContextMiddleware` casts `ITenantContext` to concrete type.** Silent-failure risk on the tenant boundary. → **QW-3 in spec 017.**
- **CI-F Modules with zero tests.** Knowledge, Mentoring, Notification, Subscription. → **QW-6 in spec 017.**
- **CI-G No architecture tests.** Constitution enforced only by human review. → **QW-4 in spec 017.**

### Refactoring Opportunities (partially addressed)

- **RO-1** `ValidatorBehavior` reflection-based factory. → **QW-5 in spec 017.**
- **RO-2** Namespace-parsing `DbContextFactory`. *(Deferred; own spec.)*
- **RO-3** Introduce `BaseController`. *(Deferred; driver not yet strong enough.)*
- **RO-4** Migrate custom `ITimeProvider` to `System.TimeProvider`. *(Deferred; mechanical.)*
- **RO-5** Extract `Mentoory.Contracts` assembly. *(Deferred; own brainstorm.)*

### Ship-violation fixes

- **SV-1** `ProjectsController` missing `ProjectCoordinator` role. → **QW-7 in spec 017.**
- **SV-2** `RegisterUser` field-keyed error enumeration. → **QW-7 in spec 017.**

## Decision

Create **spec 017-crosscut-hardening-phase0** covering QW-1 through QW-7 as a bundled hardening feature. Defer the three strategic items (Outbox, Audit pipeline, Permission layer) to their own specs, each driven by brainstorm #10 which already holds their decided designs.

**Recommended spec order after 017:**

1. **Outbox** (unblocks Notification).
2. **Audit pipeline + `[Audited]` attribute** (unlocks compliance; uses outbox pattern).
3. **Permission layer** (`CheckPermissionQuery` + `[RequirePermission]`, additive to `[Authorize]`).

## Open Threads

- **OQ-1** CI rule failing the build when any `Mentoory.*.Tests` project has zero tests — follow-up PR.
- **OQ-2** `RegisterUser` response-time equalization (timing-based enumeration) — follow-up spec.
- **OQ-3** *(Resolved during spec drafting)* — `Roles.cs` lives in `Shared.Application/Authorization/`, not Domain.
- **OT-1** Namespace-parsing `DbContextFactory` rewrite — own spec.
- **OT-2** `BaseController` introduction — defer until the driver is stronger than convenience.
- **OT-3** `System.TimeProvider` migration — fold into a later dependency-upgrade pass.
- **OT-4** `Mentoory.Contracts` extraction — own brainstorm session.
- **OT-5** Correlation-ID propagation into `AuditEntry` and outbox rows (Aspire/OTel integration) — design during Audit spec.

## References

- Constitution: `.specify/memory/constitution.md` (v1.1.1)
- Access-security constitution: `.specify/memory/access-security-constitution.md`
- Prior roadmap/gap-analysis brainstorm: `brainstorm/06-platform-roadmap-gap-analysis.md`
- Cross-cutting design seeds: `brainstorm/10-cross-cutting-hardening.md`
- Spec: `specs/017-crosscut-hardening-phase0/spec.md`
