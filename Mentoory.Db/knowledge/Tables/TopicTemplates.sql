CREATE TABLE [knowledge].[TopicTemplates]
(
    [Id] BIGINT IDENTITY(1, 1) NOT NULL,
    [ExternalId] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [ModuleTemplateId] BIGINT NOT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    [Description] NVARCHAR(2000) NULL,
    [SortOrder] INT NOT NULL,
    [HighRangeMin] DECIMAL(10, 2) NULL,
    [HighRangeMax] DECIMAL(10, 2) NULL,
    [MediumRangeMin] DECIMAL(10, 2) NULL,
    [MediumRangeMax] DECIMAL(10, 2) NULL,
    [LowRangeMin] DECIMAL(10, 2) NULL,
    [LowRangeMax] DECIMAL(10, 2) NULL,
    CONSTRAINT [PK_knowledge_TopicTemplates] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_TopicTemplates_ExternalId] UNIQUE ([ExternalId]),
    CONSTRAINT [FK_TopicTemplates_ModuleTemplates] FOREIGN KEY ([ModuleTemplateId]) REFERENCES [knowledge].[ModuleTemplates] ([Id]) ON DELETE CASCADE
)
GO

CREATE NONCLUSTERED INDEX [IX_TopicTemplates_ModuleTemplateId_SortOrder]
    ON [knowledge].[TopicTemplates] ([ModuleTemplateId], [SortOrder])
