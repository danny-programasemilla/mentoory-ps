-- Enable SQLCMD mode in project settings for this to work.
-- This script will be executed after the main deployment script.
-- It is useful for seeding data, etc.
-- --------------------------------------------------------------------------------------

PRINT '[Script.PostDeployment.sql] Starting';

-- All scripts now run unconditionally for consistent deployment
PRINT '[000.SeedExample.sql] Starting';
:r ./000.SeedExample.sql
PRINT '[000.SeedExample.sql] Finished';

PRINT '[Script.PostDeployment.sql] Finished';