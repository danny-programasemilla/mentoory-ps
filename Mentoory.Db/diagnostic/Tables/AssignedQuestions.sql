CREATE TABLE [diagnostic].[AssignedQuestions]
(
    [Id] BIGINT IDENTITY(1, 1) NOT NULL,
    [StageFormAssignmentId] BIGINT NOT NULL,
    [QuestionId] BIGINT NOT NULL,
    [SortOrder] INT NOT NULL,
    CONSTRAINT [PK_diagnostic_AssignedQuestions] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_AssignedQuestions_StageFormAssignments] FOREIGN KEY ([StageFormAssignmentId]) REFERENCES [diagnostic].[StageFormAssignments] ([Id]) ON DELETE CASCADE
)
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_AssignedQuestions_StageFormAssignmentId_QuestionId]
    ON [diagnostic].[AssignedQuestions] ([StageFormAssignmentId], [QuestionId])
GO

CREATE NONCLUSTERED INDEX [IX_AssignedQuestions_QuestionId]
    ON [diagnostic].[AssignedQuestions] ([QuestionId])
