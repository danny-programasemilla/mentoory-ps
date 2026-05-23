# Implementation Plan: Sidebar Context Footer

**Branch**: `020-sidebar-context-footer` | **Date**: 2026-05-23 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/020-sidebar-context-footer/spec.md`

## Summary

Relocate the active-context display (Rol / Incubadora / Proyecto) from the page header (`_TopBar.cshtml` center zone) into an avatar-style card pinned to the bottom of the left sidebar (`_Navigation.cshtml`). The card is the single entry point for the existing context switcher modal: when the user can actually switch, the card is a button that opens `#contextSwitcherModal`; otherwise it is a static display. "Cambiar contexto" is removed from the avatar dropdown. The switcher modal, its shared partial, and `context-switcher.js` are reused unchanged — only the trigger and the read-only display relocate.

Switchability is determined by a new `CanSwitchContext` claim emitted when the auth cookie is (re)built in `ContextController.UpdateAuthCookie`, computed from the user's already-loaded context list (`GetUserContextsQuery`) plus the GlobalAdmin browse rule. A claim-reader extension exposes it to the layout. No new server round-trips per page render and no database changes.

## Technical Context

**Language/Version**: C# / .NET 10.0 (SDK 10.0.0)
**Primary Dependencies**: ASP.NET Core MVC (Razor views), Tabler v1.4.0 (Bootstrap 5), Tabler Icons Webfont; existing `context-switcher.js` / `context-selector.js` (reused, unchanged)
**Storage**: N/A — no schema changes. Reads existing cookie claims (`ActiveRole`, `ActiveIncubatorName`, `ActiveProjectName`) + one new claim (`CanSwitchContext`)
**Testing**: xUnit + Microsoft.Playwright E2E (`tests/Mentoory.Tests.E2E/Tests/ContextSwitchingTests.cs`, repointed); existing `PlaywrightFixture`
**Target Platform**: Web (authenticated server-rendered pages)
**Project Type**: Web application (modular monolith, Razor MVC front end)
**Performance Goals**: No added per-request work for the card; `CanSwitchContext` computed only at context-set/switch time (infrequent user action)
**Constraints**: Zero build warnings (`TreatWarningsAsErrors`); Spanish UI; no new JavaScript; dark sidebar contrast adequate
**Scale/Scope**: ~3 view partials touched, 1 claim reader added, 1 claim emitted, 1 E2E test repointed, minor CSS

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Clean Architecture Layer Boundaries | PASS | Changes confined to Web layer (Razor partials, `ClaimsPrincipalExtensions`, `ContextController`). No Domain/Application edits; no repositories injected into controllers. The claim is built in the Web layer from an existing Application query result. |
| II. CQRS | PASS | Reuses existing `GetUserContextsQuery`; no new commands/queries required (count derived from existing query result). |
| V. Zero-Warnings | PASS | SC-005 requires a zero-warning build. |
| VI. DateTime Handling | N/A | No time logic. |
| VIII. File Organization | PASS | No new JS (reuses `wwwroot/js/context-switcher.js`); partials stay in `Views/Shared`; any CSS goes in `wwwroot/css/mentoory.css`. |
| IX. Spanish-First UI | PASS | FR-008; all card text in Spanish, code/comments English. |
| X. Role Hierarchy & Session Context | PASS | Read-only display of existing claims; missing context → card not rendered (graceful). GlobalAdmin global-scope (no incubator) handled as an explicit edge case and as part of the switchability rule. |
| Web Layer Patterns | PASS | `ContextController` already inherits the executor pattern; the claim emission is added at the existing cookie-build point. |
| Dependency Governance | PASS | No new packages. |

**Result**: No violations. Complexity Tracking not required.

## Project Structure

### Documentation (this feature)

```text
specs/020-sidebar-context-footer/
├── plan.md              # This file
├── research.md          # Phase 0 — switchability detection, Tabler footer pattern, contrast
├── data-model.md        # Phase 1 — claim + read-model contract (no DB entities)
├── quickstart.md        # Phase 1 — manual + automated verification steps
├── contracts/
│   └── ui-contract.md   # Phase 1 — card structure, states, test hooks, claim contract
├── spec.md              # Feature spec
├── REVIEW-SPEC.md       # Spec soundness gate result
└── review_brief.md      # Reviewer guide
```

### Source Code (repository root)

```text
Mentoory.Web/
├── Views/Shared/
│   ├── _Navigation.cshtml      # ADD: context card pinned at sidebar bottom (mt-auto block)
│   ├── _TopBar.cshtml          # REMOVE: context-display center zone; REMOVE "Cambiar contexto" dropdown item; KEEP #contextSwitcherModal + bell + avatar
│   └── _ContextSelector.cshtml # UNCHANGED (reused inside the modal)
├── Controllers/
│   └── ContextController.cs    # ADD: emit "CanSwitchContext" claim in UpdateAuthCookie (from already-loaded context count + GlobalAdmin rule)
├── Infrastructure/
│   └── ClaimsPrincipalExtensions.cs  # ADD: CanSwitchContext() reader (+ default-clickable when absent)
└── wwwroot/
    ├── css/mentoory.css        # ADD: sidebar context-card styling (dark-theme contrast, truncation)
    └── js/context-switcher.js  # UNCHANGED (modal id #contextSwitcherModal preserved)

tests/Mentoory.Tests.E2E/Tests/
└── ContextSwitchingTests.cs    # REPOINT: avatar-dropdown trigger → sidebar context card; current-context locator survives
```

**Structure Decision**: Existing modular-monolith Web project. This is a view-layer relocation plus one claim; no new projects, layers, or packages.

## Complexity Tracking

No constitution violations — section intentionally empty.
