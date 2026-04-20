# Coverage Matrix — E2E for feature 016

Populated incrementally by each chunk. One row per scenario in `specs/016-project-lifecycle-finish/spec.md` plus one row per non-automatable success criterion (explicit "not covered" is a valid row).

## Status legend

- ✅ Covered — test exists, in-suite green
- 🚧 Pending — planned for a later chunk
- ⏭️ Not covered (by design) — with rationale
- 🛑 Skipped (blocked) — test written but `[Fact(Skip=...)]` pending product-code decision

## Rows

| # | Parent spec ref | Scenario | Chunk | Test file | Status |
|---|-----------------|----------|-------|-----------|--------|
| — | SC-E-foundation | Fixtures wired correctly | C0 | `LifecycleSmokeTests.Fixtures_CanLoginAsCoordinatorAndOpenCoordinationProjectsList` | ✅ |
| 1 | US1 §1 | Registration→Forms advance via UI | C1 | `WalkthroughAdvanceTests.Coordinator_AdvancesProjectFromRegistrationToForms` | ✅ |
| 2 | US1 §2 | Closure project rejects advance | C1 | `WalkthroughAdvanceTests.Coordinator_CannotAdvanceProjectAtClosure` | ✅ |
| 3 | US1 §3 | Completed stage rejects advance | C1 | `WalkthroughAdvanceTests.Coordinator_CannotAdvanceWhenCurrentStageIsCompleted` | ✅ |
| 4 | US1 §4 | Non-coordinator role blocked | C1 | `WalkthroughAdvanceTests.NonCoordinator_CannotSeeAdvanceButtonOrInvokeAdvance` | ✅ |
| 5 | US2 §1 | Mid-lifecycle rendering | C2 | `WalkthroughLifecyclePageTests.Lifecycle_MidProject_RegistrationCompleted_FormsInProgress` | ✅ |
| 6 | US2 §2 | Brand-new project rendering | C2 | `WalkthroughLifecyclePageTests.Lifecycle_BrandNewProject_OnlyRegistrationInProgress` | ✅ |
| 7 | US2 §3 | Closed project rendering | C2 | `WalkthroughLifecyclePageTests.Lifecycle_ClosedProject_PriorStagesCompleted_ClosureInProgress` | ✅ (see note A) |
| 8 | US3 §1 | Registration → all 6 cards Locked | C3 | `WalkthroughGatedActionsTests.GatedActions_RegistrationStage_AllSixCardsLocked` | 🚧 |
| 9 | US3 §2 | Cards flip states on advance | C3 | `WalkthroughGatedActionsTests.GatedActions_FlipStatesOnAdvance` | 🚧 |
| 10 | US3 §3 | Direct URL to locked action | C3 | `WalkthroughGatedActionsTests.GatedActions_DirectUrlToLockedAction_RedirectsToLifecycleWithSpanishToast` | 🚧 |
| 11 | US3 §4 | Locked-card tooltip names unlocking stage | C3 | `WalkthroughGatedActionsTests.GatedActions_LockedCardTooltipNamesUnlockingStage` | 🚧 |
| 12 | Edge: cross-incubator | IncubatorAdmin A blocked from B's project | C4 | `WalkthroughRoleScopeTests.IncubatorAdminA_CannotFetchIncubatorBProjectLifecycle` | 🚧 |
| 13 | Edge: no context | Coordinator without context redirected | C4 | `WalkthroughRoleScopeTests.Coordinator_WithoutIncubatorContext_RedirectedToSelector` | 🚧 |
| 14 | US1 §4 (Mentor) | Mentor blocked at controller | C4 | `WalkthroughRoleScopeTests.Mentor_CannotAccessCoordinationProjectsList` | 🚧 |
| 15 | SC-005 | Audit trail — 3 coordinators | C5 | `WalkthroughAuditConcurrencyTests.AuditTrail_ThreeAdvancesByThreeCoordinators_AllNamesVisibleOnLifecyclePage` | 🚧 |
| 16 | Edge: concurrency | Concurrent advance — second shows toast | C5 | `WalkthroughAuditConcurrencyTests.ConcurrentAdvance_SecondAttemptShowsConcurrencyToast` | 🚧 |
| 17 | Edge: inactive | Inactive project advance blocked | C5 | `WalkthroughAuditConcurrencyTests.InactiveProject_AdvanceAttemptShowsInactiveToast` | 🚧 |

## Notes

- **Note A (row 7 — US2 §3).** Parent spec says "all seven stages appear in the completed state with their timestamps." This state is unreachable through the product: `AdvanceProjectStageHandler.cs:48` rejects any 7th advance (`ProjectAlreadyClosed`), so the UI always renders Closure as `En progreso` once reached. The test asserts the renderable reality (6 stages `Completada` + Closure `En progreso` + advance button hidden + `El proyecto ya está en la etapa final (Cierre).` muted text). Spec/product discrepancy logged in `open-questions.md`.

## Success Criteria — automated vs. not

| SC | Automated here? | Rationale |
|-----|-----------------|-----------|
| SC-001 | ✅ via row 1 | Happy-path UI advance covers "advanceable via coordinator UI, no DB tools needed" |
| SC-002 | ⏭️ Not covered | Timing criterion (under 30s) — test-time timing depends on Testcontainers warm-up; not a useful assertion |
| SC-003 | ⏭️ Not covered | Usability (human coordinators) — requires real user study |
| SC-004 | ✅ via rows 8, 10 | Locked-URL rejection proves "zero stage-gated actions executable out-of-stage" |
| SC-005 | ✅ via row 15 | Audit trail visible without navigation |
| SC-006 | ⏭️ Not covered | Usability (human identification) — requires real user study |
| SC-007 | ⏭️ Not covered | Support-ticket metric — operational, not testable |

## Parent-spec Edge Cases — covered vs. parked

| Edge case (from `specs/016-project-lifecycle-finish/spec.md` §Edge Cases) | Coverage |
|---|---|
| Concurrent advance | ✅ row 16 |
| Inactive project | ✅ row 17 |
| Cross-incubator coordinator | ✅ row 12 |
| Legacy stage-gated action pre-feature | ⏭️ Parked to `open-questions.md` (Cannot-test-as-specified) |
| Closure has no next stage | ✅ row 2 + row 7 |
| No incubator context selected | ✅ row 13 |
