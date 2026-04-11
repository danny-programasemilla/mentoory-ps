# Internal Contracts: Phase 1–3 Hardening

**Date**: 2026-04-03

This project is a modular monolith with no external API surface. All contracts are internal — between bounded contexts and between the Application and Web layers.

## Integration Events (Cross-Domain)

### UserRegisteredEvent

Published by: Access domain (RegisterUserHandler, RegisterInternalUserHandler, BatchRegisterUsersHandler)  
Consumed by: Tenant domain (UserRegisteredEventHandler)

| Field | Type | Description |
|-------|------|-------------|
| UserId | long | Internal user ID |
| UserExternalId | Guid | Public-facing user ID |
| Email | string | User's email address |
| ProjectExternalId | Guid? | Target project (null for public self-registration) |
| RequiresVerification | bool | Whether email verification is required |
| EnrollmentVariant | string | "FullFlow" or "Bypass" |
| InvitationExpiryHours | int | Token expiry for invitation (read from SystemConfiguration by publisher) |
| OccurredAtUtc | DateTime | When the event occurred |

### UserEmailVerifiedEvent (existing)

Published by: Access domain (VerifyEmailHandler, AdminVerifyEmailHandler)  
Consumed by: Tenant domain — to auto-progress pending invitations that were waiting on verification

| Field | Type | Description |
|-------|------|-------------|
| UserId | long | Internal user ID |
| OccurredAtUtc | DateTime | When verification occurred |

---

## Command/Query Contracts (Application Layer)

### New Commands

#### RegisterInternalUserCommand
| Field | Type | Constraints |
|-------|------|-------------|
| Country | string | Required, must match existing Country.Code |
| Identification | string | Required, validated against country rules |
| Email | string | Required, valid email format |
| Password | string | Required, meets OWASP complexity rules |
| RequireEmailVerification | bool | Required |
| ProjectExternalId | Guid | Required, must reference existing active project |

Returns: `Result<RegisterInternalUserResult>` with `UserExternalId` and enrollment status

#### BatchRegisterUsersCommand
| Field | Type | Constraints |
|-------|------|-------------|
| CsvStream | Stream | Required, UTF-8 CSV with header row |
| ProjectExternalId | Guid | Required |
| IncubatorExternalId | Guid | Required |

Returns: `Result<BatchRegistrationResult>` with `List<BatchRowResult>`

#### BatchRowResult
| Field | Type | Description |
|-------|------|-------------|
| RowNumber | int | 1-based row index |
| Country | string | Country from CSV |
| Identification | string | Identification from CSV |
| Email | string | Email from CSV |
| UserAlreadyExisted | bool | Whether user was found by country+identification |
| UserCreated | bool | Whether a new user was created |
| AlreadyInProject | bool | Whether user was already a project participant |
| InvitationCreated | bool | Whether a new invitation was generated |
| EnrolledDirectly | bool | Whether user was auto-enrolled (bypass variant) |
| TemporaryPassword | string? | Generated temporary password (null if user already existed) |
| Status | string | "Success", "Skipped", "Error" |
| ErrorMessage | string? | Error details if Status = "Error" |
| Warnings | List\<string\> | Warnings (e.g., email mismatch) |

#### AdminVerifyEmailCommand
| Field | Type | Constraints |
|-------|------|-------------|
| UserExternalId | Guid | Required, must reference existing user |

#### RegenerateVerificationTokenCommand
| Field | Type | Constraints |
|-------|------|-------------|
| UserExternalId | Guid | Required, must reference existing user |

#### AdminResetPasswordCommand
| Field | Type | Constraints |
|-------|------|-------------|
| UserExternalId | Guid | Required, must reference existing user |

Returns: `Result<AdminResetPasswordResult>` with generated `TemporaryPassword` (shown once in response, never persisted in plaintext)

#### CreateInvitationCommand
| Field | Type | Constraints |
|-------|------|-------------|
| UserExternalId | Guid | Required |
| ProjectExternalId | Guid | Required |
| ExpiryHours | int | Required, read from SystemConfiguration by the caller |

#### AcceptInvitationCommand
| Field | Type | Constraints |
|-------|------|-------------|
| InvitationExternalId | Guid | Required |

#### AdminAcceptInvitationCommand
| Field | Type | Constraints |
|-------|------|-------------|
| InvitationExternalId | Guid | Required |

#### ReissueInvitationCommand
| Field | Type | Constraints |
|-------|------|-------------|
| InvitationExternalId | Guid | Required |
| ExpiryHours | int | Required, read from SystemConfiguration by the caller |

#### RequestSelfEnrollmentCommand
| Field | Type | Constraints |
|-------|------|-------------|
| ProjectExternalId | Guid | Required |
| UserExternalId | Guid | Required (from auth context) |

#### UpdateConfigurationCommand
| Field | Type | Constraints |
|-------|------|-------------|
| Key | string | Required, must match existing ConfigurationKey |
| Value | string | Required, must be parseable to declared DataType |

#### ForcedPasswordChangeCommand
| Field | Type | Constraints |
|-------|------|-------------|
| CurrentPassword | string | Required |
| NewPassword | string | Required, meets OWASP complexity rules |

### New Queries

#### ListUsersByStatusQuery
| Field | Type | Constraints |
|-------|------|-------------|
| AccountStatus | string? | Optional filter |
| SearchTerm | string? | Optional search on email/name/identification |

Returns: paged list of user summaries

#### GetUserDetailsQuery
| Field | Type | Constraints |
|-------|------|-------------|
| UserExternalId | Guid | Required |

Returns: Access-domain user state only — account status, email verification state, active credentials count, email verified date. Does NOT cross into Tenant domain.

#### GetUserProjectAssociationsQuery
| Field | Type | Constraints |
|-------|------|-------------|
| UserId | long | Required (internal ID, passed from controller after resolving ExternalId via GetUserDetails) |

Returns: list of invitations (with status) and active project participations for the user. Lives in Tenant.Application. Controller composes both queries.

#### ListPublicProjectsQuery
| Field | Type | Constraints |
|-------|------|-------------|
| (none) | — | Returns all public projects in Registration stage |

Returns: list of project summaries (name, description, incubator name)

#### GetConfigurationQuery
| Field | Type | Constraints |
|-------|------|-------------|
| Key | string | Required |

Returns: configuration value as string

#### ListPendingInvitationsQuery
| Field | Type | Constraints |
|-------|------|-------------|
| ProjectExternalId | Guid? | Optional filter by project |
| UserExternalId | Guid? | Optional filter by user |

Returns: list of pending/expired invitations

---

## Modified Commands

### RegisterUserCommand (existing)
**Changes**:
- Handler must call `GenerateEmailVerificationToken(utcNow, expiryHours)` after creating the user
- Handler must split uniqueness check into two sequential queries returning specific errors
- `expiryHours` read from SystemConfiguration

### LoginUserCommand (existing)
**Changes**:
- Handler must check for `PasswordResetRequired` status and include it in the result so the controller can redirect
- `maxAttempts`, `lockoutDuration`, `sessionTimeout` read from SystemConfiguration

### ChangePasswordCommand (existing)
**Changes**:
- `passwordHistoryDepth` read from SystemConfiguration instead of hardcoded constant
