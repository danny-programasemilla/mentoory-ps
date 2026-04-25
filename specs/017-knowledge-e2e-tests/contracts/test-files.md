# Contract: Test File Inventory & Scenario Mapping

**Scope**: `tests/Mentoory.Tests.E2E/Tests/` (six files) + `tests/Mentoory.Tests.Integration/Knowledge/DiagnosticCascadeRoundTripTests.cs` (extended).

Each entry below lists the target test class, the acceptance scenarios from `spec.md` it covers, and the test-method name (xUnit `[Fact]` or `[Theory]` with `[InlineData]`). Method names follow the `{Scenario}_{Expectation}` pattern used in the existing suite.

---

## File 1 — `KnowledgeTemplatesTests.cs` *(extended)*

**User Story**: US1 — Global-admin template curation
**Login**: `multirole@test.mentoory.com` → `ContextSelection.GlobalAdmin`

| # | Method | Covers | Notes |
|---|---|---|---|
| 1.1 | `Templates_PageLoads_ShowsSeededTemplate` | US1-1 (partial) | **Existing**; keep. Assert seeded `Emprendimiento Básico` visible. |
| 1.2 | `Templates_ListSurface_ShowsArchivedStateAndNewButton` | US1-1 (remainder) | **New**; visible archived/active indicator + "Nueva plantilla" action reachable. |
| 1.3 | `TemplateDetail_PageLoads_ShowsTree` | US1-2 | **Existing**; keep. Extend assertions to cover all four hierarchy levels in Spanish. |
| 1.4 | `TemplateDetail_AddNodes_PersistsAfterReload` | US1-3 | **New**; add Module → Topic → Subject → Resource(Video); reload; assert all present. |
| 1.5 | `TopicPriorityRanges_SaveAndReload_PersistsThreeBands` | US1-4 | **New**; fill High/Medium/Low; reload; assert values on the editor. |
| 1.6 | `TopicPriorityRanges_OverlappingBands_ShowsValidationError` | US1-5 | **New**; submit overlap; assert Spanish error; assert previous state preserved. |
| 1.7 | `TemplateModules_Reorder_PersistsAfterReload` | US1-6 | **New**; reorder two seeded modules; reload; assert new order. |
| 1.8 | `Template_ArchiveAndUnarchive_ReflectsInListToggles` | US1-7 | **New**; test-local template (unique name); archive; toggle "Mostrar archivadas"; assert visible; unarchive; assert back. |
| 1.9 | `Template_HardDelete_BlockedWhenProjectCloneExists` | US1-8 | **New**; attempt hard-delete on seeded `Emprendimiento Básico`; assert Spanish block message; assert template still present. |
| 1.10 | `CreateTemplate_HappyPath_AppearsInList` | — | **Existing**; keep (smoke). Not a direct spec-scenario target; counted as regression coverage. |
| 1.11 | `Templates_CoordinatorCannotAccess` | US1-9 (partial) | **Existing**; keep. Extended variants live in `KnowledgeAuthorizationTests` (US6-1 theory). |

**File total**: 11 methods (5 existing + 6 new)

---

## File 2 — `KnowledgeProjectStructureTests.cs` *(extended)*

**User Story**: US2 + US4 (partial)
**Login**: `coord1@test.mentoory.com` → `ContextSelection.FirstEnabled`

| # | Method | Covers | Notes |
|---|---|---|---|
| 2.1 | `Projects_PageLoads_ShowsMaterializedKs` | US2-1 | **Existing**; keep. |
| 2.2 | `Projects_NoCloneFromTemplateButton_Phase9Regression` | US2-5 | **Existing**; keep. |
| 2.3 | `CloneFromTemplate_LegacyRoute_NoLongerAccessible` | US2-6 | **Existing**; keep. |
| 2.4 | `ProjectStructureDetail_PageLoads_ShowsTree` | US2-4 | **Existing**; extend. Assert SyncMode badge shows `Desconectado` and all four levels render in Spanish. |
| 2.5 | `ProjectStructureDetail_TreeContainsClonedNames` | US4-2 (partial / regression) | **New**; assert specific Spanish names from the seed (`Finanzas`, `Mercadeo`, etc.) appear in the tree. |
| 2.6 | `ProjectStructure_CoordinatorB_CannotSeeCoordinatorAsProject` | US4-7 / US6-3 | **New**; log in as `coordnorte@test.mentoory.com` (second incubator); GET `coord1`'s project KS by ExternalId; assert ≥400 or empty body. |

**File total**: 6 methods (4 existing + 2 new)

---

## File 3 — `KnowledgeProjectTreeEditingTests.cs` *(new)*

**User Story**: US4 — Coordinator project-tree editing
**Login**: `coord1@test.mentoory.com` → `ContextSelection.FirstEnabled`

| # | Method | Covers | Notes |
|---|---|---|---|
| 3.1 | `AddNodesAtEachLevel_PersistsAfterReload` | US4-1 | Add Module → Topic → Subject → Resource; reload; assert all present with clone-only styling (no template-source badge). |
| 3.2 | `RenameClonedTopic_PersistsOnCloneOnly` | US4-2 | Rename `Finanzas` → `Finanzas Básicas` on the project KS. Open template detail in second browser context as GlobalAdmin; assert template's `Finanzas` is unchanged. |
| 3.3 | `ProjectTopicPriorityRanges_SaveAndReload_PersistsBands` | US4-3 | Set High/Medium/Low on a project topic; reload; assert bands in order High → Medium → Low. |
| 3.4 | `ProjectTopicPriorityRanges_OverlappingBands_ShowsValidationError` | US4-4 | Overlap attempt → Spanish error; previous bands preserved. |
| 3.5 | `DeleteProjectTopic_Referenced_Blocked` | US4-5 | Prereq: run US3-1 happy path first (creates `Question.TopicId` ref); attempt delete; assert Spanish error naming question count; topic still visible. |
| 3.6 | `DeleteProjectTopic_Unreferenced_Succeeds` | US4-6 | Add a new project topic (clone-only, no FK refs); delete; assert gone after reload. |
| 3.7 | `ProjectTopic_TenantIsolation_CrossProjectReturnsNotFound` | US4-7 | Coord A requests coord B's KS detail by ExternalId; assert ≥400 or unauthorized redirect. |

**File total**: 7 methods (all new)

**Shared state caveat**: Method 3.5 depends on method 3.x from `KnowledgeFormCloneCascadeTests` having run (to create the `Question.TopicId` reference). Since xUnit does not guarantee cross-file ordering, the test MUST perform its own cascade setup via `KnowledgeIntegrationHelpers.CloneFormIntoProjectAsync(projectId, formTemplateExternalId)` before the delete attempt.

---

## File 4 — `KnowledgeFormCloneCascadeTests.cs` *(new)*

**User Story**: US3 — Cross-module form clone cascade (UI portion)
**Login**: `coord1@test.mentoory.com` → `ContextSelection.FirstEnabled`

| # | Method | Covers | Notes |
|---|---|---|---|
| 4.1 | `CloneCompatibleForm_HappyPath_CreatesProjectForm` | US3-1 (UI) | Navigate to Diagnostic forms → pick seeded bound FormTemplate → clone into project → assert success toast + new `ProjectForm` visible in the list. |
| 4.2 | `CloneMismatchedForm_ShowsPhase9MismatchError` | US3-2 | Create a test-local FormTemplate bound to a different KS template via `KnowledgeIntegrationHelpers.CreateFormTemplateAsync`. Clone into the seeded project. Assert exact Spanish substring `"Este formulario está diseñado para una estructura de conocimiento diferente"`. Assert no new `ProjectForm` created (via integration-helper count). |

**File total**: 2 methods

**Integration backstops** for US3-3 and US3-4 live in the integration-test file (see File 7).

---

## File 5 — `KnowledgePartialSyncTests.cs` *(new)*

**User Story**: US5 — PartialSync lifecycle
**Login**: `coord1@test.mentoory.com` → `ContextSelection.FirstEnabled`

| # | Method | Covers | Notes |
|---|---|---|---|
| 5.1 | `Disconnected_SyncActionHiddenOrDisabled` | US5-1 | Open seeded project KS (SyncMode = Disconnected by default); assert "Sincronizar desde plantilla" action is disabled or absent; assert SyncMode toggle present. |
| 5.2 | `SwitchToPartialSync_PersistsAndEnablesSyncAction` | US5-2 | Toggle SyncMode; save; reload; assert sync action is enabled. |
| 5.3 | `PartialSync_AppendsNewTemplateTopic_UnderMatchingParent` | US5-3 | Toggle PartialSync. Via `KnowledgeIntegrationHelpers.AddTopicToTemplateAsync`, add `Topic_Nuevo_{Guid}` under a seeded module. Click sync. Assert summary notification in Spanish with non-zero count. Reload tree; assert the new topic appears under its matching clone-side parent with `SortOrder = max+1`. |
| 5.4 | `PartialSync_LocalAddedTopic_Untouched` | US5-4 | In PartialSync mode, add a clone-only topic M_local. Trigger sync. Assert M_local still present (name, sort-order, children unchanged). |
| 5.5 | `PartialSync_LocallyRenamedTopic_RetainsLocalName` | US5-5 | Rename a cloned-from-template topic locally. Trigger sync. Assert the topic retains its local name and position. |

**File total**: 5 methods

---

## File 6 — `KnowledgeAuthorizationTests.cs` *(new)*

**User Story**: US6 — Authorization + tenant isolation + menu visibility

| # | Method | Covers | Notes |
|---|---|---|---|
| 6.1 | `ProtectedRoutes_CoordinatorDenied_ForTemplateRoutes` | US6-1 | `[Theory]` over `[InlineData]` for ~10 routes under `/Coordination/Knowledge/Templates/**`. Login as `coord1`. Assert every response is ≥400 OR URL no longer contains the protected path. |
| 6.2 | `ProtectedRoutes_Unauthenticated_RedirectToLogin` | US6-2 | `[Theory]` over ~5 routes under `/Coordination/Knowledge/*`. No login. Assert URL ends at `/Access/Login`. |
| 6.3 | `TenantIsolation_CoordinatorB_CannotAccessCoordinatorAsKs` | US6-3 | Log in as `coordnorte@test.mentoory.com`. GET `coord1`'s project KS detail by ExternalId. Assert ≥400 OR body contains none of coord1's KS module/topic names. |
| 6.4 | `CreateProject_CrossIncubatorForm_Denied` | US6-4 | Log in as `incadmin1`. POST `/Administration/Projects/Create` with a hand-crafted body carrying a foreign IncubatorId (form-hack simulation). Assert ≥400 / authz error / no project row (assert via integration helper post-check). |
| 6.5 | `Menu_GlobalAdmin_ShowsBothEntries_Coordinator_ShowsOnlyProjects` | US6-5 | Login as `multirole` → GlobalAdmin context; assert sidebar shows "Plantillas de conocimiento" AND "Estructuras del proyecto". Login as `coord1`; assert sidebar shows only "Estructuras del proyecto". |

**File total**: 5 methods (theories count as 1 but generate multiple `[InlineData]` rows — ~20 parameterized cases)

---

## File 7 — `DiagnosticCascadeRoundTripTests.cs` *(extended integration file)*

**User Story**: US3 (DB-invariant portions) + US5 setup support

| # | Method | Covers | Notes |
|---|---|---|---|
| 7.1 | `CloneFormTemplate_CompatibleForm_RewritesTopicIdsToProjectsKs` | US3-1 (backstop) | **Existing**; keep. |
| 7.2 | `CloneFormTemplate_TwiceForSameProject_DoesNotDuplicateProjectKs` | US3-4 | **Existing**; keep. |
| 7.3 | `CloneFormTemplate_NullBinding_NoKnowledgeCascade` | US3-3 | **New**; FormTemplate with `DefaultKnowledgeStructureTemplateExternalId = null`. Clone into project. Assert clone succeeds, project's KS rowcount remains 1, Question.TopicId values retain template-topic references. |
| 7.4 | `KnowledgeIntegrationHelpers_UtilityContract_RegressionGuard` | — | **New**; tiny round-trip test that proves `AddTopicToTemplateAsync` and `CreateFormTemplateAsync` helpers (consumed by US5 E2E) commit to the same DB the E2E fixture reads. Runs in under 1 second; acts as a fail-fast guard on helper drift. |

**File total**: 4 methods (2 existing + 2 new)

---

## Shared Integration Helpers

**File**: `tests/Mentoory.Tests.E2E/Infrastructure/KnowledgeIntegrationHelpers.cs` (new)

Used by File 3 (method 3.5 setup), File 4 (method 4.2 setup + post-check), File 5 (methods 5.3 setup). Wraps `WebApplicationFactory.Services.CreateScope()` + `IMediator.Send` calls.

```csharp
public static class KnowledgeIntegrationHelpers
{
    public static Task<Guid> CreateFormTemplateAsync(
        WebApplicationFactory<Program> factory,
        Guid? boundKsTemplateExternalId,
        params (long TopicId, string Text)[] questions);

    public static Task<Guid> AddTopicToTemplateAsync(
        WebApplicationFactory<Program> factory,
        Guid ksTemplateExternalId,
        Guid moduleExternalId,
        string topicName);

    public static Task<int> CountProjectFormsAsync(
        WebApplicationFactory<Program> factory,
        long projectId);

    public static Task<Result<Guid>> CloneFormIntoProjectAsync(
        WebApplicationFactory<Program> factory,
        Guid formTemplateExternalId,
        long projectId,
        long incubatorId);
}
```

**Contract**:
- All methods take the shared `PlaywrightFixture` (which IS the `WebApplicationFactory<Program>`) and use the same DB container the browser is talking to.
- All methods are idempotent in the sense that calling them with fresh unique inputs produces fresh unique outputs; they do not assume initial DB state beyond what the DACPAC seed provides.
- None of these helpers swallow `Result<T>` failures — callers must check `.IsSuccess` and assert via FluentAssertions with a descriptive `because` string.

## Coverage Summary

| User Story | Spec scenarios | Test methods | Coverage |
|---|---|---|---|
| US1 | 9 | 11 (incl. 2 existing regression) | ✅ |
| US2 | 6 | 6 (all existing or extended) | ✅ |
| US3 | 4 | 2 E2E + 2 integration = 4 | ✅ |
| US4 | 7 | 7 | ✅ |
| US5 | 5 | 5 | ✅ |
| US6 | 5 | 5 (one is a parameterized theory) | ✅ |
| **Total** | **36 scenarios** | **~40 test methods (incl. theory rows)** | ✅ |
