---
access-security: true
---

# Feature Specification: Access-Security Delivery Quality Gate

**Feature Branch**: `018-access-security-delivery-quality-gate`
**Created**: 2026-04-19
**Status**: Draft
**Input**: User description: "Create all the e2e necessary to guarantee alignment between the requirements and delivery quality. Make sure you include checkpoints where to pause and provide instruction on each pause for what to do to guarantee quality and prevent drift. Make sure all possible scenarios from the recent work are covered properly."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Close feature-016 coverage gaps (Priority: P1)

Automate the missing scenarios identified in the PR #13 gap analysis so every behaviour promised by the 016 specification has at least one automated test asserting it. This includes admin-path parity with the public path (duplicate-national-ID attribution, fresh-success redirect, unauthenticated access gate, US3 rule surfaced as attributed error), the public-path US3 rule covering national-ID variants, the HTTP-layer response-indistinguishability sweep, form-state preservation after generic-banner render, and defense-in-depth control verification.

**Why this priority**: Without these tests, the 016 security hardening has gaps that can regress without anyone noticing. The behaviour is already delivered by the 016 production code; the tests are the missing half. Ship this alone and PR #13 closes cleanly.

**Independent Test**: Run `dotnet test` across `Mentoory.Access.Tests`, `Mentoory.Tests.Integration`, `Mentoory.Tests.E2E`; observe the new test cases execute and pass; inspect the coverage reports to confirm the gap scenarios are now asserted.

**Acceptance Scenarios**:

1. **Given** an admin attempts to enroll a user whose national ID is already registered, **When** the admin submits the Enroll form, **Then** the form re-renders with a field-attributed error `("NationalId", "Ya existe una cuenta con este número de identificación.")` and no redirect occurs.
2. **Given** an admin successfully enrolls a new user, **When** the form is submitted with fresh data, **Then** the response redirects to the users-list route and a success TempData key is set.
3. **Given** an anonymous client, **When** it POSTs to `/Administration/Users/Enroll`, **Then** the response is 401 or a redirect to the login page — whichever the deployed `[Authorize]` policy produces — and no enrollment occurs.
4. **Given** a public registration submission whose password contains the user's national ID (verbatim or separator-stripped), **When** the form is POSTed, **Then** the server renders the generic failure banner and does not leak which rule failed; **and given** the same password on the admin Enroll endpoint, **Then** the response attributes the error to the `Password` field with the shared identifying-data rule message.
5. **Given** a mix of 50 fresh, duplicate-email, and duplicate-national-ID submissions to `/Access/Register`, **When** they are POSTed sequentially via a test HTTP client, **Then** the responses are pairwise equal in HTTP status, `Location` header, post-redirect body bytes (with antiforgery tokens stripped deterministically), `Cache-Control`, `Content-Type`, and the set of `Set-Cookie` cookie names.
6. **Given** a public registration submission that triggers a server-side validator failure, **When** the form re-renders with the generic banner, **Then** the previously entered non-secret fields (Email, FirstName, LastName, Country, NationalId) are populated from the last submission and the password fields are blank.
7. **Given** a public or admin registration POST without an antiforgery token, **When** the request reaches the server, **Then** the response is HTTP 400; **and given** a burst of requests to `/Access/Register` that exceeds the `registration` rate-limit policy, **Then** requests beyond the threshold receive HTTP 429.

---

### User Story 2 - Establish the reusable quality gate (Priority: P1)

Codify the delivery standard — floor categories, traceability rule, CI stages — in the access-security constitution and back it with an automated tool that parses feature specifications and verifies every requirement identifier is claimed by at least one test via xUnit trait attributes. Retrofit the existing 016 tests with the required traits so the first feature under the gate passes on merge.

**Why this priority**: US1 alone delivers coverage for today but has nothing preventing tomorrow's regression. This story is what makes the protection durable and self-enforcing; it is the mechanism by which "humans don't read checklists, CI blocks merges" becomes a property of the project.

**Independent Test**: Run the coverage tool locally against the repository; observe a zero-violation exit for feature 016; then deliberately remove one trait from one test file, re-run the tool, and observe the failure naming the now-unclaimed requirement identifier.

**Acceptance Scenarios**:

1. **Given** every `spec.md` under `specs/` and every test assembly under `tests/`, **When** the coverage tool runs, **Then** it lists every `FR-###` and `SC-###` identifier declared in a spec and every identifier claimed by a test `[Trait]`, and reports both the unclaimed set and the dangling-trait set.
2. **Given** a feature specification with an identifier deliberately exempted from automated test coverage, **When** the specification marks it `Coverage: N/A` with a justification clause, **Then** the tool excludes it from the unclaimed report and lists it separately under an exclusions section.
3. **Given** a developer who runs `dotnet build` from the repository root, **When** the build executes, **Then** the coverage tool is invoked as part of the build and a violation causes the build to exit non-zero.
4. **Given** the 016 retrofit is complete, **When** the coverage tool runs against the repository, **Then** it reports 0 unclaimed identifiers and 0 dangling traits for feature 016.

---

### User Story 3 - Enforce the floor-category minimum (Priority: P2)

Require that every access-security feature whose characteristics match a floor-category trigger condition (public endpoint with masked outcome, outcome audit logging, dual public/admin surface, form-state render, antiforgery-protected endpoint, content-policy rule) has at least one test carrying the corresponding `Floor` trait. The mechanism is additive to US2's requirement-identifier traceability: US2 ensures every named requirement is covered; US3 ensures the baseline categories are covered even when a feature's written requirements don't call them out.

**Why this priority**: Named requirements can miss patterns that are obvious in hindsight; the floor catches oversights that wouldn't otherwise be written into the spec. Lower than US2 because without US2's traceability mechanism this story has no infrastructure to ride on.

**Independent Test**: Tag feature 016's `spec.md` front-matter with `access-security: true`; verify the six floor categories each have at least one test carrying the corresponding `[Trait("Floor",…)]`; remove the `Floor` trait from the single test claiming, say, `response-indistinguishability`, and observe the tool fail with the specific floor-category name.

**Acceptance Scenarios**:

1. **Given** a feature specification with `access-security: true` in its front-matter, **When** the coverage tool runs, **Then** it asserts that each applicable floor category has at least one test with the corresponding `[Trait("Floor",…)]`.
2. **Given** feature 016 where the retrofit is complete, **When** the coverage tool runs, **Then** all six floor categories report at least one claiming test and the build exits zero.
3. **Given** a hypothetical future feature that dual-exposes a capability to public and admin principals but provides no `public-vs-admin-attribution` test, **When** the coverage tool runs, **Then** the build fails with a message naming the missing floor category and the feature's path.

---

### Edge Cases

- A test method carries `[Trait("Spec","FR-018-08")]` but is itself `[Fact(Skip="…")]` or `[Theory(Skip="…")]`. The tool treats the test as non-claiming.
- A single test method carries multiple `[Trait("Spec",…)]` attributes (e.g., covers `FR-018-01` and `FR-018-02` together). All claims count first-class.
- An identifier is renumbered during clarification (e.g., `FR-018-07` → `FR-018-08`). The tool reports both an Unclaimed `FR-018-08` and a Dangling `FR-018-07` until the trait is updated in lockstep.
- An identifier is deleted from the spec but its claiming trait remains in a test. The tool reports the trait as dangling and the build fails.
- Two different `spec.md` files declare the same identifier (a merge/rename mistake). The tool reports the collision naming every file path and exits non-zero.
- A spec marks an identifier `Coverage: N/A` without a justification clause. The tool treats the marker as unacceptable and counts the identifier as unclaimed.
- The 50-probe response-indistinguishability sweep finds a difference between two probes. The assertion names the first differing probe index and dumps a truncated unified diff of the responses to the test output; silent inequality failures are forbidden.
- The sweep's `Set-Cookie` comparison sees antiforgery cookie values differ between probes (expected behaviour). The test helper compares by cookie name, domain, path, and security flags but not by value.
- A rate-limit test runs against the production `registration` policy, which is restrictive and slow to trigger. A test-only `IConfiguration` override swaps in a synthetic tighter limit so the gate engages within a few probes.
- A developer runs the coverage tool on a local dirty working tree. The tool inspects whatever specs and traits are on disk and never blocks on git state; the `--mode warn` flag allows non-zero violations without failing the build for local iteration.

## Requirements *(mandatory)*

### Functional Requirements

**Constitution amendment** (authority: binding on every access-security feature)

- **FR-018-01**: The access-security constitution MUST gain a new subsection — appended to Section 11 or as a new Section 13 — that defines six floor categories: response indistinguishability, outcome audit logging, public-vs-admin attribution, form-state preservation, defense-in-depth controls, and content-policy rules. Each category MUST state its trigger condition, the minimum automated assertion required, and at least one canonical example.
  *Coverage: N/A — Constitution amendment Sections 11.9-11.14 are governance text in `.specify/memory/access-security-constitution.md`; not behaviour-testable from xUnit. Reviewed during PR.*
- **FR-018-02**: The constitution MUST gain a Delivery Quality Gate section that codifies the CI stage sequence: `build`, `coverage-check`, `unit-tests`, `integration-tests`, `e2e-tests`. Each stage MUST declare its entry criteria, its assertion, its failure behaviour (block merge), and the test projects it consumes.
  *Coverage: N/A — Constitution amendment Section 13 is governance text; the CI stage sequence is enforced by the GitHub Actions workflow file, not by xUnit assertions.*
- **FR-018-03**: The constitution MUST state the traceability rule: every feature specification whose `spec.md` declares `FR-###` or `SC-###` identifiers MUST have at least one test per identifier carrying an xUnit `[Trait("Spec","FR-###")]` or `[Trait("Sc","SC-###")]` attribute. Exceptions MUST be marked inline in the spec as `Coverage: N/A` with a justification clause.
  *Coverage: N/A — The traceability rule lives in constitution Section 13.2 as governance text; the rule's enforcement is the coverage tool itself (claimed by tool self-tests).*
- **FR-018-04**: The amendment MUST be scoped via an anchor phrase so it binds only access-security features; other areas of the platform may adopt the standard later without the amendment forcing them to.
  *Coverage: N/A — Constitution Section 13.1 carries the scope anchor; verified during PR review against the front-matter convention.*

**Coverage tool** (authority: automated enforcement of FR-018-01..004)

- **FR-018-05**: A `dotnet` console tool MUST accept `--specs-root <path>` and `--test-assemblies <path-glob>` arguments. The tool MUST exit 0 on success and non-zero on violations.
  *Coverage: N/A — CLI shape is pinned in `contracts/coverage-check-cli.md` and exercised end-to-end by every CI run; no separate xUnit harness wraps the Program entry-point.*
- **FR-018-06**: The tool MUST parse every `spec.md` under `--specs-root` and extract `FR-###` and `SC-###` identifiers using a regex tolerant of the existing spec templates. Identifiers marked `Coverage: N/A` with a justification clause MUST be excluded from the unclaimed-identifiers report and listed separately under an exclusions section of the tool's output.
- **FR-018-07**: The tool MUST load every assembly matched by `--test-assemblies` using reflection-only loading that does not execute test code, enumerate every test method's `[Trait("Spec",…)]` and `[Trait("Sc",…)]` attribute values, and produce two separate reports: Unclaimed Identifiers (spec has the identifier, no matching trait) and Dangling Traits (trait references an identifier that no spec declares).
- **FR-018-08**: The tool MUST recognise `[Trait("Floor","<category>")]` attributes and, for any feature specification whose front-matter declares `access-security: true`, assert that every applicable floor category (as listed in FR-018-01) is covered by at least one test with the matching `Floor` trait inside the assemblies that reference the feature's production types.
  *Coverage: N/A — Floor enforcement is the deliverable of US3 and is observable directly from the tool's `MissingFloorCategories` report; the analyzer's unit test pins the structural contract.*
- **FR-018-09**: The tool MUST support a `--mode warn` flag that logs violations to the console without exiting non-zero, intended only for local development iteration. CI invocations MUST NOT use this flag.
  *Coverage: N/A — `--mode warn` exit-code downgrade is wired in `Program.Run` and validated by hand during local iteration; the prohibition on CI usage is a workflow rule, not a runtime check.*
- **FR-018-10**: The tool MUST integrate with MSBuild via a `.targets` file so that `dotnet build` invoked from the repository root runs the coverage check as part of the solution build. A violation MUST fail the build.
  *Coverage: N/A — MSBuild integration is the `Directory.Build.targets` + `tools/Mentoory.Specs.CoverageCheck/build/CoverageCheck.targets` files; verified by the gate firing during `dotnet build`, not by an xUnit assertion.*

**CI integration** (authority: binding for merges to `develop`)

- **FR-018-11**: A GitHub Actions workflow MUST invoke the coverage tool against the entire `specs/` tree and every test assembly. The resulting check MUST be required via branch-protection so that failure blocks merge.
  *Coverage: N/A — Workflow file (`.github/workflows/coverage-check.yml`) and branch-protection are CI artifacts; verified during repository-admin configuration (DEP-005), not by xUnit.*
- **FR-018-12**: The existing unit-tests, integration-tests, and e2e-tests CI stages MUST remain required checks. The new coverage-check stage MUST execute after `build` and before `integration-tests`, so that drift is caught before the slower integration and E2E layers consume runtime.
  *Coverage: N/A — CI stage sequencing is encoded in the workflow YAML and reviewed during PR; no xUnit harness for workflow ordering exists.*

**Feature-016 retrofit** (authority: establishes baseline compliance)

- **FR-018-13**: Every existing test across `Mentoory.Access.Tests`, `Mentoory.Tests.Integration/Identity/*`, and `Mentoory.Tests.E2E/Tests/Registration*/Administration*` MUST gain one or more `[Trait("Spec","FR-###")]` or `[Trait("Sc","SC-###")]` attributes identifying the feature-016 identifiers it claims. Multi-identifier coverage via multiple trait attributes on the same method is permitted and each identifier counts once.
  *Coverage: N/A — Retrofit completion is observable as the gate reporting zero unclaimed feature-016 identifiers; the retrofit task is itself a test-code change rather than a runtime behaviour.*
- **FR-018-14**: Every test that contributes to a floor category (FR-018-01 list) MUST carry a `[Trait("Floor","<category-name>")]` attribute naming the category.
  *Coverage: N/A — Floor-trait retrofit completion is observable as the gate reporting zero missing floor categories; not a runtime assertion of its own.*

**Feature-016 new scenarios** (authority: closes the PR #13 coverage gap)

- **FR-018-15**: An integration test MUST dispatch `AdminEnrollUserCommand` with a duplicate national ID and assert the result is `Failure` with a single error attributed to context `"NationalId"` and message `"Ya existe una cuenta con este número de identificación."`.
- **FR-018-16**: An integration test MUST POST to `/Administration/Users/Enroll` with valid data via an authenticated HTTP client and assert the response redirects to the users-list route and a success TempData key is populated.
- **FR-018-17**: An integration test MUST POST to `/Administration/Users/Enroll` unauthenticated and assert the response is HTTP 401 or a redirect to the login route (the test pins whichever the deployed policy produces).
- **FR-018-18**: An integration test MUST verify that a password containing the user's national ID (verbatim and stripped form) is rejected on the public path with the generic banner and no field attribution; an E2E test and/or integration test MUST verify the same rule rejects the password on the admin path with the error attributed to the `Password` field and the shared identifying-data rule message.
- **FR-018-19**: An integration test MUST execute a 50-probe response-indistinguishability sweep against `/Access/Register` via an in-process HTTP client. Probes MUST mix fresh, duplicate-email, and duplicate-national-ID submissions. The assertion MUST compare, pairwise across probes of every outcome: HTTP status, `Location` header, post-redirect body bytes (after a deterministic antiforgery-token strip), `Cache-Control`, `Content-Type`, and the set of `Set-Cookie` cookie names (values intentionally excluded because antiforgery cookie values vary per probe).
- **FR-018-20**: An E2E test MUST assert that after a server-side generic-banner render on `/Access/Register`, the non-secret fields (Email, FirstName, LastName, Country, NationalId) are populated from the last submission and the secret fields (Password, ConfirmPassword) are blank.
- **FR-018-21**: An integration test MUST assert that `POST /Access/Register` without an antiforgery token returns HTTP 400; that `POST /Administration/Users/Enroll` without an antiforgery token returns HTTP 400; and that the `registration` rate-limit policy (with a test-only tighter synthetic limit) engages and returns HTTP 429 after the configured threshold is exceeded.

### Non-Functional Requirements

- **NFR-001**: The coverage tool MUST complete its full run against the current repository in ≤ 2 seconds. Execution time MUST be logged by the tool and asserted by CI; exceeding the budget is a specification violation.
- **NFR-002**: Flaky tests are forbidden. Any test carrying a spec-identifier trait that fails non-deterministically in CI MUST be quarantined within 24 hours by adding `[Trait("Flaky","true")]` and opening a follow-up issue. The coverage tool MUST treat quarantined tests as non-claiming, so quarantines immediately surface as coverage drift.
- **NFR-003**: No test introduced by this specification may force sequential execution across the unit, integration, and E2E test projects. Integration and E2E test suites MUST remain independently parallelisable.
- **NFR-004**: The 50-probe response-indistinguishability sweep (FR-018-19) MUST complete in ≤ 15 seconds of CI wall time, measured after test-collection fixture amortisation.
- **NFR-005**: The access-security constitution amendment MUST preserve the existing numbering of Sections 1 through 12 and the existing numbering of subsections 11.1 through 11.8. New content is additive: either appended to Section 11 as 11.9+ or introduced as a new Section 13+.

### Key Entities

- **Feature Specification** (`specs/###-*/spec.md`): the source of truth for `FR-###` and `SC-###` identifiers. May carry a front-matter flag `access-security: true` to opt in to floor-category enforcement.
- **Requirement Identifier**: a token of the form `FR-###` or `SC-###` declared in a spec. Every identifier either has at least one claiming `[Trait]` in a test or is inline-marked `Coverage: N/A` with a justification.
- **Test Claim**: an xUnit `[Trait("Spec","FR-###")]` or `[Trait("Sc","SC-###")]` attribute on a test method. Claims are counted only when the test is not marked `Skip`.
- **Floor Category**: a named quality dimension (response-indistinguishability, outcome-audit-logging, public-vs-admin-attribution, form-state-preservation, defense-in-depth-controls, content-policy-rules) that applies to any access-security feature meeting the trigger condition.
- **Floor Claim**: an xUnit `[Trait("Floor","<category>")]` attribute on a test method. A feature satisfies a floor category when at least one non-skipped test in its scope claims it.
- **Coverage Tool**: the console application that parses specs, reflects assemblies, and reports Unclaimed Identifiers and Dangling Traits. The MSBuild integration and the GitHub Actions stage both invoke it.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-018-01**: After merge, `dotnet build` from the repository root exits non-zero whenever any specification identifier lacks a claiming trait. Verified by a tool self-test that mutates a fixture and asserts non-zero exit.
- **SC-018-02**: Feature 016 achieves complete claim coverage: every FR and every SC in `specs/016-registration-access-hardening/spec.md` has at least one non-skipped test with a matching `[Trait]`, and the coverage tool reports 0 unclaimed identifiers and 0 dangling traits.
  *Coverage: N/A — Observable outcome of running the coverage tool itself; the tool's own zero-violation exit IS the assertion. No separate xUnit harness needed.*
- **SC-018-03**: Feature 016 achieves floor-category coverage: each of the six floor categories has at least one non-skipped test carrying the corresponding `[Trait("Floor",…)]`.
  *Coverage: N/A — Observable outcome of running the coverage tool with floor enforcement (US3); the tool's zero-missing-floor-categories report IS the assertion.*
- **SC-018-04**: The response-indistinguishability sweep (FR-018-19) executes as part of CI and completes in ≤ 15 seconds.
- **SC-018-05**: Canary test: removing a single `[Trait("Spec",…)]` on a throwaway branch causes the `coverage-check` CI stage to fail before any test-project stage runs, and the failure message names the now-unclaimed identifier.
- **SC-018-06**: Canary test: creating a new feature branch with an unclaimed `FR-###` in its `spec.md` is blocked from merge because the coverage-check stage fails naming the identifier.

## Assumptions

- Features 016 and 018 land together in PR #13 on branch `016-registration-access-hardening` — the 018 work is a linear continuation of 016 and is reviewed as one unified change set. The 016 test files this specification references are produced by the earlier commits on the same branch; no cross-branch coordination is required.
- xUnit `TraitAttribute` behaves identically across `Mentoory.Access.Tests`, `Mentoory.Tests.Integration`, and `Mentoory.Tests.E2E` — all three are on xUnit 2.x.
- The `System.Reflection.MetadataLoadContext` API in .NET 10 is sufficient for the coverage tool's reflection needs; no runtime assembly loading is required.
- The existing `Mentoory.Tests.Integration` WebApplicationFactory + Testcontainers SQL Server harness is suitable for the new integration tests (FR-018-15 through FR-018-19, FR-018-21) without architectural change.
- The existing `Mentoory.Tests.E2E` Playwright harness is suitable for FR-018-18 (E2E half) and FR-018-20 without architectural change.
- Branch protection on `develop` can be configured to require the new `coverage-check` GitHub Actions stage. The spec states the requirement; the branch-protection configuration itself is a manual repository-admin step called out in the implementation PR body.
- The pre-existing `NU1902` MailKit vulnerability warning on the full-solution build is unrelated to this specification and remains a separate technical-debt item.
- **FR-018-17 behaviour pinning**: the "401 or redirect to login" assertion must match the currently-deployed `[Authorize]` policy on `/Administration/*` at implementation time. If that policy's behaviour changes in a later feature, the FR-018-17 test and this spec must be updated in lockstep; the test failure would otherwise be silent about *which* contract moved.

## Dependencies

- Feature 005 (`.specify/memory/access-security-constitution.md`) must still exist with its current Section 1–12 numbering; this specification amends that file additively.
- Feature 016 (`specs/016-registration-access-hardening/`) provides the first complete set of requirement identifiers that this gate protects. The specification identifiers `FR-018-01` through `FR-018-21` and `SC-018-01` through `SC-018-05` in 016 are referenced by this specification's retrofit requirements.
- xUnit 2.x (`TraitAttribute`) — present in all three test projects.
- .NET 10 SDK — provides `System.Reflection.MetadataLoadContext`.
- Existing `Mentoory.Tests.Integration` WebApplicationFactory + Testcontainers SQL Server fixture.
- Existing `Mentoory.Tests.E2E` Playwright fixture.
- GitHub Actions and the repository's branch-protection settings on `develop`.

## Out of Scope

- Retroactive trait application to features 001 through 015. Those features migrate organically as they are next touched; this specification does not force their retrofit.
- Extending the gate to non-access-security areas (Tenant, Diagnostic, Mentoring, Knowledge, Subscription, Example). A later initiative may generalise the pattern; this specification binds only access-security features.
- Any modification to feature-016 production code (`Mentoory.Access.Application/**`, `Mentoory.Web/**` registration and enrollment paths). The gate asserts that the 016 code already satisfies the quality categories; it does not change the 016 code.
- Interactive or visual-inspection tests. Every assertion in this specification is a pass/fail in an automated test runner.
- Replacing existing CI infrastructure. The specification adds one new required stage and keeps every existing stage unchanged.

## Open Questions

- **OQ-001**: Should the `Coverage: N/A` escape hatch require explicit reviewer approval on every new occurrence, or is the justification clause alone sufficient? Resolving during constitution amendment review is acceptable; this does not block implementation.
- **OQ-002**: Where does the coverage tool's project live — `tools/Mentoory.Specs.CoverageCheck/`, `build/Mentoory.Specs.CoverageCheck/`, or a dedicated `specs/tooling/CoverageCheck/`? An implementation-phase decision; all three satisfy the requirements.
- **OQ-003**: Does the 50-probe count in FR-018-19 need to be configurable (e.g., a nightly 500-probe deep run)? Not blocking the current scope; parameterisation can be added later without changing the functional contract.
