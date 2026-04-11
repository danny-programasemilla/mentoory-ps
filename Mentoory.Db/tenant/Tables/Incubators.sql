CREATE TABLE [tenant].[Incubators]
(
    [Id] BIGINT IDENTITY(1, 1) NOT NULL,
    [ExternalId] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [Name] NVARCHAR(200) NOT NULL,
    [Description] NVARCHAR(1000) NULL,
    [SubscriptionPlanId] BIGINT NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [CreatedAtUtc] DATETIME2 NOT NULL,
    [UpdatedAtUtc] DATETIME2 NOT NULL,
    CONSTRAINT [PK_tenant_Incubators] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_Incubators_ExternalId] UNIQUE ([ExternalId])
)
