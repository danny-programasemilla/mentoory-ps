CREATE TABLE [tenant].[ProjectInvitations]
(
    [Id] BIGINT IDENTITY(1, 1) NOT NULL,
    [ExternalId] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [ProjectId] BIGINT NOT NULL,
    [UserId] BIGINT NOT NULL,
    [Status] TINYINT NOT NULL DEFAULT 0,
    [ExpiresAtUtc] DATETIME2 NOT NULL,
    [AcceptedAtUtc] DATETIME2 NULL,
    [CreatedAtUtc] DATETIME2 NOT NULL,
    [CreatedByUserId] BIGINT NOT NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [RequiresAcceptance] BIT NOT NULL DEFAULT 1,
    CONSTRAINT [PK_tenant_ProjectInvitations] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_ProjectInvitations_ExternalId] UNIQUE ([ExternalId]),
    CONSTRAINT [FK_ProjectInvitations_Projects] FOREIGN KEY ([ProjectId]) REFERENCES [tenant].[Projects] ([Id])
)
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_ProjectInvitations_ActivePending]
    ON [tenant].[ProjectInvitations] ([UserId], [ProjectId])
    WHERE [IsActive] = 1 AND [Status] = 0
GO

CREATE NONCLUSTERED INDEX [IX_ProjectInvitations_ProjectStatus]
    ON [tenant].[ProjectInvitations] ([ProjectId], [Status])
    WHERE [IsActive] = 1
GO

CREATE NONCLUSTERED INDEX [IX_ProjectInvitations_UserId]
    ON [tenant].[ProjectInvitations] ([UserId])
    WHERE [IsActive] = 1
