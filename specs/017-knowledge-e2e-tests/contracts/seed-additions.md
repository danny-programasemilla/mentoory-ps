# Contract: PostDeployment Seed Additions

> **Resolution (T002, 2026-04-19)** — Template-side questions live in `diagnostic.QuestionTemplates`,
> NOT in `diagnostic.Questions`. `QuestionTemplates.TopicId` is a `BIGINT NOT NULL` with **no FK**
> (soft reference to `knowledge.TopicTemplates.Id`, enforced in application code only).
> `diagnostic.Questions.TopicId` has `FK_Questions_Topics → knowledge.Topics(Id)` and is the
> project-side topic reference rewritten by `CloneFormTemplateHandler.BuildTopicIdRewriteMap`.
> **Seed implication for Addition 2**: the FormTemplate's questions are inserted into
> `diagnostic.QuestionTemplates` with `TopicId` values resolved from `knowledge.TopicTemplates`
> under the seeded `Emprendimiento Básico` KS template (e.g., `Finanzas` / `Mercadeo` topic
> templates under module template `Ideación`). No rows go into `diagnostic.Questions` at seed
> time — those are created only when a coordinator clones the FormTemplate into a project via
> `CloneFormTemplateCommand`.

**Files modified**:
- `Mentoory.Db.PostDeployment/004.SeedTestData.sql` — add second incubator + `coord2` user
- `Mentoory.Db.PostDeployment/005.SeedKnowledgeData.sql` — add one bound `FormTemplate`

All additions MUST follow the **idempotent INSERT guard** pattern already in use:

```sql
IF NOT EXISTS (SELECT 1 FROM {table} WHERE {unique_key} = {value})
BEGIN
    INSERT INTO {table} (...) VALUES (...)
END
```

Scripts run on every DACPAC publish; duplicate runs MUST NOT produce duplicate rows.

---

## Addition 1 — Second Incubator + Second Coordinator (for US6-3 tenant isolation)

**File**: `Mentoory.Db.PostDeployment/004.SeedTestData.sql`

**Entities to add**:
1. `tenant.Incubators` row: `Incubadora Norte` (external id: fixed Guid `22222222-2222-2222-2222-222222222222`).
2. `access.Users` row: `coordnorte@test.mentoory.com` with password hash matching `Test123!@#` (same hashing fn the existing seed uses).
3. `access.RoleAssignments` row: `(userId: coord2, roleId: ProjectCoordinator, incubatorId: Incubadora Norte, projectId: NULL)`.
4. `tenant.Projects` row: `Proyecto Norte Uno` in Incubadora Norte, bound to seeded KS template (existing `Emprendimiento Básico`). `IKnowledgeStructureProvisioner` at seed time materializes its `knowledge.KnowledgeStructures` row.
5. Optionally: assign `coord2` to `Proyecto Norte Uno` via a new `access.RoleAssignments` row with both `incubatorId` and `projectId` set.

**Verification** (after DACPAC publish):
- `SELECT COUNT(*) FROM tenant.Incubators WHERE Name = 'Incubadora Norte'` → 1
- `SELECT COUNT(*) FROM access.Users WHERE Email = 'coordnorte@test.mentoory.com'` → 1
- Login as `coord2` / `Test123!@#` → lands on `/Context/Select` with only Incubadora Norte visible.

**Existing patterns to mirror**: the `multirole` user insert in `004.SeedTestData.sql` (§ 4) is the reference implementation. The new user block copies it, substituting email + Guid.

---

## Addition 2 — Bound FormTemplate (for US3-1 cascade happy path)

**File**: `Mentoory.Db.PostDeployment/005.SeedKnowledgeData.sql`

**Entities to add**:
1. `diagnostic.FormTemplates` row:
   - `Name`: `Diagnóstico Básico de Emprendimiento`
   - `Description`: `Formulario de diagnóstico vinculado a la plantilla Emprendimiento Básico`
   - `ExternalId`: fixed Guid `33333333-3333-3333-3333-333333333333`
   - `DefaultKnowledgeStructureTemplateExternalId`: matches the seeded `Emprendimiento Básico` KS template's ExternalId.
   - `CreatedAt`: `SYSUTCDATETIME()`
2. `diagnostic.QuestionTemplates` rows (2–3 questions; **not** `Questions` — see T002 resolution above):
   - Each question's `TopicId` (INTERNAL `BIGINT`) resolves to a TopicTemplate row under the seeded KS template (e.g., the `Finanzas` and `Mercadeo` topic templates under module template `Ideación`).
   - `QuestionText`: Spanish question text (`"¿Cómo describe el flujo de caja actual de su proyecto?"`, etc.).
   - `QuestionType`: `0` (Text) or `1` (Numeric).
   - `StageApplicability`: `2` (Both).
   - `SortOrder`: 1, 2, 3.
   - `IsOptional`: `0`.

**Verification**:
- `SELECT COUNT(*) FROM diagnostic.FormTemplates WHERE ExternalId = '33333333-...'` → 1
- `SELECT DefaultKnowledgeStructureTemplateExternalId FROM diagnostic.FormTemplates WHERE ExternalId = '33333333-...'` → matches the seeded KS template ExternalId.
- Every seeded `QuestionTemplates.TopicId` resolves through `JOIN knowledge.TopicTemplates tt ON tt.Id = qt.TopicId`. `QuestionTemplates.TopicId` has no DB-level FK, but application code + `CloneFormTemplateHandler.BuildTopicIdRewriteMap` expect the value to be a live `TopicTemplates.Id`.

---

## Addition 3 — Helper-callable state verification (no SQL change, but exposed as helpers)

**File**: `tests/Mentoory.Tests.E2E/Infrastructure/KnowledgeIntegrationHelpers.cs` (see `contracts/test-files.md`).

These are NOT SQL seed additions but are listed here for completeness — test assertions that query the DB go through these helpers, not through raw SQL:

- `GetSeededKsTemplateExternalIdAsync()` — returns the `Emprendimiento Básico` template's Guid for theory parameterization.
- `GetSeededBoundFormTemplateExternalIdAsync()` — returns the new `Diagnóstico Básico de Emprendimiento` template's Guid (Addition 2).
- `CountProjectFormsAsync(projectId)` — returns the number of ProjectForm rows under a project (used by US3 scenario 3 to assert no new row on null-binding cascade).
- `CountProjectKnowledgeStructuresAsync(projectId)` — returns the KS rowcount for a project (used by US3-4 assertion of UNIQUE constraint).

---

## DACPAC Publish Verification

After modifying the seed scripts:

```bash
cd /mnt/D/repos/mentoory-ps-knowledge-module/Mentoory.Db
./publish-mentoorydb.sh    # existing project convention
```

Expected output: `Publish succeeded.` with zero errors. The PostDeployment scripts run idempotently; a second publish MUST produce the same rowcount.

Local E2E suite smoke:

```bash
dotnet test tests/Mentoory.Tests.E2E/Mentoory.Tests.E2E.csproj \
    --filter "FullyQualifiedName~KnowledgeTemplatesTests.Templates_PageLoads"
```

If this passes on a clean container, the seed changes haven't broken the existing happy path.
