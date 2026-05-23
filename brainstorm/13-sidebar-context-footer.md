# Brainstorm: Sidebar Context Footer

**Date:** 2026-05-22
**Status:** spec-created
**Spec:** specs/020-sidebar-context-footer/

## Problem Framing

The active-context indicators (Rol / Incubadora / Proyecto) live in the page header's center zone (`_TopBar.cshtml`), crowding it alongside the breadcrumb, page title, page actions, notification bell, and avatar menu. The user wants the header simplified and the context moved to the bottom of the left sidebar, with a professional, dashboard-standard look aligned with the rest of the site. Switching context currently happens via "Cambiar contexto" in the avatar dropdown (opens `#contextSwitcherModal`).

## Approaches Considered

Three layout patterns were presented (with ASCII mockups):

### A: Full footer + slim header
- Move BOTH context AND user identity (avatar/name) to the sidebar bottom; header keeps only breadcrumb/title/actions + bell.
- Pros: most "enterprise dashboard"; maximally clean header.
- Cons: larger change; relocates the user menu too (more blast radius, not requested).

### B: Context-only footer (read-only)
- Move only the context chips to the sidebar bottom as a static display; header keeps bell + avatar dropdown (which retains switch/logout).
- Pros: smallest change.
- Cons: switching stays buried in the avatar dropdown; two places hold context concerns.

### C: Clickable context switcher in sidebar (CHOSEN)
- Context card at sidebar bottom; the whole card is the trigger that opens the existing switcher modal. Header keeps bell + avatar.
- Pros: single obvious switch location; reuses the existing modal/JS untouched; focused blast radius.
- Cons: removes the dropdown switch path (mobile reach via hamburger → card).

### Block style sub-decision
- Labeled rows vs. status chips (current) vs. **avatar-style card (CHOSEN)** — compact card: icon + role primary line, "Incubadora · Proyecto" muted secondary line, switch chevron at right.

### Switch entry sub-decision
- **Sidebar block only (CHOSEN)** vs. both places — "Cambiar contexto" removed from the avatar dropdown to avoid duplication.

## Decision

**Approach C** with an **avatar-style card** and the **sidebar card as the sole switch entry point**. Single-context users get a static (non-clickable) card to avoid a dead affordance, matching the existing `ContextController` auto-skip. The existing switcher modal (`#contextSwitcherModal`), `_ContextSelector` partial, and `context-switcher.js` are reused unchanged — only the trigger relocates. `data-testid="current-context"` is preserved on the new card; the context-switching E2E tests are repointed from the avatar-dropdown trigger to the sidebar card.

Spec created at `specs/020-sidebar-context-footer/spec.md` (FR-001..FR-014, SC-001..SC-005). Spec review gate: SOUND, no critical/important issues. Constitution-aligned (Spanish UI, graceful missing-context, zero-warnings, Tabler reuse).

## Open Threads

- Switchability detection: cheapest reliable way to know "user has > 1 selectable context" at layout render — claim set at sign-in vs. cached value vs. always-clickable fallback. Deferred to `/speckit-plan`.
- Optional: quantify card contrast against WCAG AA (≥ 4.5:1) on the dark sidebar.
- Mobile reach: with the dropdown switch path removed, switching on small screens requires opening the hamburger and tapping the card — acceptable, flagged for UX confirmation.
