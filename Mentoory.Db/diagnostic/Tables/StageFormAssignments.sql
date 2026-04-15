CREATE TABLE [diagnostic].[StageFormAssignments]
(
    [Id] BIGINT IDENTITY(1, 1) NOT NULL,
    [ExternalId] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [ProjectId] BIGINT NOT NULL,
    [IncubatorId] BIGINT NOT NULL,
    [ProjectStageId] BIGINT NOT NULL,
    [ProjectFormId] BIGINT NOT NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [CreatedAtUtc] DATETIME2 NOT NULL,
    CONSTRAINT [PK_diagnostic_StageFormAssignments] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_StageFormAssignments_ProjectForms] FOREIGN KEY ([ProjectFormId]) REFERENCES [diagnostic].[ProjectForms] ([Id])
)
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_StageFormAssignments_ExternalId]
    ON [diagnostic].[StageFormAssignments] ([ExternalId])
GO

CREATE NONCLUSTERED INDEX [IX_StageFormAssignments_ProjectStageId]
    ON [diagnostic].[StageFormAssignments] ([ProjectStageId])
GO

CREATE NONCLUSTERED INDEX [IX_StageFormAssignments_ProjectFormId]
    ON [diagnostic].[StageFormAssignments] ([ProjectFormId])
GO

CREATE NONCLUSTERED INDEX [IX_StageFormAssignments_IncubatorId]
    ON [diagnostic].[StageFormAssignments] ([IncubatorId])
