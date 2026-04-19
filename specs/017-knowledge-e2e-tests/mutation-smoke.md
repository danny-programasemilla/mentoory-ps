# Mutation-Test Smoke (T053 / SC-T04)

Manual procedure run on 2026-04-19 during Phase 9 of spec 017 to confirm the E2E + integration suite catches Phase-9 regressions in the two invariants the spec calls out (`quickstart.md § Mutation-Test Smoke`). Each mutation was applied, the targeted test was run, the result recorded, and the change reverted before moving on. No mutation is on-branch at the end of this procedure.

---

## Mutation A — Drop `UQ_KnowledgeStructures_ProjectId`

**Target invariant**: exactly-one KS per Project (`specs/016-knowledge-module-core` Phase 9 binding). The `CloneFormTemplate` cascade is expected to create at most one `knowledge.KnowledgeStructures` row per `ProjectId`; the UNIQUE constraint is the DB-level backstop.

**Mutation**: `Mentoory.Db/knowledge/Tables/KnowledgeStructures.sql` — commented out `CONSTRAINT [UQ_KnowledgeStructures_ProjectId] UNIQUE ([ProjectId])`, rebuilt the DACPAC.

**Target test**: `tests/Mentoory.Tests.Integration/Knowledge/DiagnosticCascadeRoundTripTests.cs` → `CloneFormTemplate_TwiceForSameProject_DoesNotDuplicateProjectKs`

**Command**:
```bash
dotnet test tests/Mentoory.Tests.Integration/Mentoory.Tests.Integration.csproj \
    --filter "FullyQualifiedName~CloneFormTemplate_TwiceForSameProject_DoesNotDuplicateProjectKs"
```

**Result**: `Passed! — Failed: 0, Passed: 1, Skipped: 0, Total: 1, Duration: 1 s`

**Interpretation**: the test **still passes** with the UNIQUE constraint dropped because `CloneFormTemplateHandler` is idempotent at the application layer — on the second clone it looks up the existing `KnowledgeStructure` for the project (`FirstOrDefault` on `ProjectId`) and reuses it rather than inserting a new row. The DB-level UNIQUE constraint is defense-in-depth; the invariant itself IS still asserted (line 26 of the test: `projectStructures.Should().HaveCount(1)`) but the guard that would be exercised by this mutation sits in the handler, not the DB.

This is a legitimate gap in the mutation test's discriminating power, but not a coverage gap in the invariant: the handler-level idempotence check is what the test actually covers. To exercise the DB constraint directly, a future test would bypass the handler and insert a raw `KnowledgeStructure` row via `KnowledgeDbContext`. Not in Phase 9 scope; flagged here so a Phase-10 candidate exists.

**Revert**: restored the constraint and rebuilt the DACPAC before running Mutation B. Re-ran the same test to confirm it still passes with the constraint restored: `Passed! — 1/1, 2 s`.

---

## Mutation B — Remove `[Authorize(Roles="GlobalAdmin")]` from the Templates GET action

**Target invariant**: GlobalAdmin-only access to `/Coordination/Knowledge/Templates`. The `[Authorize(Roles="GlobalAdmin")]` attribute on the controller action is the authorization guard; any regression would surface as coordinators reaching the curation surface.

**Mutation**: `Mentoory.Web/Areas/Coordination/Controllers/KnowledgeController.cs` — commented out the `[Authorize(Roles = "GlobalAdmin")]` attribute on the `Templates` GET action (line 72). The other four `[Authorize]` attributes on sibling Template routes (`Create`, detail, etc.) were left intact so this mutation targets exactly one row of the `[Theory]`.

**Target test**: `tests/Mentoory.Tests.E2E/Tests/KnowledgeAuthorizationTests.cs` → `ProtectedRoutes_CoordinatorDenied_ForTemplateRoutes` (theory with 3 rows: `/Templates`, `/Templates/Create`, `/Templates/{id}`)

**Command**:
```bash
dotnet test tests/Mentoory.Tests.E2E/Mentoory.Tests.E2E.csproj \
    --filter "FullyQualifiedName~ProtectedRoutes_CoordinatorDenied_ForTemplateRoutes"
```

**Result**:
```
[FAIL]  ProtectedRoutes_CoordinatorDenied_ForTemplateRoutes(routeTemplate: "/Coordination/Knowledge/Templates")
   Expected (effectiveStatus >= 400) to be True because ProjectCoordinator must be denied
   on GET /Coordination/Knowledge/Templates (got HTTP 200), but found False.

Failed!  — Failed: 1, Passed: 2, Skipped: 0, Total: 3, Duration: 7 s
```

**Interpretation**: exactly as predicted — the `/Templates` row fails with HTTP 200 (coordinator reaches the GlobalAdmin-only curation page), the other two rows still pass because their `[Authorize]` attributes were not touched. SC-T04 satisfied: the suite catches this regression.

**Important execution note**: `dotnet test --no-build` does **not** rebuild the Web assembly. The E2E runner loads `tests/Mentoory.Tests.E2E/bin/Debug/net10.0/Mentoory.Web.dll`, which is refreshed only when the E2E project is rebuilt (or via the default `dotnet test` build step). On first attempt, `dotnet build Mentoory.Web/Mentoory.Web.csproj` updated the Web bin but the stale test-bin copy made the mutation invisible to the test (all 3 rows appeared to pass). Rebuilding `tests/Mentoory.Tests.E2E` picked up the fresh DLL. Future T053 invocations should omit `--no-build` or explicitly rebuild the test project after mutating controllers.

**Revert**: restored the `[Authorize]` attribute and rebuilt. Re-ran the test to confirm all 3 rows pass again: `Passed! — 3/3, 7 s`.

---

## Outcome Summary

| # | Mutation | Target test | Expected | Observed | SC-T04 |
|---|---|---|---|---|---|
| A | Drop `UQ_KnowledgeStructures_ProjectId` | `CloneFormTemplate_TwiceForSameProject_DoesNotDuplicateProjectKs` | FAIL | PASS (app-layer idempotence pre-empts DB constraint) | Partial — test covers invariant via handler path; DB-level constraint is backstop only |
| B | Remove `[Authorize(Roles="GlobalAdmin")]` on Templates GET | `ProtectedRoutes_CoordinatorDenied_ForTemplateRoutes` | ≥1 row FAIL | 1 row FAIL (`/Templates`), 2 rows PASS (unchanged) | ✅ |

**Net**: Mutation B fully satisfies SC-T04's spirit (the suite catches an authorization regression). Mutation A's false-negative surfaces a nuance — the handler-level check absorbs the mutation before it reaches the DB — but the invariant itself is still asserted at the test's post-clone rowcount. Recommended Phase-10 follow-up: add a direct-DbContext test that inserts a second `KnowledgeStructure` row for an already-populated `ProjectId` and asserts `SaveChangesAsync` throws a `DbUpdateException` with a unique-index violation, so the DB-level backstop has its own dedicated regression guard.

**Working tree clean after procedure**: `git status` showed no changes to `Mentoory.Db/` or `Mentoory.Web/` after both reverts. Verified via a full build: `dotnet build` exited 0 warnings / 0 errors.
