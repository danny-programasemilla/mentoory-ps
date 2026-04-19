# Contract: xUnit `[Trait]` Conventions

Defines the exact shape of trait attributes the coverage tool recognises, and the behaviour of tests that carry them.

## Recognised trait keys

The tool recognises exactly four trait keys. All other keys are ignored (but not rejected — teams may use them for filtering, IDE grouping, etc.).

| Key | Value format | Value regex | Purpose |
|-----|--------------|-------------|---------|
| `Spec` | `FR-###` | `^FR-\d{3}$` | Test claims it covers this functional-requirement identifier. |
| `Sc` | `SC-###` | `^SC-\d{3}$` | Test claims it covers this success-criterion identifier. |
| `Floor` | kebab-case | `^[a-z][a-z0-9-]+$` + membership in the canonical list | Test contributes to this floor category. |
| `Flaky` | literal `true` | `^true$` | Quarantines the test (NFR-002). |

### Canonical floor names

Exactly six values are accepted for `Floor`:

1. `response-indistinguishability`
2. `outcome-audit-logging`
3. `public-vs-admin-attribution`
4. `form-state-preservation`
5. `defense-in-depth-controls`
6. `content-policy-rules`

Any other Floor value causes the build to fail with a clear message naming the test method and the offending value.

## Syntax

Standard xUnit `TraitAttribute` applied to a test method. Multiple attributes on the same method are supported.

```csharp
[Fact]
[Trait("Spec", "FR-015")]
[Trait("Sc", "SC-002")]
[Trait("Floor", "response-indistinguishability")]
public async Task Handle_DuplicateNationalId_ReturnsFieldAttributedError() { ... }
```

Method-level traits only. Class-level and assembly-level traits are ignored (xUnit supports them; tool deliberately does not, because coverage is granular per test method).

## Claim semantics

A test method is said to **claim** an identifier (or floor category) when:

1. It is decorated with the corresponding `[Trait]` attribute.
2. The test is NOT marked `Skip`:
   - `[Fact(Skip = "reason")]` → non-claiming.
   - `[Theory(Skip = "reason")]` → non-claiming.
3. The test is NOT quarantined as flaky:
   - Any method with `[Trait("Flaky", "true")]` is treated as non-claiming for every other trait on the method.

A claim counts exactly once per (method, identifier) pair, regardless of how many times the trait is repeated.

## Skip with a claim still makes the claim

When a test is `Skip`-ped, ALL its claims are discarded. Example:

```csharp
[Fact(Skip = "awaiting FR-XYZ resolution")]
[Trait("Spec", "FR-020")]
public async Task FormStatePreserved() { ... }
```

If this is the only test claiming `FR-020`, the tool reports `FR-020` as unclaimed. This is intentional (EC-001) and aligns with NFR-002's flaky-test policy: a disabled test is not coverage.

## Quarantine workflow

When a previously-passing trait-carrying test becomes flaky in CI:

1. Within 24 hours, add `[Trait("Flaky", "true")]` to the method (PR merged fast-path).
2. Open an issue describing the symptom, referencing the test and the identifiers it previously claimed.
3. The coverage tool will treat the test as non-claiming on the next run. If the test was the only claimant for some identifier, the tool reports that identifier as unclaimed → build fails → forces either fixing the test or finding another claim (or marking the identifier `Coverage: N/A` with a justification, if genuinely untestable at this time).

This workflow is intentionally noisy. The goal is drift visibility, not convenience.

## Retrofit mapping for feature 016

Implementation-phase concern; the plan does not prescribe which specific test claims which specific identifier. The retrofit task (Phase 2 tasks.md) produces a mapping table. Minimum rules for that mapping:

- Every feature-016 FR/SC must end up with ≥1 non-skipped test carrying the corresponding trait.
- Each floor category must end up with ≥1 test carrying the corresponding `Floor` trait.
- A single test method may claim multiple identifiers and a floor category; this is encouraged for tests that genuinely exercise several contract surfaces.

## Inter-file consistency

When an identifier is claimed by more than one test (multi-claim), the coverage tool counts it as claimed once. This is the expected case for identifiers exercised at multiple test layers (unit + integration + E2E).

Collisions on identifier declarations across different spec files are errors (EH-003). Collisions on trait claims across different tests are normal and expected.
