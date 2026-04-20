# Implementation Plan: Cross-cutting Hardening — Phase 0 (Quick Wins)

**Branch**: `017-crosscut-hardening-phase0` | **Date**: 2026-04-20 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/017-crosscut-hardening-phase0/spec.md`

## Summary

Land seven bundled cross-cutting quick wins as a deterministic, checkpointed sequence. Each checkpoint is a cohesive, independently reviewable slice with a hard pre-checkpoint gate (build green, tests green, zero warnings, no uncommitted changes) and ends with a commit + push. The checkpoint order is chosen to minimize rebase pain: architectural primitives (constants, markers, adapters) land before broad find/replace passes; the architecture-test suite lands last so every rule it encodes is already green.

## Technical Context

**Language/Version**: C# / .NET 10.0 (SDK 10.0.0)
**Primary Dependencies**: ASP.NET Core MVC, MediatR 14.1, FluentValidation 12.1, Mapperly 4.x, EF Core 10.x
**New Dependencies**: `NetArchTest.Rules` (test-only, added via `Directory.Packages.props`)
**Storage**: SQL Server with SSDT/DACPAC (no schema changes in this feature)
**Testing**: xUnit, Moq, FluentAssertions, Respawn, Testcontainers.MsSql (integration), Playwright (E2E). Architecture-test project uses xUnit + NetArchTest.Rules.
**Target Platform**: Linux/Windows .NET 10 runtime; Aspire 13.2.0 orchestration
**Project Type**: Modular monolith — multi-project solution
**Performance Goals**: N/A (refactor only; behavioral parity required)
**Constraints**: Zero-warnings (`TreatWarningsAsErrors=true`); Spanish-first UI; no `DateTime.UtcNow` in Domain/Application layers
**Scale/Scope**: ~50 controllers across 5 areas, 7 domain modules, ~18 concrete domain events, 4 empty test projects, 1 validator behavior to split, 1 middleware to harden

## Constitution Check

Gates evaluated against `.specify/memory/constitution.md` (v1.1.1) and `.specify/memory/access-security-constitution.md`.

| Gate | Status | Notes |
|---|---|---|
| §I Clean Architecture layer boundaries | **Planned-enforced** | QW-2 removes MediatR from Domain; QW-4 architecture tests mechanically enforce R-4.2.1, R-4.2.3, R-4.2.4 |
| §II CQRS pattern (IBaseRequest / BaseCommandHandler) | **Planned-enforced** | QW-4 rule R-4.2.7 asserts naming + inheritance |
| §III DDD constraints (aggregate control, ExternalId, value objects) | Unchanged | No entity modelling work |
| §IV Integration events placement | Unchanged | Existing integration events untouched |
| §V Zero-warnings policy | **Pre-checkpoint gate** | Every checkpoint MUST build with zero warnings |
| §VI DateTime handling (no UtcNow in Domain/Application) | **Planned-enforced** | QW-4 rule R-4.2.2 asserts this |
| §VII Naming conventions | **Planned-enforced** | QW-4 rule R-4.2.7 |
| §VIII File organization | Unchanged | Single class per file; no view JS in Views/ |
| §IX Spanish-first UI | **Planned-preserved** | QW-7 new error message is Spanish: `"No se pudo completar el registro. Verifica tus datos."` |
| §X Role Hierarchy & Session Context | **Planned-fixed** | QW-7 corrects `ProjectsController`; QW-1 centralizes hierarchy strings |
| §XI SSDT/DACPAC strategy | Unchanged | No database changes |
| Forbidden patterns (AutoMapper, Dapper as primary, service locator, static business logic, swallow exceptions) | **Planned-enforced** | QW-4 rules R-4.2.5, R-4.2.6 |
| Access-security constitution | **Planned-fixed** | QW-7 closes `RegisterUser` enumeration gap; QW-1 reduces role-string drift surface |

**Result**: PASS. No gate violations. The feature directly *strengthens* enforcement of these gates.

## Project Structure

### Documentation (this feature)

```text
specs/017-crosscut-hardening-phase0/
├── spec.md                 # feature specification (approved)
├── plan.md                 # this file
├── research.md             # Phase 0 — decisions & alternatives
├── data-model.md           # Phase 1 — new contracts (Roles, IDomainEvent, adapters)
├── quickstart.md           # Phase 1 — verification playbook
├── contracts/
│   ├── IDomainEvent.md
│   ├── DomainEventNotification.md
│   ├── ITenantContextWriter.md
│   └── RoleHierarchies.md
├── checklists/
│   └── requirements.md     # spec quality checklist
└── tasks.md                # Phase 2 output — created by /speckit.tasks
```

### Source Code (repository root)

```text
Mentoory.Shared.Domain/
├── Constants/
│   ├── Roles.cs                         # EXISTS — no change
│   └── RoleHierarchies.cs               # NEW (QW-1)
└── SeedWork/
    ├── Entity.cs                        # MODIFIED (QW-2) — stop referencing MediatR
    ├── IAggregateRoot.cs                # UNCHANGED
    ├── SoftDeletableEntity.cs           # UNCHANGED
    ├── ValueObject.cs                   # UNCHANGED
    └── IDomainEvent.cs                  # NEW (QW-2) — marker interface

Mentoory.Shared.Application/
├── Behaviors/
│   ├── ValidatorBehavior.cs             # DELETED (QW-5)
│   ├── ResultValidatorBehavior.cs       # NEW (QW-5)
│   └── ResultTValidatorBehavior.cs      # NEW (QW-5)
├── DomainEvents/
│   └── DomainEventNotification.cs       # NEW (QW-2) — IDomainEvent -> INotification adapter
└── Interfaces/
    ├── ITenantContext.cs                # UNCHANGED (read-only)
    └── ITenantContextWriter.cs          # NEW (QW-3)

Mentoory.Shared.Infrastructure/
└── Persistence/
    └── SharedAbstractDbContext.cs       # MODIFIED (QW-2) — wrap domain events via adapter

Mentoory.Access.Application/
└── Commands/RegisterUser/
    ├── RegisterUserHandler.cs           # MODIFIED (QW-7) — collapse error payload

Mentoory.Web/
├── Areas/**/Controllers/*Controller.cs  # MODIFIED (QW-1, QW-7) — use RoleHierarchies.*
├── Infrastructure/
│   ├── Authorization/
│   │   └── TenantContextMiddleware.cs   # MODIFIED (QW-3) — inject ITenantContextWriter
│   └── Menu/
│       └── MenuConfiguration.cs         # MODIFIED (QW-1) — use Roles / RoleHierarchies
└── Program.cs                           # MODIFIED (QW-3, QW-5) — DI registration changes

tests/
├── Mentoory.Access.Tests/               # MODIFIED (QW-7) — add RegisterUser identical-shape test
├── Mentoory.Knowledge.Tests/            # MODIFIED (QW-6) — add real smoke test
├── Mentoory.Mentoring.Tests/            # MODIFIED (QW-6) — add real smoke test
├── Mentoory.Notification.Tests/         # MODIFIED (QW-6) — add real smoke test
├── Mentoory.Subscription.Tests/         # MODIFIED (QW-6) — add real smoke test
└── Mentoory.Tests.Architecture/         # NEW (QW-4) — NetArchTest rules R-4.2.1..R-4.2.8

Directory.Packages.props                 # MODIFIED (QW-4) — add NetArchTest.Rules version
Mentoory.Shared.Domain.csproj            # MODIFIED (QW-2) — remove MediatR reference (final sub-step of CP-6)
Mentoory.sln                             # MODIFIED — add Mentoory.Tests.Architecture project
```

**Structure Decision**: Modular-monolith solution with per-module `{Module}.{Domain|Application|Infrastructure}` projects plus a `Mentoory.Web` entry point. Test projects live under `/tests/`. This feature adds **one new test project** (`Mentoory.Tests.Architecture`) and modifies files across Shared, Access, Web, and four empty test projects. No new source projects.

**Note on spec divergence (FR-001, FR-002)**: Spec specifies `Roles`/`RoleHierarchies` under `Mentoory.Shared.Application/Authorization/`. Actual existing code has `Roles` at `Mentoory.Shared.Domain/Constants/Roles.cs`. The plan places `RoleHierarchies` adjacent (`Mentoory.Shared.Domain/Constants/RoleHierarchies.cs`) to minimise churn; the Domain location is acceptable because these files hold only `const string` values with zero framework coupling. This divergence is documented in `research.md` (Decision R-1).

## Checkpoint Execution Plan

This plan mandates a **deterministic checkpoint sequence**. Each checkpoint is a cohesive, independently reviewable slice. The implementer MUST NOT proceed to the next checkpoint until the current one passes all pre-checkpoint gates.

### Pre-checkpoint gates (apply to every checkpoint)

Before a checkpoint is considered passed, ALL of the following MUST be true:

1. `dotnet build Mentoory.sln` — exits 0 with **zero warnings** (constitution §V).
2. `dotnet test Mentoory.sln` — all previously passing tests still pass; any newly added tests pass.
3. `git status --short` — clean (no unstaged, no uncommitted).
4. Commit message follows the repository convention and references the checkpoint ID (`[CP-N]`).
5. `git push origin 017-crosscut-hardening-phase0` — succeeds.

If any gate fails, the checkpoint is **rejected**. The implementer MUST fix the root cause (no `--no-verify`, no warning suppressions, no test skips). If root cause cannot be fixed without expanding scope, stop and surface to the user.

### Checkpoint sequence

| CP | QW | Scope | Why this order |
|---|---|---|---|
| **CP-0** | — | Baseline: commit spec + plan + brainstorm artifacts; confirm build + tests green on fresh branch | Establishes the baseline so subsequent CP diffs are clean |
| **CP-1** | QW-1 | Add `RoleHierarchies` constants; replace every `[Authorize(Roles = "...")]` and menu-config role literal with `Roles.*` / `RoleHierarchies.*` | Must precede CP-2 (uses `RoleHierarchies.ProjectScope`) and CP-7 (rule R-4.2.8 asserts zero role literals outside `Roles.cs`) |
| **CP-2** | QW-7 | Fix shipped violations: (a) `ProjectsController` uses `RoleHierarchies.ProjectScope`; (b) `RegisterUserHandler` collapses duplicate-email/nationalid errors; (c) new identical-response-shape test | Depends on CP-1 for `RoleHierarchies.ProjectScope`; small and low-risk — ships the security-relevant changes early |
| **CP-3** | QW-3 | Add `ITenantContextWriter`; `TenantContextService` implements both; DI registration change; `TenantContextMiddleware` removes cast | Independent of other CPs; small and contained |
| **CP-4** | QW-5 | Delete old `ValidatorBehavior<,>`; add `ResultValidatorBehavior<>` + `ResultTValidatorBehavior<,>`; update DI open-generic registration | Independent; mechanical |
| **CP-5** | QW-6 | Add one real `[Fact]` to each of Knowledge / Mentoring / Notification / Subscription test projects | Independent; establishes test-culture beachhead before the architecture tests land |
| **CP-6** | QW-2 | IDomainEvent migration. Internal sub-steps (single commit at CP close):<br>1) Add `IDomainEvent` in Shared.Domain (no consumers yet).<br>2) Add `DomainEventNotification<T>` adapter in Shared.Application.<br>3) Change `Entity.cs` to store `IDomainEvent`. Update `SharedAbstractDbContext.DispatchDomainEventsAsync` to wrap events in the adapter.<br>4) Migrate every concrete domain event from `: INotification` to `: IDomainEvent`.<br>5) Migrate every `INotificationHandler<TEvent>` to `INotificationHandler<DomainEventNotification<TEvent>>`.<br>6) Remove MediatR `PackageReference` from `Mentoory.Shared.Domain.csproj`.<br>7) Run full test suite; confirm event-emitting flows still work end-to-end. | Biggest change; lands AFTER small CPs so the branch is mostly-green when it arrives. Internal ordering is critical (sub-step 6 last) |
| **CP-7** | QW-4 | Add `Mentoory.Tests.Architecture` project; add NetArchTest.Rules to `Directory.Packages.props`; implement R-4.2.1 through R-4.2.8; all 8 rules green | MUST be last — every rule depends on prior CPs landing their respective changes. R-4.2.1 requires CP-6 done; R-4.2.8 requires CP-1 done |
| **CP-8** | — | PR: open PR to `develop`; confirm CI green; request review | Final gate |

### Checkpoint commit-message template

```
[CP-N] <Quick Win ID>: <concise summary>

<Body: what changed, references to spec FRs covered, verification steps run>

Refs: specs/017-crosscut-hardening-phase0/plan.md

🤖 Generated with [Claude Code](https://claude.ai/claude-code)
```

### Rollback strategy

- Each checkpoint is a single logical commit (or a small cluster). If a later checkpoint reveals a regression caused by an earlier one, `git revert <CP-commit>` MUST restore green state; if it does not, the CP was not truly self-contained — escalate.
- The branch MUST remain rebaseable onto `develop` at every checkpoint boundary. If `develop` drifts, rebase between CPs; never mid-CP.

### Per-checkpoint verification recipe

Each checkpoint's verification MUST include at minimum:

```bash
# 1. Build
dotnet build Mentoory.sln -warnaserror

# 2. Test
dotnet test Mentoory.sln --no-build

# 3. Git hygiene
git status --short    # expect empty
git diff develop --stat    # expect only intended files

# 4. Checkpoint close
git add <intended files>
git commit -m "[CP-N] ..."
git push origin 017-crosscut-hardening-phase0
```

Additional per-CP verification steps are documented in `quickstart.md`.

## Complexity Tracking

No constitution violations identified. The feature **strengthens** existing gates rather than introducing new complexity. Intentional deviations:

| Deviation | Why | Simpler Alternative Rejected Because |
|---|---|---|
| `RoleHierarchies` in `Shared.Domain/Constants/` rather than spec-suggested `Shared.Application/Authorization/` | `Roles` already lives in Domain/Constants; colocation keeps them discoverable and reduces churn | Moving both to Application requires touching every consumer of `Roles` (dozens of files) for no architectural gain; the files are framework-free constants |
| Single PR for 7 QWs instead of 7 PRs | Shared blast radius, shared rollback boundary, architecture-test rule coverage matures together | 7 micro-PRs multiply spec-kit overhead, introduce merge-order fragility, and delay the architecture-test landing gate |
| Architecture test R-4.2.2 may use source-grep fallback instead of IL inspection | NetArchTest cannot express "no `DateTime.UtcNow` references" at IL level in all cases | Accepted fallback — the rule is preserved; implementation detail documented in `research.md` |
