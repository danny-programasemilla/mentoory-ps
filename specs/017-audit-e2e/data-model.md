# Data Model: Audit Pipeline — Browser E2E Coverage

**Phase:** 1 (Design & Contracts)
**Feature:** 017-audit-e2e
**Date:** 2026-04-19

This is a test-infrastructure deliverable — no new database tables, EF entities, or aggregates. The "data model" here is the **logical model of the session protocol**: Phase, Checkpoint, and ResumePrompt. These entities live as files and git commits, not rows in a database.

---

## 1. Phase

A named unit of work that a single AI session delivers. Four Phases exist for this feature.

### Attributes

| Attribute | Type | Notes |
|-----------|------|-------|
| `Number` | int (1-4) | Sequential phase number |
| `Theme` | string | Short label (e.g., `"Viewer shell"`, `"Capture flows"`) |
| `TestFile` | path | Exactly one new file under `tests/Mentoory.Tests.E2E/Tests/AuditLog{Theme}Tests.cs` |
| `TestCount` | int | Fixed by spec FR-001 (P1=9, P2=7, P3=3, P4=5) |
| `TestMethods` | list<string> | Copied verbatim from the spec's acceptance scenarios |
| `ExitCriterion` | string | Binary: all phase tests green + all lower-layer suites green |
| `CommitSha` | string (optional) | Set once the phase's commit lands |

### State transitions

```
 Not started  ───────────────►  In progress
                                    │
                                    │  (all tests green + lower layers green)
                                    ▼
                              Checkpoint ready
                                    │
                                    │  (commit + push + resume prompt + push)
                                    ▼
                                 Handed off
                                    │
                                    │  (next session begins)
                                    ▼
                                 Completed
```

### Invariants

- Exactly one commit per phase (SC-004). Amending or squashing is a protocol violation.
- A phase that cannot complete emits a partial `RESUME-P{N}.md` with `status: in-progress` listing pending test methods (FR-011).
- Production code outside the test project is never modified (SC-005).

---

## 2. Checkpoint

The seven-step exit ritual at the end of each phase. A Checkpoint is the transition between a Phase completing and the next Phase being claimable.

### Steps (strict order)

1. Run the phase's new tests — all green.
2. Run the lower-layer suites (`Shared.Application.Tests` + `Tests.Architecture` + `Tests.Integration`) — all green.
3. Commit with message `Add E2E coverage for audit pipeline — phase N ({theme})`.
4. Push to origin.
5. Write `specs/017-audit-e2e/RESUME-P{N+1}.md` per the canonical schema (see contracts/).
6. Commit + push the resume prompt.
7. End the session.

### Violations

- Pushing red is a protocol violation (step 1 must pass first).
- Starting Phase N+1 in the same session is a protocol violation (step 7 forbids it).
- Two phases in one commit is a protocol violation (SC-004).

---

## 3. ResumePrompt

A Markdown file at `specs/017-audit-e2e/RESUME-P{N}.md` that is the ONLY dynamic context a fresh AI session receives beyond the spec itself.

### Canonical structure

Defined in `contracts/resume-prompt-schema.md`. Five required sections in fixed order:

1. **What was shipped in P{N-1}** — audit trail of tests added, files touched
2. **What to do in this session (P{N})** — objective + test-method list
3. **Invariants to preserve** — duplicated across every RESUME (intentional drift signal)
4. **Gotchas discovered in P{N-1}** — forward-propagated operational knowledge
5. **Definition of done for P{N}** — exit checklist

### Invariant rules

- The Invariants section is copied verbatim into every RESUME file (not referenced). Drift between files signals that the spec or fixture has changed and reconciliation is needed before continuing.
- Gotchas bubble forward, not up. P2 findings land in RESUME-P3.md, not in spec.md. The spec stays clean; operational knowledge flows through the chain.

---

## 4. TestCoverageMatrix

Binding between the spec's acceptance scenarios and Phase test methods. Informational — no enforcement beyond code review.

### Phase 1 — AuditLogViewerTests (9 tests)

| Test method | Acceptance scenario |
|-------------|---------------------|
| `GlobalAdmin_CanOpenAuditLog_TableRenders` | US1 §1 |
| `IncubatorAdmin_IsDenied` | US1 §2 |
| `Entrepreneur_IsDenied` | US1 §2 |
| `Mentor_IsDenied` | US1 §2 |
| `Sponsor_IsDenied` | US1 §2 |
| `GlobalAdmin_SeesMenuEntryUnderPlataforma` | US1 §3 |
| `IncubatorAdmin_DoesNotSeeMenuEntry` | US1 §4 |
| `FilterWithNoMatch_ShowsSpanishEmptyState` | US1 §5 |
| `OutcomeDropdown_HasSpanishOptions` | US1 §6 |

### Phase 2 — AuditLogCaptureTests (7 tests)

| Test method | Acceptance scenario |
|-------------|---------------------|
| `RegisterUser_ProducesAuditRow_WithRedactedPassword` | US2 §1 |
| `LoginWithInvalidPassword_ProducesFailureRow` | US2 §2 |
| `LoginWithValidPassword_ProducesSuccessRow` | US2 §3 |
| `AssignRole_ProducesAuditRow` | US2 §4 |
| `CorrectAnswer_ProducesRowWithBeforeAndAfter` | US2 §5 |
| `CorrectAnswer_ProducesExactlyOneRow_NoDoubleWrite` | US2 §6 |
| `ExpandButton_RevealsPrettyPrintedDetails` | US2 §7 |

### Phase 3 — AuditLogCorrelationTests (3 tests)

| Test method | Acceptance scenario |
|-------------|---------------------|
| `AnyGetResponse_CarriesValidGuidCorrelationId` | US3 §1 |
| `ClientProvidedCorrelationId_IsEchoedVerbatim` | US3 §2 |
| `TwoRequestsWithoutHeader_GetDistinctCorrelationIds` | US3 §3 |

### Phase 4 — AuditLogRegressionTests (5 tests)

| Test method | Acceptance scenario |
|-------------|---------------------|
| `NoPlaintextPasswordAppearsInAnyDetails` | US4 §1 |
| `FilterBarButtons_HaveSpanishLabels` | US4 §2 |
| `PaginationControls_HaveSpanishLabels` | US4 §3 |
| `OutcomeBadges_RenderSpanishTextAndColor` | US4 §4 |
| `ValidationRejectedCommand_ProducesNoAuditRow` | US4 §5 |

---

## 5. Volumes and growth

| Metric | Value |
|--------|-------|
| New C# files under `tests/Mentoory.Tests.E2E/` | 4 test files + 1 shared login helper + 1 shared Spanish copy constants = 6 |
| New Markdown files under `specs/017-audit-e2e/` | 5 (RESUME-P1..P4 + RESUME-COMPLETE) + 3 design docs already emitted |
| Git commits on branch `017-audit-e2e` | 1 spec commit (already landed) + 4 phase commits + 4 resume-prompt commits + 1 terminal retrospective commit = 10 |
| Total test count at end of P4 | 24 new + 97 existing = 121 E2E tests |
