# Resume — Phase 4: Regressions + Spanish QA

**Previous phase:** P3 (Correlation)
**Branch:** 017-audit-e2e
**Base commit:** 73dc201
**Authored:** 2026-04-19
**Status:** ready

---

## What was shipped in P3

- `tests/Mentoory.Tests.E2E/Tests/AuditLogCorrelationTests.cs` — 3 test methods:
  - `AnyGetResponse_CarriesValidGuidCorrelationId`
  - `ClientProvidedCorrelationId_IsEchoedVerbatim`
  - `TwoRequestsWithoutHeader_GetDistinctCorrelationIds`

No infrastructure helpers were added in P3 — the P1 shared `LoginHelper`, `AuditSpanishCopy`, `PlaywrightFixture.ResetDatabaseAsync()`, and `E2ETestBase` covered every new test. The per-test browser context with custom `ExtraHTTPHeaders` (T025 echo test) is built inline against `Fixture.Browser!.NewContextAsync(...)`; the fixture's `CreateBrowserContextAsync()` helper has no overload for options and was intentionally not extended (YAGNI). If P4 (or future phases) needs the pattern repeatedly, promote it to an `Infrastructure/` helper at that point.

No code touched outside `tests/Mentoory.Tests.E2E/` and `specs/017-audit-e2e/`. `git diff 61109ca..HEAD -- ':!tests' ':!specs'` emits zero lines.

Cumulative `AuditLog*` runtime at the P3 checkpoint: **49 s for 19 tests** (still well inside the ≤ 3 min per-feature budget). P3-only subset: 731 ms for 3 tests.

---

## What to do in this session (P4)

**Objective**: Encode invariants that should never regress — no plaintext passwords leak into Details, every Spanish UI label remains pinned, Outcome badges render with the correct color class, and validator-rejected commands produce zero audit rows (proves the pipeline order `Validator → Auditing → Transaction`). This is the terminal phase: it ends with `RESUME-COMPLETE.md`, not another `RESUME-P{N+1}.md`.

**Spec reference**: `specs/017-audit-e2e/spec.md § User Story 4` and `specs/017-audit-e2e/tasks.md § Phase 5 US4` (T028–T036).

**Test methods to add** (verbatim from `data-model.md § Phase 4` coverage matrix):

- `NoPlaintextPasswordAppearsInAnyDetails`
- `FilterBarButtons_HaveSpanishLabels`
- `PaginationControls_HaveSpanishLabels`
- `OutcomeBadges_RenderSpanishTextAndColor`
- `ValidationRejectedCommand_ProducesNoAuditRow`

All five live in a single new file: `tests/Mentoory.Tests.E2E/Tests/AuditLogRegressionTests.cs`. Inherit `E2ETestBase` so each test starts with an empty `[audit]` schema.

Kickoff gate (T028): run `dotnet test tests/Mentoory.Tests.E2E/ --filter "FullyQualifiedName~AuditLog" -p:WarningsNotAsErrors=NU1902` — must be 19/19 green before writing any new test.

P4 is the **terminal** phase — after the Phase 4 commit and before ending the session, run T034-T035 polish (full AuditLog* suite end-to-end + scope-boundary check) and write `RESUME-COMPLETE.md` (NOT `RESUME-P5.md`) per `contracts/resume-prompt-schema.md § RESUME-COMPLETE.md special structure`.

---

## Invariants to preserve (do NOT violate)

- **Seeded users** (from DACPAC PostDeployment, verified present):
  - `admin@mentoory.com` — GlobalAdmin, password `123abc987`
  - `incadmin1@test.mentoory.com` — IncubatorAdmin, password `Test123!@#`
  - `entrepreneur1@test.mentoory.com` — Entrepreneur, password `Test123!@#`
  - `mentor1@test.mentoory.com` — Mentor, password `Test123!@#`
  - `sponsor1@test.mentoory.com` — Sponsor, password `Test123!@#`
- **Shared fixture**: `PlaywrightFixture` via `[Collection(E2ETestCollection.Name)]`. Do not create new fixtures (FR-014). After T001, the fixture respawns the `audit` schema between tests — assume it.
- **Login helper**: shared `LoginHelper.LoginAsync(page, email, password, baseUrl)` under `tests/Mentoory.Tests.E2E/Infrastructure/`. Do not duplicate; do not write parallel helpers (FR-014).
- **Spanish copy pinning**: shared `AuditSpanishCopy` static class under `tests/Mentoory.Tests.E2E/Infrastructure/` holds every Spanish string asserted by audit tests. All assertions reference constants; NO inline literals.
- **Playwright timeouts**: every `WaitForLoadStateAsync` / `WaitForFunctionAsync` / `WaitForSelectorAsync` call MUST pass an explicit timeout ≤ 15 s (FR-015).
- **Scope boundary**: no file outside `tests/Mentoory.Tests.E2E/` and `specs/017-audit-e2e/` may be modified. Any deviation MUST be called out in the commit body AND the next RESUME's Gotchas section.
- **Manual-mode JSON shape**: `Details` for `CorrectAnswerCommand` carries `Before.TextValue` / `After.NewTextValue` — pinned by `specs/016-audit-pipeline/data-model.md`.

---

## Gotchas discovered in P3

- **Playwright lowercases response header keys.** `IResponse.Headers` returns a `Dictionary<string, string>` whose keys are all lowercase, regardless of the casing the server actually emitted. The CorrelationMiddleware writes `X-Correlation-Id` (title case, see `Mentoory.Web/Infrastructure/Correlation/CorrelationMiddleware.cs:14`), but Playwright reads it back as `x-correlation-id`. P3 declares two constants (`OutboundHeaderName = "X-Correlation-Id"` for setting via `ExtraHTTPHeaders`, `InboundHeaderName = "x-correlation-id"` for reading via `IResponse.Headers`). Apply the same split in any P4 test that asserts response headers.
- **CorrelationMiddleware silently regenerates the GUID for non-GUID inputs.** The middleware's `TryParseIncoming` (line 31-40 of `CorrelationMiddleware.cs`) returns `null` if `Guid.TryParse(raw, out var parsed)` fails, falling back to `Guid.NewGuid()`. Consequence: the "echo verbatim" assertion (`ClientProvidedCorrelationId_IsEchoedVerbatim`) MUST send a valid-GUID value. If P4's `NoPlaintextPasswordAppearsInAnyDetails` (or any other test) ever sends a non-GUID `X-Correlation-Id` to keep traces sortable, do NOT then assert echo.
- **`PlaywrightFixture.CreateBrowserContextAsync()` has no options overload.** P3's T025 echo test needed per-context `ExtraHTTPHeaders`, so the test calls `Fixture.Browser!.NewContextAsync(new BrowserNewContextOptions { … })` directly (mirrors the fixture body verbatim: `IgnoreHTTPSErrors = true`, `ScreenSize = 1280×720`). YAGNI applied — no helper added. If P4 needs the same per-context configuration in more than one test, promote it to `Infrastructure/PlaywrightFixture.cs` as an overload.
- **Tests that don't read audit rows still benefit from `E2ETestBase`.** The 3 P3 tests never query the viewer or the `[audit]` table, but they still inherit `E2ETestBase` so the `audit` schema is respawned per test. This keeps any incidental write (a stray request that triggers a logged event) from leaking into the next test's count assertion. Apply the same default to P4 tests even when not asserting on audit rows directly — `T030 FilterBarButtons_HaveSpanishLabels` is a good example: opens the viewer, asserts copy only, but the GlobalAdmin login emits `User.LoggedIn` + `Context.Activated` rows that other tests would otherwise see.
- **The proxy in `PlaywrightFixture.ProxyRequestAsync` faithfully forwards every request header AND every response header.** This is what makes the correlation tests work end-to-end (the X-Correlation-Id round-trip survives the Kestrel→TestServer hop). If P4 introduces any header-based assertion (e.g., security headers, Cache-Control), trust the proxy and assert directly on `IResponse.Headers`. The single proxy quirk: `transfer-encoding` is stripped (line 247) — don't try to assert that one.
- **`git diff 61109ca..HEAD -- ':!tests' ':!specs'` is the canonical scope check for P3.** For P4, switch the base to the P3 phase commit: `git diff 73dc201..HEAD -- ':!tests' ':!specs'` MUST emit zero lines. (Comparing against `develop` will continue to produce non-empty output because of the `aae56bb` / `66128a3` spec-kit bookkeeping commits at the start of the feature — pre-existing artifacts of `/speckit-specify` + `/speckit-plan`, NOT production code.)

---

## Definition of done for P4

- All five US4 tests green:
  ```
  dotnet test tests/Mentoory.Tests.E2E/ \
    --filter "FullyQualifiedName~AuditLogRegressionTests" \
    -p:WarningsNotAsErrors=NU1902
  ```
- Cumulative `AuditLog*` suite (P1 + P2 + P3 + P4) green: 24/24.
- All lower-layer suites still green:
  ```
  dotnet test tests/Mentoory.Shared.Application.Tests/ -p:WarningsNotAsErrors=NU1902
  dotnet test tests/Mentoory.Tests.Architecture/ -p:WarningsNotAsErrors=NU1902
  dotnet test tests/Mentoory.Tests.Integration/ -p:WarningsNotAsErrors=NU1902
  ```
- Full E2E suite green (proves no regression in the existing 97 tests):
  ```
  dotnet test tests/Mentoory.Tests.E2E/ -p:WarningsNotAsErrors=NU1902
  ```
- Total `AuditLog*` runtime under 3 min (P3's 19 cumulative tests ran in 49 s; expect ≤ 1 min for the 24).
- No diff outside `tests/Mentoory.Tests.E2E/` and `specs/017-audit-e2e/` from the P3 base commit:
  ```
  git diff 73dc201..HEAD -- ':!tests' ':!specs'
  ```
  Must emit zero lines.
- **Commit message** (exact title, load-bearing for SC-004):
  ```
  Add E2E coverage for audit pipeline — phase 4 (Regressions + Spanish QA)
  ```
  Body lists the 5 tests + any infrastructure added.

---

## Next handoff

P4 is the terminal phase. After the phase commit is green and pushed:

1. Write `specs/017-audit-e2e/RESUME-COMPLETE.md` following `contracts/resume-prompt-schema.md § RESUME-COMPLETE.md special structure` (NOT the per-phase RESUME schema). Required sections:
   - Header block (same as per-phase, but `Status: complete`).
   - `## What was shipped in P4` (the 5 tests + any helpers).
   - `## Invariants to preserve` (copy verbatim, one last time).
   - `## Retrospective` with subsections:
     - `### Coverage delivered` — 24 tests across 4 files, total runtime, headcount per phase.
     - `### Deviations from spec` — flag anything done differently from `tasks.md` (e.g., AssignRole's MediatR dispatch instead of UI flow, P2 gotcha #2).
     - `### Gotchas aggregated across phases` — copy from RESUME-P2/P3/P4 Gotchas sections, deduplicated.
     - `### Suggested follow-ups` — non-blocking items (e.g., promote PlaywrightFixture overload, migrate `MenuVisibilityTests.LoginAsync` to `LoginHelper`).
2. Commit with title `Add RESUME-COMPLETE after phase 4 terminal checkpoint`, push.
3. **End the session.** The feature branch `017-audit-e2e` is now ready for PR to `develop`.

## Final session message (paste verbatim)

```
Phase 4 shipped: 5 new tests. Feature complete.
Phase commit: {sha}
RESUME-COMPLETE commit: {sha}
Cumulative AuditLog* tests: 24, runtime {seconds}s
Full E2E suite: {n}/{n} green
Branch 017-audit-e2e ready for PR to develop.
```
