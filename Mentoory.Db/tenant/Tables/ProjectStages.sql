CREATE TABLE [tenant].[ProjectStages]
(
    [Id] BIGINT IDENTITY(1, 1) NOT NULL,
    [ProjectId] BIGINT NOT NULL,
    [StageType] TINYINT NOT NULL,
    [State] TINYINT NOT NULL DEFAULT 0,
    [StartedAtUtc] DATETIME2 NULL,
    [CompletedAtUtc] DATETIME2 NULL,
    [AdvancedByUserId] BIGINT NULL,
    CONSTRAINT [PK_tenant_ProjectStages] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_ProjectStages_Projects] FOREIGN KEY ([ProjectId]) REFERENCES [tenant].[Projects] ([Id])
)
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_ProjectStages_ProjectId_StageType]
    ON [tenant].[ProjectStages] ([ProjectId], [StageType])
