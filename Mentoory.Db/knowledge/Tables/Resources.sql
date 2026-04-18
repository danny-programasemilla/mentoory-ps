CREATE TABLE [knowledge].[Resources]
(
    [Id] BIGINT IDENTITY(1, 1) NOT NULL,
    [ExternalId] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [SubjectId] BIGINT NOT NULL,
    [SourceTemplateResourceExternalId] UNIQUEIDENTIFIER NULL,
    [Title] NVARCHAR(200) NOT NULL,
    [Description] NVARCHAR(2000) NULL,
    [Url] NVARCHAR(2000) NOT NULL,
    [ResourceType] TINYINT NOT NULL,
    [SortOrder] INT NOT NULL,
    CONSTRAINT [PK_knowledge_Resources] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_Resources_ExternalId] UNIQUE ([ExternalId]),
    CONSTRAINT [FK_Resources_Subjects] FOREIGN KEY ([SubjectId]) REFERENCES [knowledge].[Subjects] ([Id]) ON DELETE CASCADE
)
GO

CREATE NONCLUSTERED INDEX [IX_Resources_SubjectId_SortOrder]
    ON [knowledge].[Resources] ([SubjectId], [SortOrder])
