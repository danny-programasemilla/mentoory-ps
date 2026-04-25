# Quickstart: Knowledge Module Core

**Feature**: 016-knowledge-module-core
**Audience**: developers picking up the implementation; QA walking through end-to-end; future reviewers verifying behavior.

This walkthrough exercises every user story in spec.md against a freshly-seeded local environment. It does NOT cover every CRUD operation — that's in the integration-test suite. It covers the golden path + two of the highest-signal edge cases (diagnostic cascade + PartialSync).

---

## Prerequisites

1. Repo cloned and dependencies restored (`dotnet restore`).
2. SQL Server reachable at the connection string defined in `appsettings.Development.json`.
3. Aspire AppHost running: `dotnet run --project Mentoory.Aspire.AppHost`.
4. Database published with the new SSDT artifacts: `cd Mentoory.Db && publish-mentoorydb.sh`. This deploys `knowledge.*` tables, the new Diagnostic FKs, and runs PostDeployment seed `005.SeedKnowledgeData.sql`.
5. Authentication wired so you can log in as each role. Seed accounts (per existing `003.SeedAccessData.sql`):
   - `globaladmin@mentoory.local` (GlobalAdmin)
   - `coordinator@mentoory.local` (ProjectCoordinator, tied to an Incubator + Project)

---

## US1 — Global admin curates the knowledge catalog

**Goal**: verify template CRUD, priority ranges, archival.

1. Log in as `globaladmin@mentoory.local`. Navigate to `/Coordination/Knowledge/Templates`.
2. Click **Nueva plantilla**. Enter:
   - Name: `Emprendimiento Básico`
   - Description: `Ruta de aprendizaje para etapas tempranas.`
3. Save. **Expect**: toast "Plantilla creada."; template appears in list with `IsArchived = false`.
4. Open the template. Click **Agregar módulo** twice:
   - M1: `Ideación`
   - M2: `Validación`
5. Under M1, add two topics:
   - T1.1: `Propuesta de valor` — priority bands High (8.00–10.00), Medium (5.00–7.99), Low (0.00–4.99)
   - T1.2: `Segmento de clientes` — priority bands High (8.00–10.00) only, leave Medium/Low empty
6. For T1.1, open the priority-range editor and try to save with High (7.00–10.00) and Medium (6.00–8.00). **Expect**: field-level error "Las bandas se solapan." No save persists.
7. Correct to non-overlapping. Save.
8. Add 2 subjects under T1.1, and under one subject add 3 resources: one Video (YouTube URL), one Link, one File (URL to a PDF on Drive).
9. Reorder M1 and M2 via drag handles. **Expect**: sort order persists on reload.
10. **Archive** the template. **Expect**: template disappears from the default list. Toggle **Mostrar archivados**. **Expect**: template reappears. Unarchive.
11. Try to **delete** the template. (No clones yet.) **Expect**: delete succeeds. Recreate the template before continuing (use step 1–8, keep it short).

**Validation**:
- Row in `knowledge.KnowledgeStructureTemplates` with `IsArchived = 0`, `Version >= 5` (bumped on each descendant change).
- Nested rows in `ModuleTemplates`, `TopicTemplates`, `SubjectTemplates`, `ResourceTemplates`.

---

## US2 — Coordinator clones a template and customizes

**Goal**: verify clone creates an independent project structure; edits on clone don't affect template.

1. Log in as `coordinator@mentoory.local`. Ensure active project selected (via `/Context/Select`).
2. Navigate to `/Coordination/Knowledge/Projects`. Click **Clonar desde plantilla**.
3. Pick `Emprendimiento Básico`. **Expect**: new row appears under "Estructuras del proyecto" with `SyncMode = Desconectado` and `Versión de origen = <current template version>`.
4. Open the clone detail. **Expect**: full tree (2 modules, 2 topics, 2 subjects, 3 resources) mirrors template.
5. Rename T1.1 from `Propuesta de valor` to `Propuesta de valor – versión proyecto`. Save.
6. Under M2 `Validación`, add a **clone-only** topic `T-local: Entrevistas`. **Expect**: saves successfully; in `knowledge.Topics` table, this new row has `SourceTemplateTopicExternalId IS NULL`.
7. Edit T1.1 priority ranges on the clone: change High to (9.00–10.00). Save. **Expect**: saves succeed. Template's T1.1 in `knowledge.TopicTemplates` UNCHANGED.
8. Try to delete T1.1 on the clone. (No diagnostic questions yet reference it.) **Expect**: delete succeeds. **Undo** (re-add if you care for US3).

**Validation**:
- `knowledge.KnowledgeStructures` row exists with `ProjectId = <your project>`, `SourceTemplateId = template.Id`, `SyncMode = 0`.
- Every cloned descendant has a non-null `SourceTemplateXExternalId` matching the template side; the locally-added T-local has null.

---

## US4 — Priority range edit emits TopicPriorityRangesChanged

**Goal**: verify event publication on project-side edits only.

Prereqs: US2 complete.

1. Attach a debugger or subscribe via a temporary diagnostic `INotificationHandler<TopicPriorityRangesChanged>` registered in a test profile (or use the integration-test assertion).
2. As coordinator, change T1.1 priority ranges on the project clone again: High (9.50–10.00). Save.
3. **Expect**: one `TopicPriorityRangesChanged` notification published with:
   - `TopicExternalId = <T1.1 clone ExternalId>`
   - `ProjectId = <your project id>`
   - `HighRange = { Min = 9.50m, Max = 10.00m }`
   - `MediumRange`, `LowRange` = their current values (whatever you configured).
4. As global admin, edit T1.1 ranges on the **template** (not the clone). **Expect**: NO `TopicPriorityRangesChanged` event published.

---

## US3 — Diagnostic form clone cascades

**Goal**: verify the cross-module cascade; `Question.TopicId` rewritten to per-project topic ids.

Prereqs: a form template already bound to `Emprendimiento Básico` knowledge template with at least one question per template topic. The seed script or test fixtures should provide this; otherwise:

1. Log in as GlobalAdmin. Go to Diagnostic Forms (existing controller).
2. Open an existing FormTemplate (or create one). Set **Plantilla de conocimiento asociada** to `Emprendimiento Básico`. Save.
3. Ensure at least one question exists whose `TopicId` points at a topic inside `Emprendimiento Básico` (template-side).
4. Switch to Coordinator account. Navigate to Diagnostic Forms.
5. **Clone** the form template into your active project.
6. **Expect**:
   - A new `ProjectForm` exists for your project.
   - If you did NOT already have a `KnowledgeStructure` clone of `Emprendimiento Básico`, one is created now. (If you did, it is REUSED per EC-22.)
   - Open `SELECT * FROM diagnostic.Questions WHERE ProjectFormId = <newProjectFormId>` and check `TopicId`. Every value MUST resolve to a row in `knowledge.Topics` owned by your project (verify via join: `JOIN knowledge.Topics t ON q.TopicId = t.Id JOIN knowledge.Modules m ON t.ModuleId = m.Id JOIN knowledge.KnowledgeStructures ks ON m.KnowledgeStructureId = ks.Id WHERE ks.ProjectId = <yourProject>`).
7. To verify rollback (FR-K23): contrive a form template with a question whose `TopicId` does NOT belong to the bound knowledge template (temporarily point it at a template topic from a different `KnowledgeStructureTemplate`). Clone. **Expect**: command fails with a clear Spanish error naming the offending question; no `KnowledgeStructure` or `ProjectForm` rows were created (check via count before/after).

**Validation**:
- Query: `SELECT COUNT(*) FROM diagnostic.Questions q JOIN knowledge.Topics t ON q.TopicId = t.Id WHERE q.ProjectFormId = @new` matches the total question count of the new form.
- No cross-tenant rows: every `knowledge.Topics` row reached has `Module.KnowledgeStructureId → KnowledgeStructure.ProjectId = @yourProject`.

---

## US5 — PartialSync pulls new template items

**Goal**: verify sync appends new items without touching local edits.

Prereqs: US2 complete; clone is `Disconnected` by default.

1. As coordinator, toggle the clone's sync mode to **Sincronización parcial**.
2. As GlobalAdmin, on the template `Emprendimiento Básico`:
   - Under M1 `Ideación`, add a brand-new topic T1.3 `Recursos iniciales` with priority bands.
   - Under T1.1, add a new subject `Herramientas recomendadas` with one Resource.
   - Bump `Version` automatically (the aggregate does this).
3. As coordinator, return to the clone detail. Click **Sincronizar desde plantilla**.
4. **Expect** the summary toast: `5 elementos agregados (1 tema, 1 asunto, 3 recursos)` — or whatever matches (1 topic + 1 subject + however many resources you added on the template).
5. Verify on the clone:
   - T1.3 appears under M1 at `SortOrder = max+1` (positioned AFTER the existing topics including T-local), with `SourceTemplateTopicExternalId` stamped matching the template T1.3's `ExternalId`.
   - The new subject appears under T1.1 on the clone.
   - Your earlier rename of T1.1 (`Propuesta de valor – versión proyecto`) is PRESERVED.
   - T-local (the clone-only topic from US2 step 6) is UNCHANGED.
6. Return to clone, toggle sync mode back to **Desconectado**. Try to trigger sync again. **Expect**: action disabled / command returns failure.

**Validation**:
- `SourceTemplateVersion` on the clone equals the template's current `Version` after sync.
- For every appended item, `SourceTemplateXExternalId IS NOT NULL`.

---

## Deletion safety (EC-30)

1. As coordinator, navigate to the cloned structure.
2. Confirm T1.1 on the clone is referenced by at least one question from the cloned ProjectForm (from US3).
3. Try to delete T1.1. **Expect**: error toast "No se puede eliminar el tema: 1 pregunta(s) de diagnóstico lo referencian. Reasigne o elimine las preguntas primero." No row deleted.
4. Delete the referencing question from the ProjectForm (via the existing Diagnostic UI). Retry topic delete. **Expect**: succeeds.

---

## Template-level delete with existing clones (EC-01)

1. As GlobalAdmin, go back to `Emprendimiento Básico`. Try to **delete** it (not archive).
2. **Expect**: blocked with error "No se puede eliminar: la plantilla tiene clones en uso por 1 proyecto(s). Archívela en su lugar."
3. Archive instead. Verify the template is hidden from the clone picker by default.

---

## Success criteria traceability

| Quickstart step | Spec SC |
|---|---|
| US2 (entire flow timed) | SC-K01 |
| US1 steps 4–8, US2 steps 4–7, US5 | SC-K02 |
| US2 step 7 | SC-K03 |
| US3 step 6 validation query | SC-K04 |
| US5 step 5 | SC-K05 |
| US4 steps 2–3 | SC-K06 |

---

## Smoke-test checklist (5 minutes)

If pressed for time, the fastest sanity check:

1. Database publishes without FK errors.
2. `/Coordination/Knowledge/Templates` loads (GlobalAdmin).
3. `/Coordination/Knowledge/Projects` loads (Coordinator).
4. Clone the seeded sample template; verify tree loads.
5. Clone a seeded bound FormTemplate; verify resulting Questions' TopicIds all belong to the new project's KnowledgeStructure.
