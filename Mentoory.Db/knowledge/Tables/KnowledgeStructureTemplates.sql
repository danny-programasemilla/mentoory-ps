CREATE TABLE [knowledge].[KnowledgeStructureTemplates]
(
    [Id] BIGINT IDENTITY(1, 1) NOT NULL,
    [ExternalId] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [Name] NVARCHAR(200) NOT NULL,
    [Description] NVARCHAR(2000) NULL,
    [IsArchived] BIT NOT NULL DEFAULT 0,
    [Version] INT NOT NULL DEFAULT 1,
    [CreatedAtUtc] DATETIME2(3) NOT NULL,
    CONSTRAINT [PK_knowledge_KnowledgeStructureTemplates] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_KnowledgeStructureTemplates_ExternalId] UNIQUE ([ExternalId])
)
