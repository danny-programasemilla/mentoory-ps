# Data Model: Knowledge Module Core

**Feature**: 016-knowledge-module-core
**Date**: 2026-04-18

All types live under `Mentoory.Knowledge.Domain`. Persistence mapping lives under `Mentoory.Knowledge.Infrastructure/Persistence/Configurations/`. Tables live under `Mentoory.Db/knowledge/Tables/`. DECIMAL precision matches `diagnostic.AnswerOptions.Score` (DECIMAL(10,2)) for all numeric range columns (per research R1).

---

## Enums

### `ResourceType` (`Mentoory.Knowledge.Domain/Enums/ResourceType.cs`)
```
public enum ResourceType
{
    Video = 0,
    Link = 1,
    File = 2
}
```
Stored as `TINYINT NOT NULL` on both `ResourceTemplates` and `Resources`.

### `SyncMode` (`Mentoory.Knowledge.Domain/Enums/SyncMode.cs`)
```
public enum SyncMode
{
    Disconnected = 0,
    PartialSync = 1
}
```
Stored as `TINYINT NOT NULL` on `KnowledgeStructures`. Value must match `Mentoory.Diagnostic.Domain.Enums.SyncMode` (already defined); do NOT share the Diagnostic enum — duplicate in Knowledge to preserve module boundaries (per constitution I).

### `Priority` (`Mentoory.Knowledge.Domain/Enums/Priority.cs`)
```
public enum Priority
{
    NotApplicable = 0,
    Low = 1,
    Medium = 2,
    High = 3
}
```
Not persisted; returned by `Topic.ResolvePriority(decimal score)`.

---

## Value Objects

### `PriorityRange` (`Mentoory.Knowledge.Domain/ValueObjects/PriorityRange.cs`)
```
public sealed record PriorityRange
{
    public decimal Min { get; }
    public decimal Max { get; }

    private PriorityRange(decimal min, decimal max) { Min = min; Max = max; }

    public static PriorityRange Create(decimal min, decimal max)
    {
        if (min > max)
            throw new ArgumentException("Min must be less than or equal to Max.");
        return new PriorityRange(min, max);
    }

    public bool Contains(decimal score) => score >= Min && score <= Max;

    public bool OverlapsWith(PriorityRange other) =>
        other is not null && Min <= other.Max && other.Min <= Max;
}
```
Persisted as two nullable decimal columns on the parent entity (`HighRangeMin`, `HighRangeMax`, etc.). A row has a configured band iff both Min and Max are non-null.

---

## Aggregate 1: `KnowledgeStructureTemplate` (global catalog)

### Root: `KnowledgeStructureTemplate`
**Implements:** `Entity`, `IAggregateRoot`

| Field | Type | SQL | Notes |
|---|---|---|---|
| `Id` | `long` | `BIGINT IDENTITY PK` | Internal identity |
| `ExternalId` | `Guid` | `UNIQUEIDENTIFIER NOT NULL UNIQUE` | Route parameter |
| `Name` | `string` | `NVARCHAR(200) NOT NULL` | Required; validated by FluentValidation |
| `Description` | `string?` | `NVARCHAR(2000) NULL` | |
| `IsArchived` | `bool` | `BIT NOT NULL DEFAULT 0` | |
| `Version` | `int` | `INT NOT NULL DEFAULT 1` | Bumped on any descendant change |
| `CreatedAtUtc` | `DateTime` | `DATETIME2(3) NOT NULL` | Set on factory; domain receives `utcNow` parameter |
| `_moduleTemplates` | `List<ModuleTemplate>` | (child table) | Private backing; exposed via `Modules => _moduleTemplates.AsReadOnly()` |

**Factory**: `static KnowledgeStructureTemplate Create(string name, string? description, DateTime utcNow)`
**Mutations** (all increment `Version`):
- `UpdateDetails(string name, string? description)`
- `Archive()` / `Unarchive()` — toggle `IsArchived`
- `AddModule(string name, string? description, int sortOrder, DateTime utcNow) → ModuleTemplate`
- `UpdateModule(Guid moduleExternalId, ...)`
- `RemoveModule(Guid moduleExternalId)`
- `ReorderModules(IReadOnlyList<Guid> moduleExternalIdsInOrder)`
- `AddTopic(Guid moduleExternalId, string name, ...)` — delegates to `ModuleTemplate.AddTopic`; bumps root `Version`
- `UpdateTopic(Guid topicExternalId, ...)` / `RemoveTopic(...)` / `ReorderTopics(Guid moduleExternalId, ...)`
- `UpdateTopicPriorityRanges(Guid topicExternalId, PriorityRange? high, PriorityRange? medium, PriorityRange? low)`
- `AddSubject(Guid topicExternalId, ...)` / ...
- `AddResource(Guid subjectExternalId, string title, string? description, string url, ResourceType type, int sortOrder)` / ...

**Invariants**:
- Hard delete blocked at Application layer when any `KnowledgeStructure` has `SourceTemplateId == this.Id` (FR-K01, EC-01).
- Priority-range edits on a topic: validator asserts non-overlap across the three bands and `min <= max` each (FR-K05).

### `ModuleTemplate`
| Field | Type | SQL | Notes |
|---|---|---|---|
| `Id` | `long` | `BIGINT IDENTITY PK` | |
| `ExternalId` | `Guid` | `UNIQUEIDENTIFIER NOT NULL UNIQUE` | Stamped on clone children |
| `KnowledgeStructureTemplateId` | `long` | `BIGINT NOT NULL FK` | Parent |
| `Name` | `string` | `NVARCHAR(200) NOT NULL` | |
| `Description` | `string?` | `NVARCHAR(2000) NULL` | |
| `SortOrder` | `int` | `INT NOT NULL` | |
| `_topicTemplates` | `List<TopicTemplate>` | (child) | Private backing |

Factory / mutation methods `internal`; only root can invoke.

### `TopicTemplate`
| Field | Type | SQL | Notes |
|---|---|---|---|
| `Id` | `long` | `BIGINT IDENTITY PK` | |
| `ExternalId` | `Guid` | `UNIQUEIDENTIFIER NOT NULL UNIQUE` | |
| `ModuleTemplateId` | `long` | `BIGINT NOT NULL FK` | |
| `Name` | `string` | `NVARCHAR(200) NOT NULL` | |
| `Description` | `string?` | `NVARCHAR(2000) NULL` | |
| `SortOrder` | `int` | `INT NOT NULL` | |
| `HighRangeMin` | `decimal?` | `DECIMAL(10,2) NULL` | |
| `HighRangeMax` | `decimal?` | `DECIMAL(10,2) NULL` | |
| `MediumRangeMin` | `decimal?` | `DECIMAL(10,2) NULL` | |
| `MediumRangeMax` | `decimal?` | `DECIMAL(10,2) NULL` | |
| `LowRangeMin` | `decimal?` | `DECIMAL(10,2) NULL` | |
| `LowRangeMax` | `decimal?` | `DECIMAL(10,2) NULL` | |
| `_subjectTemplates` | `List<SubjectTemplate>` | (child) | |

Exposes `HighRange`, `MediumRange`, `LowRange` as `PriorityRange?` computed from the six columns.

### `SubjectTemplate`
| Field | Type | SQL | Notes |
|---|---|---|---|
| `Id` | `long` | `BIGINT IDENTITY PK` | |
| `ExternalId` | `Guid` | `UNIQUEIDENTIFIER NOT NULL UNIQUE` | |
| `TopicTemplateId` | `long` | `BIGINT NOT NULL FK` | |
| `Name` | `string` | `NVARCHAR(200) NOT NULL` | |
| `Description` | `string?` | `NVARCHAR(2000) NULL` | |
| `SortOrder` | `int` | `INT NOT NULL` | |
| `_resourceTemplates` | `List<ResourceTemplate>` | (child) | |

### `ResourceTemplate`
| Field | Type | SQL | Notes |
|---|---|---|---|
| `Id` | `long` | `BIGINT IDENTITY PK` | |
| `ExternalId` | `Guid` | `UNIQUEIDENTIFIER NOT NULL UNIQUE` | |
| `SubjectTemplateId` | `long` | `BIGINT NOT NULL FK` | |
| `Title` | `string` | `NVARCHAR(200) NOT NULL` | |
| `Description` | `string?` | `NVARCHAR(2000) NULL` | |
| `Url` | `string` | `NVARCHAR(2000) NOT NULL` | URL validated at Application layer |
| `ResourceType` | `ResourceType` | `TINYINT NOT NULL` | Video/Link/File |
| `SortOrder` | `int` | `INT NOT NULL` | |

---

## Aggregate 2: `KnowledgeStructure` (project-level clone)

### Root: `KnowledgeStructure`
**Implements:** `Entity`, `IAggregateRoot`

| Field | Type | SQL | Notes |
|---|---|---|---|
| `Id` | `long` | `BIGINT IDENTITY PK` | |
| `ExternalId` | `Guid` | `UNIQUEIDENTIFIER NOT NULL UNIQUE` | |
| `ProjectId` | `long` | `BIGINT NOT NULL` | Tenant scope |
| `IncubatorId` | `long` | `BIGINT NOT NULL` | Denormalized for query convenience |
| `Name` | `string` | `NVARCHAR(200) NOT NULL` | Copied from template at clone; editable |
| `Description` | `string?` | `NVARCHAR(2000) NULL` | |
| `SourceTemplateId` | `long?` | `BIGINT NULL FK → knowledge.KnowledgeStructureTemplates(Id)` | Null reserved for future "create-from-scratch" path; in v1 always set |
| `SourceTemplateVersion` | `int?` | `INT NULL` | Stamped at clone time; compared for drift (EC-03) |
| `SyncMode` | `SyncMode` | `TINYINT NOT NULL DEFAULT 0` | Disconnected by default |
| `CreatedAtUtc` | `DateTime` | `DATETIME2(3) NOT NULL` | |
| `_modules` | `List<Module>` | (child) | |

**Factory**: `static KnowledgeStructure CloneFromTemplate(KnowledgeStructureTemplate template, long projectId, long incubatorId, DateTime utcNow)`
- Deep copies Modules → Topics → Subjects → Resources.
- Stamps `SourceTemplateId = template.Id`, `SourceTemplateVersion = template.Version`.
- Every child stamps its matching `SourceTemplateXExternalId`.
- `SyncMode = Disconnected`.

**Mutations**:
- `UpdateDetails(string name, string? description)`
- `AddModule(string name, string? description, int sortOrder, DateTime utcNow) → Module` — clone-only item (no source stamp)
- `UpdateModule(...)` / `RemoveModule(...)` / `ReorderModules(...)`
- Similar Topic/Subject/Resource mutations
- `UpdateTopicPriorityRanges(Guid topicExternalId, PriorityRange? high, PriorityRange? medium, PriorityRange? low)` — emits `TopicPriorityRangesChanged` domain event (collected by `DbContext.ChangeTracker`, published after `SaveChanges`)
- `SetSyncMode(SyncMode mode)` — throws if switching to `PartialSync` with `SourceTemplateId == null`
- `ApplyPartialSync(KnowledgeStructureTemplate template)` — implements FR-K15 semantics:
  - Rejects if `SyncMode != PartialSync` or `template.Id != SourceTemplateId`
  - Depth-first traversal by ascending `SortOrder`
  - For each template item: if no descendant in clone has the matching `SourceTemplateXExternalId`, append at `max(SortOrder)+1` under the clone parent whose `SourceTemplateXExternalId` matches the template parent's `ExternalId`
  - Returns `PartialSyncResult { int ModulesAdded, int TopicsAdded, int SubjectsAdded, int ResourcesAdded }`
  - Bumps `SourceTemplateVersion = template.Version`
- `CanDeleteTopic(Guid topicExternalId)` check executed at Application layer (EC-30) — handler queries Diagnostic's `Questions` by the target project-Topic's `Id`; block if count > 0

**Invariants**:
- Topic priority-range edits validate non-overlap and `min <= max` per band.
- Clone-only items: `SourceTemplateXExternalId == null` — enforced at factory (locally-added items never receive a stamp).

### `Module` (project-clone mirror)
| Field | Type | SQL | Notes |
|---|---|---|---|
| `Id` | `long` | `BIGINT IDENTITY PK` | |
| `ExternalId` | `Guid` | `UNIQUEIDENTIFIER NOT NULL UNIQUE` | |
| `KnowledgeStructureId` | `long` | `BIGINT NOT NULL FK` | |
| `SourceTemplateModuleExternalId` | `Guid?` | `UNIQUEIDENTIFIER NULL` | Null = clone-only |
| `Name` | `string` | `NVARCHAR(200) NOT NULL` | |
| `Description` | `string?` | `NVARCHAR(2000) NULL` | |
| `SortOrder` | `int` | `INT NOT NULL` | |
| `_topics` | `List<Topic>` | (child) | |

### `Topic` (project-clone mirror)
| Field | Type | SQL | Notes |
|---|---|---|---|
| `Id` | `long` | `BIGINT IDENTITY PK` | Referenced by `diagnostic.Questions.TopicId` via cross-schema FK |
| `ExternalId` | `Guid` | `UNIQUEIDENTIFIER NOT NULL UNIQUE` | |
| `ModuleId` | `long` | `BIGINT NOT NULL FK` | |
| `SourceTemplateTopicExternalId` | `Guid?` | `UNIQUEIDENTIFIER NULL` | Null = clone-only |
| `Name` | `string` | `NVARCHAR(200) NOT NULL` | |
| `Description` | `string?` | `NVARCHAR(2000) NULL` | |
| `SortOrder` | `int` | `INT NOT NULL` | |
| `HighRangeMin` / `Max` / `MediumRangeMin` / `Max` / `LowRangeMin` / `Max` | `decimal?` | `DECIMAL(10,2) NULL` | |
| `_subjects` | `List<Subject>` | (child) | |

**Domain method**: `Priority ResolvePriority(decimal score)` — returns first matching band (High → Medium → Low) or `NotApplicable`.

### `Subject` and `Resource`
Structurally identical to template counterparts with the addition of nullable `SourceTemplateSubjectExternalId` / `SourceTemplateResourceExternalId`.

---

## Integration Events

### `TopicPriorityRangesChanged` (`Mentoory.Knowledge.Application/IntegrationEvents/`)
```
public sealed record TopicPriorityRangesChanged(
    Guid TopicExternalId,
    long ProjectId,
    PriorityRangeDto? HighRange,
    PriorityRangeDto? MediumRange,
    PriorityRangeDto? LowRange) : INotification;

public sealed record PriorityRangeDto(decimal Min, decimal Max);
```
Published by `UpdateTopicPriorityRangesHandler` (project-clone only) after successful `SaveChangesAsync`. No consumer in v1.

---

## Cross-Module Modifications

### `diagnostic.FormTemplates` — ALTER
Add column:
- `DefaultKnowledgeStructureTemplateId BIGINT NULL FK → knowledge.KnowledgeStructureTemplates(Id)`

### `diagnostic.Questions` — ALTER
Add FK constraint:
- `FK_Questions_Topics: TopicId REFERENCES knowledge.Topics(Id)` (no action cascade; deletion blocked at Application layer per EC-30)

### `Mentoory.Diagnostic.Domain.Aggregates.FormTemplate.FormTemplate` — MODIFY
- Add `DefaultKnowledgeStructureTemplateExternalId { get; private set; }` (`Guid?`)
- Add `SetDefaultKnowledgeStructureTemplate(Guid? externalId)` mutation method
- Add `ClearDefaultKnowledgeStructureTemplate()` convenience

### `Mentoory.Diagnostic.Application.Commands.CloneFormTemplate.CloneFormTemplateHandler` — MODIFY
New dependencies: `IKnowledgeStructureTemplateRepository`, `IKnowledgeStructureRepository`.
New flow (inside explicit transaction per R3):
1. Load form template with questions.
2. If `template.DefaultKnowledgeStructureTemplateExternalId is not null`:
   - Find existing `KnowledgeStructure` in target project with matching `SourceTemplateId`.
   - If none, load the `KnowledgeStructureTemplate` (with full tree) and `CloneFromTemplate` it; `Add` to repo.
   - Build `Dictionary<Guid, long>` mapping template-topic `ExternalId` → project `Topic.Id` by walking the project structure.
   - For each `Question` being cloned, if `questionTemplate.TopicId` is not a key of the map or cannot be resolved → throw `InvalidOperationException` with list of offending question texts (FR-K23; transaction rolls back).
   - Rewrite `Question.TopicId` via map during question creation.
3. Save both contexts, commit transaction.

---

## Table DDL Summary (for SSDT)

```text
Mentoory.Db/knowledge/
├── Schema.sql                          # already exists: CREATE SCHEMA [knowledge]
└── Tables/
    ├── KnowledgeStructureTemplates.sql
    ├── ModuleTemplates.sql
    ├── TopicTemplates.sql
    ├── SubjectTemplates.sql
    ├── ResourceTemplates.sql
    ├── KnowledgeStructures.sql
    ├── Modules.sql
    ├── Topics.sql
    ├── Subjects.sql
    └── Resources.sql
```

Each table follows existing SSDT conventions: PK on `Id`, unique index on `ExternalId`, FK to parent, indexes on `(ParentId, SortOrder)`. Cross-schema FK on `diagnostic.Questions.TopicId` → `knowledge.Topics(Id)` lives in `Mentoory.Db/diagnostic/Tables/Questions.sql` (ALTER).

---

## Relationship Diagram

```
KnowledgeStructureTemplate (1) ── (*) ModuleTemplate
                                         │
                                         └── (*) TopicTemplate ── (*) SubjectTemplate ── (*) ResourceTemplate
                                                     │
                                                     └── priority bands (High/Medium/Low)

KnowledgeStructure ── SourceTemplateId ── KnowledgeStructureTemplate
       │
       └── (*) Module ── (*) Topic ── (*) Subject ── (*) Resource
                              │            │            │
                              │  every clone node carries nullable
                              │  SourceTemplateXExternalId pointing at
                              │  the originating template-node ExternalId
                              │
                              └── priority bands (High/Medium/Low)

diagnostic.FormTemplate  ── DefaultKnowledgeStructureTemplateId (nullable) ── knowledge.KnowledgeStructureTemplate
diagnostic.Question      ── TopicId (FK, required) ──────────────────────── knowledge.Topic
```

---

## Validation Rules (applied by FluentValidation)

| Command | Rule |
|---|---|
| `CreateKnowledgeStructureTemplateCommand` | `Name` not empty, ≤ 200 chars; `Description` ≤ 2000 chars |
| `AddResourceTemplateCommand` / `AddResourceCommand` | `Url` must be a valid absolute URI |
| `UpdateTopicPriorityRangesCommand` | If a range is present, `Min <= Max`; no two ranges overlap; executed via `PriorityRange.Create` |
| `SetSyncModeCommand` | Target mode `PartialSync` requires `SourceTemplateId != null` |
| Reorder commands | Supplied list of ExternalIds must match exactly the current set of children |
| `CloneKnowledgeStructureTemplateCommand` | Source template not archived; target project exists |

Template-deletion guard (EC-01): implemented as an Application-layer check in the relevant delete handler (repository count query, not a validator).
