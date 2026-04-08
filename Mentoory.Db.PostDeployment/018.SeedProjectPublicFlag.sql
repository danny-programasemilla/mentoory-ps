-- Set IsPublic=0 for all existing projects (defensive — matches column default)
UPDATE [tenant].[Projects]
SET [IsPublic] = 0
WHERE [IsPublic] IS NULL
