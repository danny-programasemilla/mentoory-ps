# Resume — Feature complete

**Previous phase:** P4 (Regressions + Spanish QA)
**Branch:** 017-audit-e2e
**Base commit:** 0e24008
**Authored:** 2026-04-19
**Status:** complete

---

## What was shipped in P4

- `tests/Mentoory.Tests.E2E/Tests/AuditLogRegressionTests.cs` — 5 test methods:
  - `NoPlaintextPasswordAppearsInAnyDetails`
  - `FilterBarButtons_HaveSpanishLabels`
  - `PaginationControls_HaveSpanishLabels`
  - `OutcomeBadges_RenderSpanishTextAndColor`
  - `ValidationRejectedCommand_ProducesNoAuditRow`

No infrastructure helpers were added in P4 — the P1 shared `LoginHelper`, `AuditSpanishCopy`, `PlaywrightFixture.ResetDatabaseAsync()`, and `E2ETestBase` covered every new test. Private helpers `ApplyFilterByUserEmailAsync` / `CountDataRowsAsync` / `LoginAsGlobalAdminAndOpenViewerAsync` / `AttemptInvalidLoginAsync` / `RegisterUserViaUiAsync` live inside `AuditLogRegressionTests` itself; the Register UI driver and the viewer-open helper now have duplicate copies in both `AuditLogCaptureTests` and `AuditLogRegressionTests` (see *Suggested follow-ups*).

No code touched outside `tests/Mentoory.Tests.E2E/` and `specs/017-audit-e2e/`. `git diff 73dc201..HEAD -- ':!tests' ':!specs'` emits zero lines.

Cumulative `AuditLog*` runtime at the P4 checkpoint: **51 s for 24 tests** (well inside the ≤ 3 min per-feature budget). P4-only subset: 15 s for 5 tests. Full E2E suite (existing 97 + new 24): 121/121 green in 3 m 50 s.

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

## Retrospective

### Coverage delivered

- **24 tests** across **4 files** under `tests/Mentoory.Tests.E2E/Tests/`:
  - `AuditLogViewerTests.cs` (P1) — 9 tests — viewer shell, authorization, menu visibility, Spanish copy
  - `AuditLogCaptureTests.cs` (P2) — 7 tests — UI-driven capture flows, redaction, pretty-printed Details
  - `AuditLogCorrelationTests.cs` (P3) — 3 tests — correlation middleware at the HTTP boundary
  - `AuditLogRegressionTests.cs` (P4) — 5 tests — defensive invariants (redaction, copy, badge color, pipeline order)
- **Shared infrastructure** added under `tests/Mentoory.Tests.E2E/Infrastructure/`: `AuditSpanishCopy.cs`, `E2ETestBase.cs`, `LoginHelper.cs`; `PlaywrightFixture.cs` extended with a `Respawner` targeting `[audit]` only.
- **Runtime per phase (phase-only subset + cumulative)**:
  - P1: 9 tests / 22 s / cumulative 22 s
  - P2: 7 tests / 25 s / cumulative 43 s (16 tests)
  - P3: 3 tests / 731 ms / cumulative 49 s (19 tests)
  - P4: 5 tests / 15 s / cumulative 51 s (24 tests)
- **Full E2E suite**: 121/121 green in 3 m 50 s (97 pre-existing + 24 new). No regression in pre-existing tests across all four checkpoints.
- **Lower-layer suites** (re-verified at every checkpoint): `Mentoory.Shared.Application.Tests` 18/18, `Mentoory.Tests.Architecture` 4/4, `Mentoory.Tests.Integration` 97/97.
- **Scope (SC-005)**: across all four phases, zero production-code changes — `git diff {prior-phase-base}..HEAD -- ':!tests' ':!specs'` emits zero lines at every checkpoint.

### Deviations from spec

- **T018 (`AssignRole_ProducesAuditRow`) — dispatched via MediatR instead of a UI flow.** `Mentoory.Web` has no public endpoint that posts `AssignRoleCommand`; `tasks.md` directed the test to "reuse the role-assignment flow from `AdministrationUsersTests.cs`", but that file only exercises the users DataTable. FR-017 forbids adding a production endpoint for the sake of a test, so P2 dispatches the command directly via `Fixture.Services.CreateScope()` + `IMediator.Send(...)`. The `AuditingBehavior` still runs in the real MediatR pipeline, so the row is produced genuinely. Consequence: `UserEmail` on the resulting row is `null` (no HTTP `ITenantContext` is established for a pure MediatR dispatch), so the original `tasks.md T018` assertion "UserEmail = admin@mentoory.com (the actor)" is not satisfiable. The test asserts `EventType = Role.Assigned`, `Outcome = Éxito`, and `Action = AssignRoleCommand` instead. This trade-off is called out in the P2 commit body and the RESUME-P3 Gotchas. If a product decision later adds a `/Administration/Users/AssignRole` form, the test should be rewritten to drive it through the browser.
- **T001 Respawn schema list narrowed from six schemas to one (`[audit]`).** `tasks.md T001` prescribed `SchemasToInclude = ["access","tenant","diagnostic","example","subscription","audit"]` copied verbatim from the integration fixture. Applying it broke every Playwright login because the E2E tests depend on DACPAC-seeded users that live in `[access]`; the integration tests, by contrast, build their users per-test via `RegisterAndActivateUserAsync`. P1 shipped with `SchemasToInclude = ["audit"]` only. Documented inline in `PlaywrightFixture.cs` (~line 250) and in the P1 commit body. Implication — carried forward across P2–P4: `[diagnostic]` and `[access]` schemas are NOT reset between tests; every new row uses `Guid.NewGuid()`-derived unique IDs/emails to avoid cross-test collisions.
- **T033 (`ValidationRejectedCommand_ProducesNoAuditRow`) — rejection point is earlier than the spec implied.** The spec said "FluentValidation rejects before the handler fires". In practice the `RegisterViewModel` carries a `[EmailAddress]` DataAnnotation, so ASP.NET ModelState rejects the malformed email before MediatR is invoked at all — FluentValidation on `RegisterUserCommand` never runs for this input. The functional invariant that matters (validator-rejected commands produce zero audit rows) is proven by the same test, but note that the short-circuit in the E2E case is ModelState, not FluentValidation. The command-level FluentValidation path is covered by unit tests in `Mentoory.Shared.Application.Tests`.

### Gotchas aggregated across phases

Deduplicated from P2/P3/P4 RESUME files. Anything load-bearing for future audit-related test work in this repo belongs here:

**Fixture + DB isolation**
- **Respawn targets `[audit]` only.** Do not widen `SchemasToInclude`; every DACPAC-seeded user in `[access]` must survive between tests. Use unique IDs/emails for any new row written to `[access]` or `[diagnostic]`. (P1)
- **Admin login seeds audit rows too.** Opening the viewer after the action-under-test produces an additional `User.LoggedIn` + `Context.Activated` pair. Every P2+ assertion that counts rows filters by `eventType` or `userEmail` first. (P2)

**UI selectors + DOM quirks**
- **Column headers are CSS-uppercased** via `text-transform: uppercase`. Use `Locator.AllTextContentsAsync()` (source DOM text), NOT `AllInnerTextsAsync()` (rendered text), for header assertions. (P1)
- **Filter panel starts hidden** (`style="display:none"`, built by `datatable-helper.js initComplete`). Must click `#auditLogTable-filter-toggle` before interacting with the form. Option elements ARE in the DOM regardless of visibility. (P1)
- **Filter submit triggers an AJAX POST to `/Administration/AuditLog/Data`.** Wait for the response before asserting new `tbody` contents. Pattern: `WaitForResponseAsync(r => r.Url.Contains("/Administration/AuditLog/Data"))` + click + await. (P1)
- **DataTables 2.x child-row has no stable class.** The expanded `<tr>` carries no reliable CSS hook — locate the detail panel as `#auditLogTable tbody pre` instead of `tr.child pre`. (P2)
- **Filter values vs. display labels: the Outcome and EventType `<select>`s carry backend constants as option values** (`""`, `"Success"`, `"Failure"`; `User.LoggedIn`, `Role.Assigned`, etc.) — not the Spanish labels. Pass backend constants to `SelectOptionAsync`; display labels only appear in the assertions on rendered cells. (P2)
- **Plataforma menu selector**: `#sidebar li.nav-item.dropdown:has(span.nav-link-title:text-is("Plataforma"))`, then scope by `a.dropdown-item[href='/Administration/AuditLog']`. No `data-menu-group` attribute. (P1)

**Correlation middleware**
- **Playwright lowercases response header keys.** `CorrelationMiddleware` writes `X-Correlation-Id` (title case); Playwright reads it back as `x-correlation-id`. P3 declares two constants: `OutboundHeaderName = "X-Correlation-Id"` for `ExtraHTTPHeaders`, `InboundHeaderName = "x-correlation-id"` for `IResponse.Headers` lookup. (P3)
- **`CorrelationMiddleware.TryParseIncoming` silently regenerates a new GUID for non-GUID inputs.** The "echo verbatim" assertion MUST send a valid-GUID value; any future header-round-trip test must either send a valid GUID or not assert echo. (P3)
- **The proxy in `PlaywrightFixture.ProxyRequestAsync` faithfully forwards every request + response header**, except `transfer-encoding` (stripped at line 247). Header-based assertions can trust the round-trip. (P3)

**Framework + tool quirks**
- **HTML5 `type="email"` constraint blocks malformed-email submissions** in Chromium. To reach server-side validation with a deliberately malformed value, set `form.noValidate = true` via `page.EvaluateAsync(...)` before clicking submit. (P4)
- **Bootstrap 5 modal timing for submit**: trigger button opens with fade animation — wait with `Assertions.Expect(modal).ToBeVisibleAsync(15000ms)` before filling inputs. POST redirects back to the Index action; `WaitForResponseAsync + WaitForLoadStateAsync(NetworkIdle)` is the clean sync pattern. (P2)
- **StyleCop `SA1204`** enforces static members before instance members within the same access level. Within a class's private-helper region, all `static` helpers must precede instance helpers. `SA1512` forbids banner comments followed by blank lines — use method names alone to organize. (P2)

**Scope + diff hygiene**
- **`git diff develop..HEAD -- ':!tests' ':!specs'` is NON-empty for this branch** because the `/speckit-specify` + `/speckit-plan` workflow modified `.specify/feature.json` and `CLAUDE.md` in commits `aae56bb` and `66128a3` at the start of the feature. Those are spec-kit bookkeeping, not production code. For a clean per-phase scope check, compare against the prior phase's base commit: at every checkpoint `git diff {prior-phase-sha}..HEAD -- ':!tests' ':!specs'` MUST emit zero lines. (P2)
- **`PlaywrightFixture.CreateBrowserContextAsync()` has no options overload.** P3 needed per-context `ExtraHTTPHeaders` for the correlation-echo test, so the test calls `Fixture.Browser!.NewContextAsync(new BrowserNewContextOptions { … })` directly (mirrors the fixture body verbatim). YAGNI applied — no helper added. If a future test suite needs the same per-context configuration in 3+ places, promote it to a fixture overload. (P3)

### Suggested follow-ups

Non-blocking items left for future iteration:

- **Promote `PlaywrightFixture.CreateBrowserContextAsync()` to accept an options callback / overload.** Currently P3's `ClientProvidedCorrelationId_IsEchoedVerbatim` hand-rolls `Browser!.NewContextAsync(new BrowserNewContextOptions { ... })` to inject `ExtraHTTPHeaders`. If any future test wants per-context proxy auth, extra headers, or a different viewport, add a `CreateBrowserContextAsync(Action<BrowserNewContextOptions>? configure = null)` overload that starts from the same base configuration and applies the caller's tweaks.
- **Migrate `MenuVisibilityTests.LoginAsync` to `LoginHelper.LoginAsync`.** P1 extracted the helper and migrated `AuthorizationTests`, but `MenuVisibilityTests.cs` still has its own private `LoginAsync` copy. Low value, purely cosmetic.
- **Promote `RegisterUserViaUiAsync` to `Infrastructure/`.** It now lives verbatim in both `AuditLogCaptureTests` and `AuditLogRegressionTests` (two callers). The YAGNI threshold applied by the RESUME files is three; if a future audit-related test adds the same helper for a third time, promote it. Same applies to `LoginAsGlobalAdminAndOpenViewerAsync` (currently in two test classes).
- **Consider a shared `AuditViewerPage` page-object.** The filter-panel toggle + filter submission + AJAX wait + row-counting pattern now appears in 3+ test methods across 3 classes. A thin page-object under `Infrastructure/` would consolidate the selectors (`#auditLogTable-filter-toggle`, `#auditLogTable-filter-form`, `input[name='userEmail']`, the `Data` endpoint URL) in one place and absorb future UI-structure changes.
- **Improve `RegisterUserViaUiAsync`'s resilience to country list changes.** Every Register-driving test hard-codes `Country = "CRI"`. If `ListCountriesQuery` stops returning Costa Rica, every dependent test breaks in lockstep. A small helper that reads `select[name='Country']` options and picks the first non-empty value would remove the coupling.
- **Follow up on the `AssignRole` product gap.** The need to dispatch `AssignRoleCommand` directly via MediatR reveals that no user-facing admin page exists for role assignment. The GlobalAdmin administration UI should probably gain a role-assignment form; once it does, rewrite `AssignRole_ProducesAuditRow` to drive it through the browser.
- **Full E2E runtime trending 4 min.** 121/121 green in 3 m 50 s is acceptable today, but the existing 97 tests already dominate the budget. Before adding the next feature's tests, consider whether any currently-serial Playwright tests could be parallelized across `IBrowserContext` instances within the same fixture — the proxy/Kestrel host is already reentrant.

---

## Next handoff

None — feature complete. Branch `017-audit-e2e` is ready for PR to `develop`.

## Final session message (paste verbatim)

```
Phase 4 shipped: 5 new tests. Feature complete.
Phase commit: 0e24008
RESUME-COMPLETE commit: {sha}
Cumulative AuditLog* tests: 24, runtime 51s
Full E2E suite: 121/121 green
Branch 017-audit-e2e ready for PR to develop.
```
