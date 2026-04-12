# Brainstorm: User Registration & Onboarding Improvements

**Date:** 2026-04-12
**Status:** spec-created
**Spec:** specs/009-unified-user-creation/

## Problem Framing

Admin user creation in Mentoory is fragmented across two individual forms (Enroll and RegisterInternal) and one batch upload. Project binding is inconsistent — batch requires a project dropdown while individual forms don't enforce it. Only one onboarding toggle exists (email verification) but a second is needed (invitation acceptance). The brainstorm prompt also included navigation/menu redesign, which was decomposed into a separate future spec.

## Approaches Considered

### A: Unified Command Architecture (Selected)
- Replace three admin commands with two (CreateUserCommand + BatchCreateUsersCommand), single code path for onboarding logic
- Pros: DRY, consistent behavior, easier to maintain
- Cons: Requires retiring 3 existing commands (migration effort)

### B: Extend Existing Commands
- Add missing toggle to existing commands, keep parallel paths
- Pros: Less churn
- Cons: Two command paths for same behavior, harder to keep in sync

### C: Event-Driven Enrollment Service
- Minimal commands + async onboarding pipeline via events
- Pros: Clean separation, pluggable
- Cons: Async complexity, harder to show immediate results to admin

## Decision

**Approach A selected.** Key design decisions:

- **Session context for project binding** — no project field on forms, consistent with platform mental model
- **Global toggles on batch** — apply uniformly to all CSV rows, two uploads if different settings needed
- **Adaptive password flow** — users set own password during onboarding steps; temp passwords only when both toggles ON
- **Two-dimensional state model** — AccountStatus (account-level) separate from ProjectInvitation.Status (per-project)
- **Simple invitation acceptance** — project name + accept button + password form; no decline option
- **7-day invitation token expiration** — admins can regenerate
- **Existing users auto-enrolled** — no confirmation step, clear messaging to admin
- **Navigation/menu redesign decomposed** to separate spec

## Open Threads

- Navigation/menu redesign (capability-based instead of role-based) — needs its own brainstorm and spec
- Should ProjectCoordinator be able to do individual creation (currently batch only)?
- Automatic reminder emails before invitation expiration — deferred as future enhancement
