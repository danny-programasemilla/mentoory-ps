CREATE TABLE [access].[Users]
(
    [Id] BIGINT IDENTITY(1, 1) NOT NULL,
    [ExternalId] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [Email] NVARCHAR(256) NOT NULL,
    [NormalizedEmail] NVARCHAR(256) NOT NULL,
    [Country] NVARCHAR(100) NOT NULL,
    [NationalId] NVARCHAR(50) NOT NULL,
    [FirstName] NVARCHAR(100) NOT NULL,
    [LastName] NVARCHAR(100) NOT NULL,
    [AccountStatus] TINYINT NOT NULL DEFAULT 0,
    [FailedLoginAttempts] INT NOT NULL DEFAULT 0,
    [LockoutEndUtc] DATETIME2 NULL,
    [EmailVerifiedAtUtc] DATETIME2 NULL,
    [CreatedAtUtc] DATETIME2 NOT NULL,
    [UpdatedAtUtc] DATETIME2 NOT NULL,
    CONSTRAINT [PK_identity_Users] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_Users_ExternalId] UNIQUE ([ExternalId]),
    CONSTRAINT [UQ_Users_NormalizedEmail] UNIQUE ([NormalizedEmail]),
    CONSTRAINT [UQ_Users_Country_NationalId] UNIQUE ([Country], [NationalId])
)
