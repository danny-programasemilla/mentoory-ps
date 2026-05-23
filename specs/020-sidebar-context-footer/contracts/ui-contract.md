# UI Contract: Sidebar Context Footer

**Feature**: 020-sidebar-context-footer
**Date**: 2026-05-23

This is the UI/markup contract for the relocated context display. It defines the stable hooks and states other code (and tests) depend on. No HTTP/API contract changes — the switcher's endpoints and `_ContextSelector` partial are unchanged.

## Location

- **Rendered in:** `Views/Shared/_Navigation.cshtml`, as the last child of `<div class="collapse navbar-collapse" id="sidebar-menu">`, after `<ul class="navbar-nav">`, in a block carrying `mt-auto` (pins to sidebar bottom).
- **Removed from:** `Views/Shared/_TopBar.cshtml` — the `#context-display` center-zone block (entire `@if (!string.IsNullOrEmpty(activeRole)) { ... }` status-badge block) and the "Cambiar contexto" dropdown item.

## Stable hooks (MUST be preserved)

| Hook | Where | Contract |
|------|-------|----------|
| `data-testid="current-context"` | the context card root | Moves from the header div to the card; tests locate the active context here. |
| `#contextSwitcherModal` | modal (stays in `_TopBar.cshtml` or moves to a shared partial) | id unchanged; `context-switcher.js` binds to it. |
| `_ContextSelector` partial | inside the modal | unchanged. |

## States

### A. Switchable (`User.CanSwitchContext() == true`)

- Root is an interactive trigger: `data-bs-toggle="modal" data-bs-target="#contextSwitcherModal"`, focusable, `role="button"` (or a `<button>`), with hover affordance.
- Shows a trailing chevron icon.
- `aria-label` in Spanish, e.g. "Cambiar contexto de trabajo".

### B. Static (`User.CanSwitchContext() == false`)

- Root is a plain non-interactive `<div>` (no modal toggle, no `role=button`, no chevron, no hover affordance).
- Still carries `data-testid="current-context"`.

### Content rules (both states)

| Line | Content | Rule |
|------|---------|------|
| Primary | active role (Spanish) | always present (card only renders when role present) |
| Secondary | `Incubadora · Proyecto` | show only the parts that exist; hide the line if neither exists |
| Each line | — | `text-truncate` + `title="<full value>"` for overflow (FR-009) |

## Markup skeleton (illustrative, not binding)

```html
<!-- inside #sidebar-menu, after the nav <ul> -->
<div class="mt-auto p-3">
  <!-- Switchable -->
  <a href="#" class="d-flex align-items-center text-reset text-decoration-none"
     data-bs-toggle="modal" data-bs-target="#contextSwitcherModal"
     data-testid="current-context" role="button"
     aria-label="Cambiar contexto de trabajo">
    <span class="avatar avatar-sm me-2"><i class="ti ti-building-community"></i></span>
    <div class="text-truncate">
      <div class="text-truncate" title="@activeRole">@activeRole</div>
      <div class="small text-secondary text-truncate" title="@secondary">@secondary</div>
    </div>
    <i class="ti ti-selector ms-auto"></i>
  </a>
</div>
```

The static variant is the same block as a `<div>` without the toggle attributes, `role`, `aria-label`, and trailing chevron.

## Behavioral contract

- Clicking the switchable card opens `#contextSwitcherModal` exactly as the old "Cambiar contexto" dropdown item did. No change to the switch/confirm flow (FR-008).
- The avatar dropdown retains "Perfil" (disabled) and "Cerrar sesión"; the bell stays in the header (FR-007).
- All introduced text is Spanish (FR-010).

## Test contract (drives FR-014 / SC-005)

`tests/Mentoory.Tests.E2E/Tests/ContextSwitchingTests.cs`:
- The `[data-testid='current-context']` locator (≈L31) continues to resolve — now to the card.
- Steps that opened the avatar dropdown and clicked "Cambiar contexto" (≈L50–60, L97–104) MUST be repointed to click the sidebar card (`[data-testid='current-context']`) to open `#contextSwitcherModal`.
- After repointing, all assertions (modal opens, selection cascades, confirm switches context) MUST pass unchanged.
