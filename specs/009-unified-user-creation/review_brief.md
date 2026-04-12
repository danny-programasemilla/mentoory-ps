# Review Brief: Unified User Creation with Configurable Onboarding

**Spec:** specs/009-unified-user-creation/spec.md
**Generated:** 2026-04-12

> Reviewer's guide to scope and key decisions. See full spec for details.

---

## Feature Overview

Admin users (GlobalAdmin, IncubatorAdmin, ProjectCoordinator) currently create users through two fragmented individual forms and one batch upload flow, with inconsistent project binding and limited onboarding control. This spec unifies admin user creation into a single individual form and an improved batch flow, both driven by session context for project binding, with two configurable toggles controlling email verification and project invitation acceptance. The result is a predictable 4-case onboarding matrix that covers everything from full user self-onboarding to instant admin-provisioned accounts.

## Scope Boundaries

- **In scope:** Unified individual creation form, batch upload with onboarding toggles, session-context project enforcement, invitation flow (token, email, acceptance page), existing-user auto-enrollment, onboarding status column on users list
- **Out of scope:** Public self-registration (unchanged), navigation/menu redesign (separate spec), role assignment during creation, email template visual design, bulk re-invitation, invitation analytics, automatic reminder emails
- **Why these boundaries:** The feature focuses strictly on the admin user creation experience. Navigation redesign affects the entire platform and is decomposed into its own spec. Role assignment is a separate workflow that happens after enrollment.

## Critical Decisions

### Two-Dimensional State Model
- **Choice:** AccountStatus (account-level: PendingVerification, Active, etc.) remains unchanged. Invitation state lives on a new `ProjectInvitation` child entity (per user-project pair: Pending, Accepted, Expired). The onboarding status displayed to admins is a computed value from both dimensions.
- **Trade-off:** Requires a new entity and computed status logic, but correctly models reality — a user can be Active on their account while having pending invitations for multiple projects.
- **Feedback:** Does this two-dimensional model feel right, or should invitation state be simpler?

### Session Context Drives Project Binding
- **Choice:** Both individual and batch creation derive the target project from the top-bar context selector (spec 008). No project field on either form.
- **Trade-off:** Consistent mental model across the platform, but admins must switch context before creating users for a different project.
- **Feedback:** Is the extra context-switching acceptable?

### Adaptive Password Flow
- **Choice:** Users set their own password during verification or invitation acceptance. Temporary passwords are only generated when both toggles are ON (fully admin-provisioned).
- **Trade-off:** Better UX for users in onboarding flows, but temporary passwords still needed for the admin-provisioned path.
- **Feedback:** Any concern about the dual password paths?

## Areas of Potential Disagreement

> Decisions or approaches where reasonable reviewers might push back.

### Existing User Auto-Enrollment Without Confirmation
- **Decision:** When creating a user who already exists, the system auto-enrolls them in the current project without asking the admin to confirm.
- **Why this might be controversial:** An admin might accidentally enroll someone in the wrong project. A confirmation dialog would prevent this.
- **Alternative view:** Add a confirmation step for individual creation (batch keeps auto-enroll for throughput).
- **Seeking input on:** Is the risk of accidental enrollment low enough to skip confirmation?

### ProjectCoordinator Cannot Do Individual Creation
- **Decision:** ProjectCoordinator can only do batch upload, not individual creation (matches current behavior).
- **Why this might be controversial:** Coordinators manage project participants daily — individual creation seems like a natural need.
- **Alternative view:** Extend individual creation to ProjectCoordinator as well.
- **Seeking input on:** Should this restriction be relaxed?

### No Decline Option on Invitation
- **Decision:** Invitation acceptance page has only "Accept" — no "Decline" button. Users who don't want to join simply don't click.
- **Why this might be controversial:** Explicit decline gives the admin signal that the user actively rejected, vs. unknown (didn't see email? ignoring?).
- **Alternative view:** Add a "Decline" button that notifies the admin.
- **Seeking input on:** Is the "ignore = decline" model sufficient?

## Naming Decisions

| Item | Name | Context |
|------|------|---------|
| Toggle 1 | "Omitir verificacion de correo" | Skip email verification |
| Toggle 2 | "Omitir aceptacion de invitacion" | Skip invitation acceptance |
| New entity | ProjectInvitation | Child of User aggregate, per-project |
| Status values | "Pendiente verificacion", "Pendiente invitacion", "Pendiente ambos", "Activo" | Computed onboarding status for users list |
| Batch row statuses | "Creado", "Inscrito", "Error" | New user, existing user enrolled, failure |

## Open Questions

- [ ] Should invitation emails be re-sent automatically before expiration (e.g., reminder at day 5)? Deferred to future enhancement.

## Risk Areas

| Risk | Impact | Mitigation |
|------|--------|------------|
| Spec 008 not implemented yet | High | Hard dependency — spec 008 must ship first |
| Email delivery failures during batch | Med | Background processing + admin regenerate tokens |
| UserRegisteredEvent contract breakage | Med | Backward-compatible extension or versioning |
| Stale invitation tokens accumulating | Low | 7-day expiration + admin regeneration UI |

---
*Share with reviewers before implementation.*
