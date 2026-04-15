CREATE TABLE [notification].[NotificationConfigurations]
(
    [Id] BIGINT IDENTITY(1, 1) NOT NULL,
    [ExternalId] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [Key] NVARCHAR(100) NOT NULL,
    [Value] NVARCHAR(500) NOT NULL,
    [Description] NVARCHAR(500) NULL,
    [DataType] NVARCHAR(50) NOT NULL,
    [CreatedAtUtc] DATETIME2 NOT NULL,
    [UpdatedAtUtc] DATETIME2 NOT NULL,
    CONSTRAINT [PK_notification_NotificationConfigurations] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_NotificationConfigurations_ExternalId] UNIQUE ([ExternalId]),
    CONSTRAINT [UQ_NotificationConfigurations_Key] UNIQUE ([Key])
)
