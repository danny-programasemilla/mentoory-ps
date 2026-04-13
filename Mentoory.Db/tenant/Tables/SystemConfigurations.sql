CREATE TABLE [tenant].[SystemConfigurations]
(
    [Id] BIGINT IDENTITY(1, 1) NOT NULL,
    [ExternalId] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [Key] NVARCHAR(100) NOT NULL,
    [Value] NVARCHAR(500) NOT NULL,
    [Description] NVARCHAR(500) NULL,
    [DataType] NVARCHAR(50) NOT NULL,
    [CreatedAtUtc] DATETIME2 NOT NULL,
    [UpdatedAtUtc] DATETIME2 NOT NULL,
    CONSTRAINT [PK_tenant_SystemConfigurations] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_tenant_SystemConfigurations_ExternalId] UNIQUE ([ExternalId]),
    CONSTRAINT [UQ_tenant_SystemConfigurations_Key] UNIQUE ([Key])
)
