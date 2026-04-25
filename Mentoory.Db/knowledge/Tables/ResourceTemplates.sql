CREATE TABLE [knowledge].[ResourceTemplates]
(
    [Id] BIGINT IDENTITY(1, 1) NOT NULL,
    [ExternalId] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [SubjectTemplateId] BIGINT NOT NULL,
    [Title] NVARCHAR(200) NOT NULL,
    [Description] NVARCHAR(2000) NULL,
    [Url] NVARCHAR(2000) NOT NULL,
    [ResourceType] TINYINT NOT NULL,
    [SortOrder] INT NOT NULL,
    CONSTRAINT [PK_knowledge_ResourceTemplates] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_ResourceTemplates_ExternalId] UNIQUE ([ExternalId]),
    CONSTRAINT [FK_ResourceTemplates_SubjectTemplates] FOREIGN KEY ([SubjectTemplateId]) REFERENCES [knowledge].[SubjectTemplates] ([Id]) ON DELETE CASCADE
)
GO

CREATE NONCLUSTERED INDEX [IX_ResourceTemplates_SubjectTemplateId_SortOrder]
    ON [knowledge].[ResourceTemplates] ([SubjectTemplateId], [SortOrder])
