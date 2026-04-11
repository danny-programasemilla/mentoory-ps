# Tasks: Access & Security Constitution

**Input**: Design documents from `/specs/005-access-security-constitution/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md

**Tests**: Not requested — no test tasks generated.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story. This is a documentation artifact — all tasks produce Markdown content, not application code.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files or independent document sections)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Primary deliverable**: `.specify/memory/access-security-constitution.md`
- **Constitution link**: `.specify/memory/constitution.md`
- **PR template**: `.github/PULL_REQUEST_TEMPLATE.md`

---

## Phase 1: Setup

**Purpose**: Create document scaffold and establish structure

- [x] T001 Create document scaffold at `.specify/memory/access-security-constitution.md` with Sync Impact Report HTML comment block (matching `constitution.md` pattern), document metadata (version 1.0.0, date, status), and all 13 section headings as empty placeholders per the hierarchy in `specs/005-access-security-constitution/data-model.md`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core sections that ALL user stories depend on — establishes facts, model determination, and governance rules referenced by every subsequent section

**CRITICAL**: No user story work can begin until this phase is complete

- [x] T002 Write Section 1 — Executive Summary in `.specify/memory/access-security-constitution.md`. Cover: current 6-role hierarchy (GlobalAdmin through Sponsor), hierarchical RBAC with scoped tenancy model, tree-shaped scope boundaries (Platform > Incubator > Project), primary risks (inconsistent authorization, cross-tenant leakage, role escalation), and purpose of the constitution. Source: research.md R1, R2. Tag all statements per data-model.md tagging model.
- [x] T003 Write Section 2 — Confirmed Model vs Inferred Model in `.specify/memory/access-security-constitution.md`. Organize into three subsections: (a) Confirmed facts from codebase — PlatformRole enum, Permission enum, CheckPermissionHandler mapping, RoleAssignment aggregate structure, TenantContextMiddleware, [Authorize] patterns (source: research.md R1-R7); (b) Inferred assumptions — dual-binding model (RoleAssignment + ProjectParticipant), permission model not fully enforced across all endpoints, Sponsor role under-specified; (c) Recommended formal model — hierarchical RBAC with scoped tenancy, rationale per research.md R2. Every statement tagged [CONFIRMED], [INFERRED], or [RECOMMENDED].
- [x] T004 Write Section 3 — Access & Security Constitution Rules in `.specify/memory/access-security-constitution.md`. Write numbered non-negotiable governance statements covering: (1) all functionality classified before implementation, (2) every endpoint defines actor/action/resource/scope, (3) platform-category features GlobalAdmin only, (4) no user may access data outside assigned scope, (5) frontend visibility never replaces backend authorization, (6) role inheritance must be explicit in authorize attributes, (7) user creation constrained by creator's scope, (8) all high-risk actions auditable, (9) permission changes require re-authentication or session refresh, (10) open questions block implementation until resolved. Source: FR-004, spec edge cases.
- [x] T005 Write Glossary/Terminology subsection at end of Section 5 placeholder in `.specify/memory/access-security-constitution.md`. Define canonical terms: ProjectCoordinator (canonical, formerly "Project Admin" — deprecated alias), GlobalAdmin, IncubatorAdmin, Mentor, Entrepreneur, Sponsor, Scope, RoleAssignment, Permission, Permission Matrix. Source: FR-017, clarification session.

**Checkpoint**: Foundation ready — sections 1-3 and glossary establish the baseline for all user story sections.

---

## Phase 3: User Story 1 — Engineering Lead Consults Access Rules (Priority: P1) MVP

**Goal**: An engineer can look up any role's permissions, scope boundaries, and constraints using only the constitution document.

**Independent Test**: Present a hypothetical feature scenario (e.g., "who can create diagnostic forms?") and verify the document provides a definitive answer with role, scope, and constraints.

### Implementation for User Story 1

- [x] T006 [US1] Write Section 4 — Domain Scope Model in `.specify/memory/access-security-constitution.md`. Define five scope levels: (1) Platform scope — system-wide, GlobalAdmin only; (2) Incubator scope — data within one incubator; (3) Project scope — data within one project inside an incubator; (4) Mentor relationship scope — mentor's assigned entrepreneurs within a project; (5) Entrepreneur relationship scope — entrepreneur's own data within a project. Document: downward visibility (higher scope sees lower), prohibited lateral access (no cross-incubator, no cross-project), prohibited upward access (lower roles cannot see higher-scope data), multi-assignment implications (user with roles in multiple incubators sees union of their scopes, never cross-scope). Source: research.md R2, R4, R7, FR-005.
- [x] T007 [US1] Write Section 5 — Role Catalog entries for GlobalAdmin and IncubatorAdmin in `.specify/memory/access-security-constitution.md`. Each entry follows the schema from data-model.md: business purpose, allowed scope, high-risk permissions, user creation capabilities, operational capabilities, restrictions, "must never" statements. GlobalAdmin: all permissions, platform scope, can create any user/role/incubator. IncubatorAdmin: ManageProjects, ManageIncubatorUsers, EnrollParticipants + project-level permissions, own incubator scope only, can create IncubatorAdmin/ProjectCoordinator/Mentor/Entrepreneur within own incubator. Source: research.md R1, R3, R5, spec FR-006.
- [x] T008 [P] [US1] Write Section 5 — Role Catalog entry for ProjectCoordinator in `.specify/memory/access-security-constitution.md`. Allowed scope: assigned project(s) within incubator. Permissions: ManageDiagnostics, ManageKnowledge, ManageLifecycle, ManageProjectParticipants, ViewProjectProgress. Can define/manage diagnostic forms. Must never: access data outside assigned projects, create incubators, manage platform settings. Source: research.md R3, R5, spec FR-006.
- [x] T009 [P] [US1] Write Section 5 — Role Catalog entries for Mentor, Entrepreneur, and Sponsor in `.specify/memory/access-security-constitution.md`. Each entry: known facts from CheckPermissionHandler (research.md R3), implied behaviors, and minimum 3 open questions per role per FR-007/SC-007. Mentor: ManageMentoringPlans, ManageSessions, ManageAssignments, CorrectAnswers, ViewProjectProgress. Entrepreneur: CompleteDiagnostic, ViewMentoringPlan, SubmitAssignments. Sponsor: ViewProjectProgress (read-only). Tag each fact as [CONFIRMED] or [INFERRED]; open questions as [OPEN].
- [x] T010 [US1] Write Section 6 — Sustainable Permission Matrix in `.specify/memory/access-security-constitution.md`. (a) Explain matrix format and why it is sustainable (one row per action, extensible by adding rows). (b) Build initial matrix with 17+ rows per FR-008 using column schema from data-model.md (Action, Resource, Platform?, GA, IA, PC, M, E, S, Scope, Constraints, Audit, Notes). Actions: create incubator, create user, assign role, assign user to incubator, assign user to project, view incubator, update incubator, create project, view project, update project, create diagnostic form, edit diagnostic form, publish diagnostic form, assign mentor to project, assign mentor to entrepreneur, view entrepreneur data, manage platform settings. Source: research.md R3, R5, spec FR-008/FR-009.

**Checkpoint**: User Story 1 complete — an engineer can consult scope model, role catalog, and permission matrix to answer any access question for current features.

---

## Phase 4: User Story 2 — Developer Uses Feature Design Checklist (Priority: P1)

**Goal**: Developers have a mandatory checklist to complete before shipping any feature, enforced through PR template and SpecKit integration.

**Independent Test**: Have a developer walk through the checklist for a real project-scoped endpoint and verify every question produces actionable guidance.

### Implementation for User Story 2

- [x] T011 [US2] Write Section 10 — Secure Feature Design Workflow in `.specify/memory/access-security-constitution.md`. Write mandatory checklist with questions: (1) What business capability is being added? (2) Which role(s) may perform it? (3) What scope applies (platform/incubator/project)? (4) Does it belong to platform, incubator, or project category? (5) Can the action affect users, permissions, or visibility? (6) What backend checks enforce it? (7) What audit events are required? (8) What tests prove security boundaries? Include enforcement note: dual mechanism — PR template for ad-hoc changes, SpecKit `/speckit.checklist` for planned features. Source: FR-014, clarification session.
- [x] T012 [P] [US2] Create `.github/PULL_REQUEST_TEMPLATE.md` with access checklist section. Include markdown checkboxes mirroring the 8 questions from Section 10. Add header: "## Access & Security Checklist (Required)". Add instruction: "Complete all items below. PRs with incomplete access sections will be rejected during review. Reference: `.specify/memory/access-security-constitution.md` Section 10." If file already exists, add section without overwriting existing content. Source: FR-014, clarification session.

**Checkpoint**: User Story 2 complete — developers have a mandatory checklist in the constitution and a PR template that enforces it.

---

## Phase 5: User Story 3 — QA Designs Authorization Test Cases (Priority: P1)

**Goal**: QA engineers can derive comprehensive authorization test cases from the constitution's testing requirements and permission matrix.

**Independent Test**: Have a QA engineer derive at least 3 test cases per permission matrix row using only the constitution.

### Implementation for User Story 3

- [x] T013 [US3] Write Section 11 — Testing and Verification Requirements in `.specify/memory/access-security-constitution.md`. Define 8 mandatory test categories with descriptions and examples tied to the platform: (1) Positive authorization — verify permitted role+scope combinations succeed; (2) Negative authorization — verify denied role+scope combinations return 403; (3) Cross-scope isolation — verify IncubatorAdmin in incubator A cannot see incubator B data; (4) Multi-role — verify user with roles in multiple incubators sees correct union; (5) Assignment change — verify revoking a RoleAssignment immediately removes access; (6) Regression — verify new features don't break existing authorization; (7) UI hiding vs backend enforcement — verify hidden UI elements are also blocked at backend; (8) Audit logging verification — verify high-risk actions produce audit records. Source: FR-015, research.md R4/R5.

**Checkpoint**: User Story 3 complete — QA can derive test cases from permission matrix + testing requirements section.

---

## Phase 6: User Story 4 — Security Reviewer Audits Threat Model (Priority: P2)

**Goal**: Security reviewers can assess the platform's access control posture using a structured threat model with platform-specific safeguards.

**Independent Test**: Verify the threat analysis covers all 11 risk categories from FR-011, each with platform-specific (not generic) safeguards.

### Implementation for User Story 4

- [x] T014 [US4] Write Section 7 — Security Design Principles in `.specify/memory/access-security-constitution.md`. Define 10 principles with platform-specific explanations: least privilege, deny by default, explicit scope validation (TenantContext), backend enforcement as source of truth ([Authorize] + handler checks), defense in depth (middleware + handler + query layers), separation of duties (GlobalAdmin vs IncubatorAdmin creation rights), auditability, secure onboarding (email verification, AccountStatus lifecycle), secure role assignment/reassignment (RoleAssignment aggregate validation), secure deprovisioning (IsActive flags, session invalidation). Source: FR-010, research.md R4/R6.
- [x] T015 [P] [US4] Write Section 8 — Threat and Failure Analysis, entries 1-6 in `.specify/memory/access-security-constitution.md`. Threats: (1) Broken access control — missing [Authorize] or wrong role list; (2) IDOR/BOLA — ExternalId guessing across tenants; (3) Trusting frontend role filtering — hiding UI without backend check; (4) Incorrect scope joins in queries — missing IncubatorId filter in repository queries; (5) Role escalation during user creation — IncubatorAdmin creating GlobalAdmin; (6) Cross-incubator data leakage — ITenantContext not applied or bypassed. Each entry: why it matters here, typical mistakes, required safeguards. Source: FR-011, FR-012, research.md R3-R5.
- [x] T016 [P] [US4] Write Section 8 — Threat and Failure Analysis, entries 7-11 in `.specify/memory/access-security-constitution.md`. Threats: (7) Cross-project data leakage — ProjectCoordinator accessing other projects in same incubator; (8) Mentor seeing unauthorized entrepreneur data — mentor not assigned to that entrepreneur; (9) Stale permissions after reassignment — RoleAssignment revoked but session claims not refreshed; (10) Soft-deleted/archived entity leakage — IsActive=false entities appearing in queries; (11) Unauthorized access via exports/reports/notifications/search/background jobs — secondary data paths bypassing authorization. Each entry: why it matters, typical mistakes, safeguards. Source: FR-011, FR-012, research.md R4/R6/R7.

**Checkpoint**: User Story 4 complete — security reviewers have a structured threat model with 11 platform-specific threat entries and 10 security principles.

---

## Phase 7: User Story 5 — Product Owner Validates Role Catalog (Priority: P2)

**Goal**: Product owners can consult open questions to understand which decisions must be made before implementing role-specific features.

**Independent Test**: Verify open questions section lists unresolved items for all 10 categories from FR-016.

### Implementation for User Story 5

- [x] T017 [US5] Write Section 12 — Open Questions / Decisions Needed in `.specify/memory/access-security-constitution.md`. List unresolved items organized by category: (1) Mentor permissions — what data can mentors read/write beyond their assignments? (2) Entrepreneur permissions — can entrepreneurs see other entrepreneurs in same project? (3) Sponsor permissions — dashboard scope, data granularity, what constitutes "read-only"? (4) IncubatorAdmin project creation — can they create new projects or only manage existing? (5) ProjectCoordinator entrepreneur management — create directly or only manage existing assignments? (6) Multi-role across scopes — conflict resolution when user has different roles in different contexts; (7) Impersonation/support access — should GlobalAdmin be able to act as another user? (8) Archived entity access — who can see deactivated incubators/projects/users? (9) Notification visibility rules — do notifications respect scope boundaries? (10) Reporting/export permissions — who can export data, at what scope? Each item tagged [OPEN]. Source: FR-016, research.md R3.

**Checkpoint**: User Story 5 complete — product owners have a structured list of decisions needed before implementing features for under-specified roles.

---

## Phase 8: User Story 6 — New Contributor Understands Model (Priority: P3)

**Goal**: New contributors can quickly understand the authorization architecture and enforcement approach by reading the constitution.

**Independent Test**: A new contributor can correctly answer basic questions about roles, scopes, and authorization model after reading sections 1, 2, and 9.

### Implementation for User Story 6

- [x] T018 [US6] Write Section 9 — Enforcement Model Recommendations in `.specify/memory/access-security-constitution.md`. Address 11 areas with implementation-aware (not framework-specific) guidance: (1) backend authorization architecture — layered checks at controller, handler, and query levels; (2) route/endpoint guards — [Authorize(Roles)] with hierarchical role inclusion; (3) service-layer policy checks — CheckPermissionQuery for granular permission validation; (4) query-layer scope enforcement — ITenantContext filtering in repository queries; (5) database considerations — row-level IncubatorId/ProjectId as implicit filters; (6) token/session claims — ActiveRole, ActiveIncubatorId, ActiveProjectId in claims; (7) role-to-scope resolution — RoleAssignment lookup for context validation; (8) user creation/assignment validation — creator scope must contain target scope; (9) audit logs — what to log, retention, tamper protection; (10) testing strategy — reference Section 11; (11) admin action traceability — all GlobalAdmin actions logged with actor identity. Source: FR-013, research.md R4/R5.
- [x] T019 [US6] Write Appendices in `.specify/memory/access-security-constitution.md`. (A) Permission Matrix compact — condensed version of Section 6 matrix suitable for quick reference (roles as columns, actions as rows, access level as cell values). (B) Developer Checklist standalone — the 8 questions from Section 10 formatted as a copy-paste checklist with brief instructions. (C) Immediate Next Actions — ordered list: (1) ratify this document with stakeholders, (2) resolve open questions for Mentor/Entrepreneur/Sponsor roles, (3) integrate PR template into repository, (4) add SpecKit checklist hook, (5) audit existing endpoints against permission matrix, (6) add missing authorization checks identified during audit. Source: FR-018.

**Checkpoint**: User Story 6 complete — new contributors have enforcement model guidance and appendices for quick reference.

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Integration, validation, and final quality checks

- [x] T020 [P] Add link to access constitution from `.specify/memory/constitution.md`. In the Knowledge Base or a new "Related Governance Documents" subsection, add: `[Access & Security Constitution](access-security-constitution.md)` with brief description. Update constitution.md Sync Impact Report with minor version bump noting the new linked document.
- [x] T021 [P] Validate all factual statements in sections 1-5 of `.specify/memory/access-security-constitution.md` carry exactly one tag ([CONFIRMED], [INFERRED], [RECOMMENDED], or [OPEN]) per SC-006. Fix any untagged or ambiguously tagged statements.
- [x] T022 [P] Validate permission matrix in Section 6 of `.specify/memory/access-security-constitution.md` covers all 17 required actions from FR-008 per SC-003. Verify each row has all required columns per data-model.md schema. Fix any missing rows or incomplete columns.
- [x] T023 [P] Validate role catalog entries for Mentor, Entrepreneur, and Sponsor in Section 5 of `.specify/memory/access-security-constitution.md` each contain at least 3 actionable open questions per SC-007. Fix any role with fewer than 3 open questions.
- [x] T024 Final document review of `.specify/memory/access-security-constitution.md` — verify cross-reference consistency between sections (role catalog references match permission matrix, threat safeguards reference enforcement model, checklist references testing requirements), verify Markdown formatting (heading hierarchy, table alignment, no broken links), verify glossary terms used consistently throughout, verify document version is 1.0.0 with correct Sync Impact Report.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion — BLOCKS all user stories
- **User Stories (Phase 3-8)**: All depend on Foundational phase completion
  - US1 (Phase 3) should complete first as US2/US3 reference the permission matrix and role catalog
  - US2 (Phase 4) and US3 (Phase 5) can run in parallel after US1
  - US4 (Phase 6) and US5 (Phase 7) can run in parallel, independent of US2/US3
  - US6 (Phase 8) can run in parallel with US4/US5 but benefits from their completion
- **Polish (Phase 9)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) — No dependencies on other stories. **MVP target.**
- **User Story 2 (P1)**: Depends on US1 — checklist references permission matrix and role catalog
- **User Story 3 (P1)**: Depends on US1 — testing section references permission matrix
- **User Story 4 (P2)**: Can start after Foundational — independent of US1/US2/US3
- **User Story 5 (P2)**: Can start after Foundational — independent of other stories
- **User Story 6 (P3)**: Can start after Foundational — benefits from US4 (references enforcement model) but not blocked

### Within Each User Story

- Tasks without [P] must run sequentially within their phase
- Tasks with [P] can run in parallel within their phase
- Each user story's checkpoint validates independent testability

### Parallel Opportunities

- T008 and T009 (role catalog entries) can run in parallel within US1
- T011 and T012 (checklist and PR template) can run in parallel within US2
- T015 and T016 (threat entries 1-6 and 7-11) can run in parallel within US4
- T020, T021, T022, T023 (all polish validation tasks) can run in parallel
- US4 and US5 can run in parallel after US1 completes
- US2 and US3 can run in parallel after US1 completes

---

## Parallel Example: User Story 1

```bash
# After T006 (scope model) and T007 (GA+IA catalog entries) complete sequentially:

# Launch remaining role catalog entries in parallel:
Task: "T008 [P] [US1] Write ProjectCoordinator role catalog entry"
Task: "T009 [P] [US1] Write Mentor, Entrepreneur, Sponsor role catalog entries"

# Then T010 (permission matrix) runs after all catalog entries complete
```

## Parallel Example: User Story 4

```bash
# After T014 (security principles) completes:

# Launch both halves of threat analysis in parallel:
Task: "T015 [P] [US4] Write threats 1-6"
Task: "T016 [P] [US4] Write threats 7-11"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001)
2. Complete Phase 2: Foundational (T002-T005)
3. Complete Phase 3: User Story 1 (T006-T010)
4. **STOP and VALIDATE**: Can an engineer answer "who can create diagnostic forms?" using only the document?
5. If yes: MVP is viable — the core reference value is delivered

### Incremental Delivery

1. Complete Setup + Foundational -> Document baseline established
2. Add User Story 1 -> Role catalog + permission matrix usable (MVP!)
3. Add User Story 2 + 3 -> Developer checklist + QA testing guidance operational
4. Add User Story 4 + 5 -> Threat model + open questions complete
5. Add User Story 6 -> Enforcement model + appendices finalize document
6. Polish -> Integration and validation complete

### Sequential Strategy (Recommended for Single Author)

Since this is a single document, sequential authoring within each phase is natural:

1. T001 -> T002 -> T003 -> T004 -> T005 (scaffold + foundation)
2. T006 -> T007 -> T008/T009 parallel -> T010 (US1 — scope, roles, matrix)
3. T011/T012 parallel (US2 — checklist + PR template)
4. T013 (US3 — testing requirements)
5. T014 -> T015/T016 parallel (US4 — principles + threats)
6. T017 (US5 — open questions)
7. T018 -> T019 (US6 — enforcement + appendices)
8. T020/T021/T022/T023 parallel -> T024 (polish + final review)

---

## Notes

- [P] tasks = independent sections or separate files, no content dependencies
- [Story] label maps task to specific user story for traceability
- Each user story delivers an independently valuable increment of the governance document
- All content sources referenced in task descriptions (research.md, data-model.md, spec FR-### numbers)
- Commit after each phase completion for clean history
- The document is a living artifact — version 1.0.0 is the initial ratification; updates follow the amendment procedure
