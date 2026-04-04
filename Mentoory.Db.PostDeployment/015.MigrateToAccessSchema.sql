-- ==========================================================================================
-- 015.MigrateToAccessSchema.sql
-- Idempotent migration: copies data from old [identity] and [authorization] schemas
-- to the new unified [access] schema, then drops old schemas if empty.
--
-- Must run BEFORE any seed scripts that reference [access] tables, or after DACPAC
-- has created the [access] schema objects.
-- ==========================================================================================

SET NOCOUNT ON;

-- ========================================================================================
-- STEP 1: Migrate [identity] tables to [access]
-- ========================================================================================

-- Check if the old [identity] schema exists and has a Users table
IF OBJECT_ID(N'[identity].[Users]', N'U') IS NOT NULL
BEGIN
    PRINT 'Migrating [identity].[Users] -> [access].[Users]';

    INSERT INTO [access].[Users]
        ([ExternalId], [Email], [NormalizedEmail], [Country], [NationalId],
         [FirstName], [LastName], [AccountStatus], [FailedLoginAttempts],
         [LockoutEndUtc], [EmailVerifiedAtUtc], [CreatedAtUtc], [UpdatedAtUtc])
    SELECT
        src.[ExternalId], src.[Email], src.[NormalizedEmail], src.[Country], src.[NationalId],
        src.[FirstName], src.[LastName], src.[AccountStatus], src.[FailedLoginAttempts],
        src.[LockoutEndUtc], src.[EmailVerifiedAtUtc], src.[CreatedAtUtc], src.[UpdatedAtUtc]
    FROM [identity].[Users] src
    WHERE NOT EXISTS (
        SELECT 1 FROM [access].[Users] tgt
        WHERE tgt.[NormalizedEmail] = src.[NormalizedEmail]
    );

    PRINT '  Users migrated: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' rows';
END

IF OBJECT_ID(N'[identity].[Credentials]', N'U') IS NOT NULL
BEGIN
    PRINT 'Migrating [identity].[Credentials] -> [access].[Credentials]';

    -- Map old UserId to new UserId via NormalizedEmail
    INSERT INTO [access].[Credentials]
        ([UserId], [PasswordHash], [IsActive], [CreatedAtUtc])
    SELECT
        newU.[Id], src.[PasswordHash], src.[IsActive], src.[CreatedAtUtc]
    FROM [identity].[Credentials] src
    INNER JOIN [identity].[Users] oldU ON oldU.[Id] = src.[UserId]
    INNER JOIN [access].[Users] newU ON newU.[NormalizedEmail] = oldU.[NormalizedEmail]
    WHERE NOT EXISTS (
        SELECT 1 FROM [access].[Credentials] tgt
        WHERE tgt.[UserId] = newU.[Id]
          AND tgt.[PasswordHash] = src.[PasswordHash]
          AND tgt.[CreatedAtUtc] = src.[CreatedAtUtc]
    );

    PRINT '  Credentials migrated: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' rows';
END

IF OBJECT_ID(N'[identity].[AuthSessions]', N'U') IS NOT NULL
BEGIN
    PRINT 'Migrating [identity].[AuthSessions] -> [access].[AuthSessions]';

    INSERT INTO [access].[AuthSessions]
        ([SessionToken], [UserId], [IpAddress], [UserAgent],
         [CreatedAtUtc], [LastActivityUtc], [ExpiresAtUtc], [IsActive],
         [ActiveIncubatorId], [ActiveProjectId], [ActiveRole])
    SELECT
        src.[SessionToken], newU.[Id], src.[IpAddress], src.[UserAgent],
        src.[CreatedAtUtc], src.[LastActivityUtc], src.[ExpiresAtUtc], src.[IsActive],
        src.[ActiveIncubatorId], src.[ActiveProjectId], src.[ActiveRole]
    FROM [identity].[AuthSessions] src
    INNER JOIN [identity].[Users] oldU ON oldU.[Id] = src.[UserId]
    INNER JOIN [access].[Users] newU ON newU.[NormalizedEmail] = oldU.[NormalizedEmail]
    WHERE NOT EXISTS (
        SELECT 1 FROM [access].[AuthSessions] tgt
        WHERE tgt.[SessionToken] = src.[SessionToken]
          AND tgt.[CreatedAtUtc] = src.[CreatedAtUtc]
    );

    PRINT '  AuthSessions migrated: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' rows';
END

IF OBJECT_ID(N'[identity].[EmailVerificationTokens]', N'U') IS NOT NULL
BEGIN
    PRINT 'Migrating [identity].[EmailVerificationTokens] -> [access].[EmailVerificationTokens]';

    INSERT INTO [access].[EmailVerificationTokens]
        ([UserId], [TokenHash], [ExpiresAtUtc], [IsUsed], [CreatedAtUtc])
    SELECT
        newU.[Id], src.[TokenHash], src.[ExpiresAtUtc], src.[IsUsed], src.[CreatedAtUtc]
    FROM [identity].[EmailVerificationTokens] src
    INNER JOIN [identity].[Users] oldU ON oldU.[Id] = src.[UserId]
    INNER JOIN [access].[Users] newU ON newU.[NormalizedEmail] = oldU.[NormalizedEmail]
    WHERE NOT EXISTS (
        SELECT 1 FROM [access].[EmailVerificationTokens] tgt
        WHERE tgt.[UserId] = newU.[Id]
          AND tgt.[TokenHash] = src.[TokenHash]
    );

    PRINT '  EmailVerificationTokens migrated: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' rows';
END

IF OBJECT_ID(N'[identity].[PasswordResetTokens]', N'U') IS NOT NULL
BEGIN
    PRINT 'Migrating [identity].[PasswordResetTokens] -> [access].[PasswordResetTokens]';

    INSERT INTO [access].[PasswordResetTokens]
        ([UserId], [TokenHash], [ExpiresAtUtc], [IsUsed], [CreatedAtUtc])
    SELECT
        newU.[Id], src.[TokenHash], src.[ExpiresAtUtc], src.[IsUsed], src.[CreatedAtUtc]
    FROM [identity].[PasswordResetTokens] src
    INNER JOIN [identity].[Users] oldU ON oldU.[Id] = src.[UserId]
    INNER JOIN [access].[Users] newU ON newU.[NormalizedEmail] = oldU.[NormalizedEmail]
    WHERE NOT EXISTS (
        SELECT 1 FROM [access].[PasswordResetTokens] tgt
        WHERE tgt.[UserId] = newU.[Id]
          AND tgt.[TokenHash] = src.[TokenHash]
    );

    PRINT '  PasswordResetTokens migrated: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' rows';
END

-- ========================================================================================
-- STEP 2: Migrate [authorization] tables to [access]
-- ========================================================================================

IF OBJECT_ID(N'[authorization].[RoleAssignments]', N'U') IS NOT NULL
BEGIN
    PRINT 'Migrating [authorization].[RoleAssignments] -> [access].[RoleAssignments]';

    -- Map old UserId to new UserId via NormalizedEmail
    INSERT INTO [access].[RoleAssignments]
        ([ExternalId], [UserId], [IncubatorId], [ProjectId], [Role],
         [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
    SELECT
        src.[ExternalId], newU.[Id], src.[IncubatorId], src.[ProjectId], src.[Role],
        src.[IsActive], src.[CreatedAtUtc], src.[UpdatedAtUtc]
    FROM [authorization].[RoleAssignments] src
    INNER JOIN [identity].[Users] oldU ON oldU.[Id] = src.[UserId]
    INNER JOIN [access].[Users] newU ON newU.[NormalizedEmail] = oldU.[NormalizedEmail]
    WHERE NOT EXISTS (
        SELECT 1 FROM [access].[RoleAssignments] tgt
        WHERE tgt.[UserId] = newU.[Id]
          AND tgt.[IncubatorId] = src.[IncubatorId]
          AND ISNULL(tgt.[ProjectId], -1) = ISNULL(src.[ProjectId], -1)
          AND tgt.[Role] = src.[Role]
    );

    PRINT '  RoleAssignments migrated: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' rows';
END

IF OBJECT_ID(N'[authorization].[UserProfiles]', N'U') IS NOT NULL
BEGIN
    PRINT 'Migrating [authorization].[UserProfiles] -> [access].[UserProfiles]';

    INSERT INTO [access].[UserProfiles]
        ([UserId], [UserExternalId], [Email], [FirstName], [LastName],
         [AccountStatus], [CreatedAtUtc], [LastSyncedAtUtc])
    SELECT
        newU.[Id], src.[UserExternalId], src.[Email], src.[FirstName], src.[LastName],
        src.[AccountStatus], src.[CreatedAtUtc], src.[LastSyncedAtUtc]
    FROM [authorization].[UserProfiles] src
    INNER JOIN [identity].[Users] oldU ON oldU.[Id] = src.[UserId]
    INNER JOIN [access].[Users] newU ON newU.[NormalizedEmail] = oldU.[NormalizedEmail]
    WHERE NOT EXISTS (
        SELECT 1 FROM [access].[UserProfiles] tgt
        WHERE tgt.[UserId] = newU.[Id]
    );

    PRINT '  UserProfiles migrated: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' rows';
END

-- ========================================================================================
-- STEP 3: Drop old schemas if they exist and are empty
-- Uses nested IFs because T-SQL does not short-circuit AND conditions,
-- so SELECT from a non-existent table in an IF condition causes a runtime error.
-- ========================================================================================

-- Drop [authorization] schema tables and schema if empty
IF OBJECT_ID(N'[authorization].[UserProfiles]', N'U') IS NOT NULL
BEGIN
    DECLARE @AuthProfileCount INT, @AuthRoleCount INT;
    SELECT @AuthProfileCount = COUNT(*) FROM [authorization].[UserProfiles];
    IF OBJECT_ID(N'[authorization].[RoleAssignments]', N'U') IS NOT NULL
        SELECT @AuthRoleCount = COUNT(*) FROM [authorization].[RoleAssignments];
    ELSE
        SET @AuthRoleCount = 0;

    IF @AuthProfileCount = 0 AND @AuthRoleCount = 0
    BEGIN
        DROP TABLE [authorization].[UserProfiles];
        IF OBJECT_ID(N'[authorization].[RoleAssignments]', N'U') IS NOT NULL
            DROP TABLE [authorization].[RoleAssignments];

        IF NOT EXISTS (
            SELECT 1 FROM sys.objects o
            INNER JOIN sys.schemas s ON o.schema_id = s.schema_id
            WHERE s.name = 'authorization'
        )
            DROP SCHEMA [authorization];

        PRINT 'Old [authorization] schema dropped.';
    END
END

-- Drop [identity] schema tables and schema if empty
IF OBJECT_ID(N'[identity].[Users]', N'U') IS NOT NULL
BEGIN
    DECLARE @IdentityUserCount INT;
    SELECT @IdentityUserCount = COUNT(*) FROM [identity].[Users];

    IF @IdentityUserCount = 0
    BEGIN
        -- Drop in FK-safe order (children first)
        IF OBJECT_ID(N'[identity].[PasswordResetTokens]', N'U') IS NOT NULL
            DROP TABLE [identity].[PasswordResetTokens];
        IF OBJECT_ID(N'[identity].[EmailVerificationTokens]', N'U') IS NOT NULL
            DROP TABLE [identity].[EmailVerificationTokens];
        IF OBJECT_ID(N'[identity].[AuthSessions]', N'U') IS NOT NULL
            DROP TABLE [identity].[AuthSessions];
        IF OBJECT_ID(N'[identity].[Credentials]', N'U') IS NOT NULL
            DROP TABLE [identity].[Credentials];
        IF OBJECT_ID(N'[identity].[Users]', N'U') IS NOT NULL
            DROP TABLE [identity].[Users];

        IF NOT EXISTS (
            SELECT 1 FROM sys.objects o
            INNER JOIN sys.schemas s ON o.schema_id = s.schema_id
            WHERE s.name = 'identity'
        )
            DROP SCHEMA [identity];

        PRINT 'Old [identity] schema dropped.';
    END
END

PRINT '015.MigrateToAccessSchema.sql completed.';
