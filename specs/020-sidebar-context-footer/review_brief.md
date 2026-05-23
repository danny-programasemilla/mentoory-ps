# Review Brief: Sidebar Context Footer

**Spec:** specs/020-sidebar-context-footer/spec.md
**Generated:** 2026-05-22

> Reviewer's guide to scope and key decisions. See full spec for details.

---

## Feature Overview

Relocate the active-context display (Rol / Incubadora / Proyecto) from the page header into an avatar-style card pinned at the bottom of the left sidebar. The card becomes the single entry point for opening the existing context switcher. The header is thereby simplified to breadcrumb + page title + page-action buttons + notification bell + user avatar. No change to how context is selected, persisted, or applied — only where it is displayed and triggered.

## Scope Boundaries

- **In scope:** sidebar context card (display + clickable trigger), header de-cluttering, removal of "Cambiar contexto" from the avatar dropdown, repointing the existing context-switching E2E test.
- **Out of scope:** the switcher's cascading selection logic (feature 008), notification/bell behavior, the Profile page, any server-side context handling.
- **Why these boundaries:** the request is purely about presentation/placement; the underlying context machinery already works and should not be disturbed.

## Critical Decisions

### Single switch entry point
- **Choice:** the sidebar card is the only way to open the switcher; "Cambiar contexto" is removed from the avatar dropdown.
- **Trade-off:** one obvious location vs. losing a second discoverability path (notably on mobile, where the card lives at the end of the collapsed menu).
- **Feedback:** is one entry point acceptable, or should the avatar dropdown keep a switch link for mobile reach?

### Single-context users get a static card
- **Choice:** when nothing is switchable, the card is non-interactive (no chevron/hover).
- **Trade-off:** avoids a dead control, but requires knowing the selectable-context count at render time (see Open Questions).
- **Feedback:** acceptable, or prefer always-clickable for simplicity?

## Areas of Potential Disagreement

### Removing the header context badges entirely
- **Decision:** header shows no context at all.
- **Why this might be controversial:** some users scan the top of the page, not the sidebar bottom, for "where am I".
- **Alternative view:** keep a minimal context breadcrumb in the header in addition to the sidebar card.
- **Seeking input on:** is the sidebar-only placement sufficient?

## Naming Decisions

| Item | Name | Context |
|------|------|---------|
| Card primary line | Active role | e.g. "Coordinador" |
| Card secondary line | "Incubadora · Proyecto" | middot-separated, omitted parts hidden |
| Switcher modal title | "Cambiar Contexto de Trabajo" | unchanged (existing) |
| Test hook | `data-testid="current-context"` | preserved on the new card |

## Open Questions

- [ ] Switchability detection: how to know "user has > 1 selectable context" at render time — claim set at sign-in, cached lookup, or always-clickable fallback? (Deferred to `/speckit-plan`.)

## Risk Areas

| Risk | Impact | Mitigation |
|------|--------|------------|
| E2E tests that click avatar-dropdown "Cambiar contexto" break | Med | FR-014 mandates repointing them to the card; SC-005 gates on them passing |
| Pinned-bottom card conflicts with collapsing sidebar on mobile | Low | Edge case defines card-at-end-of-menu behavior on small screens |
| Low contrast on dark sidebar | Low | FR-010 requires adequate contrast (optionally WCAG AA) |

---
*Share with reviewers before implementation.*
