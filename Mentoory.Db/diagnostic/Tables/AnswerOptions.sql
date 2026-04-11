CREATE TABLE [diagnostic].[AnswerOptions]
(
    [Id] BIGINT IDENTITY(1, 1) NOT NULL,
    [QuestionId] BIGINT NOT NULL,
    [OptionText] NVARCHAR(500) NOT NULL,
    [Score] DECIMAL(10, 2) NOT NULL,
    [SwotClassification] TINYINT NOT NULL,
    [OdsrOrientation] TINYINT NOT NULL,
    [SortOrder] INT NOT NULL,
    CONSTRAINT [PK_diagnostic_AnswerOptions] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_AnswerOptions_Questions] FOREIGN KEY ([QuestionId]) REFERENCES [diagnostic].[Questions] ([Id]) ON DELETE CASCADE
)
GO

CREATE NONCLUSTERED INDEX [IX_AnswerOptions_QuestionId]
    ON [diagnostic].[AnswerOptions] ([QuestionId])
