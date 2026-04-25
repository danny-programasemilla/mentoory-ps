# Contract: Session Checkpoint Protocol

**Layer:** Process / Execution
**Visibility:** Public (binding on every AI session working on this feature)
**Enforced by**: SC-004, SC-005, SC-006, SC-007, SC-008 in spec.md

## Purpose

Define the exact behavior every AI session MUST follow when working on feature 017-audit-e2e, so that:

- A single AI session never tries to deliver more than one phase (prevents drift).
- Every phase ends in a reviewable, resumable state.
- A fresh session can pick up Phase N+1 without access to the prior session's transcript.

## Phase kickoff (what every session does FIRST)

Before writing any new code, a session MUST:

1. Verify it is on branch `017-audit-e2e`. If not, checkout.
2. `git pull` to ensure it has the latest commits from prior phases.
3. Read `specs/017-audit-e2e/spec.md` — full file.
4. Read `specs/017-audit-e2e/RESUME-P{N}.md` for the phase it is claiming — full file.
5. If the RESUME Status is `in-progress`, read the pending test-methods list — those are the work items.
6. Run the prior phase's tests to confirm a green baseline:
   ```
   dotnet test tests/Mentoory.Tests.E2E/ --filter "FullyQualifiedName~AuditLog" -p:WarningsNotAsErrors=NU1902
   ```
   If any existing test is red, STOP. Do not begin new work on top of a red baseline. Flag it in the session's opening message and ask for direction.
7. Only then begin writing new tests.

SC-008 measures this: a session MUST complete steps 1-6 within its first five tool calls. Steps 3-4 are Read calls; step 2 is a Bash call; step 6 is a Bash call. Five calls total.

## Phase exit (the seven-step checkpoint)

After the phase's tests are written and green, in strict order:

### 1. Confirm the phase's new tests pass

```bash
dotnet test tests/Mentoory.Tests.E2E/ \
  --filter "FullyQualifiedName~AuditLog{Theme}Tests" \
  -p:WarningsNotAsErrors=NU1902
```

Must exit 0. If not, fix in-session — do NOT push red.

### 2. Confirm lower-layer suites still pass

```bash
dotnet test tests/Mentoory.Shared.Application.Tests/ \
             tests/Mentoory.Tests.Architecture/ \
             tests/Mentoory.Tests.Integration/ \
  -p:WarningsNotAsErrors=NU1902
```

Must exit 0. Catches accidental production-code edits that broke something below.

### 3. Commit the phase work

```bash
git add tests/Mentoory.Tests.E2E/ specs/017-audit-e2e/
git commit -m "Add E2E coverage for audit pipeline — phase {N} ({theme})

{body: list of tests added + any spec deviations}

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

Exact commit-title format is load-bearing (SC-004 greps for it). Body is free-form but MUST list the tests added and MUST flag any deviations from the spec — do not hide them.

### 4. Push the commit

```bash
git push origin 017-audit-e2e
```

Halts if push fails (e.g., remote-ahead). The session SHOULD NOT force-push; it SHOULD pull, rebase or merge cleanly, and try again.

### 5. Write the next RESUME prompt

Author `specs/017-audit-e2e/RESUME-P{N+1}.md` following `contracts/resume-prompt-schema.md` verbatim.

For the terminal phase (P4), write `RESUME-COMPLETE.md` instead, following the retrospective structure in the schema.

### 6. Commit + push the RESUME prompt

```bash
git add specs/017-audit-e2e/RESUME-P{N+1}.md  # or RESUME-COMPLETE.md
git commit -m "Add RESUME-P{N+1} after phase {N} checkpoint"
git push origin 017-audit-e2e
```

A separate commit from step 3 keeps the test-delivery commit focused and makes SC-004's `git log` output clean.

### 7. End the session

Do not start Phase N+1 in this context, even if time remains. The protocol exists precisely to force a fresh session.

The session's final message to the user MUST include:

- Test count added this phase
- Commit SHAs (phase commit + RESUME commit)
- Link to `RESUME-P{N+1}.md` for the next session
- Total cumulative `AuditLog*` test count and runtime

## Partial phase handling (FR-011)

If a session cannot complete its phase — context exhausted, blocker found, external dependency broken — it MUST:

1. Run step 1 (phase tests); note which are green vs pending.
2. Run step 2 (lower layers).
3. Commit what is green with message `WIP phase {N}: {count}/{total} tests green — {short reason}`.
4. Push.
5. Write `RESUME-P{N}.md` with `Status: in-progress`. In Section 2, list ONLY the pending test methods. In Section 4 (Gotchas), describe why the session stopped.
6. Commit + push the RESUME prompt.
7. End.

The next session claims Phase N (not N+1), completes the pending tests, then runs the full seven-step exit ritual.

## Protocol violations (hard errors)

- Starting Phase N+1 in the same session as Phase N. Remedy: end session, start fresh.
- Amending, squashing, or force-pushing a phase commit. Remedy: revert, re-commit cleanly.
- Pushing red tests. Remedy: fix in the same session, or downgrade to partial (WIP) with `Status: in-progress`.
- Modifying code outside `tests/Mentoory.Tests.E2E/` + `specs/017-audit-e2e/` without flagging in commit body AND Gotchas. Remedy: flag retroactively in the next RESUME; document the scope stretch.
- Writing a RESUME file that omits any of the five required sections. Remedy: rewrite and re-push.

## What the protocol is NOT

- Not a permission gate. A session does not ask for human approval at each step — the protocol is procedural, not authoritative.
- Not a runtime hook. Nothing in the test code knows about checkpoints. They are session-level discipline.
- Not a CI enforcement. A human reviewer verifies compliance at PR time.
