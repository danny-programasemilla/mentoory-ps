# Research: Audit Pipeline — Browser E2E Coverage

**Phase:** 0 (Research — resolve unknowns before design)
**Feature:** 017-audit-e2e
**Date:** 2026-04-19

This document resolves every uncertainty the spec surfaced so Phase 1 design (contracts, quickstart) can proceed deterministically. Each section follows the Decision / Rationale / Alternatives pattern.

---

## R-01: Does `PlaywrightFixture` respawn the `audit` schema between tests?

**Decision**: No — the fixture does NOT use Respawn at all. Phase 1 MUST extend the fixture to respawn the `audit` schema between tests (and possibly the `access` / `diagnostic` / `tenant` schemas too, matching `MentooryWebApplicationFactory`).

**Rationale**:

- Grep of `tests/Mentoory.Tests.E2E/Infrastructure/PlaywrightFixture.cs` returns zero hits for `Respawn` or `SchemasToInclude`.
- The fixture deploys the DACPAC **once** in `IAsyncLifetime.InitializeAsync` and never resets between test methods.
- Without cross-test cleanup, P2's "exactly one row for CorrectAnswer" assertion would be polluted by prior test runs.
- The integration-test fixture (`MentooryWebApplicationFactory`) already respawns `["access", "tenant", "diagnostic", "example", "subscription", "audit"]` per feature 016. Bringing the E2E fixture to parity removes this inconsistency.

**Alternatives considered**:

- **Unique identifiers per test**: use random email / GUID in each test and filter the viewer by that value. Rejected: doesn't isolate menu-visibility or empty-state assertions; still leaves leaked rows; brittle.
- **Drop the DB between tests**: heavy-handed, would require redeploying DACPAC every test; slow.
- **Respawn (chosen)**: lightweight, already in use by the integration fixture, one-time extension.

**Phase 1 concrete task**: extend `PlaywrightFixture.InitializeDatabase` to instantiate a `Respawner` and expose a `ResetDatabaseAsync` method; wire it into `IAsyncLifetime.InitializeAsync` of each test class (or into a new `E2ETestBase` that mirrors `IntegrationTestBase.InitializeAsync`).

---

## R-02: How do tests drive the UI for each audited command?

**Decision**: Reuse the existing UI paths. All four retrofitted commands have production UI surfaces:

| Command | UI Path | Method | Source |
|---------|---------|--------|--------|
| `RegisterUserCommand` | `/Access/Register` | POST form | `Mentoory.Web/Areas/Access/Controllers/RegisterController.cs` |
| `LoginUserCommand` | `/Access/Login` | POST form | `Mentoory.Web/Areas/Access/Controllers/LoginController.cs` |
| `AssignRoleCommand` | `/Administration/Users/...` | modal form | `Mentoory.Web/Areas/Administration/Controllers/UsersController.cs` (existing `AdministrationUsersTests` confirms a role-assignment flow) |
| `CorrectAnswerCommand` | `/Coordination/AnswerCorrection/{guid}` | modal form (`Correct` action) | `Mentoory.Web/Areas/Coordination/Controllers/AnswerCorrectionController.cs` |
| `SetActiveContextCommand` | `/Context/Select` | POST | covered by the existing login flow (context selector runs on first login) |

**Rationale**: No test-only API stubs needed; the spec's "real user journey" intent is served by the actual production surfaces.

**Alternatives considered**:

- Dispatch commands via raw HTTP POST in the test (bypassing the form UI). Rejected: does not prove the UI-to-handler path works, which is the entire point of E2E.
- Dispatch via MediatR in-process (like `IntegrationTestBase.SendAsync`). Rejected: conflates E2E with integration; would violate FR-014 (no parallel helpers).

---

## R-03: How do tests assert the `X-Correlation-Id` response header in Playwright?

**Decision**: Use Playwright's `IResponse.Headers` on the navigation response, OR attach a `Page.Response` event handler and capture headers from the first document response.

```csharp
var response = await page.GotoAsync($"{fixture.BaseUrl}/");
response!.Headers.Should().ContainKey("x-correlation-id");
```

**Rationale**: Playwright exposes response headers on `IResponse` in a case-insensitive dictionary. Asserting on GET `/` (the home page) — no authentication required; the middleware runs for every request.

**Alternatives considered**:

- Intercept via `page.RouteAsync(...)`: more powerful but overkill for a header-echo assertion.
- Raw `HttpClient` from the test (bypassing the browser): simpler but defeats the "browser-driven" intent of E2E; already covered by integration tests.

---

## R-04: How do tests seed a corrigible diagnostic response for the Manual-mode test?

**Decision**: Seed through the database directly in the test's Arrange block — same pattern as the integration-level `CorrectAnswerAuditTests`. Steps:

1. Acquire `DiagnosticDbContext` via `fixture.Services.CreateScope()`.
2. Create `FormTemplate` + `ProjectForm` + `DiagnosticResponse` with one `QuestionResponse` (text-type) set to `"OriginalAnswer"`.
3. Save and capture the response's `ExternalId` and the `QuestionResponse.Id`.
4. Navigate the browser to `/Coordination/AnswerCorrection/{ExternalId}`, submit the modal form with `newTextValue=FixedAnswer`.
5. Assert the viewer shows the correction.

**Rationale**: Aligns with the existing integration-test seeding pattern; no need to add a test-only endpoint or DACPAC seed data for corrigible responses.

**Alternatives considered**:

- Add seeded diagnostic data to the DACPAC PostDeployment scripts. Rejected: pollutes global seed for one test; couples E2E to SSDT.
- Build a dedicated test-only API endpoint. Rejected: violates SC-005 (zero production-code changes).

---

## R-05: How do tests authenticate as each role in the browser?

**Decision**: Reuse the `LoginAsync(page, email, password)` helper pattern established in `AuthorizationTests.cs`. Copy the method verbatim into each new test class's private helper section, OR extract to a shared helper class under `tests/Mentoory.Tests.E2E/Infrastructure/Helpers/`.

**Preferred form**: extract to `tests/Mentoory.Tests.E2E/Infrastructure/LoginHelper.cs` (static method) to eliminate duplication across four new test files. This is a test-infrastructure addition, not a new fixture — allowed under FR-014 which forbids "new fixtures" specifically.

**Rationale**:

- `LoginAsync` handles the GlobalAdmin context-selector detour (incubator + role dropdown), which is non-trivial and would cause drift if duplicated four times.
- Seeded users and their passwords are already known (see invariants in RESUME-P1.md).

**Alternatives considered**:

- Duplicate `LoginAsync` in each test file. Rejected: drift risk — change the context-selector contract and four copies diverge.
- Cookie-based pre-auth (skip the login form): bypasses the UI path we're trying to exercise in P2.

**Phase 1 decision to confirm with user**: extract to a shared helper (adds one file) vs inline copy-paste (matches existing `AuthorizationTests` convention). Recommendation: shared helper — clarity beats mimicry here.

---

## R-06: What DOM selectors does the audit viewer expose for tests to target?

**Decision**: The viewer uses a standard DataTables-generated table with `id="auditLogTable"`. Tests target:

- `#auditLogTable` — the table itself
- `#auditLogTable thead th` — column headers (Spanish text assertion)
- `#auditLogTable tbody tr` — data rows
- `#auditLogTable tbody tr.dt-row-empty` or the empty-state container rendered inside `tbody` — empty-state assertion
- `.audit-expand` class on the expand button (from `audit-log.js`)
- `.filter-panel` / `#auditLogTable-filter-panel` — filter form
- `select[name='outcome']` within the filter panel — Outcome dropdown
- `.status.status-success / .status.status-danger` — Outcome badges
- DataTables pagination controls: `.dt-paging-button` (class from DataTables 2.x)

**Rationale**: Selectors are stable — they're either IDs (emitted by DataTables based on the tableId we pass to `initDataTable`) or Tabler-prescribed class names that would only change with a global UI migration.

**Alternatives considered**:

- `data-test="..."` attributes: cleaner but requires modifying the view (violates SC-005).
- XPath via text content: fragile across Spanish copy changes — defeats the point of the assertion.

---

## R-07: Does Playwright's navigation response preserve custom headers through the TestServer proxy?

**Decision**: Yes — the proxy in `PlaywrightFixture.ProxyRequestAsync` explicitly copies all response headers from the TestServer to the Kestrel response: lines 222-228 iterate `response.Headers` and `response.Content.Headers` into `context.Response.Headers`. The only header removed is `transfer-encoding`. `X-Correlation-Id` passes through unchanged.

**Rationale**: Verified by reading the fixture source. No test-time monkey-patching needed.

**Alternatives considered**: N/A — behavior already correct.

---

## R-08: How do tests query the viewer's DataTables server-side JSON?

**Decision**: Tests assert via the rendered DOM, not via the `/Administration/AuditLog/Data` JSON endpoint. Two reasons:

1. The DOM IS the product surface — a regression in DataTable column rendering would pass a JSON-level test.
2. The server-side endpoint is already covered by `AdminViewerTests` at the integration layer.

For assertions that need to scan the Details panel (e.g., "`***REDACTED***` present, password absent"), the test expands the row via `.audit-expand` click, waits for the child `<pre>` element, and reads its `innerText`.

**Rationale**: Keeps E2E focused on user-observable behavior.

**Alternatives considered**:

- Hybrid: hit `/Data` endpoint from within the browser. Rejected: complicates assertions with anti-forgery handling and duplicates integration coverage.

---

## R-09: Spanish copy — pinning strategy

**Decision**: Store every Spanish string under test as a `const` at the top of each test file (or in a shared `AuditSpanishCopy.cs` static class). Assertions reference the constants. Drift between the view and the constant produces a clean diff in code review.

**Rationale**: Keeps DOM assertions robust against whitespace noise, centralizes the "what Spanish copy we're pinning" contract.

**Alternatives considered**:

- Inline literal strings in each assertion. Rejected: duplication across four files; typos in the test go undetected.

**Phase 1 decision**: shared `AuditSpanishCopy` static class under `tests/Mentoory.Tests.E2E/Infrastructure/` — same justification as R-05 (test infrastructure, not fixture).

---

## R-10: RESUME-COMPLETE.md format — Markdown vs structured JSON

**Decision**: Human-readable Markdown for v1. Defer JSON until a concrete CI gate demands it.

**Rationale**:

- The final document's primary audience is a human reviewer comparing delivered coverage to the spec.
- Structured JSON adds maintenance cost (schema, parser, gate consumer) with no current consumer.
- If a CI gate is added later, a companion `RESUME-COMPLETE.json` can be emitted alongside the Markdown without changing the human format.

**Alternatives considered**:

- JSON now. Rejected: YAGNI.
- Both formats. Rejected: two sources of truth, drift risk.

---

## Summary of Phase 0 outputs

| Unknown | Resolved to |
|---------|-------------|
| Fixture respawn | Extend `PlaywrightFixture` to add Respawn matching the integration fixture |
| UI paths | All four commands reuse existing production controllers (no test-only surfaces) |
| Correlation header assertion | Playwright `IResponse.Headers` on GET `/` |
| Diagnostic response seeding | Arrange via `DiagnosticDbContext` in-test |
| Login | Extract shared `LoginHelper` (allowed — not a fixture) |
| DOM selectors | Stable DataTables IDs + Tabler classes |
| Header proxy | Already correct — no change needed |
| Data endpoint assertions | Skip — DOM assertions only |
| Spanish copy pinning | Shared `AuditSpanishCopy` constants |
| RESUME-COMPLETE format | Markdown |

**All NEEDS CLARIFICATION resolved. Proceed to Phase 1.**
