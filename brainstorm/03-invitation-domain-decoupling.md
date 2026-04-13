# Brainstorm: Invitation Domain Decoupling

**Date:** 2026-04-12
**Status:** spec-created
**Spec:** specs/010-invitation-domain-decoupling/

## Problem Framing

The previous work on spec 009 (unified user creation) introduced a DDD boundary violation: `SetInitialPasswordCommandHandler` in the Access domain synchronously calls `IInvitationTokenValidator`, which is implemented in Tenant infrastructure and queries Tenant's `ProjectInvitationRepository`. Moving the interface to `Mentoory.Shared.Application` didn't fix the coupling — it laundered it.

The deeper question: why does the Access domain need to validate invitations at all? Authentication (proving identity) is Access's concern. Invitation acceptance (enrolling in a project) is Tenant's concern. The original design conflated these by using the invitation ExternalId as an authentication token.

## Approaches Considered

### A: Unify on EmailVerificationToken (chosen)
- All onboarding emails carry an Access-domain EmailVerificationToken
- Invitation acceptance is a Tenant reaction to UserEmailVerifiedEvent (already exists)
- Remove TokenType.Invitation, IInvitationTokenValidator, TokenHash from ProjectInvitation
- Reissue triggers InvitationReissuedEvent → Access generates fresh token
- Pros: Simplest, uses existing infrastructure, stronger security (PBKDF2)
- Cons: Requires new integration event for reissue; email link format changes

### B: Read Model in Access
- Access maintains lightweight copy of active invitations synced via Tenant events
- SetInitialPasswordCommandHandler validates against its own read model
- Pros: No synchronous cross-domain calls
- Cons: More infrastructure (new events, new read model, eventual consistency), overkill for yes/no check

### C: Controller Orchestration
- Web layer validates invitation (Tenant query), shows password form, calls Access to set password
- Pros: Keeps domains pure
- Cons: Business logic in controller (violates project rules), security gap between validation and submission

## Decision

Approach A selected. It's the simplest fix that makes domains genuinely independent. The invitation token path was a shortcut that created coupling — the fix is to stop taking the shortcut and use the mechanism Access already owns (EmailVerificationToken).

## Open Threads
- Notification module must be updated to use EmailVerificationToken in invitation emails (out of scope for spec 010, but required for full integration)
