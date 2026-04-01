CREATE TABLE [diagnostic].[ProjectForms]
(
    [Id] BIGINT IDENTITY(1, 1) NOT NULL,
    [ExternalId] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [ProjectId] BIGINT NOT NULL,
    [IncubatorId] BIGINT NOT NULL,
    [SourceTemplateId] BIGINT NULL,
    [SourceTemplateVersion] INT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    [SyncMode] TINYINT NOT NULL DEFAULT 0,
    [CreatedAtUtc] DATETIME2 NOT NULL,
    CONSTRAINT [PK_diagnostic_ProjectForms] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_ProjectForms_FormTemplates] FOREIGN KEY ([SourceTemplateId]) REFERENCES [diagnostic].[FormTemplates] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [UQ_ProjectForms_ExternalId] UNIQUE ([ExternalId])
)
GO

CREATE NONCLUSTERED INDEX [IX_ProjectForms_ProjectId]
    ON [diagnostic].[ProjectForms] ([ProjectId])
GO

CREATE NONCLUSTERED INDEX [IX_ProjectForms_IncubatorId]
    ON [diagnostic].[ProjectForms] ([IncubatorId])
