# Deep Review Findings

**Date:** 2026-05-23
**Branch:** 020-sidebar-context-footer
**Rounds:** 1
**Gate Outcome:** PASS
**Invocation:** quality-gate (after_implement hook)

## Summary

| Severity | Found | Fixed | Remaining |
|----------|-------|-------|-----------|
| Critical | 0 | 0 | 0 |
| Important | 2 | 2 | 0 |
| Minor | 3 | 1 | 2 |
| **Total** | **5** | **3** | **2** |

**Agents completed:** 5/5 (Correctness, Architecture, Security, Production Readiness, Test Quality)
**External tools:** CodeRabbit — skipped (CLI not installed); Copilot — skipped (CLI not installed)
**Agents failed:** none
**Stage 1 spec compliance:** 100%

Correctness, Security, and Production-Readiness agents each returned **zero findings** after a two-pass read (notably: nullable `count > 1` comparison is safe; `CanSwitchContext` is a UI-only claim never used for server-side authz; `_Navigation.cshtml` adds zero per-render queries — FR-013 holds; the AJAX recompute is switch-time only with `CancellationToken` propagated).

## Findings

### FINDING-1
- **Severity:** Important
- **Confidence:** 88
- **File:** tests/Mentoory.Tests.E2E/Tests/ContextSwitchingTests.cs:23-40 (original)
- **Category:** test-quality
- **Source:** test-quality-agent
- **Round found:** 1
- **Resolution:** fixed (round 1)

**What is wrong:**
`TopBar_ShouldDisplay_CurrentContext` located the indicator with a dead OR-selector
`[data-testid='current-context'], .context-indicator, .navbar .context-name` and asserted
only `CountAsync() > 0`. The two legacy class branches no longer exist in markup; the test
neither pinned the relocation to the sidebar (SC-001/SC-004) nor asserted the card shows
content.

**Why this matters:**
This is the FR-012/SC-001 regression guard. A bare presence count would pass even if the
card rendered empty or in the wrong place — the exact properties the feature changes.

**How it was resolved:**
Renamed to `Sidebar_ShouldDisplay_CurrentContext`; locator narrowed to
`[data-testid='current-context']`; assertions now require exactly one card, that it lives
inside `#sidebar` (and nowhere else — proving header has no context badge), and that its
text is non-empty.

### FINDING-2
- **Severity:** Important
- **Confidence:** 82
- **File:** tests/Mentoory.Tests.E2E/Tests/ContextSwitchingTests.cs (no test existed)
- **Category:** test-quality
- **Source:** test-quality-agent
- **Round found:** 1
- **Resolution:** fixed (round 1)

**What is wrong:**
No automated test covered User Story 3 / FR-005 / SC-003 — the single-context **static**
card with no clickable switch affordance. Both interactive tests log in as `multirole@`
(always `CanSwitchContext=true`), leaving the static `<div>` branch unexercised.

**Why this matters:**
SC-003 ("zero clickable switch affordances") is a measurable success criterion. A
regression rendering the interactive `<a>` for single-context users — the dead-control bug
the feature set out to avoid — would go uncaught. Seed data makes it free to close.

**How it was resolved:**
Added `SingleContextUser_Card_IsStatic_NotClickable`: logs in as the confirmed single-role
user `entrepreneur1@test.mentoory.com`, asserts the card has no `data-bs-toggle="modal"`,
no `role="button"`, no `.ti-selector` chevron, and that clicking it does not open
`#contextSwitcherModal`. Passing (verified in fix-round E2E run).

### FINDING-3
- **Severity:** Minor
- **Confidence:** 72
- **File:** tests/Mentoory.Tests.E2E/Tests/ContextSwitchingTests.cs (ContextCard_ShouldOpen_Modal)
- **Category:** test-quality
- **Source:** test-quality-agent
- **Round found:** 1
- **Resolution:** fixed (round 1)

**What is wrong:**
The modal-open test never asserted the interactive card carries its switch affordance
(FR-004 / US2 #1); a dropped trigger attribute would surface only as an opaque 5s
modal-visibility timeout.

**How it was resolved:**
Added explicit pre-click assertions that the card is
`[role='button'][data-bs-toggle='modal']` (count == 1) and shows the `.ti-selector` chevron
(count == 1). Also renamed the stale `CambiarContextoButton_ShouldOpen_Modal` →
`ContextCard_ShouldOpen_Modal`.

### FINDING-4
- **Severity:** Minor
- **Confidence:** 70
- **File:** tests/Mentoory.Tests.E2E/Tests/ContextSwitchingTests.cs (ContextSwitch_ViaModal_ShouldShowToastAndReload)
- **Category:** test-quality
- **Source:** test-quality-agent
- **Round found:** 1
- **Resolution:** remaining (accepted)

**What is wrong:**
The post-confirm verification is wrapped in `if (!await confirmBtn.IsDisabledAsync())` with
only a `NotContain("Internal Server Error")` assertion inside; it can pass vacuously and
never checks the success toast or that the context actually changed. The name oversells
what it guards.

**Why this matters / why not auto-fixed:**
The suggested hardening (force `ToBeEnabledAsync` + assert the toast) runs on the multirole
switch path that is **already quarantined as flaky** (`ContextSwitch_ShouldNavigateSafely_WithoutErrors`
is `[Fact(Skip=...)]` for exactly this reason). Tightening this sibling risks importing the
same full-suite-load flakiness. Left as a documented Minor; the happy-path switch is still
exercised by `MultiRoleUser_CanComplete_FullCascadeAndSubmit` in `ContextSelectionTests`.
Recommend revisiting alongside the underlying multirole-concurrency investigation.

### FINDING-5
- **Severity:** Minor
- **Confidence:** 72
- **File:** Mentoory.Web/Views/Shared/_Navigation.cshtml:129-158
- **Category:** architecture
- **Source:** architecture-agent
- **Round found:** 1
- **Resolution:** remaining (accepted)

**What is wrong:**
The interactive `<a>` and static `<div>` branches duplicate the ~7-line inner block (avatar
+ truncated role/secondary lines + `title` tooltips + `data-testid`). A change to the inner
markup must be applied in two places.

**Why this matters / why not auto-fixed:**
The only genuine differences are the outer tag and the trailing chevron, so the duplication
is exactly twice and the outer-tag difference is legitimate (a static card must not be an
anchor). The reviewing agent itself rated leaving it "defensible." Extracting a
`_SidebarContextBody` partial is a reasonable cleanup but is a style preference, not a
correctness or maintainability blocker. Left for human discretion during code review.

## Remaining Findings

Both remaining items are **Minor** and do not block the gate (Critical + Important = 0):

- **FINDING-4** (test vacuous-pass): not auto-fixed to avoid importing known multirole flakiness; happy-path switch covered elsewhere.
- **FINDING-5** (card markup duplication): accepted as defensible twice-over duplication; optional partial extraction left to reviewer judgment.
