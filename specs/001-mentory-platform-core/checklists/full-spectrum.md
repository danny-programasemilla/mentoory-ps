# Full-Spectrum Deployment Gate Checklist: Mentory Enterprise SaaS Platform

**Purpose**: Thorough, formal deployment gate validating requirement quality across security, architecture, data model, UX, and cross-cutting concerns
**Created**: 2026-03-31
**Feature**: [spec.md](../spec.md) | [plan.md](../plan.md) | [tasks.md](../tasks.md)
**Depth**: Thorough | **Audience**: Formal gate (pre-deployment) | **Scope**: All 8 user stories, 59 FRs, 12 SCs

---

## Security & Authentication Requirements

- [ ] CHK001 - Are brute-force mitigation thresholds specified for all authentication endpoints (login, registration, password reset), not just login? [Completeness, Spec §FR-054, §FR-055]
- [ ] CHK002 - Is the session invalidation behavior fully specified when a user's role is revoked mid-session? [Edge Case, Spec §Edge Cases]
- [ ] CHK003 - Are timing-safe comparison requirements documented for password verification and token validation to prevent timing attacks? [Gap, Spec §FR-049]
- [ ] CHK004 - Is the email verification token expiry duration specified with a concrete value (e.g., 24 hours)? [Clarity, Spec §FR-051]
- [ ] CHK005 - Are password reset token expiry and single-use constraints explicitly defined? [Gap, Spec §FR-049]
- [ ] CHK006 - Is the session cookie configuration specified (HttpOnly, Secure, SameSite, expiry, sliding vs. absolute)? [Gap, Plan §Authentication]
- [ ] CHK007 - Are CSRF protection requirements defined for all state-mutating endpoints, not just implicitly via anti-forgery? [Completeness, Plan §Program.cs]
- [ ] CHK008 - Is the honeypot bot detection mechanism specified with enough detail for implementation (field names, validation logic, failure behavior)? [Clarity, Spec §FR-054]
- [ ] CHK009 - Are requirements defined for what happens when a locked-out account receives a valid password reset request? [Edge Case, Gap, Spec §FR-055]
- [ ] CHK010 - Is the password history depth specified (how many previous passwords are checked to prevent reuse)? [Gap, Spec §FR-056]

## Multi-Tenancy & Data Isolation Requirements

- [ ] CHK011 - Are tenant isolation requirements specified for all 8 DbContexts, not just TenantDbContext? [Coverage, Spec §FR-001, Plan §DbContexts]
- [ ] CHK012 - Is the behavior specified when a global query filter is accidentally bypassed (e.g., raw SQL, AsNoTracking)? [Edge Case, Gap, Spec §FR-001]
- [ ] CHK013 - Are cross-tenant data leakage acceptance test requirements documented with specific test scenarios? [Measurability, Spec §Edge Cases]
- [ ] CHK014 - Is the tenant context propagation path fully specified from middleware through MediatR pipeline to DbContext filter? [Completeness, Plan §Authorization]
- [ ] CHK015 - Are requirements defined for administrative queries that must span tenants (e.g., GlobalAdmin listing all incubators)? [Gap, Spec §FR-004]

## Authorization & RBAC Requirements

- [ ] CHK016 - Does the permission matrix in FR-010 cover all modules and actions defined across all 8 user stories? [Coverage, Spec §FR-010]
- [ ] CHK017 - Are permissions defined for the GlobalAdmin's ability to correct diagnostic answers (FR-018b lists them as authorized, but the matrix omits GlobalAdmin from "Correct answers")? [Conflict, Spec §FR-010 vs §FR-018b]
- [ ] CHK018 - Is the behavior specified when a user's active context becomes invalid after role revocation (e.g., redirect target, error message)? [Edge Case, Spec §Edge Cases]
- [ ] CHK019 - Are authorization requirements defined for API/AJAX endpoints separately from page-level access? [Gap, Spec §FR-013]
- [ ] CHK020 - Is the Sponsor role's read-only access scope precisely bounded — which dashboards, which data points, which projects? [Clarity, Spec §FR-003, §Assumptions]

## Domain Model & Data Consistency Requirements

- [ ] CHK021 - Is the relationship between FormTemplate and KnowledgeTemplate specified at the data model level (how "implicit selection" in FR-024 is persisted)? [Clarity, Spec §FR-024]
- [ ] CHK022 - Are optimistic concurrency requirements specified for all aggregates that support concurrent editing (mentoring plans, diagnostic forms)? [Completeness, Spec §Edge Cases]
- [ ] CHK023 - Is the cascade behavior defined when a global template is deleted after clones exist? [Edge Case, Spec §Edge Cases]
- [ ] CHK024 - Are soft-delete vs. hard-delete requirements specified per entity type? [Gap, Plan §SoftDeletableEntity]
- [ ] CHK025 - Is the score aggregation algorithm specified with enough precision to produce deterministic results (rounding, overflow, null handling)? [Clarity, Spec §FR-025]
- [ ] CHK026 - Are data retention requirements defined for audit logs, completed diagnostic responses, and archived projects? [Gap]
- [ ] CHK027 - Is the ExternalId generation strategy specified (random GUID, sequential, version)? [Gap, Constitution §III]

## CQRS & Architecture Requirements

- [ ] CHK028 - Are integration event ordering guarantees specified (at-least-once, at-most-once, exactly-once)? [Gap, Constitution §IV]
- [ ] CHK029 - Is the behavior specified when an integration event handler fails (retry, dead-letter, compensating action)? [Gap, Plan §IntegrationEvents]
- [ ] CHK030 - Are transactional boundaries clearly defined — does each command handler operate within a single DbContext transaction? [Clarity, Plan §TransactionBehavior]
- [ ] CHK031 - Is the MediatR pipeline behavior order specified (validation → transaction → handler)? [Completeness, Plan §Behaviors]

## Diagnostic & Scoring Requirements

- [ ] CHK032 - Are the question type behaviors fully specified for "numeric" type (min/max constraints, decimal support, validation rules)? [Clarity, Spec §FR-016]
- [ ] CHK033 - Is the multi-select scoring behavior specified — does selecting multiple options sum their scores or use a different aggregation? [Ambiguity, Spec §FR-016]
- [ ] CHK034 - Are requirements defined for handling questions with zero answer options or zero-score options? [Edge Case, Gap, Spec §FR-016]
- [ ] CHK035 - Is the "partial sync" conflict resolution specified — what happens when a locally modified question has the same ID as a new template question? [Clarity, Spec §FR-019]
- [ ] CHK036 - Are requirements defined for the maximum form size (number of questions, nesting depth of follow-ups)? [Gap, Spec §FR-018]

## Knowledge & Mentoring Plan Requirements

- [ ] CHK037 - Are the configurable priority score ranges specified with constraints (non-overlapping, exhaustive, minimum range width)? [Clarity, Spec §FR-025a]
- [ ] CHK038 - Is the behavior specified when a topic referenced in the mentoring plan is deleted from the knowledge structure? [Edge Case, Gap]
- [ ] CHK039 - Are resource upload requirements specified (max file size, allowed MIME types, storage location)? [Gap, Spec §FR-021, §Assumptions]
- [ ] CHK040 - Is the session calendar generation algorithm specified with enough detail to be deterministic (distribution strategy, weekend handling, time zones)? [Clarity, Spec §FR-028]

## Lifecycle & Workflow Requirements

- [ ] CHK041 - Are stage advancement preconditions defined for each of the 7 stages (what must be "Completed" before advancing)? [Gap, Spec §FR-036, §FR-037]
- [ ] CHK042 - Is the behavior specified when a stage is advanced while dependent work is incomplete (e.g., advancing past Forms before all entrepreneurs complete diagnostics)? [Edge Case, Gap, Spec §FR-037]
- [ ] CHK043 - Are requirements defined for what UI elements and actions are available/hidden at each specific stage? [Coverage, Spec §FR-039]
- [ ] CHK044 - Is the project archival behavior at Closure fully specified (data accessibility, read-only enforcement, notification to participants)? [Completeness, Spec §US7 Scenario 6]

## Notification Requirements

- [ ] CHK045 - Are the specific events that trigger notifications exhaustively listed, or is it left to implementation discretion? [Coverage, Spec §FR-040]
- [ ] CHK046 - Is the deduplication scope specified — per recipient per event, per recipient per event type per time window, or another granularity? [Clarity, Spec §FR-040]
- [ ] CHK047 - Are email template requirements specified (HTML vs plain text, branding, required fields, Spanish content)? [Gap, Constitution §IX, Spec §FR-044]
- [ ] CHK048 - Is the scheduled notification timing precision defined — "within 2 minutes" per SC-007, but is polling interval or push mechanism specified? [Clarity, Spec §SC-007]
- [ ] CHK049 - Are notification preference granularity requirements clear — per notification type, per role, per context, or per type+role+context combination? [Clarity, Spec §FR-043]

## Subscription & Feature Gating Requirements

- [ ] CHK050 - Is the subscription enforcement point specified — at command level, controller level, or both? [Gap, Spec §FR-034]
- [ ] CHK051 - Are all the specific boolean and quantitative features enumerated, or is the feature list left undefined? [Gap, Spec §FR-032]
- [ ] CHK052 - Is the behavior specified when an incubator's plan is downgraded below current usage (e.g., project limit reduced below existing count)? [Completeness, Spec §Edge Cases]

## UX, Accessibility & Internationalization Requirements

- [ ] CHK053 - Are error message requirements defined for all user-facing validation failures in Spanish with specific wording? [Coverage, Constitution §IX]
- [ ] CHK054 - Are keyboard navigation and screen reader requirements defined for all interactive UI elements (DataTables, tree views, form wizards)? [Gap]
- [ ] CHK055 - Is the context switcher behavior fully specified — what happens when the user switches context mid-operation (unsaved form, in-progress wizard)? [Edge Case, Gap, Spec §FR-005]
- [ ] CHK056 - Are loading states and empty states defined for all list views and dashboards? [Gap]
- [ ] CHK057 - Are the form auto-save requirements (30s interval, 5s debounce per plan) specified in the spec or only in the plan? [Traceability, Plan §UI Framework]

## Non-Functional & Operational Requirements

- [ ] CHK058 - Are health check endpoint requirements specified with expected response format and failure thresholds? [Gap, Plan §Aspire]
- [ ] CHK059 - Is the logging strategy specified — what events are logged, at what level, with what structured fields? [Gap]
- [ ] CHK060 - Can SC-009 ("sub-200ms p95 page load") be verified without a load testing task or infrastructure? [Measurability, Spec §SC-009]
- [ ] CHK061 - Are database backup and disaster recovery requirements defined? [Gap]
- [ ] CHK062 - Is the SMTP failure handling specified — retry count, backoff strategy, dead-letter behavior for failed emails? [Gap, Plan §EmailService]

## Cross-Artifact Consistency

- [ ] CHK063 - Are all 59 functional requirements traceable to at least one task in tasks.md? [Traceability, Spec → Tasks]
- [ ] CHK064 - Do the plan's 8 bounded contexts align one-to-one with the spec's domain modules, with no orphaned or missing contexts? [Consistency, Plan §Solution Structure vs Spec §FRs]
- [ ] CHK065 - Are the data model entities in data-model.md consistent with the Key Entities section in spec.md (no missing or extra entities)? [Consistency, Spec §Key Entities]

## Notes

- Check items off as completed: `[x]`
- Add findings or references inline after each item
- Items marked `[Gap]` indicate requirements that may need to be added to spec.md before deployment
- Items marked `[Conflict]` indicate inconsistencies that must be resolved
- CHK017 specifically flags a permission matrix inconsistency discovered during analysis
