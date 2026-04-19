-- ==========================================================================================
-- 005.SeedKnowledgeData.sql
-- Seeds the Knowledge module sample data (spec 016-knowledge-module-core):
--   * Sample KnowledgeStructureTemplate "Emprendimiento Básico" with one Module / Topic /
--     Subject / Resource set.
--   * Updates diagnostic.FormTemplates.DefaultKnowledgeStructureTemplateId for the existing
--     diagnostic seed templates to point at "Emprendimiento Básico".
-- NOTE: The project-side knowledge.Topics rows 1-5 (required by the
-- diagnostic.Questions.TopicId FK) are seeded earlier in 004.SeedTestData.sql § 3.5 so
-- the FK resolves mid-batch while 004 is still running.
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
-- SECTION 2: Wire existing diagnostic FormTemplates to the sample KnowledgeStructureTemplate
-- ==========================================================================================

UPDATE [diagnostic].[FormTemplates]
SET [DefaultKnowledgeStructureTemplateExternalId] = @TemplateExternalId
WHERE [Name] = N'Diagnóstico Inicial Estándar'
  AND ([DefaultKnowledgeStructureTemplateExternalId] IS NULL OR [DefaultKnowledgeStructureTemplateExternalId] != @TemplateExternalId);

UPDATE [diagnostic].[FormTemplates]
SET [DefaultKnowledgeStructureTemplateExternalId] = @TemplateExternalId
WHERE [Name] = N'Diagnóstico de Impacto'
  AND ([DefaultKnowledgeStructureTemplateExternalId] IS NULL OR [DefaultKnowledgeStructureTemplateExternalId] != @TemplateExternalId);
