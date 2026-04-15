CREATE TABLE [tenant].[ProjectStages]
(
    [Id] BIGINT IDENTITY(1, 1) NOT NULL,
    [ProjectId] BIGINT NOT NULL,
    [ExternalId] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [StageType] TINYINT NOT NULL,
    [State] TINYINT NOT NULL DEFAULT 0,
    [Position] INT NOT NULL,
    [DisplayName] NVARCHAR(200) NOT NULL,
    [PlannedStartDate] DATETIME2 NULL,
    [PlannedEndDate] DATETIME2 NULL,
    [StartedAtUtc] DATETIME2 NULL,
    [CompletedAtUtc] DATETIME2 NULL,
    [AdvancedByUserId] BIGINT NULL,
    CONSTRAINT [PK_tenant_ProjectStages] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_ProjectStages_Projects] FOREIGN KEY ([ProjectId]) REFERENCES [tenant].[Projects] ([Id])
)
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_ProjectStages_ExternalId]
    ON [tenant].[ProjectStages] ([ExternalId])
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_ProjectStages_ProjectId_Position]
    ON [tenant].[ProjectStages] ([ProjectId], [Position])
