# Contracts: Project Knowledge Structures (per-project clones)

**Scope**: FR-K10 through FR-K15 (clone, full CRUD on clone, sync-mode toggle, PartialSync).
**Authorization**: `[Authorize(Roles = "ProjectCoordinator,IncubatorAdmin,GlobalAdmin")]`. All handlers enforce `ITenantContext.CurrentProjectId == entity.ProjectId` (EC-40).

---

## Commands

### `CloneKnowledgeStructureTemplateCommand : IBaseRequest<Guid>`
```
record CloneKnowledgeStructureTemplateCommand(
    Guid TemplateExternalId,
    long ProjectId,
    long IncubatorId) : IBaseRequest<Guid>;
```
**Handler**: loads template full tree → `KnowledgeStructure.CloneFromTemplate(template, projectId, incubatorId, _timeProvider.UtcNow)` → add → save → return new clone's `ExternalId`. Validates template not archived.

**Note**: This command is also invoked internally by the modified `CloneFormTemplateHandler` (see `diagnostic-cascade.md`) when auto-cascading.

### `UpdateKnowledgeStructureCommand : IBaseRequest`
```
record UpdateKnowledgeStructureCommand(
    Guid StructureExternalId,
    string Name,
    string? Description) : IBaseRequest;
```

### Module-level (on clone)
- `AddModuleCommand : IBaseRequest<Guid>` — locally-added, no source stamp
- `UpdateModuleCommand`
- `DeleteModuleCommand` — EC-31 applies (no downstream checks needed in v1)
- `ReorderModulesCommand`

### Topic-level (on clone)
- `AddTopicCommand : IBaseRequest<Guid>`
- `UpdateTopicCommand`
- `DeleteTopicCommand`:
  ```
  record DeleteTopicCommand(Guid TopicExternalId) : IBaseRequest;
  ```
  **Handler**: Application-layer pre-check (EC-30):
  - Query `diagnostic.Questions` count where `TopicId == topic.Id`.
  - If count > 0, return `Failure` with message `"No se puede eliminar el tema: {count} pregunta(s) de diagnóstico lo referencian. Reasigne o elimine las preguntas primero."`
  - Else proceed to `structure.RemoveTopic(...)` + save.
- `ReorderTopicsCommand`
- `UpdateTopicPriorityRangesCommand`:
  ```
  record UpdateTopicPriorityRangesCommand(
      Guid TopicExternalId,
      PriorityRangeDto? HighRange,
      PriorityRangeDto? MediumRange,
      PriorityRangeDto? LowRange) : IBaseRequest;
  ```
  **Handler**: applies ranges; `TopicPriorityRangesChanged` integration event is appended to the root aggregate's domain-event collection; `SaveChangesAsync` triggers the outbox-style flush that publishes it as `INotification` via `IMediator.Publish` (in-process delivery per FR-K30).

### Subject / Resource level (on clone)
Same shape as template-side equivalents; mirrored contract.

### `SetSyncModeCommand : IBaseRequest`
```
record SetSyncModeCommand(
    Guid StructureExternalId,
    SyncMode Mode) : IBaseRequest;
```
**Handler**: `structure.SetSyncMode(mode)` (throws if switching to `PartialSync` without `SourceTemplateId`); save.

### `SyncFromTemplateCommand : IBaseRequest<PartialSyncResultDto>`
```
record SyncFromTemplateCommand(Guid StructureExternalId) : IBaseRequest<PartialSyncResultDto>;

record PartialSyncResultDto(
    int ModulesAdded,
    int TopicsAdded,
    int SubjectsAdded,
    int ResourcesAdded)
{
    public int TotalAdded => ModulesAdded + TopicsAdded + SubjectsAdded + ResourcesAdded;
}
```
**Handler** (single transaction via `IUnitOfWork.SaveEntitiesAsync`):
1. Load clone with full tree via `GetByExternalIdWithFullTreeAsync`.
2. Reject if `structure.SyncMode != PartialSync`.
3. Load template with full tree via `GetByIdWithFullTreeAsync(structure.SourceTemplateId.Value)`.
4. Reject if template not found.
5. Call `structure.ApplyPartialSync(template)` → returns `PartialSyncResult`.
6. Save; return DTO (mapped by Mapperly).

---

## Queries

### `ListProjectKnowledgeStructuresQuery : IBaseRequest<IReadOnlyList<ProjectKnowledgeStructureListItemDto>>`
```
record ListProjectKnowledgeStructuresQuery() : ...;   // tenant-scoped by handler via ITenantContext

record ProjectKnowledgeStructureListItemDto(
    Guid ExternalId,
    string Name,
    Guid? SourceTemplateExternalId,
    int? SourceTemplateVersion,
    SyncMode SyncMode,
    int ModuleCount,
    int TopicCount,
    bool HasTemplateVersionDrift,    // true when SourceTemplateVersion < current template Version
    DateTime CreatedAtUtc);
```
Handler uses `AsNoTracking()` and joins to `KnowledgeStructureTemplates` to compute drift.

### `GetProjectKnowledgeStructureQuery : IBaseRequest<ProjectKnowledgeStructureDto?>`
Returns full-tree DTO for detail view with source-stamp indicators on each node (Mapperly-projected).

---

## UI Routes (`KnowledgeController`)

| Action | HTTP | Route | Auth |
|---|---|---|---|
| Projects (list) | GET | `/Coordination/Knowledge/Projects` | PC,IA,GA |
| Project structure detail | GET | `/Coordination/Knowledge/Projects/{externalId}` | PC,IA,GA |
| Clone from template | POST | `/Coordination/Knowledge/Projects/Clone/{templateExternalId}` | PC,IA,GA |
| Structure update | POST | `/Coordination/Knowledge/Projects/{externalId}` | PC,IA,GA |
| Module/Topic/Subject/Resource add/update/delete/reorder | POST | `/Coordination/Knowledge/Projects/{externalId}/{Level}/...` | PC,IA,GA |
| Topic ranges update | POST | `/Coordination/Knowledge/Projects/{externalId}/Topics/{topicExternalId}/Ranges` | PC,IA,GA |
| Set sync mode | POST | `/Coordination/Knowledge/Projects/{externalId}/SyncMode` | PC,IA,GA |
| Sync from template | POST | `/Coordination/Knowledge/Projects/{externalId}/Sync` | PC,IA,GA |

All routes carry `ExternalId`; never internal `long` ids.
