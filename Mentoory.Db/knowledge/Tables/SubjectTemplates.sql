CREATE TABLE [knowledge].[SubjectTemplates]
(
    [Id] BIGINT IDENTITY(1, 1) NOT NULL,
    [ExternalId] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [TopicTemplateId] BIGINT NOT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    [Description] NVARCHAR(2000) NULL,
    [SortOrder] INT NOT NULL,
    CONSTRAINT [PK_knowledge_SubjectTemplates] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_SubjectTemplates_ExternalId] UNIQUE ([ExternalId]),
    CONSTRAINT [FK_SubjectTemplates_TopicTemplates] FOREIGN KEY ([TopicTemplateId]) REFERENCES [knowledge].[TopicTemplates] ([Id]) ON DELETE CASCADE
)
GO

CREATE NONCLUSTERED INDEX [IX_SubjectTemplates_TopicTemplateId_SortOrder]
    ON [knowledge].[SubjectTemplates] ([TopicTemplateId], [SortOrder])
