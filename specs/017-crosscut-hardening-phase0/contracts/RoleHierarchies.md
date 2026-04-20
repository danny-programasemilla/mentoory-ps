# Contract: `Roles` / `RoleHierarchies` constants

**Files**:
- `Mentoory.Shared.Domain/Constants/Roles.cs` (exists — unchanged)
- `Mentoory.Shared.Domain/Constants/RoleHierarchies.cs` (new — CP-1)

**Consumers**: every `[Authorize(Roles = ...)]` attribute and `MenuConfiguration.cs`; any code that compares `User.GetActiveRole()` against a role name.

## `Roles` — atomic role identifiers

Already exists. Do not modify. Reference shape:

```csharp
public static class Roles
{
    public const string GlobalAdmin         = "GlobalAdmin";
    public const string IncubatorAdmin      = "IncubatorAdmin";
    public const string ProjectCoordinator  = "ProjectCoordinator";
    public const string Mentor              = "Mentor";
    public const string Entrepreneur        = "Entrepreneur";
    public const string Sponsor             = "Sponsor";
    public static readonly IReadOnlyList<string> All = new[] { /* … */ };
}
```

## `RoleHierarchies` — `[Authorize]`-ready comma-joined strings

```csharp
public static class RoleHierarchies
{
    public const string PlatformScope     = Roles.GlobalAdmin;
    public const string IncubatorScope    = Roles.IncubatorAdmin + "," + Roles.GlobalAdmin;
    public const string ProjectScope      = Roles.ProjectCoordinator + "," + Roles.IncubatorAdmin + "," + Roles.GlobalAdmin;
    public const string MentoringScope    = Roles.Mentor + "," + Roles.ProjectCoordinator + "," + Roles.IncubatorAdmin + "," + Roles.GlobalAdmin;
    public const string ParticipantScope  = Roles.Entrepreneur + "," + Roles.Mentor + "," + Roles.ProjectCoordinator + "," + Roles.IncubatorAdmin + "," + Roles.GlobalAdmin;
    public const string SponsorScope      = Roles.Sponsor + "," + Roles.IncubatorAdmin + "," + Roles.GlobalAdmin;
}
```

## Usage rules

1. Every `[Authorize(Roles = "...")]` attribute in the solution MUST reference `RoleHierarchies.*`. Hardcoded comma-joined literals are forbidden.
2. Every role-equality check (e.g., `User.GetActiveRole() == "IncubatorAdmin"`) MUST use `Roles.IncubatorAdmin`.
3. Menu-configuration role arrays MUST reference `Roles.*` constants, not string literals.
4. Architecture test R-4.2.8 enforces rule #1 and #2 above at the source-grep level.

## Hierarchy design principle

Each `*Scope` constant encodes "the minimum-privilege role that this scope is designed for, PLUS every strictly higher role" per constitution §X. Adding a new role:

1. Add constant to `Roles`.
2. Decide which scopes include it (usually: the scope named after the new role, plus any scopes strictly below it in the hierarchy that should still grant access).
3. Add/adjust `RoleHierarchies.*` constants accordingly.
4. Re-run architecture tests; fix any surfaced violations.

## Deviation from spec text

Spec FR-001 and FR-002 suggest placement under `Mentoory.Shared.Application/Authorization/`. The implementation places the files at `Mentoory.Shared.Domain/Constants/` to preserve the existing `Roles.cs` location and avoid a wide rename. Rationale in [research.md § R-1](../research.md).
