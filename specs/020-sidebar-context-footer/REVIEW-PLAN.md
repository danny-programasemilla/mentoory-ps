# Review Guide: Sidebar Context Footer

**Spec:** [spec.md](spec.md) | **Plan:** [plan.md](plan.md) | **Tasks:** [tasks.md](tasks.md)
**Generated:** 2026-05-23

---

## What This Spec Does

Today the "where am I" context — your role, current incubator, current project — sits as colored badges in the page header. This feature moves that into an avatar-style card pinned to the bottom of the left sidebar, and makes the card the one place you click to switch context. The header is left with just the breadcrumb, page title, action buttons, the bell, and your avatar.

**In scope:** the sidebar card (display + click-to-switch), removing the header context badges, removing "Cambiar contexto" from the avatar dropdown, a new `CanSwitchContext` claim to decide when the card is interactive, and repointing the existing context-switching E2E test.

**Out of scope:** the switcher modal's cascading logic (role → incubator → project, owned by feature 008), the bell/notifications, the Profile page, and anything server-side about how context is persisted or applied. See [Out of Scope](spec.md#out-of-scope).

## Bigger Picture

This is a direct descendant of feature 008 (the context selector / switcher). 008 built the modal and its cascading dropdowns; this feature only changes *where the trigger and the read-out live*. The deliberate constraint is "reuse, don't rewrite": the modal id (`#contextSwitcherModal`), the `_ContextSelector` partial, and `context-switcher.js` are untouched. The interesting design pressure is that the active-context info has no home of its own — it's reconstructed from cookie claims set during context selection — so the feature has to answer "can this user even switch?" without a database trip on every page. That question is the one genuinely new piece of machinery here (the `CanSwitchContext` claim).

---

## Spec Review Guide (30 minutes)

### Understanding the approach (8 min)

Read [User Story 1](spec.md#user-scenarios--testing-mandatory) and the [Reuse & Integration Constraints](spec.md#reuse--integration-constraints), then [research.md R1](research.md#r1-switchability-detection--how-to-know-the-user-can-actually-switch-context). As you read:

- Is "context belongs at the bottom of the sidebar" actually the more professional placement, or do some users scan the *top* of the page for orientation? (This is the one decision the brainstorm leaned into hardest — see [review_brief Areas of Disagreement](review_brief.md).)
- The switcher modal is reused verbatim and only the trigger moves. Does keeping the modal markup physically in `_TopBar.cshtml` while the *trigger* lives in `_Navigation.cshtml` ([plan Project Structure](plan.md#project-structure)) read as clean, or should the modal move to a neutral shared partial?

### Key decisions that need your eyes (12 min)

**Switchability via a `CanSwitchContext` claim** ([research.md R1](research.md#r1-switchability-detection--how-to-know-the-user-can-actually-switch-context), [data-model.md](data-model.md#cansswitchcontext-new))

The card is interactive only when switching is meaningful. Rather than query per render, the plan stamps a boolean claim at context-set time, computed `count > 1 || role == GlobalAdmin`.
- Is folding GlobalAdmin into the rule the right call? A GlobalAdmin with a single role assignment can still browse all incubators, so a naive "count > 1" would wrongly make their card static. Does the rule capture every browseable case?
- The AJAX `Switch` path recomputes the count with one extra `GetUserContextsQuery`. Acceptable on a rare user action, or worth threading the count through `SetActiveContextCommand` instead?

**Default-to-clickable when the claim is absent** ([research.md R1](research.md#r1-switchability-detection--how-to-know-the-user-can-actually-switch-context))

Sessions that predate this change won't carry the claim. The plan defaults those to *clickable*.
- Is "a single-context legacy session sees a clickable card that opens a read-only modal" an acceptable transient, versus the opposite failure (a multi-context user wrongly locked out of switching)? Do you agree clickable is the safer default?

**Single switch entry point** ([FR-006](spec.md#requirements-mandatory), task [T006](tasks.md#phase-4-user-story-2---switch-context-from-the-sidebar-card-priority-p1))

"Cambiar contexto" is removed from the avatar dropdown entirely.
- On a phone, the card lives at the end of the collapsed hamburger menu. Is one entry point genuinely enough, or does mobile reach justify keeping a dropdown link?

### Areas where I'm less certain (5 min)

- [spec.md FR-010](spec.md#requirements-mandatory) / [research.md R3](research.md#r3-dark-sidebar-contrast--truncation): I treated "professional, adequate contrast" as "inherit the nav's existing dark-theme text tokens, spot-check the muted secondary line for WCAG AA." The muted secondary line on `#1B2434` is the one place that could fail AA — worth a designer's eye, not just mine.
- [tasks.md T004 vs T006/T008](tasks.md#phase-3-user-story-1---context-in-sidebar-header-decluttered-priority-p1): I split three "user stories" that are really one card partial. T004 builds the whole card (both interactive and static branches); US2/US3 then mostly *remove the old path* and *verify* the static branch. If you'd expect cleaner per-story isolation, this coupling is a fair thing to push on — it's inherent to editing a single partial.

### Risks and open questions (5 min)

- The E2E repoint ([T007](tasks.md#phase-4-user-story-2---switch-context-from-the-sidebar-card-priority-p1)) is the only safety net for the switch flow. If the card's `data-testid="current-context"` or the modal id drift during implementation, the test silently exercises the wrong element — is one repointed test enough coverage for FR-006/008, or do we want an explicit assertion that the *header* no longer renders context?
- Does relocating the trigger interact with any keyboard/focus expectations? The old trigger was a dropdown item; the new one is a sidebar card opening a modal. Is focus-return-on-close still correct given `context-switcher.js` is unchanged ([FR-008](spec.md#requirements-mandatory))?

---
*Full context in linked [spec](spec.md), [plan](plan.md), and [tasks](tasks.md).*
