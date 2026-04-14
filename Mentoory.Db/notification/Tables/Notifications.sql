CREATE TABLE [notification].[Notifications]
(
    [Id]               BIGINT            IDENTITY(1,1) NOT NULL,
    [ExternalId]       UNIQUEIDENTIFIER  NOT NULL DEFAULT NEWID(),
    [NotificationType] TINYINT           NOT NULL,
    [Subject]          NVARCHAR(256)     NOT NULL,
    [HtmlBody]         NVARCHAR(MAX)     NOT NULL,
    [SourceEventId]    UNIQUEIDENTIFIER  NULL,
    [ScheduledForUtc]  DATETIME2(7)      NOT NULL,
    [CreatedAtUtc]     DATETIME2(7)      NOT NULL,
    [LoginContext_IpAddress]       NVARCHAR(45)   NULL,
    [LoginContext_BrowserName]     NVARCHAR(128)  NULL,
    [LoginContext_OperatingSystem] NVARCHAR(128)  NULL,
    [LoginContext_IsSuspicious]    BIT            NULL,
    CONSTRAINT [PK_Notifications] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [UQ_Notifications_ExternalId] UNIQUE ([ExternalId])
);

CREATE UNIQUE NONCLUSTERED INDEX [IX_Notifications_SourceEventId_Type]
    ON [notification].[Notifications] ([SourceEventId], [NotificationType])
    WHERE [SourceEventId] IS NOT NULL;

CREATE NONCLUSTERED INDEX [IX_Notifications_ScheduledForUtc]
    ON [notification].[Notifications] ([ScheduledForUtc])
    INCLUDE ([NotificationType]);
