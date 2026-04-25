---
description: "Task list for feature 018-access-security-delivery-quality-gate"
---

# Tasks: Access-Security Delivery Quality Gate

**Input**: Design documents from `/specs/018-access-security-delivery-quality-gate/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/{coverage-check-cli,msbuild-integration,github-actions-stage,trait-conventions,spec-front-matter}.md, quickstart.md

**Tests**: INCLUDED — this feature's core deliverable is a coverage-enforcement tool whose correctness depends on its own test suite. Spec SC-001 and SC-005 require self-tests explicitly. FR-013 retrofit is also test-code work. Every new feature-016 scenario (FR-015..021) is a test. Non-test tasks are the tool source, the constitution amendment, CI glue, and MSBuild integration.

**Organization**: Tasks are grouped by user story so each story can be implemented, verified, and retrofit independently. The monolithic PR (brainstorm decision) lands all three stories together, but task sequencing respects priorities and the explicit US3 → US2 dependency.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1, US2, US3). Setup/Foundational/Polish tasks have no story label.
- File paths in every task are absolute-under-repo-root.

## Path Conventions

Modular-monolith layout with a new `tools/` directory:

- New tool: `tools/Mentoory.Specs.CoverageCheck/`
- New tool tests: `tests/Mentoory.Specs.CoverageCheck.Tests/`
- Existing test projects: `tests/Mentoory.Access.Tests/`, `tests/Mentoory.Tests.Integration/`, `tests/Mentoory.Tests.E2E/`
- Constitution amendment: `.specify/memory/access-security-constitution.md`
- CI: `.github/workflows/`
- Solution: `Mentoory.sln`
- Repo-root MSBuild: `Directory.Build.targets`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Scaffold the new tool project, test project, MSBuild hook stub, solution entries, and spec front-matter. No logic yet — every Phase 1 task is mechanical.

- [ ] T001 Create `tools/Mentoory.Specs.CoverageCheck/Mentoory.Specs.CoverageCheck.csproj` — `<OutputType>Exe</OutputType>`, `<TargetFramework>net10.0</TargetFramework>`, PackageReferences for `System.CommandLine` (beta8+), `System.Reflection.MetadataLoadContext`, `Microsoft.Extensions.FileSystemGlobbing`. Nullable enabled. TreatWarningsAsErrors inherited from repo root.
- [ ] T002 [P] Create `tools/Mentoory.Specs.CoverageCheck/Program.cs` with a minimal CLI stub: parse `--specs-root`, `--test-assemblies` (repeatable), `--mode warn`, `--report-format text|json`, `--help`; exit `64` on bad usage; exit `0` with a "not yet implemented" stub body otherwise. Full logic lands in US2.
- [ ] T003 [P] Create `tests/Mentoory.Specs.CoverageCheck.Tests/Mentoory.Specs.CoverageCheck.Tests.csproj` — xUnit + FluentAssertions + `ProjectReference` to the tool project.
- [ ] T004 [P] Create `tools/Mentoory.Specs.CoverageCheck/build/CoverageCheck.targets` with an AfterTargets="Build" target scoped to `$(MSBuildProjectName) == 'Mentoory.Specs.CoverageCheck'`. Body is a no-op echo in Phase 1; real Exec call lands in US2 (T039).
- [ ] T005 [P] Create `Directory.Build.targets` at repo root with `<Import Project="tools/Mentoory.Specs.CoverageCheck/build/CoverageCheck.targets" Condition="Exists('$(MSBuildThisFileDirectory)tools/Mentoory.Specs.CoverageCheck/build/CoverageCheck.targets')" />`. Existing `Directory.Build.props` is untouched.
- [ ] T006 [P] Add both new projects to `Mentoory.sln`: solution folder `tools/` containing `Mentoory.Specs.CoverageCheck`, and solution folder `tests/` gains `Mentoory.Specs.CoverageCheck.Tests`. Use `dotnet sln Mentoory.sln add ...` twice.
- [ ] T007 [P] Prepend YAML front-matter `---\naccess-security: true\n---\n\n` to `specs/016-registration-access-hardening/spec.md` (exactly those three lines + blank line, before the existing `# Feature Specification:` heading). Preserves every other line.
- [ ] T008 [P] Prepend the same YAML front-matter to `specs/018-access-security-delivery-quality-gate/spec.md`.

**Checkpoint**: `dotnet build Mentoory.sln` succeeds with the new projects compiled; the stub tool runs and exits 0 on `--help`; both specs opt into floor enforcement. Nothing in 016 or 018 behavior has changed.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Amend the constitution so the canonical floor-category names are committed to the repo (source of truth per research.md #14) before the tool and retrofits reference them. Add the shared authenticated-admin HTTP helper that two of US1's new integration tests depend on.

**⚠️ CRITICAL**: No US1/US2/US3 work starts until T009–T011 are complete.

- [ ] T009 Amend `.specify/memory/access-security-constitution.md` Section 11 — append six subsections: `11.9 Response Indistinguishability`, `11.10 Outcome Audit Logging`, `11.11 Public-vs-Admin Attribution`, `11.12 Form-State Preservation`, `11.13 Defense-in-Depth Controls`, `11.14 Content-Policy Rules`. Each subsection: 3-sentence **Purpose**, explicit **Trigger condition**, explicit **Minimum automated assertion**, one **Canonical example** citing a specific feature-016 FR identifier (FR-019 / FR-014 / FR-015 / FR-020 / FR-021 / FR-018 respectively). Update the `SYNC IMPACT REPORT` HTML comment at file top (bump version to 1.1.0, list added sections 11.9–11.14).
- [ ] T010 Append a new `## 13. Delivery Quality Gate` to `.specify/memory/access-security-constitution.md` (immediately before the Appendices A/B/C). Sections inside: `13.1 Scope anchor` (access-security features only, opt-in via `access-security: true` spec front-matter), `13.2 Traceability rule` (every FR-###/SC-### MUST have ≥1 claiming `[Trait("Spec"|"Sc", …)]` test or a valid `Coverage: N/A` marker), `13.3 Floor-category rule` (every applicable floor category MUST have ≥1 claiming `[Trait("Floor", …)]` test), `13.4 CI stage sequence` (`build` → `coverage-check` → `unit-tests` → `integration-tests` → `e2e-tests`), `13.5 Exclusion marker format` (exact regex + 20-char justification), `13.6 Flaky-test policy` (24h quarantine via `[Trait("Flaky","true")]`; quarantined test is non-claiming). Update SYNC IMPACT REPORT to list Section 13.
- [ ] T011 Extend `tests/Mentoory.Tests.Integration/Fixtures/IntegrationTestBase.cs` with a new `protected async Task<HttpClient> CreateAuthenticatedAdminClientAsync(string email = "incadmin1@test.mentoory.com", string password = "Test123!@#")` helper. Implementation per research.md #6: fetch `/Access/Login` to obtain antiforgery cookie+token, POST the login form, extract the auth cookie from `Set-Cookie`, attach both to a new `HttpClient` backed by a `CookieContainer`. Used by T012.

**Checkpoint**: Constitution amendment is in place; tests project builds; no new test logic yet. All three stories can now proceed in parallel (US3 still sequences after US2 for tool logic, but US2's tool is touched only by US3's T043 — US3's Floor-trait retrofit (T046) can parallelise with late-US2 trait retrofits).

---

## Phase 3: User Story 1 — Close feature-016 coverage gaps (Priority: P1) 🎯 MVP

**Goal**: Seven new automated scenarios closing every coverage gap identified in the PR #13 analysis: admin duplicate-NID attribution (FR-015), admin fresh redirect (FR-016), admin unauthenticated gate (FR-017), public-side national-ID identifying-data rejection (FR-018 public half), admin-side same (FR-018 admin half), 50-probe response-equality sweep (FR-019), form-state preservation after generic banner (FR-020), defense-in-depth controls intact (FR-021).

**Independent Test**: Run `dotnet test tests/Mentoory.Tests.Integration tests/Mentoory.Tests.E2E` with the new tests present; observe all seven new scenarios pass. Spec feature 016 is unchanged at runtime but its behavioural contract is now asserted at every CI run.

- [ ] T012 [US1] Extend `tests/Mentoory.Tests.Integration/Identity/AdminEnrollmentTests.cs` with three new test methods: `AdminEnrollment_DuplicateNationalId_ReturnsAttributedError` (FR-015 — dispatches `AdminEnrollUserCommand` with a pre-persisted NID, asserts `Result.IsFailure` and `ErrorMessages` contains `("NationalId", "Ya existe una cuenta con este número de identificación.")`); `AdminEnrollment_ValidData_RedirectsToUsersList` (FR-016 — uses T011 helper to POST `/Administration/Users/Enroll` with fresh data, asserts `302` to `/Administration/Users` and response's `Set-Cookie` does not clear the success TempData key before first read); `AdminEnrollment_Unauthenticated_RedirectsToLoginOrReturns401` (FR-017 — anonymous HttpClient POSTs, asserts either `401` or `302 Location: /Access/Login` — pinned to whichever the current `[Authorize]` policy produces).
- [ ] T013 [P] [US1] Create `tests/Mentoory.Tests.Integration/Identity/PublicRegistrationSweepTests.cs` (FR-019). Define `const int ProbeCount = 50;` per research.md #13. Use `WebApplicationFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false })`. For each of 50 probes (mixed fresh + duplicate-email + duplicate-NID), POST to `/Access/Register` with a valid antiforgery token. After receiving the `302`, follow the redirect with a GET and capture the response body. Assert pairwise equality across all 50 responses for: HTTP status, `Location` header, stripped body bytes, `Cache-Control`, `Content-Type`, set of `Set-Cookie` cookie *names*. The assertion helper `AntiforgeryStripper` (research.md #7) lives in `tests/Mentoory.Tests.Integration/Fixtures/AntiforgeryStripper.cs` and is created by this task. Timing assertion: `stopwatch.ElapsedMilliseconds.Should().BeLessThan(15000)` per NFR-004.
- [ ] T014 [P] [US1] Create `tests/Mentoory.Tests.Integration/Identity/DefenseInDepthTests.cs` (FR-021). Three tests: `PublicRegistration_AntiforgeryMissing_Returns400` (POST with no `__RequestVerificationToken`), `AdminEnrollment_AntiforgeryMissing_Returns400` (authenticated admin POST without token), `PublicRegistration_RateLimitExceeded_Returns429`. The rate-limit test uses a new `DefenseInDepthTestsFactory : MentooryWebApplicationFactory` subclass (inside the same file) that overrides `ConfigureWebHost` to add `builder.ConfigureAppConfiguration(cb => cb.AddInMemoryCollection(new[] { KeyValuePair.Create<string,string?>("RateLimiting:Registration:PermitLimit", "3"), KeyValuePair.Create<string,string?>("RateLimiting:Registration:WindowSeconds", "10") }))`. Fires 4 POSTs in quick succession; asserts fourth receives `429`.
- [ ] T015 [P] [US1] Create `tests/Mentoory.Tests.Integration/Identity/PasswordIdentifyingDataAdminIntegrationTests.cs` (FR-018 admin-integration half). Two tests: `AdminEnrollment_PasswordContainsNationalId_Verbatim_ReturnsPasswordAttributedError` and `AdminEnrollment_PasswordContainsNationalId_Stripped_ReturnsPasswordAttributedError`. Each dispatches `AdminEnrollUserCommand` whose `Password` contains the NID (verbatim or with separators removed). Asserts `Result.IsFailure` with `ErrorMessages` containing `("Password", PasswordIdentifyingDataRule.Message)`.
- [ ] T016 [P] [US1] Extend `tests/Mentoory.Tests.E2E/Tests/RegistrationTests.cs` with two new tests. `Register_PasswordContainsNationalId_ShowsGenericBanner` (FR-018 public E2E — uses Playwright to submit via browser, asserts banner text `"No fue posible completar el registro. Revise los datos e intente nuevamente."` is rendered and no per-field error span appears). `Registration_GenericBannerRender_PreservesNonSecretFields` (FR-020 — submit a form that triggers the server-side banner, then assert input values for Email, FirstName, LastName, Country, NationalId are populated in the re-rendered form, while Password and ConfirmPassword inputs are empty).
- [ ] T017 [P] [US1] Extend `tests/Mentoory.Tests.E2E/Tests/AdministrationUsersTests.cs` with `AdminEnroll_PasswordContainsIdentifyingData_ShowsAttributedError` (FR-018 admin E2E). Uses the existing authenticated-admin Playwright fixture; submits Enroll form with a password containing the candidate's email local part; asserts the `Password` field shows the shared `PasswordIdentifyingDataRule.Message` via the `data-valmsg-for="Password"` span.
- [ ] T018 [US1] Run `dotnet test tests/Mentoory.Tests.Integration/Mentoory.Tests.Integration.csproj tests/Mentoory.Tests.E2E/Mentoory.Tests.E2E.csproj --configuration Release --logger "console;verbosity=minimal"` and confirm every T012–T017 test passes. Paste the passing output into the PR body for reviewer reference (not committed — ephemeral verification).

**Checkpoint**: US1 alone closes the PR #13 coverage gap. If US2/US3 slip, US1 can still ship to `develop` by itself; the new tests are additive and do not depend on the tool. This is the MVP-independent property.

---

## Phase 4: User Story 2 — Establish the reusable quality gate (Priority: P1)

**Goal**: Build the coverage tool (parser + reflector + analyzer + writer + CLI), retrofit `[Trait("Spec", …)]` / `[Trait("Sc", …)]` claims onto every feature-016 test, wire MSBuild integration, add the `coverage-check` GitHub Actions stage. After US2, drift between spec IDs and tests will fail the build mechanically.

**Independent Test**: `dotnet build Mentoory.sln --configuration Release` runs the coverage tool as part of the build, tool reports `RESULT: PASSED (exit code 0)` with 0 unclaimed IDs and 0 dangling traits for feature 016. Deliberately deleting one `[Trait]` makes the next build fail with a named identifier in the `== Unclaimed Identifiers ==` section (SC-005 canary). Tool unit tests (`Mentoory.Specs.CoverageCheck.Tests`) pass.

### Coverage tool source (lives under `tools/Mentoory.Specs.CoverageCheck/`)

- [ ] T019 [P] [US2] Create `tools/Mentoory.Specs.CoverageCheck/Parsing/SpecModels.cs` — records `FeatureSpec`, `RequirementId`, `ExclusionMarker` per data-model.md; `RequirementId` has structural equality on `(Kind, Value)` only.
- [ ] T020 [P] [US2] Create `tools/Mentoory.Specs.CoverageCheck/Parsing/SpecParser.cs` — public `IReadOnlyList<FeatureSpec> Parse(string specsRoot)`. Implements: directory walk for `spec.md`, front-matter extraction (YAML subset per research.md #9), FR/SC regex `\*\*(FR|SC)-\d{3}\*\*`, exclusion regex `\*Coverage:\s*N/A\s*[—-]{1,2}\s*(.{20,}?)\.\s*\*`, malformed-exclusion reporting, duplicate-ID-within-spec parse error. Returns `(specs, parseErrors)`.
- [ ] T021 [P] [US2] Create `tools/Mentoory.Specs.CoverageCheck/Reflection/TestClaimModels.cs` — records `TestAssembly`, `TestMethodMetadata`, `TestClaim`, enum `TraitKind { Spec, Sc, Floor, Flaky, Other }`.
- [ ] T022 [P] [US2] Create `tools/Mentoory.Specs.CoverageCheck/Reflection/TraitReflector.cs` — public `IReadOnlyList<TestAssembly> Load(IEnumerable<string> assemblyPaths)`. Implements `MetadataLoadContext` with a `PathAssemblyResolver` configured for the runtime's reference assemblies + assembly-adjacent xUnit DLLs per research.md #3. Enumerates methods with `[Fact]` / `[Theory]` / descendants; reads `[Trait]` via `CustomAttributeData.ConstructorArguments`. Detects `Skip=...` on `[Fact]`/`[Theory]`.
- [ ] T023 [US2] Create `tools/Mentoory.Specs.CoverageCheck/Coverage/FloorCategories.cs` — canonical list of six `FloorCategory` constants matching the names in constitution Section 11.9–11.14 (kebab-case strings): `response-indistinguishability`, `outcome-audit-logging`, `public-vs-admin-attribution`, `form-state-preservation`, `defense-in-depth-controls`, `content-policy-rules`. `IReadOnlyDictionary<string, FloorCategory> Canonical`. Comment at top cross-references the constitution file + section.
- [ ] T024 [US2] Create `tools/Mentoory.Specs.CoverageCheck/Coverage/CoverageReport.cs` — record with `UnclaimedIds`, `DanglingTraits`, `Exclusions`, `MissingFloorCategories`, `DuplicateIds`, `MalformedExclusions`, `ReflectionErrors`. Depends on T019–T023.
- [ ] T025 [US2] Create `tools/Mentoory.Specs.CoverageCheck/Coverage/CoverageAnalyzer.cs` — public `CoverageReport Analyze(IEnumerable<FeatureSpec>, IEnumerable<TestAssembly>)`. Builds the six report sections per data-model.md semantics: quarantined-as-non-claiming logic (NFR-002), skip-as-non-claiming logic (EC-001), multi-trait credit per EC-002. Depends on T019–T024.
- [ ] T026 [P] [US2] Create `tools/Mentoory.Specs.CoverageCheck/Output/TextReportWriter.cs` — deterministic text output matching contracts/coverage-check-cli.md exactly, including the final `RESULT: PASSED/FAILED (exit code N)` line and omission of empty sections.
- [ ] T027 [P] [US2] Create `tools/Mentoory.Specs.CoverageCheck/Output/JsonReportWriter.cs` — deterministic JSON output matching the schema in contracts/coverage-check-cli.md, sorted keys + sorted collections per the determinism requirement (SC-001 golden-file compatibility).
- [ ] T028 [US2] Fill in `tools/Mentoory.Specs.CoverageCheck/Program.cs` (replace T002 stub). Wire `System.CommandLine` root command → call `SpecParser.Parse` → call `TraitReflector.Load` → call `CoverageAnalyzer.Analyze` → call the chosen writer → map to exit code per contracts/coverage-check-cli.md (0 success, 1 coverage violation, 2 parse, 3 reflection, 64 usage). Honour `--mode warn` (downgrade 1/2/3 → 0; 64 unchanged) and `MENTOORY_COVERAGECHECK_MODE` env var.

### Coverage tool self-tests (`tests/Mentoory.Specs.CoverageCheck.Tests/`)

- [ ] T029 [P] [US2] Create fixture specs in `tests/Mentoory.Specs.CoverageCheck.Tests/Fixtures/specs/`: `sample-clean.md`, `sample-unclaimed.md`, `sample-excluded.md`, `sample-malformed-frontmatter.md`, `sample-duplicate-id.md`, `sample-dangling.md`. Each file shows one edge case described in data-model.md / research.md #10.
- [ ] T030 [P] [US2] Create synthetic xUnit test fixture project `tests/Mentoory.Specs.CoverageCheck.Tests/Fixtures/assemblies/SampleXunitTests/SampleXunitTests.csproj` (xUnit package + `net10.0`). Inside, hand-write `SampleTests.cs` carrying traits: one good claim, one dangling claim, one skipped claim with trait, one multi-trait method, one `[Trait("Flaky","true")]` method. Fixture-project builds at test time via `<BeforeTargets>Build</BeforeTargets>` in `Mentoory.Specs.CoverageCheck.Tests.csproj`.
- [ ] T031 [P] [US2] Create `tests/Mentoory.Specs.CoverageCheck.Tests/Parsing/SpecParserTests.cs` — ≥8 test methods covering: valid spec, exclusion marker accepted, exclusion justification too short, malformed front-matter, duplicate ID within spec, front-matter absent, non-access-security front-matter, arbitrary body ignored.
- [ ] T032 [P] [US2] Create `tests/Mentoory.Specs.CoverageCheck.Tests/Reflection/TraitReflectorTests.cs` — ≥5 methods covering: trait enumeration, Skip detection, bad `Spec` value regex rejection, unknown trait key ignored, assembly load failure (corrupt DLL fixture).
- [ ] T033 [US2] Create `tests/Mentoory.Specs.CoverageCheck.Tests/Coverage/CoverageAnalyzerTests.cs` — ≥10 methods covering every `CoverageReport` field at least once, plus the SC-005 canary (remove trait → Unclaimed), quarantined-flaky-as-non-claiming (NFR-002), multi-ID claim accounting, cross-spec duplicate-ID detection. One golden-file test pins `TextReportWriter` output against a saved `expected-output.txt` fixture; another does the same for `JsonReportWriter`.

### Retrofit `[Trait]` attributes on every feature-016 test

- [ ] T034 [US2] Retrofit `tests/Mentoory.Access.Tests/Handlers/RegisterUserHandlerTests.cs` — add `[Trait("Spec", "FR-014")]` to outcome-mapping tests, `[Trait("Spec", "FR-014"), Trait("Sc", "SC-005")]` to the log-capture test, etc. Full mapping derived from 016's FR list is the implementer's responsibility; rule is every 016 FR and SC has ≥1 claiming method somewhere across the three test projects.
- [ ] T035 [P] [US2] Retrofit `tests/Mentoory.Access.Tests/Handlers/AdminEnrollUserHandlerTests.cs` — `[Trait("Spec", "FR-022")]` (the AdminEnrollUserCommand/Handler outcome mapping in 016's spec — implementer confirms actual FR number against 016's spec), etc.
- [ ] T036 [P] [US2] Retrofit `tests/Mentoory.Access.Tests/Services/UserProvisioningServiceTests.cs` — at minimum `[Trait("Spec", "FR-011")]` (provisioning-service extraction) per 016's FR list.
- [ ] T037 [P] [US2] Retrofit `tests/Mentoory.Access.Tests/Validators/RegisterUserValidatorTests.cs`, `AdminEnrollUserValidatorTests.cs`, `PasswordIdentifyingDataRuleTests.cs`. Password-identifying-data tests claim the 016 FR covering the rule introduction (implementer confirms against 016 spec).
- [ ] T038 [P] [US2] Retrofit integration tests in `tests/Mentoory.Tests.Integration/Identity/`: existing `RegistrationTests.cs` gets traits for 016 FRs; `AdminEnrollmentTests.cs` (now extended by T012) gets traits covering FR-015/FR-016/FR-017; T013–T015's new files get their traits added inline.
- [ ] T039 [P] [US2] Retrofit E2E tests in `tests/Mentoory.Tests.E2E/Tests/RegistrationTests.cs` and `AdministrationUsersTests.cs` — including T016/T017 additions. Every existing 016-relevant test gains ≥1 `[Trait]` claim.

### MSBuild + CI wiring

- [ ] T040 [US2] Replace `tools/Mentoory.Specs.CoverageCheck/build/CoverageCheck.targets`'s Phase-1 stub with the real target per contracts/msbuild-integration.md: `AfterTargets="Build"` on the tool project only, `<Exec>` invoking `dotnet $(TargetPath)` with args pointing at `$(MSBuildThisFileDirectory)../../../specs/` and `$(MSBuildThisFileDirectory)../../../tests/**/bin/$(Configuration)/net10.0/Mentoory.*.Tests.dll`. Honour `$(CoverageCheckMode)` and `$(SkipCoverageCheck)` properties. Pipes `ConsoleToMSBuild=true`.
- [ ] T041 [US2] Create OR modify `.github/workflows/<name>.yml` (implementer inspects repo first to choose filename) to add the `coverage-check` job per contracts/github-actions-stage.md. Job `needs: build`; publishes the tool; runs it against `specs/` + the test-assembly glob; fails the workflow on non-zero exit. Add `needs: coverage-check` to the existing unit-tests, integration-tests, and e2e-tests jobs (or to new jobs if those don't yet exist).
- [ ] T042 [US2] Verify US2 locally: `dotnet build Mentoory.sln --configuration Release` succeeds and logs `RESULT: PASSED (exit code 0)` from the CoverageCheck target. Temporarily delete the `[Trait("Spec", "FR-013")]` attribute from one retrofitted test, `dotnet build` again, observe `RESULT: FAILED (exit code 1)` with `FR-013` named in the Unclaimed Identifiers section. Restore the trait. This is the SC-005 canary executed manually; its automated equivalent is in T033.

**Checkpoint**: The gate is live. US1 tests still pass; 016 is fully claim-covered; the CI workflow now includes a required status check blocking merges on drift. US3 can begin (floor enforcement rides on the same tool).

---

## Phase 5: User Story 3 — Enforce the floor-category minimum (Priority: P2)

**Goal**: Extend the coverage analyzer so opted-in specs (`access-security: true`) must have at least one test claiming each of the six canonical floor categories. Retrofit feature-016 tests with `[Trait("Floor", …)]` attributes so 016 passes the floor gate on merge.

**Independent Test**: Run the coverage tool against 016 (via `dotnet build` or directly): `RESULT: PASSED` with all six floor categories reported as covered (the reporter need not list satisfied categories, but the `== Missing Floor Categories ==` section must be absent). Then remove one `[Trait("Floor", …)]` attribute — say, from the FR-019 sweep test — and re-run: `== Missing Floor Categories ==` appears naming `response-indistinguishability` and the feature path, exit code 1.

- [ ] T043 [US3] Extend `tools/Mentoory.Specs.CoverageCheck/Coverage/CoverageAnalyzer.cs` with floor enforcement: for each `FeatureSpec` where `AccessSecurityOptIn == true`, check that every category in `FloorCategories.Canonical` has ≥1 `TestClaim` with `Kind == Floor` anywhere in the loaded assemblies. Report deficits in `CoverageReport.MissingFloorCategories` with `(Spec, Category)` tuples. Uncovered categories contribute to exit code 1 per contracts/coverage-check-cli.md.
- [ ] T044 [P] [US3] Extend `tests/Mentoory.Specs.CoverageCheck.Tests/Coverage/CoverageAnalyzerTests.cs` with floor tests: `OptedInSpec_MissingFloor_ReportedAsViolation`, `OptedInSpec_AllFloorsClaimed_Passes`, `NonOptedInSpec_NoFloorTraits_Passes`, `FloorTraitOnSkippedTest_DoesNotCount` (EC-001 applies to Floor traits too). Add fixture `Fixtures/specs/sample-access-security-missing-floor.md` referenced by the first test.
- [ ] T045 [US3] Extend `tools/Mentoory.Specs.CoverageCheck/Output/TextReportWriter.cs` AND `Output/JsonReportWriter.cs` to emit the `== Missing Floor Categories ==` section (text) and `missingFloorCategories` array (JSON). Update the golden-file fixtures in T033 to include a floor-violation scenario.
- [ ] T046 [US3] Retrofit `[Trait("Floor", …)]` on feature-016 tests, mapping per research.md + 016 spec:
    - `response-indistinguishability` → the 50-probe sweep test in T013's `PublicRegistrationSweepTests.cs`.
    - `outcome-audit-logging` → the log-capture test in `RegisterUserHandlerTests` (already trait-claimed for FR-014 / SC-005 by T034).
    - `public-vs-admin-attribution` → the duplicate-email/NID tests in `AdminEnrollmentTests.cs` (T012 extension) and the masked-to-Success test in `RegisterUserHandlerTests`.
    - `form-state-preservation` → the form-state test in T016.
    - `defense-in-depth-controls` → the antiforgery + rate-limit tests in T014's `DefenseInDepthTests.cs`.
    - `content-policy-rules` → `PasswordIdentifyingDataRuleTests` (already present, T037 retrofits it for FR; add Floor trait here).
- [ ] T047 [US3] Verify US3 locally: `dotnet build Mentoory.sln --configuration Release` — tool reports zero `Missing Floor Categories`. Remove the `[Trait("Floor", "response-indistinguishability")]` from the sweep test, rebuild, observe the violation report and non-zero exit. Restore.

**Checkpoint**: All three stories are functional. Feature 016 is fully claim-covered (FR/SC) AND fully floor-covered. Drift in either dimension fails CI.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: End-to-end walkthrough, timing-budget verification, CLAUDE.md refresh, branch hygiene. No new business logic.

- [ ] T048 [P] Update `CLAUDE.md` Recent Changes block with a one-line entry for feature 018 summarising the behavioural change ("Access-Security Delivery Quality Gate: coverage tool + constitution amendment + CI stage; feature 016 is the first instantiation"). The `update-agent-context.sh` script may have already added a generic tech-list entry during `/speckit-plan`; replace or augment it with the behavioural summary.
- [ ] T049 [P] Verify NFR-001 runtime budget: in CI, the `coverage-check` job prints `Elapsed: <s>` in the tool output. Add a grep assertion in the job step that fails if the reported elapsed is > 2000 ms. (Bash: `grep -E 'Elapsed: [0-2]\.' out.log || exit 1`.)
- [ ] T050 [P] Verify NFR-004 runtime budget: T013's `PublicRegistrationSweepTests` already contains the 15-second wall-time assertion. No additional task — this item is a confirmation that the assertion exists and runs in CI.
- [ ] T051 [P] Verify Spanish-copy constitution compliance (Principle IX): the only user-facing strings this feature touches are those already delivered by feature 016 (referenced from the constitution amendment examples). No new Spanish strings introduced; no English leakage into user-facing copy.
- [ ] T052 Run the full `specs/018-access-security-delivery-quality-gate/quickstart.md` walkthrough (Steps 1–9) from a clean branch checkout. Every step exits green. Fix any drift between the plan and actual behaviour and re-run.
- [ ] T053 Commit all changes. Commit message structure: one commit covering constitution amendment; one covering tool source; one covering retrofit + new scenarios; one covering CI + MSBuild wiring. Each commit message references the FR/SC identifiers it satisfies. Co-Authored-By trailer included per repo convention.
- [ ] T054 `git push -u origin 018-access-security-delivery-quality-gate`. Open PR against `develop` using the `review_brief.md` as the summary source. PR body includes: admin action item (DEP-005 — configure `coverage-check` as a required status check on `develop`); the three OQ items from the spec; link to `specs/018-access-security-delivery-quality-gate/quickstart.md` for reviewers to run locally.

---

## Dependencies & Execution Order

### Phase dependencies

- **Setup (Phase 1, T001–T008)**: no dependencies; T001 and T002 are sequential (T002 edits the file T001 created); T003–T008 are `[P]` and run together.
- **Foundational (Phase 2, T009–T011)**: depends on Setup. T009 and T010 edit the same file → sequential. T011 is independent (`[P]` with T010 if the implementer wants).
- **User Stories (Phase 3, 4, 5)**: all depend on Phase 2 checkpoint. US1 is genuinely independent of US2 (no tool dependency inside the seven scenarios). US2 is independent of US1 work-wise but the retrofit step (T038–T039) touches the files US1 extends, so T038/T039 wait for US1's T012–T017 to merge their edits into those files first. US3 depends on US2 (T043 extends the tool that T025 creates).
- **Polish (Phase 6)**: depends on US1 + US2 + US3 complete.

### User story dependencies

- **US1 (P1) MVP**: blocked only by Phase 2. Independent of US2 and US3. Can ship alone if US2/US3 slip.
- **US2 (P1)**: blocked by Phase 2. Logically independent of US1, but its trait-retrofit tasks (T038, T039) edit US1's new files — so US1 test files must be in place before the retrofit tasks touch them. Practical ordering: US1 tasks (T012–T017) → US2 tool+retrofit.
- **US3 (P2)**: blocked by US2's tool source (T019–T025). Its retrofit (T046) also touches 016's test files, parallelisable with US2's FR retrofit on those same files (T034–T039) only if the implementer coordinates line-level edits.

### Within each user story

- US1: T012 is sequential (3 new tests in one existing file); T013–T015 are parallel (three new files); T016+T017 are parallel (two existing E2E files); T018 is the verification step, serial after all others.
- US2: T019–T022 are parallel (four separate new files); T023–T028 are sequential (shared Program.cs + analyser); T029–T033 can parallelise once the fixture is in place; T034–T039 parallelise across test files; T040–T041 are sequential after tool is built; T042 is verification, serial.
- US3: T043 is sequential (shared analyser); T044–T045 can parallelise; T046 is a retrofit sweep; T047 is verification.

### Parallel opportunities

- All of T002–T008 (Setup).
- T010 + T011 (Foundational — different files).
- T013 + T014 + T015 + T016 + T017 in US1.
- T019 + T020 + T021 + T022 + T026 + T027 in US2 (each is a different new file).
- T029 + T030 (US2 fixtures — independent).
- T031 + T032 (US2 unit tests).
- T035 + T036 + T037 + T038 + T039 in US2 (retrofit across different test files).
- T044 + T045 in US3 (different files).
- T048 + T049 + T050 + T051 in Polish.

---

## Parallel Example: Phase 4 Coverage Tool Core

```text
# After T019 (SpecModels), T021 (TestClaimModels), and T024 (CoverageReport record) exist,
# the three pure-logic components can be implemented in parallel:
- Launch T020 (SpecParser)
- Launch T022 (TraitReflector)
- Launch T023 (FloorCategories)
- Launch T026 (TextReportWriter)
- Launch T027 (JsonReportWriter)

# Once T025 (CoverageAnalyzer) lands:
- Launch T028 (Program.cs wiring)
- Launch T031 (SpecParserTests)
- Launch T032 (TraitReflectorTests)
- T033 (CoverageAnalyzerTests) serial after T025.
```

## Parallel Example: User Story 1 scenarios

```text
# After T011 (auth-admin helper) lands, US1's integration + E2E tests can run in parallel:
- Launch T012 (AdminEnrollmentTests extension)        # file edit
- Launch T013 (PublicRegistrationSweepTests new)       # new file
- Launch T014 (DefenseInDepthTests new)                # new file
- Launch T015 (PasswordIdentifyingDataAdminIntegrationTests new)  # new file
- Launch T016 (RegistrationTests E2E extension)        # file edit
- Launch T017 (AdministrationUsersTests E2E extension) # file edit
# Then T018 verification serial at the end.
```

---

## MVP scope

**Suggested MVP**: Phase 1 → Phase 2 → **Phase 3 (US1)** → Phase 6 (polish subset: T048, T052, T053, T054).

US1 alone closes the PR #13 coverage gap. If US2 or US3 slip, the seven new test scenarios still ship to `develop` and protect the delivered behaviour of feature 016. US2+US3 add the durable, CI-enforced drift protection; they are high value but not blocking for the immediate security concern.

However, per the brainstorm's monolithic-PR decision, the planned rollout is all six phases in one PR. The MVP-scope note here is purely for risk-management: if implementation hits a blocker on the tool side, US1 can be extracted and shipped independently without unpicking the rest.

---

## Format validation

All tasks follow the strict checklist format:

- ✅ Checkbox `- [ ]` at line start
- ✅ Sequential ID `T001`–`T054`
- ✅ `[P]` marker only where genuine file-level independence exists
- ✅ `[US1]` / `[US2]` / `[US3]` story labels on all Phase 3/4/5 tasks
- ✅ No story label on Setup (Phase 1), Foundational (Phase 2), or Polish (Phase 6) tasks
- ✅ Exact file paths on every implementation / test task (csprojs, Program.cs, integration test files, retrofit targets identified absolutely)

## Task-count summary

| Phase | Count | Notes |
|-------|-------|-------|
| 1 — Setup | 8 | 7 [P], T001 serial |
| 2 — Foundational | 3 | T009 + T010 sequential (same file); T011 independent |
| 3 — US1 (P1 MVP) | 7 | 6 impl + 1 verification |
| 4 — US2 (P1) | 24 | Tool source (10) + self-tests (5) + retrofit (6) + MSBuild/CI (2) + verification (1) |
| 5 — US3 (P2) | 5 | Tool extension (1) + tests (2) + retrofit (1) + verification (1) |
| 6 — Polish | 7 | 4 [P] confirmations + 3 sequential (quickstart, commit, push+PR) |
| **Total** | **54** | |

**Parallel opportunities**: 26 tasks tagged `[P]`, clustered in Setup (7), US2 tool core (6) + retrofit (5), US3 (2), and Polish (4).

**Independent-test criteria**:

- US1: the seven scenarios pass `dotnet test` (T018).
- US2: `dotnet build` runs the tool, reports zero violations for 016; deliberate trait removal produces a named-identifier failure (T042).
- US3: `dotnet build` runs the tool, reports zero missing floor categories for 016; deliberate Floor-trait removal produces a named-category failure (T047).
