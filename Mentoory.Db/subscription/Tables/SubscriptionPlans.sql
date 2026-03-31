CREATE TABLE [subscription].[SubscriptionPlans]
(
    [Id] BIGINT IDENTITY(1, 1) NOT NULL,
    [ExternalId] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [Name] NVARCHAR(200) NOT NULL,
    [Description] NVARCHAR(1000) NULL,
    [Version] INT NOT NULL DEFAULT 1,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [CreatedAtUtc] DATETIME2 NOT NULL,
    CONSTRAINT [PK_subscription_SubscriptionPlans] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_SubscriptionPlans_ExternalId] UNIQUE ([ExternalId])
)
