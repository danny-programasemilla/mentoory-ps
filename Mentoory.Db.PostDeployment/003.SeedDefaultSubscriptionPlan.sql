-- Seed default subscription plan
IF NOT EXISTS (SELECT 1 FROM [subscription].[SubscriptionPlans] WHERE [Name] = N'Plan Básico')
BEGIN
    INSERT INTO [subscription].[SubscriptionPlans] ([ExternalId], [Name], [Description], [Version], [IsActive], [CreatedAtUtc])
    VALUES (NEWID(), N'Plan Básico', N'Plan de suscripción básico con funcionalidades esenciales', 1, 1, GETUTCDATE())
END
