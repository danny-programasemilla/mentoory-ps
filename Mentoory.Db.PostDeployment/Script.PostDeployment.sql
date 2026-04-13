-- Enable SQLCMD mode in project settings for this to work.
-- This script will be executed after the main deployment script.
-- It is useful for seeding data, etc.
-- --------------------------------------------------------------------------------------

PRINT '[Script.PostDeployment.sql] Starting';

-- All scripts now run unconditionally for consistent deployment

PRINT '[000.SeedExample.sql] Starting';
:r ./000.SeedExample.sql
PRINT '[000.SeedExample.sql] Finished';

PRINT '[001.SeedRoles.sql] Starting';
:r ./001.SeedRoles.sql
PRINT '[001.SeedRoles.sql] Finished';

PRINT '[002.SeedGlobalAdmin.sql] Starting';
:r ./002.SeedGlobalAdmin.sql
PRINT '[002.SeedGlobalAdmin.sql] Finished';

PRINT '[003.SeedDefaultSubscriptionPlan.sql] Starting';
:r ./003.SeedDefaultSubscriptionPlan.sql
PRINT '[003.SeedDefaultSubscriptionPlan.sql] Finished';

PRINT '[004.SeedTestData.sql] Starting';
:r ./004.SeedTestData.sql
PRINT '[004.SeedTestData.sql] Finished';

PRINT '[016.SeedCountries.sql] Starting';
:r ./016.SeedCountries.sql
PRINT '[016.SeedCountries.sql] Finished';

PRINT '[017.SeedSystemConfiguration.sql] Starting';
:r ./017.SeedSystemConfiguration.sql
PRINT '[017.SeedSystemConfiguration.sql] Finished';

PRINT '[018.SeedProjectPublicFlag.sql] Starting';
:r ./018.SeedProjectPublicFlag.sql
PRINT '[018.SeedProjectPublicFlag.sql] Finished';

PRINT '[019.SeedTenantSystemConfiguration.sql] Starting';
:r ./019.SeedTenantSystemConfiguration.sql
PRINT '[019.SeedTenantSystemConfiguration.sql] Finished';

PRINT '[Script.PostDeployment.sql] Finished';