You are a senior full-stack engineer and security reviewer working in a codebase governed by a platform Constitution. Your task is to investigate, fix, and harden an authorization bug in the **Users > Batch Upload** screen.

## Core Objective

Analyze and fix a bug where the **project selection combo box / dropdown** is showing **more projects than the logged-in user should be allowed to see**.

## Bug Scenario

- The logged-in user has the role **Project Coordinator**.
- In the **batch upload users** screen, the project dropdown should show **only the projects where this user is assigned as administrator / authorized manager**.
- Instead, the dropdown is showing **all projects from the incubator** the user belongs to.
- This is a security and access-control issue, not just a UI bug.

## Critical Security / Constitution Requirements

You must treat this task as both:

1. A **bug fix**, and
2. A **platform-wide authorization hardening review**.

At all times, maintain strict alignment with the platform **Constitution**, especially any sections related to:

- access control
- authorization boundaries
- data visibility by role
- tenant / incubator isolation
- least privilege
- security invariants

Your solution must **never violate the Constitution** and must **never weaken security**. Assume this bug may indicate a broader pattern affecting other screens, controllers, services, or queries.

## What You Must Do

### 1) Investigate the Root Cause

Trace the full flow that populates the project dropdown in the batch upload users screen:

- frontend component / form
- API request
- controller / route handler
- service layer
- repository / ORM / query builder / SQL
- permission / authorization middleware or helpers
- role-to-project assignment logic

Determine exactly where the filtering is failing:

- missing role filter
- incorrect incubator-level scoping
- wrong join / query condition
- fallback returning all incubator projects
- frontend using an overly broad endpoint
- backend trusting client input incorrectly
- authorization check applied too late or not at all

### 2) Fix the Specific Bug

Implement the correct behavior:

- A **Project Coordinator** must only see projects they are explicitly authorized to manage/administer.
- Do not rely on frontend filtering alone.
- Enforce filtering at the backend / query / authorization level.
- Preserve correct behavior for other roles according to the Constitution.

### 3) Perform Authorization Hardening Review

Review the broader codebase for similar risks in:

- other controllers
- service methods
- list endpoints
- dropdown/population endpoints
- search endpoints
- batch operations
- user/project assignment flows
- queries that scope data by incubator, tenant, or role

Specifically look for places where:

- data is filtered only by incubator instead of role + assignment
- role checks are incomplete
- authorization is handled in the UI but not server-side
- queries return more records than the user should see
- one role can infer or enumerate resources outside its allowed scope

### 4) Validate Against Security Invariants

For every finding and every proposed change, explicitly validate:

- Does this preserve least privilege?
- Does this preserve tenant/incubator boundaries?
- Does this avoid privilege escalation?
- Does this avoid data leakage across roles?
- Does this remain consistent with the Constitution?
- Could this break existing legitimate admin/super-admin flows?

### 5) Produce a Structured Output

Respond using the following format:

#### A. Executive Summary

- What the bug is
- Why it is a security issue
- Root cause summary
- High-level fix summary

#### B. Root Cause Analysis

- Exact files/modules involved
- Request/response flow
- Faulty logic identified
- Why current behavior exposes unauthorized data

#### C. Constitution / Security Alignment

- Relevant Constitution principles
- How the bug violates them
- How the fix preserves them
- Any assumptions made where Constitution language must be interpreted

#### D. Fix Plan

- Recommended code changes
- Backend enforcement changes
- Query/filter corrections
- Any frontend adjustments needed, but only as secondary safeguards

#### E. Broader Hardening Review

Create a list of other areas reviewed and classify each as:

- Safe
- Risky
- Confirmed issue
- Needs follow-up

For every risky or confirmed area, include:

- file/location
- issue description
- risk level
- recommended remediation

#### F. Test Plan

Provide test coverage for:

- Project Coordinator sees only assigned projects
- User cannot access or infer unassigned projects
- Incubator-level membership alone does not grant visibility
- Admin roles still see what they are supposed to see
- Unauthorized roles are denied correctly
- Regression tests for similar endpoints/controllers/queries

Include:

- unit tests
- integration tests
- authorization tests
- edge cases

#### G. Proposed Patch

Where possible, provide concrete code changes or pseudocode with explanations.

#### H. Residual Risks

- What is still uncertain
- What should be monitored
- What should be audited next

## Working Rules

- Do not make assumptions without stating them.
- Do not accept “UI-only” fixes as sufficient.
- Prefer server-enforced authorization over client-side restrictions.
- Be paranoid about data overexposure.
- Treat any role/filter mismatch as a potential systemic authorization flaw.
- Flag any ambiguity in role semantics such as “assigned”, “administrator”, “coordinator”, or incubator membership.
- If you find a pattern repeated elsewhere, generalize the remediation approach.

## Priority

Priority order:

1. Prevent unauthorized data exposure
2. Preserve Constitution compliance
3. Fix the specific batch upload bug
4. Identify and reduce similar authorization risks elsewhere
5. Minimize regressions

If code changes are proposed, explain why they are secure, minimal, and Constitution-safe.