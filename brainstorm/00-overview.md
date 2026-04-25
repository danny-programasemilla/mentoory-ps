# Brainstorm Overview

Last updated: 2026-04-19 (bundle 019 — combined #11 e2e-quality-gate → 018, #12 e2e-lifecycle-coverage → 016/e2e, #13 audit-pipeline → 016, #14 audit-e2e → 017; renumbered audit sessions from #11/#12 to #13/#14 to avoid collision; intra-document references "(from #11)/(from #12)" carrying audit context have been retargeted to "(from #13)/(from #14)")

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
| 12 | 2026-04-19 | e2e-lifecycle-coverage | spec-created | 016/e2e |
| 13 | 2026-04-18 | audit-pipeline | spec-created | 016 |
| 14 | 2026-04-19 | audit-e2e | spec-created | 017 |

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
- Audit events mechanism: `[Audited]` attribute pipeline behavior vs explicit `IAuditService` calls (from #06 — RESOLVED in #13 as hybrid; see spec 016)
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
- Cross-cutting ship order: Subscription first vs Audit first (from #10 — now effectively "Audit first" since spec 016 exists)
- `Coverage: N/A` escape hatch — require reviewer approval per occurrence, or is inline justification sufficient? (from #11, OQ-001)
- Coverage tool project path — `tools/`, `build/`, or `specs/tooling/`? (from #11, OQ-002)
- 50-probe sweep count — configurable for nightly deep-run? (from #11, OQ-003)
- Floor-category enforcement — opt-in via `access-security: true` vs heuristic-detected? (from #11)
- Retroactive trait retrofit for features 001–015 — organic migration expected (from #11)
- PlaywrightFixture extensibility assumption may fail at C0 (from #12)
- Test #3 broken-state fixture: pull-weight vs redundant with unit coverage (from #12)
- Concurrency UI test mechanism: parallel contexts vs test-time stale-advance helper (from #12)
- Nested-object redaction depth for audit payloads (from #13)
- Correlation-id propagation from hosted-service background jobs (from #13)
- Audit retention / TTL policy (from #13)
- Sensitive-action regex completeness — add `Delete*`, `Revoke*`, `Reset*` stems? (from #13)
- Priority ordering US3 (correlation) vs US4 (correction detail) in spec 016 (from #13)
- `RESUME-COMPLETE.md` format — Markdown (current) vs structured JSON for a possible CI gate (from #14)
- Phase-boundary philosophy: user-story-aligned (current) vs implementation-complexity ramp (from #14)
- Spanish-copy assertions distributed across P1+P4 vs consolidated into one file (from #14)
- SC-008 "bootstrap within 5 tool calls" — novel metric, possibly reframe as "reads no files beyond spec + RESUME" (from #14)
- `PlaywrightFixture` respawn of the `audit` schema is unverified — flagged as P1's first concrete task in FR-016 (from #14)

## Decisions Ratified During Review

Decisions that were open during earlier sessions and resolved later. See the individual seed docs for full rationale.

- **Outbox pattern** (#10): `SaveChangesInterceptor` per module DbContext, same-transaction outbox writes. Dual-write rejected.
- **Audit v1 scope** (#10): command type + payload (redacted) + user/tenant/project/role context + correlation + outcome. Entity diffs out of scope.
- **Topic identity in Question** (#07): project-form questions reference cloned topics; template questions reference template topics.
- **Authorization layering** (#06): `CheckPermission` is ADDITIVE to `[Authorize]` role guards, not a replacement.
- **Tenant query filters** (#06): GlobalAdmin-aware filters with bypass, NOT blanket `OnModelCreating` filters.
- **Phase placement of tests and `AsNoTracking` audit** (#06): tests ship with each feature; `AsNoTracking` compliance is a Phase A gate.
- **Audit capture mechanism** (#13): hybrid `[Audited]` attribute + `AuditingBehavior` + `Mode = Manual` escape hatch. Schema extended with typed columns (CorrelationId, Outcome, ExceptionType, UserEmail, RoleContext). Architecture-test enforcement + governance rule in `access-security-constitution.md`. Five shipped commands retrofitted (SetActiveContext, AssignRole, RegisterUser, LoginUser, CorrectAnswer-Manual). IP + correlation read through `ICorrelationContext` / `IRequestContext` abstraction, never `IHttpContextAccessor` directly.
- **E2E phasing protocol** (#14): four phases mapped to 016 user stories, each ending in a mandatory commit + push + context-clear + resume-prompt handoff. Resume prompts carry duplicated Invariants blocks (intentional redundancy as drift signal). Phase N+1 MUST bootstrap by reading spec + latest RESUME file, pulling latest, and verifying prior phase's green baseline before writing new tests.

## Parked Ideas

- **Knowledge module (US3)** (#07) — full hierarchical learning content domain. Reason: first focused brainstorm scheduled at start of Phase A hot stream.
- **Mentoring Plan (US4)** (#08) — diagnostic scoring → priority plan. Reason: depends on Knowledge module; brainstorm at start of Phase B hot stream.
- **Mentoring Execution (US5)** (#09) — scheduling + sessions + assignments. Reason: depends on Mentoring Plan; consider splitting into two specs. Brainstorm at start of Phase C.
- **Cross-cutting Subscription + Notification** (#10) — Subscription (Section 1) and Notification (Section 2) remain as warm streams. Reason: Section 3 (Audit) was extracted into spec 016 via brainstorm #11; Subscription + Notification still queued as AI-driven warm work.
