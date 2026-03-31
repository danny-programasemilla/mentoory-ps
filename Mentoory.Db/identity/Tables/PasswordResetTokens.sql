CREATE TABLE [identity].[PasswordResetTokens]
(
    [Id] BIGINT IDENTITY(1, 1) NOT NULL,
    [UserId] BIGINT NOT NULL,
    [TokenHash] NVARCHAR(128) NOT NULL,
    [ExpiresAtUtc] DATETIME2 NOT NULL,
    [IsUsed] BIT NOT NULL DEFAULT 0,
    [CreatedAtUtc] DATETIME2 NOT NULL,
    CONSTRAINT [PK_identity_PasswordResetTokens] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_PasswordResetTokens_Users] FOREIGN KEY ([UserId]) REFERENCES [identity].[Users] ([Id])
)
