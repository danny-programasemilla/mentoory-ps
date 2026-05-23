---
description: "Task list for Sidebar Context Footer (020)"
---

# Tasks: Sidebar Context Footer

**Input**: Design documents from `/specs/020-sidebar-context-footer/`
**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, contracts/ui-contract.md ✓, quickstart.md ✓

**Tests**: No new TDD tests requested. The only required test work is **repointing the existing E2E** (`ContextSwitchingTests.cs`) per FR-014 / SC-005. No new contract/unit tests are generated.

**Organization**: Tasks grouped by user story. NOTE: all three stories are facets of one card partial (`_Navigation.cshtml`) plus the header partial (`_TopBar.cshtml`); cross-story `[P]` is therefore limited where the same file is touched. File-level parallelism is marked where files differ.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1 / US2 / US3 (Setup, Foundational, Polish carry no story label)

## Path Conventions

Modular-monolith Web project. Relevant roots:
- Views: `Mentoory.Web/Views/Shared/`
- Web infra: `Mentoory.Web/Infrastructure/`, `Mentoory.Web/Controllers/`
- Assets: `Mentoory.Web/wwwroot/css/`, `Mentoory.Web/wwwroot/js/`
- E2E: `tests/Mentoory.Tests.E2E/Tests/`

---

## Phase 1: Setup

**Purpose**: Establish a green baseline and confirm the exact edit points.

- [X] T001 Confirm baseline is green (`dotnet build` zero warnings; `dotnet test tests/Mentoory.Tests.E2E --filter "FullyQualifiedName~ContextSwitchingTests"` passes) and re-read the current markup blocks in `Mentoory.Web/Views/Shared/_Navigation.cshtml` (lines 32–117, the `#sidebar-menu` collapse) and `Mentoory.Web/Views/Shared/_TopBar.cshtml` (lines 10–31 context-display; lines 57–73 avatar dropdown; lines 77–89 modal) to anchor the edits below.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Switchability-detection infrastructure (research.md R1). Blocks the card's interactive/static decision used by US1/US2/US3.

**⚠️ CRITICAL**: User-story work depends on the reader (T002) for compilation and the claim (T003) for correct runtime state.

- [X] T002 [P] Add `CanSwitchContext(this ClaimsPrincipal user)` reader to `Mentoory.Web/Infrastructure/ClaimsPrincipalExtensions.cs` — returns `user.FindFirst("CanSwitchContext")?.Value != "false"` (default = clickable when the claim is absent, per research.md R1).
- [X] T003 [P] Emit the `CanSwitchContext` claim in `Mentoory.Web/Controllers/ContextController.cs` `UpdateAuthCookie`. Thread the selectable-context count into the method (optional param `int? selectableContextCount = null`; when null, load via the existing `GetUserContextsQuery(userId)`); add `new Claim("CanSwitchContext", (selectableContextCount > 1 || context.Role == Roles.GlobalAdmin) ? "true" : "false")`. Pass `contexts.Count` from the `Select()` paths that already load it; let the AJAX `Switch` path recompute.

**Checkpoint**: Layout can read `User.CanSwitchContext()`; new sessions carry an accurate claim.

---

## Phase 3: User Story 1 - Context in sidebar, header decluttered (Priority: P1) 🎯 MVP

**Goal**: Active context appears as an avatar-style card at the bottom of the left sidebar on every authenticated page; the header no longer carries context badges.

**Independent Test**: Log in, open any authenticated page → context card visible at the sidebar bottom (role + Incubadora·Proyecto), and the header shows no context badges (SC-001, SC-004).

### Implementation for User Story 1

- [X] T004 [US1] Add the avatar-style context card to `Mentoory.Web/Views/Shared/_Navigation.cshtml` as the last child of `<div class="collapse navbar-collapse" id="sidebar-menu">` (after the `<ul class="navbar-nav">`), wrapped in an `mt-auto` block so it pins to the sidebar bottom. Compute `activeRole` / `activeIncubatorName` / `activeProjectName` from claims; render the block ONLY when `activeRole` is present. Primary line = role; secondary muted line = incubator and project joined by " · " (omit missing parts; hide the line if both absent); leading context icon. Carry `data-testid="current-context"`. When `User.CanSwitchContext()`: render the root as a modal trigger (`data-bs-toggle="modal" data-bs-target="#contextSwitcherModal"`, `role="button"`, Spanish `aria-label`, trailing chevron `ti ti-selector`); otherwise render a plain non-interactive `<div>` (no toggle attrs / role / chevron). Follow the markup skeleton in `contracts/ui-contract.md`.
- [X] T005 [P] [US1] Remove the context status-badge block (`#context-display`, the `@if (!string.IsNullOrEmpty(activeRole)) { ... }` center-zone span block, lines ~10–31) from `Mentoory.Web/Views/Shared/_TopBar.cshtml`. Keep `activeRole` only if still used by the avatar-dropdown subtitle; delete the now-unused `activeIncubatorName` / `activeProjectName` locals.

**Checkpoint**: MVP — context shows in the sidebar, header is clean. SC-001 + SC-004 verifiable.

---

## Phase 4: User Story 2 - Switch context from the sidebar card (Priority: P1)

**Goal**: The sidebar card is the single entry point to open the existing switcher; "Cambiar contexto" no longer appears in the avatar dropdown; switching behaves exactly as before.

**Independent Test**: As a multi-context user, click the sidebar card → `#contextSwitcherModal` opens; confirm a new selection → context changes. Avatar dropdown contains only Perfil + Cerrar sesión (SC-002, FR-006/007/008).

### Implementation for User Story 2

- [X] T006 [US2] In `Mentoory.Web/Views/Shared/_TopBar.cshtml`, remove the "Cambiar contexto" dropdown `<button>` (lines ~61–65) from the avatar dropdown. KEEP the `#contextSwitcherModal` markup (lines ~77–89), the notification bell, "Perfil" (disabled), and "Cerrar sesión". (Sequential after T005 — same file.)
- [X] T007 [US2] Repoint `tests/Mentoory.Tests.E2E/Tests/ContextSwitchingTests.cs`: replace the steps that open the avatar dropdown and click "Cambiar contexto" (≈L50–60 and L97–104) with a click on the sidebar card (`page.Locator("[data-testid='current-context']")`) to open `#contextSwitcherModal`. Keep the `current-context` locator (≈L31). Add an assertion that the avatar dropdown no longer contains "Cambiar contexto". (Depends on T004 for the card + testid.)

**Checkpoint**: Switching works solely from the card; old entry point gone; E2E repointed.

---

## Phase 5: User Story 3 - Single-context user sees a static card (Priority: P2)

**Goal**: A user with one selectable context sees the card as a static, non-interactive display (no chevron, no hover affordance, not clickable).

**Independent Test**: As a single-context (non-GlobalAdmin) user, view the card → no switch affordance, not clickable; as GlobalAdmin → interactive (SC-003, FR-005, research.md R1 rule).

### Implementation for User Story 3

- [X] T008 [US3] Verify the static branch from T004 in `Mentoory.Web/Views/Shared/_Navigation.cshtml` renders correctly for `User.CanSwitchContext() == false` (no `data-bs-toggle`, no `role="button"`, no chevron). If any interactive affordance leaks into the static branch, fix it. Confirm GlobalAdmin (single role assignment, ≥1 incubator) resolves to interactive via the T003 rule.

**Checkpoint**: All three stories independently functional.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T009 [P] Add sidebar context-card styling to `Mentoory.Web/wwwroot/css/mentoory.css`: dark-theme contrast (primary line meets WCAG AA on `#1B2434`; muted secondary line), `text-truncate` + ensure `title="<full value>"` overflow tooltips (FR-009), hover/cursor affordance applied ONLY to the interactive variant, top divider/spacing separating the card from the nav.
- [X] T010 Run `specs/020-sidebar-context-footer/quickstart.md` manual checks: multi-context user, single-context user, GlobalAdmin, no-incubator/no-project, long names, and `<sm` collapsed-sidebar (card at end of expanded menu).
- [X] T011 Verify SC-005: `dotnet test tests/Mentoory.Tests.E2E --filter "FullyQualifiedName~ContextSwitchingTests"` green and `dotnet build` zero warnings.
- [X] T012 [P] Run `/simplify` over the touched files (`_Navigation.cshtml`, `_TopBar.cshtml`, `ClaimsPrincipalExtensions.cs`, `ContextController.cs`) and remove any dead code introduced (e.g., orphaned claim locals).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: no dependencies.
- **Foundational (Phase 2)**: after Setup. Blocks user stories (T004 needs the T002 reader to compile; correct runtime needs T003).
- **US1 (Phase 3)**: after Foundational.
- **US2 (Phase 4)**: T006 after T005 (same file `_TopBar.cshtml`); T007 after T004 (needs card + testid).
- **US3 (Phase 5)**: T008 after T004 (same card partial).
- **Polish (Phase 6)**: after the stories it covers (T009 after T004; T010/T011 after all impl; T012 last).

### Within / Across Stories

- T002 ∥ T003 (different files).
- T004 ∥ T005 (different files: `_Navigation.cshtml` vs `_TopBar.cshtml`).
- T006, T008 touch files already edited (`_TopBar.cshtml`, `_Navigation.cshtml`) → sequential w.r.t. T005/T004 respectively.
- T009 ∥ T012 (CSS vs source cleanup), both after impl.

### Parallel Opportunities

```bash
# Foundational (different files):
T002  ClaimsPrincipalExtensions.cs  (reader)
T003  ContextController.cs          (claim emission)

# US1 (different files):
T004  _Navigation.cshtml            (card)
T005  _TopBar.cshtml                (remove header badges)
```

---

## Implementation Strategy

### MVP First (US1 only)

1. Phase 1 Setup → Phase 2 Foundational (T002, T003).
2. Phase 3 US1 (T004, T005).
3. **STOP & VALIDATE**: context in sidebar, header clean (SC-001, SC-004).

### Incremental Delivery

US1 (MVP) → US2 (single switch entry + E2E repoint) → US3 (static-state correctness) → Polish (CSS, contrast, mobile, quickstart, zero-warning build).

## Notes

- The switcher modal, `#contextSwitcherModal` id, `_ContextSelector` partial, and `context-switcher.js` are NOT modified — only the trigger relocates (FR-008).
- The modal stays in `_TopBar.cshtml`; the card in `_Navigation.cshtml` triggers it by id (both render on every authenticated page via `_Layout.cshtml`).
- Commit after each phase or logical group.
- Total: 12 tasks (Setup 1, Foundational 2, US1 2, US2 2, US3 1, Polish 4).
