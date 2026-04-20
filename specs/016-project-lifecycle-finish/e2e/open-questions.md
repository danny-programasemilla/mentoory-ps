# Open Questions — E2E for feature 016

Execution-time parking lot. Populated as chunks run. **Not** used during the spec phase.

## Cannot-test-as-specified

| Scenario | Reason | Noted by |
|---|---|---|
| Legacy stage-gated action started before feature 016 shipped (spec.md §Edge Cases item 4) | No realistic seed path exists in the current codebase for "a diagnostic response created before the feature existed." Would require either backdated seed data (fragile) or a schema-level change to bypass the aggregate invariant (out-of-scope). | Spec phase (C0 predecessor) |

## Cross-chunk refactoring opportunities

*(Populated when `/simplify` reveals duplication across chunks. Post-suite cleanup pass, not during the chunks themselves.)*

## Product-code issues surfaced during testing

*(Populated if a test reveals a real bug. Each entry: test name, expected behavior, observed behavior, chunk, severity. Triage separately — do NOT fix during the E2E execution per R1/E2.)*

### US3 §1 / §4 — Locked action cards have broken ARIA and tooltip attributes (C3)

- **Tests** (both skipped):
  - `WalkthroughGatedActionsTests.GatedActions_RegistrationStage_AllSixCardsLocked`
  - `WalkthroughGatedActionsTests.GatedActions_LockedCardTooltipNamesUnlockingStage`
- **Expected per parent spec US3 §1/§4 and Lifecycle.cshtml intent**: A locked action card renders `aria-disabled="true"`, `tabindex="-1"`, `data-bs-toggle="tooltip"`, and `data-bs-title="Disponible desde la etapa <Spanish stage name>"`.
- **Observed**: `Mentoory.Web/Areas/Coordination/Views/Projects/Lifecycle.cshtml:145-147` emits the full attribute blob through `@(isLocked ? "aria-disabled=\"true\" ..." : "")`. Razor HTML-encodes the string, so the rendered HTML contains `aria-disabled=&quot;true&quot; tabindex=&quot;-1&quot; data-bs-toggle=&quot;tooltip&quot; data-bs-title=&quot;Disponible desde la etapa Formularios&quot;`. The HTML parser then reads each attribute value as unquoted, stopping at the first whitespace. Net effect:
  - `aria-disabled` value parses as `"true"` (with literal quote chars, 6 chars long).
  - `tabindex` value parses as `"-1"` (with literal quote chars).
  - `data-bs-toggle` value parses as `"tooltip"` (with literal quote chars).
  - `data-bs-title` value parses as `"Disponible` (truncated at whitespace, stage name lost).
- **User-facing impact**:
  - Bootstrap's tooltip initializer looks for `data-bs-toggle="tooltip"` (no literal quotes). Locked cards never get a tooltip in production.
  - Screen readers receive `aria-disabled="true"` with extra quote chars — many ATs treat this as a malformed value and fall through to interactive semantics.
  - Keyboard focus skipping (`tabindex="-1"`) is also malformed; browsers differ on recovery.
- **Severity**: Real accessibility + UX regression on locked action cards, but silent because no existing test asserts these attributes. `ReadActionStateAsync` only checks CSS class (`border-secondary`), which is emitted correctly by `@cardClass`.
- **Resolution options for triage**:
  1. Wrap each `@(...)` in `Html.Raw(...)` so the attribute blob is emitted literally.
  2. Replace the blob with individual Razor conditional attributes (`@(isLocked ? "true" : null)` bound to real attribute names).
  3. Option 2 is the more idiomatic Razor fix and avoids the `Html.Raw` safety caveat since the interpolated value (`action.GatingStageDisplayName`) is static app-controlled Spanish.
- **Current coverage** (C3): two tests kept in the suite as `[Fact(Skip = ...)]` with a pointer back to this entry. When the view is fixed, remove the `Skip` attribute in the same commit.

### US2 §3 — "all seven stages completed" state is unreachable (C2)

- **Test**: `WalkthroughLifecyclePageTests.Lifecycle_ClosedProject_PriorStagesCompleted_ClosureInProgress`
- **Expected per parent spec US2 §3 / FR-010-FR-011**: "all seven stages appear in the completed state with their timestamps."
- **Observed**: Closure stage renders as `En progreso`. `AdvanceProjectStageHandler.cs:48` explicitly rejects a 7th advance (`ProjectAlreadyClosed`), so no UI or command path completes Closure. The domain (`Project.AdvanceStage`) *does* support a 7th advance that would complete Closure — exercised by `Mentoory.Tenant.Tests.Domain.ProjectTests.AdvanceStage_AtFinalStage_StopsInClosureWithCompletedState` — but that path is unreachable from the application layer.
- **Severity**: Spec/product discrepancy, not a regression. FR-004 explicitly forbids any advance at Closure, which is consistent with the handler behavior. Parent-spec US2 §3 and FR-010/FR-011 read as if Closure can be completed, which no product flow supports.
- **Resolution options for triage**:
  1. Add a "finalize project" application-layer command that performs the 7th domain-level advance. US2 §3 then becomes achievable verbatim.
  2. Amend parent spec US2 §3 to describe the actually-achievable "closed" state (6 completed + Closure in progress + no advance).
- **Current coverage** (C2): test asserts the renderable reality. See coverage-matrix note A.

## Fixture extensions proposed but deferred

*(Populated when a test would benefit from a fixture method that doesn't warrant adding in the current chunk.)*
