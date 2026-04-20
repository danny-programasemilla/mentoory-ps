# Research: Cross-cutting Hardening — Phase 0

**Spec**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md)

This document resolves all open decisions surfaced during planning. Each decision records rationale and rejected alternatives.

## R-1 — `Roles` / `RoleHierarchies` location

**Decision**: Keep the existing `Mentoory.Shared.Domain/Constants/Roles.cs` in place. Add `Mentoory.Shared.Domain/Constants/RoleHierarchies.cs` adjacent to it.

**Rationale**:
- `Roles.cs` already exists at `Shared.Domain/Constants/Roles.cs` and is referenced in ~20 call sites (verified via grep). Moving it is a large mechanical change with no architectural benefit.
- Both files hold only `public const string` values with zero framework coupling, so Domain is a valid home per constitution §I.
- Colocation keeps authorization identifiers discoverable from one folder.

**Alternatives considered**:
- **Move both to `Shared.Application/Authorization/`** (spec FR-001 original suggestion). Rejected: forces renames across every existing `Roles.*` consumer for aesthetic gain only.
- **Put `RoleHierarchies` in Application, `Roles` in Domain**. Rejected: splits related constants across layers, hurts discoverability.

**Impact on spec**: spec FR-001, FR-002 note `Shared.Application/Authorization/`. `data-model.md` and `plan.md` override that to `Shared.Domain/Constants/`. The intent of the FR (centralised constants, no duplication) is preserved.

## R-2 — Architecture test framework

**Decision**: `NetArchTest.Rules` (latest stable version; added to `Directory.Packages.props` for centralised version management).

**Rationale**:
- Minimal ceremony; reads as fluent C#.
- Covers 7 of 8 rules cleanly (assembly dependency, class-name patterns, namespace references).
- Actively maintained; stable API.

**Alternatives considered**:
- **ArchUnitNET**. Rejected: richer DSL but significantly more API surface; overkill for 8 initial rules.
- **Roslyn analyzers**. Rejected: powerful but every rule becomes a NuGet package with separate lifecycle; wrong tool for "forbid this pattern in a test".

## R-3 — Rule R-4.2.2 expression (`no DateTime.UtcNow in Domain/Application`)

**Decision**: Source-grep-based xUnit `[Fact]` inside the architecture-test project. If NetArchTest grows this capability later, migrate then.

**Rationale**:
- NetArchTest operates at assembly/metadata level and cannot detect method-call sites to `DateTime.UtcNow` reliably.
- A simple `[Fact]` that walks `*.cs` files under `Mentoory.*.Domain` and `Mentoory.*.Application` and asserts `DateTime.UtcNow` and `DateTime.Now` are absent is deterministic, fast, and readable.
- The check MUST exclude comments and string literals to avoid false positives.

**Alternatives considered**:
- **Roslyn analyzer as a separate NuGet package**. Rejected for this bundle (own lifecycle, too heavy). Revisit if rule count grows beyond ~15.
- **IL inspection via Cecil**. Rejected: over-engineering for a text check.

## R-4 — `IDomainEvent → INotification` adapter shape

**Decision**: Option (b) — a generic adapter `DomainEventNotification<TEvent> : INotification where TEvent : IDomainEvent`, constructed at the dispatch boundary. Concrete domain events implement only `IDomainEvent`.

**Rationale**:
- No per-event boilerplate (option a would require every concrete event class to implement both interfaces).
- Domain stays entirely MediatR-free (closes constitution §I gap completely, not partially).
- Handler subscription syntax becomes `INotificationHandler<DomainEventNotification<UserRegisteredEvent>>` — mechanical migration, greppable.

**Alternatives considered**:
- **Option (a): concrete events implement both `IDomainEvent` and `INotification`**. Rejected: leaves Domain transitively coupled to MediatR through its event types.
- **Dispatch via a custom in-process bus (drop MediatR entirely)**. Rejected: scope explosion; out of bundle.

## R-5 — Migration strategy for existing `INotification` domain events

**Decision**: Mechanical rename within CP-6. Use a grep-and-replace pass, then rely on the compiler to surface every consumer. The compiler output is the migration checklist.

**Rationale**:
- Compiler-driven refactors are deterministic and safe in a statically-typed language.
- MediatR's `INotificationHandler<T>` is contravariant in its type parameter; the wrapper introduction changes the `T`, which the compiler will flag at every handler site.

**Estimated surface** (verified via grep):
- `INotification` direct references: ~4 files in source (excluding integration events, which stay as `INotification` directly — they are not Domain events).
- `INotificationHandler<T>` where `T : IDomainEvent` (after migration): every current handler of a domain event — audited during implementation, not planning.

**Integration events vs domain events**: `IIntegrationEvent` intentionally inherits `INotification` in `Shared.Application/IntegrationEvents/`. It is an **Application-layer** cross-module contract, not a Domain event. It stays as-is. Only events emitted *from aggregates* (Domain events) migrate to `IDomainEvent`.

## R-6 — Checkpoint granularity

**Decision**: 9 checkpoints total (CP-0 through CP-8). One per quick win, plus baseline + final PR.

**Rationale**:
- User explicitly requested "checkpoints between chunks" with commit+push gates. Per-QW granularity is the natural unit: each QW is cohesive, independently reviewable, and maps to a single logical change.
- Smaller granularity (e.g. per-FR checkpoints) would fragment review and multiply commit overhead without improving rollback safety.
- Larger granularity (e.g. one CP for multiple QWs) would make rollback more expensive.

**Ordering principle**: dependency-first. An earlier CP never depends on a later one. CP-7 (architecture tests) is forced last because every rule it encodes depends on a prior CP having landed.

## R-7 — Behavioral parity for CP-4 (ValidatorBehavior split)

**Decision**: The two replacement behaviors MUST emit `Result.Failure` / `Result<T>.Failure` with identical `ErrorCode` (`ErrorCode.Validation_SomeFieldsAreInvalid`) and identical `ErrorMessages` tuple shape (`(Context, Message)`) as the prior reflection-based implementation. A regression test locks this invariant.

**Rationale**:
- The validator behavior is in the hot path of every command. Any shape change ripples to every controller's `MapErrorsToModelStateAndSetErrorToast<T>` consumer.
- A small dedicated test (submit an invalid command; capture the returned `Result`; assert every field-level invariant) is the cheapest way to prove parity.

**Alternatives considered**:
- **Snapshot testing of Result JSON**. Rejected: brittle, introduces a snapshot library for one test.
- **Manual smoke**. Rejected: relies on implementer discipline; not reviewable post-merge.

## R-8 — `RegisterUser` error message wording

**Decision**: `"No se pudo completar el registro. Verifica tus datos."` (Spanish, matches constitution §IX tone).

**Rationale**:
- Field-agnostic; leaks no information about which identifier conflicted.
- Actionable ("Verifica tus datos" prompts the user to re-check their input).
- Consistent with existing error-toast register (e.g., `"No se pudo guardar los cambios."`).

## R-9 — Test-culture smoke tests (CP-5)

**Decision**: Each smoke test must exercise real module code, not just assert assembly loads. Suggested initial tests:
- **Knowledge.Tests**: instantiate any existing Domain type (even if placeholder) and assert invariants.
- **Mentoring.Tests**: same.
- **Notification.Tests**: same.
- **Subscription.Tests**: same.

**Rationale**:
- The goal is establishing a test-culture beachhead, not coverage. One real test per project raises the floor from "zero expectations" to "every PR must maintain at least one test". A no-op would not achieve that floor.

**Fallback if a module is truly empty**: test the module's `AssemblyMarker` or `DependencyInjection` registration as a proxy. This is still a real test (DI container resolves the module without error) and meets the spirit of FR-019.

## R-10 — CI wiring for the architecture-test project

**Decision**: `Mentoory.Tests.Architecture` runs in the standard `dotnet test Mentoory.sln` invocation — no separate CI stage, no separate project filter.

**Rationale**:
- Minimal CI churn. Constitution enforcement becomes part of the normal test gate.
- Failure mode is a standard xUnit failure; existing CI reporting handles it without change.

## References

- Constitution: `.specify/memory/constitution.md` (v1.1.1)
- Access-security constitution: `.specify/memory/access-security-constitution.md`
- NetArchTest: <https://github.com/BenMorris/NetArchTest>
- MediatR contravariant handler dispatch: MediatR v12+ behavior preserved in v14.1
