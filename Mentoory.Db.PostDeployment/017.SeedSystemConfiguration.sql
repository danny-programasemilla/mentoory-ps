-- Seed default system configuration values
IF NOT EXISTS (SELECT 1 FROM [access].[SystemConfigurations] WHERE [Key] = N'EmailVerificationTokenExpiryHours')
BEGIN
    INSERT INTO [access].[SystemConfigurations] ([ExternalId], [Key], [Value], [Description], [DataType], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES
        (NEWID(), N'EmailVerificationTokenExpiryHours', N'24', N'Horas de vigencia del token de verificación de correo electrónico', N'Integer', GETUTCDATE(), GETUTCDATE()),
        (NEWID(), N'PasswordResetTokenExpiryHours', N'1', N'Horas de vigencia del token de restablecimiento de contraseña', N'Integer', GETUTCDATE(), GETUTCDATE()),
        (NEWID(), N'InvitationTokenExpiryHours', N'72', N'Horas de vigencia del token de invitación a proyecto', N'Integer', GETUTCDATE(), GETUTCDATE()),
        (NEWID(), N'MaxFailedLoginAttempts', N'5', N'Número máximo de intentos de inicio de sesión fallidos antes del bloqueo', N'Integer', GETUTCDATE(), GETUTCDATE()),
        (NEWID(), N'LockoutDurationMinutes', N'15', N'Duración del bloqueo de cuenta en minutos', N'Integer', GETUTCDATE(), GETUTCDATE()),
        (NEWID(), N'SessionTimeoutHours', N'8', N'Duración máxima de la sesión en horas', N'Integer', GETUTCDATE(), GETUTCDATE()),
        (NEWID(), N'PasswordHistoryDepth', N'5', N'Cantidad de contraseñas anteriores que no se pueden reutilizar', N'Integer', GETUTCDATE(), GETUTCDATE())
END
