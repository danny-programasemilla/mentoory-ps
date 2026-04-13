-- Seed default tenant system configuration values
IF NOT EXISTS (SELECT 1 FROM [tenant].[SystemConfigurations] WHERE [Key] = N'InvitationTokenExpiryHours')
BEGIN
    INSERT INTO [tenant].[SystemConfigurations] ([ExternalId], [Key], [Value], [Description], [DataType], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES
        (NEWID(), N'InvitationTokenExpiryHours', N'72', N'Horas de vigencia del token de invitación a proyecto', N'Integer', GETUTCDATE(), GETUTCDATE())
END
