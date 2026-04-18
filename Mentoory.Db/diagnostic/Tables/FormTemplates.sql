CREATE TABLE [diagnostic].[FormTemplates]
(
    [Id] BIGINT IDENTITY(1, 1) NOT NULL,
    [ExternalId] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [Name] NVARCHAR(200) NOT NULL,
    [Description] NVARCHAR(1000) NULL,
    [SubscriptionTier] NVARCHAR(50) NULL,
    [Version] INT NOT NULL DEFAULT 1,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [CreatedAtUtc] DATETIME2 NOT NULL,
    [DefaultKnowledgeStructureTemplateId] BIGINT NULL,
    CONSTRAINT [PK_diagnostic_FormTemplates] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_FormTemplates_ExternalId] UNIQUE ([ExternalId]),
    CONSTRAINT [FK_FormTemplates_DefaultKnowledgeStructureTemplate] FOREIGN KEY ([DefaultKnowledgeStructureTemplateId]) REFERENCES [knowledge].[KnowledgeStructureTemplates] ([Id]) ON DELETE SET NULL
)
GO

CREATE NONCLUSTERED INDEX [IX_FormTemplates_DefaultKnowledgeStructureTemplateId]
    ON [diagnostic].[FormTemplates] ([DefaultKnowledgeStructureTemplateId])
