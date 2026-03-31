CREATE TABLE [authorization].[RoleAssignments]
(
    [Id] BIGINT IDENTITY(1, 1) NOT NULL,
    [ExternalId] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [UserId] BIGINT NOT NULL,
    [IncubatorId] BIGINT NOT NULL,
    [ProjectId] BIGINT NULL,
    [Role] NVARCHAR(50) NOT NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [CreatedAtUtc] DATETIME2 NOT NULL,
    [UpdatedAtUtc] DATETIME2 NOT NULL,
    CONSTRAINT [PK_authorization_RoleAssignments] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_RoleAssignments_ExternalId] UNIQUE ([ExternalId])
)
GO

CREATE NONCLUSTERED INDEX [IX_RoleAssignments_UserId_IsActive]
    ON [authorization].[RoleAssignments] ([UserId], [IsActive])
    INCLUDE ([IncubatorId], [ProjectId], [Role])
GO

CREATE NONCLUSTERED INDEX [IX_RoleAssignments_IncubatorId_ProjectId_Role]
    ON [authorization].[RoleAssignments] ([IncubatorId], [ProjectId], [Role])
GO

CREATE UNIQUE NONCLUSTERED INDEX [UQ_RoleAssignments_Unique]
    ON [authorization].[RoleAssignments] ([UserId], [IncubatorId], [ProjectId], [Role])
    WHERE [IsActive] = 1
