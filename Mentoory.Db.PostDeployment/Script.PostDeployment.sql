-- Enable SQLCMD mode in project settings for this to work.
-- This script will be executed after the main deployment script.
-- It is useful for seeding data, etc.
-- --------------------------------------------------------------------------------------

PRINT '[Script.PostDeployment.sql] Starting';
GO

-- All scripts now run unconditionally for consistent deployment.
-- Each :r include is followed by a GO batch separator so local variables
-- (e.g., DECLARE @Now) are scoped per seed file and don't collide when
-- multiple scripts are concatenated into one post-deployment blob.

PRINT '[000.SeedExample.sql] Starting';
GO
:r ./000.SeedExample.sql
GO
PRINT '[000.SeedExample.sql] Finished';
GO

PRINT '[001.SeedRoles.sql] Starting';
GO
:r ./001.SeedRoles.sql
GO
PRINT '[001.SeedRoles.sql] Finished';
GO

PRINT '[002.SeedGlobalAdmin.sql] Starting';
GO
:r ./002.SeedGlobalAdmin.sql
GO
PRINT '[002.SeedGlobalAdmin.sql] Finished';
GO

PRINT '[003.SeedDefaultSubscriptionPlan.sql] Starting';
GO
:r ./003.SeedDefaultSubscriptionPlan.sql
GO
PRINT '[003.SeedDefaultSubscriptionPlan.sql] Finished';
GO

PRINT '[004.SeedTestData.sql] Starting';
GO
:r ./004.SeedTestData.sql
GO
PRINT '[004.SeedTestData.sql] Finished';
GO

PRINT '[005.SeedKnowledgeData.sql] Starting';
GO
:r ./005.SeedKnowledgeData.sql
GO
PRINT '[005.SeedKnowledgeData.sql] Finished';
GO

PRINT '[016.SeedCountries.sql] Starting';
GO
:r ./016.SeedCountries.sql
GO
PRINT '[016.SeedCountries.sql] Finished';
GO

PRINT '[017.SeedSystemConfiguration.sql] Starting';
GO
:r ./017.SeedSystemConfiguration.sql
GO
PRINT '[017.SeedSystemConfiguration.sql] Finished';
GO

PRINT '[018.SeedProjectPublicFlag.sql] Starting';
GO
:r ./018.SeedProjectPublicFlag.sql
GO
PRINT '[018.SeedProjectPublicFlag.sql] Finished';
GO

PRINT '[Script.PostDeployment.sql] Finished';