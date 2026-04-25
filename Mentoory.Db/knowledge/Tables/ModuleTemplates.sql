CREATE TABLE [knowledge].[ModuleTemplates]
(
    [Id] BIGINT IDENTITY(1, 1) NOT NULL,
    [ExternalId] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [KnowledgeStructureTemplateId] BIGINT NOT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    [Description] NVARCHAR(2000) NULL,
    [SortOrder] INT NOT NULL,
    CONSTRAINT [PK_knowledge_ModuleTemplates] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_ModuleTemplates_ExternalId] UNIQUE ([ExternalId]),
    CONSTRAINT [FK_ModuleTemplates_KnowledgeStructureTemplates] FOREIGN KEY ([KnowledgeStructureTemplateId]) REFERENCES [knowledge].[KnowledgeStructureTemplates] ([Id]) ON DELETE CASCADE
)
GO

CREATE NONCLUSTERED INDEX [IX_ModuleTemplates_KnowledgeStructureTemplateId_SortOrder]
    ON [knowledge].[ModuleTemplates] ([KnowledgeStructureTemplateId], [SortOrder])
