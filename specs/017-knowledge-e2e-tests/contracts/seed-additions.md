# Contract: PostDeployment Seed Additions

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
2. `access.Users` row: `coord2@test.mentoory.com` with password hash matching `Test123!@#` (same hashing fn the existing seed uses).
3. `access.RoleAssignments` row: `(userId: coord2, roleId: ProjectCoordinator, incubatorId: Incubadora Norte, projectId: NULL)`.
4. `tenant.Projects` row: `Proyecto Norte Uno` in Incubadora Norte, bound to seeded KS template (existing `Emprendimiento Básico`). `IKnowledgeStructureProvisioner` at seed time materializes its `knowledge.KnowledgeStructures` row.
5. Optionally: assign `coord2` to `Proyecto Norte Uno` via a new `access.RoleAssignments` row with both `incubatorId` and `projectId` set.

**Verification** (after DACPAC publish):
- `SELECT COUNT(*) FROM tenant.Incubators WHERE Name = 'Incubadora Norte'` → 1
- `SELECT COUNT(*) FROM access.Users WHERE Email = 'coord2@test.mentoory.com'` → 1
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
2. `diagnostic.Questions` rows (2–3 questions):
   - Each question's `TopicId` (INTERNAL `BIGINT`) resolves to a Topic row under the seeded KS template's hierarchy (e.g., the `Finanzas` and `Mercadeo` topics under module `Ideación`).
   - `Text`: Spanish question text (`"¿Cómo describe el flujo de caja actual de su proyecto?"`, etc.).
   - `QuestionType`: `0` (Text) or `1` (Numeric).
   - `StageApplicability`: `2` (Both).
   - `SortOrder`: 1, 2, 3.
   - `IsRequired`: `0`.

**Verification**:
- `SELECT COUNT(*) FROM diagnostic.FormTemplates WHERE ExternalId = '33333333-...'` → 1
- `SELECT DefaultKnowledgeStructureTemplateExternalId FROM diagnostic.FormTemplates WHERE ExternalId = '33333333-...'` → matches the seeded KS template ExternalId.
- Every seeded Question's `TopicId` resolves through `JOIN knowledge.TopicTemplates tt ON tt.Id = q.TopicId` (NOT `Topics` — FormTemplate-side questions reference TEMPLATE topics per the ratified decision).

**Wait — TopicId on template-side questions**: The spec clarifies that `diagnostic.Questions.TopicId` on **template-side** questions points at `knowledge.TopicTemplates.Id` (template topics, NOT project topics). The `CloneFormTemplateHandler` rewrites these at clone time. The seed must honor this: query against `knowledge.TopicTemplates`, not `knowledge.Topics`.

Re-check the existing `diagnostic.Questions.TopicId` FK: the 016 branch adds the constraint `FK_diagnostic_Questions_knowledge_Topics`. **But the template-side questions reference TopicTemplates, not Topics**. This suggests either:
- The FK is ONLY enforced on `ProjectForm.Questions` (via a filtered/conditional constraint), OR
- The FK is a union/polymorphic constraint (both tables), OR
- The 016 amendment replaces the FK with a soft-reference pattern.

**Action item**: Before writing this seed, verify with the 016 plan's contracts (`specs/016-knowledge-module-core/contracts/diagnostic-cascade.md`) how the FK actually works on template-side questions. If the FK strictly points at `knowledge.Topics`, the seeded `FormTemplate.Questions.TopicId` values would violate it. The seed MUST use whatever id space the schema allows for template-side questions.

> **Resolution needed during Phase 2 task implementation** — not a Phase 1 blocker. The risk is that the seed insert will fail DACPAC publish if the TopicId space is wrong. The tasks list will include a read-the-016-schema sub-task before writing this seed.

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
