# Feature Specification: Audit Pipeline Wiring

**Feature Branch**: `016-audit-pipeline`
**Created**: 2026-04-18
**Status**: Draft
**Input**: User description: "Audit Pipeline Wiring — uniform capture of security-sensitive commands via hybrid `[Audited]` attribute + MediatR pipeline behavior, with an explicit-call escape hatch. Closes FR-045 and FR-018b; satisfies SC-010."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Platform Admin reviews security-sensitive actions (Priority: P1)

A Platform Admin needs to see who did what across the system — role assignments, context switches, authentication events, content corrections — in one place, filter by event type or user, and see whether the action succeeded or failed. Today the only way is a raw database query.

**Why this priority**: Without this, the audit trail FR-045 has zero user-facing value. This story delivers the MVP: capture + storage + viewing. Everything else is reinforcement.

**Independent Test**: Log in as a Platform Admin, assign a role to a user, then correct a diagnostic answer, then navigate to `/Admin/AuditLog`. Both actions appear in reverse-chronological order with the acting user, target entity, outcome, and timestamp. Filter by event type `Role.Assigned` and only the first action remains.

**Acceptance Scenarios**:

1. **Given** a Platform Admin has performed a role assignment, **When** they open the audit log viewer, **Then** a row appears with event type `Role.Assigned`, the acting admin's email, the target entity identifier, outcome `Success`, and the UTC timestamp of completion.
2. **Given** a user fails a login attempt due to wrong credentials, **When** the Platform Admin filters the log by event type `User.LoggedIn` and outcome `Failure`, **Then** the failed attempt appears with the exception type populated.
3. **Given** 1,000 audit entries exist, **When** the admin sorts or filters the viewer, **Then** results render in a server-driven table without client-side lag.
4. **Given** a command payload contained a password, **When** the admin inspects the row's details, **Then** the password field shows a redaction sentinel (not the original value) and no trace of the original value appears anywhere in the stored entry.

---

### User Story 2 - Developer applies audit coverage to a new sensitive command (Priority: P2)

A developer adds a new security-sensitive command class (e.g., `ApproveMentoringPlanCommand`). They want the system to automatically require audit coverage and reject the change if they forget. If they apply the `[Audited]` attribute, capture happens automatically without touching the handler body.

**Why this priority**: Keeps the audit coverage comprehensive as the codebase grows. Without this, coverage decays to the retrofitted subset and new sensitive flows go uninstrumented. It's "earning the trust" of the P1 story over time.

**Independent Test**: Add a command named `ApproveSomethingCommand : IBaseRequest<...>` without the `[Audited]` attribute. Run the test suite. The architecture test fails with a clear message naming the command and the constitution rule. Add `[Audited(EventType = "Something.Approved")]`, re-run, test passes.

**Acceptance Scenarios**:

1. **Given** a command class named `Approve*`, `Assign*`, `Correct*`, `Advance*`, `Login*`, `Register*`, or `SetActive*`, **When** the architecture test runs in CI, **Then** the test fails with a message identifying the command type, the required attribute, and the constitution section if `[Audited]` is absent.
2. **Given** a handler whose command carries `[Audited(Mode = Automatic)]`, **When** the command is dispatched through MediatR and returns (success or failure), **Then** exactly one audit entry is written with the configured event type and the handler source code contains zero audit-related calls.
3. **Given** the handler throws an unhandled exception, **When** the pipeline unwinds, **Then** the audit entry is written with outcome `Failure`, exception type populated, the exception is rethrown, and the caller sees the original exception (not a swallowed one).

---

### User Story 3 - Auditor reconstructs an incident across multiple commands (Priority: P3)

A security reviewer investigating a suspicious sequence (e.g., "a user changed context, assigned themselves a role, then corrected an answer all within one session") needs to group audit entries that belong to the same HTTP request.

**Why this priority**: Correlation turns isolated rows into a timeline. Useful, but the P1 rows alone still answer most "who did what" questions. This is value-add, not critical path.

**Independent Test**: Trigger an HTTP request that dispatches two commands in sequence (e.g., `SetActiveContext` followed by `AssignRole`). Read the audit log. Both entries share the same correlation identifier. A request with the `X-Correlation-Id` header set to a known GUID produces entries with that GUID.

**Acceptance Scenarios**:

1. **Given** an HTTP request without a correlation header that triggers two commands, **When** the audit entries are written, **Then** both rows carry the same generated correlation identifier and the response carries it back in an `X-Correlation-Id` header.
2. **Given** a caller provides an `X-Correlation-Id` header, **When** the commands execute, **Then** the audit entries reuse that identifier verbatim.
3. **Given** a background hosted service dispatches a command with no HTTP context, **When** the audit entry is written, **Then** a fresh correlation identifier is generated (from the current activity if available, else a new GUID) and the row is still written.

---

### User Story 4 - Correction handler captures domain-specific detail (Priority: P2)

When a coordinator corrects a diagnostic answer, the audit entry must capture the previous answer text and the new answer text — not just that a correction occurred. Generic command-payload capture cannot express this because the "previous value" lives in the aggregate, not the command.

**Why this priority**: FR-018b is explicit that corrections need before/after traceability. The escape-hatch pattern exists for exactly this case; validating it on one real handler proves the pattern works for future use.

**Independent Test**: Correct an answer via the coordinator UI. The audit row's details contain both the previous answer text and the corrected text, alongside the standard payload. The `AnswerCorrection` aggregate still records its own correction history (no double-source-of-truth loss).

**Acceptance Scenarios**:

1. **Given** an answer with text "original", **When** it is corrected to "fixed", **Then** the audit row's details contain both "original" and "fixed" and the `AnswerCorrection` aggregate also reflects the change.
2. **Given** a `Manual`-mode command, **When** the handler completes without explicitly calling the audit logger, **Then** no audit row is written (developer responsibility — covered by a per-handler integration test, not the architecture test).

---

### Edge Cases

- **Handler exception before completion**: the pipeline catches the exception, records the audit entry with `Failure` outcome and the exception type, then rethrows the original exception unchanged.
- **Anonymous commands (Register, Login pre-auth)**: tenant context is empty. The handler or the attribute configuration pulls the acting user's email from the command payload itself; the row is written with only the fields that are knowable.
- **Very large command payload (>8 KB serialized)**: the details column is truncated at 8,000 characters with a truncation suffix. No row loss.
- **Payload serialization failure (e.g., circular graph)**: the details field is set to a sentinel string describing the failure; the row is still written with all other fields.
- **Audit database write failure**: the exception is caught, logged via the application logger, and does not fail the wrapped business command — the user sees the business outcome as if audit did not exist.
- **Missing correlation identifier source**: no HTTP context and no ambient activity — a fresh GUID is generated inline; the row still has a correlation identifier.
- **Concurrent commands in one request**: each gets its own row, all sharing the same correlation identifier. Ordering within the request is by occurrence timestamp then row id.
- **Long-running handler (>30 s)**: the occurrence timestamp reflects completion time, not dispatch time. Documented in the admin viewer's column help text for `OccurredAtUtc` and in the `AuditingBehavior` source-level XML doc comment.
- **Redaction of nested objects**: v1 redacts only top-level command properties (see FR-007). A command carrying a nested value object that itself contains a sensitive field (e.g., a `UserProfile` embedded in a command with a `NationalId` field) will NOT be redacted at that depth. Limitation documented; revisit when a handler hits it (tracked as OQ-1).
- **Missing `[Audited]` on a sensitive-named command**: the architecture test fails the build and directs the developer to the constitution section.

## Requirements *(mandatory)*

### Functional Requirements

#### Capture mechanism

- **FR-001**: System MUST provide an `[Audited]` attribute accepting `EventType` (string, required), `EntityType` (string, optional), and `Mode` (enum with values `Automatic` default and `Manual`) that can be applied to MediatR command classes.
- **FR-002**: System MUST provide an `AuditingBehavior<TRequest, TResponse>` pipeline behavior that runs AFTER the handler returns or throws, positioned after the existing `ValidatorBehavior` and after the `TransactionBehavior` so that the business transaction has committed or rolled back before the audit write is attempted.
- **FR-003**: For commands with `[Audited(Mode = Automatic)]`, the pipeline behavior MUST construct an audit entry from (a) the command type name as the `Action`, (b) a redacted JSON serialization of the command payload as the `Details`, (c) the active tenant context (`UserId`, `IncubatorId`, `ProjectId`, `RoleContext`, `UserEmail`), (d) the correlation identifier, (e) the occurrence timestamp from `ITimeProvider`, (f) the outcome (`Success` when the handler returned a successful `Result`, `Failure` otherwise), (g) the exception type when outcome is `Failure` and an exception was thrown, (h) the remote IP address exposed by the request-context abstraction (see FR-011a). The pipeline behavior MUST NOT take a direct dependency on `IHttpContextAccessor` to preserve Application-layer purity.
- **FR-004**: For commands with `[Audited(Mode = Manual)]`, the pipeline behavior MUST NOT emit an audit entry — the handler is responsible for calling `IAuditService.LogAsync` explicitly and MAY include domain-specific detail (e.g., before/after values fetched from an aggregate) in the entry's `Details` payload.
- **FR-005**: The audit write MUST be best-effort: a failure in the audit service (database unreachable, serialization error) MUST NOT fail the wrapped business command; the failure MUST be logged via the application logger.

#### Redaction

- **FR-006**: Before serializing the command payload, the pipeline behavior MUST replace any top-level property whose name matches the redaction list (case-insensitive) with the sentinel string `***REDACTED***`. The default redaction list is `Password`, `PasswordHash`, `NationalId`, `VerificationToken`, `Token`, `Secret`, `ApiKey`. The list MUST be configurable via an `AuditOptions.RedactedFields` setting so future commands can extend it.
- **FR-007**: Redaction depth is top-level only in v1 (see Open Questions).

#### Persistence

- **FR-008**: The `[audit].[AuditLog]` table MUST be extended to include: `CorrelationId UNIQUEIDENTIFIER NULL`, `Outcome NVARCHAR(20) NOT NULL DEFAULT 'Success'`, `ExceptionType NVARCHAR(200) NULL`, `UserEmail NVARCHAR(256) NULL`, `RoleContext NVARCHAR(50) NULL`. A non-clustered index on `CorrelationId` MUST be added for cross-entry lookup.
- **FR-009**: The `AuditEntry` record and `AuditService.LogAsync` implementation MUST be updated to carry and persist the new fields.
- **FR-010**: The audit write MUST execute OUTSIDE the caller's database transaction (preserving the current ADO.NET-direct implementation) so that an audit failure cannot roll back the business transaction and vice versa.

#### Correlation

- **FR-011**: System MUST provide an `ICorrelationContext` abstraction and a web middleware that reads the `X-Correlation-Id` request header when present (and validates it as a GUID), generates a fresh GUID when absent, and attaches the value to the response as `X-Correlation-Id`.
- **FR-011a**: The request-context abstraction (co-located with `ICorrelationContext`, e.g., `IRequestContext` or an additional member on `ICorrelationContext`) MUST expose the client IP address (populated from `HttpContext.Connection.RemoteIpAddress` in the web implementation, `null` in non-HTTP implementations). Application-layer code, including `AuditingBehavior`, MUST consume IP and correlation values through this abstraction, never through `IHttpContextAccessor` directly, to preserve the constitution's Clean Architecture boundary.
- **FR-012**: When commands are dispatched from a non-HTTP context (e.g., a hosted background service), the correlation context MUST derive the identifier from the current ambient activity if one exists, else generate a fresh GUID. In the non-HTTP implementation the client IP address MUST be `null`.

#### Retrofit

- **FR-013**: The following five already-shipped command classes MUST carry `[Audited]` with the listed event types:
  - `SetActiveContextCommand` → `Context.Activated` (Automatic)
  - `AssignRoleCommand` → `Role.Assigned` (Automatic)
  - `RegisterUserCommand` → `User.Registered` (Automatic)
  - `LoginUserCommand` → `User.LoggedIn` (Automatic)
  - `CorrectAnswerCommand` → `Answer.Corrected` (Manual — handler captures before/after answer text in `Details`)
- **FR-014**: For anonymous-phase commands (`RegisterUser`, `LoginUser`), the audit entry MUST pull `UserId` and `UserEmail` from the command payload itself rather than from the empty tenant context.

#### Enforcement

- **FR-015**: An architecture test MUST be added that asserts: every type implementing `IBaseRequest` or `IBaseRequest<T>` whose name matches the sensitive-action regex `^(Assign|Approve|Correct|Advance|Login|Register|SetActive).*Command$` MUST be decorated with `[Audited]`. Build MUST fail otherwise.
- **FR-016**: The sensitive-action regex MUST be hardcoded as a constant in the architecture test with a comment pointing to the canonical definition in `access-security-constitution.md`. Future features extending the category list MUST extend both the regex and the constitution.

#### Admin viewer

- **FR-017**: System MUST expose a Platform-Admin-only page at `/Admin/AuditLog` that displays audit entries in a read-only server-side DataTable (pattern established by feature 013).
- **FR-018**: The viewer MUST surface columns `OccurredAtUtc`, `EventType`, `UserEmail`, `Action`, `Outcome`, `RoleContext` with default sort by `OccurredAtUtc DESC`.
- **FR-019**: The viewer MUST support filters on `EventType` (dropdown populated from DISTINCT values), `UserId` (typeahead by email), `Outcome` (Success / Failure), and a date range (from / to).
- **FR-020**: The viewer MUST provide an expandable detail row exposing the full `Details` JSON pretty-printed. No edit or delete actions are exposed.
- **FR-021**: Access to the viewer MUST be restricted to `GlobalAdmin` via `[Authorize(Roles = "GlobalAdmin")]`.

#### Governance

- **FR-022**: `.specify/memory/access-security-constitution.md` MUST gain a new section titled "Audit Trail Obligations" containing: (a) the definition of a sensitive command (the seven name patterns in FR-015), (b) the rule that `[Audited]` is mandatory on such commands, (c) a pointer to FR-015 as the CI enforcement gate, (d) the obligation that future features adding new command categories extend the regex in the architecture test and this constitution section together.
- **FR-023**: The constitution version footer MUST be bumped following the constitution's existing SemVer policy (additive governance patch → minor bump).

### Key Entities

- **Audit entry**: a single immutable record of one command execution. Attributes: occurrence timestamp (UTC), event type, action (the command type name), user identifier, user email, incubator identifier, project identifier, role context, entity type and identifier (when applicable), correlation identifier, outcome, exception type (when failed), redacted command payload, remote IP address.
- **Correlation context**: a per-logical-operation identifier shared across all audit entries produced by the same logical operation (an HTTP request or a background-job activity). Primary use is incident reconstruction.
- **Audited command**: a MediatR command class carrying the `[Audited]` attribute, marking it as a participant in the audit trail. Mode determines whether the pipeline behavior writes the entry automatically or the handler owns the call.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: After deployment, a Platform Admin can identify, for any of the five retrofitted command types, who performed the action, when, under which tenant/role context, and whether it succeeded, within 24 hours of the action — visible in one place, without running database queries (satisfies FR-045 / SC-010 / FR-018b).
- **SC-002**: 100% of actions matching the five retrofitted command types produce an audit record regardless of whether the command succeeded or failed.
- **SC-003**: Zero redacted-value leaks: any audit entry inspected by the admin shows the redaction sentinel for sensitive fields and contains no occurrence of the original sensitive value.
- **SC-004**: A build containing a new sensitive-named command without the required `[Audited]` attribute fails continuous integration with a message naming the command and pointing to the governance rule.
- **SC-005**: Audit system failure (database unreachable) leaves the business flow unaffected: the business command returns its normal result and the admin still sees the action if audit recovers; no user-visible error, no rolled-back business transaction.
- **SC-006**: Two or more actions triggered by the same originating operation can be grouped and reviewed together by a single identifier shared across their audit entries.
- **SC-007**: A Platform Admin can open the audit viewer and see the most recent 1,000 entries rendered on a modern browser (Chrome/Firefox/Edge on broadband) within 1 second from navigation to a fully interactive table, with filters and sorting responsive to input.
- **SC-008**: Answer corrections capture both the previous answer text and the new answer text in the audit record, visible to the Platform Admin without consulting the domain aggregate.
- **SC-009**: The governance rule is discoverable: a developer reading `access-security-constitution.md` finds the audit obligation, the enforcement mechanism, and an extension procedure without needing oral context.

## Assumptions

- `ITenantContext`, `ITimeProvider`, `IHttpContextAccessor`, and the existing MediatR pipeline (with `ValidatorBehavior` and `TransactionBehavior`) are available and unchanged; the audit behavior slots in after them.
- The existing `IAuditService` / `AuditService` contract (best-effort, ADO.NET direct, swallows exceptions) is preserved. Only the record shape and the column set change; the write path stays the same.
- The five retrofitted commands already exist, have stable contracts, and do not carry business changes in this feature — only attribute decoration and, for `CorrectAnswer`, one added audit call.
- The Platform Admin area uses the feature-013 DataTable pattern and the existing `[Authorize(Roles = "GlobalAdmin")]` convention; no new authorization primitives are introduced.
- DACPAC-based schema migration via `publish-mentoorydb.sh` is the canonical mechanism for the `AuditLog` column additions and index.
- Redaction at the top-level-property depth is sufficient for v1; nested-object depth is deferred pending a concrete triggering case.
- `AdvanceProjectStageCommand` and `ApproveMentoringPlanCommand` are out of scope and will receive `[Audited]` when they ship in their own feature specs; the governance rule added by this feature will surface the gap via the CI architecture test the moment those commands land.
- Audit retention / TTL is out of scope; existing rows live forever until a future feature introduces a retention policy.
- Audit-driven notifications to Platform Admins are out of scope; notifications are the Notification module's concern, and audit is for read-back, not alerting.

## Open Questions

- **OQ-1**: Nested-object redaction depth — v1 redacts only top-level command properties. Should a future version walk value objects and collections? Deferred until a concrete Manual-mode handler requires nested redaction.
- **OQ-2**: Correlation identifier propagation from background jobs — v1 derives from `Activity.Current?.Id` if available, else generates fresh. Should jobs instead accept an explicit correlation identifier as part of their input? Revisit when the first non-trivial hosted-service command lands.
- **OQ-3**: Audit retention / archival — SC-010 requires 24-hour traceability, not permanence. A retention policy is out of scope; the decision about 90-day vs 1-year vs indefinite retention belongs to a later operational feature.
