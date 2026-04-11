# Data Model: Access & Security Constitution

**Feature**: 005-access-security-constitution  
**Date**: 2026-04-08  
**Purpose**: Define the document structure and content model for the governance artifact

## Document Entity: Access & Security Constitution

This feature's deliverable is a structured Markdown document, not a data schema. The "data model" defines the document's section hierarchy, content requirements, and relationships between sections.

## Document Structure

### Top-Level Metadata

| Field | Value | Source |
|-------|-------|--------|
| Title | Mentoory Access & Security Constitution | FR-019 |
| Version | 1.0.0 | New document, initial ratification |
| Location | `.specify/memory/access-security-constitution.md` | Clarification R8 |
| Versioning | Independent semantic versioning (MAJOR.MINOR.PATCH) | Clarification R8 |
| Language | English | FR-019 |

### Section Hierarchy (13 sections per spec)

```text
access-security-constitution.md
├── [Sync Impact Report]           # HTML comment block (matches constitution.md pattern)
│
├── 1. Executive Summary           # FR-001
│   ├── Current access structure
│   ├── Major security goals
│   ├── Primary risks
│   └── Purpose of this constitution
│
├── 2. Confirmed Model vs Inferred Model   # FR-002, FR-003
│   ├── Confirmed facts (from codebase)
│   ├── Inferred assumptions
│   ├── Recommended formal model
│   └── Authorization model determination (hierarchical RBAC + scoped tenancy)
│
├── 3. Access & Security Constitution Rules   # FR-004
│   └── Non-negotiable governance statements (numbered rules)
│
├── 4. Domain Scope Model           # FR-005
│   ├── Platform scope
│   ├── Incubator scope
│   ├── Project scope
│   ├── Mentor relationship scope
│   ├── Entrepreneur relationship scope
│   ├── Downward visibility rules
│   ├── Prohibited lateral access
│   ├── Prohibited upward access
│   └── Multi-assignment implications
│
├── 5. Role Catalog                 # FR-006, FR-007
│   ├── GlobalAdmin
│   ├── IncubatorAdmin
│   ├── ProjectCoordinator
│   ├── Mentor
│   ├── Entrepreneur
│   └── Sponsor
│   │   (each with: business purpose, allowed scope, high-risk permissions,
│   │    user creation capabilities, operational capabilities, restrictions,
│   │    "must never" statements, open questions for under-specified roles)
│   └── Glossary / Terminology      # FR-017
│       └── ProjectCoordinator canonical; "Project Admin" deprecated alias
│
├── 6. Sustainable Permission Matrix   # FR-008, FR-009
│   ├── Matrix format explanation
│   ├── Why format is sustainable
│   └── Initial matrix (17+ rows)
│       Columns: Action | Resource | Platform? | GA | IA | PC | M | E | S | Scope | Constraints | Audit | Notes
│
├── 7. Security Design Principles   # FR-010
│   ├── Least privilege
│   ├── Deny by default
│   ├── Explicit scope validation
│   ├── Backend enforcement as source of truth
│   ├── Defense in depth
│   ├── Separation of duties
│   ├── Auditability
│   ├── Secure onboarding / identity verification
│   ├── Secure role assignment / reassignment
│   └── Secure deprovisioning / offboarding
│
├── 8. Threat and Failure Analysis   # FR-011, FR-012
│   ├── Broken access control
│   ├── IDOR / BOLA
│   ├── Trusting frontend role filtering
│   ├── Incorrect scope joins in queries
│   ├── Role escalation during user creation
│   ├── Cross-incubator data leakage
│   ├── Cross-project data leakage
│   ├── Mentor seeing unauthorized entrepreneur data
│   ├── Stale permissions after reassignment
│   ├── Soft-deleted / archived entity leakage
│   └── Unauthorized access via exports/reports/notifications/search/background jobs
│       (each with: why it matters, typical mistakes, required safeguards)
│
├── 9. Enforcement Model Recommendations   # FR-013
│   ├── Backend authorization architecture
│   ├── Route/endpoint guards
│   ├── Service-layer policy checks
│   ├── Query-layer scope enforcement
│   ├── Database considerations
│   ├── Token/session claims
│   ├── Role-to-scope resolution
│   ├── User creation/assignment validation
│   ├── Audit logs
│   ├── Testing strategy
│   └── Admin action traceability
│
├── 10. Secure Feature Design Workflow   # FR-014
│   ├── Mandatory checklist questions
│   ├── PR template integration (ad-hoc changes)
│   └── SpecKit checklist integration (planned features)
│
├── 11. Testing and Verification Requirements   # FR-015
│   ├── Positive authorization tests
│   ├── Negative authorization tests
│   ├── Cross-scope isolation tests
│   ├── Multi-role tests
│   ├── Assignment change tests
│   ├── Regression tests
│   ├── UI hiding vs backend enforcement tests
│   └── Audit logging verification
│
├── 12. Open Questions / Decisions Needed   # FR-016
│   ├── Mentor permissions
│   ├── Entrepreneur permissions
│   ├── Sponsor permissions
│   ├── IncubatorAdmin project creation
│   ├── ProjectCoordinator entrepreneur creation
│   ├── Multi-role across scopes
│   ├── Impersonation / support access
│   ├── Archived entity access
│   ├── Notification visibility rules
│   └── Reporting/export permissions
│
└── Appendices                      # FR-018
    ├── A. Permission Matrix (compact)
    ├── B. Developer Checklist (standalone)
    └── C. Immediate Next Actions
```

## Content Tagging Model

Every factual statement in sections 1-5 must be tagged with one of:

| Tag | Meaning | Visual |
|-----|---------|--------|
| **[CONFIRMED]** | Verified from codebase or explicit business rule | Bold prefix |
| **[INFERRED]** | Reasonable assumption from codebase patterns | Bold prefix |
| **[RECOMMENDED]** | Author recommendation, not yet ratified | Bold prefix |
| **[OPEN]** | Unresolved decision requiring stakeholder input | Bold prefix |

Per SC-006, every statement must carry exactly one tag. Tags are used in sections 1 (Executive Summary), 2 (Confirmed vs Inferred), 4 (Scope Model), and 5 (Role Catalog).

## Permission Matrix Row Schema

Each row in section 6 follows this schema:

| Column | Type | Required | Description |
|--------|------|----------|-------------|
| Action | string | Yes | Verb-noun capability (e.g., "Create Incubator") |
| Resource | string | Yes | Target entity (e.g., "Incubator") |
| Platform? | boolean | Yes | Whether this is a platform-level action (GlobalAdmin only) |
| GlobalAdmin | access | Yes | One of: FULL, SCOPED, READ, NONE |
| IncubatorAdmin | access | Yes | Same access values |
| ProjectCoordinator | access | Yes | Same access values |
| Mentor | access | Yes | Same access values |
| Entrepreneur | access | Yes | Same access values |
| Sponsor | access | Yes | Same access values |
| Scope | string | Yes | Platform / Incubator / Project |
| Constraints | string | No | Additional conditions (e.g., "own incubator only") |
| Audit | string | Yes | One of: REQUIRED, RECOMMENDED, NONE |
| Notes | string | No | Open questions or clarifications |

## Role Catalog Entry Schema

Each role entry in section 5 follows this structure:

| Field | Required | Description |
|-------|----------|-------------|
| Business Purpose | Yes | Why this role exists |
| Allowed Scope | Yes | Platform / Incubator / Project |
| High-Risk Permissions | Yes | Permissions with elevated security concern |
| User Creation Capabilities | Yes | What user types this role can create |
| Operational Capabilities | Yes | What business functions this role performs |
| Restrictions | Yes | What this role cannot do |
| "Must Never" Statements | Yes | Explicit prohibitions |
| Open Questions | Conditional | Required for under-specified roles (Mentor, Entrepreneur, Sponsor) |

## Threat Entry Schema

Each threat entry in section 8 follows this structure:

| Field | Required | Description |
|-------|----------|-------------|
| Threat Name | Yes | OWASP-aligned category name |
| Why It Matters Here | Yes | Platform-specific risk explanation |
| Typical Implementation Mistakes | Yes | Concrete anti-patterns |
| Required Safeguards | Yes | Mandatory preventive measures |

## Cross-References

| From Section | To Section | Relationship |
|-------------|------------|-------------|
| 5. Role Catalog | 6. Permission Matrix | Roles reference matrix for detailed permissions |
| 6. Permission Matrix | 12. Open Questions | Matrix rows with notes link to open decisions |
| 8. Threat Analysis | 9. Enforcement Model | Each threat's safeguards reference enforcement patterns |
| 10. Feature Checklist | 6. Permission Matrix | Checklist requires consulting matrix |
| 10. Feature Checklist | 11. Testing Requirements | Checklist requires defining tests per testing section |
| 3. Constitution Rules | All sections | Rules are the governing authority for all other sections |
