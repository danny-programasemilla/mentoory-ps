---
access-security: true
---

# Feature Specification: Registration & Access Hardening

**Feature Branch**: `016-registration-access-hardening`
**Created**: 2026-04-18
**Status**: Draft
**Input**: User description: "Access hardening: FR-052 generic registration errors, FR-053 admin enrollment controller with specific feedback, FR-056 password-contains-email-or-ID validator"

## Overview

Three narrow hardening items are being addressed together because they all touch the same account-creation code path and must ship as one consistent change. All three close gaps against requirements already ratified in the platform-core specification (spec 001, FR-052 / FR-053 / FR-056):

1. **Close the public-endpoint enumeration oracle.** The public self-registration form today returns distinct field-attributed errors when a submitted national ID or email is already registered. An unauthenticated attacker can probe the form to learn whether any given national ID or email corresponds to an existing account. Public registration must instead return one indistinguishable generic failure for all validation-and-uniqueness outcomes.

2. **Give administrative enrollment its own specific-feedback path.** Incubator admins enrolling users on behalf of the organisation need to know exactly which field conflicts (email vs national ID) so they can efficiently resolve duplicates. The admin UI currently shares the same command as the public form, so it currently either leaks on the public path or degrades admin UX — it must be separated so public stays generic and admin stays specific.

3. **Reject passwords that embed the user's identifying data.** The existing password rule enforces length, case, digits, and a special character, but does not prevent a user from choosing a password that contains their own email address or national ID. Policy (FR-056) requires rejecting such passwords.

## Clarifications

### Session 2026-04-18

- Q: Does the public-registration response also need to be indistinguishable between *success* and *uniqueness-conflict failure*? → A: Yes — close the oracle fully. A submission that passed all validator rules but conflicted on an existing account MUST land on the same "check your email" confirmation page as a genuinely new registration. No account is created on the conflict path, and no verification email is sent. Only validator-rule failures (format, length, missing fields, password rules) continue to render the generic in-form failure.
- Q: What is the minimum substring length before a password is rejected for containing the user's email local part or national ID? → A: **4 characters.** The full email address (always contains `@`, always ≥ 4 characters in practice) is always checked. The email local part (the portion before `@`) and the national ID are checked only when they are 4 characters or longer; below that threshold they are skipped so that trivially-short values (`a@b.co`, a 3-letter identifier) don't cause pervasive false-positive rejections.
- Q: For the password-contains-national-ID check, is the national ID compared verbatim as stored, or in a separator-stripped form? → A: **Both.** The check compares the password against (i) the national ID exactly as stored and (ii) the national ID with all non-alphanumeric characters removed. Reject on either match. The 4-character minimum-length threshold applies to both forms: if the stripped form ends up shorter than 4 characters, the stripped check is skipped.
- Q: For logging public-registration failures, what information about the submission does the server log? → A: **Email address yes, national ID redacted.** Each public-registration outcome log entry records: submitted email, outcome code (e.g. `Success`, `DuplicateEmail`, `DuplicateNationalId`, `ValidatorFailure:<rule>`), a correlation ID that ties the log to the HTTP request, and the client IP. The national ID is redacted at the log surface — only the database retains the canonical form. This matches the existing precedent of logging email on successful registration while treating the government-issued identifier as higher-sensitivity PII.

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Close the public registration enumeration oracle (Priority: P1)

A prospective entrepreneur (unauthenticated) submits the public registration form. Two response shapes are possible:

- **Rule-level failure** — a required field is missing, the email is malformed, the password fails one of the strength rules, etc. The form re-renders with a single generic message ("No fue posible completar el registro. Revise los datos e intente nuevamente.") and carries no field-level attribution.
- **Passed all rules** — the submission went through every validator. Whether the combination is genuinely new or conflicts with an existing account on email or national ID, the browser lands on the same confirmation page ("Revise su correo electrónico para confirmar su cuenta"). On the genuinely-new path an account is created and a verification email is sent; on the conflict path no account is created and no email is sent, but the response the unauthenticated submitter sees is identical.

**Why this priority**: This is a shipped security vulnerability. Any unauthenticated visitor can iterate through candidate emails or national IDs and use today's distinguishable responses as an oracle to confirm account existence. Closing it requires both (a) collapsing failures and (b) making the "rules all passed" response identical regardless of uniqueness outcome — otherwise the oracle persists as "success-vs-failure".

**Independent Test**: The public flow can be exercised end-to-end without touching admin enrollment or password rules. Submit the public form with (a) a fresh combination, (b) a duplicate national ID with otherwise-valid data, (c) a duplicate email with otherwise-valid data, (d) a validator-rule failure (e.g., malformed email, short password), and verify that paths (a)–(c) return byte-for-byte identical responses (the confirmation page) and that path (d) returns the generic in-form failure without field attribution.

**Acceptance Scenarios**:

1. **Given** an unauthenticated user, **When** they submit the public registration form with a fresh, valid combination, **Then** an account is created, a verification email is sent, and the browser lands on the "Revise su correo electrónico para confirmar su cuenta" confirmation page.
2. **Given** an unauthenticated user, **When** they submit the public registration form with a national ID already registered (all other fields otherwise valid), **Then** no account is created and no verification email is sent, but the browser lands on the exact same confirmation page as scenario 1 — byte-for-byte identical visible response.
3. **Given** an unauthenticated user, **When** they submit the public registration form with an email already registered (all other fields otherwise valid), **Then** no account is created and no verification email is sent, but the browser lands on the exact same confirmation page as scenario 1.
4. **Given** an unauthenticated user, **When** they submit the public registration form with a rule-level failure (missing field, malformed email, weak password), **Then** the form re-renders with the single generic failure message and no field-level attribution.
5. **Given** two submissions that differ only in the chosen email or national ID (one fresh, one conflicting), **When** an observer compares the two responses, **Then** they cannot distinguish which submission conflicted on uniqueness.

### User Story 2 — Admin enrollment returns specific field feedback (Priority: P1)

An incubator admin uses the administrative enrollment UI to add a new member on behalf of the organisation. When the submission conflicts with an existing account, the admin sees a specific message identifying which field is the conflict: either "Ya existe una cuenta con este número de identificación" against the national ID field, or "Ya existe una cuenta con este correo electrónico" against the email field. This preserves the efficient resolution workflow for admins, while the public path (User Story 1) remains generic.

**Why this priority**: Admins need this feedback to resolve duplicates efficiently. Without it, this feature cannot ship — the public fix alone would regress admin UX. The two stories ship together.

**Independent Test**: Exercise only the admin enrollment UI as an authenticated user with the permitted role. Submit with a duplicate national ID and with a duplicate email and verify that each returns specific, field-attributed feedback. This path is reachable only behind authentication, so the feedback specificity is not usable as an enumeration oracle.

**Acceptance Scenarios**:

1. **Given** an authenticated incubator admin on the enrollment form, **When** they submit a national ID already registered, **Then** the form re-renders with a message identifying the national ID field as the conflict.
2. **Given** an authenticated incubator admin on the enrollment form, **When** they submit an email already registered, **Then** the form re-renders with a message identifying the email field as the conflict.
3. **Given** an unauthenticated user, **When** they attempt to reach the admin enrollment endpoint directly, **Then** access is denied (no information about the form's behaviour is returned).
4. **Given** a valid admin submission, **When** the admin enrolls a fresh user, **Then** the user is enrolled and the admin is returned to the member list with a success indicator, as today.

### User Story 3 — Passwords cannot contain the user's identifying data (Priority: P2)

A user choosing a password at registration (or at admin enrollment, or during a future password change) cannot use the email address or the national ID they are registering. If they try, the password is rejected with a clear message ("La contraseña no puede contener su correo electrónico ni su número de identificación."). This rule layers on top of the existing length / case / digit / special-character rules.

**Why this priority**: This closes a material weakness in password policy — identifying data is low-entropy and trivially discoverable. It is scoped P2 because it is a rule addition rather than a security vulnerability with an active exploit vector, and it ships in the same release as Stories 1 and 2 once its wording and surface are agreed.

**Independent Test**: The password rule is a validator addition. It can be tested at the validator level by submitting a password containing the email or national ID against otherwise-valid inputs and confirming rejection. It is independent of the response-shape changes in Stories 1 and 2 because the validator runs before or alongside uniqueness checks, and a rejection at this rule is one of the validator failures already subsumed by Story 1's generic public response.

**Acceptance Scenarios**:

1. **Given** a user registering with email `jane.doe@example.com`, **When** they submit a password of `Jane.Doe@Example.com1!`, **Then** the password is rejected.
2. **Given** a user registering with national ID `9-123-4567`, **When** they submit a password of `Secure9-123-4567!` (contains verbatim form) OR a password of `Secure91234567!` (contains separator-stripped form), **Then** the password is rejected in both cases.
3. **Given** a user choosing a password that does not contain their email or national ID, **When** the password otherwise meets the length, case, digit, and special-character rules, **Then** the password is accepted.
4. **Given** a user registering with email `jane.doe@example.com`, **When** they submit a password containing only the local part `jane.doe` embedded in a larger string, **Then** the password is rejected — the local part is 8 characters (≥ 4) so the containment rule applies.
5. **Given** a user registering with a short email like `a@b.co` (local part `a` is 2 characters), **When** they submit a password of `CorrectHorseBattery9!`, **Then** the password is accepted because the local part is below the 4-character threshold and is skipped. The full email string `a@b.co` is still checked; it is not present in the password, so no rejection occurs.
6. **Given** a user registering with a national ID of `91` (2 digits, below threshold), **When** they submit a password of `Bicycle91TestingA!`, **Then** the password is accepted because the national ID is below the 4-character threshold and is skipped.
7. **Given** a user registering with national ID `9-123-4567` (10 characters), **When** they submit a password containing the substring `9-123-4567`, **Then** the password is rejected.

### Edge Cases

- **Timing / response-size disclosure on the success-shaped path**: the fresh-creation branch writes an account, hashes a password, and sends a verification email; the uniqueness-conflict branch does none of those. Wall-clock timing and server-side work can differ materially, leaving a degraded timing oracle. The feature's requirement is visible-output indistinguishability only (status, body, rendered view). Timing-channel mitigation is called out in Assumptions as best-effort; the `registration` rate-limit policy remains the defence-in-depth layer against iterative probing.
- **Client-side validation on the public form**: client-side validation must not reveal uniqueness state — it never does today because uniqueness is server-side, but this is explicitly preserved (no client-side unique-probe endpoints are added).
- **Admin path reaching the public endpoint**: if a user happens to hold an admin role but submits the public form (e.g., while logged in), they receive the public generic response. The specificity is a property of the enrollment endpoint, not the caller's role.
- **Batch user upload** (existing feature): batch upload already has its own authenticated-admin-only error-reporting UI and is out of scope for response-shape changes. The rule change in Story 3 still applies to each row's password validation; batch's error-reporting format is unchanged.
- **Password change / reset flows**: future password changes must apply the Story 3 rule. In this feature, the rule is added to the shared password validation surface so that any handler invoking it (registration today, password change / reset tomorrow) picks it up automatically. No new password-change flow is introduced here.
- **Case sensitivity**: the "contains email or national ID" check is case-insensitive for the email (emails are normalised) and exact-match for the national ID (IDs are stored as given).
- **Whitespace and punctuation in national IDs**: the containment check compares against both the verbatim stored form and the separator-stripped form (all non-alphanumeric characters removed), so a user whose ID is `9-123-4567` cannot bypass the rule by embedding `91234567` in a password. Each form is checked only when it is 4 characters or longer.

## Requirements *(mandatory)*

### Functional Requirements

**Public registration — generic responses (addresses platform-core FR-052):**

- **FR-016-01**: Public self-registration MUST return one indistinguishable generic in-form failure response for every validator-rule failure (missing field, malformed email, password strength rule, password identifying-data rule, and any other rule-level failure) with identical visible status, text, and rendered output.
- **FR-016-02**: The public registration failure response MUST NOT attach any field-level error attribution, positive or negative, that would let an observer infer which field or rule failed.
- **FR-016-03**: Public self-registration MUST render the same post-submission confirmation response ("Revise su correo electrónico para confirmar su cuenta") for both (a) a submission that created a new account and (b) a submission that passed all validator rules but conflicted with an existing account on email or national ID. The visible response — status code, headers that affect rendering, rendered view content, and response size class — MUST be byte-for-byte indistinguishable between those two cases.
- **FR-016-04**: On the uniqueness-conflict path, the system MUST NOT create an account, MUST NOT send a verification email, and MUST NOT send any other notification that would reveal the existence of the conflict to the submitter.
- **FR-016-05**: The public registration flow MUST continue to verify email and national-ID uniqueness before creating an account — it is the response shape and the no-op-on-conflict behaviour that change, not the underlying uniqueness checks.
- **FR-016-06**: Server-side logging for every public-registration outcome (success, uniqueness conflict on email, uniqueness conflict on national ID, rule-level failure) MUST record: (a) the submitted email address, (b) an outcome code identifying the true result (e.g., `Success`, `DuplicateEmail`, `DuplicateNationalId`, `ValidatorFailure:<rule>`), (c) a correlation ID that ties the log entry to the HTTP request, and (d) the client IP address. The submitted national ID MUST NOT be written to the log in cleartext — it is either omitted or redacted. None of the logged detail MUST be reflected in any user-facing response.

**Admin enrollment — specific feedback (addresses platform-core FR-053):**

- **FR-016-07**: The administrative enrollment endpoint MUST be a distinct code path from the public self-registration endpoint, with its own response-shaping behaviour (specific feedback on conflict, not the masked success-shaped response of FR-016-03).
- **FR-016-08**: The administrative enrollment endpoint MUST, on a uniqueness conflict, return a response that identifies the conflicting field (national ID vs email) to the authenticated admin.
- **FR-016-09**: The administrative enrollment endpoint MUST be reachable only by users authorised to enroll members (existing role guard on the admin controller stays in force); unauthenticated or unauthorised requests MUST NOT receive the specific-feedback response.
- **FR-016-10**: Business rules enforced by administrative enrollment — including the password identifying-data rule of FR-016-12 — MUST be identical to those enforced by public registration. Only the response shape differs; acceptance criteria for validator rules are the same.

**Password identifying-data rule (addresses platform-core FR-056 final clause):**

- **FR-016-11**: Password validation MUST reject any password whose normalised form contains the user's email address. The check runs against (a) the full email string (case-insensitive, always applied), and (b) the email local part (the portion before `@`, case-insensitive) — but (b) is applied only when the local part is **4 characters or longer**; a local part shorter than 4 characters is skipped to avoid pervasive false positives.
- **FR-016-12**: Password validation MUST reject any password that contains the user's national ID in either of two forms: (a) the value exactly as stored, or (b) the value with all non-alphanumeric characters removed (the "separator-stripped" form). Each form is checked only when it is 4 characters or longer; below that threshold the form is skipped to avoid false positives. Match on either form triggers rejection.
- **FR-016-13**: The rejection message for FR-016-11 and FR-016-12 MUST be a single user-facing message stating that the password cannot contain the email or national ID — it MUST NOT distinguish which of the two was matched (avoids telling the user "your national ID was matched" as a weak oracle).
- **FR-016-14**: The identifying-data password rule MUST apply to every entry point that sets a user password — public self-registration, administrative enrollment, and any future password-change / password-reset flow — by being implemented on the shared password validation surface rather than per-handler.

### Key Entities

*(No new persistent data is introduced. The changes are behavioural — error shaping in handlers and validator rule additions. The existing User account, credential, and related entities are unchanged.)*

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-016-01**: On the public registration endpoint, an automated probe that submits 50 distinct otherwise-valid submissions (mixing fresh, email-conflict, and national-ID-conflict combinations) finds **0** responses that differ in visible output (status, headers that affect rendering, rendered body text) — fresh-creation and uniqueness-conflict responses are indistinguishable.
- **SC-016-02**: On the public registration endpoint, an automated probe that submits 50 distinct rule-level failure submissions finds **0** responses that differ from each other in visible output, and those responses are visibly distinct from the SC-016-01 confirmation response (users whose submissions have fixable rule-level problems are not misled into thinking registration succeeded).
- **SC-016-03**: An authenticated admin user, when submitting an enrollment with a duplicate national ID, sees the conflict attributed to the national-ID field in **100%** of test runs; the same holds for the email field.
- **SC-016-04**: A password that contains the user's full email address is rejected in **100%** of attempts. A password that contains the user's email local part (when the local part is 4+ characters) is rejected in **100%** of attempts. A password that contains the user's national ID (when the national ID is 4+ characters) is rejected in **100%** of attempts. A password that contains only a below-threshold local part or below-threshold national ID, with no full-email match, is **not** rejected by this rule. The rejection message never identifies which of the three checks matched.
- **SC-016-05**: Legitimate users completing a fresh valid registration see **no regression** in flow completion time or UX compared to the current behaviour (they still receive a verification email and land on the confirmation page). Users who are already registered and attempt to register again ALSO land on the same confirmation page without the system confirming that fact to them — their recovery path is via password reset or by following the original verification email, which they can do without the site disclosing the account's existence.
  *Coverage: N/A — Performance-regression assertions are tracked in CI runtime budgets, not as a feature test.*

## Assumptions

- **Response-body indistinguishability is visible-only, best-effort at the transport layer**: acceptance inspects rendered HTML, HTTP status, and headers, not low-level TLS packet timing. Cryptographic constant-time comparisons are out of scope; the goal is to prevent the casual enumeration oracle, not to resist a dedicated timing-channel attack.
- **Public rate-limiting remains in force**: the `registration` rate-limit policy already on the public endpoint stays attached. Its presence is a second line of defence against enumeration even after response normalisation.
- **Admin role guard is sufficient authorisation for this feature**: the existing `[Authorize(Roles = "IncubatorAdmin,GlobalAdmin")]` attribute on the admin controller is considered sufficient to gate access to the specific-feedback endpoint. Finer-grained per-action permission layering is tracked separately (Phase C-α in the platform roadmap brainstorm) and is out of scope here.
- **Password rule scope is registration + admin enrollment today**: a password-change / password-reset flow does not exist in the current build; the rule is placed on the shared validation surface so it is picked up automatically when those flows land, but no new change / reset flow is created as part of this feature.
- **Email normalisation is already in place**: emails are trimmed and upper-cased before storage and comparison; the password-contains-email check uses the same normalisation.
- **National-ID normalisation for the password check**: the containment check runs against both the verbatim stored form and a separator-stripped form (all non-alphanumeric characters removed). The stored form in the database is unchanged — the stripping is only for the password comparison. This closes the most common bypass (typing the ID without separators) while keeping the stored canonical form stable.
- **Two message copies in Spanish**: (a) the generic in-form failure message for rule-level failures is a single Spanish sentence ("No fue posible completar el registro. Revise los datos e intente nuevamente."); (b) the masked success-shaped confirmation page (used for both fresh-creation and uniqueness-conflict) is the existing "Revise su correo electrónico para confirmar su cuenta" confirmation page plus a secondary line guiding users who believe they already have an account to try password recovery instead — so a user whose account already exists has a recovery path without the site confirming the account exists.
- **Batch upload is out of scope for response-shape changes**: the existing batch-upload CSV flow already reports per-row outcomes in its own UI; it is authenticated-admin-only and thus analogous to admin enrollment, so it is not an enumeration oracle and does not need response-shape changes. The password rule of FR-016-11 / FR-016-12 does apply to each row.

## Dependencies

- Depends on the existing public self-registration endpoint, the administrative enrollment endpoint, the registration command/handler code path, and the registration validator — all already present in the codebase. The feature modifies their behaviour and splits admin and public paths.
- Depends on the existing anti-forgery token and rate-limiting configuration for the public endpoint; no changes to that configuration are required.
- No database schema changes; no DACPAC publish required.
- Sequenced independently of the other Phase A-α items (ProjectsController authorization fix, `AsNoTracking` audit); those are tracked separately.

## Out of Scope

- `ProjectsController` authorization / multi-incubator correctness fix (tracked in brainstorm as R-SEC-2, separate spec).
- `AsNoTracking` compliance audit (tracked as R-SEC-3, separate spec).
- `CheckPermission` layering / per-action permission filters (tracked for Phase C).
- Timing-attack-resistant constant-time response generation.
- New password-change or password-reset flows (the rule surface is prepared; the flows are not built here).
- Telemetry or analytics for how often the enumeration-oracle paths were being probed historically.
