# Manual Test Guide — Feature 016 (Project Lifecycle)

**Audience**: QA reviewers, PMs, and engineers who want to exercise the shipped feature end-to-end in a local dev environment without reading code.

**Scope**: the Project Lifecycle flow for Coordinators — the `/Coordination/Projects` + `/Coordination/Projects/Lifecycle/{id}` pages, the advance-stage action, the stage-gated actions grid, and the audit/concurrency/inactive edge cases.

**Companion automation**: every walkthrough in this guide has at least one passing Playwright E2E test in `tests/Mentoory.Tests.E2E/Tests/Lifecycle/`. See the "Automated coverage" column in each section if you want to compare what the automation checks vs. what you're validating manually.

---

## 1. Setup

### 1.1 Prerequisites

- Docker daemon running (the test stack uses Testcontainers; the manual stack uses Aspire's SQL container).
- .NET 10 SDK installed (`dotnet --version` → `10.0.x`).
- Node is NOT required — the UI is Razor/Bootstrap only.
- Chromium or any modern browser.

### 1.2 Build the database schema

```bash
cd Mentoory.Db && ./publish-mentoorydb.sh
```

This builds the DACPAC. The schema includes the `RowVersion` column on `tenant.Projects` (added by this feature for optimistic concurrency) and the seed data for test users + projects.

### 1.3 Run the stack

```bash
dotnet run --project Mentoory.Aspire.AppHost
```

Aspire launches the web app + SQL Server + dependencies. The dashboard URL it prints in the console has links to `Mentoory.Web` — open that.

### 1.4 Seed data you should have

After first run:

| Entity | Identity | Purpose |
|---|---|---|
| Incubator | **Incubadora Alpha** | Hosts Proyecto Innovación + Proyecto Sostenibilidad |
| Incubator | **Incubadora Beta** | Hosts Proyecto Digital + Proyecto Comunitario |
| Project | **Proyecto Innovación** (Alpha) | Coord1's primary assignment; starts in Registration |
| Project | **Proyecto Sostenibilidad** (Alpha) | Coord2's primary assignment |
| Project | **Proyecto Digital** (Beta) | — |
| Project | **Proyecto Comunitario** (Beta) | — |

All four seed projects are created in stage **Registration / InProgress**.

### 1.5 Test accounts (all passwords: `Test123!@#`)

| Email | Role | Scope | Display name on screen |
|---|---|---|---|
| `coord1@test.mentoory.com` | ProjectCoordinator | Incubadora Alpha / Proyecto Innovación | Ana Rodríguez |
| `coord2@test.mentoory.com` | ProjectCoordinator | Incubadora Alpha / Proyecto Sostenibilidad | Luis Paredes |
| `coord3@test.mentoory.com` | ProjectCoordinator | Incubadora Alpha / Proyecto Innovación | Sofía Navarro |
| `incadmin1@test.mentoory.com` | IncubatorAdmin | Incubadora Alpha (all projects) | Carlos Mendoza |
| `incadmin2@test.mentoory.com` | IncubatorAdmin | Incubadora Beta (all projects) | María Fernández |
| `mentor1@test.mentoory.com` | Mentor | Incubadora Alpha / Proyecto Innovación | Roberto Sánchez |
| `admin@mentoory.com` | GlobalAdmin | Cross-incubator | (seed admin) — password `123abc987` |

Note: `coord3` is new in this feature (added by C5 for the audit-trail walkthrough). All three coords can advance any project in Incubadora Alpha because authorization is enforced at the incubator level, not per-project.

---

## 2. Walkthrough 1 — Advance a project (happy path)

**What you're validating**: SC-001 ("coordinators advance projects through the UI without using DB tools") and Story 1 acceptance scenarios.
**Automated coverage**: `WalkthroughAdvanceTests.Coordinator_AdvancesProjectFromRegistrationToForms`.

### Steps

1. Log in as `coord1@test.mentoory.com` / `Test123!@#`.
2. The left sidebar shows **Coordinación → Proyectos**. Click it.
3. You land on `/Coordination/Projects`. The DataTables list shows *Proyecto Innovación* (and any other Alpha-scoped projects you can see). The "Etapa Actual" column reads **Registro**.
4. Click the row or the "Ver ciclo de vida" action. URL becomes `/Coordination/Projects/Lifecycle/{externalId}`.
5. On the Lifecycle page confirm:
   - Project header card shows name, description, and a **Activo** badge.
   - Spanish current-stage badge reads **Registro**.
   - The 7-stage timeline shows:
     - Registro — **En progreso** (blue), with a start timestamp like `dd/MM/yyyy HH:mm`.
     - Formularios, Análisis, Asignación de Aprendizaje, Mentoría, Evaluación Final, Cierre — all **Pendiente** (grey), no timestamps.
   - A blue **Avanzar Etapa** button is visible at the bottom of the timeline card.
6. Click **Avanzar Etapa**. A Bootstrap confirmation modal opens titled *"Confirmar avance de etapa"* with text *"¿Confirma avanzar de Registro a Formularios?"*.
7. Click the confirm button in the modal. The page reloads.
8. Verify the post-advance state:
   - Green toast at the top: **"Proyecto avanzado a Formularios."**
   - Registro row now reads **Completada** with **Inicio:** and **Fin:** timestamps — but no "Avanzada por:" line (the initial stage is created without an acting user, see § 6 below).
   - Formularios row now reads **En progreso** with an **Inicio:** timestamp plus a new line **"Avanzada por: Ana Rodríguez"**.
   - Current stage badge (top-right) now reads **Formularios**.
   - **Avanzar Etapa** button is still visible (you can keep advancing).

### Edge checks (same project)

- Click **Avanzar Etapa** 5 more times to walk through Análisis → Asignación de Aprendizaje → Mentoría → Evaluación Final → Cierre. Each advance produces its own toast (e.g., "Proyecto avanzado a Análisis.").
- After the 6th advance (now on Cierre), the **Avanzar Etapa** button is no longer rendered. A muted grey text reads **"El proyecto ya está en la etapa final (Cierre)."** (There's no 7th-stage "close the project" flow — this is by design; see `open-questions.md` note A.)

---

## 3. Walkthrough 2 — Inspect the lifecycle at different states

**What you're validating**: Story 2 acceptance scenarios — the Lifecycle page renders correctly regardless of how far through the lifecycle the project is.
**Automated coverage**: `WalkthroughLifecyclePageTests` (3 tests: brand new, mid-lifecycle, closed).

### Steps

1. Reset state: either create a new project via the Administration area, or run the E2E test suite once (it resets the `e2e-lifecycle-*` prefixed projects between runs — your seed projects are untouched).
2. Open a **brand new** project (stage Registro). Confirm:
   - Only Registro has an **Inicio:** timestamp and the **En progreso** badge.
   - All other stages show **Pendiente**, no timestamps.
   - **Avanzar Etapa** button is visible.
3. Use the Admin UI or the other test user (coord2, working on Proyecto Sostenibilidad) to advance a project partway through. Open its Lifecycle page. Confirm:
   - Stages up to the current one are **Completada** with both Inicio/Fin timestamps + "Avanzada por:" attributions (on stages that were started by a user, not the initial Registration).
   - Current stage is **En progreso** with Inicio only.
   - Later stages are **Pendiente**.
4. Advance a project to Cierre. Confirm:
   - 6 prior stages are **Completada**.
   - Cierre is **En progreso** (never reaches Completada — by design).
   - **Avanzar Etapa** is hidden.
   - Muted text: **"El proyecto ya está en la etapa final (Cierre)."**.
5. Resize browser to mobile width. Timeline switches to vertical layout (Tabler default) and remains readable; badges, timestamps, and attribution lines don't clip.

---

## 4. Walkthrough 3 — Stage-gated actions

**What you're validating**: Story 3 — the Actions grid on the Lifecycle page flips card states (Locked → Available → Past) as the project advances, and the backend rejects out-of-stage attempts.
**Automated coverage**: `WalkthroughGatedActionsTests` — 2 passing (`FlipStatesOnAdvance`, `DirectUrlToLockedAction_RedirectsToLifecycleWithSpanishToast`) + 2 skipped (see § 7 known limitations).

### Steps

1. Open a project in **Registro** stage. Below the timeline card is the **"Acciones por Etapa"** grid with 6 cards:
   - Diagnósticos (gated on Formularios)
   - Corrección de Respuestas (gated on Análisis)
   - Asignaciones de Aprendizaje (gated on Asignación de Aprendizaje)
   - Mentorías (gated on Mentoría)
   - Evaluación Final (gated on Evaluación Final)
   - Cierre (gated on Cierre)
2. At Registration, all 6 cards should render with:
   - Muted grey border (`border-secondary`).
   - Lock icon.
   - Text "Disponible desde la etapa {stage name}".
   - A disabled "Bloqueada" button.
3. **Known bug**: hover doesn't produce a Bootstrap tooltip on locked cards in this build. This is tracked as T056b — the Razor view encodes attribute quotes wrong. Confirm the visual lock state is correct regardless.
4. Advance the project to **Formularios**. Reload the Lifecycle page. The **Diagnósticos** card should now be **Available** (blue border, primary "Abrir" button).
5. Advance to **Análisis**. Now:
   - **Diagnósticos** card is **Past** (green border, checkmark icon, "Ver historial" outline button — still clickable for historical review).
   - **Corrección de Respuestas** card is **Available**.
6. Click the "Ver historial" button on Diagnósticos — navigates to the existing Diagnóstico forms page. This confirms Past-state actions remain functional for audit.
7. Back on Lifecycle: click **Corrección de Respuestas**. You land on the existing answer-correction workflow (unchanged by this feature).
8. **Direct URL bypass test**: in a project still at **Registro**, paste `/Coordination/AnswerCorrection` directly in the URL bar (or any of the stage-gated routes). The `[RequiresStage]` filter intercepts, redirects you back to `/Coordination/Projects/Lifecycle/{externalId}`, and shows the red toast **"Esta acción estará disponible desde la etapa Análisis."**.

---

## 5. Walkthrough 4 — Role and scope enforcement

**What you're validating**: Parent spec's Edge Cases section (cross-incubator / no-context / wrong role).
**Automated coverage**: `WalkthroughRoleScopeTests` (3 tests).

### Steps

1. **Wrong role (Mentor)**: Log in as `mentor1@test.mentoory.com` / `Test123!@#`.
   - The **Coordinación → Proyectos** menu link is visible (Mentors are in the Coordinación group).
   - Click it. You should get a **403 Acceso denegado** (or be bounced to login) — the controller's `[Authorize(Roles="ProjectCoordinator,IncubatorAdmin,GlobalAdmin")]` rejects Mentor.
2. **Cross-incubator**: Log in as `incadmin1@test.mentoory.com` (Incubadora Alpha admin).
   - Note the external ID of a project in Incubadora Beta (e.g., *Proyecto Digital* — you can look it up via the Administration area or directly in the DB as an admin).
   - Paste `/Coordination/Projects/Lifecycle/{externalId of Proyecto Digital}` into the URL bar.
   - Confirm you get a **403** and the project's name does NOT appear in the rendered page (no data leak across incubators).
3. **No active incubator context**: Log in as `admin@mentoory.com` / `123abc987` (GlobalAdmin). During context selection, the only role available is `GlobalAdmin` with `IncubatorId=0` (global-scope sentinel).
   - Select the GlobalAdmin role and confirm. You land on the dashboard with no active incubator.
   - Navigate to `/Coordination/Projects` directly.
   - Confirm you're redirected to `/Context/Select` (the context selector).
   - **Known product gap**: the warning toast "Debe seleccionar una incubadora antes de continuar." is set in TempData but the `Select.cshtml` view doesn't render TempData warnings. Visually you'll see the selector without the warning. Tracked in `e2e/coverage-matrix.md` note C.

---

## 6. Walkthrough 5 — Audit trail + concurrency + inactive

**What you're validating**: SC-005 ("audit trail visible without navigation") plus two edge cases — concurrent advance and inactive project.
**Automated coverage**: `WalkthroughAuditConcurrencyTests` (3 tests).

### 6.1 Audit trail

1. Create a fresh test project in Incubadora Alpha, or use one of the `e2e-lifecycle-*` seed projects (they get wiped between test runs so expect them to be reset).
2. Log in as `coord1`. Open the project's Lifecycle page. Advance Registro → Formularios. Log out.
3. Log in as `coord2`. Open the same project. Advance Formularios → Análisis. Log out.
4. Log in as `coord3`. Open the same project. Advance Análisis → Asignación de Aprendizaje. Log out.
5. Log back in as `coord1`. Reopen the Lifecycle page. On the timeline confirm:
   - **Registro**: Completada. No "Avanzada por" line. (Registration was initialized by `Project.Create`, not by a user advance.)
   - **Formularios**: Completada. **Avanzada por: Ana Rodríguez**.
   - **Análisis**: Completada. **Avanzada por: Luis Paredes**.
   - **Asignación de Aprendizaje**: En progreso. **Avanzada por: Sofía Navarro**.

**Important semantic note**: `AdvancedByUserId` is stamped on the stage being *started* by the advance, not the stage being *completed*. So the attribution row shifts by one compared to a naive "who completed this stage?" reading. The audit trail still correctly shows three distinct coordinators involved in the project's progression on one view — no separate audit screen is needed (SC-005 satisfied).

### 6.2 Concurrent advance (lost race)

The UI has no explicit "race" button. To reproduce manually:

1. Open two browser windows (Firefox + Chrome, or two Chrome profiles). Log in as `coord1` in window A and `coord2` in window B.
2. Both open the same project's Lifecycle page (project in Registro).
3. Click **Avanzar Etapa** and the modal confirm button in **both windows as simultaneously as possible**.
4. Expected:
   - One window shows a green success toast ("Proyecto avanzado a Formularios.") and the project advances.
   - The other window shows a red toast: **"Otra operación modificó este proyecto. Actualice la página e intente de nuevo."**

This is EF Core's optimistic-concurrency check firing (via the `RowVersion` column). The automated test `ConcurrentAdvance_SecondAttemptShowsConcurrencyToast` fires two parallel POSTs via Playwright to trigger this deterministically — the manual reproduction depends on click timing.

### 6.3 Inactive project

1. Using the Administration area (or a DB update as admin), set an existing project's `IsActive` to `false`.
2. Log in as a coordinator with access to that project. Open its Lifecycle page.
3. Expected:
   - Status badge in the header reads **Inactivo** (grey).
   - Timeline is rendered but the **Avanzar Etapa** button is hidden.
   - Muted help text below the timeline: **"El proyecto está inactivo."**
4. **Bypass / defense-in-depth check**: if somehow a POST to `/Coordination/Projects/AdvanceStage/{id}` reaches the controller (hand-crafted request, stale tab, developer tool), the server responds with the red toast **"El proyecto está inactivo. Active el proyecto antes de avanzar de etapa."** The automated `InactiveProject_AdvanceAttemptShowsInactiveToast` test verifies both the visible UI state and the server-level rejection.

---

## 7. Running the automated E2E suite

### 7.1 Full solution

```bash
dotnet test /p:NuGetAudit=false
```

Expected:
- **Passed**: 462 (all unit + integration + E2E)
- **Skipped**: 2 — both in `WalkthroughGatedActionsTests` (see § 7.3)
- **Failed**: 0

Full-suite runtime on a typical dev box: ~4 minutes (~3.5 min is the E2E project; Testcontainers spins up a SQL Server container once per run).

### 7.2 Just the lifecycle E2E tests

```bash
dotnet test tests/Mentoory.Tests.E2E/Mentoory.Tests.E2E.csproj \
    --filter "FullyQualifiedName~Mentoory.Tests.E2E.Tests.Lifecycle" \
    /p:NuGetAudit=false
```

16 tests pass + 2 skipped. Runtime: ~45 seconds.

### 7.3 Known skipped tests (product bugs — not regressions)

| Test | Why skipped | Tracked in |
|---|---|---|
| `WalkthroughGatedActionsTests.GatedActions_RegistrationStage_AllSixCardsLocked` | Razor attribute-encoding bug in `Lifecycle.cshtml:145-147` — locked cards render malformed `aria-disabled` and never get tooltips. Tests assert correct ARIA state that doesn't render. | `e2e/open-questions.md` US3 §1; `tasks.md` T056b |
| `WalkthroughGatedActionsTests.GatedActions_LockedCardTooltipNamesUnlockingStage` | Same root cause. `data-bs-title` is truncated at the first whitespace, so tooltips never populate. | Same as above |

Both unblock when T056b is merged. The same commit that fixes the view should remove the `[Fact(Skip=...)]` attributes.

### 7.4 Step-through debugging a single test

```bash
PWDEBUG=1 dotnet test tests/Mentoory.Tests.E2E/Mentoory.Tests.E2E.csproj \
    --filter "FullyQualifiedName~WalkthroughAdvanceTests.Coordinator_AdvancesProjectFromRegistrationToForms" \
    /p:NuGetAudit=false
```

Playwright opens a headed browser with the Playwright Inspector attached. Use it to step through locators and confirm selectors match the rendered DOM.

Screenshots on failure land in `tests/Mentoory.Tests.E2E/bin/Debug/net10.0/screenshots/`.

---

## 8. Known limitations & deferred items

1. **Concurrency manual repro is timing-dependent.** The automated test bypasses timing by POSTing two requests in parallel from two Playwright contexts. Reproducing the lost race by hand may need several tries.
2. **Closure completion (7th advance) is unreachable through the UI.** `AdvanceProjectStageHandler.cs:48` explicitly rejects any advance at Closure. So "all 7 stages Completada" is unobtainable — Closure always renders as *En progreso* once reached. Documented in `e2e/open-questions.md`.
3. **Razor ARIA/tooltip encoding bug on locked action cards (T056b).** Locked cards render with malformed `aria-disabled`/`tabindex`/`data-bs-title` values (Razor HTML-encodes the inner quotes). Real accessibility + UX regression that this feature did not introduce — discovered during C3 E2E work. Fix is straightforward (replace attribute-blob with conditional individual attributes).
4. **TempData warning not rendered on `Context/Select.cshtml`.** The coordinator controller sets `TempData[WarningMessage] = "Debe seleccionar una incubadora antes de continuar."` when it redirects for a missing context, but the context-selector view doesn't render warnings. Tracked as a follow-up product fix.
5. **E2E test runtime sensitivity.** Test suite runtime is dominated by Testcontainers startup (~15–20s per run). Subsequent tests share the container. If tests hang, check `docker info` first.

---

## 9. Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| App won't start via Aspire | Docker isn't running | Start Docker, retry |
| `MentooryDb.dacpac not found` during test | Stale DACPAC | `cd Mentoory.Db && ./publish-mentoorydb.sh` |
| Login fails with seeded passwords | DACPAC didn't redeploy seed data | Rerun step 1.2, then rerun app/tests |
| "Avanzar Etapa" button missing on Registro project | Project is inactive, OR `CurrentStageState` was manually tampered with (not `InProgress`) | Check the `tenant.Projects` row's `IsActive` and `CurrentStageState` columns |
| Seed test projects look pre-advanced | Previous manual walkthrough consumed them | Re-seed by dropping `MentooryDb` and redeploying DACPAC, OR use a new project created via the Administration area |
| Spanish literal mismatch | Product UI string changed | Flag — the automated tests hold exact literals per constitution §IX; a new string means spec drift |
| Playwright test intermittently flaky | Network-idle wait races with redirect chain | Rerun once; consistent flake → report as `e2e/open-questions.md` entry |

---

## 10. Reference: where assertions live

If you want to confirm what the automation checks at each step:

| Walkthrough | Test class | Path |
|---|---|---|
| 1 — Advance | `WalkthroughAdvanceTests` | `tests/Mentoory.Tests.E2E/Tests/Lifecycle/WalkthroughAdvanceTests.cs` |
| 2 — Lifecycle page | `WalkthroughLifecyclePageTests` | `tests/Mentoory.Tests.E2E/Tests/Lifecycle/WalkthroughLifecyclePageTests.cs` |
| 3 — Gated actions | `WalkthroughGatedActionsTests` | `tests/Mentoory.Tests.E2E/Tests/Lifecycle/WalkthroughGatedActionsTests.cs` |
| 4 — Role/scope | `WalkthroughRoleScopeTests` | `tests/Mentoory.Tests.E2E/Tests/Lifecycle/WalkthroughRoleScopeTests.cs` |
| 5 — Audit/concurrency/inactive | `WalkthroughAuditConcurrencyTests` | `tests/Mentoory.Tests.E2E/Tests/Lifecycle/WalkthroughAuditConcurrencyTests.cs` |
| Fixtures smoke test | `LifecycleSmokeTests` | `tests/Mentoory.Tests.E2E/Tests/Lifecycle/LifecycleSmokeTests.cs` |

Shared infrastructure (fixtures, login helpers, page objects):

- `tests/Mentoory.Tests.E2E/Infrastructure/Lifecycle/LifecycleFixtures.cs` — seed helpers, state-reset logic.
- `tests/Mentoory.Tests.E2E/Infrastructure/Lifecycle/LifecycleLoginHelpers.cs` — `AsCoordinatorAsync`, `AsMentorAsync`, `AsIncubatorAdminAsync`, `AsGlobalAdminAsync`.
- `tests/Mentoory.Tests.E2E/Infrastructure/Lifecycle/LifecyclePageObject.cs` — reads stage state/timestamps, reads toasts, posts advance.
- `tests/Mentoory.Tests.E2E/Infrastructure/Lifecycle/CoordinationProjectsPageObject.cs` — the projects list page.

Spec-level references:

- `specs/016-project-lifecycle-finish/spec.md` — parent feature spec.
- `specs/016-project-lifecycle-finish/e2e/spec.md` — E2E sub-feature spec.
- `specs/016-project-lifecycle-finish/e2e/coverage-matrix.md` — scenario-to-test mapping (100% complete).
- `specs/016-project-lifecycle-finish/e2e/open-questions.md` — product bugs and unreachable-as-specified scenarios surfaced during E2E work.
