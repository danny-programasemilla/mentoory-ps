# Phase 0 Research: Sidebar Context Footer

**Feature**: 020-sidebar-context-footer
**Date**: 2026-05-23

This document resolves the spec's one open question (switchability detection) and records the two supporting technical decisions (Tabler bottom-pinned footer, dark-theme contrast).

---

## R1. Switchability detection — how to know the user can actually switch context

**Open question (from spec):** At layout render time, how do we cheaply and reliably know whether the user has more than one selectable context, so the card is interactive only when switching is meaningful (FR-004 / FR-005)?

### Findings (from codebase trace)

- **No context claims are issued at login.** `Mentoory.Web/Areas/Access/Controllers/LoginController.cs` (≈L59–69) issues only `NameIdentifier`, `SessionToken`, `AccountStatus`. The user is then redirected to `ContextController.Select()`.
- **Context claims are issued in `ContextController.UpdateAuthCookie(UserContext context)`** (`Mentoory.Web/Controllers/ContextController.cs` ≈L327–370), called from all three context-set paths: AJAX switch (≈L236), traditional form POST (≈L276), and GlobalAdmin incubator/project selection (≈L317). Claims added: `ActiveRole`, `ActiveIncubatorId`, `ActiveIncubatorName?`, `ActiveProjectId?`, `ActiveProjectName?`, and `ClaimTypes.Role`.
- **The available-contexts list is `List<UserContext>`** (`Mentoory.Access.Domain/ReadModels/UserContext.cs`), one entry per active `RoleAssignment`, produced by `GetUserContextsQuery` → `GetUserContextsHandler` (AsNoTracking).
- **Auto-skip logic** (`ContextController.Select()` ≈L40–61) counts `contexts.Count`:
  - `Count == 0` → redirect away (no contexts).
  - `Count == 1 && role != GlobalAdmin` → auto-set, selector never shown. **These users can never switch.**
  - `Count == 1 && role == GlobalAdmin` → only auto-set if `ListIncubatorContextOptionsQuery` returns 0 incubators; otherwise the GlobalAdmin **can** browse/switch across all incubators/projects.
- **No existing count/flag claim or cache** (`RoleAssignmentCount`, `HasMultipleContexts`, `IMemoryCache`, session) exists.
- **Claim readers** live in `Mentoory.Web/Infrastructure/ClaimsPrincipalExtensions.cs` (`GetActiveRole`, `GetActiveIncubatorId`, etc.).

### Decision

**Emit a boolean `CanSwitchContext` claim when the auth cookie is built, and read it in the layout via a new `ClaimsPrincipalExtensions.CanSwitchContext()`.**

Computation at cookie-build time:

```
canSwitch = (contexts.Count > 1) || (context.Role == Roles.GlobalAdmin)
```

- For a non-GlobalAdmin user, more than one active role assignment ⇒ switchable. (Single-assignment non-admins are auto-skipped and genuinely cannot switch.)
- A GlobalAdmin can always re-pick incubator/project from the system-wide list, so they are switchable whenever they have a working context at all. (If literally zero incubators exist, the GlobalAdmin was already auto-set and routed; the card simply opens a modal that shows the current read-only selection — harmless.)

`UpdateAuthCookie` gains the count it needs without a new query type:
- The form-POST and GlobalAdmin paths already load `contexts` in `Select()` — pass `contexts.Count` down.
- The AJAX `Switch` path recomputes via the existing `GetUserContextsQuery(userId)` (one AsNoTracking, indexed-by-UserId query) at cookie-build time. Context switching is a rare, explicit user action, so this is not a hot path.

Concretely (HOW, for the implementer — not binding):
- Add optional `int? selectableContextCount = null` to `UpdateAuthCookie`; when null, load it via `GetUserContextsQuery`.
- Add `claims.Add(new Claim("CanSwitchContext", canSwitch ? "true" : "false"));`
- Add reader:
  ```csharp
  public static bool CanSwitchContext(this ClaimsPrincipal user)
      => user.FindFirst("CanSwitchContext")?.Value != "false"; // default clickable when absent
  ```

**Default-when-absent = clickable (`true`).** Sessions established before this change won't carry the claim; defaulting to clickable means a multi-context user is never wrongly locked into a static card. The only cost is a single-context user (pre-change session) seeing a clickable card until their next switch/login — harmless, because the switcher already renders single options as read-only.

### Rationale

- **Cheapest:** the count is already in hand at the only place context claims are written; rendering reads a claim (zero query per page).
- **Most consistent:** mirrors the existing claim-emission + claim-reader pattern; no new query/command, no new dependency.
- **Correct for GlobalAdmin**, which a naive "RoleAssignment count > 1" rule would get wrong.

### Alternatives considered

- **(b) Per-request cached query** — re-fetches data we can stamp into the cookie once; adds per-render work for no benefit. Rejected.
- **(c) Always-clickable** — simplest, and the switcher already handles the single-option case as read-only, but it violates FR-005 (no dead affordance for single-context users) and undercuts the "professional" goal. Rejected as the primary design, but retained as the **safe fallback semantics** for the absent-claim case.

---

## R2. Tabler pattern — pinning a footer block to the bottom of `navbar-vertical`

**Decision:** Add a footer block as the last child of the existing `<div class="collapse navbar-collapse" id="sidebar-menu">`, after the `<ul class="navbar-nav">`, wrapped so it carries `mt-auto`.

**Rationale:** In Tabler v1.4.0 the vertical sidebar's `.navbar-vertical .navbar-collapse` is a flex column that grows to fill height; `mt-auto` on a trailing block pushes it to the bottom (the same mechanism Tabler uses with `ms-auto`/`mt-auto` to push navbar items). Confirmed against Tabler docs (layout/navbars, page-layouts vertical). The avatar-style card reuses Tabler's user-menu markup idiom: `span.avatar.avatar-sm` + a text block with a primary line and a `div.small.text-secondary` secondary line, plus a trailing chevron icon (`ti ti-selector` / `ti ti-chevron-right`).

**Trigger:** the card is an element with `data-bs-toggle="modal" data-bs-target="#contextSwitcherModal"` when switchable; otherwise a plain non-interactive `<div>` (no `role=button`, no toggle attributes).

**Mobile/collapsed:** because the block lives inside `.navbar-collapse`, on `<sm` screens it appears at the end of the expanded hamburger menu rather than floating — matching the spec's mobile edge case. No extra work needed.

**Alternatives considered:** a `dropup` menu inside the sidebar (rejected — we open the existing modal, not a new menu); a fixed-position footer outside `.navbar-collapse` (rejected — breaks the collapse behavior and the dark-theme container styling).

---

## R3. Dark-sidebar contrast & truncation

**Decision:** Style the card on the existing dark sidebar surface (`#1B2434`): primary line in the sidebar's default light text, secondary line muted (`text-secondary`), with the leading icon and chevron at reduced opacity. Truncate each line with `text-truncate` (ellipsis) and set `title="<full value>"` for the hover tooltip (FR-009).

**Rationale:** Reuses Tabler's existing dark-theme text tokens already proven legible for the nav links above it; `text-truncate` is the Bootstrap-standard single-line ellipsis. Aim for WCAG AA (≥ 4.5:1) per the optional spec recommendation; the nav text on this surface already clears it, so the primary line inherits a compliant token and only the muted secondary line needs a contrast spot-check during implementation.

**Alternatives considered:** colored status chips (current header style) — rejected in brainstorm in favor of the avatar-style card; a separate light card surface inside the dark sidebar — rejected as visually inconsistent with the nav.

---

## Resolved unknowns

| Unknown | Resolution |
|---------|-----------|
| Switchability detection | `CanSwitchContext` claim at cookie-build; reader defaults clickable when absent (R1) |
| Bottom-pin mechanism | `mt-auto` trailing block inside `.navbar-collapse` (R2) |
| Card visual / truncation | Avatar-style card on dark surface, `text-truncate` + `title` (R3) |

No `NEEDS CLARIFICATION` markers remain.
