# Research: Invitation Domain Decoupling

**Date**: 2026-04-12
**Feature**: [spec.md](spec.md)

## Research Summary

No NEEDS CLARIFICATION items in the technical context. All design decisions are resolved based on codebase analysis and the approved spec. This document captures the research findings that informed the plan.

## Decision 1: How to handle the AcceptInvitation onboarding flow

**Context**: The current `OnboardingController` has an `AcceptInvitation` endpoint that accepts an invitation ExternalId as the token and calls `SetInitialPasswordCommand` with `TokenType.Invitation`. With the removal of `TokenType.Invitation`, this flow needs to change.

**Decision**: Unify the onboarding flow. The invitation email link should point to the same VerifyEmail endpoint using an `EmailVerificationToken`. The `AcceptInvitation` endpoint for password-setting becomes unnecessary.

**Rationale**:
- The VerifyEmail flow already handles password setting + `UserEmailVerifiedEvent` publishing
- `UserEmailVerifiedEventHandler` already auto-accepts non-`RequiresAcceptance` pending invitations
- Having two endpoints for the same operation (set password) creates confusion and coupling
- The separate `AcceptInvitationHandler` in Tenant.Application (for manual acceptance by already-verified users) remains untouched

**Alternatives considered**:
- Keep AcceptInvitation endpoint but redirect to VerifyEmail internally — adds indirection without benefit
- Keep both endpoints with shared logic — duplicates code

## Decision 2: Where to place `InvitationReissuedEvent`

**Context**: Integration events can be placed in either the originating domain's Application layer or in Shared.Application.

**Decision**: Place `InvitationReissuedEvent` in `Mentoory.Tenant.Application/IntegrationEvents/`.

**Rationale**:
- Constitution Principle IV mandates events in the originating domain's `Application/IntegrationEvents/`
- Existing events follow this pattern: `UserRegisteredEvent` in Access.Application, `ProjectCreatedEvent` in Tenant.Application
- The event originates from Tenant (ReissueInvitationHandler publishes it)

**Alternatives considered**:
- Place in Shared.Application — violates Principle IV, obscures ownership

## Decision 3: Token expiry alignment on reissue

**Context**: `EmailVerificationToken` defaults to 24h expiry. `ProjectInvitation` defaults to 7 days. When reissuing, should the new verification token match the invitation's expiry?

**Decision**: Pass `InvitationExpiryHours` via the `InvitationReissuedEvent`. The Access handler uses this value for the new token's expiry, aligning both lifetimes.

**Rationale**:
- Prevents the confusing scenario where a token expires but the invitation is still "active"
- The admin's chosen expiry duration should apply to the entire onboarding experience, not just the invitation record
- `User.GenerateEmailVerificationToken()` already accepts `expiryHours` parameter

**Alternatives considered**:
- Use default 24h — creates expiry mismatch, poor UX when invitation is 7+ days
- Use a fixed longer duration — ignores the admin's configuration choice

## Decision 4: Database migration approach for TokenHash removal

**Context**: `ProjectInvitations.TokenHash` column needs to be removed. SSDT/DACPAC manages the schema.

**Decision**: Edit the SSDT table definition (`Mentoory.Db/tenant/Tables/ProjectInvitations.sql`) to remove the `TokenHash` column. The DACPAC diff will generate the DROP COLUMN migration automatically.

**Rationale**:
- Constitution Principle XI mandates SSDT/DACPAC for schema management
- Existing data in `TokenHash` column has no business value once the feature ships
- No PostDeployment script needed — it's a clean column drop with no data to preserve

**Alternatives considered**:
- Keep column as nullable — accumulates dead schema, confuses future developers
- Defer column removal — leaves schema inconsistent with code, risks bugs

## Decision 5: `InvitationReissuedEventHandler` token generation approach

**Context**: The new handler in Access needs to generate a verification token for the user. How should it generate the raw token and its hash?

**Decision**: Use the same pattern as existing registration flow — generate random bytes with `RandomNumberGenerator`, hash with `IPasswordHasher`, store the hash via `User.GenerateEmailVerificationToken()`. The raw (unhashed) token is what goes in the email link.

**Rationale**:
- Consistent with how `EmailVerificationToken`s are currently generated during registration
- `IPasswordHasher` (PBKDF2) provides the same security level as existing tokens
- `User.GenerateEmailVerificationToken()` handles invalidating previous unused tokens automatically

**Alternatives considered**:
- Different hashing — inconsistent with existing tokens, no security benefit
