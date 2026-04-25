# Phase 0 Research: Access-Security Delivery Quality Gate

**Feature**: `018-access-security-delivery-quality-gate`
**Date**: 2026-04-19
**Purpose**: Resolve every technical decision required before Phase 1 design. Each entry follows the decision / rationale / alternatives format.

---

## 1. Coverage-tool project location (resolves OQ-002)

**Decision**: `tools/Mentoory.Specs.CoverageCheck/`.

**Rationale**:
- Repository root already carries a domain-oriented structure (`Mentoory.Access.*`, `Mentoory.Tenant.*`, `Mentoory.Db`, `Mentoory.Web`, `tests/`, `specs/`, `brainstorm/`). There is no `build/` or `tools/` directory today, so any choice creates a precedent. `tools/` is the conventional .NET name for build-adjacent developer tooling (analysers, codegen, lint, coverage) and signals "not part of the runtime product." `build/` is commonly reserved for MSBuild includes and CI glue; placing a .NET project there confuses readers. `specs/tooling/` sits inside an artefact directory — mixing living source with generated specs is poor layering.
- The tool is a first-party governance binary with no customer-facing output; it belongs next to other developer tooling, not inside `specs/`.

**Alternatives considered**:
- `build/` — rejected; overloads the MSBuild-artifact convention with a full C# project.
- `specs/tooling/` — rejected; couples tool evolution to the specs directory's lifecycle.
- Top-level `Mentoory.Specs.CoverageCheck/` (sibling to existing domain projects) — rejected; promotes the tool to apparent product parity with the access/tenant/etc. modules, which it is not.

**Consequences**:
- `Mentoory.sln` gains a new solution folder entry `tools/` containing the console and its test project.
- The repo-root `Directory.Build.targets` (added in this work) imports the tool's MSBuild targets at solution scope.

---

## 2. CLI parsing library

**Decision**: `System.CommandLine` (beta8+ for .NET 10). Use the builder API, not the `Command`/`Option` reflection style.

**Rationale**:
- Officially supported by .NET; ships with .NET 10 aligned versions; no extra dependency risk versus third-party tools like McMaster.Extensions.CommandLineUtils (latter is archived as of 2024).
- Handles `--specs-root`, `--test-assemblies`, `--mode warn` cleanly; produces auto-generated `--help` that CI logs can surface.
- Supports sub-commands if the tool grows (e.g., `check`, `report`, `emit-html`); we don't need subcommands today but keeping the door open is cheap.

**Alternatives considered**:
- Manual `args[]` parsing — rejected; tool is small but not single-flag. Getting `--mode warn` precedence wrong when combined with env-var overrides is the kind of subtle bug we don't need.
- `McMaster.Extensions.CommandLineUtils` — rejected; archived upstream.
- `CommandLineParser` — rejected; large surface, attribute-driven, not a fit for a 4-option CLI.

**Consequences**:
- `Mentoory.Specs.CoverageCheck.csproj` references `System.CommandLine` beta version pinned in `Directory.Packages.props` (centrally-managed).

---

## 3. Assembly inspection: `MetadataLoadContext` vs. runtime load

**Decision**: `System.Reflection.MetadataLoadContext` over a `PathAssemblyResolver` configured with the .NET 10 BCL + xUnit assembly directories.

**Rationale**:
- No test code executes during the coverage check (we only read attribute metadata). `MetadataLoadContext` is designed exactly for this purpose: it resolves types but never runs any static constructors or loads native dependencies.
- The test assemblies reference Testcontainers, Playwright, and SqlClient — loading them at runtime into the coverage-tool process would either pull in those deps or fail. Metadata-only loading sidesteps both issues.
- Stable API since .NET 5; battle-tested in Roslyn analyzers and source generators.

**Alternatives considered**:
- Runtime `Assembly.LoadFrom` in an isolated `AssemblyLoadContext` — rejected; more code, slower startup, risk of transitive-dependency resolution failures causing false negatives.
- Roslyn source parsing (read `.cs` files directly and extract `[Trait(...)]`) — rejected; duplicates the compiler's job, fails on generated code and partial classes, adds a Roslyn dependency.
- `Mono.Cecil` — rejected; external dependency; metadata-only inspection is already a first-class .NET capability.

**Consequences**:
- Tool injects `PathAssemblyResolver` with the runtime's reference-assembly directory (`Environment.GetEnvironmentVariable("DOTNET_ROOT")` + `shared/Microsoft.NETCore.App/10.0.*/`) plus the test assemblies' own directories for xUnit.
- If a test assembly fails to load (e.g., corrupted), tool exits with code `3` (FR-005 compliance; mapped in `Error Handling` below).

---

## 4. MSBuild `.targets` integration point

**Decision**: Ship `CoverageCheck.targets` inside the tool project at `tools/Mentoory.Specs.CoverageCheck/build/CoverageCheck.targets`; import it from `Directory.Build.targets` at the repo root so every `dotnet build` from any folder below root includes the check.

**Rationale**:
- `Directory.Build.targets` is evaluated automatically by MSBuild for every project in the subtree; this is the idiomatic extension point for repo-wide behaviour.
- Placing the `.targets` inside the tool project (rather than a loose `build/` folder at root) keeps the tool + its integration co-located and versioned together.
- Target fires **after** the `Build` target of the solution's meta-project (TBD: either `Mentoory.Specs.CoverageCheck.Host.csproj` or — simpler — a direct `AfterTargets="Build"` hook on the tool project itself that runs the compiled tool binary). Decision: direct `AfterTargets="Build"` hook, so the hook runs only when the tool itself has compiled successfully.
- Hook uses `<Exec>` calling the published single-file binary when available, else `dotnet run` during local iteration. For CI, the workflow step publishes first and sets an env var so the target picks up the published binary.

**Alternatives considered**:
- Pre-compiled global tool (`dotnet tool`) — rejected; requires explicit `dotnet tool install` per environment; fragile for CI runners.
- Standalone script at `build/coverage-check.sh` — rejected; bash-only, doesn't integrate with `dotnet build` discovery.
- `AfterTargets="AfterBuild"` on every csproj — rejected; fires N times, wasteful.

**Consequences**:
- New `Directory.Build.targets` at repo root (`<Import Project="tools/Mentoory.Specs.CoverageCheck/build/CoverageCheck.targets" Condition="Exists(...)" />`).
- Local developers running `dotnet build Mentoory.Access.Application/...` on their personal machine will see the coverage check fire. NFR-001's ≤ 2 s budget makes this tolerable; `--mode warn` is available for local iteration via an env var the target honours.

---

## 5. CI integration shape

**Decision**: Extend the **existing** GitHub Actions workflow with a new job `coverage-check` that runs after `build` and before `integration-tests`. Job uses `actions/setup-dotnet@v4`, publishes the coverage tool, then runs `dotnet <tool-path> --specs-root specs/ --test-assemblies 'tests/**/bin/Release/net10.0/Mentoory.*.Tests.dll'`. Branch-protection rule (repository admin configures) requires the new job status.

**Rationale**:
- Single workflow is simpler to maintain and lets the new job share the `setup-dotnet`/cache steps of the existing build job via `needs:`.
- New job is required for merge via branch protection; that config is a manual repo-admin step (DEP-005) and called out in the implementation PR body.
- Failure mode: `coverage-check` failure aborts the workflow before the slower `integration-tests` and `e2e-tests` jobs run (FR-012), saving CI minutes and making the drift signal visible fast.

**Alternatives considered**:
- Separate workflow file `coverage-check.yml` — rejected; loses job dependency chaining with the rest of the pipeline, requires duplicate setup, marginally more config to maintain.
- Run coverage check inline inside the `build` job — rejected; muddies the signal (build-vs-gate failures hard to distinguish in status checks).

**Consequences**:
- One job added to `.github/workflows/ci.yml` (or whatever the existing primary workflow is — research task for implementer: inspect the workflow layout first; spec does not dictate filename).
- Branch-protection action item in PR body: enable `coverage-check` required status check on `develop`.

---

## 6. Authenticated `HttpClient` for admin integration tests (FR-016, FR-017)

**Decision**: Add a test-only helper on the existing `IntegrationTestBase` fixture that performs a cookie-auth login against `/Access/Login` using a pre-seeded admin account, extracts the auth cookie from the response, and attaches it to a `HttpClient` returned to the caller. No new auth infrastructure; reuses the existing login endpoint end-to-end.

**Rationale**:
- Existing admin seed account (`incadmin1@test.mentoory.com` / `Test123!@#`) is already created by the test fixture's setup path (confirmed by existing E2E tests in `AdministrationUsersTests.cs`).
- Using the real login endpoint exercises a realistic path: antiforgery cookie + token round-trip, form-based login, session cookie issuance. Shortcutting via claims-injection middleware would leave the login flow untested by these tests and could mask regressions.
- Helper lives once on `IntegrationTestBase`, reused by every admin-path integration test.

**Alternatives considered**:
- `TestServer` with a custom `AuthenticationHandler` injecting claims — rejected; diverges from production auth path, creates a parallel auth surface to maintain.
- `WebApplicationFactory.WithWebHostBuilder` replacing the auth scheme with a stub — rejected; same concern.
- Hand-crafted session cookie — rejected; couples tests to session-cookie internals.

**Consequences**:
- `IntegrationTestBase.CreateAuthenticatedAdminClientAsync()` helper added. Returns an `HttpClient` wired with `HttpClientHandler { UseCookies = true, CookieContainer = … }` pre-populated with the auth cookie.
- Each new admin test that needs authenticated access calls this helper.
- First invocation per test class pays ~100 ms for the login round-trip; subsequent calls in the same class can cache the cookie container (implementation detail, not specified here).

---

## 7. Antiforgery-token stripping for byte-equality (FR-019)

**Decision**: Strip via two deterministic passes on the rendered HTML:
1. Remove `<input name="__RequestVerificationToken" ... value="..." />` entirely.
2. Remove the `__RequestVerificationToken=<...>;` segment of any `Set-Cookie` header value retained in the comparison.

No DOM parsing — the antiforgery token format is stable and unambiguous; a regex over the response body is sufficient and deterministic.

**Rationale**:
- ASP.NET Core antiforgery renders a fixed input shape; changes to that shape would be a framework upgrade we'd catch via CI regardless.
- Token **values** are random per-request; **names** and surrounding HTML are not. Stripping leaves structural differences visible and masks only the expected entropy source.
- Byte-equality after stripping is a stronger assertion than DOM-equality and computationally cheaper.

**Alternatives considered**:
- AngleSharp DOM parse + structural comparison — rejected; introduces a parsing dependency and surfaces false negatives from whitespace/attribute-order variation that are not semantically meaningful.
- Strip only the `value="..."` attribute (keep the rest of the hidden input) — rejected; overly clever; the whole input is the antiforgery surface area and treating part of it as stable invites future framework changes to break equality for unrelated reasons.

**Consequences**:
- New helper class `AntiforgeryStripper` in the integration-test project exposes `string Strip(string html)` and `(HttpStatusCode, string) Strip(HttpResponseMessage response)`.
- Unit-tested in `PublicRegistrationSweepTests` fixture setup (golden-file comparison: fresh render stripped vs. expected strip pattern).

---

## 8. Rate-limit policy override for FR-021 (resolves EC-009)

**Decision**: Test-only configuration override keyed into `IConfiguration` via `WebApplicationFactory.WithWebHostBuilder` + `builder.ConfigureAppConfiguration(cb => cb.AddInMemoryCollection(new[] { new("RateLimiting:Registration:PermitLimit", "3"), new("RateLimiting:Registration:WindowSeconds", "10") }))`. The production rate-limit configuration loader already reads these keys.

**Rationale**:
- The existing `builder.Services.AddRateLimiter(...)` configuration reads from `IConfiguration` (verified assumption — research task: confirm this reads from config before implementing; if not, the implementer falls back to replacing the registered `RateLimiterOptions` via `PostConfigure`).
- Overriding at the `IConfiguration` layer is the least-invasive test swap; it exercises the same policy registration pipeline as production.
- Synthetic limit of 3 requests per 10 s is tight enough to trigger in a single test (4 requests → 1 blocked) without hurting neighbouring tests that share the fixture (the limit resets per-IP and tests use the in-process `HttpClient` which presents a stable local IP).

**Alternatives considered**:
- Replace the `RateLimiter` middleware entirely with a stub — rejected; tests must exercise the real middleware, not a stand-in.
- Add a test-only `IOptionsMonitor<RateLimiterOptions>` override — rejected; more code than needed; config-level override is simpler.
- Run tests against the production limit — rejected; production limit is tens of requests/min, infeasible in CI.

**Consequences**:
- `DefenseInDepthTests.cs` fixture uses a custom `WebApplicationFactory` subclass that applies the overrides.
- If the production code does NOT read `RateLimiting:Registration:*` keys today, implementer must first refactor the rate-limit setup to read from config (small change; no spec touches needed in 016 as its code path is untouched by this feature). Call-out for the Phase 2 task list.

---

## 9. Spec front-matter format for `access-security: true` (resolves assumption in FR-008)

**Decision**: YAML front-matter at the very top of `spec.md`, between two `---` fences, with at minimum the key `access-security: true` when the spec opts in. Front-matter is OPTIONAL for non-access-security specs (absence = no floor enforcement).

**Rationale**:
- YAML front-matter is already used by brainstorm documents (`brainstorm/11-e2e-quality-gate.md`, `brainstorm/07-knowledge-module.md`, etc.) — same repo convention.
- Most existing spec files do NOT have front-matter today. Adding it only to opted-in specs is backward compatible.
- Format is trivial to parse (grab lines between the first two `---`, parse as YAML).

**Alternatives considered**:
- A dedicated header line `# Access-Security: true` inside the spec body — rejected; couples semantics to body formatting; more brittle.
- A sidecar file `spec-flags.yml` next to `spec.md` — rejected; two files to keep in sync.
- Inferring access-security from spec text (regex for "Authorize", "role", etc.) — rejected; unreliable and silently wrong.

**Consequences**:
- `SpecParser.cs` reads the first 100 bytes of each `spec.md`; if it starts with `---`, parses front-matter using a minimal YAML subset parser (three keys maximum: `access-security`, `feature-num`, `status`); otherwise treats the spec as non-access-security.
- Feature 016's spec (`specs/016-registration-access-hardening/spec.md`) and feature 018's own spec (`specs/018-access-security-delivery-quality-gate/spec.md`) MUST have `access-security: true` added to their front-matter as part of the implementation (trait retrofit task).

---

## 10. `Coverage: N/A` exclusion marker format (resolves EH-004 detail)

**Decision**: Exact line format immediately after the identifier definition:
`- **FR-018-03** …requirement text…  \n  *Coverage: N/A — <justification sentence>.*`

The trailing italicised note must be on the line **immediately following** the identifier line (or nested under the same list bullet), must start with the literal `*Coverage: N/A — ` (dash is an em-dash or two hyphens; parser accepts both), and must end with a full sentence of at least 20 characters of justification text.

**Identifier format**: Per the prefixed-ID convention adopted for opted-in specs, the parser recognises `**(FR|SC)-DDD**` and `**(FR|SC)-DDD-DD**` (the second form embeds the feature number, e.g., `**FR-016-01**`, `**SC-018-05**`). Both formats are first-class; collisions are detected by exact-string equality so the two formats never overlap.

**Rationale**:
- Italic emphasis in Markdown renders clearly in GitHub's PR diff viewer; reviewers see it inline.
- Enforcing a minimum justification length (20 chars) catches `Coverage: N/A — todo.` and similar empty excuses.
- Parser regex for exclusion line: `\*Coverage:\s*N/A\s*[—-]{1,2}\s*(.{20,}?)\.\s*\*`.
- Parser regex for identifier extraction: `\*\*(FR|SC)-\d{3}(?:-\d{2})?\*\*`.
- If the exclusion regex does not match a specification line declaring an exclusion intent, the identifier is treated as NOT excluded (FR-018-06 + EH-004).

**Alternatives considered**:
- Free-text `(Coverage: N/A)` marker anywhere in the section — rejected; ambiguous positioning, hard to parse deterministically.
- Separate `coverage-exclusions.yml` manifest — rejected; two sources of truth (see #9 rationale).
- Inline HTML comment — rejected; invisible in rendered Markdown; reviewers miss it.

**Consequences**:
- Tool emits error with path+line on malformed exclusion markers (EH-004).
- Implementation includes 3-4 unit tests in `SpecParserTests.cs` covering: valid exclusion, too-short justification, missing trailing period, wrong punctuation (en-dash vs em-dash), absent italic wrapping.

---

## 11. Trait attribute conventions (resolves cross-cutting question)

**Decision**: Exactly four trait kinds recognised by the tool:

| Trait key | Value format | Purpose |
|-----------|-------------|---------|
| `Spec` | `FR-DDD` or `FR-DDD-DD` (e.g., `FR-016-01`, `FR-018-15`) | Claims a functional-requirement identifier |
| `Sc` | `SC-DDD` or `SC-DDD-DD` (e.g., `SC-016-05`, `SC-018-01`) | Claims a success-criterion identifier |
| `Floor` | kebab-case category name (e.g., `response-indistinguishability`) | Claims a floor-category contribution |
| `Flaky` | literal `true` | Quarantines the test; tool treats as non-claiming (NFR-002) |

Other trait keys (`Category`, `Priority`, etc.) are ignored by the tool.

**Rationale**:
- Short, memorable, consistent PascalCase on key and kebab-case on value for floor names. Values for Spec/Sc preserve the spec's literal ID format (uppercase prefix).
- Distinguishing `Spec` and `Sc` rather than a single `Id` trait avoids overloading — FR and SC are semantically distinct contract surfaces.
- The tool's value-validation regex: `^FR-\d{3}(-\d{2})?$`, `^SC-\d{3}(-\d{2})?$`, `^[a-z][a-z0-9-]+$` for Floor. Both the unprefixed and prefixed identifier shapes are accepted; the prefixed shape is the convention for opted-in specs (016, 018, ...). Malformed values fail the build with a clear message.

**Alternatives considered**:
- Single `Id` trait carrying any identifier — rejected; value regex becomes loose; FR/SC ambiguity.
- Custom attribute subclasses (`[ClaimsFr("FR-015")]`) — rejected; non-standard; breaks xUnit's native trait discovery pipelines (IDE Test Explorer, CI filters).

**Consequences**:
- All retrofit tasks use only these trait kinds.
- SC-001's canary test in `CoverageAnalyzerTests.cs` asserts: a test carrying `[Trait("Spec","FR-XYZ")]` where no `FR-XYZ` exists in any spec → reported as Dangling; a spec `FR-QQQ` with no test trait → reported as Unclaimed.

---

## 12. Test fixtures for the tool's self-tests (SC-001)

**Decision**: Ship two synthetic fixtures under `tests/Mentoory.Specs.CoverageCheck.Tests/Fixtures/`:

- **`specs/`** — hand-written `spec-sample-clean.md`, `spec-sample-unclaimed.md`, `spec-sample-excluded.md`, `spec-sample-malformed-frontmatter.md`, `spec-sample-duplicate-id.md`.
- **`assemblies/`** — an `xunit-sample-tests` project compiled at test time (via `dotnet build` from the xUnit test host) that carries deliberately curated `[Trait]` patterns: good claims, dangling claim, skipped claim, multi-trait claim.

The analyser is run against these fixture paths and the reports are compared to golden JSON files (`expected-clean.json`, `expected-unclaimed.json`, etc.).

**Rationale**:
- Direct unit tests of `CoverageAnalyzer` would stub out the parsers and miss the integration. Fixture-driven tests exercise Parsing + Reflection + Coverage end-to-end.
- Compiling the sample assembly at test time (not pre-committing a `.dll`) avoids binary drift and makes the fixture source reviewable.

**Alternatives considered**:
- In-memory synthetic `System.Reflection.Emit` assemblies — rejected; verbose to author; harder to make representative of real xUnit traits.
- Pre-committed `.dll` fixtures in `Fixtures/assemblies/` — rejected; opaque in PR review; risk of toolchain rot.

**Consequences**:
- `tests/Mentoory.Specs.CoverageCheck.Tests/` project has an MSBuild `BeforeTargets="Build"` step that compiles the sample-xunit-tests project first.
- Total fixture-build overhead: ~1 s per test run, acceptable.

---

## 13. 50-probe count (OQ-003)

**Decision**: Single-value constant `50` for CI (FR-019). Not parameterised in this feature.

**Rationale**:
- 50 probes is defensible as a CI-every-PR budget (≤ 15 s per NFR-004). It is not a statistical proof of indistinguishability — it is a regression smoke test sized to catch the kinds of drift that matter (a whole code path changing, a new header appearing, a cookie name changing).
- True statistical indistinguishability is a property proven by reading the code, not by Monte Carlo sampling at any achievable N. The 50-probe test is a cheap *canary*, not a proof.
- Future nightly deep-run can parameterise by reading `COVERAGE_SWEEP_PROBES` env var inside the test; deferred to post-018 per Out-of-Scope.

**Alternatives considered**:
- 10 probes — rejected; noise margin too thin for meaningful signal.
- 500 probes — rejected; blows NFR-004's 15-s budget.
- Parameterised now — rejected as scope creep; OQ-003 is explicit that non-configurability is acceptable.

**Consequences**:
- `PublicRegistrationSweepTests.cs` hard-codes `const int ProbeCount = 50;` with a nearby comment referencing this research entry.

---

## 14. Constitution amendment placement (resolves FR-001 + NFR-005 ambiguity)

**Decision**: The six new floor categories go into `.specify/memory/access-security-constitution.md` as **new subsections 11.9, 11.10, 11.11, 11.12, 11.13, 11.14** (appended to Section 11 "Testing and Verification Requirements"). The Delivery Quality Gate content becomes a **new Section 13** (after existing Section 12 "Open Questions / Decisions Needed" and before the Appendices).

**Rationale**:
- Floor categories are semantically additional testing categories alongside 11.1–11.8; co-locating them preserves "one section for all testing rules."
- Delivery Quality Gate is structurally a workflow/governance rule (like Section 10 "Secure Feature Design Workflow"); a top-level section matches its scope (it governs the CI lifecycle, not just test content).
- Preserves all existing numbering (NFR-005 satisfied).

**Alternatives considered**:
- Append Delivery Quality Gate as Section 11.9 — rejected; conflates testing-category content with CI-workflow content.
- Both go as a single new Section 13 — rejected; harder to cross-reference floor categories from tests since their number would change if Section 13 were later renamed.
- Re-number for thematic cleanliness — rejected; explicitly forbidden by NFR-005.

**Consequences**:
- Amendment preserves every existing internal reference ("See Section 11.x") intact.
- Implementation task for Phase 2: mechanical edit adding 11.9–11.14 + Section 13, plus a sync-impact-report entry at the top of the file (existing convention in that doc).

---

## Research summary

All 14 decisions are concrete, with named files, named types, named values, and named alternatives rejected. No `NEEDS CLARIFICATION` markers remain. Phase 1 can proceed to `data-model.md` and `contracts/`.
