-- ==========================================================================================
-- 005.SeedUserProfiles.sql
-- Bootstraps the authorization.UserProfiles read model from identity.Users.
-- Fully idempotent: uses MERGE to insert new profiles and update existing ones.
-- Must run AFTER user seed scripts (002, 004).
-- ==========================================================================================

SET NOCOUNT ON;

DECLARE @SyncedAt DATETIME2 = GETUTCDATE();

MERGE [authorization].[UserProfiles] AS target
USING (
    SELECT
        u.[Id]          AS [UserId],
        u.[ExternalId]  AS [UserExternalId],
        u.[Email],
        u.[FirstName],
        u.[LastName],
        CASE u.[AccountStatus]
            WHEN 0 THEN N'PendingVerification'
            WHEN 1 THEN N'Active'
            WHEN 2 THEN N'Locked'
            WHEN 3 THEN N'Disabled'
            WHEN 4 THEN N'PasswordResetRequired'
            ELSE N'Unknown'
        END             AS [AccountStatus],
        u.[CreatedAtUtc]
    FROM [identity].[Users] u
) AS source
ON target.[UserId] = source.[UserId]
WHEN MATCHED THEN
    UPDATE SET
        target.[Email]          = source.[Email],
        target.[FirstName]      = source.[FirstName],
        target.[LastName]       = source.[LastName],
        target.[AccountStatus]  = source.[AccountStatus],
        target.[LastSyncedAtUtc] = @SyncedAt
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([UserId], [UserExternalId], [Email], [FirstName], [LastName], [AccountStatus], [CreatedAtUtc], [LastSyncedAtUtc])
    VALUES (source.[UserId], source.[UserExternalId], source.[Email], source.[FirstName], source.[LastName], source.[AccountStatus], source.[CreatedAtUtc], @SyncedAt);

PRINT 'UserProfiles synced: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' rows affected';
