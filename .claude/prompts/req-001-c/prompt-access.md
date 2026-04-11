You are a **Principal Security Architect and Access Control Designer**. Your task is to analyze the current access model of an existing platform called **Mentory** and produce a **foundational governance document** that will become part of the project’s **`constitution.md`** , not inside but linkded and serve as a **living reference** for all future product and engineering decisions related to access control, permissions, security boundaries, and secure implementation.

Your output must be written in **clear, professional English** and should read like a document that engineering, product, QA, and security teams will consult continuously during design, development, code review, and feature expansion.

## Core Objective

Create a **deep, practical, implementation-aware analysis** of the platform’s current role hierarchy and security needs, then transform that analysis into a **sustainable access and security constitution**. The document must establish the **non-negotiable rules** that must always be respected whenever new functionality is created or existing functionality is changed.

This is not just a summary. It must be a **decision-making reference**, a **policy baseline**, and a **working framework** for continuous use. This is a coding task, but a deep analysis and produce a governance document to handle properly the access by role to the system.

---

## Important Instructions

1. **Analyze the current situation first.**  
    Do not assume the architecture, authorization approach, or enforcement model has already been formally designed. Instead:
    - infer the likely **tech context implications**
    - infer the most appropriate **authorization model**
    - identify security gaps and ambiguous areas
    - recommend how this should be formalized going forward
2. **Treat this as an existing application**, not a greenfield product.
3. **Separate clearly**:
    - **Confirmed facts from the provided input**
    - **Reasonable inferences**
    - **Recommendations**
    - **Open questions / unresolved decisions**
4. The final output must be practical enough to become part of **`constitution.md`**, so it should be structured, durable, maintainable, and scalable as the system grows.
5. Assume the audience includes:
    - founders / product owners
    - engineering leads
    - backend and frontend developers
    - QA
    - future contributors unfamiliar with the original design

---

## Context to Analyze

Mentory currently has the following role and business structure:

### High-level hierarchy

- **Global Admin**
- **Incubator Admin**
- **Project Admin**
- **Mentor**
- **Entrepreneur**

### Business entities

- **Incubator**
- **Project**

### Relationship model

- A **Global Admin** can do **any action in the entire system**.
- A **Global Admin** also has exclusive access to **platform-level functionality and configuration**.
- A **Global Admin** can act across **all incubators, all projects, all mentors, all entrepreneurs**, and all platform settings.
- The platform is structured around **incubators**.
- Each incubator can have **multiple projects**.
- Each project can have:
    - project administrators
    - mentors
    - entrepreneurs

### Visibility / scope principle

The hierarchy behaves like a tree:

- each role should only see what belongs to its scope and below
- access is restricted to the branch of the hierarchy the user belongs to
- users must not access data outside their allowed incubator/project boundaries
- this boundary enforcement must be strongly reinforced throughout the entire software lifecycle

### Explicit role rules already defined

#### Global Admin

- Can perform **all actions**
- Is the **only** role that can access anything categorized as **Platform**
- Is the **only** role that can create **new incubators**
- Is the **only** role that can create **users of any type for any role**
- Can create users for any incubator, any project, and assign them accordingly
- User creation still follows the known onboarding flows such as **email verification**, etc.

#### Incubator Admin

- Can only see and operate within **their incubator**
- Can create users only within **their own incubator scope**
- Can create:
    - another incubator admin for the same incubator
    - project admins for projects inside the same incubator
    - mentors for projects inside the same incubator
    - entrepreneurs for projects inside the same incubator

#### Project Admin

- Can only see and operate within **their assigned project(s)**
- Can work with project-level configuration and operational functions
- Can define and manage **diagnostic forms** for a project
- Can assign mentors to a project
- Can assign mentors to entrepreneurs
- One entrepreneur can have **more than one mentor**

#### Incubator Admin and Project Admin

- Both can define / manipulate **diagnostic forms** for a project
- Both need access to create and manage these diagnostic forms, always constrained by scope

### Additional instruction for the constitution

A recurring development failure today is that teams are **not consistently asking**:

- what role does this feature belong to?
- who can access it?
- what scope rules apply?
- what are the security implications?

The document you produce must correct that by making these questions mandatory in the delivery process.

---

## What You Must Produce

Create a document with the following sections.

### 1. Executive Summary

Summarize:

- the current access structure
- the major security goals
- the main risks if access control is implemented inconsistently
- the purpose of this constitution

### 2. Confirmed Model vs Inferred Model

Create a section with:

- **Confirmed facts from input**
- **Inferred architecture/security assumptions**
- **Recommended formal model moving forward**

Here you must explicitly determine:

- what kind of authorization model best fits this system:
    - pure RBAC
    - hierarchical RBAC
    - RBAC + scoped tenancy
    - RBAC + ABAC
    - another hybrid
- what technical enforcement model would likely be appropriate for an existing SaaS platform like this
- what data boundaries are implied by the hierarchy

### 3. Access & Security Constitution

Write the core constitution statements as **non-negotiable rules**.  
These should be phrased as durable governance rules, such as:

- all functionality must be classified before implementation
- every endpoint/use case must define actor, action, resource, and scope
- platform-category features are Global Admin only
- no user may access data outside assigned scope
- frontend visibility never replaces backend authorization
- etc.

This section should feel like a true **constitution**.

### 4. Domain Scope Model

Define the authorization scopes and inheritance model clearly:

- platform scope
- incubator scope
- project scope
- mentor relationship scope
- entrepreneur relationship scope

Explain:

- downward visibility
- prohibited lateral access
- prohibited upward access
- multi-assignment implications
- how mentor-to-entrepreneur relationships should be interpreted securely

### 5. Role Catalog

For each role, provide:

- business purpose
- allowed scope
- high-risk permissions
- user creation capabilities
- operational capabilities
- restrictions
- explicit “must never” statements

Roles:

- Global Admin
- Incubator Admin
- Project Admin
- Mentor
- Entrepreneur

Where information is missing for Mentor and Entrepreneur, do not invent recklessly. Instead:

- define what is known
- define what is implied
- define open questions that must be resolved before implementation

### 6. Sustainable Permission Matrix

Design a **maintainable permission matrix format** that can grow over time.

You must:

- propose the best long-term structure for the matrix
- explain why it is sustainable
- show a sample matrix with columns such as:
    - capability / action
    - resource
    - platform category?
    - global admin
    - incubator admin
    - project admin
    - mentor
    - entrepreneur
    - allowed scope
    - constraints
    - audit requirement
    - notes / open questions

Include at least an initial matrix covering:

- create incubator
- create user
- assign role
- assign user to incubator
- assign user to project
- view incubator
- update incubator
- create project
- view project
- update project
- create diagnostic form
- edit diagnostic form
- publish diagnostic form
- assign mentor to project
- assign mentor to entrepreneur
- view entrepreneur data
- manage platform settings

### 7. Security Design Principles

Define the major security principles this system must follow, including:

- least privilege
- deny by default
- explicit scope validation
- backend enforcement as source of truth
- defense in depth
- separation of duties where applicable
- auditability
- secure onboarding / identity verification
- secure role assignment and reassignment
- secure deprovisioning / offboarding

### 8. Threat and Failure Analysis

Provide a deep analysis of likely risks and failure modes, including examples such as:

- broken access control
- IDOR / BOLA
- trusting frontend role filtering
- incorrect scope joins in database queries
- role escalation during user creation or assignment
- cross-incubator data leakage
- cross-project data leakage
- mentor seeing unauthorized entrepreneur data
- stale permissions after reassignment
- soft-deleted or archived entity leakage
- unauthorized access via exports, reports, notifications, search, or background jobs

For each risk, include:

- why it matters here
- typical implementation mistakes
- required safeguards

### 9. Enforcement Model Recommendations

Recommend how this should be enforced across the stack, even if the current implementation is unknown.

Address:

- backend authorization architecture
- route/endpoint guards
- service-layer policy checks
- query-layer scope enforcement
- database considerations
- token/session claims
- role-to-scope resolution
- validation for user creation and assignment flows
- audit logs
- testing strategy
- admin action traceability

Make this section implementation-aware but not overly framework-specific.

### 10. Secure Feature Design Workflow

Create a mandatory workflow/checklist that developers must go through before shipping any feature.

This must include required questions such as:

- what business capability is being added?
- which role(s) may perform it?
- what scope applies?
- does it belong to platform / incubator / project?
- can the action affect users, permissions, or visibility?
- what backend checks enforce it?
- what audit events are required?
- what tests prove security boundaries?

Make this section strong enough to be adopted as a delivery gate.

### 11. Testing and Verification Requirements

Define what should be tested for access/security at minimum:

- positive authorization tests
- negative authorization tests
- cross-scope isolation tests
- multi-role tests
- assignment change tests
- regression tests
- UI hiding vs backend enforcement tests
- audit logging verification

### 12. Open Questions / Decisions Needed

Create a final section listing unresolved items that must be decided later, especially for:

- Mentor permissions
- Entrepreneur permissions
- whether Incubator Admin can create projects
- whether Project Admin can create entrepreneurs directly or only manage existing assignments
- whether users can have multiple roles across scopes
- impersonation / support access
- archived entities and historical access
- notification visibility rules
- reporting/export permissions

### 13. Final Deliverable Format

At the end, provide:

1. a **constitution-ready version** suitable for inclusion in `constitution.md`
2. a **concise appendix** with the initial permission matrix
3. a **developer checklist**
4. a **short list of immediate next actions** to formalize the model in the existing application

---

## Output Requirements

- Write in **English**
- Use **clear headings**
- Be **deep and rigorous**
- Prefer **precise language over generic advice**
- Do **not** give shallow best practices without tying them to this platform
- Where ambiguity exists, call it out explicitly
- Do not assume missing permissions as facts
- Make the document feel like something a real engineering organization would adopt

---

## Tone and Standard

Use the tone of a senior security architect writing an internal governance artifact for a software product with sensitive multi-tenant boundaries.

The document must be:

- authoritative
- practical
- scalable
- suitable for long-term maintenance
- directly useful for implementation and code review

---

## Final Instruction

Produce the response as if it will be copied directly into the project’s governance documentation. Optimize for **clarity, enforceability, scalability, and security rigor**.

---

**Key Improvements:**

- Reframed your request into a **high-authority Claude prompt** aimed at producing a true governance artifact, not just a generic analysis.
- Added a structure that forces Claude to distinguish **facts, inferences, recommendations, and open questions**, which is crucial for an existing system with partial definition.
- Turned your role hierarchy into a **formal access model analysis task**, including matrix design, security invariants, threat modeling, and enforcement guidance.
- Included a **developer workflow and release gate**, so the output becomes operational and not just descriptive.
- Preserved your core business rules exactly, especially:
    - Global Admin = full access
    - Platform category = Global Admin only
    - Incubator scope isolation
    - Project scope isolation
    - controlled user creation and assignment flows
    - diagnostic form permissions
    - mentor assignment rules

**Techniques Applied:**  
Role assignment, context layering, governance framing, structured decomposition, constraint-based prompting, implementation-aware security analysis.