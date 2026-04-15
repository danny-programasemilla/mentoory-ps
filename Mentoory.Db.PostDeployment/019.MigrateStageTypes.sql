-- =============================================
-- 019.MigrateStageTypes.sql
-- Migrate ProjectStages from 7-type model to 4-type model
-- Old: Registration=0, Forms=1, Analysis=2, LearningAssignment=3, Mentoring=4, FinalEvaluation=5, Closure=6
-- New: Registration=0, Diagnosis=1, Mentorship=2, Closure=3
-- Also adds Position, DisplayName, ExternalId for configurable pipeline
-- =============================================

-- Only run if old stage type values exist (Forms=1 with old meaning, etc.)
IF EXISTS (SELECT 1 FROM [tenant].[ProjectStages] WHERE [StageType] > 3)
BEGIN
    PRINT '019: Migrating old stage types to new 4-type model...';

    -- Map old types to new types:
    -- Registration(0) → Registration(0) [unchanged]
    -- Forms(1) → Diagnosis(1)
    -- Analysis(2) → Diagnosis(1)
    -- LearningAssignment(3) → Mentorship(2)
    -- Mentoring(4) → Mentorship(2)
    -- FinalEvaluation(5) → Diagnosis(1)
    -- Closure(6) → Closure(3)
    UPDATE [tenant].[ProjectStages]
    SET [StageType] = CASE [StageType]
        WHEN 0 THEN 0  -- Registration → Registration
        WHEN 1 THEN 1  -- Forms → Diagnosis
        WHEN 2 THEN 1  -- Analysis → Diagnosis
        WHEN 3 THEN 2  -- LearningAssignment → Mentorship
        WHEN 4 THEN 2  -- Mentoring → Mentorship
        WHEN 5 THEN 1  -- FinalEvaluation → Diagnosis
        WHEN 6 THEN 3  -- Closure → Closure
    END;

    PRINT '019: Stage types migrated.';
END

-- Generate positions for stages that don't have them yet
-- Position is based on creation order (Id) within each project
IF EXISTS (SELECT 1 FROM [tenant].[ProjectStages] WHERE [Position] = 0 AND [StageType] != 0)
BEGIN
    PRINT '019: Generating positions for existing stages...';

    ;WITH OrderedStages AS (
        SELECT [Id], [ProjectId],
               ROW_NUMBER() OVER (PARTITION BY [ProjectId] ORDER BY [Id]) - 1 AS NewPosition
        FROM [tenant].[ProjectStages]
    )
    UPDATE ps
    SET ps.[Position] = os.NewPosition
    FROM [tenant].[ProjectStages] ps
    INNER JOIN OrderedStages os ON ps.[Id] = os.[Id];

    PRINT '019: Positions generated.';
END

-- Generate display names for stages that don't have them yet
IF EXISTS (SELECT 1 FROM [tenant].[ProjectStages] WHERE [DisplayName] = '' OR [DisplayName] IS NULL)
BEGIN
    PRINT '019: Generating display names...';

    ;WITH StageCounts AS (
        SELECT [Id], [ProjectId], [StageType], [Position],
               COUNT(*) OVER (PARTITION BY [ProjectId], [StageType]) AS TypeCount,
               ROW_NUMBER() OVER (PARTITION BY [ProjectId], [StageType] ORDER BY [Position]) AS TypeOrdinal
        FROM [tenant].[ProjectStages]
    )
    UPDATE ps
    SET ps.[DisplayName] = CASE
        WHEN sc.TypeCount = 1 THEN
            CASE sc.[StageType]
                WHEN 0 THEN N'Registro'
                WHEN 1 THEN N'Diagnóstico'
                WHEN 2 THEN N'Mentoría'
                WHEN 3 THEN N'Cierre'
            END
        ELSE
            CASE sc.[StageType]
                WHEN 0 THEN N'Registro ' + CAST(sc.TypeOrdinal AS NVARCHAR(10))
                WHEN 1 THEN N'Diagnóstico ' + CAST(sc.TypeOrdinal AS NVARCHAR(10))
                WHEN 2 THEN N'Mentoría ' + CAST(sc.TypeOrdinal AS NVARCHAR(10))
                WHEN 3 THEN N'Cierre ' + CAST(sc.TypeOrdinal AS NVARCHAR(10))
            END
    END
    FROM [tenant].[ProjectStages] ps
    INNER JOIN StageCounts sc ON ps.[Id] = sc.[Id]
    WHERE ps.[DisplayName] = '' OR ps.[DisplayName] IS NULL;

    PRINT '019: Display names generated.';
END

-- Generate ExternalIds for stages that don't have them
IF EXISTS (SELECT 1 FROM [tenant].[ProjectStages] WHERE [ExternalId] = '00000000-0000-0000-0000-000000000000')
BEGIN
    PRINT '019: Generating ExternalIds for existing stages...';

    UPDATE [tenant].[ProjectStages]
    SET [ExternalId] = NEWID()
    WHERE [ExternalId] = '00000000-0000-0000-0000-000000000000';

    PRINT '019: ExternalIds generated.';
END

PRINT '019: MigrateStageTypes complete.';
