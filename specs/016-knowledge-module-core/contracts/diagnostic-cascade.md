# Contracts: Diagnostic Form Clone Cascade

**Scope**: FR-K20 through FR-K23 — the modifications to the existing `Mentoory.Diagnostic` module that close the dangling `Questions.TopicId` FK.

---

## Modified Diagnostic Aggregate

### `Mentoory.Diagnostic.Domain.Aggregates.FormTemplate.FormTemplate` (MODIFY)

**Add field + mutation**:
```
Guid? DefaultKnowledgeStructureTemplateExternalId { get; private set; }

public void SetDefaultKnowledgeStructureTemplate(Guid? externalId)
{
    DefaultKnowledgeStructureTemplateExternalId = externalId;
    Version++;
}
```

The internal `long? DefaultKnowledgeStructureTemplateId` is a private EF navigation (not exposed on the domain surface) populated by the infrastructure layer via the `ExternalId` lookup.

**Persistence**: adds nullable column on `diagnostic.FormTemplates` table + FK constraint.

---

## New Command in Diagnostic

### `SetFormTemplateKnowledgeBindingCommand : IBaseRequest`
```
record SetFormTemplateKnowledgeBindingCommand(
    Guid FormTemplateExternalId,
    Guid? KnowledgeStructureTemplateExternalId) : IBaseRequest;
```
**Handler** (Diagnostic module):
1. Load form template by `ExternalId`.
2. If `KnowledgeStructureTemplateExternalId` is not null, verify it exists via `IKnowledgeStructureTemplateRepository.ExistsByExternalIdAsync` (cross-module query; dependency injection boundary).
3. Call `formTemplate.SetDefaultKnowledgeStructureTemplate(...)`.
4. Save.

**Authorization**: GlobalAdmin only.

---

## Modified `CloneFormTemplateHandler`

**Current signature** (unchanged):
```
CloneFormTemplateCommand(
    Guid SourceTemplateExternalId,
    long ProjectId,
    long IncubatorId) : IBaseRequest;
```

**New dependencies**:
- `IKnowledgeStructureTemplateRepository` — for loading the bound template with full tree
- `IKnowledgeStructureRepository` — for checking existing project clone and adding new one
- `IDbConnectionFactory` (or equivalent) — for acquiring a shared connection to span Knowledge + Diagnostic transactions (see research R3)

**New flow** (inside a single explicit transaction on a shared connection):

```
1. formTemplate = await formTemplateRepository
        .GetByExternalIdWithQuestionsAsync(SourceTemplateExternalId);
   if formTemplate is null → Failure (unchanged).

2. topicIdRewriteMap = null as IReadOnlyDictionary<long, long>?;
   // key: TEMPLATE-topic internal id; value: PROJECT-topic internal id

3. if formTemplate.DefaultKnowledgeStructureTemplateExternalId is not null:

   3a. Load the bound knowledge template with full tree:
       ksTemplate = await knowledgeStructureTemplateRepository
           .GetByExternalIdWithFullTreeAsync(
               formTemplate.DefaultKnowledgeStructureTemplateExternalId.Value);
       if ksTemplate is null → Failure (data integrity).

   3b. Look for existing project clone (EC-22):
       projectKs = await knowledgeStructureRepository
           .GetByProjectAndSourceTemplateIdAsync(
               projectId: command.ProjectId,
               sourceTemplateId: ksTemplate.Id);

   3c. If projectKs is null:
       projectKs = KnowledgeStructure.CloneFromTemplate(
           ksTemplate, command.ProjectId, command.IncubatorId, _timeProvider.UtcNow);
       knowledgeStructureRepository.Add(projectKs);

   3d. Build the rewrite map by walking both trees (template + project clone),
       pairing template-topic.ExternalId against the clone topic whose
       SourceTemplateTopicExternalId equals that ExternalId:

       templateTopicsByExternalId = {};  // Dictionary<Guid, long>
       foreach mt in ksTemplate.Modules:
         foreach tt in mt.Topics:
           templateTopicsByExternalId[tt.ExternalId] = tt.Id;

       topicIdRewriteMap = new Dictionary<long, long>();
       foreach m in projectKs.Modules:
         foreach t in m.Topics:
           if t.SourceTemplateTopicExternalId is not null
              AND templateTopicsByExternalId.TryGetValue(
                      t.SourceTemplateTopicExternalId.Value, out var templateTopicId):
             topicIdRewriteMap[templateTopicId] = t.Id;

4. projectForm = ProjectForm.CloneFromTemplate(
       formTemplate,
       command.ProjectId,
       command.IncubatorId,
       _timeProvider.UtcNow,
       topicIdRewriteMap);       // NEW optional parameter

5. projectFormRepository.Add(projectForm);

6. Inside the surrounding transaction: call SaveChangesAsync on BOTH contexts, then commit transaction.

7. If ProjectForm.CloneFromTemplate throws (see rewrite contract below): transaction rolls back;
   no Knowledge or Diagnostic writes commit (FR-K23).

8. Return Success.
```

### Modified `ProjectForm.CloneFromTemplate` signature

```
public static ProjectForm CloneFromTemplate(
    FormTemplate template,
    long projectId,
    long incubatorId,
    DateTime utcNow,
    IReadOnlyDictionary<long, long>? topicIdRewriteMap = null)
```

**Rewrite contract**:
- If `topicIdRewriteMap is null`: questions are cloned with `Question.TopicId` copied from `QuestionTemplate.TopicId` unchanged. (FR-K22: form template with no knowledge binding; legal template-topic references.)
- If `topicIdRewriteMap is not null`: for every `QuestionTemplate qt` being cloned, `qt.TopicId` MUST be a key of the map; the cloned `Question.TopicId` receives `topicIdRewriteMap[qt.TopicId]`. If `qt.TopicId` is NOT a key, the factory throws `InvalidOperationException` with a message identifying the offending question text (FR-K23). This is the only exit path for unresolvable topics — it rolls back the entire transaction at step 7.

**Rationale for aggregate-root-routing**: the rewrite happens inside `ProjectForm.CloneFromTemplate` rather than via a post-hoc mutator on `Question`. This keeps the DDD invariant "aggregate root controls all mutations to children" (constitution Principle III) intact; `Question` never exposes a `RewriteTopicId` method. The factory is the aggregate's natural construction point, and the map is its only external input besides the template itself.

---

## New Interfaces Introduced at the Module Boundary

### `IKnowledgeStructureTemplateRepository`
```
Task<KnowledgeStructureTemplate?> GetByExternalIdAsync(Guid externalId, CancellationToken ct = default);
Task<KnowledgeStructureTemplate?> GetByIdAsync(long id, CancellationToken ct = default);
Task<KnowledgeStructureTemplate?> GetByExternalIdWithFullTreeAsync(Guid externalId, CancellationToken ct = default);
Task<KnowledgeStructureTemplate?> GetByIdWithFullTreeAsync(long id, CancellationToken ct = default);
Task<bool> ExistsByExternalIdAsync(Guid externalId, CancellationToken ct = default);
void Add(KnowledgeStructureTemplate entity);
void Update(KnowledgeStructureTemplate entity);
void Remove(KnowledgeStructureTemplate entity);
IUnitOfWork UnitOfWork { get; }
```

### `IKnowledgeStructureRepository`
```
Task<KnowledgeStructure?> GetByExternalIdAsync(Guid externalId, CancellationToken ct = default);
Task<KnowledgeStructure?> GetByExternalIdWithFullTreeAsync(Guid externalId, CancellationToken ct = default);
Task<KnowledgeStructure?> GetByProjectAndSourceTemplateIdAsync(long projectId, long sourceTemplateId, CancellationToken ct = default);
Task<IReadOnlyList<KnowledgeStructure>> ListByProjectAsync(long projectId, CancellationToken ct = default);
Task<int> CountQuestionsReferencingTopicAsync(long topicId, CancellationToken ct = default);   // used by DeleteTopicCommand pre-check
void Add(KnowledgeStructure entity);
void Update(KnowledgeStructure entity);
void Remove(KnowledgeStructure entity);
IUnitOfWork UnitOfWork { get; }
```

**Note on `CountQuestionsReferencingTopicAsync`**: this query crosses schemas (queries `diagnostic.Questions` from Knowledge infrastructure). The cleanest placement is actually on Diagnostic side — add it to an existing `IQuestionRepository` or a new small interface `ITopicUsageQuery`. Detail decision is for implementation; contract is just "the Delete handler can check usage before deletion."

---

## Integration Points Summary

| File | Modification |
|---|---|
| `Mentoory.Diagnostic.Domain/Aggregates/FormTemplate/FormTemplate.cs` | Add `DefaultKnowledgeStructureTemplateExternalId`, setter, version bump |
| `Mentoory.Diagnostic.Domain/Aggregates/ProjectForm/ProjectForm.cs` | Add optional `IReadOnlyDictionary<long, long>? topicIdRewriteMap` parameter to `CloneFromTemplate`; factory throws when map is non-null and a template question's `TopicId` is not a map key |
| `Mentoory.Diagnostic.Application/Commands/CloneFormTemplate/CloneFormTemplateHandler.cs` | Add IKnowledgeStructureTemplateRepository + IKnowledgeStructureRepository deps; implement cascade logic; introduce explicit transaction |
| `Mentoory.Diagnostic.Application/Commands/SetFormTemplateKnowledgeBinding/` | NEW command + handler + validator |
| `Mentoory.Diagnostic.Infrastructure/Persistence/Configurations/FormTemplateConfiguration.cs` | Add EF config for new column + FK |
| `Mentoory.Db/diagnostic/Tables/FormTemplates.sql` | Add column `DefaultKnowledgeStructureTemplateId BIGINT NULL` + FK constraint to `knowledge.KnowledgeStructureTemplates` |
| `Mentoory.Db/diagnostic/Tables/Questions.sql` | Add FK constraint `TopicId → knowledge.Topics(Id)` |
