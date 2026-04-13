# Review Brief: Invitation Domain Decoupling

**Spec:** specs/010-invitation-domain-decoupling/spec.md
**Generated:** 2026-04-12

> Reviewer's guide to scope and key decisions. See full spec for details.

---

## Feature Overview

This is an architectural refactoring that removes a cross-domain coupling between the Access and Tenant modules. The current `SetInitialPasswordCommandHandler` in Access synchronously calls into Tenant's invitation repository via `IInvitationTokenValidator` — an interface placed in Shared to hide the dependency, but the runtime coupling remains. The fix unifies all onboarding authentication on Access-domain `EmailVerificationToken`s and makes invitation acceptance a pure Tenant-domain reaction to events. A new `InvitationReissuedEvent` enables token regeneration during invitation reissue without cross-domain calls.

## Scope Boundaries

- **In scope:** Remove `IInvitationTokenValidator`, `TokenType.Invitation`, `TokenHash` from `ProjectInvitation`; simplify `SetInitialPasswordCommandHandler`; add `InvitationReissuedEvent` and handler; update controller/views
- **Out of scope:** Notification module / email template changes; `RequiresAcceptance=true` manual acceptance flow; self-registration flow; database migration for dropping column (optional inclusion)
- **Why these boundaries:** The core coupling is in the handler + interface. Notification module is a separate concern that must be coordinated but is not part of the domain boundary fix.

## Critical Decisions

### Unify on EmailVerificationToken instead of adding a read model
- **Choice:** All onboarding uses Access-domain EmailVerificationToken exclusively
- **Trade-off:** Simpler implementation vs. requiring notification module to change email link format
- **Feedback:** Is the assumption that registration already generates an EmailVerificationToken valid for all admin-created user flows?

### Token expiry alignment on reissue
- **Choice:** `InvitationReissuedEvent` carries `InvitationExpiryHours`, Access aligns token expiry to invitation expiry
- **Trade-off:** Consistent UX (no expiry mismatch) vs. longer-lived tokens than the default 24h
- **Feedback:** Is aligning token expiry to invitation expiry (potentially 7 days) acceptable from a security perspective?

## Areas of Potential Disagreement

### Removing TokenHash from ProjectInvitation entirely
- **Decision:** Drop the column and property — Access owns all token validation
- **Why this might be controversial:** Some may argue ProjectInvitation should retain its own token for future use cases (e.g., direct invitation link without email verification)
- **Alternative view:** Keep TokenHash as optional/nullable for future extensibility
- **Seeking input on:** Is there a foreseeable use case where the invitation itself needs its own authentication token, separate from email verification?

### Eliminating the AcceptInvitation password-setting endpoint
- **Decision:** Invitation onboarding routes to the VerifyEmail flow; AcceptInvitation is for manual acceptance only
- **Why this might be controversial:** This changes the email link format, which may affect existing pending invitations in production
- **Alternative view:** Keep a thin redirect endpoint for backward compatibility
- **Seeking input on:** Are there pending invitations in production with the old link format that need a migration/redirect path?

## Naming Decisions

| Item | Name | Context |
|------|------|---------|
| Integration event | `InvitationReissuedEvent` | Published by Tenant when invitation is reissued |
| Event handler | `InvitationReissuedEventHandler` | Access handler that generates fresh verification token |

## Open Questions

- [ ] Are there pending invitations in production using the old link format (invitation ExternalId)?
- [ ] Does the notification module currently send invitation emails with a dedicated template, or does it reuse the verification email template?

## Risk Areas

| Risk | Impact | Mitigation |
|------|--------|------------|
| Email link format change breaks existing pending invitations | Medium | Check production data; add redirect endpoint if needed |
| Build breakage during refactoring (Phase 2 → Phase 3 gap) | Low | Commit at phase checkpoints; plan single-session implementation |
| Notification module not updated to use new token format | Medium | Coordinate with notification work; document dependency in Assumptions |

---
*Share with reviewers before implementation.*
