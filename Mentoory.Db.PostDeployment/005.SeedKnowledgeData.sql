-- ==========================================================================================
-- 005.SeedKnowledgeData.sql
-- Seeds the Knowledge module sample data (spec 016-knowledge-module-core):
--   * Sample KnowledgeStructureTemplate "Emprendimiento Básico" with one Module / Topic /
--     Subject / Resource set.
--   * Project-side knowledge.Topics rows with fixed Ids 1-5 (IDENTITY_INSERT) to match the
--     literal TopicId values referenced by existing diagnostic seed data in
--     004.SeedTestData.sql (diagnostic.Questions and diagnostic.QuestionTemplates).
--   * Updates diagnostic.FormTemplates.DefaultKnowledgeStructureTemplateId for the existing
--     diagnostic seed templates to point at "Emprendimiento Básico".
-- Idempotent: guarded with IF NOT EXISTS / conditional UPDATE checks. Safe to re-run.
-- ==========================================================================================

SET NOCOUNT ON;

DECLARE @Now DATETIME2(3) = SYSUTCDATETIME();

-- ==========================================================================================
-- SECTION 1: KnowledgeStructureTemplate "Emprendimiento Básico"
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

-- ------------------------------------------------------------------------------------------
-- ModuleTemplate: "Ideación"
-- ------------------------------------------------------------------------------------------
DECLARE @ModuleTemplateExternalId UNIQUEIDENTIFIER = CAST('22222222-2222-2222-2222-222222222222' AS UNIQUEIDENTIFIER);
DECLARE @ModuleTemplateId BIGINT;

IF NOT EXISTS (SELECT 1 FROM [knowledge].[ModuleTemplates] WHERE [ExternalId] = @ModuleTemplateExternalId)
BEGIN
    INSERT INTO [knowledge].[ModuleTemplates] ([ExternalId], [KnowledgeStructureTemplateId], [Name], [Description], [SortOrder])
    VALUES (@ModuleTemplateExternalId, @StructureTemplateId, N'Ideación', N'Generación y validación temprana de ideas.', 1);

    SET @ModuleTemplateId = SCOPE_IDENTITY();
END
ELSE
    SELECT @ModuleTemplateId = [Id] FROM [knowledge].[ModuleTemplates] WHERE [ExternalId] = @ModuleTemplateExternalId;

-- ------------------------------------------------------------------------------------------
-- TopicTemplate: "Propuesta de valor"
-- ------------------------------------------------------------------------------------------
DECLARE @TopicTemplateExternalId UNIQUEIDENTIFIER = CAST('33333333-3333-3333-3333-333333333333' AS UNIQUEIDENTIFIER);
DECLARE @TopicTemplateId BIGINT;

IF NOT EXISTS (SELECT 1 FROM [knowledge].[TopicTemplates] WHERE [ExternalId] = @TopicTemplateExternalId)
BEGIN
    INSERT INTO [knowledge].[TopicTemplates] ([ExternalId], [ModuleTemplateId], [Name], [Description], [SortOrder],
        [HighRangeMin], [HighRangeMax], [MediumRangeMin], [MediumRangeMax], [LowRangeMin], [LowRangeMax])
    VALUES (@TopicTemplateExternalId, @ModuleTemplateId, N'Propuesta de valor', N'Claridad sobre el valor entregado al cliente.', 1,
        8.00, 10.00, 5.00, 7.99, 0.00, 4.99);

    SET @TopicTemplateId = SCOPE_IDENTITY();
END
ELSE
    SELECT @TopicTemplateId = [Id] FROM [knowledge].[TopicTemplates] WHERE [ExternalId] = @TopicTemplateExternalId;

-- ------------------------------------------------------------------------------------------
-- SubjectTemplate: "Definición"
-- ------------------------------------------------------------------------------------------
DECLARE @SubjectTemplateExternalId UNIQUEIDENTIFIER = CAST('44444444-4444-4444-4444-444444444444' AS UNIQUEIDENTIFIER);
DECLARE @SubjectTemplateId BIGINT;

IF NOT EXISTS (SELECT 1 FROM [knowledge].[SubjectTemplates] WHERE [ExternalId] = @SubjectTemplateExternalId)
BEGIN
    INSERT INTO [knowledge].[SubjectTemplates] ([ExternalId], [TopicTemplateId], [Name], [Description], [SortOrder])
    VALUES (@SubjectTemplateExternalId, @TopicTemplateId, N'Definición', N'Formular la propuesta de valor.', 1);

    SET @SubjectTemplateId = SCOPE_IDENTITY();
END
ELSE
    SELECT @SubjectTemplateId = [Id] FROM [knowledge].[SubjectTemplates] WHERE [ExternalId] = @SubjectTemplateExternalId;

-- ------------------------------------------------------------------------------------------
-- ResourceTemplates: Video, Link, File
-- ------------------------------------------------------------------------------------------
DECLARE @ResourceVideoExternalId UNIQUEIDENTIFIER = CAST('55555555-5555-5555-5555-555555555551' AS UNIQUEIDENTIFIER);
DECLARE @ResourceLinkExternalId  UNIQUEIDENTIFIER = CAST('55555555-5555-5555-5555-555555555552' AS UNIQUEIDENTIFIER);
DECLARE @ResourceFileExternalId  UNIQUEIDENTIFIER = CAST('55555555-5555-5555-5555-555555555553' AS UNIQUEIDENTIFIER);

IF NOT EXISTS (SELECT 1 FROM [knowledge].[ResourceTemplates] WHERE [ExternalId] = @ResourceVideoExternalId)
BEGIN
    INSERT INTO [knowledge].[ResourceTemplates] ([ExternalId], [SubjectTemplateId], [Title], [Description], [Url], [ResourceType], [SortOrder])
    VALUES (@ResourceVideoExternalId, @SubjectTemplateId, N'Introducción a Propuesta de Valor (video)', NULL, N'https://example.com/video-propuesta-valor', 0, 1);
END

IF NOT EXISTS (SELECT 1 FROM [knowledge].[ResourceTemplates] WHERE [ExternalId] = @ResourceLinkExternalId)
BEGIN
    INSERT INTO [knowledge].[ResourceTemplates] ([ExternalId], [SubjectTemplateId], [Title], [Description], [Url], [ResourceType], [SortOrder])
    VALUES (@ResourceLinkExternalId, @SubjectTemplateId, N'Canvas de Propuesta de Valor (enlace)', NULL, N'https://example.com/canvas-link', 1, 2);
END

IF NOT EXISTS (SELECT 1 FROM [knowledge].[ResourceTemplates] WHERE [ExternalId] = @ResourceFileExternalId)
BEGIN
    INSERT INTO [knowledge].[ResourceTemplates] ([ExternalId], [SubjectTemplateId], [Title], [Description], [Url], [ResourceType], [SortOrder])
    VALUES (@ResourceFileExternalId, @SubjectTemplateId, N'Plantilla PDF descargable', NULL, N'https://example.com/plantilla.pdf', 2, 3);
END


-- ==========================================================================================
-- SECTION 2: Project-side knowledge hierarchy with fixed Topic Ids 1-5
-- ------------------------------------------------------------------------------------------
-- Existing seeds (004.SeedTestData.sql) insert literal TopicId values 1-5 into both
-- diagnostic.Questions and diagnostic.QuestionTemplates, with the convention:
--   1=Modelo de Negocio, 2=Equipo, 3=Mercado, 4=Finanzas, 5=Impacto
-- The FK diagnostic.Questions.TopicId -> knowledge.Topics.Id requires these exact Ids to
-- exist. We seed them here using IDENTITY_INSERT. A parent KnowledgeStructure + Module is
-- created for Proyecto Innovación (the project that owns the seeded ProjectForms).
-- ==========================================================================================

DECLARE @ProjectInnovacionId BIGINT;
DECLARE @IncubatorAlphaId BIGINT;

SELECT @IncubatorAlphaId = [Id] FROM [tenant].[Incubators] WHERE [Name] = N'Incubadora Alpha';
SELECT @ProjectInnovacionId = [Id] FROM [tenant].[Projects]
    WHERE [Name] = N'Proyecto Innovación' AND [IncubatorId] = @IncubatorAlphaId;

-- Only seed the project-side hierarchy if the target project exists (i.e., 004.SeedTestData
-- ran). Otherwise skip — test data is gated and the FK values don't matter in prod.
IF @ProjectInnovacionId IS NOT NULL
BEGIN
    -- --------------------------------------------------------------------------------------
    -- KnowledgeStructure for Proyecto Innovación
    -- --------------------------------------------------------------------------------------
    DECLARE @ProjectStructureExternalId UNIQUEIDENTIFIER = CAST('99999999-9999-9999-9999-999999999901' AS UNIQUEIDENTIFIER);
    DECLARE @ProjectStructureId BIGINT;

    IF NOT EXISTS (SELECT 1 FROM [knowledge].[KnowledgeStructures] WHERE [ExternalId] = @ProjectStructureExternalId)
    BEGIN
        INSERT INTO [knowledge].[KnowledgeStructures] ([ExternalId], [ProjectId], [IncubatorId], [Name], [Description],
            [SourceTemplateId], [SourceTemplateVersion], [SyncMode], [CreatedAtUtc])
        VALUES (@ProjectStructureExternalId, @ProjectInnovacionId, @IncubatorAlphaId,
            N'Estructura de Conocimiento - Proyecto Innovación',
            N'Estructura de conocimiento sembrada para alinear con los temas del diagnóstico de prueba.',
            @StructureTemplateId, 1, 0, @Now);

        SET @ProjectStructureId = SCOPE_IDENTITY();
    END
    ELSE
        SELECT @ProjectStructureId = [Id] FROM [knowledge].[KnowledgeStructures] WHERE [ExternalId] = @ProjectStructureExternalId;

    -- --------------------------------------------------------------------------------------
    -- Module under the KnowledgeStructure (parent for the fixed-Id Topics)
    -- --------------------------------------------------------------------------------------
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

    -- --------------------------------------------------------------------------------------
    -- Topics with fixed Ids 1-5 (IDENTITY_INSERT) — must match literal TopicIds used by
    -- existing diagnostic seed data in 004.SeedTestData.sql.
    -- Convention: 1=Modelo de Negocio, 2=Equipo, 3=Mercado, 4=Finanzas, 5=Impacto
    -- --------------------------------------------------------------------------------------
    DECLARE @TopicExternalId1 UNIQUEIDENTIFIER = CAST('99999999-9999-9999-9999-999999990001' AS UNIQUEIDENTIFIER);
    DECLARE @TopicExternalId2 UNIQUEIDENTIFIER = CAST('99999999-9999-9999-9999-999999990002' AS UNIQUEIDENTIFIER);
    DECLARE @TopicExternalId3 UNIQUEIDENTIFIER = CAST('99999999-9999-9999-9999-999999990003' AS UNIQUEIDENTIFIER);
    DECLARE @TopicExternalId4 UNIQUEIDENTIFIER = CAST('99999999-9999-9999-9999-999999990004' AS UNIQUEIDENTIFIER);
    DECLARE @TopicExternalId5 UNIQUEIDENTIFIER = CAST('99999999-9999-9999-9999-999999990005' AS UNIQUEIDENTIFIER);

    SET IDENTITY_INSERT [knowledge].[Topics] ON;

    IF NOT EXISTS (SELECT 1 FROM [knowledge].[Topics] WHERE [Id] = 1)
    BEGIN
        INSERT INTO [knowledge].[Topics] ([Id], [ExternalId], [ModuleId], [SourceTemplateTopicExternalId],
            [Name], [Description], [SortOrder],
            [HighRangeMin], [HighRangeMax], [MediumRangeMin], [MediumRangeMax], [LowRangeMin], [LowRangeMax])
        VALUES (1, @TopicExternalId1, @ProjectModuleId, NULL,
            N'Modelo de Negocio', N'Tema referenciado por preguntas de diagnóstico.', 1,
            8.00, 10.00, 5.00, 7.99, 0.00, 4.99);
    END

    IF NOT EXISTS (SELECT 1 FROM [knowledge].[Topics] WHERE [Id] = 2)
    BEGIN
        INSERT INTO [knowledge].[Topics] ([Id], [ExternalId], [ModuleId], [SourceTemplateTopicExternalId],
            [Name], [Description], [SortOrder],
            [HighRangeMin], [HighRangeMax], [MediumRangeMin], [MediumRangeMax], [LowRangeMin], [LowRangeMax])
        VALUES (2, @TopicExternalId2, @ProjectModuleId, NULL,
            N'Equipo', N'Tema referenciado por preguntas de diagnóstico.', 2,
            8.00, 10.00, 5.00, 7.99, 0.00, 4.99);
    END

    IF NOT EXISTS (SELECT 1 FROM [knowledge].[Topics] WHERE [Id] = 3)
    BEGIN
        INSERT INTO [knowledge].[Topics] ([Id], [ExternalId], [ModuleId], [SourceTemplateTopicExternalId],
            [Name], [Description], [SortOrder],
            [HighRangeMin], [HighRangeMax], [MediumRangeMin], [MediumRangeMax], [LowRangeMin], [LowRangeMax])
        VALUES (3, @TopicExternalId3, @ProjectModuleId, NULL,
            N'Mercado', N'Tema referenciado por preguntas de diagnóstico.', 3,
            8.00, 10.00, 5.00, 7.99, 0.00, 4.99);
    END

    IF NOT EXISTS (SELECT 1 FROM [knowledge].[Topics] WHERE [Id] = 4)
    BEGIN
        INSERT INTO [knowledge].[Topics] ([Id], [ExternalId], [ModuleId], [SourceTemplateTopicExternalId],
            [Name], [Description], [SortOrder],
            [HighRangeMin], [HighRangeMax], [MediumRangeMin], [MediumRangeMax], [LowRangeMin], [LowRangeMax])
        VALUES (4, @TopicExternalId4, @ProjectModuleId, NULL,
            N'Finanzas', N'Tema referenciado por preguntas de diagnóstico.', 4,
            8.00, 10.00, 5.00, 7.99, 0.00, 4.99);
    END

    IF NOT EXISTS (SELECT 1 FROM [knowledge].[Topics] WHERE [Id] = 5)
    BEGIN
        INSERT INTO [knowledge].[Topics] ([Id], [ExternalId], [ModuleId], [SourceTemplateTopicExternalId],
            [Name], [Description], [SortOrder],
            [HighRangeMin], [HighRangeMax], [MediumRangeMin], [MediumRangeMax], [LowRangeMin], [LowRangeMax])
        VALUES (5, @TopicExternalId5, @ProjectModuleId, NULL,
            N'Impacto', N'Tema referenciado por preguntas de diagnóstico.', 5,
            8.00, 10.00, 5.00, 7.99, 0.00, 4.99);
    END

    SET IDENTITY_INSERT [knowledge].[Topics] OFF;
END


-- ==========================================================================================
-- SECTION 3: Wire existing diagnostic FormTemplates to the sample KnowledgeStructureTemplate
-- ==========================================================================================

UPDATE [diagnostic].[FormTemplates]
SET [DefaultKnowledgeStructureTemplateId] = @StructureTemplateId
WHERE [Name] = N'Diagnóstico Inicial Estándar'
  AND ([DefaultKnowledgeStructureTemplateId] IS NULL OR [DefaultKnowledgeStructureTemplateId] != @StructureTemplateId);

UPDATE [diagnostic].[FormTemplates]
SET [DefaultKnowledgeStructureTemplateId] = @StructureTemplateId
WHERE [Name] = N'Diagnóstico de Impacto'
  AND ([DefaultKnowledgeStructureTemplateId] IS NULL OR [DefaultKnowledgeStructureTemplateId] != @StructureTemplateId);
