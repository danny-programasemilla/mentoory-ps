# Resume — Phase 3: Correlation

**Previous phase:** P2 (Capture flows)
**Branch:** 017-audit-e2e
**Base commit:** 61109ca
**Authored:** 2026-04-19
**Status:** ready

---

## What was shipped in P2

- `tests/Mentoory.Tests.E2E/Tests/AuditLogCaptureTests.cs` — 7 test methods:
  - `RegisterUser_ProducesAuditRow_WithRedactedPassword`
  - `LoginWithInvalidPassword_ProducesFailureRow`
  - `LoginWithValidPassword_ProducesSuccessRow`
  - `AssignRole_ProducesAuditRow`
  - `CorrectAnswer_ProducesRowWithBeforeAndAfter`
  - `CorrectAnswer_ProducesExactlyOneRow_NoDoubleWrite`
  - `ExpandButton_RevealsPrettyPrintedDetails`

No infrastructure helpers were added in P2 — the P1 shared `LoginHelper`, `AuditSpanishCopy`, and `PlaywrightFixture.ResetDatabaseAsync()` covered every new test. Private helpers `ApplyFiltersAsync` / `ReadDataRowsAsync` / `ExpandFirstRowAndReadDetailsAsync` / `LoginAsGlobalAdminAndOpenViewerAsync` / `SeedDiagnosticResponseAsync` / `SubmitAnswerCorrectionViaUiAsync` live inside `AuditLogCaptureTests` itself (scope: P2 flows only). If P3 / P4 need any of them, promote them to `Infrastructure/` at that point (YAGNI-style).

No code touched outside `tests/Mentoory.Tests.E2E/` and `specs/017-audit-e2e/`. `git diff 5e6d440..HEAD -- ':!tests' ':!specs'` emits zero lines.

Cumulative `AuditLog*` runtime at the P2 checkpoint: **43 s for 16 tests** (still well inside the ≤ 3 min per-feature budget). P2-only subset: 25 s for 7 tests.

---

## What to do in this session (P3)

**Objective**: Prove the correlation middleware is wired through the real production pipeline and surfaced at the browser boundary — any GET response carries a valid-GUID `x-correlation-id`, a client-supplied header is echoed verbatim, and unheadered requests receive distinct ids.

**Spec reference**: `specs/017-audit-e2e/spec.md § User Story 3` and `specs/017-audit-e2e/tasks.md § Phase 4 US3` (T023–T027).

**Test methods to add** (verbatim from `data-model.md § Phase 3` coverage matrix):

- `AnyGetResponse_CarriesValidGuidCorrelationId`
- `ClientProvidedCorrelationId_IsEchoedVerbatim`
- `TwoRequestsWithoutHeader_GetDistinctCorrelationIds`

All three live in a single new file: `tests/Mentoory.Tests.E2E/Tests/AuditLogCorrelationTests.cs`. Inherit `E2ETestBase` so each test starts with an empty `[audit]` schema (even though these tests read no audit rows — the respawn matters for consistency).

Kickoff gate (T023): run `dotnet test tests/Mentoory.Tests.E2E/ --filter "FullyQualifiedName~AuditLog" -p:WarningsNotAsErrors=NU1902` — must be 16/16 green before writing any new test.

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

## Gotchas discovered in P2

- **DataTables 2.x child-row selector surprise.** The RESUME-P2 plan assumed the expanded detail row would carry a `.child` CSS class (`tbody tr.child pre`), but the jQuery plugin in use here inserts the child `<tr>` without any reliable class attribute — the outer `<tr>` and its nested `<td>` simply appear after the parent row. Fix that shipped in P2: locate by `#auditLogTable tbody pre` (there is no other `<pre>` element under the audit table, so `.First` is unambiguous after clicking `.audit-expand`). Apply the same pattern in any P3/P4 expand-based assertion.
- **`AssignRoleCommand` has NO public UI endpoint in `Mentoory.Web`.** RESUME-P2 directed us to "reuse the role-assignment flow from `AdministrationUsersTests.cs`", but that test file only exercises the users DataTable — neither it nor any other controller posts an AssignRole request. FR-017 forbids adding a production endpoint, so `AuditLogCaptureTests.AssignRole_ProducesAuditRow` dispatches `AssignRoleCommand` directly via `Fixture.Services.CreateScope()` + `IMediator.Send(...)` with an existing seeded user (`sponsor1@test.mentoory.com` gets the Mentor role in `Incubadora Alpha`, which they don't already hold). Consequences: the resulting audit row's `UserEmail` is **null** (no HTTP `ITenantContext` is established for a pure MediatR dispatch), so the original spec assertion "UserEmail = admin@mentoory.com (the actor)" from `tasks.md T018` is not satisfiable. The test asserts `EventType=Role.Assigned`, `Outcome=Éxito`, and `Action=AssignRoleCommand` instead. **Implication for P3/P4**: if any downstream test assumes AssignRole is a UI-driven action, substitute the same MediatR dispatch pattern; do NOT attempt to click through the admin users page.
- **Filter pipeline: text-input values vs. select-option values.** The audit-log viewer's outcome filter is a `<select>` whose option values are the backend constants (`''`, `Success`, `Failure`) — NOT the displayed Spanish labels (`Todos`, `Éxito`, `Fallo`). When selecting, pass `"Failure"` to `SelectOptionAsync`, not `AuditSpanishCopy.FalloOption`. The eventType select is similar — option values come from `AuditEventTypes` constants (`User.Registered`, `Role.Assigned`, etc.); use those. Only the `userEmail` text input takes the raw email string.
- **StyleCop ordering (`SA1204`, `SA1512`) enforced on new test files.** Within private-method clusters, all `static` helpers MUST come before instance helpers. A banner comment `// ---- Private helpers ----` followed by a blank line hits `SA1512` ("single-line comments should not be followed by blank line"). Trim the banner — method names alone are sufficient organization.
- **`[diagnostic]` + `[access]` schemas persist across tests.** (Carried forward from P1.) P2 relied on this for DACPAC-seeded projects (`Proyecto Innovación`, `Incubadora Alpha`, `coord1@test.mentoory.com`). The CorrectAnswer tests seed fresh `FormTemplate` + `ProjectForm` + `DiagnosticResponse` per test with Guid-derived template names, so no cross-test collisions; but any P3/P4 test that creates persistent rows MUST use unique IDs/emails.
- **Bootstrap 5 modal timing for submit.** The AnswerCorrection UI is a Bootstrap 5 modal triggered by `data-bs-toggle="modal"` / `data-bs-target="#correctModal-{id}"`. Playwright clicking the trigger button opens the modal with a fade animation; waiting with `Assertions.Expect(modal).ToBeVisibleAsync(15000ms)` is sufficient. Then fill the textareas and click submit — the POST redirects back to the Index action, so `WaitForResponseAsync(url=Correct) + WaitForLoadStateAsync(NetworkIdle)` is a clean synchronization pattern.
- **Scope-boundary check caveat.** `git diff develop..HEAD -- ':!tests' ':!specs'` emits non-empty output on this branch because `.specify/feature.json` and `CLAUDE.md` were modified by the `/speckit-specify` and `/speckit-plan` workflow at the start of the feature (commits `aae56bb`, `66128a3`). Those are pre-existing spec-kit bookkeeping, not production code. For a clean per-phase check, compare against the prior phase's base commit instead: `git diff 5e6d440..HEAD -- ':!tests' ':!specs'` — that MUST emit zero lines for P2, and the equivalent check (`git diff {P2-base}..HEAD …`) MUST emit zero lines for P3.
- **Admin login in the viewer page test produces audit rows too.** When a test opens the viewer after performing the action-under-test, the GlobalAdmin login itself generates `User.LoggedIn` + `Context.Activated` rows. That's why every P2 assertion filters by `eventType` or `userEmail` — the raw `[audit]` table after the test run has the action-under-test row PLUS the admin-session rows. Keep this pattern in P3 correlation tests if any of them inspect audit rows.

---

## Definition of done for P3

- All three US3 tests green:
  ```
  dotnet test tests/Mentoory.Tests.E2E/ \
    --filter "FullyQualifiedName~AuditLogCorrelationTests" \
    -p:WarningsNotAsErrors=NU1902
  ```
- Cumulative `AuditLog*` suite (P1 + P2 + P3) green: 19/19.
- All lower-layer suites still green:
  ```
  dotnet test tests/Mentoory.Shared.Application.Tests/ -p:WarningsNotAsErrors=NU1902
  dotnet test tests/Mentoory.Tests.Architecture/ -p:WarningsNotAsErrors=NU1902
  dotnet test tests/Mentoory.Tests.Integration/ -p:WarningsNotAsErrors=NU1902
  ```
- Total `AuditLog*` runtime under 3 min (expect ≤ 1 min for the 19 cumulative tests — P2 ran 16 in 43 s).
- No diff outside `tests/Mentoory.Tests.E2E/` and `specs/017-audit-e2e/` from the P2 base commit:
  ```
  git diff 61109ca..HEAD -- ':!tests' ':!specs'
  ```
  Must emit zero lines.
- **Commit message** (exact title, load-bearing for SC-004):
  ```
  Add E2E coverage for audit pipeline — phase 3 (Correlation)
  ```
  Body lists the 3 tests + any infrastructure added.

---

## Next handoff

After the phase commit is green and pushed:

1. Write `specs/017-audit-e2e/RESUME-P4.md` following `contracts/resume-prompt-schema.md` verbatim. Theme: `Regressions + Spanish QA`. Copy this file's Invariants section VERBATIM into it. Populate Section 1 with the 3 test methods shipped + any helper additions. Populate Gotchas with anything non-obvious learned during P3. Section 2's test-method list comes from `tasks.md § Phase 5 US4` (T029-T033).
2. Commit with title `Add RESUME-P4 after phase 3 checkpoint`, push.
3. **End the session.** Do NOT start Phase 4 in this context. The next session claims it by reading `RESUME-P4.md` fresh.

## Final session message (paste verbatim)

```
Phase 3 shipped: 3 new tests.
Phase commit: {sha}
RESUME commit: {sha}
Next session: read specs/017-audit-e2e/RESUME-P4.md
Cumulative AuditLog* tests: 19, runtime {seconds}s
```
