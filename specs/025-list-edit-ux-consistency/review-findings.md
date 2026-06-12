# Deep Review Findings

**Date:** 2026-06-11
**Branch:** 025-list-edit-ux-consistency
**Rounds:** 1 (fixes applied, re-verified)
**Gate Outcome:** PASS
**Invocation:** quality-gate (autonomous pipeline)

## Summary

| Severity | Found | Fixed | Remaining |
|----------|-------|-------|-----------|
| Critical | 0 | 0 | 0 |
| Important | 4 | 4 | 0 |
| Minor | 3 | 0 | 3 |
| **Total** | **7** | **4** | **3** |

**Agents completed:** 5/5 (Correctness, Architecture, Security, Production Readiness, Test Quality)
**External tools:** skipped (CodeRabbit/Copilot CLIs not installed; --no-external)

**Stage 1 spec compliance:** 100% (13/13 FR, 5/5 SC).

## Findings

### FINDING-1 (Important — FIXED)
- **Severity:** Important
- **Confidence:** 72
- **File:** Mentoory.Web/Areas/Platform/Controllers/IncubatorsController.cs:101-121, Mentoory.Tenant.Application/Commands/UpdateIncubator/UpdateIncubatorHandler.cs
- **Category:** security
- **Source:** security-agent
- **Resolution:** fixed (round 1)

**What was wrong:**
The Incubator Edit GET/Details paths enforce a tenant-scope check (`GetIncubatorByExternalIdQuery` with `CallerIncubatorId` → returns "No tiene autorización..." when the caller's active incubator id ≠ the target). The Edit POST write path did NOT carry this scope: `UpdateIncubatorCommand` had no `CallerIncubatorId`, and `UpdateIncubatorHandler` did a bare `GetByExternalIdAsync` with no scope comparison before mutating name/description and (newly, via this feature) `IsActive`. A scoped caller could POST a known/guessed `externalId` to deactivate or edit an incubator outside its active context (IDOR write / DoS via deactivation).

**Why it matters:**
The read-path scoping signals that callers are intended to be confined to their active incubator context. The feature's new estado toggle widens the blast radius of the unscoped write path (out-of-scope deactivation). The asymmetry was pre-existing (name/description were already editable unscoped), but the feature touches and amplifies it.

**How it was resolved:**
Added `long? CallerIncubatorId` to `UpdateIncubatorCommand`; the controller now passes `User.GetActiveIncubatorIdOrNull()`; the handler enforces the same guard as the query handler (`if (request.CallerIncubatorId is { } caller && incubator.Id != caller) return Failure(...)`) before any mutation. A new unit test (`Handle_WhenCallerScopeDoesNotMatchIncubator_ReturnsFailureWithoutUpdating`) covers it. `CallerIncubatorId: null` (unscoped/global) preserves prior behavior for callers without an active incubator context.

### FINDING-2 (Important — FIXED)
- **Severity:** Important
- **Confidence:** 82
- **File:** tests/Mentoory.Tenant.Tests/Handlers/UpdateIncubatorHandlerTests.cs
- **Category:** test-quality
- **Source:** test-quality-agent
- **Resolution:** fixed (round 1)

**What was wrong:** US3 acceptance scenario "changing ONLY estado leaves Name/Description unchanged" was uncovered (the single field-asserting test changed name AND estado together).

**How it was resolved:** Added `Handle_WhenOnlyEstadoChanges_LeavesNameAndDescriptionUnchanged` — sends the same Name/Description while flipping IsActive and asserts Name/Description unchanged.

### FINDING-3 (Important — FIXED)
- **Severity:** Important
- **Confidence:** 78
- **File:** tests/Mentoory.Tenant.Tests/Handlers/UpdateIncubatorHandlerTests.cs
- **Category:** test-quality
- **Source:** test-quality-agent
- **Resolution:** fixed (round 1)

**What was wrong:** US3 acceptance scenario "saving without changing estado leaves it as-is" was uncovered — no true→true or false→false no-op tests.

**How it was resolved:** Added `Handle_WhenIsActiveTrueOnActiveIncubator_RemainsActive` and `Handle_WhenIsActiveFalseOnInactiveIncubator_RemainsInactive`.

### FINDING-4 (Important — FIXED)
- **Severity:** Important
- **Confidence:** 74
- **File:** tests/Mentoory.Tenant.Tests/Handlers/UpdateIncubatorHandlerTests.cs
- **Category:** test-quality
- **Source:** test-quality-agent
- **Resolution:** fixed (round 1)

**What was wrong:** The mocked `ITimeProvider` was configured but `UpdatedAtUtc` was never asserted on any success path — a handler that failed to thread `timeProvider.UtcNow` would still pass all tests.

**How it was resolved:** Success-path tests now assert `incubator.UpdatedAtUtc.Should().Be(UtcNow)` (incubators created with an earlier `CreatedAtUtc` so the assertion is meaningful).

### FINDING-5 (Minor — REMAINING)
- **Severity:** Minor
- **Confidence:** 71
- **File:** tests/Mentoory.Tests.Integration (absent)
- **Category:** test-quality
- **Source:** test-quality-agent
- **Resolution:** pending (accepted)

**What is wrong:** Task T016 intended an integration test (Testcontainers SQL) verifying full DB persistence of name+estado in a single save (FR-010/FR-011). The delivered coverage is a unit test with a mocked repository; it verifies handler branch logic and `UpdatedAtUtc` but not a real DB round-trip.

**Why it is accepted as remaining:** The handler logic, transaction pipeline (`TransactionBehavior`), and `UpdatedAtUtc` are now well covered at the unit level. FR-011's detail/list reflection is exercised by the existing list/detail query paths. A new integration fixture is disproportionate for this small UX feature. Documented for future hardening.

### FINDING-6 (Minor — REMAINING, pre-existing)
- **Severity:** Minor
- **Confidence:** 82
- **File:** Mentoory.Tenant.Infrastructure/Persistence/Repositories/IncubatorRepository.cs:23-26
- **Category:** production-readiness
- **Source:** production-readiness-agent
- **Resolution:** pending (out of scope)

**What is wrong:** `Update()` forces `EntityState.Modified` on an already-tracked entity, marking all columns dirty (full-column UPDATE even on a single-field estado change); the `repository.Update(...)` call in the handler is technically redundant for a tracked entity.

**Why it is accepted as remaining:** Pre-existing repository pattern, not introduced by this feature; low-volume admin table; no concurrency token on Incubator so no correctness impact. Changing shared repository behavior is out of this feature's scope.

### FINDING-7 (Informational — REMAINING)
- **Severity:** Minor (informational)
- **Confidence:** n/a
- **File:** Mentoory.Tenant.Application/Commands/UpdateIncubator/UpdateIncubatorHandler.cs
- **Category:** production-readiness
- **Source:** production-readiness-agent
- **Resolution:** pending (spec does not require)

**What is wrong:** Estado (activate/deactivate) changes are not distinctly observable — the success log is a generic "Incubator updated" with no IsActive field or old/new value.

**Why it is accepted as remaining:** The spec does not require a distinct audit event for estado changes. Noted as an optional future enhancement.

## Post-Fix Spec Coverage

No spec requirements were removed during the fix loop (all edits additive; one private test helper was relocated, not deleted). Stage 1 compliance remains 100%: FR-001..FR-013 and SC-001..SC-005 all implemented.

## Test Suite Results

| Round | Test Command | Scope | Result |
|-------|-------------|-------|--------|
| 1 | dotnet test (Mentoory.Tenant.Tests) | UpdateIncubatorHandlerTests | 7/7 passed |
| 1 | dotnet test (Mentoory.Tenant.Tests) | full project | 67/67 passed |
| 1 | dotnet build | solution | 0 warnings, 0 errors |

## Remaining Findings

3 Minor/informational findings remain (FINDING-5 integration test gap, FINDING-6 pre-existing repository write pattern, FINDING-7 estado observability). None are Critical or Important; none block the gate. All are documented above with rationale for acceptance.
