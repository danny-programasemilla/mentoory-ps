CREATE TABLE [diagnostic].[DiagnosticResponses]
(
    [Id] BIGINT IDENTITY(1, 1) NOT NULL,
    [ExternalId] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [ProjectFormId] BIGINT NOT NULL,
    [ProjectId] BIGINT NOT NULL,
    [IncubatorId] BIGINT NOT NULL,
    [EntrepreneurUserId] BIGINT NOT NULL,
    [StageFormAssignmentId] BIGINT NOT NULL,
    [IsCompleted] BIT NOT NULL DEFAULT 0,
    [CompletedAtUtc] DATETIME2 NULL,
    [CreatedAtUtc] DATETIME2 NOT NULL,
    CONSTRAINT [PK_diagnostic_DiagnosticResponses] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_DiagnosticResponses_ProjectForms] FOREIGN KEY ([ProjectFormId]) REFERENCES [diagnostic].[ProjectForms] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_DiagnosticResponses_StageFormAssignments] FOREIGN KEY ([StageFormAssignmentId]) REFERENCES [diagnostic].[StageFormAssignments] ([Id]),
    CONSTRAINT [UQ_DiagnosticResponses_ExternalId] UNIQUE ([ExternalId]),
    CONSTRAINT [UQ_DiagnosticResponses_Unique] UNIQUE ([StageFormAssignmentId], [EntrepreneurUserId])
)
GO

CREATE NONCLUSTERED INDEX [IX_DiagnosticResponses_IncubatorId]
    ON [diagnostic].[DiagnosticResponses] ([IncubatorId])
