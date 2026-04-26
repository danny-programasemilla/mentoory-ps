# Quickstart — Integration Soak Bundle

**Date:** 2026-04-25
**Spec:** [spec.md](./spec.md)

This is the operational runbook the release manager follows to execute the bundle. Each step references a Gate Command ID from [contracts/gate-commands.md](./contracts/gate-commands.md). Estimated total time: 1 working day (per NFR-001 soft target).

> **TL;DR:** `git checkout -b 019-integration-soak origin/develop` → merge 4 PRs in size order → `dotnet build && dotnet test` → open bundle PR → close source PRs.

---

## Step 0 — Pre-flight (10 min)

1. Confirm you're on a clean working tree:
   ```bash
   git status --short  # should be empty (or only .specify/.idea/ etc.)
   ```
2. Run **G-PRE-1**: `gh auth status` — confirm logged in.
3. Run **G-PRE-2**: verify all 4 source PRs are non-draft.
4. Make sure Docker is running (Testcontainers needs it).
5. Run **G-PRE-3**: pin the pre-soak E2E baseline. Save `pre-soak-e2e-baseline.txt` to `logs/`.

**Checkpoint:** `logs/` directory exists with `pre-soak-e2e-baseline.txt` populated.

---

## Step 1 — Standalone build verification (30–60 min)

Per FR-001, each source PR branch must build standalone before merging. The four can be checked in parallel via worktrees if you want speed, or sequentially via `git checkout`.

**Sequential approach (simpler, what this quickstart follows):**

1. Run **G-STD-1**: registration standalone build. **Strict mode** (coverage-check is on this branch and must pass against its own retrofit).
2. Run **G-STD-2**: lifecycle standalone build.
3. Run **G-STD-3**: audit standalone build.
4. Run **G-STD-4**: knowledge standalone build.

**Failure handling:**
- If registration fails, the bundle still proceeds with the other 3 (without the coverage-check tool entering develop). Comment on PR #13 with the failure log.
- If lifecycle / audit / knowledge fail standalone, the bundle proceeds without that branch. Comment on the failing PR.

**Checkpoint:** `logs/standalone-*.log` files for each branch that passed; failures noted in commit-friendly notes.

---

## Step 2 — Create integration branch (2 min)

```bash
git fetch origin
git checkout -b 019-integration-soak origin/develop
git push -u origin 019-integration-soak
```

Pushing to origin early is intentional: CI runs (e.g., `coverage-check.yml`) hook on push, giving you a sanity check before any merges.

**Checkpoint:** `origin/019-integration-soak` exists and matches `origin/develop`.

---

## Step 3 — Merge in size order (2–4 hours, conflict-resolution-dominated)

For each merge, follow the contract command and resolve conflicts inline. Do not revert; per FR-005, fix in place.

### 3a. Merge registration (#13) — **G-MERGE-1**

Smallest semantic surface. Should merge with minimal conflicts (cleanest pre-flight against develop).

Expected resolutions:
- `Mentoory.sln`, `Directory.Packages.props` — accept registration's union additions

### 3b. Merge lifecycle (#11) — **G-MERGE-2**

Adds `Coordination` area + project lifecycle UI.

Expected resolutions:
- `Mentoory.Web/Program.cs` — union DI registrations
- `Mentoory.Web/Infrastructure/Menu/MenuConfiguration.cs` — union menu entries; verify constitution X (GlobalAdmin in all groups)
- DACPAC: lifecycle adds `Projects.RowVersion` — clean apply, no conflict yet (knowledge comes later)

### 3c. Merge audit (#12) — **G-MERGE-3**

Adds `AuditingBehavior` MediatR pipeline + audit log viewer.

Expected resolutions:
- `Mentoory.Access.Application/Commands/RegisterUser/RegisterUserCommand.cs` — apply registration's restructure first, then re-apply audit's `[Audited]` (per spec EC-4)
- `Mentoory.Web/Program.cs` — verify audit's `AuditingBehavior` is registered early in the MediatR pipeline (typically before validators)
- `tests/Mentoory.Tests.Integration/Fixtures/IntegrationTestBase.cs` — union test setup
- **Apply EC-2:** add `[Audited(eventType: AuditEventTypes.ProjectStageAdvanced)]` to `Mentoory.Tenant.Application/Commands/AdvanceProjectStage/AdvanceProjectStageCommand.cs` (the only command across the four branches that matches audit's regex and isn't already covered).
- Verify with `dotnet test --filter FullyQualifiedName~AuditCoverageTests` immediately after this merge.

### 3d. Merge knowledge (#14) — **G-MERGE-4**

Largest delta. Adds `Mentoory.Knowledge` module + DACPAC schema + diagnostic cascade.

Expected resolutions:
- DACPAC SSDT files: hand-stitch lifecycle's `Projects.RowVersion` with knowledge's cross-schema FKs (`diagnostic.FormTemplates.DefaultKnowledgeStructureTemplateId`, `diagnostic.Questions.TopicId`). Verify with `dotnet build Mentoory.Db/MentooryDb.sqlproj`.
- `Mentoory.Tenant.Domain/Aggregates/Project/Project.cs` — union; lifecycle adds RowVersion concurrency token, knowledge does not modify
- `Mentoory.Web/Areas/Coordination/Controllers/DiagnosticsController.cs` — union routes
- `tests/Mentoory.Tests.E2E/Infrastructure/PlaywrightFixture.cs` — union helpers

**Checkpoint:** `git log --merges --oneline | head -4` shows four merge commits, each with a clean `dotnet build` log captured in `logs/merge-N-build.log`.

---

## Step 4 — Full gate (~15 min serial, can parallelize)

Run **G-FULL-BUILD** first to ensure the post-knowledge build is clean.

Then run the test phases. They can run in parallel if your machine handles it; sequential is fine for predictable logs.

| Step | Command ID | Wall-clock target |
|---|---|---|
| Build (final) | G-FULL-BUILD | 3 min |
| Unit tests | G-FULL-UNIT | < 1 min |
| Integration tests | G-FULL-INTEGRATION | 2 min |
| E2E tests | G-FULL-E2E | 4–7 min |
| DACPAC publish | G-FULL-DACPAC | 1 min + 1 min for idempotency rerun |
| Coverage-check capture | G-FULL-COVERAGE | already part of build, just locate the log |

**Flake handling (EC-5):** if any single E2E test fails, re-run that test with `dotnet test --filter FullyQualifiedName=<full.test.name>`. Two consecutive failures = real regression; jump to Step 5b.

**Checkpoint:** all logs under `logs/gate-*.log` show passing results. Coverage-check warnings recorded; do NOT treat as failures.

---

## Step 5a — Ship the bundle (green path, ~30 min including review wait)

1. Generate the bundle PR body. A working template:

   ```markdown
   ## Bundle ship: 4 feature-016 PRs as one

   This PR ships the four 016-* feature PRs as a single integration bundle. Spec at [019-integration-soak-bundle](specs/019-integration-soak-bundle/).

   ### Source PRs (closed via cross-reference at merge time, NOT separately merged)
   - #11 — Project lifecycle finish
   - #12 — Audit pipeline + browser E2E coverage
   - #13 — Registration hardening + access-security delivery quality gate
   - #14 — Knowledge module core

   ### Gate evidence (attached)
   - Build: zero warnings ([logs/gate-build.log])
   - Unit: ALL_GREEN_COUNT pass / 0 fail ([logs/gate-unit.log])
   - Integration: ALL_GREEN_COUNT pass / 0 fail ([logs/gate-integration.log])
   - E2E: PRE_SOAK_BASELINE pre-soak baseline + ΔN new = TOTAL pass / 0 fail ([logs/gate-e2e.log])
   - DACPAC publish: clean apply + idempotency rerun no-op ([logs/gate-dacpac-*.log])
   - Coverage-check: warn-mode violations report ([logs/coverage-check.log]) — N violations from lifecycle/audit/knowledge specs missing `[Trait("Spec",..)]` attributes; deferred to follow-up retrofit spec.

   ### Conflict resolutions applied during integration
   - `Mentoory.Web/Program.cs` — 3-way union (lifecycle + audit + knowledge DI registrations)
   - `Mentoory.Web/Infrastructure/Menu/MenuConfiguration.cs` — 3-way union; GlobalAdmin verified in all groups (constitution X)
   - `Mentoory.Access.Application/Commands/RegisterUser/RegisterUserCommand.cs` — registration's restructure + audit's `[Audited]` re-applied
   - `Mentoory.Tenant.Application/Commands/AdvanceProjectStage/AdvanceProjectStageCommand.cs` — `[Audited]` attribute added to satisfy `AuditCoverageTests`
   - DACPAC SSDT files — hand-stitched `Projects.RowVersion` + knowledge cross-FKs
   - (others as discovered during integration)

   ### Follow-ups
   - [ ] Retrofit `[Trait("Spec",..)("Sc",..)]` attributes on lifecycle / audit / knowledge tests so a future bundle can run coverage-check in strict mode (tracked in TODO spec).
   - [ ] Manual quickstart walkthroughs per source PR (deferred per spec out-of-scope; see each PR for the list).
   ```

2. Run **G-SHIP-1** + **G-SHIP-2**: push integration branch and open the PR.
3. Reviewer signs off (per repo policy).
4. Run **G-SHIP-3**: merge the bundle PR (default `--merge`; switch to `--squash` if OQ-1 flipped).
5. Run **G-SHIP-4**: close source PRs with cross-reference comment.
6. Run **G-SHIP-5**: delete integration branch.

**Done.** Develop now contains all four features.

---

## Step 5b — Bisect on gate failure (red path, +30–60 min)

If any G-FULL-* step failed, follow **G-BISECT-1..4** (revert in reverse order, re-run the failing step, repeat).

**Outcome decision tree:**

- **Bisect identifies one regression-owning branch:** ship the partial bundle (skip Step 5a's source PR closure for the regression branch; comment on its PR with the bisect evidence and leave it open for follow-up).
- **Bisect identifies two regression-owning branches:** ship the 2-branch partial bundle if green; close 2 source PRs with cross-reference; leave the regression-owning 2 PRs open.
- **All four reverted, still red:** the regression is on develop itself or in the bundle's conflict-resolution work. Abort the bundle (`git push origin --delete 019-integration-soak`); investigate develop separately.

---

## Step 6 — Post-soak cleanup (5 min)

After the bundle ships:

1. Verify `develop` has the expected merge structure: `git log --oneline -5` shows the bundle merge commit pointing at the four-branch history.
2. Verify all four `specs/{NNN}-*/` directories are present on `develop`: `git ls-tree develop -- specs/ | grep -E "016-|017-audit-e2e|018-access-security"`.
3. Update the brainstorm overview: edit `brainstorm/00-overview.md` to mark session #11 complete with the bundle PR link.
4. Schedule the follow-up retrofit spec (see Step 5a's Follow-ups section).

---

## Soak-extended path (NFR-001 / EC-7)

If you exceed 24 hours of wall-clock time on Step 3 or Step 4 (conflict resolution took longer, gate flakes required investigation, etc.):

1. `git fetch origin` — pick up any new develop commits.
2. `git rebase origin/develop` on the integration branch. Resolve any new conflicts.
3. Re-run **G-FULL-*** end-to-end. Capture fresh logs (don't reuse the stale ones — they no longer reflect the integration branch state).
4. Note the rebase + re-gate cycle in the bundle PR body as ship justification.

---

## Time budget reference

| Phase | Estimate |
|---|---|
| Step 0 pre-flight | 10 min |
| Step 1 standalone builds (sequential, 4 branches) | 30–60 min |
| Step 2 create integration branch | 2 min |
| Step 3 four merges with conflict resolution | 2–4 hours |
| Step 4 full gate | 15 min serial |
| Step 5a ship | 30 min (incl. review wait) |
| Step 5b bisect (only if red) | 30–60 min per iteration, max 4 iterations |
| Step 6 cleanup | 5 min |
| **Total green path** | **3.5–6 hours** |
| **Total red path** | **5.5–10 hours** |

Both fit comfortably inside the 1-working-day NFR-001 target. The 24-hour wall-clock trigger for rebase + re-gate is for genuine multi-day soaks, not the normal happy path.
