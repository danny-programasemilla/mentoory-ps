-- Seed supported countries with identification format rules
IF NOT EXISTS (SELECT 1 FROM [access].[Countries] WHERE [Code] = N'CRI')
BEGIN
    INSERT INTO [access].[Countries] ([ExternalId], [Name], [Code], [IdentificationLabel], [IdentificationMask], [IdentificationRegex], [IdentificationMaxLength], [IsActive], [CreatedAtUtc])
    VALUES (NEWID(), N'Costa Rica', N'CRI', N'Cédula Nacional', N'0-0000-0000', N'^\d{1}-\d{4}-\d{4}$', 11, 1, GETUTCDATE())
END
