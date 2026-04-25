-- ==========================================================================================
-- 004.SeedTestData.sql
-- Deterministic test seed data for the Mentoory platform.
-- Fully idempotent: safe to run multiple times via IF NOT EXISTS / MERGE patterns.
--
-- Depends on:
--   002.SeedGlobalAdmin.sql  (GlobalAdmin user already exists)
--   003.SeedDefaultSubscriptionPlan.sql  (Plan Básico already exists)
--
-- Password for all test users (Test123!@#):
--   pbkdf2-sha512$600000$KZE34056Y9V0NKiN+P/+xA==$ojunkeMWPG9fM6oomGSp1QfmDxANxffYnGrmuF/MNho=
-- ==========================================================================================

SET NOCOUNT ON;

DECLARE @PasswordHash NVARCHAR(500) = N'pbkdf2-sha512$600000$vXEKVDDyckYOMgyiPBPUQg==$ZTWBL0Wx1XaoHBz9SRedZ1nbH0wZDh5taxDgqlKsn7A=';
DECLARE @Now DATETIME2 = GETUTCDATE();

-- ==========================================================================================
-- SECTION 1: Users and Credentials
-- ==========================================================================================

-- Helper variables for user IDs (populated after insert or lookup)
DECLARE @IncAdmin1Id   BIGINT;
DECLARE @IncAdmin2Id   BIGINT;
DECLARE @Coord1Id      BIGINT;
DECLARE @Coord2Id      BIGINT;
DECLARE @Coord3Id      BIGINT;
DECLARE @CoordNorteId  BIGINT;  -- spec 017 (knowledge-e2e-tests): coordinator in Incubadora Norte (US6-3 tenant isolation)
DECLARE @Mentor1Id     BIGINT;
DECLARE @Entrep1Id     BIGINT;
DECLARE @Entrep2Id     BIGINT;
DECLARE @Sponsor1Id    BIGINT;
DECLARE @MultiRoleId   BIGINT;

-- ------------------------------------------------------------------------------------------
-- User: Incubator Admin 1
-- ------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [access].[Users] WHERE [NormalizedEmail] = N'INCADMIN1@TEST.MENTOORY.COM')
BEGIN
    INSERT INTO [access].[Users] ([ExternalId], [Email], [NormalizedEmail], [Country], [NationalId], [FirstName], [LastName], [AccountStatus], [FailedLoginAttempts], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES (NEWID(), N'incadmin1@test.mentoory.com', N'INCADMIN1@TEST.MENTOORY.COM', N'Chile', N'TEST-INCADMIN1', N'Carlos', N'Mendoza', 1, 0, @Now, @Now);

    SET @IncAdmin1Id = SCOPE_IDENTITY();

    INSERT INTO [access].[Credentials] ([UserId], [PasswordHash], [IsActive], [CreatedAtUtc])
    VALUES (@IncAdmin1Id, @PasswordHash, 1, @Now);
END
ELSE
    SELECT @IncAdmin1Id = [Id] FROM [access].[Users] WHERE [NormalizedEmail] = N'INCADMIN1@TEST.MENTOORY.COM';

-- ------------------------------------------------------------------------------------------
-- User: Incubator Admin 2
-- ------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [access].[Users] WHERE [NormalizedEmail] = N'INCADMIN2@TEST.MENTOORY.COM')
BEGIN
    INSERT INTO [access].[Users] ([ExternalId], [Email], [NormalizedEmail], [Country], [NationalId], [FirstName], [LastName], [AccountStatus], [FailedLoginAttempts], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES (NEWID(), N'incadmin2@test.mentoory.com', N'INCADMIN2@TEST.MENTOORY.COM', N'Chile', N'TEST-INCADMIN2', N'María', N'Fernández', 1, 0, @Now, @Now);

    SET @IncAdmin2Id = SCOPE_IDENTITY();

    INSERT INTO [access].[Credentials] ([UserId], [PasswordHash], [IsActive], [CreatedAtUtc])
    VALUES (@IncAdmin2Id, @PasswordHash, 1, @Now);
END
ELSE
    SELECT @IncAdmin2Id = [Id] FROM [access].[Users] WHERE [NormalizedEmail] = N'INCADMIN2@TEST.MENTOORY.COM';

-- ------------------------------------------------------------------------------------------
-- User: Project Coordinator 1
-- ------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [access].[Users] WHERE [NormalizedEmail] = N'COORD1@TEST.MENTOORY.COM')
BEGIN
    INSERT INTO [access].[Users] ([ExternalId], [Email], [NormalizedEmail], [Country], [NationalId], [FirstName], [LastName], [AccountStatus], [FailedLoginAttempts], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES (NEWID(), N'coord1@test.mentoory.com', N'COORD1@TEST.MENTOORY.COM', N'Chile', N'TEST-COORD1', N'Ana', N'Rodríguez', 1, 0, @Now, @Now);

    SET @Coord1Id = SCOPE_IDENTITY();

    INSERT INTO [access].[Credentials] ([UserId], [PasswordHash], [IsActive], [CreatedAtUtc])
    VALUES (@Coord1Id, @PasswordHash, 1, @Now);
END
ELSE
    SELECT @Coord1Id = [Id] FROM [access].[Users] WHERE [NormalizedEmail] = N'COORD1@TEST.MENTOORY.COM';

-- ------------------------------------------------------------------------------------------
-- User: Project Coordinator 2
-- ------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [access].[Users] WHERE [NormalizedEmail] = N'COORD2@TEST.MENTOORY.COM')
BEGIN
    INSERT INTO [access].[Users] ([ExternalId], [Email], [NormalizedEmail], [Country], [NationalId], [FirstName], [LastName], [AccountStatus], [FailedLoginAttempts], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES (NEWID(), N'coord2@test.mentoory.com', N'COORD2@TEST.MENTOORY.COM', N'Chile', N'TEST-COORD2', N'Luis', N'Paredes', 1, 0, @Now, @Now);

    SET @Coord2Id = SCOPE_IDENTITY();

    INSERT INTO [access].[Credentials] ([UserId], [PasswordHash], [IsActive], [CreatedAtUtc])
    VALUES (@Coord2Id, @PasswordHash, 1, @Now);
END
ELSE
    SELECT @Coord2Id = [Id] FROM [access].[Users] WHERE [NormalizedEmail] = N'COORD2@TEST.MENTOORY.COM';

-- ------------------------------------------------------------------------------------------
-- User: Project Coordinator 3
-- ------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [access].[Users] WHERE [NormalizedEmail] = N'COORD3@TEST.MENTOORY.COM')
BEGIN
    INSERT INTO [access].[Users] ([ExternalId], [Email], [NormalizedEmail], [Country], [NationalId], [FirstName], [LastName], [AccountStatus], [FailedLoginAttempts], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES (NEWID(), N'coord3@test.mentoory.com', N'COORD3@TEST.MENTOORY.COM', N'Chile', N'TEST-COORD3', N'Sofía', N'Navarro', 1, 0, @Now, @Now);

    SET @Coord3Id = SCOPE_IDENTITY();

    INSERT INTO [access].[Credentials] ([UserId], [PasswordHash], [IsActive], [CreatedAtUtc])
    VALUES (@Coord3Id, @PasswordHash, 1, @Now);
END
ELSE
    SELECT @Coord3Id = [Id] FROM [access].[Users] WHERE [NormalizedEmail] = N'COORD3@TEST.MENTOORY.COM';

-- ------------------------------------------------------------------------------------------
-- User: Project Coordinator Norte (spec 017 — US6-3 tenant isolation)
-- Lives only in Incubadora Norte (seeded in §2). Deliberately separate from coord2 to
-- avoid breaking the "coord2 is in exactly one project (Sostenibilidad, Alpha)" invariant
-- asserted by BatchUploadScopeTests.ProjectCoordinator2_SeesOnlyTheirProject.
-- ------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [access].[Users] WHERE [NormalizedEmail] = N'COORDNORTE@TEST.MENTOORY.COM')
BEGIN
    INSERT INTO [access].[Users] ([ExternalId], [Email], [NormalizedEmail], [Country], [NationalId], [FirstName], [LastName], [AccountStatus], [FailedLoginAttempts], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES (NEWID(), N'coordnorte@test.mentoory.com', N'COORDNORTE@TEST.MENTOORY.COM', N'Chile', N'TEST-COORDNORTE', N'Camila', N'Riquelme', 1, 0, @Now, @Now);

    SET @CoordNorteId = SCOPE_IDENTITY();

    INSERT INTO [access].[Credentials] ([UserId], [PasswordHash], [IsActive], [CreatedAtUtc])
    VALUES (@CoordNorteId, @PasswordHash, 1, @Now);
END
ELSE
    SELECT @CoordNorteId = [Id] FROM [access].[Users] WHERE [NormalizedEmail] = N'COORDNORTE@TEST.MENTOORY.COM';

-- ------------------------------------------------------------------------------------------
-- User: Mentor
-- ------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [access].[Users] WHERE [NormalizedEmail] = N'MENTOR1@TEST.MENTOORY.COM')
BEGIN
    INSERT INTO [access].[Users] ([ExternalId], [Email], [NormalizedEmail], [Country], [NationalId], [FirstName], [LastName], [AccountStatus], [FailedLoginAttempts], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES (NEWID(), N'mentor1@test.mentoory.com', N'MENTOR1@TEST.MENTOORY.COM', N'Chile', N'TEST-MENTOR1', N'Roberto', N'Sánchez', 1, 0, @Now, @Now);

    SET @Mentor1Id = SCOPE_IDENTITY();

    INSERT INTO [access].[Credentials] ([UserId], [PasswordHash], [IsActive], [CreatedAtUtc])
    VALUES (@Mentor1Id, @PasswordHash, 1, @Now);
END
ELSE
    SELECT @Mentor1Id = [Id] FROM [access].[Users] WHERE [NormalizedEmail] = N'MENTOR1@TEST.MENTOORY.COM';

-- ------------------------------------------------------------------------------------------
-- User: Entrepreneur 1
-- ------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [access].[Users] WHERE [NormalizedEmail] = N'ENTREPRENEUR1@TEST.MENTOORY.COM')
BEGIN
    INSERT INTO [access].[Users] ([ExternalId], [Email], [NormalizedEmail], [Country], [NationalId], [FirstName], [LastName], [AccountStatus], [FailedLoginAttempts], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES (NEWID(), N'entrepreneur1@test.mentoory.com', N'ENTREPRENEUR1@TEST.MENTOORY.COM', N'Chile', N'TEST-ENTREP1', N'Valentina', N'López', 1, 0, @Now, @Now);

    SET @Entrep1Id = SCOPE_IDENTITY();

    INSERT INTO [access].[Credentials] ([UserId], [PasswordHash], [IsActive], [CreatedAtUtc])
    VALUES (@Entrep1Id, @PasswordHash, 1, @Now);
END
ELSE
    SELECT @Entrep1Id = [Id] FROM [access].[Users] WHERE [NormalizedEmail] = N'ENTREPRENEUR1@TEST.MENTOORY.COM';

-- ------------------------------------------------------------------------------------------
-- User: Entrepreneur 2
-- ------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [access].[Users] WHERE [NormalizedEmail] = N'ENTREPRENEUR2@TEST.MENTOORY.COM')
BEGIN
    INSERT INTO [access].[Users] ([ExternalId], [Email], [NormalizedEmail], [Country], [NationalId], [FirstName], [LastName], [AccountStatus], [FailedLoginAttempts], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES (NEWID(), N'entrepreneur2@test.mentoory.com', N'ENTREPRENEUR2@TEST.MENTOORY.COM', N'Chile', N'TEST-ENTREP2', N'Diego', N'Torres', 1, 0, @Now, @Now);

    SET @Entrep2Id = SCOPE_IDENTITY();

    INSERT INTO [access].[Credentials] ([UserId], [PasswordHash], [IsActive], [CreatedAtUtc])
    VALUES (@Entrep2Id, @PasswordHash, 1, @Now);
END
ELSE
    SELECT @Entrep2Id = [Id] FROM [access].[Users] WHERE [NormalizedEmail] = N'ENTREPRENEUR2@TEST.MENTOORY.COM';

-- ------------------------------------------------------------------------------------------
-- User: Sponsor
-- ------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [access].[Users] WHERE [NormalizedEmail] = N'SPONSOR1@TEST.MENTOORY.COM')
BEGIN
    INSERT INTO [access].[Users] ([ExternalId], [Email], [NormalizedEmail], [Country], [NationalId], [FirstName], [LastName], [AccountStatus], [FailedLoginAttempts], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES (NEWID(), N'sponsor1@test.mentoory.com', N'SPONSOR1@TEST.MENTOORY.COM', N'Chile', N'TEST-SPONSOR1', N'Gabriela', N'Morales', 1, 0, @Now, @Now);

    SET @Sponsor1Id = SCOPE_IDENTITY();

    INSERT INTO [access].[Credentials] ([UserId], [PasswordHash], [IsActive], [CreatedAtUtc])
    VALUES (@Sponsor1Id, @PasswordHash, 1, @Now);
END
ELSE
    SELECT @Sponsor1Id = [Id] FROM [access].[Users] WHERE [NormalizedEmail] = N'SPONSOR1@TEST.MENTOORY.COM';

-- ------------------------------------------------------------------------------------------
-- User: Multi-Role User
-- ------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [access].[Users] WHERE [NormalizedEmail] = N'MULTIROLE@TEST.MENTOORY.COM')
BEGIN
    INSERT INTO [access].[Users] ([ExternalId], [Email], [NormalizedEmail], [Country], [NationalId], [FirstName], [LastName], [AccountStatus], [FailedLoginAttempts], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES (NEWID(), N'multirole@test.mentoory.com', N'MULTIROLE@TEST.MENTOORY.COM', N'Chile', N'TEST-MULTIROLE', N'Javier', N'Gutiérrez', 1, 0, @Now, @Now);

    SET @MultiRoleId = SCOPE_IDENTITY();

    INSERT INTO [access].[Credentials] ([UserId], [PasswordHash], [IsActive], [CreatedAtUtc])
    VALUES (@MultiRoleId, @PasswordHash, 1, @Now);
END
ELSE
    SELECT @MultiRoleId = [Id] FROM [access].[Users] WHERE [NormalizedEmail] = N'MULTIROLE@TEST.MENTOORY.COM';


-- ==========================================================================================
-- SECTION 2: Incubators
-- ==========================================================================================

DECLARE @Incubator1Id BIGINT;
DECLARE @Incubator2Id BIGINT;
DECLARE @IncubatorNorteId BIGINT;  -- spec 017: third incubator for tenant isolation (US6-3)
DECLARE @SubscriptionPlanId BIGINT;

-- Resolve subscription plan seeded by 003.SeedDefaultSubscriptionPlan.sql
SELECT @SubscriptionPlanId = [Id] FROM [subscription].[SubscriptionPlans] WHERE [Name] = N'Plan Básico';

-- ------------------------------------------------------------------------------------------
-- Incubator: Incubadora Alpha
-- ------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [tenant].[Incubators] WHERE [Name] = N'Incubadora Alpha')
BEGIN
    INSERT INTO [tenant].[Incubators] ([ExternalId], [Name], [Description], [SubscriptionPlanId], [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES (NEWID(), N'Incubadora Alpha', N'Incubadora de negocios enfocada en innovación tecnológica y sostenibilidad', @SubscriptionPlanId, 1, @Now, @Now);

    SET @Incubator1Id = SCOPE_IDENTITY();
END
ELSE
    SELECT @Incubator1Id = [Id] FROM [tenant].[Incubators] WHERE [Name] = N'Incubadora Alpha';

-- ------------------------------------------------------------------------------------------
-- Incubator: Incubadora Beta
-- ------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [tenant].[Incubators] WHERE [Name] = N'Incubadora Beta')
BEGIN
    INSERT INTO [tenant].[Incubators] ([ExternalId], [Name], [Description], [SubscriptionPlanId], [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES (NEWID(), N'Incubadora Beta', N'Incubadora especializada en transformación digital y emprendimiento social', @SubscriptionPlanId, 1, @Now, @Now);

    SET @Incubator2Id = SCOPE_IDENTITY();
END
ELSE
    SELECT @Incubator2Id = [Id] FROM [tenant].[Incubators] WHERE [Name] = N'Incubadora Beta';

-- ------------------------------------------------------------------------------------------
-- Incubator: Incubadora Norte (spec 017, Addition 1 — for US6-3 tenant isolation)
-- ExternalId collides with ModuleTemplate "Ideación" in 005 — legal (different table).
-- ------------------------------------------------------------------------------------------
DECLARE @IncubatorNorteExternalId UNIQUEIDENTIFIER = CAST('22222222-2222-2222-2222-222222222222' AS UNIQUEIDENTIFIER);

IF NOT EXISTS (SELECT 1 FROM [tenant].[Incubators] WHERE [ExternalId] = @IncubatorNorteExternalId)
BEGIN
    INSERT INTO [tenant].[Incubators] ([ExternalId], [Name], [Description], [SubscriptionPlanId], [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES (@IncubatorNorteExternalId, N'Incubadora Norte', N'Incubadora regional del norte para pruebas de aislamiento de inquilinos.', @SubscriptionPlanId, 1, @Now, @Now);

    SET @IncubatorNorteId = SCOPE_IDENTITY();
END
ELSE
    SELECT @IncubatorNorteId = [Id] FROM [tenant].[Incubators] WHERE [ExternalId] = @IncubatorNorteExternalId;


-- ==========================================================================================
-- SECTION 2.5: Knowledge structure template (required by tenant.Projects FK)
-- ------------------------------------------------------------------------------------------
-- Seeded inline here (before §3 Projects) because each project now references the
-- "Emprendimiento Básico" template via KnowledgeStructureTemplateExternalId (NOT NULL).
-- 005.SeedKnowledgeData.sql re-seeds the template idempotently (IF NOT EXISTS) — safe.
-- ==========================================================================================

DECLARE @TemplateExternalId UNIQUEIDENTIFIER = CAST('11111111-1111-1111-1111-111111111111' AS UNIQUEIDENTIFIER);
DECLARE @StructureTemplateId BIGINT;

IF NOT EXISTS (SELECT 1 FROM [knowledge].[KnowledgeStructureTemplates] WHERE [ExternalId] = @TemplateExternalId)
BEGIN
    INSERT INTO [knowledge].[KnowledgeStructureTemplates] ([ExternalId], [Name], [Description], [IsArchived], [Version], [CreatedAtUtc])
    VALUES (@TemplateExternalId, N'Emprendimiento Básico', N'Plantilla de ejemplo para proyectos de emprendimiento en etapa temprana.', 0, 1, @Now);

    SET @StructureTemplateId = SCOPE_IDENTITY();
END
ELSE
    SELECT @StructureTemplateId = [Id] FROM [knowledge].[KnowledgeStructureTemplates] WHERE [ExternalId] = @TemplateExternalId;


-- ==========================================================================================
-- SECTION 3: Projects
-- ==========================================================================================

DECLARE @Project1Id BIGINT;  -- Proyecto Innovación (Incubator 1)
DECLARE @Project2Id BIGINT;  -- Proyecto Sostenibilidad (Incubator 1)
DECLARE @Project3Id BIGINT;  -- Proyecto Digital (Incubator 2)
DECLARE @Project4Id BIGINT;  -- Proyecto Comunitario (Incubator 2)
DECLARE @ProjectNorteId BIGINT;  -- Proyecto Norte Uno (Incubadora Norte; spec 017 US6-3)

-- ------------------------------------------------------------------------------------------
-- Project: Proyecto Innovación (Incubadora Alpha)
-- ------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [tenant].[Projects] WHERE [Name] = N'Proyecto Innovación' AND [IncubatorId] = @Incubator1Id)
BEGIN
    INSERT INTO [tenant].[Projects] ([ExternalId], [IncubatorId], [Name], [Description], [KnowledgeStructureTemplateExternalId], [CurrentStageType], [CurrentStageState], [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES (NEWID(), @Incubator1Id, N'Proyecto Innovación', N'Proyecto piloto de innovación tecnológica aplicada al sector agrícola', @TemplateExternalId, 0, 1, 1, @Now, @Now);

    SET @Project1Id = SCOPE_IDENTITY();
END
ELSE
    SELECT @Project1Id = [Id] FROM [tenant].[Projects] WHERE [Name] = N'Proyecto Innovación' AND [IncubatorId] = @Incubator1Id;

-- ------------------------------------------------------------------------------------------
-- Project: Proyecto Sostenibilidad (Incubadora Alpha)
-- ------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [tenant].[Projects] WHERE [Name] = N'Proyecto Sostenibilidad' AND [IncubatorId] = @Incubator1Id)
BEGIN
    INSERT INTO [tenant].[Projects] ([ExternalId], [IncubatorId], [Name], [Description], [KnowledgeStructureTemplateExternalId], [CurrentStageType], [CurrentStageState], [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES (NEWID(), @Incubator1Id, N'Proyecto Sostenibilidad', N'Desarrollo de modelo de negocio sostenible con impacto medioambiental positivo', @TemplateExternalId, 0, 1, 1, @Now, @Now);

    SET @Project2Id = SCOPE_IDENTITY();
END
ELSE
    SELECT @Project2Id = [Id] FROM [tenant].[Projects] WHERE [Name] = N'Proyecto Sostenibilidad' AND [IncubatorId] = @Incubator1Id;

-- ------------------------------------------------------------------------------------------
-- Project: Proyecto Digital (Incubadora Beta)
-- ------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [tenant].[Projects] WHERE [Name] = N'Proyecto Digital' AND [IncubatorId] = @Incubator2Id)
BEGIN
    INSERT INTO [tenant].[Projects] ([ExternalId], [IncubatorId], [Name], [Description], [KnowledgeStructureTemplateExternalId], [CurrentStageType], [CurrentStageState], [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES (NEWID(), @Incubator2Id, N'Proyecto Digital', N'Plataforma digital para conectar emprendedores con mentores especializados', @TemplateExternalId, 0, 1, 1, @Now, @Now);

    SET @Project3Id = SCOPE_IDENTITY();
END
ELSE
    SELECT @Project3Id = [Id] FROM [tenant].[Projects] WHERE [Name] = N'Proyecto Digital' AND [IncubatorId] = @Incubator2Id;

-- ------------------------------------------------------------------------------------------
-- Project: Proyecto Comunitario (Incubadora Beta)
-- ------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [tenant].[Projects] WHERE [Name] = N'Proyecto Comunitario' AND [IncubatorId] = @Incubator2Id)
BEGIN
    INSERT INTO [tenant].[Projects] ([ExternalId], [IncubatorId], [Name], [Description], [KnowledgeStructureTemplateExternalId], [CurrentStageType], [CurrentStageState], [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES (NEWID(), @Incubator2Id, N'Proyecto Comunitario', N'Iniciativa de desarrollo comunitario para fortalecer redes de emprendimiento local', @TemplateExternalId, 0, 1, 1, @Now, @Now);

    SET @Project4Id = SCOPE_IDENTITY();
END
ELSE
    SELECT @Project4Id = [Id] FROM [tenant].[Projects] WHERE [Name] = N'Proyecto Comunitario' AND [IncubatorId] = @Incubator2Id;

-- ------------------------------------------------------------------------------------------
-- Project: Proyecto Norte Uno (Incubadora Norte) — spec 017 US6-3 tenant isolation fixture
-- ------------------------------------------------------------------------------------------
DECLARE @ProjectNorteExternalId UNIQUEIDENTIFIER = CAST('88888888-8888-8888-8888-888888880001' AS UNIQUEIDENTIFIER);

IF NOT EXISTS (SELECT 1 FROM [tenant].[Projects] WHERE [ExternalId] = @ProjectNorteExternalId)
BEGIN
    INSERT INTO [tenant].[Projects] ([ExternalId], [IncubatorId], [Name], [Description], [KnowledgeStructureTemplateExternalId], [CurrentStageType], [CurrentStageState], [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES (@ProjectNorteExternalId, @IncubatorNorteId, N'Proyecto Norte Uno', N'Proyecto del norte vinculado a la plantilla Emprendimiento Básico.', @TemplateExternalId, 0, 1, 1, @Now, @Now);

    SET @ProjectNorteId = SCOPE_IDENTITY();
END
ELSE
    SELECT @ProjectNorteId = [Id] FROM [tenant].[Projects] WHERE [ExternalId] = @ProjectNorteExternalId;


-- ==========================================================================================
-- SECTION 3.5: Knowledge topics referenced by test diagnostic questions
-- ------------------------------------------------------------------------------------------
-- The FK diagnostic.Questions.TopicId -> knowledge.Topics.Id (spec 016) requires that
-- Topic Ids 1-5 exist BEFORE Section 8 inserts diagnostic.Questions with those literal
-- TopicIds. We seed them here (inside this script) so the FK resolves mid-batch.
-- Parent rows: knowledge.KnowledgeStructures (for Proyecto Innovación) + knowledge.Modules.
-- SourceTemplateId references the "Emprendimiento Básico" template seeded in §2.5
-- (every project KS is sourced from a template under spec 016 Phase 9 — no NULL path).
-- Convention: 1=Modelo de Negocio, 2=Equipo, 3=Mercado, 4=Finanzas, 5=Impacto
-- ==========================================================================================

DECLARE @ProjectStructureExternalId UNIQUEIDENTIFIER = CAST('99999999-9999-9999-9999-999999999901' AS UNIQUEIDENTIFIER);
DECLARE @ProjectStructureId BIGINT;

IF NOT EXISTS (SELECT 1 FROM [knowledge].[KnowledgeStructures] WHERE [ExternalId] = @ProjectStructureExternalId)
BEGIN
    INSERT INTO [knowledge].[KnowledgeStructures] ([ExternalId], [ProjectId], [IncubatorId], [Name], [Description],
        [SourceTemplateId], [SourceTemplateVersion], [SyncMode], [CreatedAtUtc])
    VALUES (@ProjectStructureExternalId, @Project1Id, @Incubator1Id,
        N'Estructura de Conocimiento - Proyecto Innovación',
        N'Estructura de conocimiento sembrada para alinear con los temas del diagnóstico de prueba.',
        @StructureTemplateId, 1, 0, @Now);

    SET @ProjectStructureId = SCOPE_IDENTITY();
END
ELSE
    SELECT @ProjectStructureId = [Id] FROM [knowledge].[KnowledgeStructures] WHERE [ExternalId] = @ProjectStructureExternalId;

DECLARE @ProjectModuleExternalId UNIQUEIDENTIFIER = CAST('99999999-9999-9999-9999-999999999902' AS UNIQUEIDENTIFIER);
DECLARE @ProjectModuleId BIGINT;

IF NOT EXISTS (SELECT 1 FROM [knowledge].[Modules] WHERE [ExternalId] = @ProjectModuleExternalId)
BEGIN
    INSERT INTO [knowledge].[Modules] ([ExternalId], [KnowledgeStructureId], [SourceTemplateModuleExternalId],
        [Name], [Description], [SortOrder])
    VALUES (@ProjectModuleExternalId, @ProjectStructureId, NULL,
        N'Diagnóstico', N'Módulo agrupador de los temas referenciados por el diagnóstico.', 1);

    SET @ProjectModuleId = SCOPE_IDENTITY();
END
ELSE
    SELECT @ProjectModuleId = [Id] FROM [knowledge].[Modules] WHERE [ExternalId] = @ProjectModuleExternalId;

DECLARE @TopicExternalId1 UNIQUEIDENTIFIER = CAST('99999999-9999-9999-9999-999999990001' AS UNIQUEIDENTIFIER);
DECLARE @TopicExternalId2 UNIQUEIDENTIFIER = CAST('99999999-9999-9999-9999-999999990002' AS UNIQUEIDENTIFIER);
DECLARE @TopicExternalId3 UNIQUEIDENTIFIER = CAST('99999999-9999-9999-9999-999999990003' AS UNIQUEIDENTIFIER);
DECLARE @TopicExternalId4 UNIQUEIDENTIFIER = CAST('99999999-9999-9999-9999-999999990004' AS UNIQUEIDENTIFIER);
DECLARE @TopicExternalId5 UNIQUEIDENTIFIER = CAST('99999999-9999-9999-9999-999999990005' AS UNIQUEIDENTIFIER);

SET IDENTITY_INSERT [knowledge].[Topics] ON;

IF NOT EXISTS (SELECT 1 FROM [knowledge].[Topics] WHERE [Id] = 1)
    INSERT INTO [knowledge].[Topics] ([Id], [ExternalId], [ModuleId], [SourceTemplateTopicExternalId],
        [Name], [Description], [SortOrder],
        [HighRangeMin], [HighRangeMax], [MediumRangeMin], [MediumRangeMax], [LowRangeMin], [LowRangeMax])
    VALUES (1, @TopicExternalId1, @ProjectModuleId, NULL,
        N'Modelo de Negocio', N'Tema referenciado por preguntas de diagnóstico.', 1,
        8.00, 10.00, 5.00, 7.99, 0.00, 4.99);

IF NOT EXISTS (SELECT 1 FROM [knowledge].[Topics] WHERE [Id] = 2)
    INSERT INTO [knowledge].[Topics] ([Id], [ExternalId], [ModuleId], [SourceTemplateTopicExternalId],
        [Name], [Description], [SortOrder],
        [HighRangeMin], [HighRangeMax], [MediumRangeMin], [MediumRangeMax], [LowRangeMin], [LowRangeMax])
    VALUES (2, @TopicExternalId2, @ProjectModuleId, NULL,
        N'Equipo', N'Tema referenciado por preguntas de diagnóstico.', 2,
        8.00, 10.00, 5.00, 7.99, 0.00, 4.99);

IF NOT EXISTS (SELECT 1 FROM [knowledge].[Topics] WHERE [Id] = 3)
    INSERT INTO [knowledge].[Topics] ([Id], [ExternalId], [ModuleId], [SourceTemplateTopicExternalId],
        [Name], [Description], [SortOrder],
        [HighRangeMin], [HighRangeMax], [MediumRangeMin], [MediumRangeMax], [LowRangeMin], [LowRangeMax])
    VALUES (3, @TopicExternalId3, @ProjectModuleId, NULL,
        N'Mercado', N'Tema referenciado por preguntas de diagnóstico.', 3,
        8.00, 10.00, 5.00, 7.99, 0.00, 4.99);

IF NOT EXISTS (SELECT 1 FROM [knowledge].[Topics] WHERE [Id] = 4)
    INSERT INTO [knowledge].[Topics] ([Id], [ExternalId], [ModuleId], [SourceTemplateTopicExternalId],
        [Name], [Description], [SortOrder],
        [HighRangeMin], [HighRangeMax], [MediumRangeMin], [MediumRangeMax], [LowRangeMin], [LowRangeMax])
    VALUES (4, @TopicExternalId4, @ProjectModuleId, NULL,
        N'Finanzas', N'Tema referenciado por preguntas de diagnóstico.', 4,
        8.00, 10.00, 5.00, 7.99, 0.00, 4.99);

IF NOT EXISTS (SELECT 1 FROM [knowledge].[Topics] WHERE [Id] = 5)
    INSERT INTO [knowledge].[Topics] ([Id], [ExternalId], [ModuleId], [SourceTemplateTopicExternalId],
        [Name], [Description], [SortOrder],
        [HighRangeMin], [HighRangeMax], [MediumRangeMin], [MediumRangeMax], [LowRangeMin], [LowRangeMax])
    VALUES (5, @TopicExternalId5, @ProjectModuleId, NULL,
        N'Impacto', N'Tema referenciado por preguntas de diagnóstico.', 5,
        8.00, 10.00, 5.00, 7.99, 0.00, 4.99);

SET IDENTITY_INSERT [knowledge].[Topics] OFF;


-- ------------------------------------------------------------------------------------------
-- KnowledgeStructure for Proyecto Norte Uno (spec 017 US6-3)
-- Materialized here at seed time; no Modules/Topics under it (the tenant-isolation tests
-- only need the KS row to exist so its ExternalId is resolvable).
-- UNIQUE constraint on ProjectId means each project has exactly one KS row.
-- ------------------------------------------------------------------------------------------
DECLARE @ProjectNorteKsExternalId UNIQUEIDENTIFIER = CAST('88888888-8888-8888-8888-888888880002' AS UNIQUEIDENTIFIER);

IF NOT EXISTS (SELECT 1 FROM [knowledge].[KnowledgeStructures] WHERE [ExternalId] = @ProjectNorteKsExternalId)
BEGIN
    INSERT INTO [knowledge].[KnowledgeStructures] ([ExternalId], [ProjectId], [IncubatorId], [Name], [Description],
        [SourceTemplateId], [SourceTemplateVersion], [SyncMode], [CreatedAtUtc])
    VALUES (@ProjectNorteKsExternalId, @ProjectNorteId, @IncubatorNorteId,
        N'Estructura de Conocimiento - Proyecto Norte Uno',
        N'Estructura vacía usada como fixture de aislamiento de inquilinos.',
        @StructureTemplateId, 1, 0, @Now);
END


-- ------------------------------------------------------------------------------------------
-- Project Stages: the domain's Project.Create factory inserts 7 ProjectStage rows per
-- project (Registration in progress, all others not started). The raw SQL inserts above
-- bypass the factory, so we materialize the same 7 rows here. Idempotent via the unique
-- (ProjectId, StageType) index plus a NOT EXISTS guard.
-- ------------------------------------------------------------------------------------------
;WITH SeedProjects AS (
    SELECT @Project1Id AS ProjectId
    UNION ALL SELECT @Project2Id
    UNION ALL SELECT @Project3Id
    UNION ALL SELECT @Project4Id
), SeedStages AS (
    SELECT
        p.ProjectId,
        s.StageType,
        CASE WHEN s.StageType = 0 THEN 1 ELSE 0 END AS [State],
        CASE WHEN s.StageType = 0 THEN @Now ELSE NULL END AS StartedAtUtc
    FROM SeedProjects p
    CROSS JOIN (VALUES (0),(1),(2),(3),(4),(5),(6)) s(StageType)
    WHERE p.ProjectId IS NOT NULL
)
INSERT INTO [tenant].[ProjectStages] ([ProjectId], [StageType], [State], [StartedAtUtc])
SELECT s.ProjectId, s.StageType, s.[State], s.StartedAtUtc
FROM SeedStages s
WHERE NOT EXISTS (
    SELECT 1 FROM [tenant].[ProjectStages] ps
    WHERE ps.ProjectId = s.ProjectId AND ps.StageType = s.StageType
);


-- ==========================================================================================
-- SECTION 4: Role Assignments
-- ==========================================================================================
-- Uses MERGE to be idempotent. The unique filtered index
-- UQ_RoleAssignments_Unique (UserId, IncubatorId, ProjectId, Role) WHERE IsActive=1
-- prevents duplicates, but MERGE avoids insert errors cleanly.

-- ------------------------------------------------------------------------------------------
-- IncubatorAdmin 1 -> Incubadora Alpha
-- ------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [access].[RoleAssignments] WHERE [UserId] = @IncAdmin1Id AND [IncubatorId] = @Incubator1Id AND [ProjectId] IS NULL AND [Role] = N'IncubatorAdmin' AND [IsActive] = 1)
BEGIN
    INSERT INTO [access].[RoleAssignments] ([ExternalId], [UserId], [IncubatorId], [ProjectId], [Role], [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES (NEWID(), @IncAdmin1Id, @Incubator1Id, NULL, N'IncubatorAdmin', 1, @Now, @Now);
END

-- ------------------------------------------------------------------------------------------
-- IncubatorAdmin 2 -> Incubadora Beta
-- ------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [access].[RoleAssignments] WHERE [UserId] = @IncAdmin2Id AND [IncubatorId] = @Incubator2Id AND [ProjectId] IS NULL AND [Role] = N'IncubatorAdmin' AND [IsActive] = 1)
BEGIN
    INSERT INTO [access].[RoleAssignments] ([ExternalId], [UserId], [IncubatorId], [ProjectId], [Role], [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES (NEWID(), @IncAdmin2Id, @Incubator2Id, NULL, N'IncubatorAdmin', 1, @Now, @Now);
END

-- ------------------------------------------------------------------------------------------
-- ProjectCoordinator 1 -> Incubadora Alpha, Proyecto Innovación
-- ------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [access].[RoleAssignments] WHERE [UserId] = @Coord1Id AND [IncubatorId] = @Incubator1Id AND [ProjectId] = @Project1Id AND [Role] = N'ProjectCoordinator' AND [IsActive] = 1)
BEGIN
    INSERT INTO [access].[RoleAssignments] ([ExternalId], [UserId], [IncubatorId], [ProjectId], [Role], [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES (NEWID(), @Coord1Id, @Incubator1Id, @Project1Id, N'ProjectCoordinator', 1, @Now, @Now);
END

-- ------------------------------------------------------------------------------------------
-- ProjectCoordinator 2 -> Incubadora Alpha, Proyecto Sostenibilidad
-- ------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [access].[RoleAssignments] WHERE [UserId] = @Coord2Id AND [IncubatorId] = @Incubator1Id AND [ProjectId] = @Project2Id AND [Role] = N'ProjectCoordinator' AND [IsActive] = 1)
BEGIN
    INSERT INTO [access].[RoleAssignments] ([ExternalId], [UserId], [IncubatorId], [ProjectId], [Role], [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES (NEWID(), @Coord2Id, @Incubator1Id, @Project2Id, N'ProjectCoordinator', 1, @Now, @Now);
END

-- ------------------------------------------------------------------------------------------
-- ProjectCoordinator 3 -> Incubadora Alpha, Proyecto Innovación
-- (single-role single-incubator; context selector auto-skips server-side, so coord3 logs in
-- with ActiveIncubatorId = Alpha. Used by WalkthroughAuditConcurrencyTests so three distinct
-- coordinators can advance the same Alpha-scoped test project and be attributed by name.)
-- ------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [access].[RoleAssignments] WHERE [UserId] = @Coord3Id AND [IncubatorId] = @Incubator1Id AND [ProjectId] = @Project1Id AND [Role] = N'ProjectCoordinator' AND [IsActive] = 1)
BEGIN
    INSERT INTO [access].[RoleAssignments] ([ExternalId], [UserId], [IncubatorId], [ProjectId], [Role], [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES (NEWID(), @Coord3Id, @Incubator1Id, @Project1Id, N'ProjectCoordinator', 1, @Now, @Now);
END

-- ------------------------------------------------------------------------------------------
-- ProjectCoordinator Norte -> Incubadora Norte (incubator-level base) + Proyecto Norte Uno
-- Spec 017 (knowledge-e2e-tests): required by US6-3 tenant-isolation E2E tests.
-- ------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [access].[RoleAssignments] WHERE [UserId] = @CoordNorteId AND [IncubatorId] = @IncubatorNorteId AND [ProjectId] IS NULL AND [Role] = N'ProjectCoordinator' AND [IsActive] = 1)
BEGIN
    INSERT INTO [access].[RoleAssignments] ([ExternalId], [UserId], [IncubatorId], [ProjectId], [Role], [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES (NEWID(), @CoordNorteId, @IncubatorNorteId, NULL, N'ProjectCoordinator', 1, @Now, @Now);
END

IF NOT EXISTS (SELECT 1 FROM [access].[RoleAssignments] WHERE [UserId] = @CoordNorteId AND [IncubatorId] = @IncubatorNorteId AND [ProjectId] = @ProjectNorteId AND [Role] = N'ProjectCoordinator' AND [IsActive] = 1)
BEGIN
    INSERT INTO [access].[RoleAssignments] ([ExternalId], [UserId], [IncubatorId], [ProjectId], [Role], [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES (NEWID(), @CoordNorteId, @IncubatorNorteId, @ProjectNorteId, N'ProjectCoordinator', 1, @Now, @Now);
END

-- ------------------------------------------------------------------------------------------
-- Mentor -> Incubadora Alpha, Proyecto Innovación
-- ------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [access].[RoleAssignments] WHERE [UserId] = @Mentor1Id AND [IncubatorId] = @Incubator1Id AND [ProjectId] = @Project1Id AND [Role] = N'Mentor' AND [IsActive] = 1)
BEGIN
    INSERT INTO [access].[RoleAssignments] ([ExternalId], [UserId], [IncubatorId], [ProjectId], [Role], [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES (NEWID(), @Mentor1Id, @Incubator1Id, @Project1Id, N'Mentor', 1, @Now, @Now);
END

-- ------------------------------------------------------------------------------------------
-- Entrepreneur 1 -> Incubadora Alpha, Proyecto Innovación
-- ------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [access].[RoleAssignments] WHERE [UserId] = @Entrep1Id AND [IncubatorId] = @Incubator1Id AND [ProjectId] = @Project1Id AND [Role] = N'Entrepreneur' AND [IsActive] = 1)
BEGIN
    INSERT INTO [access].[RoleAssignments] ([ExternalId], [UserId], [IncubatorId], [ProjectId], [Role], [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES (NEWID(), @Entrep1Id, @Incubator1Id, @Project1Id, N'Entrepreneur', 1, @Now, @Now);
END

-- ------------------------------------------------------------------------------------------
-- Entrepreneur 2 -> Incubadora Beta, Proyecto Digital
-- ------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [access].[RoleAssignments] WHERE [UserId] = @Entrep2Id AND [IncubatorId] = @Incubator2Id AND [ProjectId] = @Project3Id AND [Role] = N'Entrepreneur' AND [IsActive] = 1)
BEGIN
    INSERT INTO [access].[RoleAssignments] ([ExternalId], [UserId], [IncubatorId], [ProjectId], [Role], [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES (NEWID(), @Entrep2Id, @Incubator2Id, @Project3Id, N'Entrepreneur', 1, @Now, @Now);
END

-- ------------------------------------------------------------------------------------------
-- Sponsor -> Incubadora Alpha (incubator-level, no project)
-- ------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [access].[RoleAssignments] WHERE [UserId] = @Sponsor1Id AND [IncubatorId] = @Incubator1Id AND [ProjectId] IS NULL AND [Role] = N'Sponsor' AND [IsActive] = 1)
BEGIN
    INSERT INTO [access].[RoleAssignments] ([ExternalId], [UserId], [IncubatorId], [ProjectId], [Role], [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES (NEWID(), @Sponsor1Id, @Incubator1Id, NULL, N'Sponsor', 1, @Now, @Now);
END

-- ------------------------------------------------------------------------------------------
-- Multi-Role User: All roles across both incubators (2+ projects per incubator)
-- Covers: GlobalAdmin, IncubatorAdmin, ProjectCoordinator, Mentor, Entrepreneur, Sponsor
-- ------------------------------------------------------------------------------------------

-- GlobalAdmin (incubator-scoped base assignment — cascade shows all incubators)
IF NOT EXISTS (SELECT 1 FROM [access].[RoleAssignments] WHERE [UserId] = @MultiRoleId AND [IncubatorId] = @Incubator1Id AND [ProjectId] IS NULL AND [Role] = N'GlobalAdmin' AND [IsActive] = 1)
BEGIN
    INSERT INTO [access].[RoleAssignments] ([ExternalId], [UserId], [IncubatorId], [ProjectId], [Role], [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES (NEWID(), @MultiRoleId, @Incubator1Id, NULL, N'GlobalAdmin', 1, @Now, @Now);
END

-- IncubatorAdmin @ Alpha
IF NOT EXISTS (SELECT 1 FROM [access].[RoleAssignments] WHERE [UserId] = @MultiRoleId AND [IncubatorId] = @Incubator1Id AND [ProjectId] IS NULL AND [Role] = N'IncubatorAdmin' AND [IsActive] = 1)
BEGIN
    INSERT INTO [access].[RoleAssignments] ([ExternalId], [UserId], [IncubatorId], [ProjectId], [Role], [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES (NEWID(), @MultiRoleId, @Incubator1Id, NULL, N'IncubatorAdmin', 1, @Now, @Now);
END

-- IncubatorAdmin @ Beta
IF NOT EXISTS (SELECT 1 FROM [access].[RoleAssignments] WHERE [UserId] = @MultiRoleId AND [IncubatorId] = @Incubator2Id AND [ProjectId] IS NULL AND [Role] = N'IncubatorAdmin' AND [IsActive] = 1)
BEGIN
    INSERT INTO [access].[RoleAssignments] ([ExternalId], [UserId], [IncubatorId], [ProjectId], [Role], [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES (NEWID(), @MultiRoleId, @Incubator2Id, NULL, N'IncubatorAdmin', 1, @Now, @Now);
END

-- ProjectCoordinator @ Alpha / Innovación
IF NOT EXISTS (SELECT 1 FROM [access].[RoleAssignments] WHERE [UserId] = @MultiRoleId AND [IncubatorId] = @Incubator1Id AND [ProjectId] = @Project1Id AND [Role] = N'ProjectCoordinator' AND [IsActive] = 1)
BEGIN
    INSERT INTO [access].[RoleAssignments] ([ExternalId], [UserId], [IncubatorId], [ProjectId], [Role], [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES (NEWID(), @MultiRoleId, @Incubator1Id, @Project1Id, N'ProjectCoordinator', 1, @Now, @Now);
END

-- ProjectCoordinator @ Alpha / Sostenibilidad
IF NOT EXISTS (SELECT 1 FROM [access].[RoleAssignments] WHERE [UserId] = @MultiRoleId AND [IncubatorId] = @Incubator1Id AND [ProjectId] = @Project2Id AND [Role] = N'ProjectCoordinator' AND [IsActive] = 1)
BEGIN
    INSERT INTO [access].[RoleAssignments] ([ExternalId], [UserId], [IncubatorId], [ProjectId], [Role], [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES (NEWID(), @MultiRoleId, @Incubator1Id, @Project2Id, N'ProjectCoordinator', 1, @Now, @Now);
END

-- ProjectCoordinator @ Beta / Digital
IF NOT EXISTS (SELECT 1 FROM [access].[RoleAssignments] WHERE [UserId] = @MultiRoleId AND [IncubatorId] = @Incubator2Id AND [ProjectId] = @Project3Id AND [Role] = N'ProjectCoordinator' AND [IsActive] = 1)
BEGIN
    INSERT INTO [access].[RoleAssignments] ([ExternalId], [UserId], [IncubatorId], [ProjectId], [Role], [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES (NEWID(), @MultiRoleId, @Incubator2Id, @Project3Id, N'ProjectCoordinator', 1, @Now, @Now);
END

-- ProjectCoordinator @ Beta / Comunitario
IF NOT EXISTS (SELECT 1 FROM [access].[RoleAssignments] WHERE [UserId] = @MultiRoleId AND [IncubatorId] = @Incubator2Id AND [ProjectId] = @Project4Id AND [Role] = N'ProjectCoordinator' AND [IsActive] = 1)
BEGIN
    INSERT INTO [access].[RoleAssignments] ([ExternalId], [UserId], [IncubatorId], [ProjectId], [Role], [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES (NEWID(), @MultiRoleId, @Incubator2Id, @Project4Id, N'ProjectCoordinator', 1, @Now, @Now);
END

-- Mentor @ Alpha / Innovación
IF NOT EXISTS (SELECT 1 FROM [access].[RoleAssignments] WHERE [UserId] = @MultiRoleId AND [IncubatorId] = @Incubator1Id AND [ProjectId] = @Project1Id AND [Role] = N'Mentor' AND [IsActive] = 1)
BEGIN
    INSERT INTO [access].[RoleAssignments] ([ExternalId], [UserId], [IncubatorId], [ProjectId], [Role], [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES (NEWID(), @MultiRoleId, @Incubator1Id, @Project1Id, N'Mentor', 1, @Now, @Now);
END

-- Mentor @ Alpha / Sostenibilidad
IF NOT EXISTS (SELECT 1 FROM [access].[RoleAssignments] WHERE [UserId] = @MultiRoleId AND [IncubatorId] = @Incubator1Id AND [ProjectId] = @Project2Id AND [Role] = N'Mentor' AND [IsActive] = 1)
BEGIN
    INSERT INTO [access].[RoleAssignments] ([ExternalId], [UserId], [IncubatorId], [ProjectId], [Role], [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES (NEWID(), @MultiRoleId, @Incubator1Id, @Project2Id, N'Mentor', 1, @Now, @Now);
END

-- Mentor @ Beta / Digital
IF NOT EXISTS (SELECT 1 FROM [access].[RoleAssignments] WHERE [UserId] = @MultiRoleId AND [IncubatorId] = @Incubator2Id AND [ProjectId] = @Project3Id AND [Role] = N'Mentor' AND [IsActive] = 1)
BEGIN
    INSERT INTO [access].[RoleAssignments] ([ExternalId], [UserId], [IncubatorId], [ProjectId], [Role], [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES (NEWID(), @MultiRoleId, @Incubator2Id, @Project3Id, N'Mentor', 1, @Now, @Now);
END

-- Mentor @ Beta / Comunitario
IF NOT EXISTS (SELECT 1 FROM [access].[RoleAssignments] WHERE [UserId] = @MultiRoleId AND [IncubatorId] = @Incubator2Id AND [ProjectId] = @Project4Id AND [Role] = N'Mentor' AND [IsActive] = 1)
BEGIN
    INSERT INTO [access].[RoleAssignments] ([ExternalId], [UserId], [IncubatorId], [ProjectId], [Role], [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES (NEWID(), @MultiRoleId, @Incubator2Id, @Project4Id, N'Mentor', 1, @Now, @Now);
END

-- Entrepreneur @ Alpha / Innovación
IF NOT EXISTS (SELECT 1 FROM [access].[RoleAssignments] WHERE [UserId] = @MultiRoleId AND [IncubatorId] = @Incubator1Id AND [ProjectId] = @Project1Id AND [Role] = N'Entrepreneur' AND [IsActive] = 1)
BEGIN
    INSERT INTO [access].[RoleAssignments] ([ExternalId], [UserId], [IncubatorId], [ProjectId], [Role], [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES (NEWID(), @MultiRoleId, @Incubator1Id, @Project1Id, N'Entrepreneur', 1, @Now, @Now);
END

-- Entrepreneur @ Alpha / Sostenibilidad
IF NOT EXISTS (SELECT 1 FROM [access].[RoleAssignments] WHERE [UserId] = @MultiRoleId AND [IncubatorId] = @Incubator1Id AND [ProjectId] = @Project2Id AND [Role] = N'Entrepreneur' AND [IsActive] = 1)
BEGIN
    INSERT INTO [access].[RoleAssignments] ([ExternalId], [UserId], [IncubatorId], [ProjectId], [Role], [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES (NEWID(), @MultiRoleId, @Incubator1Id, @Project2Id, N'Entrepreneur', 1, @Now, @Now);
END

-- Entrepreneur @ Beta / Digital
IF NOT EXISTS (SELECT 1 FROM [access].[RoleAssignments] WHERE [UserId] = @MultiRoleId AND [IncubatorId] = @Incubator2Id AND [ProjectId] = @Project3Id AND [Role] = N'Entrepreneur' AND [IsActive] = 1)
BEGIN
    INSERT INTO [access].[RoleAssignments] ([ExternalId], [UserId], [IncubatorId], [ProjectId], [Role], [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES (NEWID(), @MultiRoleId, @Incubator2Id, @Project3Id, N'Entrepreneur', 1, @Now, @Now);
END

-- Entrepreneur @ Beta / Comunitario
IF NOT EXISTS (SELECT 1 FROM [access].[RoleAssignments] WHERE [UserId] = @MultiRoleId AND [IncubatorId] = @Incubator2Id AND [ProjectId] = @Project4Id AND [Role] = N'Entrepreneur' AND [IsActive] = 1)
BEGIN
    INSERT INTO [access].[RoleAssignments] ([ExternalId], [UserId], [IncubatorId], [ProjectId], [Role], [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES (NEWID(), @MultiRoleId, @Incubator2Id, @Project4Id, N'Entrepreneur', 1, @Now, @Now);
END

-- Sponsor @ Alpha
IF NOT EXISTS (SELECT 1 FROM [access].[RoleAssignments] WHERE [UserId] = @MultiRoleId AND [IncubatorId] = @Incubator1Id AND [ProjectId] IS NULL AND [Role] = N'Sponsor' AND [IsActive] = 1)
BEGIN
    INSERT INTO [access].[RoleAssignments] ([ExternalId], [UserId], [IncubatorId], [ProjectId], [Role], [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES (NEWID(), @MultiRoleId, @Incubator1Id, NULL, N'Sponsor', 1, @Now, @Now);
END

-- Sponsor @ Beta
IF NOT EXISTS (SELECT 1 FROM [access].[RoleAssignments] WHERE [UserId] = @MultiRoleId AND [IncubatorId] = @Incubator2Id AND [ProjectId] IS NULL AND [Role] = N'Sponsor' AND [IsActive] = 1)
BEGIN
    INSERT INTO [access].[RoleAssignments] ([ExternalId], [UserId], [IncubatorId], [ProjectId], [Role], [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
    VALUES (NEWID(), @MultiRoleId, @Incubator2Id, NULL, N'Sponsor', 1, @Now, @Now);
END


-- ==========================================================================================
-- SECTION 5: Form Templates
-- ==========================================================================================

DECLARE @FormTemplate1Id BIGINT;  -- Diagnóstico Inicial Estándar
DECLARE @FormTemplate2Id BIGINT;  -- Diagnóstico de Impacto

-- ------------------------------------------------------------------------------------------
-- FormTemplate: Diagnóstico Inicial Estándar
-- ------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [diagnostic].[FormTemplates] WHERE [Name] = N'Diagnóstico Inicial Estándar')
BEGIN
    INSERT INTO [diagnostic].[FormTemplates] ([ExternalId], [Name], [Description], [SubscriptionTier], [Version], [IsActive], [CreatedAtUtc])
    VALUES (NEWID(), N'Diagnóstico Inicial Estándar', N'Formulario estándar para diagnóstico inicial de emprendimientos en etapa temprana', N'Basic', 1, 1, @Now);

    SET @FormTemplate1Id = SCOPE_IDENTITY();
END
ELSE
    SELECT @FormTemplate1Id = [Id] FROM [diagnostic].[FormTemplates] WHERE [Name] = N'Diagnóstico Inicial Estándar';

-- ------------------------------------------------------------------------------------------
-- FormTemplate: Diagnóstico de Impacto
-- ------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [diagnostic].[FormTemplates] WHERE [Name] = N'Diagnóstico de Impacto')
BEGIN
    INSERT INTO [diagnostic].[FormTemplates] ([ExternalId], [Name], [Description], [SubscriptionTier], [Version], [IsActive], [CreatedAtUtc])
    VALUES (NEWID(), N'Diagnóstico de Impacto', N'Formulario para evaluar el impacto social y económico del emprendimiento', N'Basic', 1, 1, @Now);

    SET @FormTemplate2Id = SCOPE_IDENTITY();
END
ELSE
    SELECT @FormTemplate2Id = [Id] FROM [diagnostic].[FormTemplates] WHERE [Name] = N'Diagnóstico de Impacto';


-- ==========================================================================================
-- SECTION 6: QuestionTemplates and AnswerOptionTemplates
-- ==========================================================================================
-- TopicId convention: 1=Modelo de Negocio, 2=Equipo, 3=Mercado, 4=Finanzas, 5=Impacto

-- ------------------------------------------------------------------------------------------
-- QuestionTemplates for FormTemplate 1: Diagnóstico Inicial Estándar
-- ------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [diagnostic].[QuestionTemplates] WHERE [FormTemplateId] = @FormTemplate1Id)
BEGIN
    DECLARE @QT1_1 BIGINT, @QT1_2 BIGINT, @QT1_3 BIGINT, @QT1_4 BIGINT, @QT1_5 BIGINT, @QT1_6 BIGINT;

    -- Q1: Text question — Business model description
    INSERT INTO [diagnostic].[QuestionTemplates] ([FormTemplateId], [TopicId], [QuestionText], [QuestionType], [StageApplicability], [SortOrder], [BlockGroup], [IsOptional])
    VALUES (@FormTemplate1Id, 1, N'Describa brevemente su modelo de negocio y propuesta de valor', 0, 0, 1, N'Modelo de Negocio', 0);
    SET @QT1_1 = SCOPE_IDENTITY();

    -- Q2: SingleSelect — Team maturity
    INSERT INTO [diagnostic].[QuestionTemplates] ([FormTemplateId], [TopicId], [QuestionText], [QuestionType], [StageApplicability], [SortOrder], [BlockGroup], [IsOptional])
    VALUES (@FormTemplate1Id, 2, N'¿Cuál es el nivel de experiencia del equipo fundador en el sector?', 2, 0, 2, N'Equipo', 0);
    SET @QT1_2 = SCOPE_IDENTITY();

    -- AnswerOptions for Q2
    INSERT INTO [diagnostic].[AnswerOptionTemplates] ([QuestionTemplateId], [OptionText], [Score], [SwotClassification], [OdsrOrientation], [SortOrder])
    VALUES
        (@QT1_2, N'Sin experiencia previa en el sector',           1.00, 2, 0, 1),  -- Weakness
        (@QT1_2, N'1-2 años de experiencia en el sector',          2.00, 0, 0, 2),  -- None
        (@QT1_2, N'3-5 años de experiencia en el sector',          3.00, 1, 0, 3),  -- Strength
        (@QT1_2, N'Más de 5 años de experiencia especializada',    4.00, 1, 0, 4);  -- Strength

    -- Q3: Numeric — Number of potential clients identified
    INSERT INTO [diagnostic].[QuestionTemplates] ([FormTemplateId], [TopicId], [QuestionText], [QuestionType], [StageApplicability], [SortOrder], [BlockGroup], [IsOptional])
    VALUES (@FormTemplate1Id, 3, N'¿Cuántos clientes potenciales ha identificado en su mercado objetivo?', 1, 0, 3, N'Mercado', 0);
    SET @QT1_3 = SCOPE_IDENTITY();

    -- Q4: SingleSelect — Revenue model clarity
    INSERT INTO [diagnostic].[QuestionTemplates] ([FormTemplateId], [TopicId], [QuestionText], [QuestionType], [StageApplicability], [SortOrder], [BlockGroup], [IsOptional])
    VALUES (@FormTemplate1Id, 4, N'¿Tiene definido un modelo de ingresos claro?', 2, 0, 4, N'Finanzas', 0);
    SET @QT1_4 = SCOPE_IDENTITY();

    -- AnswerOptions for Q4
    INSERT INTO [diagnostic].[AnswerOptionTemplates] ([QuestionTemplateId], [OptionText], [Score], [SwotClassification], [OdsrOrientation], [SortOrder])
    VALUES
        (@QT1_4, N'No tengo modelo de ingresos definido',          1.00, 2, 0, 1),  -- Weakness
        (@QT1_4, N'Tengo una idea general pero no está validada',  2.00, 4, 0, 2),  -- Threat
        (@QT1_4, N'Modelo definido con proyecciones iniciales',    3.00, 3, 0, 3),  -- Opportunity
        (@QT1_4, N'Modelo validado con ingresos reales',           4.00, 1, 0, 4);  -- Strength

    -- Q5: SingleSelect — Competitive advantage
    INSERT INTO [diagnostic].[QuestionTemplates] ([FormTemplateId], [TopicId], [QuestionText], [QuestionType], [StageApplicability], [SortOrder], [BlockGroup], [IsOptional])
    VALUES (@FormTemplate1Id, 3, N'¿Cuál es su principal ventaja competitiva frente a alternativas existentes?', 2, 0, 5, N'Mercado', 0);
    SET @QT1_5 = SCOPE_IDENTITY();

    -- AnswerOptions for Q5
    INSERT INTO [diagnostic].[AnswerOptionTemplates] ([QuestionTemplateId], [OptionText], [Score], [SwotClassification], [OdsrOrientation], [SortOrder])
    VALUES
        (@QT1_5, N'No he identificado una ventaja clara',           1.00, 2, 0, 1),  -- Weakness
        (@QT1_5, N'Precio más competitivo',                         2.00, 3, 0, 2),  -- Opportunity
        (@QT1_5, N'Tecnología o innovación diferenciadora',         3.00, 1, 0, 3),  -- Strength
        (@QT1_5, N'Acceso exclusivo a recursos o mercado',          4.00, 1, 0, 4);  -- Strength

    -- Q6: Text question — Biggest challenge (optional)
    INSERT INTO [diagnostic].[QuestionTemplates] ([FormTemplateId], [TopicId], [QuestionText], [QuestionType], [StageApplicability], [SortOrder], [BlockGroup], [IsOptional])
    VALUES (@FormTemplate1Id, 1, N'¿Cuál es el mayor desafío que enfrenta actualmente su emprendimiento?', 0, 0, 6, N'Modelo de Negocio', 1);
    SET @QT1_6 = SCOPE_IDENTITY();
END

-- ------------------------------------------------------------------------------------------
-- QuestionTemplates for FormTemplate 2: Diagnóstico de Impacto
-- ------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [diagnostic].[QuestionTemplates] WHERE [FormTemplateId] = @FormTemplate2Id)
BEGIN
    DECLARE @QT2_1 BIGINT, @QT2_2 BIGINT, @QT2_3 BIGINT, @QT2_4 BIGINT, @QT2_5 BIGINT;

    -- Q1: SingleSelect — Social impact scope
    INSERT INTO [diagnostic].[QuestionTemplates] ([FormTemplateId], [TopicId], [QuestionText], [QuestionType], [StageApplicability], [SortOrder], [BlockGroup], [IsOptional])
    VALUES (@FormTemplate2Id, 5, N'¿Cuál es el alcance del impacto social de su emprendimiento?', 2, 0, 1, N'Impacto Social', 0);
    SET @QT2_1 = SCOPE_IDENTITY();

    -- AnswerOptions for Q1
    INSERT INTO [diagnostic].[AnswerOptionTemplates] ([QuestionTemplateId], [OptionText], [Score], [SwotClassification], [OdsrOrientation], [SortOrder])
    VALUES
        (@QT2_1, N'Impacto local (barrio o comuna)',               1.00, 0, 0, 1),
        (@QT2_1, N'Impacto regional',                              2.00, 3, 0, 2),  -- Opportunity
        (@QT2_1, N'Impacto nacional',                              3.00, 1, 0, 3),  -- Strength
        (@QT2_1, N'Impacto internacional',                         4.00, 1, 0, 4);  -- Strength

    -- Q2: Numeric — Beneficiaries count
    INSERT INTO [diagnostic].[QuestionTemplates] ([FormTemplateId], [TopicId], [QuestionText], [QuestionType], [StageApplicability], [SortOrder], [BlockGroup], [IsOptional])
    VALUES (@FormTemplate2Id, 5, N'¿Cuántos beneficiarios directos tiene o proyecta tener en el primer año?', 1, 0, 2, N'Impacto Social', 0);
    SET @QT2_2 = SCOPE_IDENTITY();

    -- Q3: SingleSelect — Environmental sustainability
    INSERT INTO [diagnostic].[QuestionTemplates] ([FormTemplateId], [TopicId], [QuestionText], [QuestionType], [StageApplicability], [SortOrder], [BlockGroup], [IsOptional])
    VALUES (@FormTemplate2Id, 5, N'¿Su emprendimiento incorpora prácticas de sostenibilidad ambiental?', 2, 0, 3, N'Sostenibilidad', 0);
    SET @QT2_3 = SCOPE_IDENTITY();

    -- AnswerOptions for Q3
    INSERT INTO [diagnostic].[AnswerOptionTemplates] ([QuestionTemplateId], [OptionText], [Score], [SwotClassification], [OdsrOrientation], [SortOrder])
    VALUES
        (@QT2_3, N'No se han considerado prácticas ambientales',    1.00, 4, 0, 1),  -- Threat
        (@QT2_3, N'Se planean implementar a futuro',                2.00, 3, 0, 2),  -- Opportunity
        (@QT2_3, N'Se implementan parcialmente',                    3.00, 1, 0, 3),  -- Strength
        (@QT2_3, N'Están integradas en el modelo de negocio',       4.00, 1, 0, 4);  -- Strength

    -- Q4: Text — Impact measurement description
    INSERT INTO [diagnostic].[QuestionTemplates] ([FormTemplateId], [TopicId], [QuestionText], [QuestionType], [StageApplicability], [SortOrder], [BlockGroup], [IsOptional])
    VALUES (@FormTemplate2Id, 5, N'Describa cómo mide o planea medir el impacto de su emprendimiento', 0, 0, 4, N'Medición', 0);
    SET @QT2_4 = SCOPE_IDENTITY();

    -- Q5: SingleSelect — Job creation
    INSERT INTO [diagnostic].[QuestionTemplates] ([FormTemplateId], [TopicId], [QuestionText], [QuestionType], [StageApplicability], [SortOrder], [BlockGroup], [IsOptional])
    VALUES (@FormTemplate2Id, 5, N'¿Cuántos empleos directos ha generado o proyecta generar?', 2, 1, 5, N'Impacto Económico', 0);
    SET @QT2_5 = SCOPE_IDENTITY();

    -- AnswerOptions for Q5
    INSERT INTO [diagnostic].[AnswerOptionTemplates] ([QuestionTemplateId], [OptionText], [Score], [SwotClassification], [OdsrOrientation], [SortOrder])
    VALUES
        (@QT2_5, N'Solo autoempleo (1 persona)',                    1.00, 2, 0, 1),  -- Weakness
        (@QT2_5, N'2-5 empleos',                                   2.00, 3, 0, 2),  -- Opportunity
        (@QT2_5, N'6-20 empleos',                                  3.00, 1, 0, 3),  -- Strength
        (@QT2_5, N'Más de 20 empleos',                             4.00, 1, 0, 4);  -- Strength
END


-- ==========================================================================================
-- SECTION 7: Project Forms (cloned from templates)
-- ==========================================================================================

DECLARE @ProjectForm1Id BIGINT;  -- Diagnóstico Inicial for Project 1
DECLARE @ProjectForm2Id BIGINT;  -- Diagnóstico de Impacto for Project 1
DECLARE @ProjectForm3Id BIGINT;  -- Diagnóstico Inicial for Project 3

-- ------------------------------------------------------------------------------------------
-- ProjectForm 1: Diagnóstico Inicial Estándar -> Proyecto Innovación (Inc Alpha)
-- ------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [diagnostic].[ProjectForms] WHERE [Name] = N'Diagnóstico Inicial Estándar' AND [ProjectId] = @Project1Id AND [IncubatorId] = @Incubator1Id)
BEGIN
    INSERT INTO [diagnostic].[ProjectForms] ([ExternalId], [ProjectId], [IncubatorId], [SourceTemplateId], [SourceTemplateVersion], [Name], [SyncMode], [CreatedAtUtc])
    VALUES (NEWID(), @Project1Id, @Incubator1Id, @FormTemplate1Id, 1, N'Diagnóstico Inicial Estándar', 0, @Now);

    SET @ProjectForm1Id = SCOPE_IDENTITY();
END
ELSE
    SELECT @ProjectForm1Id = [Id] FROM [diagnostic].[ProjectForms] WHERE [Name] = N'Diagnóstico Inicial Estándar' AND [ProjectId] = @Project1Id AND [IncubatorId] = @Incubator1Id;

-- ------------------------------------------------------------------------------------------
-- ProjectForm 2: Diagnóstico de Impacto -> Proyecto Innovación (Inc Alpha)
-- ------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [diagnostic].[ProjectForms] WHERE [Name] = N'Diagnóstico de Impacto' AND [ProjectId] = @Project1Id AND [IncubatorId] = @Incubator1Id)
BEGIN
    INSERT INTO [diagnostic].[ProjectForms] ([ExternalId], [ProjectId], [IncubatorId], [SourceTemplateId], [SourceTemplateVersion], [Name], [SyncMode], [CreatedAtUtc])
    VALUES (NEWID(), @Project1Id, @Incubator1Id, @FormTemplate2Id, 1, N'Diagnóstico de Impacto', 0, @Now);

    SET @ProjectForm2Id = SCOPE_IDENTITY();
END
ELSE
    SELECT @ProjectForm2Id = [Id] FROM [diagnostic].[ProjectForms] WHERE [Name] = N'Diagnóstico de Impacto' AND [ProjectId] = @Project1Id AND [IncubatorId] = @Incubator1Id;

-- ------------------------------------------------------------------------------------------
-- ProjectForm 3: Diagnóstico Inicial Estándar -> Proyecto Digital (Inc Beta)
-- ------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [diagnostic].[ProjectForms] WHERE [Name] = N'Diagnóstico Inicial Estándar' AND [ProjectId] = @Project3Id AND [IncubatorId] = @Incubator2Id)
BEGIN
    INSERT INTO [diagnostic].[ProjectForms] ([ExternalId], [ProjectId], [IncubatorId], [SourceTemplateId], [SourceTemplateVersion], [Name], [SyncMode], [CreatedAtUtc])
    VALUES (NEWID(), @Project3Id, @Incubator2Id, @FormTemplate1Id, 1, N'Diagnóstico Inicial Estándar', 0, @Now);

    SET @ProjectForm3Id = SCOPE_IDENTITY();
END
ELSE
    SELECT @ProjectForm3Id = [Id] FROM [diagnostic].[ProjectForms] WHERE [Name] = N'Diagnóstico Inicial Estándar' AND [ProjectId] = @Project3Id AND [IncubatorId] = @Incubator2Id;


-- ==========================================================================================
-- SECTION 8: Questions and AnswerOptions (cloned into ProjectForms)
-- ==========================================================================================
-- Clones the QuestionTemplate + AnswerOptionTemplate data into the actual Questions and
-- AnswerOptions tables for each ProjectForm. Only runs if no questions exist yet.

-- ------------------------------------------------------------------------------------------
-- Questions for ProjectForm 1 (Diagnóstico Inicial -> Proyecto Innovación)
-- ------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [diagnostic].[Questions] WHERE [ProjectFormId] = @ProjectForm1Id)
BEGIN
    DECLARE @PF1_Q1 BIGINT, @PF1_Q2 BIGINT, @PF1_Q3 BIGINT, @PF1_Q4 BIGINT, @PF1_Q5 BIGINT, @PF1_Q6 BIGINT;

    INSERT INTO [diagnostic].[Questions] ([ExternalId], [ProjectFormId], [TopicId], [QuestionText], [QuestionType], [StageApplicability], [SortOrder], [BlockGroup], [IsOptional])
    VALUES (NEWID(), @ProjectForm1Id, 1, N'Describa brevemente su modelo de negocio y propuesta de valor', 0, 0, 1, N'Modelo de Negocio', 0);
    SET @PF1_Q1 = SCOPE_IDENTITY();

    INSERT INTO [diagnostic].[Questions] ([ExternalId], [ProjectFormId], [TopicId], [QuestionText], [QuestionType], [StageApplicability], [SortOrder], [BlockGroup], [IsOptional])
    VALUES (NEWID(), @ProjectForm1Id, 2, N'¿Cuál es el nivel de experiencia del equipo fundador en el sector?', 2, 0, 2, N'Equipo', 0);
    SET @PF1_Q2 = SCOPE_IDENTITY();

    INSERT INTO [diagnostic].[AnswerOptions] ([QuestionId], [OptionText], [Score], [SwotClassification], [OdsrOrientation], [SortOrder])
    VALUES
        (@PF1_Q2, N'Sin experiencia previa en el sector',           1.00, 2, 0, 1),
        (@PF1_Q2, N'1-2 años de experiencia en el sector',          2.00, 0, 0, 2),
        (@PF1_Q2, N'3-5 años de experiencia en el sector',          3.00, 1, 0, 3),
        (@PF1_Q2, N'Más de 5 años de experiencia especializada',    4.00, 1, 0, 4);

    INSERT INTO [diagnostic].[Questions] ([ExternalId], [ProjectFormId], [TopicId], [QuestionText], [QuestionType], [StageApplicability], [SortOrder], [BlockGroup], [IsOptional])
    VALUES (NEWID(), @ProjectForm1Id, 3, N'¿Cuántos clientes potenciales ha identificado en su mercado objetivo?', 1, 0, 3, N'Mercado', 0);
    SET @PF1_Q3 = SCOPE_IDENTITY();

    INSERT INTO [diagnostic].[Questions] ([ExternalId], [ProjectFormId], [TopicId], [QuestionText], [QuestionType], [StageApplicability], [SortOrder], [BlockGroup], [IsOptional])
    VALUES (NEWID(), @ProjectForm1Id, 4, N'¿Tiene definido un modelo de ingresos claro?', 2, 0, 4, N'Finanzas', 0);
    SET @PF1_Q4 = SCOPE_IDENTITY();

    INSERT INTO [diagnostic].[AnswerOptions] ([QuestionId], [OptionText], [Score], [SwotClassification], [OdsrOrientation], [SortOrder])
    VALUES
        (@PF1_Q4, N'No tengo modelo de ingresos definido',          1.00, 2, 0, 1),
        (@PF1_Q4, N'Tengo una idea general pero no está validada',  2.00, 4, 0, 2),
        (@PF1_Q4, N'Modelo definido con proyecciones iniciales',    3.00, 3, 0, 3),
        (@PF1_Q4, N'Modelo validado con ingresos reales',           4.00, 1, 0, 4);

    INSERT INTO [diagnostic].[Questions] ([ExternalId], [ProjectFormId], [TopicId], [QuestionText], [QuestionType], [StageApplicability], [SortOrder], [BlockGroup], [IsOptional])
    VALUES (NEWID(), @ProjectForm1Id, 3, N'¿Cuál es su principal ventaja competitiva frente a alternativas existentes?', 2, 0, 5, N'Mercado', 0);
    SET @PF1_Q5 = SCOPE_IDENTITY();

    INSERT INTO [diagnostic].[AnswerOptions] ([QuestionId], [OptionText], [Score], [SwotClassification], [OdsrOrientation], [SortOrder])
    VALUES
        (@PF1_Q5, N'No he identificado una ventaja clara',           1.00, 2, 0, 1),
        (@PF1_Q5, N'Precio más competitivo',                         2.00, 3, 0, 2),
        (@PF1_Q5, N'Tecnología o innovación diferenciadora',         3.00, 1, 0, 3),
        (@PF1_Q5, N'Acceso exclusivo a recursos o mercado',          4.00, 1, 0, 4);

    INSERT INTO [diagnostic].[Questions] ([ExternalId], [ProjectFormId], [TopicId], [QuestionText], [QuestionType], [StageApplicability], [SortOrder], [BlockGroup], [IsOptional])
    VALUES (NEWID(), @ProjectForm1Id, 1, N'¿Cuál es el mayor desafío que enfrenta actualmente su emprendimiento?', 0, 0, 6, N'Modelo de Negocio', 1);
    SET @PF1_Q6 = SCOPE_IDENTITY();
END

-- ------------------------------------------------------------------------------------------
-- Questions for ProjectForm 2 (Diagnóstico de Impacto -> Proyecto Innovación)
-- ------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [diagnostic].[Questions] WHERE [ProjectFormId] = @ProjectForm2Id)
BEGIN
    DECLARE @PF2_Q1 BIGINT, @PF2_Q2 BIGINT, @PF2_Q3 BIGINT, @PF2_Q4 BIGINT, @PF2_Q5 BIGINT;

    INSERT INTO [diagnostic].[Questions] ([ExternalId], [ProjectFormId], [TopicId], [QuestionText], [QuestionType], [StageApplicability], [SortOrder], [BlockGroup], [IsOptional])
    VALUES (NEWID(), @ProjectForm2Id, 5, N'¿Cuál es el alcance del impacto social de su emprendimiento?', 2, 0, 1, N'Impacto Social', 0);
    SET @PF2_Q1 = SCOPE_IDENTITY();

    INSERT INTO [diagnostic].[AnswerOptions] ([QuestionId], [OptionText], [Score], [SwotClassification], [OdsrOrientation], [SortOrder])
    VALUES
        (@PF2_Q1, N'Impacto local (barrio o comuna)',               1.00, 0, 0, 1),
        (@PF2_Q1, N'Impacto regional',                              2.00, 3, 0, 2),
        (@PF2_Q1, N'Impacto nacional',                              3.00, 1, 0, 3),
        (@PF2_Q1, N'Impacto internacional',                         4.00, 1, 0, 4);

    INSERT INTO [diagnostic].[Questions] ([ExternalId], [ProjectFormId], [TopicId], [QuestionText], [QuestionType], [StageApplicability], [SortOrder], [BlockGroup], [IsOptional])
    VALUES (NEWID(), @ProjectForm2Id, 5, N'¿Cuántos beneficiarios directos tiene o proyecta tener en el primer año?', 1, 0, 2, N'Impacto Social', 0);
    SET @PF2_Q2 = SCOPE_IDENTITY();

    INSERT INTO [diagnostic].[Questions] ([ExternalId], [ProjectFormId], [TopicId], [QuestionText], [QuestionType], [StageApplicability], [SortOrder], [BlockGroup], [IsOptional])
    VALUES (NEWID(), @ProjectForm2Id, 5, N'¿Su emprendimiento incorpora prácticas de sostenibilidad ambiental?', 2, 0, 3, N'Sostenibilidad', 0);
    SET @PF2_Q3 = SCOPE_IDENTITY();

    INSERT INTO [diagnostic].[AnswerOptions] ([QuestionId], [OptionText], [Score], [SwotClassification], [OdsrOrientation], [SortOrder])
    VALUES
        (@PF2_Q3, N'No se han considerado prácticas ambientales',    1.00, 4, 0, 1),
        (@PF2_Q3, N'Se planean implementar a futuro',                2.00, 3, 0, 2),
        (@PF2_Q3, N'Se implementan parcialmente',                    3.00, 1, 0, 3),
        (@PF2_Q3, N'Están integradas en el modelo de negocio',       4.00, 1, 0, 4);

    INSERT INTO [diagnostic].[Questions] ([ExternalId], [ProjectFormId], [TopicId], [QuestionText], [QuestionType], [StageApplicability], [SortOrder], [BlockGroup], [IsOptional])
    VALUES (NEWID(), @ProjectForm2Id, 5, N'Describa cómo mide o planea medir el impacto de su emprendimiento', 0, 0, 4, N'Medición', 0);
    SET @PF2_Q4 = SCOPE_IDENTITY();

    INSERT INTO [diagnostic].[Questions] ([ExternalId], [ProjectFormId], [TopicId], [QuestionText], [QuestionType], [StageApplicability], [SortOrder], [BlockGroup], [IsOptional])
    VALUES (NEWID(), @ProjectForm2Id, 5, N'¿Cuántos empleos directos ha generado o proyecta generar?', 2, 1, 5, N'Impacto Económico', 0);
    SET @PF2_Q5 = SCOPE_IDENTITY();

    INSERT INTO [diagnostic].[AnswerOptions] ([QuestionId], [OptionText], [Score], [SwotClassification], [OdsrOrientation], [SortOrder])
    VALUES
        (@PF2_Q5, N'Solo autoempleo (1 persona)',                    1.00, 2, 0, 1),
        (@PF2_Q5, N'2-5 empleos',                                   2.00, 3, 0, 2),
        (@PF2_Q5, N'6-20 empleos',                                  3.00, 1, 0, 3),
        (@PF2_Q5, N'Más de 20 empleos',                             4.00, 1, 0, 4);
END

-- ------------------------------------------------------------------------------------------
-- Questions for ProjectForm 3 (Diagnóstico Inicial -> Proyecto Digital)
-- ------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [diagnostic].[Questions] WHERE [ProjectFormId] = @ProjectForm3Id)
BEGIN
    DECLARE @PF3_Q1 BIGINT, @PF3_Q2 BIGINT, @PF3_Q3 BIGINT, @PF3_Q4 BIGINT, @PF3_Q5 BIGINT, @PF3_Q6 BIGINT;

    INSERT INTO [diagnostic].[Questions] ([ExternalId], [ProjectFormId], [TopicId], [QuestionText], [QuestionType], [StageApplicability], [SortOrder], [BlockGroup], [IsOptional])
    VALUES (NEWID(), @ProjectForm3Id, 1, N'Describa brevemente su modelo de negocio y propuesta de valor', 0, 0, 1, N'Modelo de Negocio', 0);
    SET @PF3_Q1 = SCOPE_IDENTITY();

    INSERT INTO [diagnostic].[Questions] ([ExternalId], [ProjectFormId], [TopicId], [QuestionText], [QuestionType], [StageApplicability], [SortOrder], [BlockGroup], [IsOptional])
    VALUES (NEWID(), @ProjectForm3Id, 2, N'¿Cuál es el nivel de experiencia del equipo fundador en el sector?', 2, 0, 2, N'Equipo', 0);
    SET @PF3_Q2 = SCOPE_IDENTITY();

    INSERT INTO [diagnostic].[AnswerOptions] ([QuestionId], [OptionText], [Score], [SwotClassification], [OdsrOrientation], [SortOrder])
    VALUES
        (@PF3_Q2, N'Sin experiencia previa en el sector',           1.00, 2, 0, 1),
        (@PF3_Q2, N'1-2 años de experiencia en el sector',          2.00, 0, 0, 2),
        (@PF3_Q2, N'3-5 años de experiencia en el sector',          3.00, 1, 0, 3),
        (@PF3_Q2, N'Más de 5 años de experiencia especializada',    4.00, 1, 0, 4);

    INSERT INTO [diagnostic].[Questions] ([ExternalId], [ProjectFormId], [TopicId], [QuestionText], [QuestionType], [StageApplicability], [SortOrder], [BlockGroup], [IsOptional])
    VALUES (NEWID(), @ProjectForm3Id, 3, N'¿Cuántos clientes potenciales ha identificado en su mercado objetivo?', 1, 0, 3, N'Mercado', 0);
    SET @PF3_Q3 = SCOPE_IDENTITY();

    INSERT INTO [diagnostic].[Questions] ([ExternalId], [ProjectFormId], [TopicId], [QuestionText], [QuestionType], [StageApplicability], [SortOrder], [BlockGroup], [IsOptional])
    VALUES (NEWID(), @ProjectForm3Id, 4, N'¿Tiene definido un modelo de ingresos claro?', 2, 0, 4, N'Finanzas', 0);
    SET @PF3_Q4 = SCOPE_IDENTITY();

    INSERT INTO [diagnostic].[AnswerOptions] ([QuestionId], [OptionText], [Score], [SwotClassification], [OdsrOrientation], [SortOrder])
    VALUES
        (@PF3_Q4, N'No tengo modelo de ingresos definido',          1.00, 2, 0, 1),
        (@PF3_Q4, N'Tengo una idea general pero no está validada',  2.00, 4, 0, 2),
        (@PF3_Q4, N'Modelo definido con proyecciones iniciales',    3.00, 3, 0, 3),
        (@PF3_Q4, N'Modelo validado con ingresos reales',           4.00, 1, 0, 4);

    INSERT INTO [diagnostic].[Questions] ([ExternalId], [ProjectFormId], [TopicId], [QuestionText], [QuestionType], [StageApplicability], [SortOrder], [BlockGroup], [IsOptional])
    VALUES (NEWID(), @ProjectForm3Id, 3, N'¿Cuál es su principal ventaja competitiva frente a alternativas existentes?', 2, 0, 5, N'Mercado', 0);
    SET @PF3_Q5 = SCOPE_IDENTITY();

    INSERT INTO [diagnostic].[AnswerOptions] ([QuestionId], [OptionText], [Score], [SwotClassification], [OdsrOrientation], [SortOrder])
    VALUES
        (@PF3_Q5, N'No he identificado una ventaja clara',           1.00, 2, 0, 1),
        (@PF3_Q5, N'Precio más competitivo',                         2.00, 3, 0, 2),
        (@PF3_Q5, N'Tecnología o innovación diferenciadora',         3.00, 1, 0, 3),
        (@PF3_Q5, N'Acceso exclusivo a recursos o mercado',          4.00, 1, 0, 4);

    INSERT INTO [diagnostic].[Questions] ([ExternalId], [ProjectFormId], [TopicId], [QuestionText], [QuestionType], [StageApplicability], [SortOrder], [BlockGroup], [IsOptional])
    VALUES (NEWID(), @ProjectForm3Id, 1, N'¿Cuál es el mayor desafío que enfrenta actualmente su emprendimiento?', 0, 0, 6, N'Modelo de Negocio', 1);
    SET @PF3_Q6 = SCOPE_IDENTITY();
END


-- ==========================================================================================
-- SECTION 9: Diagnostic Responses (sample, without QuestionResponses)
-- ==========================================================================================
-- EvaluationStage: 0 = Initial, 1 = Final
-- Unique constraint: (ProjectFormId, EntrepreneurUserId, EvaluationStage)

-- ------------------------------------------------------------------------------------------
-- DiagnosticResponse 1: Entrepreneur 1 -> ProjectForm 1 (Initial stage)
-- ------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [diagnostic].[DiagnosticResponses] WHERE [ProjectFormId] = @ProjectForm1Id AND [EntrepreneurUserId] = @Entrep1Id AND [EvaluationStage] = 0)
BEGIN
    INSERT INTO [diagnostic].[DiagnosticResponses] ([ExternalId], [ProjectFormId], [ProjectId], [IncubatorId], [EntrepreneurUserId], [EvaluationStage], [IsCompleted], [CompletedAtUtc], [CreatedAtUtc])
    VALUES (NEWID(), @ProjectForm1Id, @Project1Id, @Incubator1Id, @Entrep1Id, 0, 0, NULL, @Now);
END

-- ------------------------------------------------------------------------------------------
-- DiagnosticResponse 2: Entrepreneur 2 -> ProjectForm 3 (Initial stage)
-- ------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [diagnostic].[DiagnosticResponses] WHERE [ProjectFormId] = @ProjectForm3Id AND [EntrepreneurUserId] = @Entrep2Id AND [EvaluationStage] = 0)
BEGIN
    INSERT INTO [diagnostic].[DiagnosticResponses] ([ExternalId], [ProjectFormId], [ProjectId], [IncubatorId], [EntrepreneurUserId], [EvaluationStage], [IsCompleted], [CompletedAtUtc], [CreatedAtUtc])
    VALUES (NEWID(), @ProjectForm3Id, @Project3Id, @Incubator2Id, @Entrep2Id, 0, 0, NULL, @Now);
END


-- ==========================================================================================
-- Done
-- ==========================================================================================
PRINT '[004.SeedTestData.sql] Test seed data applied successfully.';
