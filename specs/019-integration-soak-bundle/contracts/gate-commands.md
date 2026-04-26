# Gate Command Contract

**Date:** 2026-04-25
**Spec:** [../spec.md](../spec.md)

This file specifies the **exact commands** that constitute each gate step. Operational tasks in `tasks.md` reference these by ID. Treat each command as the canonical, copy-paste-ready invocation for that step — deviations should be justified in the bundle PR body.

All commands assume the working directory is the repo root and the integration branch is currently checked out, unless noted otherwise.

---

## G-PRE: Pre-flight verification (runs once, before any merge)

### G-PRE-1: Verify gh CLI auth

```bash
gh auth status
```

**Pass:** exit 0, "Logged in" appears in output.
**Fail:** halt; release manager runs `gh auth login` interactively.

### G-PRE-2: Verify each source PR is non-draft and clean against develop

```bash
gh pr view 11 --json isDraft,mergeable,mergeStateStatus | jq -e '.isDraft == false'
gh pr view 12 --json isDraft,mergeable,mergeStateStatus | jq -e '.isDraft == false'
gh pr view 13 --json isDraft,mergeable,mergeStateStatus | jq -e '.isDraft == false'
gh pr view 14 --json isDraft,mergeable,mergeStateStatus | jq -e '.isDraft == false'
```

**Pass:** all four return 0 (non-draft).
**Fail:** halt; investigate any draft PR before proceeding.

### G-PRE-3: Pin pre-soak E2E baseline

```bash
git checkout origin/develop -- tests/Mentoory.Tests.E2E/
dotnet test tests/Mentoory.Tests.E2E/ -c Release --list-tests \
  --filter "FullyQualifiedName~Tests" \
  > pre-soak-e2e-baseline.txt
wc -l pre-soak-e2e-baseline.txt
```

**Pass:** count is captured. Save the file as later evidence (NFR-003 invariant baseline).
**Fail:** halt; if `--list-tests` fails, fall back to inspecting `tests/Mentoory.Tests.E2E/Tests/*.cs` files manually.

---

## G-STD: Standalone build per branch (FR-001)

Run once per source branch BEFORE creating the integration branch.

### G-STD-1: Registration (#13) standalone build (strict coverage-check)

```bash
git fetch origin 016-registration-access-hardening
git checkout origin/016-registration-access-hardening -- .
dotnet build Mentoory.sln -c Release -p:NuGetAudit=false 2>&1 | tee logs/standalone-registration.log
```

**Pass:** exit 0, zero warnings (excluding pre-existing `NU1902` MailKit advisory which is silenced by `NuGetAudit=false`). Coverage-check passes in strict mode (registration retrofitted itself).
**Fail:** the registration branch is **excluded from the bundle** per FR-001. Comment on PR #13 with the failure log; bundle proceeds with the remaining 3 branches and the operator manually merges registration's coverage-check escape hatch state into the integration branch via a follow-up plan.

### G-STD-2 / G-STD-3 / G-STD-4: Lifecycle / Audit / Knowledge standalone builds

```bash
# Replace BRANCH with each of:
#   016-project-lifecycle-finish
#   016-audit-pipeline
#   016-knowledge-module-core
git fetch origin <BRANCH>
git checkout origin/<BRANCH> -- .
dotnet build Mentoory.sln -c Release -p:NuGetAudit=false 2>&1 | tee logs/standalone-<branch-short>.log
```

**Pass:** exit 0, zero warnings. (Coverage-check tool is not on these branches, so it does not run.)
**Fail:** branch is excluded from the bundle per FR-001.

**Note:** After each standalone check, restore the integration branch state with `git checkout 019-integration-soak -- .` before proceeding.

---

## G-MERGE-N: Merge step N (N = 1..4)

Each merge produces a merge commit on `019-integration-soak`.

### G-MERGE-1: Merge registration (#13) — first because smallest semantic surface

```bash
git checkout 019-integration-soak
git merge origin/016-registration-access-hardening --no-ff --no-edit \
  -m "Merge registration (#13) into bundle [1/4]"
# Resolve conflicts if any (per spec EC-* rules); commit resolution in the merge commit.
dotnet build Mentoory.sln -c Release -p:NuGetAudit=false -p:CoverageCheckMode=warn 2>&1 | tee logs/merge-1-build.log
```

**Pass:** clean merge or resolved conflicts; build returns 0; zero compiler warnings (coverage-check warnings tolerated).
**Fail:** if conflicts can't be resolved cleanly, halt and consult the spec's Edge Cases section for the named-file resolution rules.

### G-MERGE-2: Merge lifecycle (#11)

```bash
git merge origin/016-project-lifecycle-finish --no-ff --no-edit \
  -m "Merge lifecycle (#11) into bundle [2/4]"
# Apply EC-2: add `[Audited(eventType: AuditEventTypes.ProjectStageAdvanced)]` to AdvanceProjectStageCommand
# (the only command across all branches that needs it post-audit-merge).
# This is also a good moment to fix any 3-way conflict in Program.cs / MenuConfiguration.cs.
dotnet build Mentoory.sln -c Release -p:NuGetAudit=false -p:CoverageCheckMode=warn 2>&1 | tee logs/merge-2-build.log
```

**Pass / Fail:** as above.

### G-MERGE-3: Merge audit (#12)

```bash
git merge origin/016-audit-pipeline --no-ff --no-edit \
  -m "Merge audit (#12) into bundle [3/4]"
# Apply EC-4: re-apply [Audited] on the post-restructure RegisterUserCommand.
# Verify AuditCoverageTests passes after this merge:
dotnet test tests/Mentoory.Tests.Architecture/ -c Release \
  --filter "FullyQualifiedName~AuditCoverageTests" 2>&1 | tee logs/merge-3-audit-coverage.log
dotnet build Mentoory.sln -c Release -p:NuGetAudit=false -p:CoverageCheckMode=warn 2>&1 | tee logs/merge-3-build.log
```

**Pass:** AuditCoverageTests green; build green.
**Fail:** if AuditCoverageTests reports a missing `[Audited]` on a command, add it (the only known case is `AdvanceProjectStageCommand` per R4) and re-run.

### G-MERGE-4: Merge knowledge (#14) — largest, last

```bash
git merge origin/016-knowledge-module-core --no-ff --no-edit \
  -m "Merge knowledge (#14) into bundle [4/4]"
# Apply EC-1: hand-stitch DACPAC SSDT files (Projects.RowVersion + knowledge schema cross-FKs).
# Verify DACPAC builds:
dotnet build Mentoory.Db/MentooryDb.sqlproj -c Release -p:NuGetAudit=false 2>&1 | tee logs/merge-4-dacpac.log
dotnet build Mentoory.sln -c Release -p:NuGetAudit=false -p:CoverageCheckMode=warn 2>&1 | tee logs/merge-4-build.log
```

**Pass:** DACPAC builds; full solution builds.
**Fail:** if DACPAC fails, the SSDT hand-stitch needs revision; halt and resolve before proceeding to G-FULL.

---

## G-FULL: Full gate (FR-006)

Run once after all four merges have landed and incremental builds are green. Runs in parallel where safe.

### G-FULL-BUILD: Solution build (final)

```bash
dotnet build Mentoory.sln -c Release -p:NuGetAudit=false -p:CoverageCheckMode=warn \
  2>&1 | tee logs/gate-build.log
```

**Pass:** exit 0, zero compiler/StyleCop warnings, coverage-check violations downgraded to warnings (acceptable).

### G-FULL-UNIT: All unit test suites

```bash
dotnet test Mentoory.sln -c Release --no-build -p:NuGetAudit=false \
  --filter "Category!=Integration&Category!=E2E" \
  --logger "console;verbosity=normal" \
  2>&1 | tee logs/gate-unit.log
```

**Pass:** all unit test projects (Access, Tenant, Diagnostic, Knowledge, Shared, Architecture) green.

### G-FULL-INTEGRATION: Testcontainers integration suite

```bash
dotnet test tests/Mentoory.Tests.Integration/ -c Release --no-build \
  -p:NuGetAudit=false \
  --logger "console;verbosity=normal" \
  2>&1 | tee logs/gate-integration.log
```

**Prereq:** Docker daemon running (Testcontainers spins up SQL Server containers).
**Pass:** all integration tests green.

### G-FULL-E2E: Playwright E2E suite

```bash
dotnet test tests/Mentoory.Tests.E2E/ -c Release --no-build \
  -p:NuGetAudit=false \
  --logger "console;verbosity=normal" \
  2>&1 | tee logs/gate-e2e.log
```

**Prereq:** Playwright browsers installed (`pwsh tests/Mentoory.Tests.E2E/bin/Release/net10.0/playwright.ps1 install` if not).
**Pass:** all E2E tests green; pre-soak baseline preserved (per NFR-003).
**Flake handling (EC-5):** if any test fails, re-run that single test once. Two consecutive failures = real regression; trigger bisect (G-BISECT).

### G-FULL-DACPAC: DACPAC publish against integration DB

```bash
# Assumes Aspire AppHost is running OR a SQL Server target is configured.
dotnet build Mentoory.Db/MentooryDb.sqlproj -c Release -p:NuGetAudit=false \
  2>&1 | tee logs/gate-dacpac-build.log

# Publish (replace connection string with integration DB target):
sqlpackage /Action:Publish \
  /SourceFile:Mentoory.Db/bin/Release/MentooryDb.dacpac \
  /TargetConnectionString:"Server=localhost,1433;Database=MentooryIntegration;User=sa;Password=...;Encrypt=False;TrustServerCertificate=True" \
  2>&1 | tee logs/gate-dacpac-publish.log

# Idempotency check (NFR-002): re-publish; second run must be a no-op.
sqlpackage /Action:Publish ... 2>&1 | tee logs/gate-dacpac-publish-rerun.log
grep -E "(0 rows affected|No schema changes)" logs/gate-dacpac-publish-rerun.log
```

**Pass:** first publish succeeds; second publish reports no schema changes.

### G-FULL-COVERAGE: Coverage-check violations (warn mode, non-blocking)

```bash
# Coverage-check runs automatically as part of G-FULL-BUILD via MSBuild AfterTargets.
# Locate the captured log:
find . -name "coverage-check.log" -type f 2>/dev/null | xargs tail -n +1
# Or extract from the build log:
grep -A 200 "CheckSpecCoverage" logs/gate-build.log | tee logs/gate-coverage.log
```

**Pass:** report captured. Violations are recorded as gate evidence and listed in the bundle PR body — they do NOT block the bundle.
**Follow-up:** A separate spec backports `[Trait]` retrofits to lifecycle/audit/knowledge tests so a future bundle can run in strict mode.

---

## G-BISECT: Bisect on gate failure (FR-008)

Run only if any G-FULL-* step fails.

### G-BISECT-1..4: Revert merges in reverse order

```bash
# G-BISECT-1: revert knowledge (latest merge)
git revert -m 1 HEAD --no-edit  # HEAD is the knowledge merge commit
dotnet build Mentoory.sln -c Release -p:NuGetAudit=false -p:CoverageCheckMode=warn
# Re-run the failing gate step. If green, knowledge owns the regression. Stop.
# If still failing, proceed to G-BISECT-2.

# G-BISECT-2: revert audit
git log --merges --oneline | head -4   # find audit merge SHA
git revert -m 1 <audit-merge-sha> --no-edit
# Re-run failing gate step. If green, audit owns it. Stop.
# Otherwise proceed.

# G-BISECT-3: revert lifecycle
git revert -m 1 <lifecycle-merge-sha> --no-edit
# Re-run failing gate step. If green, lifecycle owns it. Stop.
# Otherwise proceed.

# G-BISECT-4: revert registration (last revert; abort if this also fails)
git revert -m 1 <registration-merge-sha> --no-edit
# At this point the integration branch == origin/develop; if gate is still red, the regression
# is on develop itself, not in the bundle. Abort the bundle and triage develop separately.
```

**Iteration cap (SC-006):** at most 4 reverts. After each revert:
1. Re-run the originally failing G-FULL-* step (no need to re-run already-green steps unless the revert changed their input).
2. If green: bisect terminates; record the regression-owning branch in `logs/bisect-result.txt`.
3. If red: continue to the next revert.

**Termination outcomes:**
- **Single regression isolated**: ship partial bundle (3 branches), regression branch goes back to its PR with bisect evidence.
- **Multiple independent regressions**: continue reverting until green; ship whatever remains; both reverted branches go back with evidence.
- **All four reverted, still red**: abort bundle; integration branch deleted; investigate develop separately.

---

## G-SHIP: Bundle PR open and source PR closure (FR-007, FR-010)

### G-SHIP-1: Push integration branch

```bash
git push -u origin 019-integration-soak
```

### G-SHIP-2: Open bundle PR

```bash
gh pr create \
  --base develop \
  --head 019-integration-soak \
  --title "Bundle: 016 ship — registration + lifecycle + audit + knowledge" \
  --body-file specs/019-integration-soak-bundle/bundle-pr-body.md
```

(`bundle-pr-body.md` is generated at execution time from the gate evidence files plus a template; not stored in this spec because content is dynamic.)

### G-SHIP-3: Merge bundle PR (after review)

```bash
# Default per FR-007 (subject to OQ-1 confirmation):
gh pr merge <BUNDLE_PR_NUMBER> --merge --auto --delete-branch=false
# OR if OQ-1 flips to squash:
# gh pr merge <BUNDLE_PR_NUMBER> --squash --auto --delete-branch=false
```

**Note:** `--delete-branch=false` because we delete `019-integration-soak` manually after closing source PRs (G-SHIP-4) so the source PRs' cross-reference comments still resolve to a valid branch reference.

### G-SHIP-4: Close source PRs with cross-reference

```bash
BUNDLE_PR=<bundle pr number>
for pr in 11 12 13 14; do
  gh pr close $pr --comment "Closed in favor of bundle ship #$BUNDLE_PR. The four 016-* PRs were merged together via the integration soak (specs/019-integration-soak-bundle/) to amortize E2E test cost. See bundle PR for full ship evidence."
done
```

### G-SHIP-5: Delete integration branch

```bash
git push origin --delete 019-integration-soak
git branch -D 019-integration-soak  # local
```

---

## Failure Decision Tree

```
START
  │
  ▼
G-PRE: pre-flight (gh auth, PRs non-draft, baseline pinned)
  │ pass? no → halt
  │ yes
  ▼
G-STD-{1..4}: standalone builds (parallel-eligible)
  │ all pass? no → exclude failing branch(es), proceed with rest
  │ yes
  ▼
G-MERGE-{1..4}: sequential merges with build checks
  │ each pass? no → resolve in place per spec EC-* rules; do NOT revert
  │ yes
  ▼
G-FULL-*: full gate (parallelize where safe)
  │ all pass? no → G-BISECT
  │ yes
  ▼
G-SHIP-{1..5}: bundle PR open, merge, close source PRs, cleanup
  │
  ▼
DONE
```
