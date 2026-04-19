# Resume — Phase 2: Capture flows

**Previous phase:** P1 (Viewer shell)
**Branch:** 017-audit-e2e
**Base commit:** 5e6d440
**Authored:** 2026-04-19
**Status:** ready

---

## What was shipped in P1

- `tests/Mentoory.Tests.E2E/Tests/AuditLogViewerTests.cs` — 9 test methods:
  - `GlobalAdmin_CanOpenAuditLog_TableRenders`
  - `IncubatorAdmin_IsDenied`
  - `Entrepreneur_IsDenied`
  - `Mentor_IsDenied`
  - `Sponsor_IsDenied`
  - `GlobalAdmin_SeesMenuEntryUnderPlataforma`
  - `IncubatorAdmin_DoesNotSeeMenuEntry`
  - `FilterWithNoMatch_ShowsSpanishEmptyState`
  - `OutcomeDropdown_HasSpanishOptions`
- `tests/Mentoory.Tests.E2E/Infrastructure/AuditSpanishCopy.cs` — pinned Spanish copy constants for column headers, filter buttons, Outcome options, pagination labels, empty state, menu entry, page title.
- `tests/Mentoory.Tests.E2E/Infrastructure/E2ETestBase.cs` — abstract base that calls `Fixture.ResetDatabaseAsync()` in `IAsyncLifetime.InitializeAsync` so every inheriting test starts against a clean `[audit]` schema.
- `tests/Mentoory.Tests.E2E/Infrastructure/LoginHelper.cs` — extracted from `AuthorizationTests.LoginAsync` (verbatim, including the GlobalAdmin context-selector branch). `AuthorizationTests` now calls `LoginHelper.LoginAsync(...)`.
- `tests/Mentoory.Tests.E2E/Infrastructure/PlaywrightFixture.cs` — added `Respawner` field + public `ResetDatabaseAsync()`. Initialized inside `InitializeDatabase()` after the DACPAC deploy.
- `tests/Mentoory.Tests.E2E/Mentoory.Tests.E2E.csproj` — added `<PackageReference Include="Respawn" />`.

No code touched outside `tests/Mentoory.Tests.E2E/` and `specs/017-audit-e2e/`. `git diff develop..HEAD ':!tests' ':!specs'` emits zero lines.

---

## What to do in this session (P2)

**Objective**: Drive the real UI for Register, Login (success + failure), Assign Role, and Correct Answer; assert each action produces a correctly-shaped row in the viewer with correct Outcome, UserEmail, and (for Register) password redaction.

**Spec reference**: `specs/017-audit-e2e/spec.md § User Story 2` and `specs/017-audit-e2e/tasks.md § Phase 3 US2` (T014–T022).

**Test methods to add** (verbatim from `data-model.md § Phase 2` coverage matrix):

- `RegisterUser_ProducesAuditRow_WithRedactedPassword`
- `LoginWithInvalidPassword_ProducesFailureRow`
- `LoginWithValidPassword_ProducesSuccessRow`
- `AssignRole_ProducesAuditRow`
- `CorrectAnswer_ProducesRowWithBeforeAndAfter`
- `CorrectAnswer_ProducesExactlyOneRow_NoDoubleWrite`
- `ExpandButton_RevealsPrettyPrintedDetails`

All seven live in a single new file: `tests/Mentoory.Tests.E2E/Tests/AuditLogCaptureTests.cs`. Inherit `E2ETestBase` (or, equivalently, call `Fixture.ResetDatabaseAsync()` in the constructor) so each test starts with an empty `[audit]` schema.

Kickoff gate (T014): run `dotnet test tests/Mentoory.Tests.E2E/ --filter "FullyQualifiedName~AuditLog" -p:WarningsNotAsErrors=NU1902` — must be 9/9 green before writing any new test.

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

## Gotchas discovered in P1

- **Respawn schema list narrowed from 6 schemas to 1.** RESUME-P1 (T001) prescribed `SchemasToInclude = ["access","tenant","diagnostic","example","subscription","audit"]` copied verbatim from the integration fixture. Applying it broke every Playwright login: integration tests build their own users per test via `RegisterAndActivateUserAsync` and don't depend on DACPAC seeds, but E2E tests log in as the seeded `admin@mentoory.com` / `incadmin1@…` / etc. Wiping `[access]` deletes those users, every login fails, and the `denied` predicate in role-denial tests passes coincidentally because `/Access/Login` is in the URL. The fix shipped in P1: `SchemasToInclude = ["audit"]` only. Documented inline in `PlaywrightFixture.cs` (line ~250) and called out in the phase 1 commit body. **Implication for P2**: the `[diagnostic]` and `[access]` schemas are NOT reset between tests. Use unique entity IDs and unique email addresses (e.g., `Guid.NewGuid()`-derived) for any test that creates persistent rows so cross-test interference is impossible. The CorrectAnswer tests must arrange a fresh diagnostic graph per test rather than relying on schema reset.
- **Column headers are CSS-uppercased.** `#auditLogTable thead th` source text is `"Fecha (UTC)"`, `"Evento"`, etc. (matches `AuditSpanishCopy` constants), but the rendered display is `"FECHA (UTC)"`, `"EVENTO"` due to a `text-transform: uppercase` rule. **Use `Locator.AllTextContentsAsync()` (source DOM text), NOT `AllInnerTextsAsync()` (rendered text), for header assertions.** Same will apply to other styled labels — when an assertion fails on case, switch accessor before second-guessing the constant.
- **Filter panel starts hidden (`style="display:none;"`).** Built by `datatable-helper.js initComplete`. Must click the toggle link `#auditLogTable-filter-toggle` before the form's inputs are visible to Playwright. The form/select elements ARE in the DOM regardless of visibility — counting `<option>` elements does not require expanding the panel, but `FillAsync` on the email input does. Pattern used in P1 (`OutcomeDropdown_HasSpanishOptions`, `FilterWithNoMatch_ShowsSpanishEmptyState`): `await page.Locator("#auditLogTable-filter-toggle").ClickAsync()`, then `Assertions.Expect(form).ToBeVisibleAsync(...)` before interacting.
- **Filter submit triggers an AJAX POST to `/Administration/AuditLog/Data`, not a full page reload.** Wait for the response before asserting the new tbody contents. Pattern: `var ajaxTask = page.WaitForResponseAsync(r => r.Url.Contains("/Administration/AuditLog/Data") && r.Status == 200, new() { Timeout = 15000 }); await form.Locator("button[type='submit']").ClickAsync(); await ajaxTask;`. The empty-state copy is then visible inside `#auditLogTable tbody`.
- **Plataforma menu group selector**. Tabler renders the group as `<li class="nav-item dropdown">` containing `<span class="nav-link-title">Plataforma</span>` + a `.dropdown-menu` with `<a class="dropdown-item" href="/Administration/AuditLog">Registro de auditoría</a>`. There is no `data-menu-group` attribute (RESUME-P1's hypothetical selector). Use `#sidebar li.nav-item.dropdown:has(span.nav-link-title:text-is("Plataforma"))` or scope by the dropdown-item href directly.
- **`AuthorizationTests.LoginAsync` is now `LoginHelper.LoginAsync` (signature: `(IPage page, string email, string password, string baseUrl)`).** Note the added `baseUrl` parameter — the original method captured `_fixture.BaseUrl` from the test class. `MenuVisibilityTests` still has its own private `LoginAsync` copy; out of scope for P1, did not refactor it. P2 may want to migrate it (low value; non-blocking).
- **Existing E2E test cost**: the 97 existing tests take ~3 min wall-clock. P1's 9 tests added 22 s on the green path. P2's 7 tests should land well inside the ≤ 1 min/phase budget.

---

## Definition of done for P2

- All seven US2 tests green:
  ```
  dotnet test tests/Mentoory.Tests.E2E/ \
    --filter "FullyQualifiedName~AuditLogCaptureTests" \
    -p:WarningsNotAsErrors=NU1902
  ```
- Cumulative `AuditLog*` suite (P1 + P2) green: 16/16.
- All lower-layer suites still green:
  ```
  dotnet test tests/Mentoory.Shared.Application.Tests/ -p:WarningsNotAsErrors=NU1902
  dotnet test tests/Mentoory.Tests.Architecture/ -p:WarningsNotAsErrors=NU1902
  dotnet test tests/Mentoory.Tests.Integration/ -p:WarningsNotAsErrors=NU1902
  ```
- Total `AuditLog*` runtime under 3 min (expect ≤ 1 min for the 16 cumulative tests).
- No diff outside `tests/Mentoory.Tests.E2E/` and `specs/017-audit-e2e/`:
  ```
  git diff develop..HEAD -- ':!tests' ':!specs'
  ```
  Must emit zero lines.
- **Commit message** (exact title, load-bearing for SC-004):
  ```
  Add E2E coverage for audit pipeline — phase 2 (Capture flows)
  ```
  Body lists the 7 tests + any infrastructure added.

---

## Next handoff

After the phase commit is green and pushed:

1. Write `specs/017-audit-e2e/RESUME-P3.md` following `contracts/resume-prompt-schema.md` verbatim. Theme: `Correlation`. Copy this file's Invariants section VERBATIM into it. Populate Section 1 with the 7 test methods shipped + any helper additions. Populate Gotchas with anything non-obvious learned during P2. Section 2's test-method list comes from `tasks.md § Phase 4 US3` (T024-T026).
2. Commit with title `Add RESUME-P3 after phase 2 checkpoint`, push.
3. **End the session.** Do NOT start Phase 3 in this context. The next session claims it by reading `RESUME-P3.md` fresh.

## Final session message (paste verbatim)

```
Phase 2 shipped: 7 new tests.
Phase commit: {sha}
RESUME commit: {sha}
Next session: read specs/017-audit-e2e/RESUME-P3.md
Cumulative AuditLog* tests: 16, runtime {seconds}s
```
