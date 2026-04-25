CREATE TABLE [knowledge].[Subjects]
(
    [Id] BIGINT IDENTITY(1, 1) NOT NULL,
    [ExternalId] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [TopicId] BIGINT NOT NULL,
    [SourceTemplateSubjectExternalId] UNIQUEIDENTIFIER NULL,
    [Name] NVARCHAR(200) NOT NULL,
    [Description] NVARCHAR(2000) NULL,
    [SortOrder] INT NOT NULL,
    CONSTRAINT [PK_knowledge_Subjects] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_Subjects_ExternalId] UNIQUE ([ExternalId]),
    CONSTRAINT [FK_Subjects_Topics] FOREIGN KEY ([TopicId]) REFERENCES [knowledge].[Topics] ([Id]) ON DELETE CASCADE
)
GO

CREATE NONCLUSTERED INDEX [IX_Subjects_TopicId_SortOrder]
    ON [knowledge].[Subjects] ([TopicId], [SortOrder])
