# Spec Review: Integration Soak Bundle (016 phase wrap-up)

**Spec:** specs/019-integration-soak-bundle/spec.md
**Date:** 2026-04-25
**Reviewer:** Claude (spex:review-spec)

## Overall Assessment

**Status:** ⚠️ NEEDS WORK

**Summary:** The spec is structurally complete, well-scoped, and actionable for an operational/release-tactic spec. However, one assumption baked into FR-005, NFR-002, and SC-002 is **factually incorrect**: the coverage-check tool's *implementation* is not on the registration branch (only tests, spec, and CI workflow). The bundle gate cannot run coverage-check as written. One important issue (FR-005 step ordering) and a few clarity nits also need addressing before planning.

## Completeness: 4.5 / 5

### Structure
- ✓ All required sections present (Purpose via Input/title, FRs, NFRs, SC, Edge Cases, Out of Scope, Assumptions, Open Questions)
- ✓ User Scenarios with three priority-ranked stories, each with acceptance scenarios
- ✓ Key Entities documented
- ✓ No placeholder text remaining

### Coverage
- ✓ Functional requirements: 9 FRs, all numbered and specific
- ✓ Non-functional: 5 NFRs covering duration, gating, schema, regression, reproducibility
- ✓ Error cases: 7 edge cases with explicit resolutions
- ✓ Success criteria: 6 SCs, all measurable

**No completeness issues.**

## Clarity: 4 / 5

### Language Quality
- ✓ Most requirements are concrete and specific
- ✓ Quantitative bounds where available (≥ 121 E2E, zero warnings, ≤ 1 working day, ≤ 4 bisect iterations)
- ⚠️ Two minor ambiguities

**Ambiguities Found:**

1. **FR-005 ordering**: "After all four are merged and building, the full gate runs in this order: all unit tests, `Mentoory.Tests.Integration` (Testcontainers SQL), `Mentoory.Tests.E2E` (Playwright), DACPAC publish against an integration DB, and the coverage-check tool"
   - Issue: Reads as a strict sequence, but DACPAC publish against an *integration DB* and Testcontainers integration tests (which spin up their own SQL) are independent. The ordering is unclear and may unnecessarily serialize parallelizable steps.
   - Suggestion: Either drop the implied ordering ("the full gate covers: …") or clarify that `dotnet test` covers unit + integration in one invocation, and DACPAC publish is a separate step that can run in parallel.

2. **SC-003 "same business day"**: ambiguous timezone — project is Spanish-speaking market (Latin America time?). Minor but verifiable measurement requires it.
   - Suggestion: Either specify timezone (e.g., "America/Bogotá business day") or drop "business day" and use a wall-clock bound (e.g., "within 6 hours of the bundle merge").

3. **NFR-004 "≥ 121 + knowledge's E2E (if any new ones land there)"**: the conditional "if any new ones" is fine but verifiable only by inspection. Knowledge PR description lists 100 unit tests + 2 skipped + 5 new diagnostic cascade unit tests; no E2E mentioned. Worth verifying and pinning the number.
   - Suggestion: Inspect knowledge branch's `tests/Mentoory.Tests.E2E/` for new test files; if zero, change to "≥ 121" exact; if N new, "≥ 121 + N".

## Implementability: 3 / 5

### Plan Generation
- ✓ Operational steps are unambiguous (create branch, merge, build, gate, ship/bisect)
- ✓ Conflict-resolution playbook is concrete (per-file resolutions named in Edge Cases)
- ✗ **Critical**: The gate as defined depends on a tool that doesn't exist
- ⚠️ Constraint realism: 1 working day for a 4-way merge of 47k LOC across 16 files with 3-way conflict points may be aggressive

**Critical issue:**

**The coverage-check tool implementation is not on the registration branch.** Verified by inspection:

```
git ls-tree -r origin/016-registration-access-hardening
```

shows:
- ✓ `tests/Mentoory.Specs.CoverageCheck.Tests/` (test project + fixtures)
- ✓ `specs/018-access-security-delivery-quality-gate/` (spec, plan, tasks)
- ✓ `.github/workflows/coverage-check.yml` (CI workflow)
- ✗ `Mentoory.Specs.CoverageCheck/` — does NOT exist (production project)

The registration PR description (#13) confirms: "Feature 018 — Access-security delivery quality gate (planned, **implementation pending** — 54 tasks)". Phase 1 task T001 is "scaffold coverage-tool project" — not yet done.

**Impact**:
- FR-005 references coverage-check as a gate step → not runnable
- NFR-002 sets a coverage-check pass criterion → cannot be met
- SC-002 makes coverage-check part of the success criterion → cannot be verified
- Possibly worse: the test project `Mentoory.Specs.CoverageCheck.Tests/` may fail to compile (it likely references the non-existent production project), which would break the gate's `dotnet build` step on the bundle.

**Resolution options**:
- **A (recommended)**: Remove coverage-check from the gate. Move it to "Out of Scope" with a follow-up bundle planned once feature 018 phases T001–T054 implementation lands. Keep the spec/tests/CI workflow in the bundle as forward-looking artifacts but don't gate on them.
- **B**: Expand the bundle scope to include feature 018 implementation (54 tasks). This roughly doubles the effort and contradicts the "frozen branches" principle (FR-003).

### Other implementability concerns
- The 1-working-day NFR-001 is a target; given the conflict surface (16+ overlapping files including 3-way conflicts on `Program.cs`, `MenuConfiguration.cs`, `IntegrationTestBase.cs`, `PlaywrightFixture.cs`), 1.5–2 days is more realistic. The rebase escape hatch (NFR-001 second sentence) covers this, but consider softening the NFR or rephrasing as "soft target".
- DACPAC publish target requires either a live SQL Server or Aspire to bring one up — Assumptions list this but execution will need to confirm the actual infrastructure path before starting.

## Testability: 4.5 / 5

### Verification
- ✓ SC-001 to SC-006 are all measurable (no leftover conflict markers, gate counts, single merge commit, presence of spec dirs, soak duration, bisect iterations)
- ✓ FRs are testable as a sequence (each step has a verifiable outcome)
- ✓ Edge cases have explicit pass/fail conditions
- ⚠️ SC-002 measurability hinges on coverage-check existing (see Implementability above)

**Issues:**
- SC-002's coverage-check clause is unverifiable as written. Must be removed or reworded to pin the gate to existing test suites only.

## Constitution Alignment

Reviewed against `.specify/memory/constitution.md` v1.1.1:

- ✓ **V. Zero-Warnings Policy** — SC-001 explicitly enforces `TreatWarningsAsErrors=true`
- ✓ **IX. Spanish-First UI** — N/A (no UI changes; the spec is operational)
- ✓ **X. Role Hierarchy & Session Context** — N/A (no security changes)
- ✓ **XI. SSDT/DACPAC Database Strategy** — NFR-003 covers DACPAC publish + cross-schema FK chain + idempotent seed scripts
- ✓ Forbidden patterns (AutoMapper, Dapper for primary access, etc.) — N/A (no new code)

No constitution violations.

## Recommendations

### Critical (Must Fix Before Implementation)

- [ ] **Remove coverage-check from the gate.** Update FR-005 (drop "and the coverage-check tool from feature 018"), NFR-002 (delete entirely or move to a "Future" section), and SC-002 (remove the coverage-check clause). Add an explicit Out-of-Scope bullet: "Coverage-check enforcement (feature 018) — implementation pending; will gate a future bundle."
- [ ] **Verify the registration branch builds standalone.** Before relying on the registration branch as the smallest-first merge, confirm `dotnet build` on `origin/016-registration-access-hardening` succeeds (zero warnings) — the new `Mentoory.Specs.CoverageCheck.Tests/` project may break the build if it references a non-existent production project. If it does, the spec needs a remediation step (exclude the test project from the solution, or stub the production project).

### Important (Should Fix)

- [ ] **Clarify FR-005 step ordering** — drop the implied sequence or split into "build" / "test" / "DACPAC publish" parallel-eligible groups.
- [ ] **Pin SC-003 timezone** — replace "same business day" with a concrete bound (e.g., "within 6 hours" or "America/Bogotá business day").
- [ ] **Pin NFR-004 E2E count** — verify knowledge branch's E2E test count and replace "≥ 121 + knowledge's E2E (if any new ones land there)" with the exact number.
- [ ] **Soften NFR-001 framing** — phrase 1 working day as a soft target, with the rebase + re-gate cycle as the explicit overrun protocol (already partially there).

### Optional (Nice to Have)

- [ ] **Specify bundle PR title format** — e.g., "Bundle: 016 ship — registration + lifecycle + audit + knowledge" — for consistency.
- [ ] **Document branch-name disambiguation rule** — if `019-integration-soak` and `019-integration-soak-bundle` cause confusion in tooling/automation, rename the integration branch to `integration-soak-bundle-exec` or similar. Currently spec just notes they coexist briefly.

## Conclusion

The spec is well-structured and operationally sound for the bundle-merge tactic. The blocker is a single factual error: the coverage-check tool isn't built yet, so it cannot be part of the gate. Once that's removed (and the registration branch is verified to build standalone), the spec is ready for planning.

**Ready for implementation:** No — fix the critical issue first.

**Next steps:**
1. Apply the two critical fixes (drop coverage-check, verify registration build)
2. Apply the important fixes (FR-005 ordering, SC-003 timezone, NFR-004 E2E count, NFR-001 framing)
3. Re-run `spex:review-spec` to confirm the gate is clean
4. Then proceed to `/speckit-plan`

---

## Re-Review (2026-04-25, after inline fixes)

**Status:** ✅ SOUND

**Changes applied:**

| Issue | Resolution | Status |
|---|---|---|
| Coverage-check tool not implemented | Removed from FR-006, NFR list, SC-002, Key Entities, Open Questions; added explicit Out-of-Scope bullet citing the gap | ✅ |
| Registration build verification missing | Added new FR-001 (standalone build verification) + companion assumption + SC-001 reference to it | ✅ |
| FR-005 strict ordering | Reworded as "the full gate covers" with explicit allowance for parallelism | ✅ |
| SC-003 timezone | Replaced "same business day" with "within 6 hours" | ✅ |
| NFR-004 E2E count | Replaced fixed count with relative invariant (NFR-003: pre-soak baseline preserved + new tests pass + total > pre-soak) and pinned the new-test surface (lifecycle ~6 files, audit 24 tests / 4 files, knowledge ~6 files) by inspection | ✅ |
| NFR-001 framing | Reframed as "soft target" with explicit 24h-wall-clock trigger for the rebase + re-gate cycle | ✅ |
| FR renumbering side effects | Updated User Story 1 acceptance scenarios; corrected OQ-1's FR cross-reference to FR-007; removed obsolete OQ-2; renumbered OQ-3 to OQ-2 | ✅ |

**Outstanding open questions** (do not block planning):
- OQ-1: Merge commit vs. squash for bundle PR (decision before merging the bundle PR, not before planning)
- OQ-2: PR body verbatim vs. cross-reference (decision before opening the bundle PR)

**Remaining concerns:** None blocking. The spec is now factually accurate, internally consistent, and ready for planning.

**Ready for implementation:** Yes — proceed to `/speckit-plan`.

---

## Correction (2026-04-25, during Phase 0 planning research)

**My earlier "critical" finding about the coverage-check tool was WRONG.** The tool exists on the registration branch at `tools/Mentoory.Specs.CoverageCheck/` (I had searched at repo root, missing the `tools/` subdirectory):

- `tools/Mentoory.Specs.CoverageCheck/Program.cs` — full CLI entry point using System.CommandLine
- `tools/Mentoory.Specs.CoverageCheck/Coverage/CoverageAnalyzer.cs` — 215 lines of analysis logic
- `tools/Mentoory.Specs.CoverageCheck/build/CoverageCheck.targets` — MSBuild target wired to `AfterTargets="Build"` on the tool's own project; runs the gate automatically on every solution build
- `.github/workflows/coverage-check.yml` — CI workflow

The PR description on registration ("implementation pending — 54 tasks") is outdated; the implementation has landed.

**New finding from planning research:** The tool fires on every solution-level `dotnet build`. Without an escape hatch, the bundle's gate would fail on dangling-trait / unclaimed-identifier violations from the lifecycle, audit, and knowledge specs (whose tests have not been retrofitted with `[Trait("Spec",..)("Sc",..)]` attributes). The tool documents two escape hatches:
- `-p:SkipCoverageCheck=true` — skip entirely
- `-p:CoverageCheckMode=warn` — downgrade violations to warnings

**Spec correction applied (2026-04-25):**
- FR-001 — registration's standalone build runs in strict coverage-check mode; the others don't have the tool
- FR-005 — incremental build checks run with `-p:CoverageCheckMode=warn` from the moment registration is merged onward
- FR-006 — gate explicitly includes coverage-check in warn mode; violations recorded as evidence, do NOT block bundle
- Out-of-Scope bullet rewritten — strict enforcement is what's deferred, not the tool itself
- Key Entities (Gate evidence) — adds coverage-check violations report to the captured artifacts

**Status after correction:** still ✅ SOUND — the operational tactic is the same; only the gate's coverage-check handling is updated.
