# Feature Specification: Mentory Enterprise SaaS Platform

**Feature Branch**: `001-mentory-platform-core`  
**Created**: 2026-03-30  
**Status**: Draft  
**Input**: User description: "Multi-tenant SaaS platform for incubators to support entrepreneurs through diagnostic assessments, personalized mentoring plans, structured learning, and mentorship execution."

## Clarifications

### Session 2026-03-30 (1)



- Q: What authentication approach should the platform use? → A: Vendor-neutral standard auth (email/password with open protocol support for future SSO/OAuth2). Users are identified by Country + National ID (compound unique per country) and Email (globally unique). Registration and administrative enrollment collect country, national ID, and email with sequential uniqueness validation (country+ID first, then email). Login uses email and password only. Open signup requires security-by-design protections: enumeration prevention, rate limiting, email verification, and abuse mitigation.
- Q: What does "configurable" mean for project lifecycle stages? → A: Fixed pipeline. All 7 stages (Registration, Forms, Analysis, Learning Assignment, Mentoring, Final Evaluation, Closure) are mandatory and in fixed order for every project. "Configurable" in the original prompt referred to the stage transitions being manually controlled, not to the stage list itself.
- Q: What level of tenant data isolation is required? → A: Logical isolation — shared tables with mandatory tenant-scoped filtering on every query. No incubator can ever see another's data. Combined with server-side context enforcement (FR-006, FR-011, FR-013) for defense in depth.
- Q: Can one entrepreneur participate in multiple projects within the same incubator? → A: Role-dependent. Entrepreneurs are limited to one active project per incubator at a time. Mentors, Project Coordinators, and Incubator Admins can span multiple projects within the same incubator.
- Q: How do diagnostic responses translate into suggested mentoring topics? → A: Score aggregation and priority mapping model with multi-dimensional classifications. Each answer option contributes a numerical score, a SWOT classification (Strength/Weakness/Opportunity/Threat), and an ODSR strategic orientation (Offensive/Defensive/Survival/Reorientation). Scores are aggregated per topic. Each topic defines configurable priority ranges mapping cumulative scores to priority levels (High/Medium/Low/Not Applicable). High and Medium priority topics are auto-included in the mentoring plan; Low is optional; Not Applicable is excluded. SWOT and ODSR provide interpretive context but do not affect suggestion logic. Optional non-scored follow-up questions can deepen qualitative insight.

### Session 2026-03-30 (2)

- Q: Can an entrepreneur retake or redo the diagnostic assessment? → A: No retakes. The project has exactly two evaluation stages: Initial (during Forms stage) and Final (during Final Evaluation stage). Each question is tagged as "initial", "final", or "both" to determine when it appears. The initial evaluation drives mentoring plan generation; the final evaluation measures progress. Authorized users (mentor, project coordinator, incubator admin, global admin) can correct individual answers at any time — even after the evaluation stage is completed — to fix errors or typos, but this is answer correction, not a retake.
- Q: How are mentors assigned to entrepreneurs, and what is the cardinality? → A: Many-to-many. Multiple mentors can be assigned to one entrepreneur (e.g., domain specialists for different topics). A Project Coordinator manages assignments. One mentor is flagged as "lead" for visual/coordination purposes only — the lead flag carries no functional permission difference. All assigned mentors have equal access to sessions, assignments, plan adjustments, and all mentor actions for that entrepreneur.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Platform Foundation: Tenant, User & Context Management (Priority: P1)

A Global Admin creates and manages incubators (tenants) on the platform. Each incubator has its own administrators who manage projects and invite users. New users register through an open signup form or are enrolled administratively by an authorized user. Registration requires country, national identification number, and email — the system validates that the country+ID combination is unique and that the email is not already in use. Users log in with their email and password only; country and national ID are not part of the login process. After login, users select their active context (incubator, project, role) before accessing any functionality. Users who belong to only one context are auto-directed. The server stores and enforces the active context, ensuring users only see data and actions permitted for their current role.

**Why this priority**: Without multi-tenancy, user management, and context-driven access, no other module can function. This is the foundation that all platform capabilities depend on.

**Independent Test**: Can be fully tested by creating an incubator, adding users with different roles, and verifying that context selection restricts access appropriately. Delivers a functioning multi-tenant user management system.

**Acceptance Scenarios**:

1. **Given** a Global Admin is logged in, **When** they create a new incubator with name and configuration, **Then** the incubator is created as an independent tenant with its own data boundary.
2. **Given** an Incubator Admin is logged in, **When** they create a project within their incubator, **Then** the project is created under the correct incubator and is invisible to other incubators.
3. **Given** a user with roles in multiple incubators/projects logs in, **When** they reach the context selection screen, **Then** they see only the incubator/project/role combinations assigned to them.
4. **Given** a user selects a context (incubator + project + role), **When** the context is set, **Then** the server persists this as their active session context, and all subsequent requests are authorized against it.
5. **Given** a user has an active context as Mentor in Project A, **When** they attempt to access data from Project B via URL manipulation, **Then** the system denies the request and returns an authorization error.
6. **Given** a user belongs to exactly one incubator, one project, and one role, **When** they log in, **Then** the system auto-selects that context without showing the selection screen.
7. **Given** a user has an active session, **When** they attempt to open a second concurrent session, **Then** only one session remains active (the newest replaces the old).
8. **Given** a new user visits the open registration page, **When** they provide their country, national ID, and email, **Then** the system first validates the country+ID combination is not already registered, then validates the email is not already registered, and proceeds only if both are unique.
9. **Given** a user attempts to register via open signup with a national ID or email that already exists, **When** they submit the form, **Then** the system rejects the registration with a generic error that does not reveal which specific field caused the conflict.
10. **Given** an Incubator Admin enrolls a new user administratively, **When** they provide the user's country, national ID, and email, **Then** the same sequential uniqueness validation applies, but the admin receives specific feedback about which field conflicts.
11. **Given** multiple registration attempts arrive from the same source in a short period, **When** the rate threshold is exceeded, **Then** the system blocks further attempts temporarily and logs the event.
12. **Given** a new user has registered but has not yet verified their email, **When** they attempt to log in, **Then** the system denies access and prompts them to complete email verification.
13. **Given** a registered user with a verified email, **When** they reach the login page, **Then** they are asked for email and password only (no country or national ID).
14. **Given** a user enters incorrect credentials repeatedly, **When** the failed attempt threshold is reached, **Then** the account is temporarily locked and the user is informed of the lockout.

---

### User Story 2 - Diagnostic Assessment: Form Management & Completion (Priority: P2)

A Project Coordinator selects a global diagnostic form template (available per the incubator's subscription) and clones it into a project. The coordinator can customize the cloned form by adding, removing, or modifying questions. Each question is linked to a knowledge topic and tagged with an evaluation stage: "initial" (shown only during the Initial evaluation at the Forms stage), "final" (shown only during the Final Evaluation stage), or "both" (shown in both stages). Each answer option carries a numerical score, a SWOT classification (Strength, Weakness, Opportunity, Threat), and an ODSR strategic orientation (Offensive, Defensive, Survival, Reorientation). Questions may also have optional non-scored follow-up questions for deeper qualitative insight. The form is used twice per project: during the Initial evaluation, the Entrepreneur answers "initial" and "both" questions; during the Final Evaluation stage, the Entrepreneur answers "final" and "both" questions. Upon completion of each evaluation stage, the system records all responses, aggregates scores per topic, and marks that evaluation stage as completed. Authorized users (mentor, project coordinator, incubator admin, global admin) can correct individual answers at any time to fix errors or typos.

**Why this priority**: The diagnostic is the entry point to the mentoring process. Without it, the system cannot assess entrepreneurs or generate mentoring plans. It's the first functional module after the platform foundation.

**Independent Test**: Can be fully tested by cloning a form template, customizing it, having an entrepreneur fill it out, and verifying responses are stored correctly with topic mappings intact.

**Acceptance Scenarios**:

1. **Given** a Project Coordinator is in their project context, **When** they browse available diagnostic form templates, **Then** they see only templates available under their incubator's subscription plan.
2. **Given** a coordinator selects a global template, **When** they clone it to their project, **Then** an independent copy is created that can be modified without affecting the global template.
3. **Given** a cloned form exists, **When** the coordinator adds, removes, or reorders questions, **Then** the changes apply only to the project-level clone.
4. **Given** an Entrepreneur is at the Forms stage (Initial evaluation), **When** they view the diagnostic form, **Then** they see only questions tagged as "initial" or "both."
5. **Given** an Entrepreneur is at the Final Evaluation stage, **When** they view the diagnostic form, **Then** they see only questions tagged as "final" or "both."
6. **Given** an Entrepreneur has a diagnostic form assigned, **When** they complete and submit all applicable questions for the current evaluation stage, **Then** responses are persisted and that evaluation stage is marked as completed.
7. **Given** a question is of type "options," **When** the entrepreneur views it, **Then** they see the defined options and can select one or more as configured; each selected option contributes its score and classifications to the topic's aggregation.
8. **Given** a question has optional follow-up questions, **When** the entrepreneur completes the main question, **Then** the follow-up questions are presented for qualitative input without affecting the topic's numerical score.
9. **Given** a form was cloned and later the global template is updated, **When** the coordinator views sync options, **Then** they can choose to remain disconnected or partially sync new questions from the global template.
10. **Given** an entrepreneur has completed the Initial evaluation, **When** the system processes their responses, **Then** scores from all answer options are aggregated per topic, producing a cumulative score for each topic that drives mentoring plan generation.
11. **Given** an evaluation stage has been completed, **When** an authorized user (mentor, coordinator, incubator admin, or global admin) corrects an individual answer, **Then** the correction is saved with an audit trail of who changed it, when, and the previous value.
12. **Given** a project has exactly two evaluation stages (Initial and Final), **When** the entrepreneur completes both, **Then** the system can compare scores across both evaluations to measure progress per topic.

---

### User Story 3 - Knowledge Structure Management (Priority: P3)

An Incubator Admin or Project Coordinator manages a hierarchical knowledge structure: Knowledge Structures contain Modules, which contain Topics, which contain Subjects, which contain Resources (videos, links, files). Global knowledge structure templates can be cloned into projects following the same clone/customize pattern as diagnostic forms. Topics serve as the bridge between diagnostics and learning, linking questions to relevant content areas.

**Why this priority**: The knowledge structure provides the content that mentoring plans draw from. It must exist before mentoring plans can reference specific modules and topics for learning paths.

**Independent Test**: Can be fully tested by creating a knowledge structure with the full hierarchy, cloning a global template, customizing it, and verifying that topics correctly link to diagnostic questions.

**Acceptance Scenarios**:

1. **Given** a Project Coordinator is in their project context, **When** they clone a global knowledge structure template, **Then** the full hierarchy (modules, topics, subjects, resources) is copied as an independent project-level clone.
2. **Given** a cloned knowledge structure exists, **When** the coordinator modifies a topic's subjects or resources, **Then** changes apply only to the project-level clone.
3. **Given** a topic exists in the knowledge structure, **When** a diagnostic question is linked to that topic, **Then** the system records the mapping between the question and the topic.
4. **Given** a coordinator selects a diagnostic form for the project, **When** that form is selected, **Then** the associated knowledge structure is implicitly selected as well.
5. **Given** a knowledge structure has modules with topics, **When** a user views a module, **Then** they see all topics, subjects, and resources in their defined order.

---

### User Story 4 - Mentoring Plan Generation (Priority: P4)

After an entrepreneur completes their Initial evaluation, the system aggregates scores per topic and maps each topic to a priority level (High, Medium, Low, Not Applicable) based on configurable score ranges. High and Medium priority topics are automatically included in the suggested mentoring plan; Low priority topics are presented as optional; Not Applicable topics are excluded. The system also surfaces SWOT and ODSR classifications as interpretive context alongside each suggested topic. Any of the assigned mentors reviews the suggested topics and their priority levels with the Entrepreneur. Together, they adjust the plan: confirming auto-included topics, opting in or out of Low priority topics, adding topics not suggested by the system, and reordering priorities. The final agreed-upon plan is saved and becomes the basis for mentoring execution.

**Why this priority**: The mentoring plan is the core value proposition — translating diagnostic results into a personalized learning path. It depends on both the diagnostic and knowledge structure being in place.

**Independent Test**: Can be fully tested by completing a diagnostic, reviewing the auto-suggested topics, adjusting the plan collaboratively, and verifying the saved plan accurately reflects the agreed selections.

**Acceptance Scenarios**:

1. **Given** an entrepreneur has completed their Initial evaluation, **When** the system generates topic suggestions, **Then** it aggregates scores per topic and assigns a priority level (High, Medium, Low, Not Applicable) based on the topic's configured score ranges.
2. **Given** suggestions have been generated, **When** the mentor views the suggested plan, **Then** High and Medium priority topics are pre-included, Low priority topics are shown as optional, and Not Applicable topics are excluded.
3. **Given** a mentor is reviewing the suggested plan, **When** they view a topic, **Then** they see the cumulative score, priority level, SWOT classification summary, and ODSR strategic orientation as interpretive context.
4. **Given** a mentor is reviewing the suggested plan with an entrepreneur, **When** they add a Low priority or excluded topic, **Then** the topic is included in the plan with a manual override note.
5. **Given** a mentor is reviewing the suggested plan, **When** they remove an auto-included topic, **Then** it is excluded from the final plan with a justification note.
6. **Given** mentor and entrepreneur have finalized adjustments, **When** they save the plan, **Then** the plan is persisted with the priority levels, any manual overrides, and an audit record of who approved it and when.
7. **Given** a mentoring plan is saved, **When** any user with access views it, **Then** they see the complete list of topics with their priority, order, SWOT/ODSR context, and any notes from the planning session.

---

### User Story 5 - Mentoring Execution: Scheduling, Sessions & Assignments (Priority: P5)

Based on the approved mentoring plan, the system generates a calendar of mentoring sessions using scheduling inputs: subject duration estimates, sessions per week, and hours per session. Any of the entrepreneur's assigned mentors can conduct sessions flexibly — they can cover topics in any order and adjust dynamically. After each session, the conducting mentor logs notes, topics covered, and decisions made. Assignments linked to specific subjects can be created by any assigned mentor, with entrepreneurs submitting work and any assigned mentor providing review and feedback.

**Why this priority**: Execution is where the value is delivered — actual mentoring happens here. It depends on the mentoring plan being defined first.

**Independent Test**: Can be fully tested by generating a session calendar from a plan, conducting a session with logging, creating an assignment, submitting work, and reviewing it.

**Acceptance Scenarios**:

1. **Given** a mentoring plan exists with subjects and duration estimates, **When** the scheduling engine receives sessions/week and hours/session parameters, **Then** it generates a proposed calendar of sessions distributed across the defined timeframe.
2. **Given** a session is scheduled, **When** the mentor starts the session, **Then** they can access the plan topics and mark which ones they will cover in this session.
3. **Given** a session is in progress, **When** the mentor decides to cover a topic not originally planned for this session, **Then** the system allows flexible topic selection without enforcing a fixed order.
4. **Given** a session has been conducted, **When** the mentor logs session details, **Then** notes, topics covered, and decisions are recorded with timestamp and participant information.
5. **Given** a subject has been covered in a session, **When** the mentor creates an assignment for the entrepreneur, **Then** the assignment is linked to the specific subject and includes instructions and a deadline.
6. **Given** an entrepreneur has submitted an assignment, **When** the mentor reviews it, **Then** they can provide feedback, mark it as approved or request revisions, and the entrepreneur is notified.

---

### User Story 6 - Subscription & Plan Management (Priority: P6)

A Global Admin manages subscription plans with versioned feature sets. Plans define what each incubator can access through boolean features (e.g., "advanced diagnostics enabled") and quantitative limits (e.g., "maximum 50 projects"). Incubators are assigned to plans, and positive overrides can be granted to individual incubators to extend capabilities beyond their base plan. Overrides are accumulative, never expire, and can only add — never remove — capabilities. No payment integration exists; plan assignment is fully admin-managed.

**Why this priority**: Subscriptions control feature availability across the platform but can function as a simple "all access" mode initially. It's important for commercial viability but not for core learning flow.

**Independent Test**: Can be fully tested by creating subscription plans with features, assigning them to incubators, applying overrides, and verifying feature availability reflects the plan + overrides.

**Acceptance Scenarios**:

1. **Given** a Global Admin is managing subscriptions, **When** they create a new plan with boolean and quantitative features, **Then** the plan is saved with a version identifier.
2. **Given** a plan exists, **When** the admin assigns it to an incubator, **Then** the incubator's available features reflect the plan's configuration.
3. **Given** an incubator is on a plan with a 50-project limit, **When** the admin grants a +20 project override, **Then** the incubator's effective limit becomes 70 projects.
4. **Given** an incubator has overrides applied, **When** the admin views the incubator's effective features, **Then** they see the base plan features combined with all accumulative overrides.
5. **Given** an override has been applied, **When** time passes, **Then** the override remains active indefinitely (no expiration).
6. **Given** an incubator is on a plan, **When** an Incubator Admin attempts to create more projects than their effective limit, **Then** the system prevents the creation and displays the current limit.

---

### User Story 7 - Project Lifecycle Management (Priority: P7)

A Project Coordinator manages the lifecycle of entrepreneur projects through a fixed sequence of seven mandatory stages: Registration, Forms, Analysis, Learning Assignment, Mentoring, Final Evaluation, and Closure. Each stage has a state (Not started, In progress, Completed). Stage transitions are manual — the coordinator explicitly advances projects when criteria are met. The current stage drives what UI elements and actions are available to participants, guiding them through the process.

**Why this priority**: Lifecycle management provides structure and governance to the mentoring process. While important for operational maturity, the core mentoring flow can function without strict stage enforcement initially.

**Independent Test**: Can be fully tested by creating a project, advancing it through stages manually, and verifying that available actions and UI guidance change per stage.

**Acceptance Scenarios**:

1. **Given** a new project is created, **When** it is initialized, **Then** it starts at the Registration stage with state "Not started."
2. **Given** a project is at the Forms stage with state "Completed," **When** the coordinator advances it, **Then** the project moves to the Analysis stage with state "Not started."
3. **Given** a project is at the Mentoring stage, **When** an Entrepreneur views their dashboard, **Then** they see actions relevant to mentoring (sessions, assignments) and not actions from other stages (e.g., form submission).
4. **Given** a project is at a stage, **When** the coordinator attempts to skip a stage, **Then** the system prevents the skip and requires sequential progression.
5. **Given** a project reaches the Final Evaluation stage, **When** the entrepreneur opens the diagnostic form, **Then** they see only questions tagged as "final" or "both" for the progress assessment.
6. **Given** a project reaches Closure stage with state "Completed," **When** the coordinator finalizes it, **Then** the project is archived and all participants are notified.

---

### User Story 8 - Notification System (Priority: P8)

The platform sends notifications to users based on events and schedules. Notifications are centralized, auditable, and deduplicated. Each notification has content, a list of recipients (individual users or groups), and delivery tracking. Notifications can be immediate (e.g., session reminder) or scheduled (e.g., weekly mentor agenda). Users can configure their notification preferences per role and context. Email is the initial delivery channel, with the system designed to support future channels (SMS, in-app).

**Why this priority**: Notifications enhance engagement and operational awareness but are not required for core functionality to work. The platform can operate with manual coordination initially.

**Independent Test**: Can be fully tested by triggering notification events (session reminder, task reminder), verifying delivery, confirming deduplication, and testing preference overrides.

**Acceptance Scenarios**:

1. **Given** a mentoring session is scheduled for tomorrow, **When** the notification engine runs, **Then** both mentor and entrepreneur receive a reminder via email.
2. **Given** a user has already received a reminder for a specific session, **When** the engine runs again, **Then** no duplicate notification is sent.
3. **Given** a mentor has 3 sessions scheduled this week, **When** the weekly agenda notification triggers, **Then** the mentor receives a single summary email listing all upcoming sessions.
4. **Given** a user has configured preferences to disable session reminders for their Mentor role, **When** a session reminder would fire, **Then** the notification is suppressed for that user in that context.
5. **Given** a notification is sent, **When** an administrator views the audit log, **Then** they can see the notification content, recipients, delivery channel, delivery status, and timestamp.

---

### Edge Cases

- What happens when a user's last remaining role in an incubator is removed while they have an active session in that context? The system must invalidate the session and redirect to context selection.
- How does the system handle a diagnostic form clone when the global template has been deleted? Cloned forms must remain functional independently of the template's existence.
- What happens when a subscription plan's project limit is reduced below the incubator's current project count? Existing projects must remain accessible; only new project creation is blocked.
- How does the system handle concurrent edits to a mentoring plan by mentor and entrepreneur simultaneously? The system must prevent conflicting saves — last-write-wins with conflict notification or optimistic concurrency.
- What happens when a mentor tries to log a session for a project that has moved to the Closure stage? The system must prevent session logging for closed projects.
- What happens when a scheduled notification references a session that has been canceled? The system must check event validity before sending and suppress stale notifications.
- What happens when someone attempts to register with a valid national ID from one country but provides an email already associated with a different country's ID? The system must reject — email is globally unique regardless of country.
- What happens when an attacker probes the signup endpoint to discover whether specific national IDs or emails exist? The system must return identical generic responses for all failure cases on public-facing endpoints.
- What happens when a user's email changes after registration? The system must re-verify the new email before it becomes active, and the old email must remain functional until verification completes. *(Note: email change functionality is OUT OF SCOPE for this feature; this edge case is documented for future implementation.)*
- What happens when a topic's priority score ranges are modified after diagnostics have already been completed? Existing mentoring plans remain as-is (approved snapshot); new plan generations use the updated ranges.
- What happens when a cloned form modifies an answer option's score? Only future diagnostic completions use the updated score; previously aggregated scores are not recalculated unless explicitly triggered.
- What happens when an admin attempts to assign an entrepreneur to a second project within the same incubator? The system must reject the assignment and inform the admin that the entrepreneur is already enrolled in an active project in that incubator.
- What happens when an entrepreneur's project reaches Closure? The entrepreneur becomes eligible for enrollment in a new project within the same incubator.
- What happens if a bug or misconfiguration in tenant-scoped filtering exposes data across incubators? The system must treat cross-tenant data leakage as a critical severity incident; acceptance tests must verify isolation across all data access paths.
- What happens when an authorized user corrects an answer in the Initial evaluation after the mentoring plan has already been generated? The existing approved plan remains as-is (it is a snapshot); the corrected score is reflected if the plan is ever re-generated.
- What happens when a question tagged as "both" has different answer values in the Initial vs Final evaluation? Both response sets are maintained independently; the system can compare scores per topic across evaluations to measure progress.

## Requirements *(mandatory)*

### Functional Requirements

**Multi-Tenancy & User Management**

- **FR-001**: System MUST support multiple incubators as independent tenants with complete logical data isolation — all tenant data resides in shared tables with mandatory tenant-scoped filtering on every data access operation, ensuring no incubator can ever see another's data.
- **FR-002**: System MUST enforce a single global user identity, uniquely identified by both a Country + National ID combination (unique per country) and an Email address (globally unique). Users can hold multiple roles across different incubators and projects, subject to role-specific constraints: Entrepreneurs are limited to one active project per incubator; Mentors, Project Coordinators, and Incubator Admins may span multiple projects within the same incubator.
- **FR-003**: System MUST support the following roles: Global Admin, Incubator Admin, Project Coordinator, Mentor, Entrepreneur, and Sponsor (read-only).
- **FR-004**: System MUST provide hierarchical tenant structure: Platform > Incubators > Projects > Users.

**Session & Context**

- **FR-005**: System MUST require users to select an active context (incubator, project, role) after login.
- **FR-006**: System MUST store the active session context server-side and never trust client-provided context values.
- **FR-007**: System MUST enforce a single active session per user at any time.
- **FR-008**: System MUST enforce a single active context per session.
- **FR-009**: System MUST auto-select context when only one valid combination exists for the user. For Entrepreneurs with one incubator, the project is always deterministic (one active project per incubator) and context selection can be fully automatic.

**Authorization**

- **FR-010**: System MUST implement role-based access control with granular permissions at module, action, and resource levels. The permission matrix is as follows:

  | Module | Action | GlobalAdmin | IncubatorAdmin | ProjectCoordinator | Mentor | Entrepreneur | Sponsor |
  |--------|--------|:-----------:|:--------------:|:------------------:|:------:|:------------:|:-------:|
  | Incubators | Create/Edit | Yes | — | — | — | — | — |
  | Incubators | View own | Yes | Yes | — | — | — | — |
  | Projects | Create | — | Yes | — | — | — | — |
  | Projects | Edit/View | — | Yes | Yes | — | — | — |
  | Users | Platform list | Yes | — | — | — | — | — |
  | Users | Enroll in incubator | — | Yes | — | — | — | — |
  | Participants | Enroll in project | — | — | Yes | — | — | — |
  | Participants | Assign mentor | — | — | Yes | — | — | — |
  | Diagnostic Templates | Create/Edit/View | Yes | — | — | — | — | — |
  | Diagnostic Forms | Clone/Customize | — | — | Yes | — | — | — |
  | Diagnostic Forms | Fill (eval) | — | — | — | — | Yes | — |
  | Diagnostic Forms | Correct answers | — | Yes | Yes | Yes | — | — |
  | Knowledge Templates | Create/Edit/View | Yes | — | — | — | — | — |
  | Knowledge Structures | Clone/Customize | — | — | Yes | — | — | — |
  | Mentoring Plans | Generate/Adjust/Approve | — | — | — | Yes | — | — |
  | Mentoring Plans | View | — | Yes | Yes | Yes | Yes | — |
  | Sessions | Schedule/Log | — | — | — | Yes | — | — |
  | Sessions | View upcoming | — | — | — | Yes | Yes | — |
  | Assignments | Create/Review | — | — | — | Yes | — | — |
  | Assignments | Submit | — | — | — | — | Yes | — |
  | Subscriptions | Create/Assign/Override | Yes | — | — | — | — | — |
  | Lifecycle | Advance stage | — | — | Yes | — | — | — |
  | Notifications | View preferences | — | — | — | Yes | Yes | — |
  | Dashboards | Sponsor view | — | — | — | — | — | Yes |
- **FR-011**: System MUST validate all access requests against the user's active context on the server. *(Complements FR-006 server-side storage and FR-013 backend enforcement.)*
- **FR-012**: System MUST deny access to resources via shared URLs when the requesting user lacks permission.
- **FR-013**: System MUST enforce all authorization on the backend — frontend restrictions are for UX only, never for security. *(See also FR-006, FR-011.)*

**Diagnostic Module**

- **FR-014**: System MUST maintain global diagnostic form templates accessible by subscription tier.
- **FR-015**: System MUST support cloning global templates into project-level forms that can be independently customized.
- **FR-016**: System MUST support question types: text, numeric, and options (single/multi-select). Each answer option for option-type questions MUST carry a numerical score, a SWOT classification (Strength/Weakness/Opportunity/Threat), and an ODSR strategic orientation (Offensive/Defensive/Survival/Reorientation).
- **FR-016a**: Each question MUST be tagged with an evaluation stage applicability: "initial" (Forms stage only), "final" (Final Evaluation stage only), or "both" (shown in both stages).
- **FR-016b**: The system MUST support exactly two evaluation stages per project: Initial (during Forms stage) and Final (during Final Evaluation stage). During each stage, only questions matching that stage's tag are presented to the entrepreneur.
- **FR-017**: System MUST link each diagnostic question to a knowledge topic.
- **FR-018**: System MUST support optional question blocks and question ordering within forms.
- **FR-018a**: System MUST support optional non-scored follow-up questions attached to any question, for qualitative insight that does not affect topic scoring.
- **FR-018b**: Authorized users (mentor, project coordinator, incubator admin, global admin) MUST be able to correct individual answers at any time — including after an evaluation stage is completed — with a full audit trail of the change (who, when, previous value).
- **FR-019**: System MUST support sync modes for cloned forms: fully disconnected (no updates from template) and partial sync (pull new questions added to the global template without overwriting locally modified questions).

**Knowledge Structure**

- **FR-020**: System MUST support a hierarchical knowledge structure: Knowledge Structure > Modules > Topics > Subjects > Resources.
- **FR-021**: System MUST support resource types including video, link, and file.
- **FR-022**: System MUST use Topics as the linkage unit between diagnostics and learning content.
- **FR-023**: System MUST support the same clone/customize pattern for knowledge structures as for diagnostic forms.
- **FR-024**: Selecting a diagnostic form for a project MUST implicitly select the associated knowledge structure.

**Mentoring Plan**

- **FR-025**: System MUST aggregate scores from the Initial evaluation responses linked to each topic, producing a cumulative score per topic that drives mentoring plan generation.
- **FR-025a**: Each topic MUST define configurable score ranges that map cumulative scores to priority levels: High Priority, Medium Priority, Low Priority, and Not Applicable.
- **FR-025b**: System MUST auto-include High and Medium priority topics in the suggested mentoring plan, present Low priority topics as optional, and exclude Not Applicable topics.
- **FR-025c**: System MUST surface SWOT and ODSR classifications as interpretive context alongside each suggested topic; these classifications MUST NOT affect the suggestion/priority logic.
- **FR-026**: System MUST allow mentor and entrepreneur to collaboratively adjust the suggested plan before finalizing, including adding excluded/optional topics and removing auto-included topics with justification.
- **FR-027**: System MUST persist the final mentoring plan with priority levels, manual overrides, and an audit record of who approved it and when.

**Mentoring Execution**

- **FR-028**: System MUST generate a session calendar based on subject duration, sessions per week, and hours per session.
- **FR-029**: System MUST allow flexible, non-linear session execution where mentors can adjust topics dynamically.
- **FR-030**: System MUST capture session logs including notes, topics covered, and decisions made.
- **FR-031**: System MUST support assignments linked to subjects with submission, review, and feedback workflows.

**Subscription System**

- **FR-032**: System MUST support versioned subscription plans with boolean and quantitative features. Creating a new version archives the previous version; incubators remain on their assigned version until explicitly reassigned by a Global Admin.
- **FR-033**: System MUST support positive-only, accumulative overrides per incubator with no expiration.
- **FR-034**: System MUST enforce feature limits (e.g., project count) based on the effective plan (base + overrides).
- **FR-035**: Subscription management MUST be admin-managed with no payment gateway integration.

**Project Lifecycle**

- **FR-036**: System MUST enforce a fixed, mandatory project lifecycle of 7 stages in order: Registration, Forms, Analysis, Learning Assignment, Mentoring, Final Evaluation, Closure. All stages apply to every project; no stages can be skipped or reordered.
- **FR-037**: System MUST enforce manual stage transitions by authorized users (Project Coordinator).
- **FR-038**: System MUST track stage state as Not started, In progress, or Completed.
- **FR-039**: System MUST drive UI guidance and available actions based on the current project stage.

**Notification System**

- **FR-040**: System MUST provide centralized, auditable notification delivery with no duplication.
- **FR-041**: System MUST support immediate and scheduled notification types.
- **FR-042**: System MUST track notification content, recipients, delivery channel, and delivery status.
- **FR-043**: System MUST support user-configurable notification preferences per role and context.
- **FR-044**: System MUST support email as the initial delivery channel with extensibility for additional channels.

**Auditability**

- **FR-045**: System MUST maintain full audit trails for security events, context changes, plan approvals, and stage transitions.

**Authentication & Registration**

- **FR-046**: Registration and administrative enrollment MUST collect the user's country, national identification number, and email address. *(See FR-002 for uniqueness constraints.)*
- **FR-047**: *(Consolidation note: uniqueness rules are defined in FR-002.)* Registration and enrollment workflows MUST enforce these uniqueness rules at the point of data entry, rejecting duplicates before account creation.
- **FR-048**: During registration, the system MUST validate uniqueness sequentially: first confirm the country + national ID combination does not exist, then confirm the email does not exist.
- **FR-049**: Authentication MUST use a vendor-neutral approach (email + password) with open protocol support for future SSO/OAuth2 integration.
- **FR-050**: Login MUST require only the user's email address and password; country and national ID are not part of the login process.
- **FR-051**: New user accounts MUST verify their email address before gaining any access to the platform.
- **FR-052**: Open (public) registration endpoints MUST NOT reveal to unauthenticated users whether a specific national ID or email address is already registered; all validation failures must return identical generic responses.
- **FR-053**: Administrative enrollment (by authenticated, authorized users) MUST provide specific feedback about which field (national ID or email) conflicts, to support efficient user management.
- **FR-054**: Open registration MUST be protected against automated abuse through rate limiting, bot detection, and throttling mechanisms.
- **FR-055**: The system MUST temporarily lock accounts after a configurable number of consecutive failed login attempts and log all lockout events.
- **FR-056**: The system MUST enforce minimum password strength requirements at registration and password change: minimum 10 characters, at least one uppercase letter, one lowercase letter, one digit, and one special character. Passwords MUST NOT contain the user's email address or national ID.

**Mentor Assignment**

- **FR-057**: System MUST support many-to-many assignment of mentors to entrepreneurs within a project, managed by the Project Coordinator.
- **FR-058**: System MUST allow exactly one assigned mentor per entrepreneur to be flagged as "lead mentor" for visual identification and internal coordination purposes only.
- **FR-059**: The lead mentor flag MUST NOT affect permissions or access — all assigned mentors have equal functional capabilities (sessions, assignments, plan adjustments, logging) for that entrepreneur.

### Key Entities

- **Incubator (Tenant)**: An organization that operates mentoring programs. Top-level data boundary enforced through logical isolation (tenant-scoped filtering on shared tables). Has a subscription plan, administrators, and projects.
- **Project**: A mentoring program instance within an incubator. Contains entrepreneurs, mentors, diagnostic forms, knowledge structures, and mentoring plans. Progresses through lifecycle stages.
- **User**: A person with a single global identity defined by Country + National ID (compound unique per country) and Email (globally unique). Registered via open signup or admin enrollment. Can hold multiple roles across incubators and projects. Has notification preferences.
- **Role Assignment**: The contextual binding of a user to a specific role within an incubator/project combination. Entrepreneurs are constrained to one active project per incubator; Mentors, Coordinators, and Admins may hold assignments across multiple projects within the same incubator.
- **Session Context**: The server-side record of a user's currently active incubator, project, and role. Drives all authorization decisions.
- **Subscription Plan**: A versioned set of boolean and quantitative features that determines what an incubator can access.
- **Override**: A positive-only, non-expiring extension to an incubator's subscription plan.
- **Diagnostic Form (Template)**: A global-level assessment template with ordered questions linked to topics.
- **Diagnostic Form (Clone)**: A project-level copy of a template that can be independently customized.
- **Question**: A form element with a type (text, numeric, options), linked to a topic, optionally grouped in a block. Tagged with evaluation stage applicability: "initial", "final", or "both". May have optional non-scored follow-up questions for qualitative insight.
- **Answer Option**: A selectable choice within an option-type question. Carries a numerical score, a SWOT classification (Strength/Weakness/Opportunity/Threat), and an ODSR strategic orientation (Offensive/Defensive/Survival/Reorientation).
- **Diagnostic Response**: An entrepreneur's answers to a completed evaluation stage (Initial or Final). Each project produces two response sets. Initial responses drive mentoring plan generation; Final responses measure progress. Individual answers can be corrected by authorized users with audit trail. The system aggregates answer scores per topic, producing a cumulative score used for priority mapping.
- **Knowledge Structure**: A hierarchical learning content container: Modules > Topics > Subjects > Resources.
- **Topic**: The core analytical unit that links diagnostic questions to learning content. Central to plan generation. Defines configurable score ranges for priority mapping (High/Medium/Low/Not Applicable).
- **Subject**: A content block within a topic, containing resources.
- **Resource**: A learning material item (video, link, or file) within a subject.
- **Mentoring Plan**: A personalized set of topics selected for an entrepreneur based on diagnostic score aggregation, priority mapping, and mentor/entrepreneur agreement. Records priority levels, SWOT/ODSR context, and any manual overrides with justification.
- **Mentor Assignment**: The many-to-many binding of mentors to an entrepreneur within a project. One mentor per entrepreneur is flagged as "lead" (visual/coordination only, no permission difference). Managed by the Project Coordinator.
- **Session (Mentoring)**: A scheduled meeting between an assigned mentor and an entrepreneur. Any assigned mentor can conduct sessions. Tracks topics covered, notes, decisions, and which mentor conducted.
- **Assignment**: A task linked to a subject, with submission and review workflow.
- **Notification**: A message with content, recipients, delivery tracking, and audit information.
- **Project Stage**: A lifecycle phase (Registration through Closure) with state tracking.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An Incubator Admin can onboard a new project with users, diagnostic forms, and knowledge structures in under 30 minutes.
- **SC-002**: An Entrepreneur can complete a full diagnostic assessment in under 20 minutes.
- **SC-003**: A Mentor and Entrepreneur can review and finalize a mentoring plan in a single session (under 45 minutes).
- **SC-004**: The system auto-generates a session calendar within 5 seconds of receiving scheduling parameters.
- **SC-005**: 95% of users are able to select their correct context and begin working within 30 seconds of login.
- **SC-006**: No user can access data outside their authorized context, verified through access control testing across all roles.
- **SC-007**: Session reminders and task notifications are delivered within 2 minutes of their scheduled trigger time.
- **SC-008**: Zero duplicate notifications are sent for the same event to the same recipient.
- **SC-009**: The platform supports at least 50 concurrent incubators, each with up to 100 active projects, maintaining sub-200ms p95 page load time under concurrent load.
- **SC-010**: All audit-worthy events (logins, context changes, plan approvals, stage transitions) are traceable in the audit log within 24 hours of occurrence.
- **SC-011**: No unauthenticated user can determine whether a specific national ID or email is registered in the platform through any public-facing endpoint.
- **SC-012**: 100% of new user accounts require successful email verification before first access.

## Assumptions

- Users access the platform via desktop web browsers with stable internet connectivity. Native mobile apps are out of scope for this specification.
- Each incubator operates independently; cross-incubator collaboration features are not included.
- The global template library for diagnostic forms and knowledge structures is pre-populated by Global Admins before incubators begin onboarding.
- Authentication uses a vendor-neutral approach (email + password) to avoid vendor lock-in, with extensibility for future SSO/OAuth2 providers. Authorization (RBAC + context) is built on top of this.
- Email delivery relies on an external email service; the platform is responsible for composing and queuing messages, not for SMTP infrastructure.
- The scheduling engine generates a proposed calendar that can be manually adjusted; it does not integrate with external calendar systems (Google Calendar, Outlook) in this version.
- Sponsor users have strictly read-only access to project dashboards and reports; they cannot interact with diagnostics, plans, or sessions.
- File uploads for resources and assignment submissions follow standard web upload size limits. Specific limits are configured at the infrastructure level.
- The "partial sync" mode for cloned forms allows pulling new questions from the global template but does not overwrite locally modified questions.
- Subscription plan changes take effect immediately upon assignment; there is no grace period or transition mechanism.
