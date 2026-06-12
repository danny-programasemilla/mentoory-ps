# Brainstorm Overview

Last updated: 2026-06-11 (#21 form-error-feedback → shipped via PR #28; app-wide inline form-error pattern — remove the duplicating validation summary, global field CSS (danger border + persistent glow + inside-right alert-circle icon), page-level errors surface as toasts; rollout in waves CRUD → auth → atypical; prototype approved on Incubators/Create)

## Sessions

| # | Date | Topic | Status | Spec |
|---|------|-------|--------|------|
| 01 | 2026-04-11 | context-selector-ux | spec-created | 008 |
| 02 | 2026-04-13 | tabler-template-migration | spec-created | 009 |
| 03 | 2026-04-13 | design-system-ux-polish | spec-created | 010, 011 |
| 04 | 2026-04-13 | table-polish | spec-created | 012 |
| 05 | 2026-04-13 | table-filtering | spec-created | 013 |
| 06 | 2026-04-18 | platform-roadmap-gap-analysis | active | - |
| 07 | 2026-04-18 | knowledge-module | spec-created (revisited 2026-04-18) | 016 |
| 08 | 2026-04-18 | mentoring-plan | parked | - |
| 09 | 2026-04-18 | mentoring-execution | parked | - |
| 10 | 2026-04-18 | cross-cutting-hardening | parked | - |
| 11 | 2026-04-19 | e2e-quality-gate | spec-created | 018 |
| 12 | 2026-04-19 | e2e-lifecycle-coverage | spec-created | 016/e2e |
| 13 | 2026-04-18 | audit-pipeline | spec-created | 016 |
| 14 | 2026-04-19 | audit-e2e | spec-created | 017 |
| 15 | 2026-04-19 | knowledge-module-binding-redesign | spec-created | 016 (amended) |
| 16 | 2026-04-25 | integration-soak-bundle | spec-shipped | 019 (PR #15, squash `fbd5ac6`) |
| 17 | 2026-05-22 | sidebar-context-footer | spec-created | 020 |
| 18 | 2026-05-23 | themed-header-band | spec-created | 021 |
| 19 | 2026-06-10 | page-content-banner | active | - |
| 20 | 2026-06-11 | page-banner-redesign | spec-created | 024 (PR #27) |
| 21 | 2026-06-11 | form-error-feedback | active | 024-form-error-feedback (PR #28) |

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
- Knowledge `TopicPriorityRangesChanged` outbox upgrade when Mentoring Plan consumer lands (from #07 revisit)
- Knowledge topic score normalization (0–100 vs raw) — verify against `GetTopicScoreAggregationHandler` during `/speckit-plan` (from #07 revisit)
- Knowledge concurrent-edit semantics on project clones (from #07 revisit)
- Cross-module transaction semantics for project-creation-plus-KS-materialization — shared `DbContextTransaction` vs transient NULL window (from #15)
- Fate of `CloneKnowledgeStructureTemplateCommand` after the binding redesign — keep as internal/Tenant-side entry point or fold into domain factory (from #15)
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
- Bundle PR merge style — regular merge commit vs squash (from #16 — RESOLVED: forced to squash by repo ruleset on develop; FR-007 deviation recorded in PR #15 squash commit body and spec 019 tasks.md T032)
- Bundle PR description — verbatim source PR bodies vs cross-reference (from #16 — RESOLVED: cross-reference per `contracts/gate-commands.md` G-SHIP-4; source PRs #11–#14 closed with "Closed in favor of bundle ship #15" comments)
- Registration branch standalone build verification — coverage-check tests may not compile without production project (from #16 — RESOLVED: standalone strict-mode failed with 33 unclaimed identifiers on registration #13; release-manager elected Path B / warn-mode override per FR-005; see `logs/branch-eligibility.txt` and spec 019 followups FU-1)
- Follow-up bundle gating on coverage-check once feature 018 implementation lands (from #16 — partially RESOLVED: integrated bundle build runs `-p:CoverageCheckMode=warn` per FR-005 with zero unclaimed identifiers; full strict-mode return chartered as spec 019 followup FU-1, T053)
- Switchability detection — cheapest reliable way to know "user has > 1 selectable context" at layout render: claim set at sign-in vs cached value vs always-clickable fallback (from #17, deferred to `/speckit-plan`)
- Card contrast on dark sidebar — optionally quantify against WCAG AA ≥ 4.5:1 (from #17)
- Mobile switch reach — with the avatar-dropdown switch path removed, switching on small screens requires opening the hamburger and tapping the card; confirm acceptable in UX review (from #17)
- Header band vertical size / SVG canvas dimensions — deferred to `/speckit-plan` (from #18)
- Header band background tint: per-theme colored wash vs. single neutral wash (from #18)
- Exact section→theme routing table (`Sponsor`→`personas`; `Configuration`/`BatchUpload`→`default`) — finalize in planning (from #18)
- Strictly-static band vs. later one-shot reduced-motion-aware entrance (from #18)
- Icon + gradient key computation: extend 021's `HeaderTheme` resolver vs. sibling helper (from #19)
- Complete action→icon map + default glyph for unmapped actions (from #19)
- Heading hierarchy when the page title relocates into the strip (from #19)
- Small-screen behavior: drop watermark icon below `md` (like 021) vs. keep full strip (from #19)
- Per-view override of strip icon/subtitle vs. route-derived mapping only (from #19)
- Exact final banner height within ~88–100px and how the SVG art scales to fill it (from #20)
- Per-area palette: reuse exact 021/023 section colors vs. refresh for the bolder treatment (from #20)
- Specific geometric style per area (chevron/diagonal/mosaic/slash) — bespoke vs. shared vocabulary (from #20)
- SVG delivery: inline `<svg>` vs `<img>` vs CSS `background-image`; recolor in-file vs CSS vars (from #20)
- Responsive <md behavior for richer SVG art: hide art / simplify to color band / keep full art (from #20)
- Guaranteeing the title's dark-on-light region stays clean where art meets the title column, AA across all areas (from #20)
- Print + reduced-motion behavior for the SVG art (from #20)
- Same-request toast mechanism for non-field ModelState errors — on-load script vs data attribute vs `_Layout` partial (TempData only covers the redirect case) (from #21)
- `_AuthLayout` toast container/component presence for Wave 2 — Login "invalid credentials" is a page-level error that must toast (from #21)
- `<select>` fields: inside-right error icon vs native dropdown arrow overlap (from #21)
- Atypical forms (Wave 3): icon placement on file inputs (BatchUpload), per-question error display for the dynamic Participant Diagnostic (from #21)
- Form-error accessibility: `aria-invalid` on fields, `role="alert"`/`aria-live` for messages + toasts (from #21)
- Error icon rendering: CSS background-image alert-circle (approved) vs real Tabler icon element for selects/file inputs (from #21)

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

- **Mentoring Plan (US4)** (#08) — diagnostic scoring → priority plan. Reason: depends on Knowledge module; brainstorm at start of Phase B hot stream.
- **Mentoring Execution (US5)** (#09) — scheduling + sessions + assignments. Reason: depends on Mentoring Plan; consider splitting into two specs. Brainstorm at start of Phase C.
- **Cross-cutting Subscription + Notification** (#10) — Subscription (Section 1) and Notification (Section 2) remain as warm streams. Reason: Section 3 (Audit) was extracted into spec 016 via brainstorm #11; Subscription + Notification still queued as AI-driven warm work.
