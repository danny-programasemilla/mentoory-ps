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
    [CorrelationId] UNIQUEIDENTIFIER NULL,
    [Outcome] NVARCHAR(20) NOT NULL CONSTRAINT [DF_AuditLog_Outcome] DEFAULT ('Success'),
    [ExceptionType] NVARCHAR(200) NULL,
    [UserEmail] NVARCHAR(256) NULL,
    [RoleContext] NVARCHAR(50) NULL,
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

CREATE NONCLUSTERED INDEX [IX_AuditLog_CorrelationId]
    ON [audit].[AuditLog] ([CorrelationId], [OccurredAtUtc] DESC)
    WHERE [CorrelationId] IS NOT NULL
GO
