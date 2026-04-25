# Contract: Resume Prompt Schema

**Layer:** Process / Handoff
**Visibility:** Public (part of the repo at `specs/017-audit-e2e/RESUME-P{N}.md`)
**Files produced under this contract**:
- `specs/017-audit-e2e/RESUME-P1.md` — bootstrap for Phase 1 (authored by the spec session)
- `specs/017-audit-e2e/RESUME-P2.md` — authored by P1 session at its checkpoint
- `specs/017-audit-e2e/RESUME-P3.md` — authored by P2 session
- `specs/017-audit-e2e/RESUME-P4.md` — authored by P3 session
- `specs/017-audit-e2e/RESUME-COMPLETE.md` — authored by P4 session (terminal retrospective)

## Purpose

A RESUME-P{N}.md file is the ONLY dynamic context a fresh AI session receives beyond the spec itself. It MUST be self-sufficient: reading `spec.md` + the latest `RESUME-P{N}.md` MUST be enough to bootstrap Phase N correctly, even if intermediate sessions made undocumented mistakes.

## Required sections (fixed order, fixed names)

A RESUME file MUST contain exactly these sections. No more, no less. Adding sections is a protocol violation; omitting sections is a protocol violation.

### Header block

```markdown
# Resume — Phase {N}: {theme}

**Previous phase:** P{N-1} ({theme}) — or "initial bootstrap" for RESUME-P1
**Branch:** 017-audit-e2e
**Base commit:** {short-sha of HEAD after the previous checkpoint's push}
**Authored:** YYYY-MM-DD
**Status:** `ready` | `in-progress`
```

- `Status: ready` means the previous phase completed cleanly and Phase N can start fresh.
- `Status: in-progress` means the previous session ran out of context mid-phase and the next session MUST resume the pending test methods (FR-011).

### Section 1: What was shipped in P{N-1}

```markdown
## What was shipped in P{N-1}

- {test-file path}: {list of test method names}
- {any code touched outside the test project — should normally be none; FLAG if yes}
```

For RESUME-P1 (the initial bootstrap), this section is titled `## What has been shipped before` and lists:
- `specs/017-audit-e2e/*` — the spec, plan, research, data-model, contracts
- feature 016-audit-pipeline merged to `develop` (PR #12)

### Section 2: What to do in this session (P{N})

```markdown
## What to do in this session (P{N})

**Objective**: {one-line summary}

**Spec reference**: `specs/017-audit-e2e/spec.md § Phase {N}`

**Test methods to add** (verbatim from coverage matrix in `data-model.md`):
- `{test method 1}`
- `{test method 2}`
- ...
```

### Section 3: Invariants to preserve

**MUST be copied verbatim into every RESUME-P{N}.md.** Intentional redundancy — drift between files is a reconciliation signal.

```markdown
## Invariants to preserve (do NOT violate)

- **Seeded users** (from DACPAC PostDeployment):
  - `admin@mentoory.com` — GlobalAdmin, password `123abc987`
  - `incadmin1@test.mentoory.com` — IncubatorAdmin, password `Test123!@#`
  - `entrepreneur1@test.mentoory.com` — Entrepreneur, password `Test123!@#`
  - `mentor1@test.mentoory.com` — Mentor, password `Test123!@#`
  - `sponsor1@test.mentoory.com` — Sponsor, password `Test123!@#`
- **Shared fixture**: `PlaywrightFixture` via `[Collection(E2ETestCollection.Name)]`. Do not create new fixtures (FR-014). The fixture now respawns the `audit` schema between tests (P1 Phase 1 extension) — assume it.
- **Login helper**: shared `LoginHelper.LoginAsync(page, email, password)` under `tests/Mentoory.Tests.E2E/Infrastructure/`. Do not duplicate; do not write parallel helpers (FR-014).
- **Spanish copy pinning**: shared `AuditSpanishCopy` static class under `tests/Mentoory.Tests.E2E/Infrastructure/` holds every Spanish string asserted by audit tests. All assertions reference constants; NO inline literals.
- **Playwright timeouts**: every `WaitForLoadStateAsync` / `WaitForFunctionAsync` / `WaitForSelectorAsync` call MUST pass an explicit timeout ≤ 15 s (FR-015).
- **Scope boundary**: no file outside `tests/Mentoory.Tests.E2E/` and `specs/017-audit-e2e/` may be modified. Any deviation MUST be called out in the commit body AND the next RESUME's Gotchas section.
- **Manual-mode JSON shape**: `Details` for `CorrectAnswerCommand` carries `Before.TextValue` / `After.NewTextValue` — pinned by `specs/016-audit-pipeline/data-model.md`.
```

### Section 4: Gotchas discovered in P{N-1}

```markdown
## Gotchas discovered in P{N-1}

- {non-obvious finding — selector quirks, timing issues, seed-data surprises}
- {if none: "None — all scenarios behaved as expected."}
```

Gotchas bubble forward, not up. They go into the NEXT phase's RESUME file, never back-propagated into the spec.

### Section 5: Definition of done for P{N}

```markdown
## Definition of done for P{N}

- All P{N} tests listed above green (run: `dotnet test tests/Mentoory.Tests.E2E/ --filter "FullyQualifiedName~AuditLog{Theme}Tests" -p:WarningsNotAsErrors=NU1902`)
- Lower-layer suites still green (`Mentoory.Shared.Application.Tests`, `Mentoory.Tests.Architecture`, `Mentoory.Tests.Integration`)
- Total `AuditLog*` runtime under 3 min
- No diff outside `tests/Mentoory.Tests.E2E/` and `specs/017-audit-e2e/`
- Commit message: exactly `Add E2E coverage for audit pipeline — phase {N} ({theme})`

## Next handoff (not present on RESUME-COMPLETE)

After green: write `RESUME-P{N+1}.md` per this schema, commit, push, end session.
(Phase 4 is terminal — instead of a next handoff, write `RESUME-COMPLETE.md`.)
```

## RESUME-COMPLETE.md special structure

The terminal file replaces the "Next handoff" block with a retrospective:

```markdown
## Retrospective

### Coverage delivered
- {summary: 24 tests across 4 files}

### Deviations from spec
- {any scope or invariant deviations encountered}

### Gotchas aggregated across phases
- P1: ...
- P2: ...
- P3: ...
- P4: ...

### Suggested follow-ups
- {non-blocking items for future iteration}
```

## Validation rules

A RESUME file is **valid** iff:

1. All five required sections are present in order.
2. The Invariants section matches the template above character-for-character (whitespace/punctuation-tolerant).
3. The Status line is one of `ready | in-progress`.
4. If Status is `in-progress`, Section 2's test-method list identifies the pending tests (not the full phase list).
5. File is committed and pushed before the authoring session ends.

A simple sanity-check script (future work) could grep for these headers; for now, code review enforces the rule.
