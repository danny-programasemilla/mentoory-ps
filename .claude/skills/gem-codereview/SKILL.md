---
name: gem-codereview
description: Post-implementation code review for the Mentoory C#/.NET project. Validates fixes and changes against constitution, spec intent, and test coverage. Designed to run after /gem-completion fixes to verify correctness.
---

## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding (if not empty). The user may specify:
- A specific phase, user story, or task range (e.g., "phase 4", "T083-T107b", "US2")
- A specific bounded context or layer (e.g., "Diagnostic domain", "web layer")
- "full" or "all" to review the entire codebase
- If empty, review the most recently completed phase

## Goal

Validate that recent code changes (from `/gem-completion` fixes, `/speckit.implement`, or manual work) are:
1. **Correct** — they actually solve the problem they claim to solve
2. **Aligned** — they match the spec, plan, and data-model intent
3. **Safe** — they don't break existing functionality
4. **Covered** — they have appropriate test coverage at each layer
5. **Compliant** — they follow constitution principles

This is NOT a style review. This is a functional correctness review that catches the things builds and unit tests miss.

## Pre-Review: Load Context

Before any review work, load these documents:
1. `.specify/memory/constitution.md` — governance principles (non-negotiable)
2. `CLAUDE.md` — project conventions and critical rules
3. `.claude/architecture.md` — layer boundaries and patterns
4. `.claude/web-patterns.md` — web layer conventions
5. `.claude/ddd-patterns.md` — domain patterns
6. The feature's `spec.md`, `plan.md`, `data-model.md`, and `tasks.md`

## Review Phases

### Phase 1: Change Detection

Identify what was changed:

```bash
# If reviewing uncommitted work
git diff HEAD --stat
git status -s

# If reviewing a completed phase, find files by task markers in tasks.md
grep '\[X\].*\[US{N}\]' specs/{feature}/tasks.md
```

Group changed files by layer: Domain, Application, Infrastructure, SSDT, Web, Tests.

### Phase 2: Build Gate

```bash
dotnet build --configuration Release
```

If not 0 warnings, 0 errors — **STOP**. Report build failures. Nothing else matters.

### Phase 3: Test Gate

```bash
dotnet test
```

Report results. If any test fails — **STOP**. Report failures with root cause analysis.

### Phase 4: Constitution Compliance Review

For each changed file, verify against the 10 constitution principles:

| Principle | What to check |
|-----------|---------------|
| I. Layer Boundaries | No web types in Domain/Application. Controllers don't inject repositories. |
| II. CQRS | Commands use `IBaseRequest`. Handlers extend `BaseCommandHandler`. FluentValidation on all user-input commands. |
| III. DDD | Aggregate roots control children. Private backing fields. `internal` factory methods on child entities. ExternalId on external entities. ID-only cross-aggregate refs. |
| IV. Integration Events | Events in originating domain's Application/IntegrationEvents/. No business logic sharing. |
| V. Zero Warnings | Build clean. No suppressed warnings. |
| VI. DateTime | No `DateTime.UtcNow` anywhere. `ITimeProvider` in Application. Parameters in Domain. |
| VII. Naming | `{Verb}{Entity}Command`, `{Get|List}{Entity}Query`, `{Entity}Dto`, etc. |
| VIII. File Organization | One class per file. JS in `/wwwroot/js/`. SQL UTF-8. PostDeployment outside Mentoory.Db/. |
| IX. Spanish UI | All user-facing text (validation messages, toasts, labels) in Spanish. Code in English. |
| X. SSDT | Schema via .sql files. No EF migrations. Idempotent PostDeployment scripts. |

Report violations with `file_path:line_number` and the specific principle violated.

### Phase 5: Spec Alignment Review

For each completed task in scope, verify the implementation matches the **intent** from spec.md and plan.md:

1. **Read the spec requirement** that the task addresses.
2. **Read the actual implementation** (the code file).
3. **Ask**: Does the code do what the spec says? Not just "does it compile" but "does it implement the described behavior?"

Common misalignments to catch:
- Spec says "filter by subscription tier" but the handler ignores the filter parameter
- Spec says "audit trail with who/when/previous value" but the entity only stores who/when
- Spec says "score aggregation per topic" but the handler aggregates per question
- Spec says "unique constraint on (A, B, C)" but the DbContext has no such index
- Spec says "cascade to knowledge structure" but the handler only clones the diagnostic form

### Phase 6: Data Flow Review

For the 5–8 most critical user workflows in the scope, trace the full data path:

**For each workflow:**
1. **Controller** → verify it creates the right command/query with correct parameters
2. **Validator** → verify all required fields are validated, Spanish messages
3. **Handler** → verify it calls the right repository method (with correct Includes), uses `SaveEntitiesAsync`, publishes events when required
4. **Repository** → verify the called method loads the data the handler needs (children included?)
5. **DbContext** → verify entity configuration maps all columns from data-model.md
6. **SSDT** → verify the table exists with matching columns, constraints, FKs
7. **Round-trip** → could data written by the handler be correctly loaded by a query handler?

Flag any break in the chain.

### Phase 7: Test Coverage Review

For each layer in scope, assess test coverage:

**Domain Tests** — check:
- [ ] Every aggregate root factory method tested (happy + invalid input)
- [ ] Every business rule / invariant enforced by the aggregate tested
- [ ] Every state transition tested (e.g., MarkAsCompleted when already completed → throws)
- [ ] Child entity creation via aggregate root tested
- [ ] Value object equality and validation tested

**Application Handler Tests** — check:
- [ ] Happy path: command succeeds, entity persisted, events published
- [ ] Not-found path: returns correct failure code
- [ ] Validation: validator catches invalid input
- [ ] Edge cases: duplicate, concurrent, missing dependencies

**Integration Tests** — check:
- [ ] At least one round-trip test per aggregate (Create → Save → Reload → Assert)
- [ ] EF configuration validated against real DB (catches missing tables, wrong mappings)
- [ ] Cross-aggregate queries work (e.g., diagnostic references project form)

**Missing coverage** is a finding. Report what SHOULD be tested but isn't, with severity:
- Critical: No round-trip test for an aggregate (EF mapping errors go undetected)
- High: No handler tests (orchestration bugs undetected)
- Medium: Missing edge case tests
- Low: Missing assertion on a non-critical property

### Phase 8: Regression Risk Assessment

For each changed file, assess what ELSE could break:

1. **Domain changes** — Do any handlers, tests, or views depend on the changed entity? Did a property type or access modifier change that could break callers?
2. **DbContext changes** — Could the new configuration affect existing queries? (e.g., adding a global query filter, changing a delete behavior)
3. **SSDT changes** — Could the schema change break existing data? (e.g., adding a NOT NULL column without default, changing FK cascade behavior)
4. **Web changes** — Could routing changes affect existing Area controllers? Could new Areas conflict with existing routes?

Run a targeted test if regression risk is identified:
```bash
dotnet test tests/Mentoory.{AffectedBC}.Tests/
dotnet test tests/Mentoory.Tests.Integration/
```

### Phase 9: Report

```markdown
## Gem Code Review: {Scope}

### Gates
- [ ] Build: 0 warnings, 0 errors
- [ ] Tests: X passed, 0 failed

### Constitution Compliance
| Principle | Status | Notes |
|-----------|--------|-------|
| I–X      | PASS/FAIL | details if FAIL |

### Spec Alignment
| Task | Spec Requirement | Implementation Status | Gap |
|------|-----------------|----------------------|-----|

### Data Flow Issues
| Workflow | Break Point | Finding | Severity |
|----------|-------------|---------|----------|

### Test Coverage
| Layer | Files | Tested | Missing | Severity |
|-------|-------|--------|---------|----------|

### Regression Risks
| Changed File | Affected By | Risk | Mitigation |
|--------------|-------------|------|------------|

### Verdict

**PASS** — Changes are correct, aligned, safe, and covered.

OR

**FAIL** — N issues found. Action items:
1. ...
2. ...
```

## Interaction with Other Skills

- Run **after** `/gem-completion fix` to verify the fixes are sound
- Run **after** `/speckit.implement` to verify implementation matches spec
- Run **before** committing or creating a PR
- Findings feed back into `/gem-completion` if new gaps are discovered

## What This Skill Does NOT Do

- Style review (use `/simplify` for that)
- Feature design (use `/speckit.plan`)
- Task completeness audit (use `/gem-completion`)
- Performance optimization (out of scope — file a separate task)
