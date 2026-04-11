CREATE TABLE [audit].[AuditLog]
(
    [Id] BIGINT IDENTITY(1, 1) NOT NULL,
    [EventType] NVARCHAR(100) NOT NULL,
    [UserId] BIGINT NULL,
    [IncubatorId] BIGINT NULL,
    [ProjectId] BIGINT NULL,
    [EntityType] NVARCHAR(100) NULL,
    [EntityId] NVARCHAR(100) NULL,
    [Action] NVARCHAR(50) NOT NULL,
    [Details] NVARCHAR(MAX) NULL,
    [IpAddress] NVARCHAR(45) NULL,
    [OccurredAtUtc] DATETIME2 NOT NULL,
    CONSTRAINT [PK_audit_AuditLog] PRIMARY KEY CLUSTERED ([Id] ASC)
)
GO

CREATE NONCLUSTERED INDEX [IX_AuditLog_EventType_OccurredAtUtc]
    ON [audit].[AuditLog] ([EventType], [OccurredAtUtc] DESC)
GO

CREATE NONCLUSTERED INDEX [IX_AuditLog_UserId_OccurredAtUtc]
    ON [audit].[AuditLog] ([UserId], [OccurredAtUtc] DESC)
GO

CREATE NONCLUSTERED INDEX [IX_AuditLog_EntityType_EntityId]
    ON [audit].[AuditLog] ([EntityType], [EntityId])
GO
