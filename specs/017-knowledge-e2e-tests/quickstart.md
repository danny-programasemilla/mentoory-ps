# Quickstart: Running the Knowledge Module E2E Suite

**Audience**: Developer who just pulled the 016-branch PR and wants to verify the new E2E coverage locally.

## Prerequisites

1. **.NET 10 SDK** (pre-release) installed. Check: `dotnet --version` → `10.0.*`.
2. **Docker Desktop** running. Testcontainers needs it for the MSSQL instance.
3. **Playwright browsers** installed. After the first restore, run:
   ```bash
   pwsh tests/Mentoory.Tests.E2E/bin/Debug/net10.0/playwright.ps1 install chromium
   ```
   (or `powershell` on Windows). This downloads Chromium once per machine.
4. **DACPAC built**. The fixture locates `Mentoory.Db/bin/Debug/MentooryDb.dacpac`. Build it with:
   ```bash
   dotnet build Mentoory.Db/MentooryDb.sqlproj
   ```
   Rebuild after any schema or PostDeployment script change — the fixture does NOT rebuild automatically.

## Running the Suite

### Run everything

```bash
dotnet test tests/Mentoory.Tests.E2E/Mentoory.Tests.E2E.csproj
```

Expected wall-time on a typical laptop: ~5–6 minutes (Phase 9 measured 5m 28s locally; CI reference target: ≤ 6 minutes per SC-T02). One test is currently quarantined (`KnowledgeAuthorizationTests.TenantIsolation_CoordinatorB_CannotAccessCoordinatorAsKs`) via `[Fact(Skip=...)]` — see the XML doc on that method; the same invariant is covered by `KnowledgeProjectTreeEditingTests.ProjectTopic_TenantIsolation_CrossProjectReturnsNotFound`. Expect `Passed! — Failed: 0, Passed: 138, Skipped: 1, Total: 139`.

### Run a single user-story file

```bash
# US1 — template curation
dotnet test tests/Mentoory.Tests.E2E/Mentoory.Tests.E2E.csproj \
    --filter "FullyQualifiedName~KnowledgeTemplatesTests"

# US4 — project tree editing
dotnet test tests/Mentoory.Tests.E2E/Mentoory.Tests.E2E.csproj \
    --filter "FullyQualifiedName~KnowledgeProjectTreeEditingTests"
```

### Run a single test method

```bash
dotnet test tests/Mentoory.Tests.E2E/Mentoory.Tests.E2E.csproj \
    --filter "FullyQualifiedName~KnowledgeTemplatesTests.TopicPriorityRanges_SaveAndReload_PersistsThreeBands"
```

### Run with a visible browser (debugging)

```bash
E2E_HEADED=1 dotnet test tests/Mentoory.Tests.E2E/Mentoory.Tests.E2E.csproj \
    --filter "FullyQualifiedName~KnowledgePartialSyncTests.PartialSync_AppendsNewTemplateTopic_UnderMatchingParent"
```

The `E2E_HEADED=1` env var (added to `PlaywrightFixture.cs` per research R7) launches Chromium with `Headless=false` and `SlowMo=250ms` so you can watch the test drive the UI.

## Running Integration Backstops

```bash
dotnet test tests/Mentoory.Tests.Integration/Mentoory.Tests.Integration.csproj \
    --filter "FullyQualifiedName~DiagnosticCascadeRoundTripTests"
```

These are faster (~30s total) because they don't drive Playwright. They also verify the DB-level invariants the UI can't see.

## Troubleshooting

### "MentooryDb.dacpac not found"

Rebuild the DACPAC: `dotnet build Mentoory.Db/MentooryDb.sqlproj`. The fixture looks in `Mentoory.Db/bin/Debug/` from the test binary's working directory and walks up.

### "Docker daemon not running"

Testcontainers requires a live Docker daemon. Start Docker Desktop (or `systemctl start docker` on Linux) and re-run.

### Tests hang on login

Symptom: `WaitForFunctionAsync("sel => sel.options.length > 1")` times out.

Likely cause: the context-selector XHR never returned. Check:
- Is the seed data actually in place? `SELECT COUNT(*) FROM access.Users WHERE Email LIKE '%@test.mentoory.com'` should return ≥ 4.
- Did the DACPAC deploy fail silently? Run `dotnet build Mentoory.Db/MentooryDb.sqlproj` and check for PostDeployment errors.

### Flaky test blocking a PR

1. Re-run the single test in isolation: `--filter "FullyQualifiedName~{method}"`.
2. If it passes, the suite has a cross-test ordering dependency (FR-T22 violation). File a bug and tag the author.
3. If it fails consistently, the test is a genuine regression — investigate before merging.
4. If the test is flaky (fails 3+ times in 10 runs), quarantine via `[Trait("Quarantine","true")]` per FR-T32 and file a tracking issue.

## Screenshots on Failure

Every test captures `screenshots/{method}_{timestamp}.png` on failure (EC-10). On CI, these are uploaded as build artifacts; locally, find them at:

```
tests/Mentoory.Tests.E2E/bin/Debug/net10.0/screenshots/
```

## Adding a New Test

1. Put the test in the file that matches its user-story (see `contracts/test-files.md`).
2. Use `KnowledgeTestHelpers.LoginAndSelectAsync` — do not copy-paste login logic.
3. Use unique names for any entity the test creates: `$"E2E Thing {Guid.NewGuid():N}"`.
4. Wrap the body in `try/finally` with `_fixture.TakeScreenshotOnFailureAsync` + `page.Context.DisposeAsync`.
5. Keep total wall-time under 30 seconds (FR-T30).
6. Add a one-line XML doc comment referencing the spec section: `/// <summary>Covers US1-5 (overlapping priority ranges).</summary>`.

## Mutation-Test Smoke (SC-T04)

Full procedure and reference output: [`mutation-smoke.md`](./mutation-smoke.md) (generated 2026-04-19 during Phase 9).

Short version — to manually verify the suite catches Phase 9 regressions:

1. On a scratch branch, revert one invariant. Two worked-out examples:
   - **A (DB constraint)**: comment out `CONSTRAINT [UQ_KnowledgeStructures_ProjectId] UNIQUE ([ProjectId])` in `Mentoory.Db/knowledge/Tables/KnowledgeStructures.sql`. Note: `CloneFormTemplate_TwiceForSameProject_DoesNotDuplicateProjectKs` still **passes** under this mutation because `CloneFormTemplateHandler` is idempotent at the application layer — the UNIQUE constraint is a DB-level backstop. The invariant itself is still asserted; the constraint just never fires in this path. See `mutation-smoke.md § Mutation A` for the Phase-10 follow-up recommendation (direct-DbContext test targeting the DB constraint).
   - **B (Authorization attribute)**: comment out `[Authorize(Roles = "GlobalAdmin")]` on the `Templates` GET action in `Mentoory.Web/Areas/Coordination/Controllers/KnowledgeController.cs`. `ProtectedRoutes_CoordinatorDenied_ForTemplateRoutes` fails on the `/Templates` row as expected.
2. Rebuild the affected project (DACPAC for A, Web for B) **and** rebuild the test project so the fresh binary propagates into `tests/Mentoory.Tests.E2E/bin/Debug/net10.0/`. Do NOT use `--no-build` after a controller or SQL change — the stale test bin silently hides the mutation.
3. Run the targeted test.
4. Revert the mutation; re-run the test to confirm green. `git status` should show no changes to `Mentoory.Db/` or `Mentoory.Web/`.
