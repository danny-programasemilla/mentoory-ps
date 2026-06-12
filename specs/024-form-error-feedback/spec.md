# Feature Specification: Inline form-error feedback

**Feature Branch**: `024-form-error-feedback`

**Created**: 2026-06-12

**Status**: Draft

**Input**: Brainstorm `brainstorm/16-form-error-feedback.md` (approved). Prototype validated on `Platform/Incubators/Create`.

## User Scenarios & Testing *(mandatory)*

The waves are prioritized user journeys. Each is independently testable and shippable; Wave 1 establishes the shared foundation (global field styling + page-level-error toasts) and is a viable MVP on its own.

### User Story 1 - Clear inline errors on standard forms (Priority: P1)

A user fills in a standard create/edit form (e.g. a new incubator) and submits with mistakes. Instead of a duplicated list of errors at the top of the page plus a repeat under each field, every invalid field itself is clearly highlighted — a red border, a soft red glow, and an error icon inside the field on the right — with a single concise message beneath it. If the whole operation fails for a reason not tied to a field (e.g. "the incubator could not be created"), the user still sees that as a prominent toast notification rather than losing it.

**Why this priority**: This is the core problem (duplicated, plain, easy-to-miss errors) and covers the most-used forms. It also builds the centralized field-styling and toast mechanism that all later waves reuse, so it is the MVP.

**Independent Test**: Submit any Wave-1 form with empty/invalid fields and confirm each invalid field is visually emphasized with no duplicated top summary; force a server-side non-field failure and confirm it appears as a toast.

**Acceptance Scenarios**:

1. **Given** a standard form with a required field left empty, **When** the user submits, **Then** that field shows a danger border, danger glow, and an inside-right error icon, and a single message appears beneath it.
2. **Given** the same submission, **When** the page renders, **Then** there is no separate validation-summary block repeating the field errors.
3. **Given** a corrected field, **When** it becomes valid, **Then** its border, glow, icon, and message clear.
4. **Given** a submission that fails for a non-field reason (e.g. persistence error), **When** the view re-renders, **Then** the user sees a danger toast carrying that message.
5. **Given** several fields are invalid at once, **When** the user submits, **Then** each invalid field independently shows its own error state.

---

### User Story 2 - Consistent errors on authentication pages (Priority: P2)

A user on the login, forgot-password, reset-password, or change-password page enters invalid input or wrong credentials. Field-level problems are shown inline on the field (same treatment as Wave 1), and authentication-level failures such as "invalid credentials" appear as a toast, consistent with the rest of the app — even though these pages use a different layout.

**Why this priority**: Auth pages are high-traffic and security-relevant, but they use a separate layout that must first gain the toast surface, so they follow the foundation established in Wave 1.

**Independent Test**: On each auth page, trigger a field error and a credential/page-level error and confirm inline field styling and a toast respectively, with no top summary.

**Acceptance Scenarios**:

1. **Given** the login page, **When** the user submits wrong credentials, **Then** the failure is shown as a danger toast (not a top summary block).
2. **Given** any auth form with an invalid field, **When** submitted, **Then** the field shows the standard inline error treatment.

---

### User Story 3 - Errors on atypical forms (Priority: P3)

A user interacts with the batch-upload form (file input) or the participant diagnostic (a dynamically generated questionnaire). Validation errors are presented consistently with the app-wide pattern, with icon placement adapted where the standard inside-right position is not feasible (e.g. file inputs, repeated question blocks).

**Why this priority**: These forms have non-standard structure and are lower traffic, so they are handled last once the pattern is proven.

**Independent Test**: Trigger validation on the batch-upload and diagnostic forms and confirm errors follow the pattern with sensible icon placement and no duplication.

**Acceptance Scenarios**:

1. **Given** the batch-upload form with an invalid/missing file, **When** submitted, **Then** the field shows the error treatment adapted to a file input.
2. **Given** the diagnostic questionnaire with an unanswered required question, **When** submitted, **Then** the relevant question shows an inline error consistent with the pattern.

---

### Edge Cases

- A field becomes valid after correction → all error styling for it is removed.
- Both field-level and non-field errors occur together → fields show inline errors **and** a toast appears for the non-field error(s).
- A non-field error occurs with no field errors → no top summary appears; only the toast.
- Native `<select>` fields whose dropdown arrow would collide with the inside-right icon → icon placement adapted to avoid overlap.
- Client-side validation (before submit) and server-rendered validation (after submit) must both produce the same look.
- Screen-reader users must be informed of invalid fields, inline messages, and toasts.
- Long error messages must not break the field layout or hide the icon.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Invalid form fields MUST be visually emphasized with a danger-colored border, a persistent danger glow (retained while the field is focused, overriding the default focus ring), and an error icon positioned inside the field on the right (top-right for multi-line fields).
- **FR-002**: Each field's error message MUST appear once, directly associated with that field, and MUST NOT be duplicated elsewhere on the page.
- **FR-003**: The top page-level validation-summary block MUST be removed from every in-scope form.
- **FR-004**: Genuine page-level (non-field) errors MUST be surfaced to the user as a dismissible danger toast, so that no error information is lost when the summary is removed.
- **FR-005**: The field-error styling MUST be applied through a single centralized convention, so in-scope forms (and future forms) receive it without per-form styling code.
- **FR-006**: When an invalid field is corrected and becomes valid, its error styling (border, glow, icon, message) MUST clear.
- **FR-007**: All user-facing error text MUST be in Spanish.
- **FR-008**: Error states MUST be exposed to assistive technologies — invalid fields marked as invalid, and inline messages plus toasts announced.
- **FR-009**: The pattern MUST work for both client-side validation (before submit) and server-rendered validation (after submit), producing the same appearance.
- **FR-010**: Where a form has multiple invalid fields, each MUST independently show its own error state.
- **FR-011**: Authentication pages MUST display authentication-level failures (e.g. invalid credentials) as page-level toasts, which requires the toast surface to be available on the authentication layout.
- **FR-012**: Atypical forms (file upload, dynamic questionnaire) MUST present field errors consistent with the pattern, adapting icon placement where the standard inside-right position is not feasible.
- **FR-013**: The reference prototype on `Incubators/Create` MUST be migrated onto the centralized convention (its page-scoped styles removed in favor of the shared rule), so it is not a special case.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: On every in-scope form, any given validation error appears exactly once — never both in a top summary and under the field.
- **SC-002**: 100% of in-scope forms (14 total: 8 standard, 4 auth, 2 atypical) present invalid fields with the danger border + glow + inside-right icon treatment.
- **SC-003**: No page-level error that previously appeared only in a summary is lost — every such error is shown via toast (verified on forms that emit non-field errors, e.g. incubator create/edit and login).
- **SC-004**: A user can identify which specific field is in error from the field itself, without reading a separate summary.
- **SC-005**: Invalid fields, inline messages, and toasts are announced by screen readers.
- **SC-006**: The solution builds with zero warnings and all error copy is in Spanish.

## Assumptions

- The existing toast infrastructure (`showToast` in `wwwroot/js/site.js`, Bootstrap Toast + the shared Toast view component) is reused for page-level errors rather than introducing a new mechanism.
- jQuery-unobtrusive validation remains the validation mechanism, so the `input-validation-error` / `field-validation-error` classes emitted by ASP.NET tag helpers and the client validator are the styling hooks.
- The approved `Incubators/Create` prototype defines the target visual (danger border, persistent glow, inside-right alert-circle icon, restyled message).
- 14 forms are in scope, grouped into 3 waves (8 standard CRUD, 4 auth, 2 atypical).
- This is a Web-layer-only change — Razor views, CSS, a small shared JS/partial helper, and minimal controller error-routing where needed. No Domain, Application, or Infrastructure changes; no validation-rule changes.
- The authentication layout (`_AuthLayout`) does not yet include the toast surface and will need it added for Wave 2.
- Existing `mentoory.css` design tokens (`--tblr-danger`, `--tblr-danger-rgb`) provide the danger color.

## Out of Scope

- Redesigning the forms themselves (layout, fields, copy beyond error text).
- Changing or adding validation rules.
- Altering controllers beyond routing existing non-field `ModelState` errors to the toast surface.
- Success/confirmation toasts for non-error flows (already handled elsewhere).
