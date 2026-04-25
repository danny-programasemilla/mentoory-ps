# Review Brief: E2E coverage for Project Lifecycle (feature 016)

**Spec:** `specs/016-project-lifecycle-finish/e2e/spec.md`
**Generated:** 2026-04-19

> Reviewer's guide to scope and key decisions. See full spec for details.

---

## Feature Overview

Feature 016 shipped the Coordinator UI for project lifecycle advancement (timeline view, advance command, gated actions, server-side guard), but its user-visible surface has zero automated coverage — the three manual walkthroughs (T042, T051, T056) in the parent `tasks.md` were deferred. This spec delivers ~16 Playwright E2E tests that cover every acceptance scenario, automatable edge case, and automatable success criterion in the parent spec. It also defines a **checkpoint protocol** (6 chunks, pre-authored resume prompts, strict order) that lets the work execute safely across multiple AI sessions without drift.

## Scope Boundaries

- **In scope:** Every US1 / US2 / US3 acceptance scenario from parent spec, 4 automatable edge cases (concurrency, inactive, cross-incubator, no-context), SC-001 + SC-004 + SC-005.
- **Out of scope:** SC-002 (timing), SC-003 / SC-006 (usability), SC-007 (ops metric), accessibility audit beyond tooltip text, visual regression, cross-browser matrix, general HTTP controller test infrastructure, manual-walkthrough preservation as separate playbooks, product-code changes.
- **Why these boundaries:** Automate what a test can prove. Leave the usability/ops surface to the processes that already own them. Keep product code untouched so the work is a pure additive safety net.

## Critical Decisions

### D1 — Two-tier chunking (foundation + per-walkthrough)
- **Choice:** C0 ships fixtures only (no scenario tests); C1–C5 ship one walkthrough each.
- **Trade-off:** One extra chunk (six total instead of five). Pays back because every later chunk starts with a known-good substrate; the seed-data regression from yesterday would have been caught by C0's smoke test alone.
- **Feedback:** Is the smoke test (one trivial test in C0) worth the extra setup overhead?

### D2 — Strict chunk ordering, no parallelism
- **Choice:** C0 → C1 → C2 → C3 → C4 → C5, no skipping, no interleaving.
- **Trade-off:** Sacrifices parallelism (five Walkthrough chunks COULD run in parallel after C0). Gains: each resume prompt is short and unambiguous; drift-detection pre-flight is trivial.
- **Feedback:** Given the ~1-day total effort, is the parallelism loss acceptable?

### D3 — Tests placed under `tests/Mentoory.Tests.E2E/Tests/Lifecycle/`, not a new E2E project
- **Choice:** Use the existing E2E project and `PlaywrightFixture`.
- **Trade-off:** Tests share a Testcontainers container with existing 97 E2E tests. Gains speed; constrains isolation. If a Walkthrough test leaks state, the existing suite could flicker.
- **Feedback:** Does the shared-container model need a Respawn reset strategy built into `LifecycleFixtures`?

### D4 — Sibling directory inside feature 016 (not a new numbered feature)
- **Choice:** `specs/016-project-lifecycle-finish/e2e/` — not `specs/NNN-e2e-lifecycle/`.
- **Trade-off:** Conflates the product feature and its E2E safety-net into one directory. Gains: reviewer can trace requirements-to-tests without opening two feature folders.
- **Feedback:** Is the conflation acceptable, or should the E2E work stand on its own numbered spec?

### D5 — No product-code changes allowed in C1–C5
- **Choice:** If a test reveals a product-code bug, skip-mark it and surface to the user. Do not fix.
- **Trade-off:** Leaves bugs known-unfixed in the skip list until a separate cycle. Gains: no silent feature creep, no session drift into non-test work.
- **Feedback:** If a blocker bug is found mid-session, is the "skip + surface" policy strict enough, or should some classes of bug justify in-chunk fixes?

## Areas of Potential Disagreement

### Test #3 (US1 §3 — Completed-state advance rejection)

- **Decision:** The test needs a "broken-state" fixture method that directly sets `CurrentStageState = Completed` on a project, bypassing the domain method.
- **Why this might be controversial:** This fixture path creates state the normal UI flow can't produce. A reviewer might argue the test exercises a scenario that can't happen, which makes the test a liability.
- **Alternative view:** Delete test #3 as redundant. Move the coverage note to "defensively tested at handler unit-test level only."
- **Seeking input on:** Keep the broken-state fixture (matches spec.md US1 §3 literally) or drop the test as a fabricated edge case?

### Concurrency test (row 16)

- **Decision:** Use a test-time helper to issue a stale `AdvanceProjectStageCommand` from a second DbContext scope while the browser holds the Lifecycle page — validates the conflict-toast UI end-to-end.
- **Why this might be controversial:** True concurrency in a browser test is hard and often non-deterministic; most shops test concurrency at integration level (which we already do — `AdvanceProjectStageConcurrencyTests`).
- **Alternative view:** Skip the concurrency UI test and rely on the existing integration test. Document in the coverage matrix as "integration-covered, UI not asserted."
- **Seeking input on:** Is the UI-level concurrency assertion worth the flakiness risk, or should it drop to the matrix "not covered at UI level" category?

### Seed-data modifications during chunks

- **Decision:** C4 / C5 may modify `Mentoory.Db.PostDeployment/004.SeedTestData.sql` to add missing test users (coord3, incubator-admin-2) if they're not present.
- **Why this might be controversial:** Seed changes in the middle of a session cross the test/fixture boundary into shared DB infrastructure. Accidental breakage there affects every E2E test.
- **Alternative view:** Create any needed users via the fixture's `CreateProjectAsync` equivalent at test time, never touching seed SQL.
- **Seeking input on:** Is the idempotent-insert approach safe enough, or should all test users come from runtime fixtures only?

## Naming Decisions

| Item | Name | Context |
|------|------|---------|
| Spec directory | `specs/016-project-lifecycle-finish/e2e/` | Sibling of parent spec (Q1 decision) |
| Test directory | `tests/Mentoory.Tests.E2E/Tests/Lifecycle/` | Isolates new tests by feature |
| Fixture directory | `tests/Mentoory.Tests.E2E/Infrastructure/Lifecycle/` | Matches existing `Infrastructure/` convention |
| Fixture class | `LifecycleFixtures` | State-seeding helpers |
| Page object | `LifecyclePageObject` | Selectors for `/Coordination/Projects/Lifecycle/{id}` |
| Chunk files | `checkpoints/CN-slug.md` | N = chunk number, slug names the walkthrough |
| Chunks | C0 … C5 | 6 sessions, strict order |
| Coverage tracker | `coverage-matrix.md` | Spec-scenario-to-test mapping, updated per chunk |
| Parking lot | `open-questions.md` | Execution-time issues, populated during chunks |

## Open Questions

No open questions at spec approval time. All execution-time questions go to `open-questions.md`.

## Risk Areas

| Risk | Impact | Mitigation |
|------|--------|------------|
| E2E tests flaky due to Playwright timing | Medium | Determinism invariant (R1 NFR) — no sleeps, `WaitFor` primitives only |
| A chunk exposes a real product-code bug mid-way | Medium | E2 protocol — skip-mark + surface; chunks don't silently fix |
| Seed SQL changes cascade to existing tests | High if triggered | Idempotent `INSERT ... WHERE NOT EXISTS`, full suite run as part of exit gate |
| Testcontainers container slow/unstable | Low | Shared container per test class; Docker validation in each pre-flight |
| Cross-tenant test reveals a data leak | Critical if triggered | Dedicated Stop condition in C4; surface immediately, do not skip and continue |
| Resume prompt drift across sessions | High if triggered | EC6 — any unexpected git state stops the session; pre-flight verifies commit chain |

---
*Share with reviewers before implementation. Execution happens in 6 chunks, one per checkpoint file under `checkpoints/`.*
