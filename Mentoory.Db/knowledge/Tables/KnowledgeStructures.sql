CREATE TABLE [knowledge].[KnowledgeStructures]
(
    [Id] BIGINT IDENTITY(1, 1) NOT NULL,
    [ExternalId] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [ProjectId] BIGINT NOT NULL,
    [IncubatorId] BIGINT NOT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    [Description] NVARCHAR(2000) NULL,
    [SourceTemplateId] BIGINT NULL,
    [SourceTemplateVersion] INT NULL,
    [SyncMode] TINYINT NOT NULL DEFAULT 0,
    [CreatedAtUtc] DATETIME2(3) NOT NULL,
    CONSTRAINT [PK_knowledge_KnowledgeStructures] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_KnowledgeStructures_ExternalId] UNIQUE ([ExternalId]),
    CONSTRAINT [FK_KnowledgeStructures_KnowledgeStructureTemplates] FOREIGN KEY ([SourceTemplateId]) REFERENCES [knowledge].[KnowledgeStructureTemplates] ([Id]) ON DELETE SET NULL
)
GO

CREATE NONCLUSTERED INDEX [IX_KnowledgeStructures_ProjectId]
    ON [knowledge].[KnowledgeStructures] ([ProjectId])
GO

CREATE NONCLUSTERED INDEX [IX_KnowledgeStructures_IncubatorId]
    ON [knowledge].[KnowledgeStructures] ([IncubatorId])
GO

CREATE NONCLUSTERED INDEX [IX_KnowledgeStructures_ProjectId_SourceTemplateId]
    ON [knowledge].[KnowledgeStructures] ([ProjectId], [SourceTemplateId])
