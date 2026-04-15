-- Seed default notification configuration values
IF NOT EXISTS (SELECT 1 FROM [notification].[NotificationConfigurations] WHERE [Key] = N'SmtpHost')
BEGIN
    INSERT INTO [notification].[NotificationConfigurations] ([ExternalId], [Key], [Value], [Description], [DataType], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES
        (NEWID(), N'SmtpHost', N'', N'Nombre del servidor SMTP para el envío de correos electrónicos', N'String', GETUTCDATE(), GETUTCDATE()),
        (NEWID(), N'SmtpPort', N'587', N'Puerto del servidor SMTP', N'Integer', GETUTCDATE(), GETUTCDATE()),
        (NEWID(), N'SmtpUsername', N'', N'Nombre de usuario para la autenticación SMTP', N'String', GETUTCDATE(), GETUTCDATE()),
        (NEWID(), N'SmtpPassword', N'', N'Contraseña para la autenticación SMTP', N'String', GETUTCDATE(), GETUTCDATE()),
        (NEWID(), N'SmtpFromAddress', N'', N'Dirección de correo electrónico del remitente', N'String', GETUTCDATE(), GETUTCDATE()),
        (NEWID(), N'SmtpFromName', N'Mentoory', N'Nombre del remitente que aparece en los correos electrónicos', N'String', GETUTCDATE(), GETUTCDATE()),
        (NEWID(), N'SmtpUseSsl', N'true', N'Indica si se debe usar SSL/TLS para la conexión SMTP', N'Boolean', GETUTCDATE(), GETUTCDATE()),
        (NEWID(), N'PollingIntervalSeconds', N'15', N'Intervalo en segundos entre cada ciclo de procesamiento de notificaciones', N'Integer', GETUTCDATE(), GETUTCDATE()),
        (NEWID(), N'MaxRetryAttempts', N'5', N'Número máximo de intentos de envío por destinatario', N'Integer', GETUTCDATE(), GETUTCDATE()),
        (NEWID(), N'BaseUrl', N'', N'URL base de la aplicación para generar enlaces en correos electrónicos', N'String', GETUTCDATE(), GETUTCDATE())
END
