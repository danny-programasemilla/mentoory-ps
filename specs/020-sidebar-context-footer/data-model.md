# Phase 1 Data Model: Sidebar Context Footer

**Feature**: 020-sidebar-context-footer
**Date**: 2026-05-23

This feature introduces **no database entities and no schema changes**. The "data model" here is the set of cookie claims the view layer reads, plus the existing read model the switcher already uses.

## Cookie Claims (auth cookie)

| Claim | Existing? | Source | Used by this feature |
|-------|-----------|--------|----------------------|
| `ActiveRole` | existing | `ContextController.UpdateAuthCookie` | Card primary line (via `User.GetActiveRole()`) |
| `ActiveIncubatorName` | existing | same | Card secondary line (first segment) |
| `ActiveProjectName` | existing | same | Card secondary line (second segment) |
| `ActiveIncubatorId` | existing | same | (not displayed) |
| `ActiveProjectId` | existing | same | (not displayed) |
| **`CanSwitchContext`** | **new** | `ContextController.UpdateAuthCookie` | Decides interactive vs static card |

### `CanSwitchContext` (new)

- **Type:** string `"true"` / `"false"` (cookie claim).
- **Written:** in `ContextController.UpdateAuthCookie`, at every context-set/switch path.
- **Value rule:** `true` when `selectableContextCount > 1` OR `ActiveRole == GlobalAdmin`; otherwise `false`.
- **Read:** `ClaimsPrincipalExtensions.CanSwitchContext(this ClaimsPrincipal)` → `bool`.
- **Default when claim absent (pre-change sessions):** `true` (clickable). Rationale in research.md R1.

## Existing Read Model (unchanged, reused by the switcher)

`Mentoory.Access.Domain/ReadModels/UserContext.cs`:

```csharp
public record UserContext(
    Guid RoleAssignmentExternalId,
    long UserId,
    long IncubatorId,
    string? IncubatorName,
    long? ProjectId,
    string? ProjectName,
    string Role);
```

- Produced by `GetUserContextsQuery` → `GetUserContextsHandler` (one entry per active `RoleAssignment`, AsNoTracking).
- This feature uses only its **count** (to compute `CanSwitchContext`); the switcher modal continues to consume the full list as today.

## Display Composition (card)

| Card element | Value | Empty-state behavior |
|--------------|-------|----------------------|
| Leading icon | static (context glyph) | always shown |
| Primary line | `ActiveRole` | card not rendered if role absent |
| Secondary line | `IncubatorName` + " · " + `ProjectName` | omit a missing part; hide line entirely if both absent |
| Trailing chevron | shown only when `CanSwitchContext` | hidden when static |

## State Transitions

None. The card is a stateless projection of the current claims. Its only "transition" is the existing context switch, which rewrites the claims (including `CanSwitchContext`) and re-renders the card on the next request — behavior owned by the unchanged switcher, not by this feature.
