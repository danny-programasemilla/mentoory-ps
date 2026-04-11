
## 📌 Current Implementation Context (CRITICAL)

This work is NOT happening in isolation.

You are working on an **existing, partially implemented system** under the branch:

**`001-mentory-platform-core`**

The current implementation progress must be reviewed in:

**`/specs/001-mentory-platform-core/tasks.md`**

---

## 📊 Current State of Progress

* The implementation has already advanced **up to Phase 4**
* However, this progression is misleading in terms of quality and correctness

---

## 🛑 Hardening Decision (VERY IMPORTANT)

We are intentionally making a **pause in the forward progression**.

Even though later phases exist and have partial work, we are **NOT continuing forward**.

Instead, we are:

* stepping back conceptually
* focusing on **Phases 1, 2, and 3**
* performing a **deep hardening cycle**

---

## 🎯 Why This Hardening Is Needed

The work already implemented in:

* Phase 1
* Phase 2
* Phase 3

is **NOT aligned with the intended requirements and expected behavior**.

There are issues such as:

* flows that do not behave correctly end-to-end
* logic that passes tests but fails in real usage
* inconsistencies between intended and actual behavior
* possible misinterpretations of requirements
* incomplete or fragile implementations

---

## 🔍 Your Responsibility

You must:

1. Review the existing scope and assumptions of Phases 1–3
2. Reconcile them with the hardening requirements defined in this prompt
3. Identify:

   * incorrect implementations
   * missing pieces
   * conflicting logic
   * fragile flows
4. Propose:

   * corrected behavior
   * required adjustments
   * necessary removals
   * missing validations and state handling

---

## ⚠️ Important Constraint

Do NOT:

* continue designing Phase 4+ features
* expand scope into unrelated domains
* assume the current implementation is correct

This is a **correction and stabilization effort**, not a forward feature expansion.

---

## 🧠 Expected Outcome

After this hardening:

* Phases 1–3 must become **production-ready in behavior**
* Flows must be:

  * deterministic
  * testable
  * consistent
  * aligned with business rules
* The system must be ready to **safely continue future phases after this correction**


* Senior Product Owner
* Security Architect
* Backend Architect
* QA Lead with strong end-to-end validation criteria

This is **not** a greenfield design task.

This is a **hardening phase on top of an existing system and existing previous SpecKit outputs**.

---

## Objective

We are pausing the current implementation to perform a **deep hardening cycle focused exclusively on:**

* Public user registration
* Internal user registration
* Authentication flows
* Email verification states
* Role and context assignment
* Project enrollment and invitations
* Batch user onboarding
* Real end-to-end behavior validation

The goal is to reach **production-ready quality** for the flows and rules that already belong to the current implementation scope.

This hardening phase is meant to polish, correct, align, and complete behavior that either:

* already exists
* was already intended by prior work
* is very close to what was already defined

This is **not** intended to introduce broad new platform domains or overengineered infrastructure.

---

## Critical Scope Constraint

There are later phases where the **Notification** domain will be developed.

Because of that, although these requirements mention emails that must eventually be sent, **it is NOT the intention of this phase to implement real email delivery**.

You must make that distinction very clearly.

For this phase:

* Email sending must be treated as an **eventual capability**
* The specification must clearly state where emails must eventually be sent
* The specification may define the expected trigger points, state transitions, and integration seams
* But the specification must **not** force implementation of a real notification/email-sending subsystem in this phase
* Do not introduce overengineering around a Notification domain that belongs to later phases

At this stage, the priority is:

* correct account states
* correct invitation states
* correct transitions
* correct validation rules
* correct administrative/manual flows
* ability to manually validate key flows while real email sending is still unavailable

If useful, you may describe future integration points such as domain events or deferred notification hooks, but only as future-facing extension points, not as mandatory implementation scope for this hardening cycle.

---

## Critical Instructions

### 1. Do Not Simplify

You must not simplify, reinterpret, or compress requirements.

Every rule defined below must be preserved and translated into:

* explicit behavior
* explicit states
* explicit transitions

If there is duplicate wording, you may remove only exact redundancy, but you must not collapse meaningful distinctions.

---

### 2. Double and Triple Check Existing System

Some of this may already exist.

You must:

* validate whether the current implementation truly satisfies the requirement
* identify mismatches
* identify partial implementations
* identify flows that exist for another purpose but conflict with the desired final behavior

---

### 3. Removal Policy

If something exists in the current system that:

* was built for another purpose
* conflicts with the required behavior
* introduces ambiguity
* produces the wrong user flow
* prevents the system from being production ready

You must:

1. List it explicitly
2. Explain why it does not comply
3. Propose removal or replacement before suggesting final behavior

Do not silently preserve legacy behavior if it conflicts with this hardening objective.

---

### 4. No Hidden Logic

Do not introduce:

* implicit transitions
* silent shortcuts
* hidden system behavior
* “magic” auto-resolution not explicitly configured

All flows must be:

* deterministic
* explicit
* auditable

---

### 5. End-to-End Reality Enforcement

Existing end-to-end tests previously passed while the real system did not work.

You must define acceptance and E2E criteria in a way that:

* reflects real user journeys
* prevents false-positive passing tests
* includes failure states
* includes broken/interrupted states
* validates that the flow really works end to end, not just that internal checks passed

---

## Core Platform Principle

Users are **global across the platform**, not scoped per incubator.

A user:

* registers once
* exists globally
* can belong to multiple incubators and projects through contextual roles

---

# Public User Registration Flow

## Public Registration Page

There must be a public registration page where any person can create a new user.

---

## Minimal Registration Data

At registration time, the system must request only:

* Country of identification
* Identification number
* Email
* Password

The rest of the data may be collected later.

This registration must remain quick and minimal.

---

## Countries

Supported countries must exist in the database.

The registration page must display a dropdown sourced from database values.

Country selection must determine the identification mask and validation behavior.

For example:

* if the person selects Costa Rica
* and the identification is for a Costa Rican national ID
* the identification field must apply the proper Costa Rica mask/format for national IDs

---

## User Uniqueness Rules

A user is defined by the combination of the following validations:

1. Country + Identification must not already exist
2. Email must not already exist globally anywhere in the system

Both conditions must be validated.

Validation order must be:

1. Validate Country + Identification uniqueness
2. Validate Email uniqueness globally

Only if both conditions pass may the user continue.

---

## Password Rules

Password rules must follow industry-standard security practices for customer-facing secure passwords.

---

## Public Registration Outcome

When a user successfully registers publicly, two things must be true:

1. The system must register the user with the correct account state
2. The system must eventually require email verification before the account is considered fully usable

Because real email delivery is not yet implemented in this phase, the specification must define the correct account states and manual validation capabilities needed to support this flow now, while still making clear that actual email sending will be added later.

The user must not be allowed to access the account as a fully active verified user until verification has happened.

Since the email sending capability does not yet exist, the system must still allow manual testing and administrative/manual confirmation that a person’s account has been verified.

Do not solve this by pretending email already exists.

Do not solve this by skipping the state model.

---

## First User Experience After Login Without Project Context

When a person is newly registered and does not yet belong to an active project context, the system must not lead them into a broken or empty flow.

Instead, their dashboard/homepage must show all projects that:

* are public
* are in registration stage
* are available for self-requested enrollment

The user must be able to initiate a request to register into one of those projects.

---

# Public Project Visibility

When an incubator creates a project, the project must have an option that determines whether it appears in the public list of projects available for self-registration.

This must be explicit, not implicit.

Only projects that are:

* public
* and in registration stage

must appear on the public landing/dashboard for users without an active project association.

---

# Internal Registration / Administrative Enrollment

The internal registration flow is different from public registration.

This flow exists so that internal users can register users into a project of an incubator.

This is effectively an invitation / onboarding flow for project participation.

There must be two modes:

1. one-by-one registration
2. batch registration by file upload

---

## One-by-One Internal Registration

The internal screen must allow an authorized internal user to add one user at a time.

The data requested must be the same minimal data:

* Country
* Identification
* Email
* Password

Two scenarios must exist:

### Scenario A — verification required

The internal user marks an option indicating that email verification is required.

In that case:

* the user must remain in the correct pending verification state
* eventual email verification must still be part of the intended flow
* actual email sending is deferred to a later notification phase
* manual/system-supported validation of that state must still be possible now

### Scenario B — verification not required

The internal user marks the option so that email verification is not required.

In that case:

* the user is treated as verified immediately
* instead of a verification email, the eventual intended communication would be a welcome email
* but real email delivery is out of scope for this phase

This option must be represented explicitly in the UI as a checkbox or equivalent explicit control.

---

## Internal Failure Handling

If the person already exists or does not comply with the uniqueness rules, the flow must stop appropriately for that user.

---

# Batch Registration / Batch Project Enrollment

There must be a batch incorporation flow.

An internal user must be able to upload a file, expected to come from Excel exported as CSV, in order to onboard one or many users into a specific project inside a specific incubator.

The batch flow is not only user creation; it is project-level onboarding/invitation.

---

## Batch File Content

### Mandatory data

The minimum required data for processing must be:

* Country
* Identification
* Email

These are the most important fields and must drive processing.

### Optional demographic data

The file may also contain additional demographic data, such as:

* First name
* Last name
* Country
* Province
* Canton
* District
* Address line
* Postal code

At the beginning, the implementation will work with Costa Rica-specific location fields:

* Province
* Canton
* District

These optional fields must not cause rejection if absent.

---

## Batch Operation Idempotency

The batch operation must be idempotent.

For each row:

* if the user already exists, do not try to create the user again
* if the user already exists, verify whether the user is already associated with the project
* if the user is already associated with the project, skip that association step
* if the user exists but is not associated, continue with the project onboarding/invitation flow as appropriate

---

## Batch Processing Result

The operation must return a per-person result list.

For each person, the output must indicate:

* whether the user already existed
* whether the user was created
* whether the user was already in the project
* whether the user was newly associated or invited
* whether an invitation was generated as part of the final outcome
* the final result for that row

This result must be explicit and traceable.

---

# Project Invitation and Enrollment Rules

Registering a user into a project is not merely creating the user.

The project participation flow must be modeled as an invitation/enrollment flow.

Users must not automatically become active project participants unless the configured behavior explicitly says so.

---

## Default Invitation Principle

If the invitation flow is in effect, then:

* the person must receive an invitation to the project
* the person must accept the invitation
* only after accepting does the person become an actual participant in the project

The system must preserve that state model even if real email delivery does not yet exist.

Therefore, this phase must still model:

* invitation pending
* invitation accepted
* invitation expired
* manual or administrative means to validate and test these states while email infrastructure is not ready

---

## Configurable Flow Variants

This behavior must be configurable.

### Variant A — full flow

A newly created person may need to:

1. verify email first
2. then accept the project invitation

This means the system must preserve the sequence:

* first email verification state
* then invitation acceptance state

The system must not skip directly to project participation if the configuration requires both steps.

### Variant B — bypass behavior

The system may be configured so that:

* the user appears already verified
* the invitation is auto-accepted
* the user appears linked to the project immediately

This must be explicit and configurable.

The system must not invent shortcuts unless configuration explicitly allows them.

---

# Temporary Passwords for Batch-Created Users

When users are created by batch upload, the system must generate a temporary password for each person.

Because real email delivery is not yet implemented in this phase, the specification must define how the system preserves and exposes the necessary operational state for controlled manual testing and onboarding, without pretending that a real notification engine already exists.

The user must eventually receive or otherwise be provisioned with the temporary password through the future notification flow.

If the verification-required path is used, the future intended communication would include:

* the verification step
* the temporary password needed for first login

---

## Forced Password Change

For batch-created users, first login must require use of the temporary password.

Immediately after that, the user must be forced to set a new password.

The user must not be allowed to proceed to any further functionality until the password has been changed.

This is required even before the future notification subsystem is implemented.

---

# Link and Token Validity

The system has different types of future links/tokens, such as:

* email verification
* project invitation
* others

These different token types may have different validity periods.

Those values must be configurable.

---

## Configuration Storage

Configuration values must live in the database.

They must not be hardcoded.

This includes, at minimum, configuration related to:

* token/link expiration
* verification behavior
* invitation behavior
* resend rules
* any similar operational flow controls introduced by this hardening work

This configuration capability is one of the few genuinely new things expected from this hardening phase.

---

# Manual Validation and Re-send Capability

Because email sending is not ready yet, the system must still allow internal/manual handling of users who remain in unfinished states.

There must be a way to search for and inspect users who:

* have not verified their email
* have pending invitation acceptance
* have incomplete onboarding state

An internal user must be able to inspect that person and perform the equivalent operational actions needed for this phase.

The specification must clearly separate:

* the future action of actually re-sending an email
* the current-phase capability of regenerating the required state/token/manual step so that the workflow can still be tested and administratively progressed

---

## Verification and Invitation Reissue Behavior

The system must support the equivalent of:

* reissuing email verification
* reissuing project invitation
* generating a new token even if the current one has not expired

Since real email sending is not yet implemented, this must currently be modeled in a way that still allows:

* state correction
* manual progression
* manual validation
* future plug-in to notification delivery

---

# End-to-End Validation Requirements

You must define end-to-end validation criteria based on real behavior, not fake success signals.

That includes:

* public registration
* internal one-by-one registration
* batch registration
* user already exists cases
* user already in project cases
* pending verification cases
* pending invitation cases
* expired token cases
* reissue/regeneration flows
* temporary password first login
* forced password change
* login blocked until required state is satisfied
* manual validation/admin-assisted verification while email delivery is not implemented

Tests and acceptance criteria must not rely on email delivery actually existing in this phase.

Instead, they must validate:

* account state correctness
* token state correctness
* transition correctness
* administrative/manual operability
* readiness for future notification integration

---

# Required Output

You must produce:

## 1. Updated Functional Specification

Covering all hardening requirements above without pushing email delivery implementation prematurely into scope.

## 2. Explicit State Model

Define explicit states for:

* User account
* Verification lifecycle
* Invitation lifecycle
* Password lifecycle
* Session/login gating where applicable

## 3. Flow Definitions

Define explicit flows for:

* public registration
* internal one-by-one registration
* batch registration
* verification progression
* invitation progression
* first login with temporary password
* manual/admin progression while notifications are not implemented

## 4. Gap Analysis

Identify:

* what already exists
* what is partial
* what conflicts
* what is missing

## 5. Removal List

List:

* behaviors
* screens
* state assumptions
* legacy logic
  that should be removed because they do not comply with the intended final flow

## 6. E2E / Acceptance Test Specification

Define real, failure-resistant, manually verifiable scenarios that match the actual current implementation scope.

---

# Final Instruction

Do not assume correctness.

Do not force future notification implementation into this phase.

Do not overengineer a notification subsystem here.

Focus on leaving the registration, verification, login, invitation, and onboarding flows truly production-ready in terms of:

* rules
* states
* transitions
* validations
* administrative/manual operability
* future integration readiness
