# Code Review: Sidebar Context Footer (020)

**Spec:** [spec.md](spec.md)
**Reviewer:** Claude (speckit.spex-gates.review-code)
**Date:** 2026-05-23

---

## Code Review Guide (30 minutes)

> This section guides a code reviewer through the implementation changes,
> focusing on high-level questions that need human judgment.

**Changed files:** 5 changed — 2 Razor partials (`_Navigation.cshtml`,
`_TopBar.cshtml`), 1 controller (`ContextController.cs`), 1 claim reader
(`ClaimsPrincipalExtensions.cs`), 1 stylesheet (`mentoory.css`), plus the
repointed E2E test (`ContextSwitchingTests.cs`).

### Understanding the changes (8 min)

- Start with `Mentoory.Web/Views/Shared/_Navigation.cshtml`: this is the heart of
  the feature — the avatar-style context card pinned to the sidebar bottom
  (`mt-auto`), with two mutually-exclusive branches (interactive `<a>` vs static
  `<div>`) chosen by `User.CanSwitchContext()`.
- Then `Mentoory.Web/Controllers/ContextController.cs`: see `UpdateAuthCookie` and
  the new `ResolveCanSwitchContextAsync` helper — this is where the
  `CanSwitchContext` claim is computed and stamped into the cookie.
- Question: the card is rendered in `_Navigation.cshtml` but triggers a modal that
  still lives in `_TopBar.cshtml` (by id `#contextSwitcherModal`). Both render on
  every authenticated page via `_Layout.cshtml`. Is this cross-partial
  card-triggers-modal coupling acceptable, or should the modal move into a shared
  partial co-located with the trigger?

### Key decisions that need your eyes (12 min)

**Switchability via a stamped claim, default-clickable when absent**
(`ClaimsPrincipalExtensions.cs`, `ContextController.cs:ResolveCanSwitchContextAsync`,
relates to [FR-004](spec.md#requirements)/[FR-005](spec.md#requirements), [research R1](research.md))

The card's interactive/static state is driven by a new `CanSwitchContext` cookie
claim, computed once at context-set time as `count > 1 || role == GlobalAdmin`.
The reader defaults to **clickable** when the claim is absent (pre-change
sessions). Net effect: a single-context user on a *pre-change* session sees a
clickable card until their next switch/login (harmless — the switcher renders
single options read-only).
- Question: is "default clickable" the right safety bias, or would you prefer
  default-static to guarantee FR-005 even for stale sessions?

**Recompute on AJAX switch vs. pass-down count** (`ContextController.cs`)

The two GET auto-skip paths pass the already-loaded `contexts.Count` to avoid a
query; the AJAX `Switch` path recomputes via `GetUserContextsQuery` at
switch-time (a rare action). GlobalAdmin short-circuits to `true` before any
count load.
- Question: is the extra `GetUserContextsQuery` on the AJAX switch path acceptable
  given it only fires on an explicit, infrequent user action ([FR-013](spec.md#reuse--integration-constraints)
  is about *render-time* round-trips, not switch-time)?

**Two-branch card markup** (`_Navigation.cshtml`)

The interactive and static variants duplicate the ~8-line inner block (avatar +
truncated text lines) because the outer tag must differ (`<a>` vs `<div>`).
- Question: acceptable duplication, or worth extracting a `_ContextCard` partial?

### Areas where I'm less certain (5 min)

- `_TopBar.cshtml` (avatar block, ~line 51): the avatar/user menu still renders
  `activeRole` as an `d-none d-xl-block` subtitle next to the user's name. I read
  [FR-003](spec.md#requirements)/[SC-004](spec.md#measurable-outcomes) as targeting
  the *context badges* (the removed `#context-display` status pills), and the role
  subtitle is part of the retained "avatar/menu". But this is a judgment call — if
  you consider that subtitle a "context indicator", it should also be removed.
- `mentoory.css` (`.sidebar-context-card .text-secondary`): I set the muted
  secondary line to `rgba(255,255,255,0.6)` on `#1B2434`, which I believe clears
  WCAG AA (~7:1), but [FR-010](spec.md#requirements) contrast was not measured with
  a tool — needs a visual/contrast-checker confirmation.
- The static-card branch and GlobalAdmin-interactive resolution were verified by
  inspection (T008), not by an automated test with single-context / GlobalAdmin
  seeded users. The repointed E2E exercises only the multi-context path.

### Deviations and risks (5 min)

No deviations from [plan.md](plan.md) were identified — the change set matches the
planned files (2 partials, claim reader, controller, CSS, E2E repoint) with no new
projects, packages, or schema changes.

- Risk: `mt-auto` bottom-pinning depends on Tabler's `.navbar-vertical
  .navbar-collapse` being a flex column ([research R2](research.md)). Verified in
  build/E2E render, but confirm visually on a short-menu account that the card
  truly sits at the bottom, not immediately under the last nav item.
- Risk (`<sm` collapsed sidebar): the card lives inside `.navbar-collapse`, so on
  mobile it appears at the end of the expanded hamburger menu. Confirm this matches
  the intended mobile edge case by hand.

---

## Deep Review Report

> Automated multi-perspective code review results. This section summarizes
> what was checked, what was found, and what remains for human review.

**Date:** 2026-05-23 | **Rounds:** 1/3 | **Gate:** PASS

### Review Agents

| Agent | Findings | Status |
|-------|----------|--------|
| Correctness | 0 | completed |
| Architecture & Idioms | 1 | completed |
| Security | 0 | completed |
| Production Readiness | 0 | completed |
| Test Quality | 4 | completed |
| CodeRabbit (external) | 0 | skipped (CLI not installed) |
| Copilot (external) | 0 | skipped (CLI not installed) |

### Findings Summary

| Severity | Found | Fixed | Remaining |
|----------|-------|-------|-----------|
| Critical | 0 | 0 | 0 |
| Important | 2 | 2 | 0 |
| Minor | 3 | 1 | 2 |

### What was fixed automatically

All test-quality gaps that mattered for the measurable success criteria were closed in one
fix round, all in `tests/Mentoory.Tests.E2E/Tests/ContextSwitchingTests.cs`:

- Strengthened the display test (renamed to `Sidebar_ShouldDisplay_CurrentContext`) to pin
  the card to `#sidebar`, require a single card, and assert non-empty content — closing the
  SC-001/SC-004/FR-012 relocation guard that the old dead OR-selector left open.
- Added `SingleContextUser_Card_IsStatic_NotClickable` (logs in as the single-role
  `entrepreneur1`) to cover SC-003/FR-005 — the previously untested static-card branch.
- Added switch-affordance assertions to `ContextCard_ShouldOpen_Modal` (FR-004 / US2 #1) and
  renamed it off the now-removed "Cambiar contexto" button.

The repointed E2E suite passes after the fixes: 4 passed, 1 skipped (pre-existing quarantined
multirole flake), 0 failed.

### What still needs human attention

All Critical and Important findings were resolved. Two **Minor** findings remain (full detail
in [review-findings.md](review-findings.md)) and are not blocking:

- `ContextSwitch_ViaModal_ShouldShowToastAndReload` can pass vacuously (its post-confirm
  assertions sit behind an `if (!IsDisabled)` guard). Not auto-hardened because the suggested
  fix runs on the multirole path already quarantined for flakiness. Question: harden it now and
  accept possible flakiness, or fold it into the separate multirole-concurrency investigation?
- The context card duplicates its inner markup across the interactive/static branches in
  [`_Navigation.cshtml`](../../Mentoory.Web/Views/Shared/_Navigation.cshtml). Question: extract a
  `_SidebarContextBody` partial, or leave the twice-over duplication (the outer tag legitimately
  differs)?

### Recommendation

All Critical/Important findings addressed and the E2E gate is green. **Code is ready for human
review**; the two remaining Minor findings are optional and can be triaged during review. See
[review-findings.md](review-findings.md) for details.
