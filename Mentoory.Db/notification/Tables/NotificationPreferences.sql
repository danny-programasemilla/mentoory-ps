CREATE TABLE [notification].[NotificationPreferences]
(
    [Id]               BIGINT           IDENTITY(1,1) NOT NULL,
    [ExternalId]       UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [UserId]           BIGINT           NOT NULL,
    [NotificationType] TINYINT          NOT NULL,
    [IsEnabled]        BIT              NOT NULL,
    [UpdatedAtUtc]     DATETIME2(7)     NOT NULL,
    CONSTRAINT [PK_NotificationPreferences] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [UQ_NotificationPreferences_ExternalId] UNIQUE ([ExternalId]),
    CONSTRAINT [UQ_NotificationPreferences_User_Type] UNIQUE ([UserId], [NotificationType])
)
