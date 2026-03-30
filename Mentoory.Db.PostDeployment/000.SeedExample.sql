-- ==========================================================================================
-- Post-Deployment Example Script
-- ==========================================================================================

-- Insert or Update Roles Using MERGE (Never delete roles to preserve custom ones)
MERGE INTO [example].[Examples] AS target
USING (VALUES 
    ('seed #1')
) AS source (Title)
ON target.Title = source.Title
WHEN NOT MATCHED THEN
    INSERT (Title, DateCreated)
    VALUES (source.Title, GETDATE())

; -- Sepparator semicolon after MERGE statement
