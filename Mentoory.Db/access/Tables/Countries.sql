CREATE TABLE [access].[Countries]
(
    [Id] BIGINT IDENTITY(1, 1) NOT NULL,
    [ExternalId] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [Name] NVARCHAR(100) NOT NULL,
    [Code] NVARCHAR(3) NOT NULL,
    [IdentificationLabel] NVARCHAR(100) NOT NULL,
    [IdentificationMask] NVARCHAR(50) NULL,
    [IdentificationRegex] NVARCHAR(200) NULL,
    [IdentificationMaxLength] INT NOT NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [CreatedAtUtc] DATETIME2 NOT NULL,
    CONSTRAINT [PK_access_Countries] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_Countries_ExternalId] UNIQUE ([ExternalId]),
    CONSTRAINT [UQ_Countries_Code] UNIQUE ([Code])
)
