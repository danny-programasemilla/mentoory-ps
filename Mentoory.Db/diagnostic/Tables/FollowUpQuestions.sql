CREATE TABLE [diagnostic].[FollowUpQuestions]
(
    [Id] BIGINT IDENTITY(1, 1) NOT NULL,
    [QuestionId] BIGINT NOT NULL,
    [QuestionText] NVARCHAR(2000) NOT NULL,
    [SortOrder] INT NOT NULL,
    CONSTRAINT [PK_diagnostic_FollowUpQuestions] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_FollowUpQuestions_Questions] FOREIGN KEY ([QuestionId]) REFERENCES [diagnostic].[Questions] ([Id]) ON DELETE CASCADE
)
GO

CREATE NONCLUSTERED INDEX [IX_FollowUpQuestions_QuestionId]
    ON [diagnostic].[FollowUpQuestions] ([QuestionId])
