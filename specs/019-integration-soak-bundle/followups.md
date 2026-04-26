# Spec 019 — Bundle Ship Follow-ups

Spec 019 (integration soak bundle) shipped via PR #15 (squash commit `fbd5ac6` on `develop`, 2026-04-25), followed by docs-only PR #16 (`33d2147`) that landed the spec artefacts on `develop` after the bundle. Two follow-ups were chartered during the ship and could not be filed as GitHub issues because **the repository has issues disabled**. They are recorded here instead.

When repo issues are re-enabled (or migrated to another tracker), copy each section into a real issue and link back here.

---

## Post-ship verifications (resolved 2026-04-25)

- **GitGuardian alerts on PR #15 (22 incidents):** verified by reading every flagged file/line. Every flagged item is a fixture password used in tests (`Test123!@#`, `123abc987`, `Secure99887766!`, etc.); all 22 incidents trace to files under `tests/` only — no production code, no real credentials. **Nothing to rotate.** Future false-positive prevention: consider adding a `.gitguardian.yaml` with `paths-ignore: ["tests/**"]` (deferred — config schema not pinned in this session).
- **Local-machine post-ship gate on `develop` (`33d2147`):** Release build clean (0 warnings, 0 errors); CoverageCheck PASSED warn-mode (15 justified exclusions); E2E suite 181 passed / 0 failed / 5 skipped (pre-existing) / 6m58s.
- **CI scanners on closed PR #15:** four checks red but all advisory (no required-checks branch protection on `develop`). Coverage-gate workflow infrastructure failure: dotnet-install hit HTTP 404 on `builds.dotnet.microsoft.com/dotnet/Sdk/10.0.0/dotnet-sdk-10.0.0-linux-x64.tar.gz` — Microsoft CDN gap, not a policy fail. CodeQL/SonarCloud findings not investigated; deferred unless they recur on a future PR.
- **Branch cleanup:** `019-integration-soak` (integration) deleted from origin and local at T034/T035; `019-spec-docs` (post-ship docs PR) deleted at PR #16 cleanup; `019-integration-soak-bundle` (spec branch, redundant after PR #16) deleted at post-ship cleanup. No 019-* branches remain.

---

## FU-1 — T053: Backport `[Trait("Spec",..)]` attributes for spec 016/017/018 tests so coverage-check can run in strict mode

**Source:** spec 019 task `T053`, FR-005 deviation (Path B) recorded in `logs/branch-eligibility.txt`.

### Context

Spec 019 shipped 4 source PRs (#11/#12/#13/#14) with coverage-check running in **warn mode** per FR-005. The integrated build reports zero unclaimed identifiers, but the standalone build of registration #13 had failed strict-mode coverage-check with 33 unclaimed identifiers (Path B deviation).

The deviation was accepted because:

- FR-005 explicitly mandates `-p:CoverageCheckMode=warn` from the registration merge onward, so violations degrade to warnings on develop.
- T053 (this follow-up) was chartered to retrofit `[Trait("Spec", "FR-XXX-YY")]` attributes on tests for legacy specs 016/017/018 so coverage-check can re-enable strict mode on develop.

### Goal

Add `[Trait("Spec", "FR-XXX-YY")]` (and `[Trait("Sc", "SC-XXX-YY")]` where applicable) attributes to xUnit tests covering specs 016/017/018, then flip the develop build back to strict mode (default `-p:CoverageCheckMode=strict` or remove the override).

### Input list (from `logs/coverage-check.log` Exclusions section)

These identifiers are currently excluded — auditing only, not violations — and are the universe of work to retrofit:

**FR-018:**

- `FR-018-01` (`specs/018-access-security-delivery-quality-gate/spec.md:87`) — Constitution amendment Sections 11.9-11.14 (governance text)
- `FR-018-02` (`...:89`) — CI stage sequence (workflow YAML)
- `FR-018-03` (`...:91`) — Traceability rule (governance/tool self-tests)
- `FR-018-04` (`...:93`) — Scope anchor (front-matter convention)
- `FR-018-05` (`...:98`) — CLI shape (`contracts/coverage-check-cli.md` + every CI run)
- `FR-018-08` (`...:102`) — Floor enforcement (US3 deliverable, observable from tool report)
- `FR-018-09` (`...:104`) — `--mode warn` exit-code downgrade
- `FR-018-10` (`...:106`) — MSBuild integration (`Directory.Build.targets`)
- `FR-018-11` (`...:111`) — Workflow file + branch-protection (CI artifacts)
- `FR-018-12` (`...:113`) — CI stage sequencing
- `FR-018-13` (`...:118`) — Retrofit completion (observable from gate)
- `FR-018-14` (`...:120`) — Floor-trait retrofit completion

**SC:**

- `SC-016-05` (`specs/016-registration-access-hardening/spec.md:135`) — Performance-regression assertions
- `SC-018-02` (`specs/018-access-security-delivery-quality-gate/spec.md:155`) — Tool zero-violation exit IS the assertion
- `SC-018-03` (`...:157`) — Floor enforcement tool zero-missing-floor-categories report IS the assertion

The 33 unclaimed identifiers from the standalone-registration coverage-check failure live in the same exclusion universe — see `logs/standalone-registration.log` Exclusions section for the full list (`FR-016-{01..14}`, `FR-018-{06,07,15..21}`, `SC-016-{01..04}`, `SC-018-{01,04,05,06}`).

### Acceptance criteria

- [ ] All listed identifiers either gain a real `[Trait("Spec",..)]` / `[Trait("Sc",..)]` claim from at least one xUnit test, or are documented as bona-fide non-test identifiers (governance, CLI surface, observable-by-tool-itself) with an `excluded:` block in the spec front-matter.
- [ ] Floor categories `access-security: true` are claimed from at least one test for: `specs/006-{...}/spec.md`, `specs/016-registration-access-hardening/spec.md`, `specs/017-audit-e2e/spec.md`, `specs/018-access-security-delivery-quality-gate/spec.md`.
- [ ] `-p:CoverageCheckMode=warn` override removed from `Directory.Build.targets` (or wherever set); strict mode is the default.
- [ ] CI's "Spec coverage gate" workflow turns green on develop and on new PRs.

### Related artefacts

- PR #15 (squash `fbd5ac6` on `develop`)
- `logs/coverage-check.log` — integrated bundle run on develop (in archive after T055)
- `logs/standalone-registration.log` — standalone strict-mode failure on registration #13 (archived)
- `logs/branch-eligibility.txt` — Path B decision record (archived)

---

## FU-2 — T054a: Audit pipeline ↔ public-registration masking gap

**Source:** spec 019 task `T054`, integrated-bundle test skip recorded in commit `f57d8b6`.

### Symptom

`Mentoory.Tests.Integration.Audit.AuditPipelineTests.RegisterUser_DuplicateEmail_WritesFailureRow` is currently **skipped** (`[Fact(Skip = "...")]` introduced in commit `f57d8b6` during bundle integration).

### Cause

- `RegisterUserHandler` always returns `Result.Success()` per FR-016 anti-enumeration (the public registration endpoint must not leak whether an email or national ID is already registered).
- `AuditingBehavior` (the MediatR pipeline behavior that writes audit rows) reads the outer `Result` returned by the handler to decide `Outcome=Success | Failure`.
- Therefore on a duplicate-email registration attempt, the handler returns success → behavior writes `Outcome=Success` even though the operation didn't actually create a user.

The admin-enrollment path (`AdminEnrollUserCommand`) was deliberately split out during registration's restructure precisely to retain real `Outcome=Failure` audit rows for admin operations on duplicate identity. So this gap is **public-registration-specific**.

### Reconciliation options

Pick one (or propose another):

1. **Side-channel between handler and behavior**: handler stuffs the "real outcome" into a context bag (`HttpContext.Items` / `AsyncLocal` / explicit context object); behavior reads from that side channel rather than the outer `Result`. Adds coupling but cleanly preserves the handler's anti-enumeration contract.
2. **Move audit-write inside the handler** for masked commands: the handler explicitly calls `IAuditService` with the real outcome before returning the masked `Success()`. Loses behavior-pipeline uniformity but is explicit at the call site.
3. **Explicitly accept the loss for public registration**: document that public-registration audit rows always show `Outcome=Success` (enumeration would be observable via the audit log otherwise — same threat model). Rename or remove the skipped test; admin-enroll path retains real `Failure` rows.

The bundle PR shipped with option 3 implicitly (skipped test, no audit-row claim for masked failures).

### Acceptance criteria

- [ ] Decision recorded in `specs/016-audit-pipeline/spec.md` or in a new follow-up spec (e.g., `020-audit-registration-reconciliation`).
- [ ] Implementation lands per the chosen option.
- [ ] `AuditPipelineTests.RegisterUser_DuplicateEmail_WritesFailureRow` is either un-skipped (options 1+2) or removed/renamed with rationale (option 3).

### Related artefacts

- PR #15 (squash `fbd5ac6` on `develop`)
- Commit `f57d8b6` (bundle integration: skip duplicate-email audit test)
- Spec 016 audit-pipeline EC-4 (sequential resolution of registration restructure + audit attribute)

---

## FU-3 — T054b: Per-source-PR deferred items

These were deferred from the source PR descriptions and roll up under spec 019 follow-ups.

### Audit (#12)

Manual Spanish QA of audit viewer UI strings (translation review pass; not code-testable from xUnit).

### Lifecycle (#11)

Manual quickstart W1–W5 walkthrough (full coordinator UI walkthrough across all five workflow steps; documented as a manual test in `specs/016-project-lifecycle-finish/quickstart.md`).

### Knowledge (#14)

Deferred T092 UI work (resource attachment UI; deferred from PR #14 description as scope-trimmed).

Each can grow into its own follow-up if and when it becomes blocking.
