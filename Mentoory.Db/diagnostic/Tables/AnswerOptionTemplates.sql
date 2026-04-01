CREATE TABLE [diagnostic].[AnswerOptionTemplates]
(
    [Id] BIGINT IDENTITY(1, 1) NOT NULL,
    [QuestionTemplateId] BIGINT NOT NULL,
    [OptionText] NVARCHAR(500) NOT NULL,
    [Score] DECIMAL(10, 2) NOT NULL,
    [SwotClassification] TINYINT NOT NULL,
    [OdsrOrientation] TINYINT NOT NULL,
    [SortOrder] INT NOT NULL,
    CONSTRAINT [PK_diagnostic_AnswerOptionTemplates] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_AnswerOptionTemplates_QuestionTemplates] FOREIGN KEY ([QuestionTemplateId]) REFERENCES [diagnostic].[QuestionTemplates] ([Id]) ON DELETE CASCADE
)
GO

CREATE NONCLUSTERED INDEX [IX_AnswerOptionTemplates_QuestionTemplateId]
    ON [diagnostic].[AnswerOptionTemplates] ([QuestionTemplateId])
