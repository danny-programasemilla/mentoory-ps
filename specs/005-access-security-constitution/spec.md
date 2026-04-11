# Feature Specification: Access & Security Constitution

**Feature Branch**: `005-access-security-constitution`  
**Created**: 2026-04-08  
**Status**: Draft  
**Input**: User description: "Foundational governance document for role hierarchy, scope boundaries, and permission enforcement"

## Clarifications

### Session 2026-04-08

- Q: Where should the access constitution document live and how should it relate to the existing constitution.md? → A: Separate file in `.specify/memory/` linked from `constitution.md`, with independent versioning.
- Q: Should the document use "ProjectCoordinator" (codebase) or "Project Admin" (business requirements) as the canonical term? → A: `ProjectCoordinator` is canonical. "Project Admin" is a deprecated business alias, noted once in the glossary.
- Q: How should the developer checklist be enforced in practice? → A: Dual enforcement — PR template with required access section for ad-hoc changes; SpecKit checklist auto-generated via `/speckit.checklist` for planned features. Reviewers reject PRs with incomplete access sections.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Engineering Lead Consults Access Rules Before Feature Design (Priority: P1)

An engineering lead is about to design a new feature that involves user data scoped to a specific project. Before writing any code or spec, they open the Access & Security Constitution to determine: which roles can access the feature, what scope boundaries apply, and what backend enforcement is required. The constitution provides unambiguous answers so the lead can design the feature correctly from the start.

**Why this priority**: Without a single authoritative reference for access rules, teams repeatedly make inconsistent authorization decisions, leading to security gaps and rework. This is the core value of the entire document.

**Independent Test**: Can be fully tested by presenting a hypothetical feature scenario to an engineer and verifying they can answer all access questions using only the constitution document.

**Acceptance Scenarios**:

1. **Given** the constitution is published, **When** an engineer looks up "who can create diagnostic forms," **Then** the document provides a definitive answer with role, scope, and constraints clearly stated.
2. **Given** the constitution is published, **When** an engineer looks up a role's "must never" restrictions, **Then** the document lists explicit prohibitions for that role.
3. **Given** the constitution is published, **When** an engineer needs to know which authorization model the platform uses, **Then** the document states the confirmed model with rationale.

---

### User Story 2 - Developer Uses Feature Design Checklist Before Shipping (Priority: P1)

A backend developer is implementing a new endpoint. Before submitting for review, they work through the Secure Feature Design Checklist embedded in the constitution. The checklist forces them to answer: what role performs this action, what scope applies, what backend checks enforce it, and what tests prove the security boundary. The developer cannot skip these questions.

**Why this priority**: The prompt explicitly identifies that teams are not consistently asking access-related questions during development. A mandatory checklist directly addresses this recurring failure.

**Independent Test**: Can be tested by having a developer walk through the checklist for a real feature and verifying every question is answerable and produces actionable guidance.

**Acceptance Scenarios**:

1. **Given** the checklist exists in the constitution, **When** a developer implements a project-scoped endpoint, **Then** the checklist requires them to declare the target role, scope, backend enforcement, audit events, and security tests.
2. **Given** the checklist exists, **When** a developer skips a checklist item, **Then** the item's absence is detectable during code review (the checklist is structured as a gate).
3. **Given** a planned feature goes through SpecKit, **When** `/speckit.checklist` runs, **Then** the access checklist is auto-generated from the constitution's workflow section.
4. **Given** an ad-hoc change bypasses SpecKit, **When** the developer opens a PR, **Then** the PR template includes a required access section derived from the constitution's checklist.

---

### User Story 3 - QA Engineer Designs Authorization Test Cases (Priority: P1)

A QA engineer needs to write test cases that verify no user can access data outside their assigned scope. They consult the constitution's Testing and Verification Requirements section and the Permission Matrix to derive positive authorization tests, negative authorization tests, cross-scope isolation tests, and multi-role tests.

**Why this priority**: Without a formalized permission matrix and testing requirements, QA cannot systematically verify access boundaries. This directly prevents broken access control, IDOR, and cross-tenant leakage.

**Independent Test**: Can be tested by having a QA engineer derive a complete set of authorization test cases from the constitution alone, covering all roles and scope levels.

**Acceptance Scenarios**:

1. **Given** the permission matrix is published, **When** a QA engineer reads the row for "view entrepreneur data," **Then** the matrix specifies which roles can perform it, at what scope, and with what constraints.
2. **Given** the testing requirements section exists, **When** a QA engineer plans test coverage, **Then** the section defines mandatory test categories: positive, negative, cross-scope, multi-role, assignment change, and audit verification.

---

### User Story 4 - Security Reviewer Audits Threat Model (Priority: P2)

A security reviewer (internal or external) needs to assess the platform's access control posture. They consult the Threat and Failure Analysis section to understand known risk categories (IDOR, BOLA, cross-tenant leakage, stale permissions, role escalation) and the required safeguards for each.

**Why this priority**: A structured threat model tied to the platform's specific multi-tenant hierarchy is essential for security audits and compliance, but is secondary to the day-to-day developer and QA workflows.

**Independent Test**: Can be tested by verifying that the threat analysis covers all OWASP Broken Access Control sub-categories relevant to a multi-tenant incubator platform.

**Acceptance Scenarios**:

1. **Given** the threat analysis section exists, **When** a reviewer looks up "cross-incubator data leakage," **Then** the document explains why it matters, lists typical implementation mistakes, and specifies required safeguards.
2. **Given** the threat analysis section exists, **When** a reviewer looks up "role escalation during user creation," **Then** the document describes the attack vector and required validation.

---

### User Story 5 - Product Owner Validates Feature Scope Against Role Catalog (Priority: P2)

A product owner is defining requirements for a new feature. They consult the Role Catalog to understand each role's business purpose, allowed scope, operational capabilities, and restrictions. This informs which roles should have access to the new feature and prevents scope creep.

**Why this priority**: Product decisions that ignore role boundaries create features that are difficult or impossible to secure. The role catalog ensures product and engineering share a common understanding.

**Independent Test**: Can be tested by having a product owner use the role catalog to determine which roles should access a hypothetical new feature, and verifying the answer is consistent with the platform's hierarchy.

**Acceptance Scenarios**:

1. **Given** the role catalog exists, **When** a product owner reads the Incubator Admin entry, **Then** it clearly states: business purpose, allowed scope, user creation capabilities, operational capabilities, restrictions, and "must never" statements.
2. **Given** the role catalog exists, **When** a product owner reads the Mentor entry, **Then** open questions about unresolved permissions are explicitly listed rather than assumed.

---

### User Story 6 - New Contributor Understands the Access Model (Priority: P3)

A new contributor joins the project and needs to understand the platform's authorization architecture. They read the Executive Summary and Confirmed vs. Inferred Model sections to quickly grasp: the role hierarchy, the authorization model type, data boundaries, and the enforcement approach.

**Why this priority**: Onboarding efficiency matters but is a secondary benefit of the document. The primary value is governance, not education.

**Independent Test**: Can be tested by having a new contributor read the first two sections and then correctly answer basic questions about which roles exist, how scope works, and what authorization model is used.

**Acceptance Scenarios**:

1. **Given** the executive summary exists, **When** a new contributor reads it, **Then** they can identify the six roles, the tree-shaped scope model, and the primary security risks.
2. **Given** the Confirmed vs. Inferred Model section exists, **When** a new contributor reads it, **Then** they can distinguish confirmed facts from inferences and recommendations.

---

### Edge Cases

- What happens when the constitution references a role permission that has not yet been decided (e.g., Mentor or Entrepreneur edge cases)? The document must explicitly mark these as open questions, not assumptions.
- How does the document handle the gap between the prompt's "Project Admin" terminology and the codebase's "ProjectCoordinator" role name? The document must reconcile and clarify the canonical terminology.
- What happens when new roles are added to the platform in the future? The permission matrix and role catalog formats must be extensible without restructuring.
- How does the document handle users with multiple role assignments across different incubators? The scope model section must address multi-assignment implications.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Document MUST contain an Executive Summary that describes the current access structure, major security goals, primary risks of inconsistent access control, and the purpose of the constitution.
- **FR-002**: Document MUST contain a Confirmed Model vs. Inferred Model section that separates confirmed facts (from the codebase and prompt) from inferred assumptions and recommendations.
- **FR-003**: Document MUST explicitly determine the authorization model type (hierarchical RBAC with scoped tenancy) with rationale tied to the platform's multi-tenant incubator structure.
- **FR-004**: Document MUST contain an Access & Security Constitution section with non-negotiable rules phrased as durable governance statements (e.g., "all functionality must be classified before implementation").
- **FR-005**: Document MUST contain a Domain Scope Model section that defines: platform scope, incubator scope, project scope, mentor relationship scope, and entrepreneur relationship scope — including downward visibility, prohibited lateral access, prohibited upward access, and multi-assignment implications.
- **FR-006**: Document MUST contain a Role Catalog with entries for all six roles (GlobalAdmin, IncubatorAdmin, ProjectCoordinator, Mentor, Entrepreneur, Sponsor), each specifying: business purpose, allowed scope, high-risk permissions, user creation capabilities, operational capabilities, restrictions, and "must never" statements.
- **FR-007**: For roles with incomplete permission definitions (Mentor, Entrepreneur, Sponsor), the Role Catalog MUST clearly separate known facts from implied behaviors and list open questions.
- **FR-008**: Document MUST contain a Sustainable Permission Matrix with at minimum these actions: create incubator, create user, assign role, assign user to incubator, assign user to project, view incubator, update incubator, create project, view project, update project, create diagnostic form, edit diagnostic form, publish diagnostic form, assign mentor to project, assign mentor to entrepreneur, view entrepreneur data, manage platform settings.
- **FR-009**: The Permission Matrix format MUST include columns for: capability/action, resource, platform category flag, each role's access level, allowed scope, constraints, audit requirement, and notes/open questions.
- **FR-010**: Document MUST contain a Security Design Principles section covering: least privilege, deny by default, explicit scope validation, backend enforcement as source of truth, defense in depth, separation of duties, auditability, secure onboarding/identity verification, secure role assignment/reassignment, and secure deprovisioning/offboarding.
- **FR-011**: Document MUST contain a Threat and Failure Analysis section covering at minimum: broken access control, IDOR/BOLA, trusting frontend role filtering, incorrect scope joins in queries, role escalation during user creation, cross-incubator data leakage, cross-project data leakage, mentor seeing unauthorized entrepreneur data, stale permissions after reassignment, soft-deleted/archived entity leakage, and unauthorized access via exports/reports/notifications/search/background jobs.
- **FR-012**: Each threat entry MUST include: why it matters for this platform, typical implementation mistakes, and required safeguards.
- **FR-013**: Document MUST contain an Enforcement Model Recommendations section addressing: backend authorization architecture, route/endpoint guards, service-layer policy checks, query-layer scope enforcement, database considerations, token/session claims, role-to-scope resolution, user creation/assignment validation, audit logs, testing strategy, and admin action traceability.
- **FR-014**: Document MUST contain a Secure Feature Design Workflow section with a mandatory checklist that developers must complete before shipping any feature, including questions about: business capability, permitted roles, applicable scope, feature category (platform/incubator/project), impact on users/permissions/visibility, backend enforcement, audit events, and security boundary tests. Enforcement is dual: (1) a PR template with a required access section that reviewers reject if incomplete, for ad-hoc changes; (2) SpecKit checklist auto-generated via `/speckit.checklist` for planned features.
- **FR-015**: Document MUST contain a Testing and Verification Requirements section defining mandatory test categories: positive authorization, negative authorization, cross-scope isolation, multi-role, assignment change, regression, UI hiding vs. backend enforcement, and audit logging verification.
- **FR-016**: Document MUST contain an Open Questions / Decisions Needed section listing unresolved items for: Mentor permissions, Entrepreneur permissions, Sponsor permissions, whether Incubator Admin can create projects, whether ProjectCoordinator can create entrepreneurs directly, whether users can have multiple roles across scopes, impersonation/support access, archived entity access, notification visibility rules, and reporting/export permissions.
- **FR-017**: Document MUST use "ProjectCoordinator" as the canonical term throughout. "Project Admin" MUST be noted once as a deprecated business alias in a glossary/terminology section, then never used as a primary label.
- **FR-018**: Document MUST be structured as a constitution-ready artifact suitable for linking from `constitution.md`, with a concise appendix containing the initial permission matrix, a developer checklist, and a short list of immediate next actions.
- **FR-019**: Document MUST be written in English with the tone of a senior security architect writing an internal governance artifact.

### Key Entities

- **Role**: A named access level (GlobalAdmin, IncubatorAdmin, ProjectCoordinator, Mentor, Entrepreneur, Sponsor) with associated permissions and scope constraints.
- **Scope**: A hierarchical boundary (Platform > Incubator > Project) that restricts data visibility and action eligibility.
- **Permission**: A specific capability (e.g., ManageIncubators, ManageDiagnostics) granted to a role within a scope.
- **RoleAssignment**: The binding of a user to a role within a specific incubator and optionally a specific project.
- **Permission Matrix**: A structured table mapping actions to roles, scopes, constraints, and audit requirements.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Any engineer can determine the permitted roles and scope for any existing platform feature by consulting only the constitution document, without asking another team member — achievable for 100% of currently implemented features.
- **SC-002**: The developer checklist covers all access-relevant questions such that a code reviewer can verify checklist completion in under 5 minutes per feature.
- **SC-003**: The permission matrix covers all 17 actions specified in the requirements and can be extended with a new action in under 2 minutes by adding a single row.
- **SC-004**: The threat analysis addresses all 11 risk categories from the requirements, each with platform-specific safeguards (not generic advice).
- **SC-005**: A QA engineer can derive at least 3 test cases per permission matrix row by reading the matrix and testing requirements section.
- **SC-006**: The document distinguishes confirmed facts from inferences with zero ambiguity — every statement is explicitly tagged as confirmed, inferred, or recommended.
- **SC-007**: The role catalog's open questions section for under-specified roles (Mentor, Entrepreneur, Sponsor) contains at least 3 actionable questions per role that must be resolved before implementing role-specific features.

## Assumptions

- The existing codebase's `PlatformRole` enum (GlobalAdmin, IncubatorAdmin, ProjectCoordinator, Mentor, Entrepreneur, Sponsor) represents the canonical role set. "ProjectCoordinator" is the confirmed canonical term; "Project Admin" is a deprecated business alias.
- The existing `Permission` enum and `CheckPermissionHandler` role-permission mapping represent the current intended permission model, even if not fully enforced across all endpoints.
- The `RoleAssignment` aggregate's structure (UserId + IncubatorId + optional ProjectId + Role) confirms that the platform uses hierarchical RBAC with scoped tenancy.
- The `TenantContextMiddleware` and `ITenantContext` service confirm that incubator-level tenant isolation is already partially implemented in the middleware layer.
- The constitution document will be stored at `.specify/memory/access-security-constitution.md` as a separate governance artifact linked from `constitution.md`, with its own independent versioning scheme.
- The Sponsor role exists in the codebase but was not mentioned in the original prompt requirements. The document will include it as a known role with limited definition.
- The document does not need to prescribe specific framework code (e.g., exact C# attribute syntax) but should be implementation-aware enough to guide developers working on this platform.
- Multi-role scenarios (e.g., a user who is IncubatorAdmin in one incubator and Mentor in another) are supported by the current `RoleAssignment` model and must be addressed in the scope model.
