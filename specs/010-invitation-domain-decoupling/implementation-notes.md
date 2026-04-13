# Implementation Notes: Invitation Domain Decoupling

## Design Decisions

### Decision: Unify on EmailVerificationToken vs Read Model vs Controller Orchestration

Three approaches were evaluated:

**A. Unify on EmailVerificationToken (chosen):** All onboarding emails carry an Access-domain `EmailVerificationToken`. Invitation acceptance is a pure Tenant reaction to `UserEmailVerifiedEvent`. No cross-domain dependency.

**B. Read model in Access:** Access maintains a lightweight copy of active invitations synced via Tenant events (`InvitationCreatedEvent`, `InvitationExpiredEvent`). Access validates invitations against its own read model.

**C. Controller-level orchestration:** The web layer validates the invitation (Tenant query) before showing the password form, then calls Access to set the password without re-validating.

**Rationale for A:**
- Simplest — uses existing infrastructure (`EmailVerificationToken`, `UserEmailVerifiedEvent` flow)
- No new read models, no new sync events
- Stronger security — PBKDF2 hashed tokens vs Base64 encoded hashes
- `ProjectInvitation` aggregate becomes simpler (no crypto concerns)

**Rejected B because:** More infrastructure to maintain for a simple yes/no validation. Eventual consistency concerns for a real-time operation.

**Rejected C because:** Business logic leaks into controller (violates project rules). Security gap between validation and password submission — needs session state to bridge.

### Decision: InvitationReissuedEvent placement

Placed in `Mentoory.Tenant.Application/IntegrationEvents/` (originating domain) per Constitution Principle IV.

### Decision: Token expiry alignment on reissue

`InvitationReissuedEvent` carries `InvitationExpiryHours`. The Access handler uses this for the new token's expiry, ensuring the verification token doesn't expire before the invitation.

**Rationale:** Prevents the confusing scenario where a user clicks a link within the invitation validity window but the token has already expired. The admin's chosen expiry should apply holistically.

### Decision: Database column removal approach

Edit the SSDT table definition directly (remove `TokenHash` column). The DACPAC diff generates the migration automatically. No PostDeployment script needed — the data has no business value post-change.

## Key Insight

The original design tried to use the invitation's ExternalId as an authentication token (proof of identity). But authentication is Access's concern. The invitation should only care about "has this user been assigned to this project?" — the identity proof should come from Access's own token mechanism. Separating these concerns eliminates the coupling entirely.

## Risk Areas

- **Email link format change**: The invitation email must now carry a verification token instead of an invitation ExternalId. This requires coordination with the notification module (out of scope but noted).
- **Build breakage during refactoring**: Phase 2 (foundational) will break the build until Phase 3 completes. Plan commits at phase checkpoints.
