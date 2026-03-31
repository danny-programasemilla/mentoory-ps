# Data Model: Mentoory Enterprise SaaS Platform

**Branch**: `001-mentory-platform-core` | **Date**: 2026-03-31 | **Phase**: 1

---

## Schema: `[identity]`

### Users

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| Id | BIGINT IDENTITY | PK | Internal only |
| ExternalId | UNIQUEIDENTIFIER | NOT NULL, UNIQUE, DEFAULT NEWID() | Used in routes/APIs |
| Email | NVARCHAR(256) | NOT NULL, UNIQUE | Globally unique, login identifier |
| NormalizedEmail | NVARCHAR(256) | NOT NULL, UNIQUE | Uppercase for case-insensitive lookups |
| Country | NVARCHAR(100) | NOT NULL | Part of compound unique with NationalId |
| NationalId | NVARCHAR(50) | NOT NULL | Unique within country |
| FirstName | NVARCHAR(100) | NOT NULL | |
| LastName | NVARCHAR(100) | NOT NULL | |
| AccountStatus | TINYINT | NOT NULL, DEFAULT 0 | 0=PendingVerification, 1=Active, 2=Locked, 3=Disabled, 4=PasswordResetRequired |
| FailedLoginAttempts | INT | NOT NULL, DEFAULT 0 | Reset on successful login |
| LockoutEndUtc | DATETIME2 | NULL | When current lockout expires |
| EmailVerifiedAtUtc | DATETIME2 | NULL | Null = not verified |
| CreatedAtUtc | DATETIME2 | NOT NULL | |
| UpdatedAtUtc | DATETIME2 | NOT NULL | |

**Indexes**:
- `UQ_Users_Country_NationalId` — UNIQUE(Country, NationalId)
- `IX_Users_NormalizedEmail` — UNIQUE(NormalizedEmail) (also serves as lookup index)
- `IX_Users_ExternalId` — UNIQUE(ExternalId)

### Credentials

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| Id | BIGINT IDENTITY | PK | |
| UserId | BIGINT | NOT NULL, FK → Users(Id) | |
| PasswordHash | NVARCHAR(500) | NOT NULL | Format: `pbkdf2-sha512$600000${salt}${hash}` |
| IsActive | BIT | NOT NULL, DEFAULT 1 | Only one active per user; old kept for history |
| CreatedAtUtc | DATETIME2 | NOT NULL | |

**Indexes**:
- `IX_Credentials_UserId_IsActive` — (UserId, IsActive) INCLUDE (PasswordHash) WHERE IsActive = 1

### AuthSessions

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| Id | BIGINT IDENTITY | PK | |
| SessionToken | NVARCHAR(100) | NOT NULL, UNIQUE | 256-bit URL-safe Base64 |
| UserId | BIGINT | NOT NULL, FK → Users(Id) | |
| IpAddress | NVARCHAR(45) | NOT NULL | IPv4 or IPv6 |
| UserAgent | NVARCHAR(500) | NULL | |
| CreatedAtUtc | DATETIME2 | NOT NULL | |
| LastActivityUtc | DATETIME2 | NOT NULL | Updated on each request |
| ExpiresAtUtc | DATETIME2 | NOT NULL | Absolute session lifetime |
| IsActive | BIT | NOT NULL, DEFAULT 1 | |
| ActiveIncubatorId | BIGINT | NULL | Set after context selection |
| ActiveProjectId | BIGINT | NULL | Set after context selection |
| ActiveRole | NVARCHAR(50) | NULL | Set after context selection |

**Indexes**:
- `IX_AuthSessions_SessionToken` — UNIQUE(SessionToken) WHERE IsActive = 1
- `IX_AuthSessions_UserId_IsActive` — (UserId, IsActive) for single-session enforcement

### EmailVerificationTokens

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| Id | BIGINT IDENTITY | PK | |
| UserId | BIGINT | NOT NULL, FK → Users(Id) | |
| TokenHash | NVARCHAR(128) | NOT NULL | SHA-256 hash of actual token |
| ExpiresAtUtc | DATETIME2 | NOT NULL | 24 hours from creation |
| IsUsed | BIT | NOT NULL, DEFAULT 0 | |
| CreatedAtUtc | DATETIME2 | NOT NULL | |

### PasswordResetTokens

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| Id | BIGINT IDENTITY | PK | |
| UserId | BIGINT | NOT NULL, FK → Users(Id) | |
| TokenHash | NVARCHAR(128) | NOT NULL | SHA-256 hash of actual token |
| ExpiresAtUtc | DATETIME2 | NOT NULL | 1 hour from creation |
| IsUsed | BIT | NOT NULL, DEFAULT 0 | |
| CreatedAtUtc | DATETIME2 | NOT NULL | |

---

## Schema: `[authorization]`

### RoleAssignments

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| Id | BIGINT IDENTITY | PK | |
| ExternalId | UNIQUEIDENTIFIER | NOT NULL, UNIQUE | |
| UserId | BIGINT | NOT NULL | Ref by ID (cross-schema) |
| IncubatorId | BIGINT | NOT NULL | Ref by ID |
| ProjectId | BIGINT | NULL | NULL for incubator-level roles (IncubatorAdmin) |
| Role | NVARCHAR(50) | NOT NULL | PlatformRole name |
| IsActive | BIT | NOT NULL, DEFAULT 1 | Soft revocation |
| CreatedAtUtc | DATETIME2 | NOT NULL | |
| UpdatedAtUtc | DATETIME2 | NOT NULL | |

**Indexes**:
- `IX_RoleAssignments_UserId_IsActive` — (UserId, IsActive) INCLUDE (IncubatorId, ProjectId, Role) — context selection query
- `IX_RoleAssignments_IncubatorId_ProjectId_Role` — (IncubatorId, ProjectId, Role) — permission check
- `UQ_RoleAssignments_Unique` — UNIQUE(UserId, IncubatorId, ProjectId, Role) WHERE IsActive = 1

**Validation rules**:
- Entrepreneur: Max 1 active assignment per incubator (enforced in domain)
- GlobalAdmin: ProjectId must be NULL, IncubatorId set to a sentinel or NULL

---

## Schema: `[tenant]`

### Incubators

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| Id | BIGINT IDENTITY | PK | |
| ExternalId | UNIQUEIDENTIFIER | NOT NULL, UNIQUE | |
| Name | NVARCHAR(200) | NOT NULL | |
| Description | NVARCHAR(1000) | NULL | |
| SubscriptionPlanId | BIGINT | NULL | FK → [subscription].SubscriptionPlans(Id) |
| IsActive | BIT | NOT NULL, DEFAULT 1 | |
| CreatedAtUtc | DATETIME2 | NOT NULL | |
| UpdatedAtUtc | DATETIME2 | NOT NULL | |

### Projects

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| Id | BIGINT IDENTITY | PK | |
| ExternalId | UNIQUEIDENTIFIER | NOT NULL, UNIQUE | |
| IncubatorId | BIGINT | NOT NULL, FK → Incubators(Id) | Tenant scoping |
| Name | NVARCHAR(200) | NOT NULL | |
| Description | NVARCHAR(1000) | NULL | |
| CurrentStageType | TINYINT | NOT NULL, DEFAULT 0 | StageType enum |
| CurrentStageState | TINYINT | NOT NULL, DEFAULT 0 | StageState enum |
| IsActive | BIT | NOT NULL, DEFAULT 1 | |
| CreatedAtUtc | DATETIME2 | NOT NULL | |
| UpdatedAtUtc | DATETIME2 | NOT NULL | |

**Indexes**:
- `IX_Projects_IncubatorId` — (IncubatorId) — tenant-scoped queries
- `IX_Projects_ExternalId` — UNIQUE(ExternalId)

### ProjectStages

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| Id | BIGINT IDENTITY | PK | |
| ProjectId | BIGINT | NOT NULL, FK → Projects(Id) | |
| StageType | TINYINT | NOT NULL | 0=Registration...6=Closure |
| State | TINYINT | NOT NULL, DEFAULT 0 | 0=NotStarted, 1=InProgress, 2=Completed |
| StartedAtUtc | DATETIME2 | NULL | |
| CompletedAtUtc | DATETIME2 | NULL | |
| AdvancedByUserId | BIGINT | NULL | Who triggered the transition |

**Indexes**:
- `IX_ProjectStages_ProjectId_StageType` — UNIQUE(ProjectId, StageType)

### ProjectParticipants

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| Id | BIGINT IDENTITY | PK | |
| ExternalId | UNIQUEIDENTIFIER | NOT NULL, UNIQUE | |
| ProjectId | BIGINT | NOT NULL, FK → Projects(Id) | |
| UserId | BIGINT | NOT NULL | Ref by ID |
| Role | NVARCHAR(50) | NOT NULL | |
| IsActive | BIT | NOT NULL, DEFAULT 1 | |
| EnrolledAtUtc | DATETIME2 | NOT NULL | |

**Indexes**:
- `IX_ProjectParticipants_ProjectId_Role` — (ProjectId, Role)
- `UQ_ProjectParticipants_Entrepreneur` — UNIQUE(ProjectId, UserId) WHERE Role = 'Entrepreneur' AND IsActive = 1

### MentorAssignments

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| Id | BIGINT IDENTITY | PK | |
| ExternalId | UNIQUEIDENTIFIER | NOT NULL, UNIQUE | |
| ProjectId | BIGINT | NOT NULL, FK → Projects(Id) | |
| MentorUserId | BIGINT | NOT NULL | |
| EntrepreneurUserId | BIGINT | NOT NULL | |
| IsLeadMentor | BIT | NOT NULL, DEFAULT 0 | Visual/coordination only (FR-058/059) |
| IsActive | BIT | NOT NULL, DEFAULT 1 | |
| AssignedAtUtc | DATETIME2 | NOT NULL | |

**Indexes**:
- `IX_MentorAssignments_ProjectId_EntrepreneurUserId` — mentor assignment lookups
- `UQ_MentorAssignments_LeadMentor` — Filtered unique for one lead per entrepreneur per project

---

## Schema: `[diagnostic]`

### FormTemplates

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| Id | BIGINT IDENTITY | PK | |
| ExternalId | UNIQUEIDENTIFIER | NOT NULL, UNIQUE | |
| Name | NVARCHAR(200) | NOT NULL | |
| Description | NVARCHAR(1000) | NULL | |
| SubscriptionTier | NVARCHAR(50) | NULL | Which subscription tiers can access |
| Version | INT | NOT NULL, DEFAULT 1 | Incremented on update |
| IsActive | BIT | NOT NULL, DEFAULT 1 | |
| CreatedAtUtc | DATETIME2 | NOT NULL | |

### ProjectForms

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| Id | BIGINT IDENTITY | PK | |
| ExternalId | UNIQUEIDENTIFIER | NOT NULL, UNIQUE | |
| ProjectId | BIGINT | NOT NULL | Tenant-scoped via project |
| IncubatorId | BIGINT | NOT NULL | Tenant scoping |
| SourceTemplateId | BIGINT | NULL, FK → FormTemplates(Id) | NULL if disconnected |
| SourceTemplateVersion | INT | NULL | Version at clone time |
| Name | NVARCHAR(200) | NOT NULL | |
| SyncMode | TINYINT | NOT NULL, DEFAULT 0 | 0=Disconnected, 1=PartialSync |
| CreatedAtUtc | DATETIME2 | NOT NULL | |

### Questions

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| Id | BIGINT IDENTITY | PK | |
| ExternalId | UNIQUEIDENTIFIER | NOT NULL, UNIQUE | |
| ProjectFormId | BIGINT | NOT NULL, FK → ProjectForms(Id) | |
| TopicId | BIGINT | NOT NULL | Ref to [knowledge].Topics(Id) |
| QuestionText | NVARCHAR(2000) | NOT NULL | |
| QuestionType | TINYINT | NOT NULL | 0=Text, 1=Numeric, 2=SingleSelect, 3=MultiSelect |
| StageApplicability | TINYINT | NOT NULL | 0=Initial, 1=Final, 2=Both |
| SortOrder | INT | NOT NULL | |
| BlockGroup | NVARCHAR(100) | NULL | Optional grouping |
| IsOptional | BIT | NOT NULL, DEFAULT 0 | |

### AnswerOptions

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| Id | BIGINT IDENTITY | PK | |
| QuestionId | BIGINT | NOT NULL, FK → Questions(Id) | |
| OptionText | NVARCHAR(500) | NOT NULL | |
| Score | DECIMAL(10,2) | NOT NULL | Numerical contribution |
| SwotClassification | TINYINT | NOT NULL | 0=Strength, 1=Weakness, 2=Opportunity, 3=Threat |
| OdsrOrientation | TINYINT | NOT NULL | 0=Offensive, 1=Defensive, 2=Survival, 3=Reorientation |
| SortOrder | INT | NOT NULL | |

### FollowUpQuestions

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| Id | BIGINT IDENTITY | PK | |
| QuestionId | BIGINT | NOT NULL, FK → Questions(Id) | Parent question |
| QuestionText | NVARCHAR(2000) | NOT NULL | |
| SortOrder | INT | NOT NULL | |

### DiagnosticResponses

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| Id | BIGINT IDENTITY | PK | |
| ExternalId | UNIQUEIDENTIFIER | NOT NULL, UNIQUE | |
| ProjectFormId | BIGINT | NOT NULL, FK → ProjectForms(Id) | |
| ProjectId | BIGINT | NOT NULL | |
| IncubatorId | BIGINT | NOT NULL | Tenant scoping |
| EntrepreneurUserId | BIGINT | NOT NULL | |
| EvaluationStage | TINYINT | NOT NULL | 0=Initial, 1=Final |
| IsCompleted | BIT | NOT NULL, DEFAULT 0 | |
| CompletedAtUtc | DATETIME2 | NULL | |
| CreatedAtUtc | DATETIME2 | NOT NULL | |

**Indexes**:
- `UQ_DiagnosticResponses_Unique` — UNIQUE(ProjectFormId, EntrepreneurUserId, EvaluationStage)

### QuestionResponses

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| Id | BIGINT IDENTITY | PK | |
| DiagnosticResponseId | BIGINT | NOT NULL, FK → DiagnosticResponses(Id) | |
| QuestionId | BIGINT | NOT NULL, FK → Questions(Id) | |
| TextValue | NVARCHAR(4000) | NULL | For text/numeric types |
| NumericValue | DECIMAL(10,2) | NULL | For numeric type |
| CreatedAtUtc | DATETIME2 | NOT NULL | |

### QuestionResponseOptions (junction for selected answer options)

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| QuestionResponseId | BIGINT | NOT NULL, FK | |
| AnswerOptionId | BIGINT | NOT NULL, FK | |
| PK: (QuestionResponseId, AnswerOptionId) | | |

### AnswerCorrections

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| Id | BIGINT IDENTITY | PK | |
| QuestionResponseId | BIGINT | NOT NULL, FK → QuestionResponses(Id) | |
| PreviousTextValue | NVARCHAR(4000) | NULL | |
| PreviousNumericValue | DECIMAL(10,2) | NULL | |
| PreviousSelectedOptionIds | NVARCHAR(500) | NULL | Comma-separated old selections |
| CorrectedByUserId | BIGINT | NOT NULL | |
| CorrectedAtUtc | DATETIME2 | NOT NULL | |
| Reason | NVARCHAR(500) | NULL | |

---

## Schema: `[knowledge]`

### KnowledgeStructures

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| Id | BIGINT IDENTITY | PK | |
| ExternalId | UNIQUEIDENTIFIER | NOT NULL, UNIQUE | |
| ProjectId | BIGINT | NULL | NULL for global templates |
| IncubatorId | BIGINT | NULL | Tenant scoping (NULL for templates) |
| SourceTemplateId | BIGINT | NULL | FK to self for clones |
| Name | NVARCHAR(200) | NOT NULL | |
| IsTemplate | BIT | NOT NULL | |
| CreatedAtUtc | DATETIME2 | NOT NULL | |

### Modules

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| Id | BIGINT IDENTITY | PK | |
| ExternalId | UNIQUEIDENTIFIER | NOT NULL, UNIQUE | |
| KnowledgeStructureId | BIGINT | NOT NULL, FK | |
| Name | NVARCHAR(200) | NOT NULL | |
| Description | NVARCHAR(1000) | NULL | |
| SortOrder | INT | NOT NULL | |

### Topics

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| Id | BIGINT IDENTITY | PK | |
| ExternalId | UNIQUEIDENTIFIER | NOT NULL, UNIQUE | |
| ModuleId | BIGINT | NOT NULL, FK → Modules(Id) | |
| Name | NVARCHAR(200) | NOT NULL | |
| Description | NVARCHAR(1000) | NULL | |
| SortOrder | INT | NOT NULL | |
| HighPriorityMinScore | DECIMAL(10,2) | NULL | Score range config for priority mapping |
| HighPriorityMaxScore | DECIMAL(10,2) | NULL | |
| MediumPriorityMinScore | DECIMAL(10,2) | NULL | |
| MediumPriorityMaxScore | DECIMAL(10,2) | NULL | |
| LowPriorityMinScore | DECIMAL(10,2) | NULL | |
| LowPriorityMaxScore | DECIMAL(10,2) | NULL | |

### Subjects

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| Id | BIGINT IDENTITY | PK | |
| ExternalId | UNIQUEIDENTIFIER | NOT NULL, UNIQUE | |
| TopicId | BIGINT | NOT NULL, FK → Topics(Id) | |
| Name | NVARCHAR(200) | NOT NULL | |
| Description | NVARCHAR(1000) | NULL | |
| EstimatedDurationMinutes | INT | NULL | Used by scheduling engine |
| SortOrder | INT | NOT NULL | |

### Resources

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| Id | BIGINT IDENTITY | PK | |
| ExternalId | UNIQUEIDENTIFIER | NOT NULL, UNIQUE | |
| SubjectId | BIGINT | NOT NULL, FK → Subjects(Id) | |
| Name | NVARCHAR(200) | NOT NULL | |
| ResourceType | TINYINT | NOT NULL | 0=Video, 1=Link, 2=File |
| Url | NVARCHAR(2000) | NULL | For Video and Link types |
| FilePath | NVARCHAR(500) | NULL | For File type |
| SortOrder | INT | NOT NULL | |

---

## Schema: `[mentoring]`

### MentoringPlans

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| Id | BIGINT IDENTITY | PK | |
| ExternalId | UNIQUEIDENTIFIER | NOT NULL, UNIQUE | |
| ProjectId | BIGINT | NOT NULL | |
| IncubatorId | BIGINT | NOT NULL | Tenant scoping |
| EntrepreneurUserId | BIGINT | NOT NULL | |
| DiagnosticResponseId | BIGINT | NULL | Source diagnostic for this plan |
| Status | TINYINT | NOT NULL, DEFAULT 0 | 0=Draft, 1=Approved, 2=InProgress, 3=Completed |
| ApprovedByUserId | BIGINT | NULL | |
| ApprovedAtUtc | DATETIME2 | NULL | |
| CreatedAtUtc | DATETIME2 | NOT NULL | |

### PlanTopics

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| Id | BIGINT IDENTITY | PK | |
| MentoringPlanId | BIGINT | NOT NULL, FK | |
| TopicId | BIGINT | NOT NULL | Ref to [knowledge].Topics(Id) |
| TopicName | NVARCHAR(200) | NOT NULL | Snapshot at plan creation |
| CumulativeScore | DECIMAL(10,2) | NOT NULL | From diagnostic aggregation |
| PriorityLevel | TINYINT | NOT NULL | 0=High, 1=Medium, 2=Low, 3=NotApplicable |
| IsIncluded | BIT | NOT NULL | Final inclusion decision |
| IsManualOverride | BIT | NOT NULL, DEFAULT 0 | Was manually added/removed |
| OverrideJustification | NVARCHAR(500) | NULL | |
| SwotSummary | NVARCHAR(200) | NULL | Interpretive context |
| OdsrSummary | NVARCHAR(200) | NULL | Interpretive context |
| SortOrder | INT | NOT NULL | |

### SessionCalendars

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| Id | BIGINT IDENTITY | PK | |
| ExternalId | UNIQUEIDENTIFIER | NOT NULL, UNIQUE | |
| MentoringPlanId | BIGINT | NOT NULL, FK | |
| ProjectId | BIGINT | NOT NULL | |
| IncubatorId | BIGINT | NOT NULL | |
| SessionsPerWeek | INT | NOT NULL | |
| HoursPerSession | DECIMAL(4,2) | NOT NULL | |
| StartDate | DATE | NOT NULL | |
| CreatedAtUtc | DATETIME2 | NOT NULL | |

### MentoringSessions

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| Id | BIGINT IDENTITY | PK | |
| ExternalId | UNIQUEIDENTIFIER | NOT NULL, UNIQUE | |
| SessionCalendarId | BIGINT | NOT NULL, FK | |
| ScheduledDate | DATETIME2 | NOT NULL | |
| Status | TINYINT | NOT NULL, DEFAULT 0 | 0=Scheduled, 1=InProgress, 2=Completed, 3=Cancelled |
| ConductedByMentorUserId | BIGINT | NULL | Which mentor actually conducted |
| Notes | NVARCHAR(MAX) | NULL | Session log |
| Decisions | NVARCHAR(MAX) | NULL | Key decisions made |
| StartedAtUtc | DATETIME2 | NULL | |
| CompletedAtUtc | DATETIME2 | NULL | |

### SessionTopicsCovered (junction)

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| MentoringSessionId | BIGINT | NOT NULL, FK | |
| PlanTopicId | BIGINT | NOT NULL, FK | |
| PK: (MentoringSessionId, PlanTopicId) | | |

### Assignments

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| Id | BIGINT IDENTITY | PK | |
| ExternalId | UNIQUEIDENTIFIER | NOT NULL, UNIQUE | |
| ProjectId | BIGINT | NOT NULL | |
| IncubatorId | BIGINT | NOT NULL | |
| SubjectId | BIGINT | NOT NULL | Ref to [knowledge].Subjects(Id) |
| EntrepreneurUserId | BIGINT | NOT NULL | |
| AssignedByMentorUserId | BIGINT | NOT NULL | |
| Title | NVARCHAR(200) | NOT NULL | |
| Instructions | NVARCHAR(MAX) | NOT NULL | |
| Deadline | DATETIME2 | NOT NULL | |
| Status | TINYINT | NOT NULL, DEFAULT 0 | 0=Pending, 1=Submitted, 2=Approved, 3=RevisionRequested |
| CreatedAtUtc | DATETIME2 | NOT NULL | |

### Submissions

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| Id | BIGINT IDENTITY | PK | |
| AssignmentId | BIGINT | NOT NULL, FK → Assignments(Id) | |
| Content | NVARCHAR(MAX) | NULL | Text submission |
| FilePath | NVARCHAR(500) | NULL | File submission |
| SubmittedAtUtc | DATETIME2 | NOT NULL | |

### ReviewFeedback

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| Id | BIGINT IDENTITY | PK | |
| SubmissionId | BIGINT | NOT NULL, FK → Submissions(Id) | |
| ReviewerUserId | BIGINT | NOT NULL | |
| Feedback | NVARCHAR(MAX) | NOT NULL | |
| Decision | TINYINT | NOT NULL | 0=Approved, 1=RevisionRequested |
| ReviewedAtUtc | DATETIME2 | NOT NULL | |

---

## Schema: `[subscription]`

### SubscriptionPlans

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| Id | BIGINT IDENTITY | PK | |
| ExternalId | UNIQUEIDENTIFIER | NOT NULL, UNIQUE | |
| Name | NVARCHAR(200) | NOT NULL | |
| Description | NVARCHAR(1000) | NULL | |
| Version | INT | NOT NULL, DEFAULT 1 | |
| IsActive | BIT | NOT NULL, DEFAULT 1 | |
| CreatedAtUtc | DATETIME2 | NOT NULL | |

### PlanFeatures

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| Id | BIGINT IDENTITY | PK | |
| SubscriptionPlanId | BIGINT | NOT NULL, FK | |
| FeatureKey | NVARCHAR(100) | NOT NULL | e.g., "advanced_diagnostics", "max_projects" |
| FeatureType | TINYINT | NOT NULL | 0=Boolean, 1=Quantitative |
| BooleanValue | BIT | NULL | For boolean features |
| QuantitativeValue | INT | NULL | For quantitative features |

**Indexes**:
- `UQ_PlanFeatures_Unique` — UNIQUE(SubscriptionPlanId, FeatureKey)

### IncubatorOverrides

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| Id | BIGINT IDENTITY | PK | |
| IncubatorId | BIGINT | NOT NULL | |
| FeatureKey | NVARCHAR(100) | NOT NULL | Matches PlanFeatures.FeatureKey |
| AdditionalValue | INT | NOT NULL | Positive-only additive override |
| Reason | NVARCHAR(500) | NULL | |
| CreatedAtUtc | DATETIME2 | NOT NULL | |
| CreatedByUserId | BIGINT | NOT NULL | |

---

## Schema: `[notification]`

### Notifications

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| Id | BIGINT IDENTITY | PK | |
| ExternalId | UNIQUEIDENTIFIER | NOT NULL, UNIQUE | |
| NotificationType | NVARCHAR(100) | NOT NULL | e.g., "SessionReminder", "WeeklyAgenda" |
| Subject | NVARCHAR(500) | NOT NULL | |
| Body | NVARCHAR(MAX) | NOT NULL | HTML email body |
| SourceEventId | NVARCHAR(200) | NULL | For deduplication |
| ScheduledForUtc | DATETIME2 | NULL | NULL = immediate |
| CreatedAtUtc | DATETIME2 | NOT NULL | |

**Indexes**:
- `IX_Notifications_SourceEventId` — For deduplication checks

### NotificationRecipients

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| Id | BIGINT IDENTITY | PK | |
| NotificationId | BIGINT | NOT NULL, FK | |
| UserId | BIGINT | NOT NULL | |
| DeliveryChannel | TINYINT | NOT NULL, DEFAULT 0 | 0=Email |
| DeliveryStatus | TINYINT | NOT NULL, DEFAULT 0 | 0=Pending, 1=Sent, 2=Failed, 3=Suppressed |
| SentAtUtc | DATETIME2 | NULL | |
| FailureReason | NVARCHAR(500) | NULL | |

### NotificationPreferences

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| Id | BIGINT IDENTITY | PK | |
| UserId | BIGINT | NOT NULL | |
| IncubatorId | BIGINT | NULL | NULL = all incubators |
| Role | NVARCHAR(50) | NULL | NULL = all roles |
| NotificationType | NVARCHAR(100) | NOT NULL | |
| IsEnabled | BIT | NOT NULL, DEFAULT 1 | |

---

## Schema: `[audit]`

### AuditLog

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| Id | BIGINT IDENTITY | PK | |
| EventType | NVARCHAR(100) | NOT NULL | e.g., "Login", "ContextChange", "StageAdvanced" |
| UserId | BIGINT | NULL | NULL for system events |
| IncubatorId | BIGINT | NULL | |
| ProjectId | BIGINT | NULL | |
| EntityType | NVARCHAR(100) | NULL | What was affected |
| EntityId | NVARCHAR(100) | NULL | ExternalId of affected entity |
| Action | NVARCHAR(50) | NOT NULL | Create, Update, Delete, Login, Logout, etc. |
| Details | NVARCHAR(MAX) | NULL | JSON payload with before/after state |
| IpAddress | NVARCHAR(45) | NULL | |
| OccurredAtUtc | DATETIME2 | NOT NULL | |

**Indexes**:
- `IX_AuditLog_EventType_OccurredAtUtc` — (EventType, OccurredAtUtc DESC)
- `IX_AuditLog_UserId_OccurredAtUtc` — (UserId, OccurredAtUtc DESC)
- `IX_AuditLog_EntityType_EntityId` — (EntityType, EntityId)

---

## State Transitions

### AccountStatus (Identity)

```
PendingVerification ──[email verified]──→ Active
Active ──[failed logins exceed threshold]──→ Locked
Active ──[admin disables]──→ Disabled
Active ──[password expired]──→ PasswordResetRequired
Locked ──[lockout period expires]──→ Active
Locked ──[admin unlocks]──→ Active
Disabled ──[admin re-enables]──→ Active
PasswordResetRequired ──[password changed]──→ Active
```

### ProjectStage (Tenant)

```
Registration (NotStarted → InProgress → Completed)
  → Forms (NotStarted → InProgress → Completed)
    → Analysis (NotStarted → InProgress → Completed)
      → LearningAssignment (NotStarted → InProgress → Completed)
        → Mentoring (NotStarted → InProgress → Completed)
          → FinalEvaluation (NotStarted → InProgress → Completed)
            → Closure (NotStarted → InProgress → Completed)

Transition rule: Current stage must be Completed before next stage can start.
No skipping. No backward movement. Manual advancement by Project Coordinator.
```

### MentoringPlan Status

```
Draft ──[mentor + entrepreneur approve]──→ Approved
Approved ──[first session starts]──→ InProgress
InProgress ──[all topics covered]──→ Completed
```

### Assignment Status

```
Pending ──[entrepreneur submits]──→ Submitted
Submitted ──[mentor approves]──→ Approved
Submitted ──[mentor requests revision]──→ RevisionRequested
RevisionRequested ──[entrepreneur resubmits]──→ Submitted
```

---

## Cross-Schema References

Cross-schema references use ID-only (no foreign key constraints across schemas), enforced at the application level:

| Source | Column | References | Notes |
|--------|--------|-----------|-------|
| [authorization].RoleAssignments.UserId | → | [identity].Users.Id | |
| [authorization].RoleAssignments.IncubatorId | → | [tenant].Incubators.Id | |
| [authorization].RoleAssignments.ProjectId | → | [tenant].Projects.Id | |
| [diagnostic].Questions.TopicId | → | [knowledge].Topics.Id | |
| [mentoring].PlanTopics.TopicId | → | [knowledge].Topics.Id | |
| [mentoring].Assignments.SubjectId | → | [knowledge].Subjects.Id | |
| [tenant].Incubators.SubscriptionPlanId | → | [subscription].SubscriptionPlans.Id | |

These references are maintained through integration events and validated at the application layer, not via SQL foreign keys. This preserves bounded context independence for future microservice extraction.
