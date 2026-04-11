---
name: gem-completion
description: Audit claimed task completions against functional reality AND user-facing readiness. Finds gaps between what's marked done and what actually works end-to-end — from menu entry to completed workflow. Creates actionable plans to finish the real work.
---

## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding (if not empty). The user may specify:
- A specific phase, user story, or task range to audit (e.g., "phase 4", "T083-T107b", "US2")
- A specific bounded context or layer to focus on (e.g., "Diagnostic domain", "web layer")
- "full" or "all" to audit the entire tasks.md
- If empty, audit the most recently completed phase

## Goal

Determine what has actually been built versus what has been claimed as complete. Identify the gap between marked-as-done checkboxes and production-ready functionality **that a real user can reach, use, and complete**. Create pragmatic, testable plans to bridge that gap.

This skill exists because:
- Checkboxes get checked when files are created, not when features work end-to-end
- Build success does not mean runtime success
- Handler code that compiles may have broken query logic, missing persistence, or dead paths
- Views that render may have broken form submissions, missing JS wiring, or wrong routes
- Tests that pass may not cover the actual failure modes
- **Features that work technically may be invisible to users** — no menu entry, no navigation link, no way to reach the page without knowing the URL
- **A QA person cannot test what they cannot find** — if there's no path from login to the feature, it's not done

## Operating Constraints

- **Read-only by default**: This skill produces a report. It does NOT fix anything unless the user explicitly says "fix" or "remediate".
- **Ask before assuming**: When findings involve design decisions (role access, workflow scope, authorization changes), surface them as **Clarification Questions** in the report. Never silently assume the answer — the user must decide. This is especially important for role-based access changes and cross-role testability.
- **Think from the user's chair**: Always consider who will be testing the feature. If the tester is typically logged in as GlobalAdmin, flag any workflow that requires a different role. Include a Testability Summary so the user knows what accounts/contexts they need before they start.
- **Constitution compliance**: Load `.specify/memory/constitution.md` and flag any violations found during audit.
- **Evidence-based**: Every finding must reference a specific `file_path:line_number` and explain why it's a problem.
- **No false positives**: Only report issues that would cause runtime failures, data loss, or broken user workflows. Style preferences are out of scope.

## Severity Levels

| Level | Meaning | Example |
|-------|---------|---------|
| **Critical** | Feature claimed as done does not work at all, or is completely unreachable by users | Data never persisted, no menu entry for the feature, controller exists but no navigation path, handler returns wrong type |
| **High** | Feature is partially reachable or works partially with a blocking gap | Menu entry exists but links to wrong URL, form submits but response is never loaded back, role can access area but there's no return link |
| **Medium** | Feature works and is reachable but has a correctness, flow, or UX gap | Missing validation feedback on screen, no success/error toast after action, broken breadcrumb, wrong redirect after form submit |
| **Low** | Feature works end-to-end but has a quality or polish gap | Missing loading indicator, inconsistent button labels, dead code, missing AsNoTracking |

## Execution Steps

### Phase 1: Scope Identification

1. Run `.specify/scripts/bash/check-prerequisites.sh --json --require-tasks --include-tasks` from repo root to locate the feature directory and tasks.md.
2. Parse tasks.md to identify **completed tasks** (`[X]`) within the user's specified scope.
3. Group completed tasks by layer: Domain, Application, Infrastructure, SSDT, Web, Tests.
4. Load `plan.md` and `data-model.md` to understand what was intended.

### Phase 2: Build Verification

Run `dotnet build --configuration Release` from repo root. If the build fails, stop and report build errors as Critical findings — nothing else matters until the build is green.

### Phase 3: Test Verification

Run `dotnet test` from repo root. Report:
- How many tests exist for the audited scope
- How many pass / fail / skip
- What's NOT covered (compare test files against the domain/application code they should test)

### Phase 4: Domain Layer Audit

For each completed Domain task, verify:

1. **Aggregate roots exist and have factory methods**: Check that `Create`/`CloneFrom*` static methods exist and enforce required fields.
2. **Child entities are properly encapsulated**: Mutation methods should be `internal`, collections should be private backing fields with `AsReadOnly()`.
3. **Value objects implement `GetEqualityComponents`**: And all semantic properties are included.
4. **Repository interfaces match the data model**: Every aggregate root in data-model.md has a repository interface, and the methods cover the needed queries.
5. **Domain invariants are enforced**: Factory methods validate required fields (not just relying on DB constraints).
6. **No cross-aggregate object references**: Only ID references across aggregate boundaries.

### Phase 5: Application Layer Audit

For each completed Application task, verify:

1. **Commands have validators**: Every command with user input has a FluentValidation validator with Spanish messages.
2. **Handlers call `SaveEntitiesAsync`**: Not just `SaveChangesAsync` (the former dispatches domain events).
3. **Handlers publish integration events when spec requires it**: Cross-reference with spec.md event requirements.
4. **Query handlers return correct data**: Check that DTOs are populated from actual repository data, not hardcoded or partially mapped.
5. **DataTable queries work end-to-end**: Verify totalRecords, filteredRecords, sorting, and paging logic. This is a common source of bugs — check that:
   - `totalRecords` counts the unfiltered set
   - `filteredRecords` counts after search/filter but before paging
   - Sorting uses the entity IQueryable (not the projected DTO)
   - The response constructor receives the right arguments in the right order
6. **Integration events contain the data consumers need**: Check that event records include IDs and timestamps that downstream handlers require.

### Phase 6: Infrastructure Layer Audit

For each completed Infrastructure task, verify:

1. **DbContext entity configurations match data-model.md**: Every column, index, FK, and constraint in the data model has a corresponding Fluent API configuration.
2. **Junction tables are properly wired**: If the data model has a junction table, verify there's either a navigation property or a value converter that connects the domain entity's collection to the junction table. **This is the #1 source of silent data loss.**
3. **Repository methods include the right child entities**: `GetByIdAsync` vs `GetByIdWithChildrenAsync` — verify callers get what they need.
4. **DependencyInjection.cs registers everything**: Every repository interface is mapped to its implementation. The DbContext is registered with Aspire enrichment.
5. **Owned value objects are configured**: `OwnsOne()` calls for all value objects used in entities.
6. **Enum conversions are configured**: `HasConversion<byte>()` for all enum properties.

### Phase 7: SSDT Audit

For each completed SSDT task, verify:

1. **Tables match data-model.md exactly**: Column names, types, nullability, defaults, constraints.
2. **Foreign keys reference the correct tables**: With correct ON DELETE behavior.
3. **Indexes exist for query patterns**: Especially for columns used in WHERE clauses by repository methods.
4. **Unique constraints match domain rules**: e.g., one active entrepreneur per project.

### Phase 8: Web Layer Audit

For each completed Web task, verify:

1. **Controllers wire to the correct commands/queries**: The command/query types match what the handler expects.
2. **Views reference correct model types**: `@model` directive matches what the controller passes.
3. **Forms submit to the correct action**: `asp-action` and `method` attributes are correct.
4. **DataTable JS matches the controller's Data endpoint**: Column names in JS match DTO property names (case-sensitive).
5. **Anti-forgery tokens are present**: Forms have `@Html.AntiForgeryToken()`, AJAX calls send the token header.
6. **Routes are accessible**: Area + controller + action routing resolves correctly.
7. **All user-facing text is in Spanish**: Labels, buttons, validation messages, empty states, toasts, confirmation text.
8. **Success/error feedback exists**: After form submissions, users see a toast (`TempData["SuccessMessage"]`) or validation errors — never a silent redirect with no indication of what happened.

### Phase 9: Cross-Layer Integration Audit

Trace 5-8 key user workflows end-to-end through all layers:

1. Pick a representative workflow (e.g., "clone a form template" or "submit a diagnostic").
2. Trace from controller action → command/query → handler → repository → DbContext → SSDT table.
3. Verify data flows correctly through every layer.
4. Identify any breaks in the chain (e.g., handler creates entity but repository never persists it, or DB has a column the entity doesn't map to).

### Phase 10: Reachability & UX Flow Audit (QA Readiness)

This phase answers the question: **"Can a real user (or QA tester) actually find and use this feature?"** A feature that compiles, passes tests, and handles data correctly is still NOT done if no one can reach it.

#### 10a. Navigation & Menu Reachability

For each Area/Controller added or modified in the audited scope:

1. **Menu entries exist in `MenuConfiguration.cs`**: Every new controller that serves user-facing pages MUST have a corresponding menu item. Check:
   - The menu item links to the correct Area/Controller/Action URL
   - The menu item is assigned to the correct role(s) (cross-reference with `[Authorize(Roles = "...")]` on the controller)
   - The menu item has an appropriate icon (`fas fa-*`) and Spanish label
   - The menu item is placed in a logical menu group (not orphaned at root level)
2. **All roles with access have navigation**: If a controller uses `[Authorize(Roles = "ProjectCoordinator")]`, verify that the ProjectCoordinator role has a menu item pointing to that controller.
3. **No dead-end pages**: Every page a user can navigate TO should have a way to navigate BACK (breadcrumbs, back links, or menu highlighting).
4. **Landing pages exist**: If a new Area is introduced (e.g., "Coordination"), verify it has a default landing page (usually an Index action on the main controller).

#### 10b. User Workflow Walkthrough (Persona-Based)

For each user role affected by the audited scope, simulate the complete journey:

1. **Identify affected roles** from the spec (e.g., ProjectCoordinator, Entrepreneur, GlobalAdmin).
2. **For each role, trace the happy path**:
   - Login → Context selection (if multi-role) → Menu navigation → Feature landing page → Primary action → Confirmation/result
3. **Verify each step is wired**:
   - Can the user see the menu item for their role?
   - Does clicking the menu item load the correct page?
   - Does the page display the expected data (not an empty page with no explanation)?
   - Can the user perform the primary action (click button, submit form, view details)?
   - After the action, does the user see feedback (success toast, redirect, confirmation page)?
   - Can the user navigate back to the list/dashboard?

4. **Cross-role workflows**: If a feature spans roles (e.g., coordinator creates form → entrepreneur fills it), verify both sides of the workflow are reachable.

5. **Testability gap analysis (CRITICAL)**: Consider that the person running this audit or doing QA testing may only have ONE role (typically GlobalAdmin). For each workflow that requires a non-GlobalAdmin role, explicitly flag:
   - Which role is required to test this workflow
   - Whether GlobalAdmin can also access it (read-only oversight is common)
   - Whether the user needs to create test accounts with other roles to test the feature
   - If a multi-role test sequence is needed (e.g., "first act as coordinator, then as entrepreneur"), document the sequence and what accounts/contexts are needed

#### 10c. View & Form Completeness

For each view (.cshtml) in the audited scope:

1. **Empty state handling**: Does the page show a meaningful message when there's no data? (e.g., "No hay formularios diagnósticos" instead of a blank table)
2. **Action buttons exist and work**: If the spec says "user can clone a template", verify there's a visible button/link that triggers the action.
3. **Form → Submit → Redirect flow**: For every form:
   - Submit button exists and is visible
   - Form action points to the correct POST endpoint
   - After successful submission, user is redirected to a meaningful page (not a 404)
   - After validation failure, form redisplays with error messages (Spanish)
   - Success/error feedback is visible (TempData toast, validation summary)
4. **DataTable wiring**: For every DataTable page:
   - The table loads data on page load (not stuck on "Loading...")
   - Action columns (edit, view, delete) have working links
   - The URL placeholder pattern uses `Guid.Empty` (not string placeholders)
5. **Detail/Edit page links**: If a list page shows items, verify "View Details" links work and point to valid routes.

#### 10d. Role-Based Access & Session Context Verification

**Source of truth:** Constitution Principle X (Role Hierarchy & Session Context). Load and enforce those rules — do NOT redefine them here. The key compliance checks are:

1. **`[Authorize]` includes higher roles** (Constitution X): Every controller's `[Authorize(Roles = "...")]` MUST include all higher-privilege roles. GlobalAdmin must appear on every controller. IncubatorAdmin must appear on project-scoped controllers. **Flag as Critical** if any controller excludes a higher role.

2. **Menu includes higher roles** (Constitution X): `MenuConfiguration.cs` MUST include `"GlobalAdmin"` in every menu group's roles array. IncubatorAdmin must appear in project-scoped groups. **Flag as Critical** if any menu group excludes GlobalAdmin.

3. **No role with an empty menu**: If a role has controllers and views, it must have at least one navigable menu item.

4. **Consistent role naming**: Role strings in `[Authorize]` must match `PlatformRole` enum constants.

#### 10e. Session Context & Prerequisite Flow

Many features are **context-scoped** — they require an active incubator and/or project selected via `/Context/Select` (Constitution X: Session Context).

1. **Identify context requirements per controller**: Check if actions read claims like `ActiveProjectId`, `ActiveIncubatorId`. Classify each:
   - Global scope (no context needed — e.g., Platform area)
   - Incubator scope (needs ActiveIncubatorId — e.g., Administration area)
   - Project scope (needs ActiveIncubatorId + ActiveProjectId — e.g., Coordination, Participant areas)

2. **Context selection supports the role hierarchy**: Can higher roles reach the required contexts? Verify:
   - GlobalAdmin can select any incubator + project pair
   - IncubatorAdmin can select any project in their incubator
   - If the context selection mechanism doesn't support this, **flag as Critical**

3. **Missing context is handled gracefully**: Controllers MUST check for missing context claims and show a helpful message with a link to context selection — never crash or show a blank page with no explanation.

4. **Testability instructions**: For each context-scoped workflow, document the prerequisite steps:
   ```
   To test "Clone diagnostic form":
   1. Go to Context Selection (/Context/Select)
   2. Select a context with an active project
   3. Navigate to Coordinación → Diagnósticos
   ```
   Include these in the Testability Summary table.

5. **Seed data prerequisites**: Document what data must exist to test each workflow (templates, forms, etc.) and whether PostDeployment scripts provide it.

#### 10f. Platform Wiring (Program.cs)

1. **Bounded context is registered**: Both `Add{BC}Application(configuration)` and `Add{BC}Infrastructure(configuration)` are called in `Program.cs`.
2. **DbContext is registered**: The bounded context's DbContext appears in the DI container.
3. **Area routing is active**: `app.MapControllerRoute` with `{area:exists}` pattern is configured.
4. **Integration test factory includes the DbContext**: `MentooryWebApplicationFactory.ConfigureWebHost` replaces the DbContext for the new bounded context, and Respawner includes the new schema.

### Phase 11: Report Generation

Produce a structured report with these sections:

```markdown
## Gem Completion Audit: {Scope}

### Build Status
- [ ] Release build: 0 warnings, 0 errors

### Test Status  
- Total: X | Pass: X | Fail: X | Skip: X
- Coverage gaps: {list uncovered areas}

### Reachability Status (QA Readiness)
For each role affected by the scope, report whether the feature is reachable:

| Role | Menu Entry | Landing Page | Primary Action | Feedback | Verdict |
|------|-----------|--------------|----------------|----------|---------|
| GlobalAdmin | YES/NO | YES/NO | YES/NO | YES/NO | PASS/FAIL |
| IncubatorAdmin | YES/NO | YES/NO | YES/NO | YES/NO | PASS/FAIL |
| ProjectCoordinator | YES/NO | YES/NO | YES/NO | YES/NO | PASS/FAIL |
| Entrepreneur | YES/NO | YES/NO | YES/NO | YES/NO | PASS/FAIL |
| ... | ... | ... | ... | ... | ... |

### User Workflow Walkthrough
For each key workflow, trace the complete user journey:

| # | Workflow | Start | Steps | Breaks At | Status |
|---|----------|-------|-------|-----------|--------|
| 1 | {e.g., "Clone diagnostic form"} | Coordinator menu | Menu → List → Clone → Confirm | {step that fails or is missing} | PASS/FAIL |

### Findings

#### Critical (blocks functionality or unreachable)
| # | Task | File:Line | Finding | Impact |
|---|------|-----------|---------|--------|

#### High (partial functionality or partially reachable)
| # | Task | File:Line | Finding | Impact |
|---|------|-----------|---------|--------|

#### Medium (correctness or UX gap)
| # | Task | File:Line | Finding | Impact |
|---|------|-----------|---------|--------|

#### Low (quality or polish gap)
| # | Task | File:Line | Finding | Impact |
|---|------|-----------|---------|--------|

### Clarification Questions (MUST answer before fixing)

Before proposing fixes, list questions that require human judgment. **Do NOT assume the answer — ASK the user.** Common questions include:

- **Role access scope**: "Should GlobalAdmin have read-only access to [feature area] for oversight/testing? Currently only [specific role] can access it."
- **Cross-role testing**: "This feature requires [Role A] to create data and [Role B] to consume it. How do you plan to test? Do you have accounts for both roles, or should we add GlobalAdmin access?"
- **Intentional restrictions**: "The controller restricts access to [Role]. Is this intentional, or should [other role] also have access?"
- **Workflow completeness**: "The spec describes [workflow], but the UI only implements [subset]. Is the rest planned for a later phase, or should it be part of this fix?"
- **Empty state behavior**: "When [entity] has no data, should the user see [option A] or [option B]?"

**Why this matters:** Fixing reachability issues often involves changing authorization rules, which is a design decision — not a bug fix. The skill must surface these as questions, not silently make assumptions about who should access what.

Format each question with enough context that the user can answer with a short directive:
```
Q1: GlobalAdmin cannot test the diagnostic clone/submit workflows because those controllers
    require ProjectCoordinator and Entrepreneur roles respectively. Should we:
    (a) Add GlobalAdmin to the authorized roles on those controllers (read-only oversight)?
    (b) Keep the role restriction — you'll test with separate accounts?
    (c) Something else?
```

### Testability Summary

For each workflow in the scope, state what is needed to test it end-to-end:

| Workflow | Required Role | Required Context | Context Selection Steps | Seed Data Needed | Can GlobalAdmin Test? |
|----------|--------------|------------------|------------------------|------------------|----------------------|
| View templates | GlobalAdmin | None (global) | Select GlobalAdmin context | Templates in DB | YES |
| Clone diagnostic form | ProjectCoordinator+ | Incubator + Project | 1. Context Selection → pick incubator → pick project as Coordinator | At least 1 form template | YES if GlobalAdmin is authorized on controller |
| Submit diagnostic | Entrepreneur+ | Incubator + Project | 1. Context Selection → pick incubator → pick project as Entrepreneur | Cloned form with questions | YES if GlobalAdmin is authorized on controller |

"+" means higher-privilege roles (IncubatorAdmin, GlobalAdmin) should also have access.

This section helps the user (or QA) know BEFORE they start testing what accounts, roles, and seed data they need.

### Action Plan
Priority-ordered list of fixes. Each item includes:
1. What to do (specific file and change)
2. Why it matters (what breaks without it)
3. Done criteria (how to verify the fix works)
4. Estimated effort (S/M/L)
5. Dependencies (what must be fixed first)

**IMPORTANT:** If any Action Plan item depends on an unanswered Clarification Question, mark it as `[BLOCKED: Q#]` and do NOT include it in "fix" execution until the user answers.

### Prevention Recommendations
What process changes would catch these issues earlier.
```

## Post-Report Actions

If the user says "fix", "remediate", or "go ahead":
1. **Check for unanswered Clarification Questions** — if any exist, present them FIRST and wait for answers before proceeding.
2. Work through the Action Plan in priority order (Critical first), skipping `[BLOCKED]` items.
3. After each fix, run `dotnet build` to verify zero warnings.
4. After all fixes, run `dotnet test` to verify all tests pass.
5. Re-run the audit on the fixed scope to confirm findings are resolved.
6. Run `/simplify` on the changed files.

If the user says "plan only" or doesn't request fixes:
1. Output the report and stop.
2. The user can invoke this skill again with "fix {finding-number}" to address specific items.
