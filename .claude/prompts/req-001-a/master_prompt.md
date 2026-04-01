# Addendum Specification — Controlled Pause for Validation, Correction, and Production Readiness (Phase 1–4)

## Purpose

This document is an **addendum** to the current macro implementation effort for the **Mentory** platform.

This is a **controlled pause** on the ongoing work to:

1. isolate corrective work,
2. validate that everything implemented up to Phase 4 behaves correctly,
3. incorporate identified corrections,
4. detect unknown issues not yet identified,
5. enforce production-level quality,
6. establish both automated and manual verification mechanisms.

This is NOT a new feature phase.
This is NOT a continuation of the roadmap.

This is a **production-readiness checkpoint applied to Phases 1–4**.

---

## 🔴 Critical Directive

You must treat the current system as if:

> **Each completed phase (Phase 1 → Phase 4) should already be independently production-ready.**

This means:

- No broken flows
- No ambiguous behavior
- No partial implementations
- No missing validation
- No inconsistent UX
- No weak authorization
- No undefined edge cases

If any of the above exist, they must be identified and corrected.

---

## ⚠️ Important Clarification

The feedback provided in this addendum includes **specific known issues** (e.g., role behavior, context handling, dashboards).

However:

> **These are NOT the only issues.**

You MUST NOT limit your analysis to only the explicitly described problems.

You are required to perform a **deep and critical analysis of the entire implemented scope (Phase 1–4)** and identify:

- Issues not mentioned
- Incomplete implementations
- Weak or fragile designs
- Missing validation
- Incorrect assumptions
- UX inconsistencies
- Security risks
- Gaps between spec and behavior

Think beyond what was reported.

---

## Scope Boundary

This addendum applies ONLY to:

- Phase 1 (Setup + Foundational)
- Phase 3 (User Management, Auth, Context)
- Phase 4 (Diagnostic Module)

Do NOT move into future phases.

---

## 🧠 Expected Mental Model

You must act as if:

- The system is about to be released to production in increments
- Each phase must be stable and reliable on its own
- There is no tolerance for “we’ll fix it later”

---

## Required Workstreams

---

## 1. Branching Strategy (Isolation)

- Create a **dedicated correction branch**
- Derived from the current working branch
- Must clearly represent:
  - pause
  - validation
  - correction
  - hardening

This branch is:
- NOT a new feature branch
- NOT disconnected from the current work
- It is a **controlled correction layer**

---

## 2. Dual Analysis Requirement

You must perform TWO types of analysis:

### A. Directed Corrections (Known Issues)

These are explicitly provided and MUST be addressed:

- GlobalAdmin behavior
- Platform vs Administration separation
- Role-based dashboards
- Context selection flow
- Context enforcement
- Context switching UX
- Safe return navigation

### B. Undirected Deep Analysis (Unknown Issues)

You must analyze EVERYTHING else implemented in Phase 1–4 and answer:

- What is not production-ready?
- What is incomplete?
- What is inconsistent?
- What would fail under real usage?
- What is fragile or unclear?
- What violates the constitution?
- What is missing from a real production system?

This is the most important part.

---

## 3. Production Readiness Criteria

For EACH phase and EACH major capability, evaluate:

### Functional Completeness
- Does it fully work end-to-end?

### UX Consistency
- Is behavior predictable and clear?

### Security
- Is authorization correctly enforced?
- Is context respected strictly?

### Error Handling
- Are failures handled properly?
- Are messages clear?

### Edge Cases
- Are edge conditions handled?

### Data Integrity
- Is data safe and consistent?

### Observability
- Can behavior be validated?

If ANY of these fail → it is NOT production-ready.

---

## 4. Role, Dashboard, and Context Corrections (Directed)

You MUST enforce:

### Login Routing
- Redirect by role

### GlobalAdmin
- Always lands in Platform dashboard
- Sees all menu options

### Context Selection Order
1. Role
2. Incubator
3. Project

### Strict Context Enforcement
- Only active context is visible and usable

### Context Switching
- Top-bar access
- Safe return
- Dashboard fallback if needed

---

## 5. Automated Verification (MANDATORY)

For EVERY corrected or reviewed item:

You MUST define a **Playwright test**.

No exceptions.

This includes:

- Login flows
- Role selection
- Context selection
- Dashboard routing
- Menu visibility
- Authorization boundaries
- Context switching
- Page rendering with data

The goal:

> Every important behavior must be executable and verifiable automatically.

---

## 6. Seed Data Strategy (MANDATORY)

Define deterministic seed data to support:

- Playwright tests
- Manual testing

Must include:

- GlobalAdmin user
- Multi-role users
- Multi-incubator users
- Multi-project users
- Real data for pages (users, projects, etc.)

All tests must rely on known data.

---

## 7. Manual Validation Playbooks (MANDATORY)

For each correction group:

Provide a **step-by-step manual validation guide**:

- Preconditions
- User to log in with
- Steps
- Expected results
- Pass/Fail criteria

Goal:

> A non-developer can validate correctness.

---

## 8. Refactoring Plan

Provide an **incremental plan**:

- No big rewrites
- Safe changes
- Ordered steps
- Validation after each step

---

## 9. Task List

Create a checklist including:

- Corrections
- Refactors
- Playwright tests
- Seed data
- Playbooks

---

## 10. Exit Criteria

Define when the pause is complete:

The system can only resume development when:

- All identified issues are resolved
- All Playwright tests pass
- Manual playbooks validate correctly
- Behavior is deterministic
- Each phase is production-ready

---

## Mindset

You are NOT continuing development.

You are:

- auditing
- correcting
- hardening
- validating

This is a **quality gate before continuing the roadmap**.

Be critical.
Be exhaustive.
Assume there are hidden issues.
Find them.
Fix them.
Prove they are fixed.