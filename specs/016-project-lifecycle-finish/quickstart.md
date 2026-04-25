# Quickstart: Project Lifecycle Application + UI Completion

**Feature**: 016-project-lifecycle-finish

This document is the human walkthrough of the feature once implemented — useful for QA, code review sign-off, and onboarding. Paths are relative to the repo root.

## Prerequisites (dev loop)

1. SQL Server reachable (Aspire spins this up). Build the database: `cd Mentoory.Db && ./publish-mentoorydb.sh`. The new `RowVersion` column on `tenant.Projects` is created as part of this build.
2. Seed data populated by the existing PostDeployment scripts (e.g., a sample incubator + project in stage `Registration`).
3. A test user in one of: `ProjectCoordinator`, `IncubatorAdmin`, or `GlobalAdmin`, associated with the incubator that owns the seed project.
4. Run the full stack: `dotnet run --project Mentoory.Aspire.AppHost`.

## Walkthrough 1 — Advance a project (Story 1, P1)

1. Sign in as `ProjectCoordinator`.
2. The left sidebar shows **Coordinación → Proyectos** (a new menu entry). Click it.
3. The list shows all projects in the active incubator. The seed project appears with current stage *Registro*.
4. Click the project row → navigates to `/Coordination/Projects/Lifecycle/{externalId}`.
5. On the Lifecycle page:
   - Header shows project name and a Spanish badge "En progreso — Registro".
   - The 7-stage timeline shows Registro highlighted as *en progreso*, all others *no iniciada*.
   - An **Avanzar Etapa** button is visible.
6. Click **Avanzar Etapa**. A confirmation modal appears: "¿Confirma avanzar de *Registro* a *Formularios*?".
7. Confirm. The page reloads. A green toast appears: "Proyecto avanzado a Formularios.".
8. Registro is now shown as *completada* with a completion timestamp and the coordinator's name under "Avanzada por". Formularios is shown as *en progreso* with a start timestamp.

**Expected failure flows** (same walkthrough path):

- If a second coordinator attempts to advance simultaneously, one call succeeds and the other shows the red toast "Otra operación modificó este proyecto. Actualice la página e intente de nuevo." (`LifecycleConcurrencyConflict`).
- If the project is inactive, the advance button is hidden and a muted help text reads "El proyecto está inactivo.".
- At stage Closure the advance button is hidden and the help text reads "El proyecto ya está en la etapa final (Cierre).".

## Walkthrough 2 — Inspect the full lifecycle (Story 2, P2)

1. From the Projects list, open any project (Story 1's seed project, or any other).
2. Verify the Lifecycle page renders:
   - Project identity (name, description, incubator name).
   - The full seven-stage timeline in canonical order (Registro, Formularios, Análisis, Asignación de Aprendizaje, Mentoría, Evaluación Final, Cierre).
   - Each stage shows its state with the correct color and icon (empty circle / spinner / checkmark).
   - Stages that have been started show the start timestamp; completed stages also show the completion timestamp and coordinator name.
3. Resize the browser to mobile width. The timeline switches to vertical layout (Tabler defaults) and remains readable.

## Walkthrough 3 — Stage-gated actions (Story 3, P3)

1. Open a project in stage *Registro*.
2. The Lifecycle page's Actions grid shows six cards. All six are in the **Locked** visual state (muted, lock icon), each showing the Spanish text "Disponible desde la etapa *Formularios/Análisis/...*".
3. Hover over a locked card. A Bootstrap tooltip confirms the unlocking stage name.
4. Attempt to open the stage-gated action by direct URL, e.g., `/Coordination/AnswerCorrection`. The app redirects back to `/Coordination/Projects/Lifecycle/{externalId}` with an error toast: "Esta acción estará disponible desde la etapa Análisis.".
5. Advance the project through stages until it reaches *Análisis*.
6. Reload the Lifecycle page. The **Corrección de Respuestas** card is now in the **Available** state (primary color, clickable). Earlier-stage actions (Diagnósticos under Formularios) are in the **Past** state (muted with a check badge, still clickable for historical review).
7. Click **Corrección de Respuestas**. The existing answer-correction workflow opens normally.

## Walkthrough 4 — Role and scope enforcement

1. Sign in as a `Mentor` (who is not a Coordinator in this incubator). `/Coordination/Projects` still appears (Mentors are in the Coordinación menu group) but the `ProjectsController.Index` returns a 403 because Mentor is not in the controller's role list `"ProjectCoordinator,IncubatorAdmin,GlobalAdmin"`. Confirm the platform's 403 page renders in Spanish.
2. Sign in as `IncubatorAdmin` of incubator A. Attempt `/Coordination/Projects/Lifecycle/{externalId of a project in incubator B}`. The handler returns `ProjectOutOfScope`; the controller responds with `403`. Confirm no data from the other incubator leaks.
3. Sign in as `GlobalAdmin` in global scope (no incubator selected). Navigate to Coordination → Proyectos. The page offers to select a context before listing, consistent with existing coordination pages. Selecting an incubator enables the flow.

## Walkthrough 5 — Audit trail (SC-005)

1. Advance a project through 3 stages, each advance by a different test coordinator.
2. Reload the Lifecycle page. The three completed stages each show their coordinator's name and timestamps. Confirm no navigation to a separate audit screen is required to see this.

## Tests to run

```bash
dotnet test --filter "FullyQualifiedName~AdvanceProjectStage"
dotnet test --filter "FullyQualifiedName~GetProjectLifecycle"
dotnet test --filter "FullyQualifiedName~RequiresStage"
dotnet test --filter "FullyQualifiedName~ProjectTests"
```

## Validation of success criteria

| SC   | How to validate                                                                 |
|------|----------------------------------------------------------------------------------|
| SC-001 | All seed projects are advanceable via UI — no tool/console usage needed.        |
| SC-002 | Time the walkthrough 1 advance from page open to toast — must be < 30 s.        |
| SC-003 | Show an unfamiliar coordinator the Lifecycle page; they correctly identify current stage and next action within 10 s. |
| SC-004 | For each stage 0..6, attempt every stage-gated action direct URL; all non-matching stages reject server-side. |
| SC-005 | Three advances by three users produce a visible audit trail on the same page.   |
| SC-006 | Show a coordinator the locked-actions view; 95% correctly identify unlocking stage from tooltip. |
| SC-007 | Post-release support dashboard: zero tickets about "cannot advance my project". |
