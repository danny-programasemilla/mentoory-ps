CREATE TABLE [notification].[NotificationRecipients]
(
    [Id]              BIGINT           IDENTITY(1,1) NOT NULL,
    [NotificationId]  BIGINT           NOT NULL,
    [UserId]          BIGINT           NOT NULL,
    [Email]           NVARCHAR(256)    NOT NULL,
    [DeliveryChannel] TINYINT          NOT NULL,
    [DeliveryStatus]  TINYINT          NOT NULL,
    [SentAtUtc]       DATETIME2(7)     NULL,
    [FailureReason]   NVARCHAR(1024)   NULL,
    CONSTRAINT [PK_NotificationRecipients] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_NotificationRecipients_Notifications]
        FOREIGN KEY ([NotificationId]) REFERENCES [notification].[Notifications]([Id])
)
GO

CREATE NONCLUSTERED INDEX [IX_NotificationRecipients_Status]
    ON [notification].[NotificationRecipients] ([DeliveryStatus])
    INCLUDE ([NotificationId], [SentAtUtc])
    WHERE [DeliveryStatus] = 0
