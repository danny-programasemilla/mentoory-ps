-- Seed default GlobalAdmin user
IF NOT EXISTS (SELECT 1 FROM [identity].[Users] WHERE [NormalizedEmail] = N'ADMIN@MENTOORY.COM')
BEGIN
    INSERT INTO [identity].[Users] ([ExternalId], [Email], [NormalizedEmail], [Country], [NationalId], [FirstName], [LastName], [AccountStatus], [FailedLoginAttempts], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES (NEWID(), N'admin@mentoory.com', N'ADMIN@MENTOORY.COM', N'System', N'ADMIN-001', N'Admin', N'Mentoory', 1, 0, GETUTCDATE(), GETUTCDATE())

    DECLARE @AdminUserId BIGINT = SCOPE_IDENTITY()

    -- Create a credential with password: 123abc987 (must be changed on first login)
    INSERT INTO [identity].[Credentials] ([UserId], [PasswordHash], [IsActive], [CreatedAtUtc])
    VALUES (@AdminUserId, N'pbkdf2-sha512$600000$KZE34056Y9V0NKiN+P/+xA==$ojunkeMWPG9fM6oomGSp1QfmDxANxffYnGrmuF/MNho=', 1, GETUTCDATE())

    -- Create GlobalAdmin role assignment (IncubatorId=0 as sentinel for global scope)
    INSERT INTO [authorization].[RoleAssignments] ([ExternalId], [UserId], [IncubatorId], [ProjectId], [Role], [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES (NEWID(), @AdminUserId, 0, NULL, N'GlobalAdmin', 1, GETUTCDATE(), GETUTCDATE())
END
