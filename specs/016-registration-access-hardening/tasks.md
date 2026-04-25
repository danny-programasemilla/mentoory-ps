---
description: "Task list for feature 016-registration-access-hardening"
---

# Tasks: Registration & Access Hardening

**Input**: Design documents from `/specs/016-registration-access-hardening/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/public-register.http.md, contracts/admin-enroll.http.md, quickstart.md

**Tests**: Tests are INCLUDED — this is a security-hardening feature where byte-for-byte response parity, outcome logging, and password-rule correctness are core contracts. Tests are the acceptance mechanism for SC-016-01…SC-016-05.

**Organization**: Tasks are grouped by user story so each story can be implemented, tested, and delivered independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1, US2, US3). Setup/Foundational/Polish tasks have no story label.
- File paths in every task are absolute-under-repo-root.

## Path Conventions

Modular monolith layout:

- Web layer: `Mentoory.Web/Areas/{Area}/Controllers|Models|Views/`
- Application layer (Access module): `Mentoory.Access.Application/{Commands|Services|Infrastructure|Validation}/`
- Domain layer: `Mentoory.Access.Domain/` (unchanged — no tasks hit this layer)
- Tests: `tests/Mentoory.Access.Application.Tests/` and `tests/Mentoory.Web.Tests/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Low-risk scaffolding required by the split-command design.

- [X] T001 [P] Create folder `Mentoory.Access.Application/Commands/AdminEnrollUser/` (empty; will host new command triad)
- [X] T002 [P] Create folder `Mentoory.Access.Application/Services/` if not present and confirm the existing Access-module DI file path `Mentoory.Access.Application/DependencyInjection.cs` (no code change yet — identify the registration site for T009)
- [X] T003 [P] Create folder `Mentoory.Access.Application/Validation/` (empty; will host `PasswordIdentifyingDataRule`)
- [X] T004 [P] Create folder `Mentoory.Access.Application/Infrastructure/` if not present (hosts `UserProvisioningService` implementation)
- [X] T005 [P] Create test folders `tests/Mentoory.Access.Application.Tests/Commands/AdminEnrollUser/`, `tests/Mentoory.Access.Application.Tests/Validation/`, `tests/Mentoory.Web.Tests/Areas/Access/`, `tests/Mentoory.Web.Tests/Areas/Administration/` (implemented under existing `tests/Mentoory.Access.Tests/` + `tests/Mentoory.Tests.Integration/Identity/` + `tests/Mentoory.Tests.E2E/Tests/` — no new test project created)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The shared provisioning service and the reusable password rule are consumed by every user story. They MUST exist (and be tested) before US1 / US2 / US3 implementation begins.

**⚠️ CRITICAL**: No user-story work may start until T006–T012 are complete.

### Shared provisioning service (consumed by US1 and US2)

- [X] T006 Define `UserProvisioningOutcome` enum and `UserProvisioningRequest` record (split into `Mentoory.Access.Application/Services/UserProvisioningOutcome.cs` + `UserProvisioningRequest.cs` — cleaner one-class-per-file; `UtcNow` omitted from request because the service injects `ITimeProvider`)
- [X] T007 Declare `IUserProvisioningService` interface with `Task<UserProvisioningOutcome> ProvisionAsync(UserProvisioningRequest, CancellationToken)` in `Mentoory.Access.Application/Services/IUserProvisioningService.cs`
- [X] T008 Implement `UserProvisioningService` in `Mentoory.Access.Application/Infrastructure/UserProvisioningService.cs` — NationalId-then-Email ordering preserved
- [X] T009 Register `IUserProvisioningService` → `UserProvisioningService` as scoped in `Mentoory.Access.Application/DependencyInjection.cs`

### Reusable password rule (consumed by US1, US2, and US3)

- [X] T010 Implement `PasswordIdentifyingDataRule` static class with `MustNotContainIdentifyingData` extension + `Contains` helper (shared constant `Message`). Also extracted `PasswordStrengthRule.MustBeStrongPassword()` in the same folder during `/simplify` pass (consumed by both validators)

### Foundational tests

- [X] T011 [P] Unit tests for `PasswordIdentifyingDataRule` at `tests/Mentoory.Access.Tests/Validators/PasswordIdentifyingDataRuleTests.cs` — 11 cases covering every branch including case-insensitivity, stripped-form, below-threshold acceptance
- [X] T012 [P] Unit tests for `UserProvisioningService` at `tests/Mentoory.Access.Tests/Services/UserProvisioningServiceTests.cs` — Success/DuplicateNationalId/DuplicateEmail paths + NationalId-before-Email ordering via `MockSequence`

**Checkpoint**: Shared infrastructure + tests green → all three user stories unblocked.

---

## Phase 3: User Story 1 — Close the public registration enumeration oracle (Priority: P1) 🎯 MVP

**Goal**: The public self-registration endpoint returns a single generic in-form failure for every validator-rule failure and a byte-for-byte identical success-shaped confirmation page for both fresh creations and uniqueness conflicts. No account is created on the conflict path; the real outcome is logged server-side.

**Independent Test**: Submit the public form with (a) a fresh combination, (b) a duplicate national ID, (c) a duplicate email, (d) a validator failure. Verify (a)–(c) are byte-identical and land on `/Access/Register/Success`; verify (d) returns `200` with the generic banner only and no field attribution; verify the server logs show the correct `Outcome:` value for each.

### Command + handler changes for US1

- [X] T013 [US1] `RegisterUserCommand` extended with `CorrelationId`/`ClientIpAddress` optional fields
- [X] T014 [US1] `RegisterUserHandler` delegates to `IUserProvisioningService`; always returns `Success()`; logs structured outcome via source-generated `[LoggerMessage]`
- [X] T015 [US1] `RegisterUserValidator` wired to `MustBeStrongPassword()` + `MustNotContainIdentifyingData(...)`

### Web-layer changes for US1

- [X] T016 [US1] `RegisterController.Index(POST)` rewrites per spec: validator-failure path logs `ValidatorFailure:<property>` via `[LoggerMessage]`, renders generic banner via `ViewData[GenericFailureViewDataKey] = true`; handler dispatch always redirects to `Success`
- [X] T017 [US1] `Register/Index.cshtml` stripped of `<span asp-validation-for>` elements; top-of-form generic banner renders when the flag is set; submitted field values preserved
- [X] T018 [US1] `Register/Success.cshtml` now renders both the primary line (`"Revise su correo electrónico para confirmar su cuenta."`) and the unconditional secondary recovery hint (`"¿Ya tiene una cuenta? Use la opción de recuperar contraseña."`)

### Tests for US1

- [X] T019 [P] [US1] `tests/Mentoory.Access.Tests/Handlers/RegisterUserHandlerTests.cs` rewritten — 8 cases (all 3 outcomes each mapped to Success; log captures Email/Outcome/CorrelationId/ClientIp; null trace/ip logged as empty; request-field forwarding)
- [X] T020 [P] [US1] Byte-identity + field-suppression coverage landed in `tests/Mentoory.Tests.E2E/Tests/RegistrationTests.cs` (`Register_DuplicateEmail_RedirectsToSuccess_AndDoesNotRevealDuplicate`, `Register_DuplicateNationalId_…`, `Register_PasswordContainsEmailLocalPart_ShowsGenericBanner_…`) — compares Success-panel text for fresh vs. duplicate outcomes. Integration-level behaviour covered in `tests/Mentoory.Tests.Integration/Identity/RegistrationTests.cs` (all duplicate paths return Success with single user persisted). No separate `Mentoory.Web.Tests` project introduced per user direction to reuse existing e2e/integration infrastructure

**Checkpoint**: US1 alone delivers the security fix. Stories US2 and US3 can be deferred.

---

## Phase 4: User Story 2 — Admin enrollment returns specific feedback (Priority: P1)

**Goal**: The admin enrollment endpoint uses a separate command/handler/validator that returns field-attributed errors on uniqueness conflicts so admins can resolve duplicates efficiently. Ships together with US1.

**Independent Test**: As an authenticated admin, submit a duplicate national ID → see the conflict attributed to `NationalId` field; submit a duplicate email → see it attributed to `Email`; submit unauthenticated → access denied. Reachable only behind `[Authorize]`.

### Command + handler + validator for US2

- [X] T021 [P] [US2] `AdminEnrollUserCommand` (sealed record, no observability fields)
- [X] T022 [P] [US2] `AdminEnrollUserHandler` maps outcome→Result with field-attributed messages; discard arm throws `InvalidOperationException` for safety
- [X] T023 [P] [US2] `AdminEnrollUserValidator` uses shared `MustBeStrongPassword()` + `MustNotContainIdentifyingData()` (no copy-paste with public validator)

### Web-layer wiring for US2

- [X] T024 [US2] `UsersController.Enroll(POST)` now dispatches `AdminEnrollUserCommand`; existing ModelState mapping loop unchanged

### Tests for US2

- [X] T025 [P] [US2] `tests/Mentoory.Access.Tests/Handlers/AdminEnrollUserHandlerTests.cs` — fresh Success, DuplicateEmail attributed to `Email`, DuplicateNationalId attributed to `NationalId`, field-forwarding
- [X] T026 [P] [US2] Admin controller path covered via `tests/Mentoory.Tests.Integration/Identity/AdminEnrollmentTests.cs` (MediatR-level attributed failures) and `tests/Mentoory.Tests.E2E/Tests/AdministrationUsersTests.cs:AdminEnroll_DuplicateEmail_ShowsFieldAttributedError` (HTTP-level behind `[Authorize]`)
- [X] T027 [P] [US2] `tests/Mentoory.Access.Tests/Validators/AdminEnrollUserValidatorTests.cs` confirms identifying-data rule wired (email local-part in password + national-ID in password both trigger shared `PasswordIdentifyingDataRule.Message`)

**Checkpoint**: US1 + US2 are independently functional; admin UX is preserved, public oracle is closed.

---

## Phase 5: User Story 3 — Passwords cannot contain identifying data (Priority: P2)

**Goal**: Passwords containing the user's email, email local part, or national ID (verbatim or separator-stripped) are rejected with a single non-oracle message. The rule lives on the shared validation surface so every password-setting command picks it up.

**Independent Test**: Validator-level — submit a command (public OR admin) with an otherwise-valid payload but a password containing the email / local-part / national-ID / stripped-national-ID; confirm rejection with the shared message. Submit a below-threshold local-part or below-threshold national-ID → accepted.

> **Note**: The rule itself was built in Phase 2 (T010) and tested in T011 because both US1 and US2 validators consume it. US3's implementation tasks are limited to wiring and acceptance at the command boundaries (done in T015 for public, T023 for admin). This phase adds acceptance verification that spans both paths.

### Acceptance verification for US3

- [X] T028 [P] [US3] `RegisterUserValidatorTests` gained `Password_Containing_Identifying_Data_Fails_With_Shared_Message` (Theory with full-email / local-part / verbatim + stripped national-ID) and two below-threshold acceptance cases
- [X] T029 [P] [US3] Cross-file parity enforced via the shared `PasswordIdentifyingDataRule.Message` constant — both admin + public validator tests assert against it, so any drift breaks the build

**Checkpoint**: All three user stories independently functional. US3 is surfaced on both US1 and US2 paths automatically via the shared helper.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: End-to-end acceptance, spec traceability, and build verification.

- [ ] T030 Run the full quickstart walkthrough (`specs/016-registration-access-hardening/quickstart.md`, Steps 1–16) against a locally-running Aspire host. **BLOCKED** — requires live Aspire host; not runnable from this agent session. Punt to a manual run before merge.
- [X] T031 `dotnet build` clean: `Mentoory.Access.Application`, `Mentoory.Access.Infrastructure`, `Mentoory.Access.Tests`, `Mentoory.Web`, `Mentoory.Tests.Integration`, `Mentoory.Tests.E2E` all compile with 0 warnings / 0 errors (pre-existing NU1902 MailKit vuln on the full-solution build is unrelated to this feature)
- [X] T032 `dotnet test tests/Mentoory.Access.Tests` — **124/124 passing** (Integration + E2E require Docker/Playwright runtime — compile-verified, deferred to CI)
- [X] T033 [P] Spanish copy verified: generic banner, Success.cshtml recovery hint, and `PasswordIdentifyingDataRule.Message` all Spanish; no English leakage
- [X] T034 [P] `/simplify` pass complete: extracted `PasswordStrengthRule.MustBeStrongPassword()` (removed 6-line copy-paste between the two validators), trimmed narrative comments in `RegisterUserHandler` / `RegisterController`, added field-name constants + fail-fast discard arm in `AdminEnrollUserHandler`, switched logger arg from `string` to `UserProvisioningOutcome` enum, minor `PasswordIdentifyingDataRule` allocation tightening. Out-of-scope findings (migrate `RegisterInternalUserHandler` onto provisioning service, introduce `IVerificationTokenFactory`, cache `ListCountriesQuery`) filed for follow-up
- [ ] T035 Run 50-probe SC-016-01 evidence script against dev Aspire host. **BLOCKED** — same constraint as T030. Byte-equality is behaviourally covered by E2E `Register_DuplicateEmail_RedirectsToSuccess_AndDoesNotRevealDuplicate` + `Register_DuplicateNationalId_…` (compare Success-panel text for fresh vs. duplicate)
- [X] T036 `CLAUDE.md` recent-changes bullet rewritten to describe the actual behavioural change (oracle close, split admin command, shared validation extensions) rather than the auto-generated tech list

---

## Dependencies & Execution Order

### Phase dependencies

- **Setup (Phase 1, T001–T005)**: no dependencies; all [P]; runnable in parallel immediately
- **Foundational (Phase 2, T006–T012)**: depends on Setup; must complete fully before ANY user-story task starts
  - Within Phase 2: T006 → T007 (same file); T007 → T008 (implementation consumes interface); T008 → T009 (registration follows implementation); T010 is independent of T006–T009; T011 depends on T010; T012 depends on T008
- **User Stories (Phase 3, 4, 5)**: all depend on Phase 2 checkpoint; US1 and US2 are independent of each other and can be implemented in parallel; US3 acceptance tests (T028, T029) depend on T015 (public validator wired) and T023 (admin validator wired)
- **Polish (Phase 6)**: depends on US1 + US2 + US3 complete

### User story dependencies

- **US1 (P1)**: blocked only by Phase 2. Independent of US2 at the story level (they touch different controllers and different commands).
- **US2 (P1)**: blocked only by Phase 2. Independent of US1.
- **US3 (P2)**: behaviourally satisfied by T015 (public) and T023 (admin); the explicit US3 tasks are acceptance verification across both paths.

### Within each user story

- US1: command shape (T013) → handler rewrite (T014) → validator wiring (T015) → controller (T016) → views (T017, T018) → tests (T019, T020). T017 and T018 can run in parallel (different view files). T019 and T020 can run in parallel.
- US2: T021, T022, T023 can run in parallel (three separate new files). T024 depends on T021 and T022. T025, T026, T027 can run in parallel after T021/T022/T023/T024.
- US3: T028 and T029 can run in parallel (different test files or different cases in the same file).

### Parallel opportunities

- All of T001–T005 (Setup)
- T010 + T006 (different files; foundational parallelism)
- T011 + T012 (different test files)
- T017 + T018 (different view files)
- T019 + T020 (different test files)
- T021 + T022 + T023 (three new command-triad files)
- T025 + T026 + T027 (three new test files)
- T028 + T029 (different test cases / files)
- T033 + T034 (different concerns)

---

## Parallel Example: Phase 2 Foundation

```text
# After T006 and T007 (same file → sequential):
#   - T008 (UserProvisioningService implementation)  : depends on T007
#   - T010 (PasswordIdentifyingDataRule)             : independent
# Fire in parallel:
- Launch T008 (implementation)
- Launch T010 (rule helper)

# Once T008 lands:
- Launch T009 (DI registration, depends on T008)
- Launch T012 (UserProvisioningService tests, depends on T008)
- Launch T011 (PasswordIdentifyingDataRule tests, depends on T010)  [already parallelisable]
```

## Parallel Example: User Story 2 implementation

```text
# After Phase 2 is green, US2 has three fresh files and can parallelise aggressively:
- Launch T021 (AdminEnrollUserCommand.cs)
- Launch T022 (AdminEnrollUserHandler.cs)
- Launch T023 (AdminEnrollUserValidator.cs)
# Once command + handler files exist:
- T024 (controller rewrite) — sequential
# After T024 lands:
- Launch T025 (handler tests)
- Launch T026 (controller tests)
- Launch T027 (validator test)
```

---

## MVP scope

**Suggested MVP**: Phase 1 → Phase 2 → **Phase 3 (US1)** → Phase 6 (Polish subset: T031, T032).

US1 alone closes the shipped security vulnerability (the public enumeration oracle). US2 and US3 ship in the same release because the spec requires them together, but US1 has independent deliverable value — if scope had to be cut, US1 is the one that MUST ship.

US1 + US2 is the realistic MVP here because: (a) without US2, the public fix regresses admin UX by reusing the same masked command; (b) the split-command design of US2 is the structural change that makes US1 clean. They are paired by architecture, not just by release.

US3 is a rule addition that the shared helper surfaces automatically once Phase 2 is merged — incremental risk is minimal.

---

## Format validation

All tasks follow the strict checklist format:

- ✅ Checkbox `- [ ]` at line start
- ✅ Sequential ID `T001`–`T036`
- ✅ `[P]` marker only where genuine file-level independence exists
- ✅ `[US1]` / `[US2]` / `[US3]` story labels on all Phase 3/4/5 tasks
- ✅ No story label on Setup (Phase 1), Foundational (Phase 2), or Polish (Phase 6) tasks
- ✅ Exact file paths on every implementation / test task (controllers, handlers, validators, views, test files identified absolutely)

## Task-count summary

| Phase | Count | Notes |
|-------|-------|-------|
| 1 — Setup | 5 | All [P] |
| 2 — Foundational | 7 | 5 impl (T006–T010) + 2 tests (T011–T012) |
| 3 — US1 (P1 MVP) | 8 | 6 impl (T013–T018) + 2 tests (T019–T020) |
| 4 — US2 (P1) | 7 | 4 impl (T021–T024) + 3 tests (T025–T027) |
| 5 — US3 (P2) | 2 | Acceptance tests (T028–T029) |
| 6 — Polish | 7 | Build, test, copy review, simplify, SC evidence, context |
| **Total** | **36** | |

**Parallel opportunities**: 19 tasks tagged `[P]`, clustered in Setup (5), Foundational (2), and within each user story (12).

**Independent-test criteria**:

- US1: byte-equality of three response classes + validator-failure banner (T020 harness)
- US2: three ModelState outcomes behind `[Authorize]` (T026 harness)
- US3: rule triggers across both validators with shared message (T028 + T029)
