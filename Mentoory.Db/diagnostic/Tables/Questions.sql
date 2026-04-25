CREATE TABLE [diagnostic].[Questions]
(
    [Id] BIGINT IDENTITY(1, 1) NOT NULL,
    [ExternalId] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [ProjectFormId] BIGINT NOT NULL,
    [TopicId] BIGINT NOT NULL,
    [QuestionText] NVARCHAR(2000) NOT NULL,
    [QuestionType] TINYINT NOT NULL,
    [StageApplicability] TINYINT NOT NULL,
    [SortOrder] INT NOT NULL,
    [BlockGroup] NVARCHAR(100) NULL,
    [IsOptional] BIT NOT NULL DEFAULT 0,
    CONSTRAINT [PK_diagnostic_Questions] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Questions_ProjectForms] FOREIGN KEY ([ProjectFormId]) REFERENCES [diagnostic].[ProjectForms] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Questions_Topics] FOREIGN KEY ([TopicId]) REFERENCES [knowledge].[Topics] ([Id]),
    CONSTRAINT [UQ_Questions_ExternalId] UNIQUE ([ExternalId])
)
GO

CREATE NONCLUSTERED INDEX [IX_Questions_ProjectFormId]
    ON [diagnostic].[Questions] ([ProjectFormId])
GO

CREATE NONCLUSTERED INDEX [IX_Questions_TopicId]
    ON [diagnostic].[Questions] ([TopicId])
