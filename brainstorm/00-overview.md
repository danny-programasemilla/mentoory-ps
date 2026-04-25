# Brainstorm Overview

Last updated: 2026-04-19

## Sessions

| # | Date | Topic | Status | Spec |
|---|------|-------|--------|------|
| 01 | 2026-04-11 | context-selector-ux | spec-created | 008 |
| 02 | 2026-04-13 | tabler-template-migration | spec-created | 009 |
| 03 | 2026-04-13 | design-system-ux-polish | spec-created | 010, 011 |
| 04 | 2026-04-13 | table-polish | spec-created | 012 |
| 05 | 2026-04-13 | table-filtering | spec-created | 013 |
| 06 | 2026-04-18 | platform-roadmap-gap-analysis | active | - |
| 07 | 2026-04-18 | knowledge-module | parked | - |
| 08 | 2026-04-18 | mentoring-plan | parked | - |
| 09 | 2026-04-18 | mentoring-execution | parked | - |
| 10 | 2026-04-18 | cross-cutting-hardening | parked | - |
| 11 | 2026-04-19 | e2e-quality-gate | spec-created | 018 |

## Open Threads

- GlobalAdmin incubator dropdown may need search/filter at scale (from #01)
- Tabler Icons rendering approach: inline SVG vs webfont (from #02)
- Logo SVG delivery mechanism: extract from PDF or create fresh? (from #03)
- CSS variable prefix: --mentory- vs --mentoory- (from #03)
- Global --tblr-primary override impact on non-primary Tabler components (from #03)
- Global card shadow/hover styles may need tuning after visual QA (from #03 revisit)
- Should the icon registry be extensible by individual views? (from #04)
- Consider standardizing date formatting across tables in a follow-up (from #04)
- Filter panel animation: CSS transitions vs Bootstrap collapse (from #05)
- URL param namespacing strategy to avoid conflicts with existing query params (from #05)
- Filter panel layout on tables with 7+ filterable columns (from #05)
- Audit events mechanism: `[Audited]` attribute pipeline behavior vs explicit `IAuditService` calls (from #06; v1 payload is decided — see #10)
- Authorization rigor mechanism: `[RequirePermission]` attribute vs pipeline behavior (from #06; layering intent is decided — additive to `[Authorize]`)
- `ITenantContext` should also carry `CurrentProjectId` + `IsGlobalAdminScope`? (from #06)
- Subscription FormTemplate tier filtering at query vs clone handler (from #06)
- Sponsor dashboards — which views exactly? (from #06)
- Scheduling engine (FR-028) — rule-based vs constraint solver (from #06)
- Answer-correction regenerates plan vs "stale plan" badge UX (from #06)
- Phase A-α bundling: one PR for R-SEC-1/2/3 + FR-053/056 vs per-FR specs (from #06)
- Knowledge clone depth (deep vs reference) (from #07)
- Knowledge partial sync semantics at module/topic/subject/resource levels (from #07)
- Resource file storage approach — URL only vs blob (from #07)
- SWOT/ODSR summary algorithm per topic (from #08)
- Mentoring plan snapshot scope — does it copy topic priority ranges too? (from #08)
- Mentor/entrepreneur concurrent-edit policy (from #08)
- Low-priority topics default selection state in UI (from #08)
- Mentoring execution: split Spec A (Scheduling+Sessions) + Spec B (Assignments) vs single spec (from #09)
- Scheduling engine: rule-based vs constraint-solver (from #09)
- Assignment submission file storage decisions (from #09)
- Session cancellation propagation to notification outbox (from #09)
- Subscription feature schema: typed vs bag-of-strings (from #10)
- Subscription override value semantics for booleans + "unlimited" quantitative (from #10)
- Notification scheduled-dispatch mechanism — hosted service vs library (from #10; v1 recommendation: hosted service)
- Notification template engine — inline vs Razor vs Scriban (from #10)
- Audit pipeline mechanism — attribute-based vs explicit calls (from #10; v1 payload scope is decided)
- Cross-cutting ship order: Subscription first vs Audit first (from #10)
- `Coverage: N/A` escape hatch — require reviewer approval per occurrence, or is inline justification sufficient? (from #11, OQ-001)
- Coverage tool project path — `tools/`, `build/`, or `specs/tooling/`? (from #11, OQ-002)
- 50-probe sweep count — configurable for nightly deep-run? (from #11, OQ-003)
- Floor-category enforcement — opt-in via `access-security: true` vs heuristic-detected? (from #11)
- Retroactive trait retrofit for features 001–015 — organic migration expected (from #11)

## Decisions Ratified During Review

Decisions that were open during the initial roadmap (#06) and resolved via the PR #10 code-review loop. See the individual seed docs for full rationale.

- **Outbox pattern** (#10): `SaveChangesInterceptor` per module DbContext, same-transaction outbox writes. Dual-write rejected.
- **Audit v1 scope** (#10): command type + payload (redacted) + user/tenant/project/role context + correlation + outcome. Entity diffs out of scope.
- **Topic identity in Question** (#07): project-form questions reference cloned topics; template questions reference template topics.
- **Authorization layering** (#06): `CheckPermission` is ADDITIVE to `[Authorize]` role guards, not a replacement.
- **Tenant query filters** (#06): GlobalAdmin-aware filters with bypass, NOT blanket `OnModelCreating` filters.
- **Phase placement of tests and `AsNoTracking` audit** (#06): tests ship with each feature; `AsNoTracking` compliance is a Phase A gate.

## Parked Ideas

- **Knowledge module (US3)** (#07) — full hierarchical learning content domain. Reason: first focused brainstorm scheduled at start of Phase A hot stream.
- **Mentoring Plan (US4)** (#08) — diagnostic scoring → priority plan. Reason: depends on Knowledge module; brainstorm at start of Phase B hot stream.
- **Mentoring Execution (US5)** (#09) — scheduling + sessions + assignments. Reason: depends on Mentoring Plan; consider splitting into two specs. Brainstorm at start of Phase C.
- **Cross-cutting Subscription + Notification + Audit** (#10) — three independent-but-related warm streams for Phases A–B. Reason: queued as AI-driven warm work alongside hot feature streams.
