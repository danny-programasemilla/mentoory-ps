CREATE TABLE [diagnostic].[QuestionResponses]
(
    [Id] BIGINT IDENTITY(1, 1) NOT NULL,
    [DiagnosticResponseId] BIGINT NOT NULL,
    [QuestionId] BIGINT NOT NULL,
    [TextValue] NVARCHAR(4000) NULL,
    [NumericValue] DECIMAL(10, 2) NULL,
    [SelectedOptionIds] NVARCHAR(500) NULL,
    [CreatedAtUtc] DATETIME2 NOT NULL,
    CONSTRAINT [PK_diagnostic_QuestionResponses] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_QuestionResponses_DiagnosticResponses] FOREIGN KEY ([DiagnosticResponseId]) REFERENCES [diagnostic].[DiagnosticResponses] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_QuestionResponses_Questions] FOREIGN KEY ([QuestionId]) REFERENCES [diagnostic].[Questions] ([Id])
)
GO

CREATE NONCLUSTERED INDEX [IX_QuestionResponses_DiagnosticResponseId]
    ON [diagnostic].[QuestionResponses] ([DiagnosticResponseId])
