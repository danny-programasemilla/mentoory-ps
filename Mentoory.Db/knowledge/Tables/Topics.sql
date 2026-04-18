CREATE TABLE [knowledge].[Topics]
(
    [Id] BIGINT IDENTITY(1, 1) NOT NULL,
    [ExternalId] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [ModuleId] BIGINT NOT NULL,
    [SourceTemplateTopicExternalId] UNIQUEIDENTIFIER NULL,
    [Name] NVARCHAR(200) NOT NULL,
    [Description] NVARCHAR(2000) NULL,
    [SortOrder] INT NOT NULL,
    [HighRangeMin] DECIMAL(10, 2) NULL,
    [HighRangeMax] DECIMAL(10, 2) NULL,
    [MediumRangeMin] DECIMAL(10, 2) NULL,
    [MediumRangeMax] DECIMAL(10, 2) NULL,
    [LowRangeMin] DECIMAL(10, 2) NULL,
    [LowRangeMax] DECIMAL(10, 2) NULL,
    CONSTRAINT [PK_knowledge_Topics] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_Topics_ExternalId] UNIQUE ([ExternalId]),
    CONSTRAINT [FK_Topics_Modules] FOREIGN KEY ([ModuleId]) REFERENCES [knowledge].[Modules] ([Id]) ON DELETE CASCADE
)
GO

CREATE NONCLUSTERED INDEX [IX_Topics_ModuleId_SortOrder]
    ON [knowledge].[Topics] ([ModuleId], [SortOrder])
