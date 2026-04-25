# Quickstart: Starting a Phase Session

**Audience:** An AI session (or human developer) claiming a phase of feature 017-audit-e2e.
**Prerequisites:** Feature 016-audit-pipeline merged to `develop`; `017-audit-e2e` branch exists locally and remotely.

This document is a one-screen runbook. For the full rules see `spec.md`, `contracts/resume-prompt-schema.md`, and `contracts/session-checkpoint-protocol.md`.

---

## Step-by-step (five tool calls, ≤ 2 min)

### 1. Sync the branch

```bash
git checkout 017-audit-e2e
git pull origin 017-audit-e2e
```

### 2. Read the spec and the latest RESUME

```bash
# Read spec.md in full
# Read specs/017-audit-e2e/RESUME-P{N}.md — where N is the phase you're claiming
```

If you don't know which phase to claim, run:

```bash
ls specs/017-audit-e2e/RESUME-*.md | sort
```

The highest-numbered RESUME file is the target phase. If `RESUME-COMPLETE.md` exists, all four phases are done — no work left.

### 3. Confirm green baseline

```bash
dotnet test tests/Mentoory.Tests.E2E/ \
  --filter "FullyQualifiedName~AuditLog" \
  -p:WarningsNotAsErrors=NU1902
```

If any existing `AuditLog*` test is red, STOP. Flag in opening message; do not begin new work.

Also run the lower-layer quick check:

```bash
dotnet test tests/Mentoory.Shared.Application.Tests/ \
             tests/Mentoory.Tests.Architecture/ \
  -p:WarningsNotAsErrors=NU1902
```

### 4. Begin the phase's work

Open the test methods listed in the RESUME's Section 2 and implement them one at a time, following:

- Reuse `PlaywrightFixture` and `LoginHelper` — no new fixtures (FR-014).
- Every Playwright wait carries an explicit timeout ≤ 15 s (FR-015).
- Every Spanish copy assertion references a constant in `AuditSpanishCopy`, never an inline literal (see research.md R-09).

### 5. On green, run the checkpoint

Follow `contracts/session-checkpoint-protocol.md § Phase exit` to the letter:

1. Phase tests green
2. Lower-layer suites green
3. Commit with the prescribed title
4. Push
5. Write next RESUME (or RESUME-COMPLETE)
6. Commit + push RESUME
7. End session

---

## Common pitfalls

- **Writing tests without login**: Phase 2, 4 tests need authenticated sessions. Use `LoginHelper.LoginAsync(page, "admin@mentoory.com", "123abc987")` for GlobalAdmin.
- **Forgetting the context selector**: GlobalAdmin login routes through `/Context/Select`. `LoginHelper` already handles this; do not duplicate the logic.
- **Asserting on untranslated strings**: Spanish copy assertions compare to `AuditSpanishCopy.*` constants. If the constant is missing, ADD IT — don't inline-quote a literal.
- **Leaving audit rows for the next test**: the fixture respawns the `audit` schema between tests (P1 extension). If you see stale rows, the respawn is not wired — flag as a Gotcha.
- **Starting Phase N+1 in the same session**: don't. End the session after the RESUME commit. The protocol exists specifically to prevent this.

---

## Exit signature (what the session must tell the user at end)

```
Phase {N} shipped: {count} new tests.
Phase commit: {sha}
RESUME commit: {sha}
Next session: read specs/017-audit-e2e/RESUME-P{N+1}.md (or RESUME-COMPLETE.md if terminal)
Cumulative AuditLog* tests: {count}, runtime {seconds}s
```

Paste this verbatim as the last message of the session.
