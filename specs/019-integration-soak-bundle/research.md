# Phase 0 Research — Integration Soak Bundle

**Date:** 2026-04-25
**Spec:** [spec.md](./spec.md)

This document resolves the unknowns and dependencies that the spec deferred. The findings inform Phase 1 design (data-model, contracts, quickstart) and the bundle's execution playbook.

---

## R1 — Coverage-check tool implementation status

**Decision:** Coverage-check IS shipped on the registration branch. The bundle's gate runs it in **warn mode** (`-p:CoverageCheckMode=warn`).

**Rationale:**
- Verified the production project exists at `tools/Mentoory.Specs.CoverageCheck/` on `origin/016-registration-access-hardening`. Files present:
  - `Program.cs` — full CLI entry point using System.CommandLine with subcommands and exit codes
  - `Coverage/CoverageAnalyzer.cs` — 215-line analyzer
  - `Coverage/CoverageReport.cs`, `Coverage/FloorCategories.cs`
  - `Output/JsonReportWriter.cs`, `Output/TextReportWriter.cs`
  - `Parsing/SpecModels.cs`, `Parsing/SpecParser.cs`
  - `Reflection/TestClaimModels.cs`, `Reflection/TraitReflector.cs`
  - `build/CoverageCheck.targets` — MSBuild integration
- The `CoverageCheck.targets` file declares `Target Name="CheckSpecCoverage" AfterTargets="Build" Condition="'$(MSBuildProjectName)' == 'Mentoory.Specs.CoverageCheck' AND '$(SkipCoverageCheck)' != 'true'"`. This means **every solution-level `dotnet build` automatically runs coverage-check after the tool's own build completes** — there is no opt-in.
- Two escape hatches are documented in the targets file:
  - `-p:SkipCoverageCheck=true` — skip entirely (build does not invoke the tool)
  - `-p:CoverageCheckMode=warn` (or `MENTOORY_COVERAGECHECK_MODE=warn` env var) — invoke the tool with `--mode warn`, which downgrades violations to warnings instead of failing the build
- Why warn mode for the bundle: the lifecycle, audit, and knowledge specs introduce SC-### / FR-### identifiers in their `spec.md` files. Coverage-check expects each identifier to be claimed by a test method's `[Trait("Spec","NNN-spec-slug")] [Trait("Sc","SC-001")]` attributes. Lifecycle, audit, and knowledge tests have not been retrofitted with these traits (only registration retrofitted itself for spec 016-registration-access-hardening). Strict enforcement would fail the bundle on dangling-trait / unclaimed-identifier violations whose remediation is not in the bundle's scope.
- Strict enforcement on registration's own standalone build (FR-001 verification) is acceptable — registration claims to ship its own retrofit (per its PR body: "016 retrofit + seven new automated scenarios").

**Alternatives considered:**
- **Use `-p:SkipCoverageCheck=true` instead of warn mode.** Rejected — warn mode produces a violations log that becomes valuable evidence for the follow-up retrofit work; skipping produces nothing.
- **Backport `[Trait]` retrofits during the bundle.** Rejected — the spec FR-003 freezes source branches; doing the retrofit in the bundle violates the freeze and would touch ~50+ test files across three branches' test suites. Defeats the soak amortization.
- **Hold the bundle until full retrofit lands.** Rejected — adds ~50+ tasks of work for a non-functional concern; the four features themselves are ready and shipping them is the priority.

---

## R2 — Per-branch standalone build state

**Decision:** All four branches have already been verified by `git merge-tree` to merge cleanly into `develop`. **Standalone build status must be verified at execution time** (FR-001) — research did not run actual builds, only static analysis.

**Findings from static analysis:**

| Branch | Commits ahead | LOC delta | Build risk |
|---|---|---|---|
| #11 lifecycle (016-project-lifecycle-finish) | 15 | +7,226 / -8 | Low — mature feature, audit PR 5/268 unit tests passing per its own PR body |
| #12 audit (016-audit-pipeline) | 13 | +8,337 / -101 | Low — full unit + integration + E2E suites green per PR body (121/121 E2E baseline) |
| #13 registration (016-registration-access-hardening) | 9 | +9,469 / -216 | Medium — runs coverage-check on its own build (strict mode); retrofit must pass; if not, exclude per FR-001 |
| #14 knowledge (016-knowledge-module-core) | 19 | +22,716 / -196 | Medium — largest delta, 313 changed files; PR notes 100/102 unit tests pass with 2 skips |

**Test counts per branch (file-level):**

| Branch | E2E test files (modified or added) |
|---|---|
| #11 lifecycle | 29 |
| #12 audit | 27 |
| #13 registration | 23 |
| #14 knowledge | 29 |

(File counts include modifications to existing files; net new test files are smaller — see R5 for the breakdown.)

**Rationale:** Standalone build must succeed before any merge to keep FR-001's pre-flight check meaningful. Static analysis is not a substitute for an actual `dotnet build` against each tip — that runs as task T001/T002 of the implementation.

---

## R3 — File-level conflict surface

**Decision:** Pairwise file overlap is captured in the table below. Three-way conflicts are limited to the four named files.

**Pairwise overlap (textual):**

| Pair | Overlapping files |
|---|---|
| Audit ↔ Registration | `RegisterUserCommand.cs`, `IntegrationTestBase.cs`, `Mentoory.sln`, `access-security-constitution.md`, `Mentoory.Access.Application/DependencyInjection.cs` |
| Audit ↔ Knowledge | `Program.cs`, `MenuConfiguration.cs`, `PlaywrightFixture.cs`, `IntegrationTestBase.cs`, `MentooryWebApplicationFactory.cs`, `AuthorizationTests.cs` |
| Audit ↔ Lifecycle | `Program.cs`, `MenuConfiguration.cs` |
| Lifecycle ↔ Knowledge | `Project.cs`, `TenantDbContext.cs`, `Projects.sql`, `004.SeedTestData.sql`, `DiagnosticsController.cs`, `Program.cs`, `MenuConfiguration.cs`, `Directory.Packages.props`, `ProjectTests.cs` |
| Lifecycle ↔ Registration | `Mentoory.Access.Application.csproj`, `Directory.Packages.props` |
| Knowledge ↔ Registration | `Directory.Packages.props`, `RegistrationTests.cs`, `ContextSelectionTests.cs`, `ContextSwitchingTests.cs`, `AdministrationUsersTests.cs` |

**Three-way conflict files** (touched by ≥3 branches):

| File | Branches touching | Resolution rule |
|---|---|---|
| `Mentoory.Web/Program.cs` | lifecycle + audit + knowledge | Union DI registrations; verify ordering preserves audit's `AuditingBehavior` pipeline position (must be early in MediatR pipeline) |
| `Mentoory.Web/Infrastructure/Menu/MenuConfiguration.cs` | lifecycle + audit + knowledge | Union menu entries; constitution X requires GlobalAdmin in all groups — verify per group |
| `tests/Mentoory.Tests.Integration/Fixtures/IntegrationTestBase.cs` | audit + registration + knowledge | Union test fixture setup (DI overrides, schema respawn config) |
| `tests/Mentoory.Tests.E2E/Infrastructure/PlaywrightFixture.cs` | audit + knowledge | (2-way only, listed for completeness) Union helper extensions |
| `Directory.Packages.props` | lifecycle + knowledge + registration | Union NuGet versions; if version mismatch, take the highest |
| `tests/Mentoory.Tests.Integration/Fixtures/MentooryWebApplicationFactory.cs` | audit + knowledge | Union service overrides |

**`RegisterUserCommand.cs` semantic conflict** (audit ↔ registration only): registration restructures the command (extracts `IUserProvisioningService`, splits into `RegisterUser` + `AdminEnrollUser`); audit decorates with `[Audited]`. Resolution sequence:
1. Apply registration's restructure first (it's the structural change)
2. Re-apply audit's `[Audited]` attribute on the post-restructure `RegisterUserCommand`
3. Inspect `AdminEnrollUserCommand` — its name does NOT match audit's regex `^(Assign|Approve|Correct|Advance|Login|Register|SetActive).*Command$` because it starts with `Admin`. So no `[Audited]` required by `AuditCoverageTests`. **However**, admin enrollment IS a security-sensitive action — recommend manually adding `[Audited]` even though the regex doesn't require it.

**Rationale:** Conflict-resolution rules baked into the spec's Edge Cases. This research formalizes them per file with constitution cross-references where relevant.

**Alternatives considered:**
- **Pre-rebase the source branches against each other** before merging to integration. Rejected — the rebasing cost is precisely what the bundle tactic is designed to avoid.
- **Accept the 3-way conflict files breaking and resolve later.** Rejected — FR-005 mandates each merge step to result in a clean build before the next merge proceeds.

---

## R4 — Audit `[Audited]` coverage gap on lifecycle/knowledge

**Decision:** `lifecycle/AdvanceProjectStageCommand` is the only command across the four branches that matches audit's regex `^(Assign|Approve|Correct|Advance|Login|Register|SetActive).*Command$` and is NOT already covered by audit's own retrofit.

**Findings:**

| Branch | New commands matching the regex | Already audited? |
|---|---|---|
| Registration | `RegisterUserCommand` (modified, not new) | Yes — audit branch decorates the upstream version of this command |
| Lifecycle | `AdvanceProjectStageCommand` | **No — needs `[Audited]` added during integration** |
| Audit | `AssignMentor`, `AssignRole`, `Correct`, `Login`, `RegisterInternal`, `Register`, `SetActive` | Yes — audit branch decorates all 7 |
| Knowledge | (none match the regex) | N/A |

**Resolution during integration:**
- Add `[Audited(eventType: AuditEventTypes.ProjectStageAdvanced)]` (or equivalent — verify the constant name from audit's `AuditEventTypes.cs`) to `AdvanceProjectStageCommand` after audit branch merges in step 3.
- Verify by running `dotnet test --filter FullyQualifiedName~AuditCoverageTests` after step 3 (audit) and step 4 (knowledge); the test asserts every regex-matching command has the attribute.

**Rationale:** Audit's `AuditCoverageTests` is an architecture test that walks all command types via reflection and checks for the attribute. If `AdvanceProjectStageCommand` lacks it, the test fails, which fails the gate. This research pre-identifies the only such command so the integration step can apply the attribute as part of the merge resolution rather than discovering it as a gate failure.

**Alternatives considered:**
- **Add `[Audited]` to `AdminEnrollUserCommand` defensively** (does not match regex but is security-sensitive). Recommended but not required by `AuditCoverageTests`. Decision deferred to integration step — the release manager can add it if time permits; otherwise it's a follow-up.

---

## R5 — DACPAC merge strategy

**Decision:** Hand-stitch the SSDT files at the conflict resolution step. Both lifecycle's `Projects.RowVersion` column and knowledge's `knowledge.*` schema land in the integration branch; `004.SeedTestData.sql` (modified by lifecycle) and `005.SeedKnowledgeData.sql` (added by knowledge) coexist at numeric ordering 004 → 005. Verify by building `Mentoory.Db/MentooryDb.sqlproj`.

**Rationale:**
- Lifecycle adds `RowVersion ROWVERSION NOT NULL` to `Projects.sql` for optimistic concurrency on `AdvanceProjectStageCommand`.
- Knowledge adds the `knowledge.*` schema (10 tables: KnowledgeStructureTemplates, Modules, Topics, Subjects, Resources × 2 for templates + project clones), plus cross-schema FKs to `diagnostic.FormTemplates.DefaultKnowledgeStructureTemplateId` (new column) and `diagnostic.Questions.TopicId` (new column with retro-fit seed data in `005.SeedKnowledgeData.sql`).
- The two changes are orthogonal at the schema level (different tables, different schemas). The conflict surface is in:
  - `Mentoory.Db/MentooryDb.sqlproj` — both branches add SqlBuild entries; union merge
  - `Mentoory.Db.PostDeployment/004.SeedTestData.sql` — lifecycle adds `Projects.RowVersion` seed, knowledge does not modify
  - `Mentoory.Db.PostDeployment/005.SeedKnowledgeData.sql` — new file from knowledge only, no conflict
- Idempotency check (constitution XI): both seed scripts use `IF EXISTS` / `MERGE` patterns or upserts. Verify each by running `sqlcmd` twice — second run must be a no-op.

**Verification command** (run after step 4 / knowledge merge):
```bash
dotnet build Mentoory.Db/MentooryDb.sqlproj -p:NuGetAudit=false
```

**Alternatives considered:**
- **Use SQL Compare to merge SSDT projects.** Rejected — overkill for orthogonal schema additions; hand-stitch is faster and more transparent in PR review.

---

## R6 — Test count baseline and pinning

**Decision:** Pin pre-soak `develop` E2E count at execution time via `dotnet test tests/Mentoory.Tests.E2E/ --list-tests` against `origin/develop` HEAD (which is `0ef00fa` per the brainstorm seed; verify at execution start). Use this as the baseline for NFR-003's "every E2E test that passed on `origin/develop` immediately before the soak began must still pass on the integration branch".

**Findings from static analysis (file counts):**

| Test surface | E2E test files (estimated additions) |
|---|---|
| Pre-soak develop baseline | (TBD — pin at execution) |
| Lifecycle adds | 6 new files in `tests/Mentoory.Tests.E2E/Tests/Lifecycle/` (LifecycleSmokeTests, WalkthroughAdvanceTests, WalkthroughAuditConcurrencyTests, WalkthroughGatedActionsTests, WalkthroughLifecyclePageTests, WalkthroughRoleScopeTests) |
| Audit adds | 4 new files in `tests/Mentoory.Tests.E2E/Tests/` (AuditLogViewerTests with 9 tests, AuditLogCaptureTests with 7, AuditLogCorrelationTests with 3, AuditLogRegressionTests with 5; total 24 tests per audit PR body) |
| Knowledge adds | 6 new files in `tests/Mentoory.Tests.E2E/Tests/` (KnowledgeAuthorizationTests, KnowledgeFormCloneCascadeTests, KnowledgePartialSyncTests, KnowledgeProjectStructureTests, KnowledgeProjectTreeEditingTests, KnowledgeTemplatesTests) |
| Registration adds | 0 new files; modifies 4 existing files (RegistrationTests, AdministrationUsersTests, ContextSelectionTests, ContextSwitchingTests) |

**Estimated post-bundle E2E count:** baseline + 24 (audit) + ~20 (lifecycle, ~3 tests/file × 6 files) + ~30 (knowledge, ~5 tests/file × 6 files) ≈ baseline + 70–80 tests. The audit PR baseline of 121/121 includes audit's 24 new tests, so develop's pre-soak count (without audit) is 97. Post-bundle expectation: 97 + 24 + ~20 + ~30 = ~171 tests minimum.

**Rationale:** Spec NFR-003 invariant is relative ("strictly greater than pre-soak") so an exact count isn't required for ship — only the invariant. Pinning the pre-soak number is a quality-of-evidence concern: it lets the bundle PR body cite "from N to M, +ΔN E2E tests, all green".

---

## R7 — gh CLI authentication and PR operations

**Decision:** Use `gh` CLI for opening the bundle PR and closing source PRs with cross-reference comments. Authentication is assumed (per spec assumption — verify at execution start with `gh auth status`).

**PR operations sequence:**
1. Open bundle PR: `gh pr create --base develop --head 019-integration-soak --title "Bundle: 016 ship — registration + lifecycle + audit + knowledge" --body-file bundle-pr-body.md`
2. Close source PRs with cross-reference (after bundle merges, NOT before):
   ```bash
   for pr in 11 12 13 14; do
     gh pr close $pr --comment "Closed in favor of bundle ship #$BUNDLE_PR. The four 016-* PRs were merged together via the integration soak (specs/019-integration-soak-bundle/) to amortize E2E test cost. See bundle PR for full ship evidence."
   done
   ```

**Rationale:** `gh pr close` (as opposed to `gh pr merge`) marks the PR as closed without merging it, which preserves the PR's review history without producing an empty merge commit. The cross-reference comment makes the bundle traceable from each source PR.

**Alternatives considered:**
- **Use `gh pr merge --squash`** on each source PR. Rejected — would create duplicate commits on develop (the source PRs' commits are already in the bundle merge).

---

## R8 — Integration branch lifetime and cleanup

**Decision:** `019-integration-soak` is deleted from `origin` after the bundle PR merges. The local branch may be kept for forensic reference but should be pruned within 1 week.

**Cleanup commands:**
```bash
# After bundle PR merges to develop:
git push origin --delete 019-integration-soak
# Optional local cleanup after a soak/cool-off period:
git branch -D 019-integration-soak
```

**Rationale:** The integration branch's purpose is amortizing the gate run. Once shipped, its history is preserved by the merge commit on `develop` (per FR-007's "regular merge commit preserves four-branch history"). Keeping the branch indefinitely clutters the remote.

The spec branch (`019-integration-soak-bundle`) is separate — it stays until its own PR merges spec/plan/tasks/research/quickstart into develop.

---

## Open Items Carried Forward

These remain undecided after Phase 0 research and feed into Phase 1 design or are deferred to execution time:

- **OQ-1** (from spec): Bundle PR merge style — regular merge commit vs. squash. Defer to PR open time.
- **OQ-2** (from spec): Bundle PR body — verbatim source PR bodies vs. cross-reference. Defer to PR open time.
- **Pre-soak E2E baseline pin** (R6): Run `dotnet test --list-tests` against `origin/develop` at execution start.
- **Coverage-check warn-mode violation triage** (R1): Capture the violation log; if any violations are surprisingly small in scope, the release manager may elect to fix them in the bundle as a one-off (overriding the "frozen branches" rule for trait additions only). Defer to execution.
