CREATE TABLE [tenant].[ProjectParticipants]
(
    [Id] BIGINT IDENTITY(1, 1) NOT NULL,
    [ExternalId] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [ProjectId] BIGINT NOT NULL,
    [UserId] BIGINT NOT NULL,
    [Role] NVARCHAR(50) NOT NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [EnrolledAtUtc] DATETIME2 NOT NULL,
    CONSTRAINT [PK_tenant_ProjectParticipants] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_ProjectParticipants_Projects] FOREIGN KEY ([ProjectId]) REFERENCES [tenant].[Projects] ([Id]),
    CONSTRAINT [UQ_ProjectParticipants_ExternalId] UNIQUE ([ExternalId])
)
GO

CREATE NONCLUSTERED INDEX [IX_ProjectParticipants_ProjectId_Role]
    ON [tenant].[ProjectParticipants] ([ProjectId], [Role])
GO

CREATE UNIQUE NONCLUSTERED INDEX [UQ_ProjectParticipants_Entrepreneur]
    ON [tenant].[ProjectParticipants] ([ProjectId], [UserId])
    WHERE [Role] = 'Entrepreneur' AND [IsActive] = 1
