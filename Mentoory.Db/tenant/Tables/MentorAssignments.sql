CREATE TABLE [tenant].[MentorAssignments]
(
    [Id] BIGINT IDENTITY(1, 1) NOT NULL,
    [ExternalId] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [ProjectId] BIGINT NOT NULL,
    [MentorUserId] BIGINT NOT NULL,
    [EntrepreneurUserId] BIGINT NOT NULL,
    [IsLeadMentor] BIT NOT NULL DEFAULT 0,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [AssignedAtUtc] DATETIME2 NOT NULL,
    CONSTRAINT [PK_tenant_MentorAssignments] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_MentorAssignments_Projects] FOREIGN KEY ([ProjectId]) REFERENCES [tenant].[Projects] ([Id]),
    CONSTRAINT [UQ_MentorAssignments_ExternalId] UNIQUE ([ExternalId])
)
GO

CREATE NONCLUSTERED INDEX [IX_MentorAssignments_ProjectId_EntrepreneurUserId]
    ON [tenant].[MentorAssignments] ([ProjectId], [EntrepreneurUserId])
