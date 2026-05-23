# Quickstart & Verification: Sidebar Context Footer

**Feature**: 020-sidebar-context-footer
**Date**: 2026-05-23

How to build, run, and verify the feature against its success criteria.

## Build & run

```bash
dotnet build                                  # must be zero warnings (TreatWarningsAsErrors)
dotnet run --project Mentoory.Aspire.AppHost   # full orchestration, or:
dotnet run --project Mentoory.Web              # web only
```

Log in with a seeded multi-context user and a seeded single-context user (see the test-credentials reference) to exercise both card states.

## Manual verification (maps to Success Criteria)

| # | Step | Expected | SC |
|---|------|----------|-----|
| 1 | Log in (multi-context user), open any authenticated page | Context card visible at the **bottom of the left sidebar**: role on the primary line, "Incubadora · Proyecto" on the muted secondary line | SC-001 |
| 2 | Inspect the page header | No context badges; header shows only breadcrumb + title + (optional) action buttons + bell + avatar | SC-001, SC-004 |
| 3 | Open the avatar dropdown | "Perfil" (disabled) + "Cerrar sesión" only — **no "Cambiar contexto"** | FR-006, FR-007 |
| 4 | Click the sidebar card | `#contextSwitcherModal` ("Cambiar Contexto de Trabajo") opens; cascading selects behave as before | SC-002, FR-008 |
| 5 | Pick a new role/incubator/project, confirm | Context changes; card updates to the new context on the next page | SC-002 |
| 6 | Log in as a single-context (non-GlobalAdmin) user | Card shows the context but is **static** — no chevron, no hover, not clickable | SC-003, FR-005 |
| 7 | Log in as GlobalAdmin | Card is **interactive** (can browse incubators/projects) | R1 rule |
| 8 | User with no project (or no incubator) | Secondary line shows only what exists, or is hidden entirely; primary role line still shown | Edge cases |
| 9 | Hover a long role/incubator/project name | Truncated with ellipsis; full value in tooltip | FR-009 |
| 10 | Shrink to < sm width | Sidebar collapses to hamburger; card appears at the end of the expanded menu; switching works via the card | Edge case |
| 11 | Check contrast of card text on the dark sidebar | Legible; primary line meets WCAG AA | FR-010 |

## Automated verification

```bash
# Repointed E2E (avatar-dropdown trigger → sidebar card)
dotnet test tests/Mentoory.Tests.E2E --filter "FullyQualifiedName~ContextSwitchingTests"
```

Expected: all `ContextSwitchingTests` pass after repointing — the `current-context` locator resolves to the card, and clicking it opens the switcher modal (SC-005). Full `dotnet build` completes with zero warnings (SC-005).

## Rollback

Pure view-layer + one claim. Reverting `_Navigation.cshtml`, `_TopBar.cshtml`, `ClaimsPrincipalExtensions.cs`, `ContextController.cs`, the CSS, and the test restores prior behavior. No data migration to undo (the `CanSwitchContext` claim simply stops being written/read).
