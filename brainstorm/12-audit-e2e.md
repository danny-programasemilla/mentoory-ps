# Brainstorm: Audit Pipeline — Browser E2E Coverage

**Date:** 2026-04-19
**Status:** spec-created
**Spec:** specs/017-audit-e2e/

## Problem Framing

Feature 016-audit-pipeline shipped with unit, architecture, and integration coverage — 406+ tests green. But no test confirms that a real user reaching the browser actually sees the audit viewer, or that non-admins actually can't. The existing E2E suite (Playwright, 97 tests) carried no audit coverage. Two gaps surfaced:

1. **Delivery quality**: Spanish copy, menu visibility, filter dropdowns, and redaction rendering could silently regress without being caught until release.
2. **Process drift**: writing ~24 E2E tests in a single AI session reliably produces quality drop-off — test data conventions diverge, assertion messages become inconsistent, setup duplicates across files. Long-running sessions need forced boundaries.

This brainstorm produces a spec that solves both: a coverage plan (24 tests across viewer, capture flows, correlation, regressions) AND a session-management protocol (four phases, each ending in commit + push + context-clear + resume prompt).

## Approaches Considered

### A: Phase by user story (CHOSEN)
- Four phases maps directly to the four user stories from feature 016.
- P1 viewer shell (9 tests) → P2 capture flows (7) → P3 correlation (3) → P4 regressions (5).
- Pros: each phase ships a coherent slice; halting after any phase leaves useful coverage; the user-facing narrative is identical to the 016 spec.
- Cons: P3 (3 tests) is lighter than P4 (5 tests); not a strict complexity ramp.

### B: Phase by test complexity
- Simple (page loads + auth) → Medium (filters + detail rows) → Complex (multi-step flows) → Hardest (correlation + regressions).
- Pros: smoother ramp; easier for low-context sessions to ride.
- Cons: phases don't map to requirements; review story becomes "what did we ship in P2?" is harder to answer.

### C: Minimal checkpoints (two phases)
- All read-only viewer tests in P1; all write-then-verify tests in P2.
- Pros: less process overhead; two commits.
- Cons: each phase is ~12 tests — drift risk rises; defeats the main purpose of the protocol.

## Decision

**Approach A** — phase by user story. The four-phase split pays the "extra commits" cost in exchange for bounded context per AI session and handoffs that map 1:1 to the 016 user stories.

**Checkpoint protocol**: each phase ends with a mandatory seven-step exit ritual (run tests green, run lower-layer suites green, commit, push, write `RESUME-P{N+1}.md`, commit, push, end session). The next session MUST bootstrap by reading the spec + the latest RESUME file, pulling latest, and running the prior phase's tests before writing any new code.

**Resume prompt schema**: five canonical sections, with the Invariants block duplicated across every RESUME file. Intentional redundancy — any single RESUME is sufficient to start the next session even if the spec has evolved; drift between the duplicated invariants is itself a review signal.

**Scope**: 24 tests in four files, all under `tests/Mentoory.Tests.E2E/Tests/AuditLog*.cs`. Zero production code modified across the four phases (SC-005 enforces a literal `git diff` check).

## Open Threads

- Should `RESUME-COMPLETE.md` be human-readable Markdown (current) or structured JSON if a CI gate is added? (from #12)
- Phase-boundary philosophy: user-story-aligned (current) vs implementation-complexity ramp — reviewable question flagged in the review brief. (from #12)
- Spanish-copy assertions are split between P1 and P4; consolidate into one file, or keep distributed? (from #12)
- SC-008 measures bootstrap in "tool calls" — a novel unit. Reframe as "reads no files beyond spec + RESUME"? (from #12)
- Fixture respawn gap: `PlaywrightFixture` respawn of the `audit` schema is unverified. Flagged as P1's first concrete task in FR-016. (from #12)
