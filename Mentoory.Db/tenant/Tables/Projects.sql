CREATE TABLE [tenant].[Projects]
(
    [Id] BIGINT IDENTITY(1, 1) NOT NULL,
    [ExternalId] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [IncubatorId] BIGINT NOT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    [Description] NVARCHAR(1000) NULL,
    [KnowledgeStructureTemplateExternalId] UNIQUEIDENTIFIER NOT NULL,
    [CurrentStageType] TINYINT NOT NULL DEFAULT 0,
    [CurrentStageState] TINYINT NOT NULL DEFAULT 0,
    [IsPublic] BIT NOT NULL DEFAULT 0,
    [EnrollmentVariant] TINYINT NOT NULL DEFAULT 0,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [CreatedAtUtc] DATETIME2 NOT NULL,
    [UpdatedAtUtc] DATETIME2 NOT NULL,
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_tenant_Projects] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Projects_Incubators] FOREIGN KEY ([IncubatorId]) REFERENCES [tenant].[Incubators] ([Id]),
    CONSTRAINT [FK_Projects_KnowledgeStructureTemplate] FOREIGN KEY ([KnowledgeStructureTemplateExternalId]) REFERENCES [knowledge].[KnowledgeStructureTemplates] ([ExternalId]),
    CONSTRAINT [UQ_Projects_ExternalId] UNIQUE ([ExternalId])
)
GO

CREATE NONCLUSTERED INDEX [IX_Projects_IncubatorId]
    ON [tenant].[Projects] ([IncubatorId])
GO

CREATE NONCLUSTERED INDEX [IX_Projects_KnowledgeStructureTemplateExternalId]
    ON [tenant].[Projects] ([KnowledgeStructureTemplateExternalId])
