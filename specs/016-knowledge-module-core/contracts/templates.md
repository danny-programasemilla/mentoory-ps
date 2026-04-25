# Contracts: Knowledge Structure Templates (global catalog)

**Scope**: FR-K01 through FR-K06 — global-admin catalog of knowledge structure templates + descendant CRUD.
**Authorization**: `[Authorize(Roles = "GlobalAdmin")]` on every controller action. `ITenantContext.IsGlobalAdminScope == true` enforced at handler level.

All commands implement `IBaseRequest` or `IBaseRequest<TResult>`. Handlers derive from `BaseCommandHandler<T>`. Each command has a FluentValidation validator. Query handlers use `AsNoTracking()`.

---

## Commands

### `CreateKnowledgeStructureTemplateCommand : IBaseRequest<Guid>`
**Purpose**: Create a new empty template. Returns the new template's `ExternalId`.

```
record CreateKnowledgeStructureTemplateCommand(
    string Name,
    string? Description) : IBaseRequest<Guid>;
```
**Handler**: loads nothing; calls `KnowledgeStructureTemplate.Create(name, description, _timeProvider.UtcNow)`; `Add` + save.

### `UpdateKnowledgeStructureTemplateCommand : IBaseRequest`
```
record UpdateKnowledgeStructureTemplateCommand(
    Guid TemplateExternalId,
    string Name,
    string? Description) : IBaseRequest;
```
**Handler**: load → `template.UpdateDetails(...)` → save.

### `ArchiveKnowledgeStructureTemplateCommand : IBaseRequest`
```
record ArchiveKnowledgeStructureTemplateCommand(Guid TemplateExternalId) : IBaseRequest;
```
**Handler**: load → `Archive()` → save.

### `UnarchiveKnowledgeStructureTemplateCommand : IBaseRequest`
Symmetric to Archive.

### `DeleteKnowledgeStructureTemplateCommand : IBaseRequest`
**Handler**: guard that no `KnowledgeStructure` has `SourceTemplateId == target.Id` (EC-01); returns `Failure` with user-friendly Spanish message if blocked; otherwise delete.

### `AddModuleTemplateCommand : IBaseRequest<Guid>`
```
record AddModuleTemplateCommand(
    Guid TemplateExternalId,
    string Name,
    string? Description,
    int SortOrder) : IBaseRequest<Guid>;
```
**Handler**: `template.AddModule(...)` → save → return new module's `ExternalId`.

### `UpdateModuleTemplateCommand : IBaseRequest`
```
record UpdateModuleTemplateCommand(
    Guid ModuleExternalId,
    string Name,
    string? Description) : IBaseRequest;
```
**Handler**: loads template by module's `ExternalId` via `GetByModuleExternalIdWithFullTreeAsync`; calls `template.UpdateModule(moduleExternalId, name, description)`.

### `DeleteModuleTemplateCommand : IBaseRequest`
### `ReorderModuleTemplatesCommand : IBaseRequest`
```
record ReorderModuleTemplatesCommand(
    Guid TemplateExternalId,
    IReadOnlyList<Guid> ModuleExternalIdsInOrder) : IBaseRequest;
```

### Topic-level commands (mirror Module-level)
- `AddTopicTemplateCommand : IBaseRequest<Guid>` — parent is `ModuleExternalId`
- `UpdateTopicTemplateCommand : IBaseRequest` — metadata only (Name, Description, SortOrder)
- `DeleteTopicTemplateCommand : IBaseRequest`
- `ReorderTopicTemplatesCommand : IBaseRequest`
- `UpdateTopicTemplatePriorityRangesCommand : IBaseRequest`:
  ```
  record UpdateTopicTemplatePriorityRangesCommand(
      Guid TopicExternalId,
      PriorityRangeDto? HighRange,
      PriorityRangeDto? MediumRange,
      PriorityRangeDto? LowRange) : IBaseRequest;
  ```
  Validator rejects overlaps and `min > max` (FR-K05).

### Subject-level commands (mirror Topic-level without range editor)
- `AddSubjectTemplateCommand : IBaseRequest<Guid>` — parent `TopicExternalId`
- `UpdateSubjectTemplateCommand`, `DeleteSubjectTemplateCommand`, `ReorderSubjectTemplatesCommand`

### Resource-level commands
- `AddResourceTemplateCommand : IBaseRequest<Guid>`:
  ```
  record AddResourceTemplateCommand(
      Guid SubjectExternalId,
      string Title,
      string? Description,
      string Url,
      ResourceType ResourceType,
      int SortOrder) : IBaseRequest<Guid>;
  ```
  Validator enforces `Url` is a valid absolute URI.
- `UpdateResourceTemplateCommand`, `DeleteResourceTemplateCommand`, `ReorderResourceTemplatesCommand`

---

## Queries

### `ListKnowledgeStructureTemplatesQuery : IBaseRequest<IReadOnlyList<KnowledgeStructureTemplateListItemDto>>`
```
record ListKnowledgeStructureTemplatesQuery(bool IncludeArchived) : ...;

record KnowledgeStructureTemplateListItemDto(
    Guid ExternalId,
    string Name,
    string? Description,
    bool IsArchived,
    int Version,
    int ModuleCount,
    int TopicCount,
    DateTime CreatedAtUtc);
```

### `GetKnowledgeStructureTemplateQuery : IBaseRequest<KnowledgeStructureTemplateDto?>`
Returns full-tree DTO (modules → topics → subjects → resources) for detail view. Mapped via Mapperly.

---

## UI Routes (`KnowledgeController`)

| Action | HTTP | Route | Auth |
|---|---|---|---|
| Templates (list) | GET | `/Coordination/Knowledge/Templates` | GlobalAdmin |
| Template detail | GET | `/Coordination/Knowledge/Templates/{externalId}` | GlobalAdmin |
| Template create | POST | `/Coordination/Knowledge/Templates` | GlobalAdmin |
| Template update | POST | `/Coordination/Knowledge/Templates/{externalId}` | GlobalAdmin |
| Template archive | POST | `/Coordination/Knowledge/Templates/{externalId}/Archive` | GlobalAdmin |
| Module add/update/delete/reorder | POST | `/Coordination/Knowledge/Templates/{externalId}/Modules/...` | GlobalAdmin |
| Topic add/update/delete/reorder/ranges | POST | `/Coordination/Knowledge/Templates/{externalId}/Topics/...` | GlobalAdmin |
| Subject add/update/delete/reorder | POST | `/Coordination/Knowledge/Templates/{externalId}/Subjects/...` | GlobalAdmin |
| Resource add/update/delete/reorder | POST | `/Coordination/Knowledge/Templates/{externalId}/Resources/...` | GlobalAdmin |

All POST actions follow the `this.SetSuccessToast()` / `this.MapErrorsToModelStateAndSetErrorToast<T>()` pattern from existing controllers.
