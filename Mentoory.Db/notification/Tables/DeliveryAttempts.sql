CREATE TABLE [notification].[DeliveryAttempts]
(
    [Id]                       BIGINT          IDENTITY(1,1) NOT NULL,
    [NotificationRecipientId]  BIGINT          NOT NULL,
    [AttemptNumber]            INT             NOT NULL,
    [AttemptedAtUtc]           DATETIME2(7)    NOT NULL,
    [Success]                  BIT             NOT NULL,
    [FailureReason]            NVARCHAR(1024)  NULL,
    CONSTRAINT [PK_DeliveryAttempts] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_DeliveryAttempts_NotificationRecipients]
        FOREIGN KEY ([NotificationRecipientId]) REFERENCES [notification].[NotificationRecipients]([Id])
)
