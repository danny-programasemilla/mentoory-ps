CREATE TABLE [diagnostic].[QuestionTemplates]
(
    [Id] BIGINT IDENTITY(1, 1) NOT NULL,
    [FormTemplateId] BIGINT NOT NULL,
    [TopicId] BIGINT NOT NULL,
    [QuestionText] NVARCHAR(2000) NOT NULL,
    [QuestionType] TINYINT NOT NULL,
    [StageApplicability] TINYINT NOT NULL,
    [SortOrder] INT NOT NULL,
    [BlockGroup] NVARCHAR(100) NULL,
    [IsOptional] BIT NOT NULL DEFAULT 0,
    CONSTRAINT [PK_diagnostic_QuestionTemplates] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_QuestionTemplates_FormTemplates] FOREIGN KEY ([FormTemplateId]) REFERENCES [diagnostic].[FormTemplates] ([Id]) ON DELETE CASCADE
)
GO

CREATE NONCLUSTERED INDEX [IX_QuestionTemplates_FormTemplateId]
    ON [diagnostic].[QuestionTemplates] ([FormTemplateId])
