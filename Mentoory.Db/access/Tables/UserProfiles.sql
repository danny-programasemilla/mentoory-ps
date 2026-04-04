CREATE TABLE [access].[UserProfiles]
(
    [Id] BIGINT IDENTITY(1, 1) NOT NULL,
    [UserId] BIGINT NOT NULL,
    [UserExternalId] UNIQUEIDENTIFIER NOT NULL,
    [Email] NVARCHAR(256) NOT NULL,
    [FirstName] NVARCHAR(100) NOT NULL,
    [LastName] NVARCHAR(100) NOT NULL,
    [AccountStatus] NVARCHAR(50) NOT NULL,
    [CreatedAtUtc] DATETIME2 NOT NULL,
    [LastSyncedAtUtc] DATETIME2 NOT NULL,
    CONSTRAINT [PK_authorization_UserProfiles] PRIMARY KEY CLUSTERED ([Id] ASC)
)
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_UserProfiles_UserId]
    ON [access].[UserProfiles] ([UserId])
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_UserProfiles_UserExternalId]
    ON [access].[UserProfiles] ([UserExternalId])
