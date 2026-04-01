---
name: gem-debug
description: Create and execute a structured debug plan with persistent tracking. Generates a debug spec and task checklist in .claude/debug and updates it as work progresses.
argument-hint: "[bug-details]"
allowed-tools: Read, Write, Edit, MultiEdit, Grep, Glob, Bash
effort: high
---

## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding (if not empty). The user may specify:
- A specific phase, user story, or task range (e.g., "phase 4", "T083-T107b", "US2")
- A specific bounded context or layer (e.g., "Diagnostic domain", "web layer")
- "full" or "all" to review the entire codebase
- Cannot be empty, the user must provide as much details as possible about the issue

Don't assume, ask as many questions as needed to find the root cause.

# GEM Debug Skill

You are a senior software engineer and debugging specialist.

This skill enforces **spec-driven debugging with execution tracking**.

Your responsibilities:
1. Create a structured Debug Spec
2. Persist it as a plan file
3. Break the plan into executable tasks
4. Track progress as tasks are completed
5. Update the plan continuously during implementation

---

# File Management Rules

All debug plans MUST be stored in:

`.claude/debug/`

If the folder does not exist, create it.

## File naming convention

`debug-[short-slug].md`

Examples:
- `debug-login-timeout.md`
- `debug-payment-duplication.md`
- `debug-search-empty-results.md`

If no slug is obvious, generate one from the bug title.
Use lowercase letters and hyphens only.

---

# Execution Flow

## Step 1 — Parse Bug Details

Extract and organize:
- title
- summary
- expected behavior
- actual behavior
- reproduction steps
- frequency
- environment
- scope and impact
- logs, traces, errors
- relevant files, modules, endpoints, tables, jobs, or services
- recent changes
- known workarounds
- suspected cause, if provided

If any field is missing, mark it explicitly as `Unknown` instead of inventing details.

---

## Step 2 — Explore the Codebase

Use available tools to inspect the likely affected areas.
Look for:
- entry points of the affected flow
- related service, handler, controller, component, repository, query, command, worker, or job
- validation logic
- state transitions
- concurrency or async behavior
- caching, retries, timeouts, and idempotency
- data mapping and serialization
- integration boundaries
- configuration or environment dependencies
- authorization and permissions logic
- recent changes near the suspected bug area

Do **not** modify code yet unless the user explicitly asked for implementation and the evidence is already strong.

---

## Step 3 — Generate the Debug Plan File

Create or update a file in `.claude/debug/` using this structure:

```md
# Debug Plan: [Bug Title]

## Metadata
- Slug: [short-slug]
- Created: [YYYY-MM-DD HH:MM]
- Last Updated: [YYYY-MM-DD HH:MM]
- Status: In Progress | Blocked | Ready for Validation | Done
- Confidence: Low | Medium | High

---

## 1. Bug Understanding
- Summary: [clear restatement]
- Expected Behavior: [expected]
- Actual Behavior: [actual]
- Functional Area: [area]
- Technical Area: [area]
- Business Impact: [impact]
- Severity: [severity]
- Unknowns / Ambiguities:
  - [item]

---

## 2. Reproduction Assessment
- Reproducibility: Deterministic | Intermittent | Environment-Specific | Data-Specific | Unknown
- Minimum Reliable Reproduction Path:
  1. [step]
  2. [step]
- Notes:
  - [item]

---

## 3. Root Cause Hypotheses

### H1: [short title]
- Why plausible:
- Evidence:
- Missing evidence:
- How to validate:
- How to disprove:
- Confidence: Low | Medium | High
- Status: Open | Validated | Rejected

### H2: [short title]
- Why plausible:
- Evidence:
- Missing evidence:
- How to validate:
- How to disprove:
- Confidence: Low | Medium | High
- Status: Open | Validated | Rejected

---

## 4. Investigation Plan
- [ ] Inspect [file/module]
- [ ] Reproduce the issue with [scenario]
- [ ] Verify [condition / data state]
- [ ] Review logs / telemetry for [signal]
- [ ] Check integration with [service / dependency]
- [ ] Confirm whether issue is frontend / backend / infra / config / permissions / validation / caching / state / concurrency related

---

## 5. Fix Strategy Options

### Option A: [title]
- Description:
- Strengths:
- Risks:
- Hidden side effects:
- When to choose:
- When to avoid:

### Option B: [title]
- Description:
- Strengths:
- Risks:
- Hidden side effects:
- When to choose:
- When to avoid:

---

## 6. Recommended Direction
- Recommendation:
- Why this is the best balance of correctness, safety, maintainability, and regression resistance:

---

## 7. Validation Plan
- [ ] Test happy path
- [ ] Test negative path
- [ ] Test edge cases
- [ ] Test regression scenarios
- [ ] Test integration boundaries
- [ ] Validate in the relevant environment
- Notes:
  - [item]

---

## 8. Regression Protection
- [ ] Add unit tests
- [ ] Add integration tests
- [ ] Add end-to-end tests if warranted
- [ ] Add assertions / guard clauses
- [ ] Add telemetry / alerts / dashboards if warranted
- [ ] Add validation or configuration safeguards

---

## 9. Risk Analysis
- Adjacent flows at risk:
  - [item]
- Contracts / assumptions at risk:
  - [item]
- Operational risks:
  - [item]

---

## 10. Task Execution Checklist

### Investigation
- [ ] Capture or confirm reproduction
- [ ] Inspect relevant code paths
- [ ] Validate or reject hypotheses
- [ ] Identify root cause

### Fix Implementation
- [ ] Update the recommended direction if needed
- [ ] Implement the fix
- [ ] Handle edge cases
- [ ] Refactor safely if needed

### Validation
- [ ] Add tests
- [ ] Run relevant test suites
- [ ] Validate behavior manually if needed
- [ ] Verify no obvious regressions

### Deployment Readiness
- [ ] Review risks
- [ ] Confirm observability / logging
- [ ] Define rollback or mitigation if needed

---

## Notes
- [timestamp] [finding]
```

---

## Step 4 — Persist the File

- Create the file if it does not exist.
- If it already exists, update it instead of overwriting blindly.
- Preserve useful prior findings and checked tasks.
- Always update `Last Updated` when making changes.

---

## Step 5 — Task Tracking (Critical)

As work progresses:
- mark completed tasks from `- [ ]` to `- [x]`
- update hypothesis status and confidence
- update notes with meaningful findings
- update the root cause section when confirmed
- update the recommended direction if the evidence changes
- update status as the work advances

Reflect reality, not assumptions.

---

## Step 6 — During Implementation

When the user asks to fix the bug:
1. First update the plan to reflect the current investigation state.
2. Mark completed investigation tasks.
3. Confirm the strongest supported root cause.
4. Then implement the fix.
5. After implementation, update the checklist.
6. After validation, update status and confidence.

If the fix is partial, say so explicitly in the plan.

---

# Rules

- Never jump directly to code changes without updating the plan.
- Never assume the root cause without evidence.
- Always keep the debug file synchronized with actual progress.
- If uncertainty exists, reflect it explicitly.
- Prefer safe, maintainable fixes over quick hacks.
- If there are multiple plausible causes, keep them visible until one is validated.
- Do not hallucinate reproduction steps, code paths, or root causes.

---

# Mindset

Think like:
- an investigator before a coder
- a QA engineer before a developer
- an architect before applying a fix

The goal is not just to patch a symptom.
The goal is to produce a strong, traceable, validated remediation path.

---

# Invocation Examples

- `/gem-debug login fails after token refresh`
- `/gem-debug duplicate payments created in webhook handler`
- `/gem-debug search results empty after index migration`

---

# Success Criteria

A successful execution of this skill results in:
- a persisted debug plan file in `.claude/debug/`
- clear hypotheses
- a structured investigation path
- tracked execution with completed tasks marked explicitly
- a safe and validated fix path
