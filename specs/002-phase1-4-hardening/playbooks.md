# Manual Validation Playbooks: Phase 1-4 Hardening

**Purpose**: Step-by-step verification scripts for non-developer validators.
**Prerequisites**: Application running with seed data from `004.SeedTestData.sql`.

---

## Playbook 1: Login Routing by Role

**Goal**: Verify each role lands on the correct dashboard after login.

| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Log in as `admin@mentoory.com` / `123abc987` | Redirected to context selection | |
| 2 | Select GlobalAdmin context | Lands on `/Platform/Incubators` (Platform dashboard) | |
| 3 | Log out. Log in as `incadmin1@test.mentoory.com` / `Test123!@#` | Redirected to context selection | |
| 4 | Select IncubatorAdmin context (Incubadora Alpha) | Lands on `/Administration/Dashboard` | |
| 5 | Log out. Log in as `coord1@test.mentoory.com` / `Test123!@#` | Redirected to context selection | |
| 6 | Select ProjectCoordinator context | Lands on `/Coordination/Diagnostics` | |
| 7 | Log out. Log in as `mentor1@test.mentoory.com` / `Test123!@#` | Redirected to context selection | |
| 8 | Select Mentor context | Lands on `/Coordination/Diagnostics` | |
| 9 | Log out. Log in as `entrepreneur1@test.mentoory.com` / `Test123!@#` | Redirected to context selection | |
| 10 | Select Entrepreneur context | Lands on `/Participant/Diagnostic` | |
| 11 | Log out. Log in as `sponsor1@test.mentoory.com` / `Test123!@#` | Redirected to context selection | |
| 12 | Select Sponsor context | Lands on `/Platform/Sponsor` with "Proximamente" message | |

---

## Playbook 2: Context Selection Flow

**Goal**: Verify context selection follows proper flow and supports return URLs.

| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Log in as `multirole@test.mentoory.com` / `Test123!@#` | Context selection screen shows 2 options (IncubatorAdmin, ProjectCoordinator) | |
| 2 | Select IncubatorAdmin context | Lands on Administration Dashboard | |
| 3 | Navigate directly to `/Administration/Projects` | Projects page loads (context is set) | |
| 4 | Log out. Navigate directly to `/Administration/Projects` | Redirected to login page | |
| 5 | Log in, then navigate to `/Context/Select?returnUrl=/Administration/Projects` | After context selection, redirected back to `/Administration/Projects` | |

---

## Playbook 3: Context Switching

**Goal**: Verify context switching works from the top bar.

| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Log in as `multirole@test.mentoory.com`, select IncubatorAdmin | Top bar shows: badge "IncubatorAdmin", incubator name "Incubadora Alpha" | |
| 2 | Click "Cambiar contexto" in top bar | Redirected to context selection screen | |
| 3 | Select ProjectCoordinator context | Top bar updates to show "ProjectCoordinator", incubator, and project name | |
| 4 | While on `/Administration/Dashboard`, switch to a role without admin access | Redirected to the appropriate dashboard (not stuck on admin page) | |

---

## Playbook 4: Authorization Boundaries

**Goal**: Verify role-based access control is enforced.

| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Log in as `entrepreneur1@test.mentoory.com`, select context | Can access `/Participant/Diagnostic` | |
| 2 | Navigate directly to `/Administration/Dashboard` | Access denied (redirect to login or 403) | |
| 3 | Navigate directly to `/Platform/Incubators` | Access denied | |
| 4 | Log in as `incadmin1@test.mentoory.com`, select Incubadora Alpha context | Can access `/Administration/Dashboard` and `/Administration/Projects` | |
| 5 | Navigate directly to `/Platform/Incubators` | Access denied (only GlobalAdmin) | |

---

## Playbook 5: Diagnostic Workflow

**Goal**: Verify full diagnostic lifecycle with correct context enforcement.

**Precondition**: Log in as `coord1@test.mentoory.com`, select ProjectCoordinator context (Proyecto Innovacion).

| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Navigate to `/Coordination/Diagnostics` | Lists project forms for Proyecto Innovacion only | |
| 2 | Click "Clonar" to clone a form template | Clone form appears with template options | |
| 3 | Select a template and clone | Success message. New form appears in list | |
| 4 | Click on a form to view details | Form details page shows questions and answer options | |
| 5 | Log out. Log in as `entrepreneur1@test.mentoory.com`, select context | Entrepreneur context active for Proyecto Innovacion | |
| 6 | Navigate to `/Participant/Diagnostic?formExternalId={guid}` | Diagnostic form loads with questions | |
| 7 | Fill in responses and submit | Success message "Diagnostico enviado exitosamente" | |
| 8 | Log out. Log in as `mentor1@test.mentoory.com`, select Mentor context | Mentor context active | |
| 9 | Navigate to answer correction for the submitted response | Response details load correctly | |
| 10 | Correct an answer | Success message "Respuesta corregida exitosamente" | |

---

## Playbook 6: Cross-Project Isolation

**Goal**: Verify no cross-project data leakage through URL manipulation.

**Precondition**: Note the ExternalId (GUID) of a form in Project 1.

| Step | Action | Expected Result | Pass/Fail |
|------|--------|-----------------|-----------|
| 1 | Log in as `coord2@test.mentoory.com`, select Project 2 context | Active context is Proyecto Sostenibilidad | |
| 2 | Navigate to `/Coordination/Diagnostics/{Project1-form-GUID}` | Returns null/NotFound (form does not belong to Project 2) | |
| 3 | Log in as `entrepreneur2@test.mentoory.com`, select Project 3 context | Active context is Proyecto Digital (Incubadora Beta) | |
| 4 | Try to access a form from Incubadora Alpha by GUID | Returns null/NotFound (cross-incubator isolation) | |

---

## Overall Pass Criteria

- All 6 playbooks pass with zero failures
- No cross-project data visible at any point
- All user-facing text is in Spanish
- Dashboard loads within 3 seconds for each role
