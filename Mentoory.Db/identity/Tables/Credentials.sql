CREATE TABLE [identity].[Credentials]
(
    [Id] BIGINT IDENTITY(1, 1) NOT NULL,
    [UserId] BIGINT NOT NULL,
    [PasswordHash] NVARCHAR(500) NOT NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [CreatedAtUtc] DATETIME2 NOT NULL,
    CONSTRAINT [PK_identity_Credentials] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Credentials_Users] FOREIGN KEY ([UserId]) REFERENCES [identity].[Users] ([Id])
)
GO

CREATE NONCLUSTERED INDEX [IX_Credentials_UserId_IsActive]
    ON [identity].[Credentials] ([UserId], [IsActive])
    INCLUDE ([PasswordHash])
    WHERE [IsActive] = 1
