/* =============================================================================
   04_Security.sql
   Least privilege: the API connects as a login that can only read and write
   rows in dbo. It can't change the schema and isn't sysadmin.

   Run with SQLCMD variables:
     sqlcmd ... -v AppLogin="catalog_api" AppPassword="<strong password>"
   In production, use Entra ID (Azure AD) / Managed Identity, not SQL logins.
   ============================================================================= */
SET NOCOUNT ON;
GO

USE master;
GO
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'$(AppLogin)')
BEGIN
    CREATE LOGIN [$(AppLogin)] WITH PASSWORD = N'$(AppPassword)', CHECK_POLICY = ON, DEFAULT_DATABASE = NorthwindCatalog;
END
ELSE
BEGIN
    -- Keep the password in sync with the secret store on redeploys
    ALTER LOGIN [$(AppLogin)] WITH PASSWORD = N'$(AppPassword)';
END
GO

USE NorthwindCatalog;
GO
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'$(AppLogin)')
    CREATE USER [$(AppLogin)] FOR LOGIN [$(AppLogin)];
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'catalog_app_role' AND type = 'R')
    CREATE ROLE catalog_app_role;
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::dbo TO catalog_app_role;
GO

IF IS_ROLEMEMBER(N'catalog_app_role', N'$(AppLogin)') = 0
    ALTER ROLE catalog_app_role ADD MEMBER [$(AppLogin)];
GO

PRINT 'Security configured for $(AppLogin).';
GO
