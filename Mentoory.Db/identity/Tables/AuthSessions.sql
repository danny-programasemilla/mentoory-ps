CREATE TABLE [identity].[AuthSessions]
(
    [Id] BIGINT IDENTITY(1, 1) NOT NULL,
    [SessionToken] NVARCHAR(100) NOT NULL,
    [UserId] BIGINT NOT NULL,
    [IpAddress] NVARCHAR(45) NOT NULL,
    [UserAgent] NVARCHAR(500) NULL,
    [CreatedAtUtc] DATETIME2 NOT NULL,
    [LastActivityUtc] DATETIME2 NOT NULL,
    [ExpiresAtUtc] DATETIME2 NOT NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [ActiveIncubatorId] BIGINT NULL,
    [ActiveProjectId] BIGINT NULL,
    [ActiveRole] NVARCHAR(50) NULL,
    CONSTRAINT [PK_identity_AuthSessions] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_AuthSessions_Users] FOREIGN KEY ([UserId]) REFERENCES [identity].[Users] ([Id])
)
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_AuthSessions_SessionToken]
    ON [identity].[AuthSessions] ([SessionToken])
    WHERE [IsActive] = 1
GO

CREATE NONCLUSTERED INDEX [IX_AuthSessions_UserId_IsActive]
    ON [identity].[AuthSessions] ([UserId], [IsActive])
