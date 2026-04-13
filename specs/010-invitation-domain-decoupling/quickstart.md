# Quickstart: Invitation Domain Decoupling

**Feature**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md)

## What This Changes

This refactoring removes a cross-domain coupling between Access and Tenant modules. Instead of Access calling into Tenant to validate invitation tokens at password-set time, all onboarding uses Access-domain `EmailVerificationToken`s. Invitation acceptance happens reactively via integration events.

## Implementation Order

### Step 1: Domain layer changes (no dependencies)

1. Remove `TokenHash` from `ProjectInvitation.Create()` and the property itself
2. Update SSDT table definition to drop `TokenHash` column
3. Update EF configuration in `TenantDbContext` to remove `TokenHash` mapping

### Step 2: Delete cross-domain bridge

1. Delete `Mentoory.Shared.Application/Interfaces/IInvitationTokenValidator.cs`
2. Delete `Mentoory.Tenant.Infrastructure/Services/InvitationTokenValidator.cs`
3. Remove DI registration from `Tenant.Infrastructure.DependencyInjection`

### Step 3: Simplify Access command handler

1. Delete `TokenType.cs` enum
2. Remove `TokenType` from `SetInitialPasswordCommand`
3. Remove `TokenType.Invitation` branch from `SetInitialPasswordCommandHandler`
4. Remove `IInvitationTokenValidator` injection

### Step 4: Add integration event

1. Create `InvitationReissuedEvent` in `Tenant.Application/IntegrationEvents/`
2. Create `InvitationReissuedEventHandler` in `Access.Application/IntegrationEvents/`

### Step 5: Update Tenant handlers

1. Remove token generation from `CreateInvitationHandler`
2. Remove token generation from `ReissueInvitationHandler`, add event publishing

### Step 6: Update web layer

1. Update `OnboardingController` — remove AcceptInvitation password-setting flow or redirect to VerifyEmail
2. Update `AcceptInvitation.cshtml` if needed

## Verification

```bash
dotnet build          # Zero warnings
dotnet test           # All tests pass
```

Then manually verify:
- Create user via admin → user receives email → click link → set password → enrolled in project
- Let token expire → admin reissues → user receives new email → set password → enrolled
