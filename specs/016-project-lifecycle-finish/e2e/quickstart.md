# Quickstart: Executing the E2E Lifecycle Suite

**Feature**: `016-project-lifecycle-finish/e2e`
**Date**: 2026-04-19

This doc tells a human operator (or a fresh AI session invoked via a checkpoint prompt) how to run, debug, and extend the suite.

---

## Prerequisites

One-time setup:

1. **Docker daemon running.** `docker info` must respond. Testcontainers.MsSql launches a SQL Server 2022 container per test run.
2. **.NET 10 SDK installed.** `dotnet --version` returns 10.0.x.
3. **Playwright browsers installed.** The `Mentoory.Tests.E2E` project has a `Microsoft.Playwright.MSBuild` target that installs Chromium on first build. If Chromium is missing, `dotnet build tests/Mentoory.Tests.E2E/Mentoory.Tests.E2E.csproj /p:NuGetAudit=false` restores it.
4. **DACPAC built.** `Mentoory.Db/bin/Debug/MentooryDb.dacpac` must exist. If missing: `cd Mentoory.Db && ./publish-mentoorydb.sh`.

Per-chunk setup (listed in each `checkpoints/CN-*.md` pre-flight): these prerequisites are re-checked at the start of every session.

---

## Running the suite

**All E2E tests (includes 97 baseline + new Lifecycle tests):**

```bash
dotnet test /mnt/D/repos/mentoory-ps-lifecycle-finish/tests/Mentoory.Tests.E2E/Mentoory.Tests.E2E.csproj \
    /p:NuGetAudit=false
```

**Only the Lifecycle tests:**

```bash
dotnet test /mnt/D/repos/mentoory-ps-lifecycle-finish/tests/Mentoory.Tests.E2E/Mentoory.Tests.E2E.csproj \
    --filter "FullyQualifiedName~Mentoory.Tests.E2E.Tests.Lifecycle" \
    /p:NuGetAudit=false
```

**A single walkthrough's tests (e.g., C3):**

```bash
dotnet test /mnt/D/repos/mentoory-ps-lifecycle-finish/tests/Mentoory.Tests.E2E/Mentoory.Tests.E2E.csproj \
    --filter "FullyQualifiedName~WalkthroughGatedActionsTests" \
    /p:NuGetAudit=false
```

**Full solution (every test project):**

```bash
dotnet test /mnt/D/repos/mentoory-ps-lifecycle-finish/Mentoory.sln /p:NuGetAudit=false
```

Full-solution run is the authoritative SC-E2 gate. Each chunk's exit gate mandates a full-solution green.

---

## Executing a chunk in a fresh AI session

The unit of work is one chunk = one session. Sequence:

1. **Confirm the previous chunk landed.** `git log --oneline -6` on `016-project-lifecycle-finish` should list the expected prior-chunk commit subjects (see the chunk's "Prior state" section).
2. **Start a fresh Claude Code session.** `/clear`, or open a new terminal.
3. **Paste the full content** of the chunk's resume prompt (e.g., `specs/016-project-lifecycle-finish/e2e/checkpoints/01-walkthrough-1-advance.md`) as the first message. The prompt is self-contained.
4. **The session runs pre-flight** (commands listed inside the prompt). Any mismatch surfaces and stops.
5. **The session writes + runs tests**, iterates until green, applies `/simplify`, runs full suite once more.
6. **The session commits** with the prompt's commit-message template and pushes.
7. **The session ends.** Next session picks up with the next chunk file.

**Do not run two chunks in the same session.** The value of the protocol is fresh context per chunk.

---

## Interpreting common failures

| Failure | Likely cause | Fix |
|---|---|---|
| `MentooryDb.dacpac not found` | Stale or missing DACPAC build | `cd Mentoory.Db && ./publish-mentoorydb.sh` |
| `Docker daemon not responding` | Docker stopped or restarted | Start Docker, retry |
| `KeyNotFoundException` during seed | Domain invariant bypassed by raw SQL | Seed via `LifecycleFixtures.CreateProjectAsync`, not raw SQL |
| Flaky first test in a run | Testcontainers warmup + DACPAC deploy (~15 s one-time) | Expected; subsequent tests share the container |
| Spanish-literal assertion fails | Product UI string changed | STOP per E6. Don't edit the test — surface. The spec's D5 says strings are stable; a divergence is noteworthy. |
| `Microsoft.Build.Sql` version warning | Pre-existing SDK-version hint on `Mentoory.Db` | Ignore — tolerated by SC-E2. Any OTHER warning stops the chunk. |
| `DbUpdateConcurrencyException` in tests other than C5 row #16 | A test leaked state into another | Reset logic failed. Check `LifecycleFixtures.ResetStateAsync` ran in the test's constructor. |
| Test passes locally, fails in CI | Playwright trace not captured | Run with `PWDEBUG=1 dotnet test ...` locally to get a trace viewer |

---

## Debugging a single test

Playwright supports step-through:

```bash
PWDEBUG=1 dotnet test /mnt/D/repos/mentoory-ps-lifecycle-finish/tests/Mentoory.Tests.E2E/Mentoory.Tests.E2E.csproj \
    --filter "FullyQualifiedName~WalkthroughAdvanceTests.Coordinator_AdvancesProjectFromRegistrationToForms" \
    /p:NuGetAudit=false
```

For a headed browser + screenshots on failure: the fixture already calls `TakeScreenshotOnFailureAsync` in each test's `finally` block (per existing convention). Screenshots land in `tests/Mentoory.Tests.E2E/bin/Debug/net10.0/screenshots/`.

---

## Suite health dashboard

Running a chunk's exit gate counts on the baseline + this-chunk test-count equation. Per spec SC-E1:

| After chunk | Expected total |
|---|---|
| Baseline (before C0) | 446 (full suite), 97 (E2E only) |
| After C0 | 447 total, 98 E2E |
| After C1 | 451 total, 102 E2E |
| After C2 | 454 total, 105 E2E |
| After C3 | 458 total, 109 E2E |
| After C4 | 461 total, 112 E2E |
| After C5 | 464 total, 115 E2E |

If your test-count math diverges from these numbers, you've either skipped a test (check for `[Fact(Skip=...)]`) or shipped an unplanned one. EC2 in the spec handles "scope adjustment" explicitly — if a chunk intentionally ships 4 tests instead of 3, document in its resume prompt.

---

## Extending the suite

After C5, if new coordination-area functionality is added in a future feature, the extension pattern is:

1. Add a new fixture method to `LifecycleFixtures` if needed (additive only).
2. Add a new method to `LifecyclePageObject` if new DOM reads are needed (additive only).
3. Create a new test class `WalkthroughNFeatureXTests` under `tests/Mentoory.Tests.E2E/Tests/Lifecycle/`.
4. Update `coverage-matrix.md` (this file's companion) with the new rows.

The checkpoint pattern (`specs/{feature}/e2e/checkpoints/CN-*.md`) is reusable for any multi-chunk test-writing effort.

---

## Troubleshooting: what "fresh AI session" actually means

- A "fresh session" is a chat/terminal where the AI has zero memory of prior chunks' specific steps, fixture revisions, bug diagnoses, etc.
- What the session reads at start = (1) the chunk's resume prompt (pasted) + (2) whatever files the prompt's execution steps direct it to read.
- The resume prompt's "Prior state" section says what should already be in the branch by commit subject. If the session sees different commits, it stops.
- This pattern requires disciplined chunking. Starting a chunk with ambiguous state ("I think C2 mostly landed but there was a test I'm not sure about") is exactly what the protocol is designed to prevent. When in doubt: `git log --oneline`, verify against the chunk's Prior State, and proceed only if they match.
