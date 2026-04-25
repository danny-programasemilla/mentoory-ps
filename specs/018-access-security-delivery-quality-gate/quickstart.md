# Quickstart: Access-Security Delivery Quality Gate

Verifies the feature end-to-end. Every step is deterministic and automatable; there is no "look at the output and decide" step. Run top-to-bottom locally before opening the implementation PR; CI runs the same checks on every push.

## Prerequisites

- Clean working tree on branch `018-access-security-delivery-quality-gate`.
- .NET 10 SDK installed.
- Docker running (needed for Testcontainers SQL Server in integration tests).
- Playwright browsers installed (`dotnet tool run playwright install chromium` from the `Mentoory.Tests.E2E` project directory).

---

## Step 1 — Solution builds clean and coverage tool compiles

```bash
dotnet build Mentoory.sln --configuration Release
```

**Expected**:

- Build succeeds with 0 warnings and 0 errors.
- Log contains a line roughly: `Mentoory.Specs.CoverageCheck -> tools/Mentoory.Specs.CoverageCheck/bin/Release/net10.0/Mentoory.Specs.CoverageCheck.dll`.
- Log contains a line at or near the end: `[CheckSpecCoverage] Mentoory.Specs.CoverageCheck ... Elapsed: <X>s` followed by `RESULT: PASSED (exit code 0)`.

**If it fails**: either the tool itself has a build error (fix and re-run) or the coverage check reports drift (read the `== Unclaimed Identifiers ==` / `== Dangling Traits ==` / `== Missing Floor Categories ==` sections, fix the offending tests or spec, re-run).

---

## Step 2 — Coverage tool reports zero violations for feature 016

```bash
dotnet out/coverage-tool/Mentoory.Specs.CoverageCheck.dll \
    --specs-root specs/016-registration-access-hardening/ \
    --test-assemblies 'tests/**/bin/Release/net10.0/Mentoory.*.Tests.dll' \
    --report-format text
```

(Adjust the binary path if running without a published `out/` directory — alternative: `dotnet run --project tools/Mentoory.Specs.CoverageCheck/ -- <args>`.)

**Expected**:

- stdout contains `Scanned: 1 spec files, 3 test assemblies, <N> test methods, <M> trait claims`.
- No `Unclaimed Identifiers` section, no `Dangling Traits` section, no `Missing Floor Categories` section.
- Final line: `RESULT: PASSED (exit code 0)`.
- Process exits with status `0`.

---

## Step 3 — Unit tests pass

```bash
dotnet test tests/Mentoory.Access.Tests/Mentoory.Access.Tests.csproj \
    tests/Mentoory.Specs.CoverageCheck.Tests/Mentoory.Specs.CoverageCheck.Tests.csproj \
    --configuration Release --no-build --logger "console;verbosity=minimal"
```

**Expected**:

- All existing 124 feature-016 unit tests pass (as before).
- Coverage-tool self-tests (`Mentoory.Specs.CoverageCheck.Tests`) pass — at minimum, tests named `CoverageAnalyzer_DetectsUnclaimedId_*`, `CoverageAnalyzer_DetectsDanglingTrait_*`, `CoverageAnalyzer_DetectsMissingFloorCategory_*`, `SpecParser_RejectsMalformedExclusion_*`.
- Total exit code `0`.

---

## Step 4 — Integration tests pass including the new FR-015 / FR-016 / FR-017 / FR-019 / FR-021 scenarios

```bash
dotnet test tests/Mentoory.Tests.Integration/Mentoory.Tests.Integration.csproj \
    --configuration Release --no-build --logger "console;verbosity=minimal"
```

**Expected**:

- Existing integration tests continue to pass.
- New tests pass: `AdminEnrollment_DuplicateNationalId_*` (FR-015), `AdminEnrollment_Valid_*` (FR-016), `AdminEnrollment_Unauthenticated_*` (FR-017), `PublicRegistration_FiftyProbeSweep_*` (FR-019), `DefenseInDepth_AntiforgeryMissing_*` and `DefenseInDepth_RateLimitEngages_*` (FR-021).
- `PublicRegistration_FiftyProbeSweep_*` completes in ≤ 15 seconds (verify by inspecting the test's timing assertion or xUnit duration column).

---

## Step 5 — E2E tests pass including FR-018 admin half and FR-020

```bash
dotnet test tests/Mentoory.Tests.E2E/Mentoory.Tests.E2E.csproj \
    --configuration Release --no-build --logger "console;verbosity=minimal"
```

**Expected**:

- Existing E2E tests continue to pass.
- New tests pass: `Registration_PasswordContainsNationalId_ShowsGenericBanner` (FR-018 public confirmation + parity with existing email case), `AdminEnroll_PasswordContainsIdentifyingData_ShowsAttributedError` (FR-018 admin half), `Registration_GenericBannerRender_PreservesNonSecretFields` (FR-020).

---

## Step 6 — Canary: removing a trait makes the build fail (SC-005)

From a clean tree at the end of Step 5:

```bash
# Remove one [Trait("Spec","FR-015")] attribute from its test method.
# Example: tests/Mentoory.Tests.Integration/Identity/AdminEnrollmentTests.cs
#   delete the line: [Trait("Spec", "FR-015")]

dotnet build Mentoory.sln --configuration Release
```

**Expected**:

- Build fails.
- Log contains:
  ```
  == Unclaimed Identifiers (coverage violation) ==
    [FR-015] specs/016-registration-access-hardening/spec.md:<line>
      Note: no test carries [Trait("Spec","FR-015")]
  ```
- Final line: `RESULT: FAILED (exit code 1)`.
- `dotnet build` overall exit code non-zero.

**Then**:

```bash
# Restore the trait attribute.
git checkout tests/Mentoory.Tests.Integration/Identity/AdminEnrollmentTests.cs
dotnet build Mentoory.sln --configuration Release
```

**Expected**: back to PASSED, exit 0.

---

## Step 7 — Canary: a new feature with an unclaimed FR is blocked (SC-006)

From a clean tree:

```bash
# Create a throwaway spec
mkdir -p specs/999-canary-test
cat <<'EOF' > specs/999-canary-test/spec.md
---
access-security: true
---

# Feature Specification: Canary Test

## Functional Requirements

- **FR-001** Canary requirement that no test claims.
EOF

dotnet build Mentoory.sln --configuration Release
```

**Expected**:

- Build fails.
- Log includes:
  ```
  == Unclaimed Identifiers (coverage violation) ==
    [FR-001] specs/999-canary-test/spec.md:8
      Note: no test carries [Trait("Spec","FR-001")]

  == Missing Floor Categories (coverage violation) ==
    specs/999-canary-test/spec.md — access-security: true
      - response-indistinguishability
      - outcome-audit-logging
      - public-vs-admin-attribution
      - form-state-preservation
      - defense-in-depth-controls
      - content-policy-rules
  ```

Wait — note the **identifier collision** hazard: feature 999 declares `FR-001`, and other specs (including 018 itself) declare their own `FR-001`. The tool MUST report this as a duplicate-identifier violation (EH-003):

```
== Duplicate Identifiers (coverage violation) ==
  FR-001 declared in:
    specs/018-access-security-delivery-quality-gate/spec.md:82
    specs/999-canary-test/spec.md:8
    ...
```

This is **expected** and is itself a canary — feature specs do NOT share identifier namespaces. In real usage, identifier numbering is local to each spec but the tool treats them globally to catch merge mistakes. If desired, the implementation MAY extend the tool to use per-spec namespaces in the future; for this feature, global namespace is accepted.

**Clean up**:
```bash
rm -rf specs/999-canary-test
dotnet build Mentoory.sln --configuration Release   # should PASS
```

---

## Step 8 — CI dry-run

Push the branch and verify the `coverage-check` job runs green on GitHub Actions:

```bash
git push origin 018-access-security-delivery-quality-gate
```

**Expected**:

- `coverage-check` job completes in ≤ 90 s, exit 0.
- `unit-tests`, `integration-tests`, `e2e-tests` jobs all run (because `coverage-check` passed) and all pass.
- Total pipeline finishes with all required checks green.

---

## Step 9 — CI canary dry-run (optional pre-merge sanity)

On a throwaway branch cut from `018-access-security-delivery-quality-gate`:

1. Remove one `[Trait("Spec", ...)]` attribute (same as Step 6).
2. Push.

**Expected**:

- `coverage-check` job fails with exit 1.
- `unit-tests`, `integration-tests`, `e2e-tests` jobs are SKIPPED (not run), because their `needs:` dependency failed. Saves CI minutes and demonstrates fail-fast behaviour.
- PR merge is blocked (assuming branch-protection has been configured to require `coverage-check`).

Delete the throwaway branch after the canary observation.

---

## Pass criteria summary

All of the following must be observable simultaneously:

- [x] Step 1: solution builds clean with tool firing.
- [x] Step 2: coverage tool reports 0 violations for feature 016.
- [x] Step 3: unit tests including tool self-tests pass.
- [x] Step 4: integration tests pass; 50-probe sweep ≤ 15 s.
- [x] Step 5: E2E tests pass.
- [x] Step 6: trait removal fails the build with a specific error.
- [x] Step 7: unclaimed FR fails the build with specific errors.
- [x] Step 8: CI `coverage-check` job passes.
- [x] Step 9 (optional): CI `coverage-check` failure skips downstream jobs.

If every check is green, feature 018 is ready for PR review.
