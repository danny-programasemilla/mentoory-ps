CREATE TABLE [knowledge].[Modules]
(
    [Id] BIGINT IDENTITY(1, 1) NOT NULL,
    [ExternalId] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [KnowledgeStructureId] BIGINT NOT NULL,
    [SourceTemplateModuleExternalId] UNIQUEIDENTIFIER NULL,
    [Name] NVARCHAR(200) NOT NULL,
    [Description] NVARCHAR(2000) NULL,
    [SortOrder] INT NOT NULL,
    CONSTRAINT [PK_knowledge_Modules] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_Modules_ExternalId] UNIQUE ([ExternalId]),
    CONSTRAINT [FK_Modules_KnowledgeStructures] FOREIGN KEY ([KnowledgeStructureId]) REFERENCES [knowledge].[KnowledgeStructures] ([Id]) ON DELETE CASCADE
)
GO

CREATE NONCLUSTERED INDEX [IX_Modules_KnowledgeStructureId_SortOrder]
    ON [knowledge].[Modules] ([KnowledgeStructureId], [SortOrder])
