# Quickstart: Cross-cutting Hardening — Phase 0

**Spec**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md) | **Data Model**: [data-model.md](./data-model.md)

Operational playbook for implementers and reviewers. Follow the Checkpoint Execution Plan in `plan.md`; this file gives the exact commands and per-CP verification recipes.

## Prerequisites

```bash
# Verify branch
git branch --show-current   # expect: 017-crosscut-hardening-phase0

# Verify baseline green
dotnet build Mentoory.sln -warnaserror
dotnet test Mentoory.sln --no-build
```

Any failure here blocks CP-0.

## Global per-checkpoint recipe

Run this at the end of every checkpoint:

```bash
# 1. Build with warnings-as-errors
dotnet build Mentoory.sln -warnaserror

# 2. Test
dotnet test Mentoory.sln --no-build

# 3. Check staged vs intended files
git status --short
git diff --stat

# 4. Commit (template — fill in CP-N and summary)
git add <intended files>
git commit -m "[CP-N] QW-X: <summary>

<body>

Refs: specs/017-crosscut-hardening-phase0/plan.md

🤖 Generated with [Claude Code](https://claude.ai/claude-code)"

# 5. Push
git push origin 017-crosscut-hardening-phase0
```

If any step fails, STOP. Fix the root cause. Do not bypass (`--no-verify` is forbidden; warning suppression is forbidden; test skipping is forbidden).

---

## CP-0 — Baseline

**Scope**: Commit spec + plan + supporting docs + brainstorm assets. Do not modify source.

**Files to stage**:
- `specs/017-crosscut-hardening-phase0/**`
- `brainstorm/11-cross-cutting-code-audit.md`
- `brainstorm/00-overview.md`
- `.specify/feature.json`

**Verify**:
```bash
dotnet build Mentoory.sln -warnaserror  # MUST be green
dotnet test Mentoory.sln --no-build     # MUST be green
```

**Commit**:
```
[CP-0] Feature 017: spec, plan, and supporting artifacts

- spec.md, plan.md, research.md, data-model.md, quickstart.md, contracts/*
- brainstorm/11-cross-cutting-code-audit.md
- overview refreshed to include #11
```

---

## CP-1 — QW-1: Roles constants + hierarchy migration

**Scope**:
- Add `Mentoory.Shared.Domain/Constants/RoleHierarchies.cs` per [data-model.md § C-2](./data-model.md).
- Replace every hardcoded role string in:
  - `Mentoory.Web/Areas/**/Controllers/*.cs` `[Authorize(Roles = "...")]` attributes.
  - `Mentoory.Web/Infrastructure/Menu/MenuConfiguration.cs`.
  - Any other `.cs` file carrying role literals (grep for the six quoted role names).

**Verify role coverage before commit**:
```bash
# Expect empty (except for Roles.cs itself)
grep -rn '"GlobalAdmin"\|"IncubatorAdmin"\|"ProjectCoordinator"\|"Mentor"\|"Entrepreneur"\|"Sponsor"' \
  --include='*.cs' Mentoory.* 2>/dev/null \
  | grep -v Roles.cs | grep -v '/bin/' | grep -v '/obj/'
```

**Gotcha**: `[Authorize(Roles = RoleHierarchies.ProjectScope)]` — the `Roles = ` keyword in the attribute argument is unrelated to the `Roles` class; do not rename the attribute argument.

**Don't do yet**: `ProjectsController` is still on `IncubatorScope` in this CP (it uses the replaced `RoleHierarchies.IncubatorScope`). The fix to `ProjectScope` belongs to CP-2 (QW-7), so reviewers see the behaviour change as part of the security fix, not a refactor. Document the current-state behaviour preserved.

---

## CP-2 — QW-7: ship-violation fixes

**Scope**:
- `Mentoory.Web/Areas/Administration/Controllers/ProjectsController.cs`: change `[Authorize(Roles = RoleHierarchies.IncubatorScope)]` (as left by CP-1) to `[Authorize(Roles = RoleHierarchies.ProjectScope)]`.
- `Mentoory.Access.Application/Commands/RegisterUser/RegisterUserHandler.cs`: collapse the two conflict branches per [data-model.md § C-12](./data-model.md). Add `ErrorCode.Registration_Conflict` to the error-code enum.
- `tests/Mentoory.Access.Tests/Commands/RegisterUser/RegisterUserHandlerTests.cs` (or similar): add a new test that constructs two commands — one duplicating email, one duplicating national ID — and asserts identical `Result.ErrorCode` and `Result.ErrorMessages`.

**Verify**:
```bash
dotnet test Mentoory.sln --filter "FullyQualifiedName~RegisterUserHandler" --no-build
```

All RegisterUser tests pass; the new identical-shape test passes.

---

## CP-3 — QW-3: ITenantContextWriter

**Scope**:
- Add `Mentoory.Shared.Application/Interfaces/ITenantContextWriter.cs` per § C-7.
- Modify `TenantContextService` to implement both interfaces per § C-8.
- Update DI registration in `Mentoory.Web/Program.cs` (or the relevant extension method) to register the same scoped instance behind both interfaces.
- Modify `TenantContextMiddleware` to inject `ITenantContextWriter`; remove the `is TenantContextService` cast per § C-9.

**Verify**:
```bash
dotnet build Mentoory.sln -warnaserror
dotnet test Mentoory.sln --filter "FullyQualifiedName~Tenant" --no-build
# Integration tests that drive the middleware should still pass
```

**Manual smoke**: Run the app locally, sign in, confirm tenant context is set on subsequent requests.

---

## CP-4 — QW-5: ValidatorBehavior split

**Scope**:
- Delete `Mentoory.Shared.Application/Behaviors/ValidatorBehavior.cs`.
- Add `ResultValidatorBehavior.cs` and `ResultTValidatorBehavior.cs` per § C-10 and § C-11.
- Update DI registration in `Mentoory.Web/Program.cs` (two open-generic registrations replace one).

**Verify**:
```bash
dotnet build Mentoory.sln -warnaserror
dotnet test Mentoory.sln --no-build

# Confirm ConcurrentDictionary gone from Behaviors folder
grep -rn ConcurrentDictionary Mentoory.Shared.Application/Behaviors/  # expect empty
```

**Parity check**: Pick one existing validation test (any command with a `FluentValidator` that fails on invalid input) and confirm the returned `Result.ErrorCode` and `ErrorMessages` match pre-refactor values.

---

## CP-5 — QW-6: smoke tests in empty modules

**Scope**:
Add one `[Fact]` with a non-trivial assertion to each of:
- `tests/Mentoory.Knowledge.Tests/`
- `tests/Mentoory.Mentoring.Tests/`
- `tests/Mentoory.Notification.Tests/`
- `tests/Mentoory.Subscription.Tests/`

If a module currently has no production types worth asserting on, the fallback (per [research.md § R-9](./research.md)) is to exercise the module's DI registration:

```csharp
[Fact]
public void Module_DI_Registrations_Resolve()
{
    var services = new ServiceCollection();
    services.AddLogging();
    // Call the module's AddXxxApplication() / AddXxxInfrastructure() as applicable
    using var provider = services.BuildServiceProvider();
    // Assert that at least one module service resolves
    provider.GetService<ISomeModuleService>().Should().NotBeNull();
}
```

**Verify**:
```bash
dotnet test tests/Mentoory.Knowledge.Tests/Mentoory.Knowledge.Tests.csproj
dotnet test tests/Mentoory.Mentoring.Tests/Mentoory.Mentoring.Tests.csproj
dotnet test tests/Mentoory.Notification.Tests/Mentoory.Notification.Tests.csproj
dotnet test tests/Mentoory.Subscription.Tests/Mentoory.Subscription.Tests.csproj
# each MUST report ≥ 1 passing test
```

---

## CP-6 — QW-2: IDomainEvent migration (biggest CP)

Execute strictly in order. Do not commit mid-CP; commit only at the end.

**Sub-step 6.1 — add marker (non-breaking)**:
- Add `Mentoory.Shared.Domain/SeedWork/IDomainEvent.cs` per § C-3. Zero consumers yet.

**Sub-step 6.2 — add adapter (non-breaking)**:
- Add `Mentoory.Shared.Application/DomainEvents/DomainEventNotification.cs` per § C-4.

**Sub-step 6.3 — update Entity + dispatcher (BREAKING for concrete events)**:
- Modify `Entity.cs` to store `List<IDomainEvent>` per § C-5.
- Modify `SharedAbstractDbContext.DispatchDomainEventsAsync` to wrap via `MakeGenericType` per § C-6.
- Solution will FAIL to compile until 6.4 is done.

**Sub-step 6.4 — migrate concrete domain events**:
- Find every class that currently inherits `INotification` AND is a Domain-layer event (NOT `IIntegrationEvent` — those stay).
- Change the inheritance from `: INotification` to `: IDomainEvent`.
- Verify these classes live under `Mentoory.*.Domain` projects. If any live under `.Application`, they're probably integration events — leave them alone.

**Sub-step 6.5 — migrate handlers**:
- For every `INotificationHandler<TDomainEvent>`, change the generic to `INotificationHandler<DomainEventNotification<TDomainEvent>>`. Update the `Handle` method to unwrap `notification.DomainEvent`.
- The compiler is your checklist — run `dotnet build` and fix every error systematically.

**Sub-step 6.6 — remove MediatR from Domain csproj**:
- In `Mentoory.Shared.Domain.csproj`, remove the `<PackageReference Include="MediatR" />` line.
- Build. If it compiles, MediatR is no longer transitively required by Domain. If it fails, a type somewhere still references MediatR — fix and retry.

**Verify at CP close**:
```bash
# Zero MediatR references in Shared.Domain
grep -rn "using MediatR\|MediatR\." Mentoory.Shared.Domain/ --include='*.cs'  # expect empty
grep -rn "<PackageReference Include=\"MediatR" Mentoory.Shared.Domain/*.csproj  # expect empty

# Full test suite
dotnet build Mentoory.sln -warnaserror
dotnet test Mentoory.sln --no-build
```

**Manual smoke**: Trigger an event-emitting flow end-to-end (e.g., register a user). Confirm the event-handling effect still occurs (email queued, audit row, whatever the existing handler did).

---

## CP-7 — QW-4: architecture test project

**Scope**:
- Add `tests/Mentoory.Tests.Architecture/` project per § C-13.
- Add `Mentoory.Tests.Architecture` to `Mentoory.sln`.
- Add `<PackageVersion Include="NetArchTest.Rules" Version="..." />` to `Directory.Packages.props`.
- Implement one `[Fact]` per rule R-4.2.1 through R-4.2.8.
- Configure exclusions for generated code (Mapperly, EF).

**Verify**:
```bash
dotnet test tests/Mentoory.Tests.Architecture/Mentoory.Tests.Architecture.csproj
# All 8 rules MUST pass on the first run
```

**If a rule fails**: the failure is a pre-existing violation surfaced by the rule. Address it in this CP (extending the CP scope) or escalate to the user with the offender list. Do not silently suppress the rule.

---

## CP-8 — PR

**Scope**:
- Push final commit.
- Open PR on GitHub targeting `develop`:

```bash
gh pr create --base develop --head 017-crosscut-hardening-phase0 \
  --title "[017] Cross-cutting hardening — Phase 0 (Quick Wins)" \
  --body "$(cat <<'EOF'
## Summary
- Bundled 7 code-quality quick wins from the April 2026 cross-cutting infrastructure audit.
- Removes shipped constitution violations, adds mechanical enforcement via NetArchTest,
  and prepares the surface for the strategic Outbox, Audit, and Permission specs.

## Checkpoints landed
- CP-0 baseline
- CP-1 QW-1 RoleHierarchies + literal migration
- CP-2 QW-7 ProjectsController scope + RegisterUser enumeration fix
- CP-3 QW-3 ITenantContextWriter
- CP-4 QW-5 ValidatorBehavior split
- CP-5 QW-6 test-project baseline
- CP-6 QW-2 IDomainEvent migration + MediatR dropped from Shared.Domain
- CP-7 QW-4 Mentoory.Tests.Architecture with 8 rules

## Test plan
- [ ] CI green (build + test)
- [ ] Architecture-test rules all pass
- [ ] RegisterUser identical-shape test added and passes
- [ ] Manual smoke: event-emitting flows still dispatch to handlers
- [ ] Manual smoke: tenant context is set on authenticated requests

🤖 Generated with [Claude Code](https://claude.ai/claude-code)
EOF
)"
```

**Confirm CI is green** before requesting review.

---

## Failure handling

| Failure | Action |
|---|---|
| Build breaks mid-CP | Do not commit. Fix root cause. If fix expands scope, stop and escalate. |
| Test breaks mid-CP | Same as above. |
| Warning appears under `TreatWarningsAsErrors` | Do not suppress. Fix the root cause. |
| NetArchTest rule surfaces pre-existing violations (CP-7) | Address inline if small; escalate to user if large. Never silently relax the rule. |
| CP-6 event-handler migration misses a handler | Compiler will fail. Work through the list systematically. |
| `git push` rejected (branch protection, stale) | Rebase onto origin/017-..., resolve conflicts, re-run pre-checkpoint gates. |
