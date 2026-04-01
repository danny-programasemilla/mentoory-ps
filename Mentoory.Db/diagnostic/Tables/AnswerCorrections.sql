CREATE TABLE [diagnostic].[AnswerCorrections]
(
    [Id] BIGINT IDENTITY(1, 1) NOT NULL,
    [QuestionResponseId] BIGINT NOT NULL,
    [PreviousTextValue] NVARCHAR(4000) NULL,
    [PreviousNumericValue] DECIMAL(10, 2) NULL,
    [PreviousSelectedOptionIds] NVARCHAR(500) NULL,
    [CorrectedByUserId] BIGINT NOT NULL,
    [CorrectedAtUtc] DATETIME2 NOT NULL,
    [Reason] NVARCHAR(500) NULL,
    CONSTRAINT [PK_diagnostic_AnswerCorrections] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_AnswerCorrections_QuestionResponses] FOREIGN KEY ([QuestionResponseId]) REFERENCES [diagnostic].[QuestionResponses] ([Id]) ON DELETE CASCADE
)
GO

CREATE NONCLUSTERED INDEX [IX_AnswerCorrections_QuestionResponseId]
    ON [diagnostic].[AnswerCorrections] ([QuestionResponseId])
